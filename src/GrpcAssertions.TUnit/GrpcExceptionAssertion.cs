using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcAssertions;
using TUnit.Assertions.Core;

namespace GrpcAssertions.TUnit;

/// <summary>
/// Fluent TUnit assertion that a delegate threw a gRPC <see cref="RpcException"/>, optionally with
/// an expected <see cref="StatusCode"/> and an expected <see cref="Status.Detail"/>. Constructed by
/// the <c>ThrowsGrpcException()</c> entry points on a delegate assertion source; the chain methods
/// (<c>WithDetail</c>, <c>WithDetailContaining</c>, and the <c>IsUnavailable()</c> / <c>IsNotFound()</c>
/// status shorthands) narrow the assertion and return the same instance.
/// </summary>
/// <remarks>
/// The delegate's caught exception is moved into the assertion value by TUnit's
/// <c>MapException&lt;RpcException&gt;()</c>: when the delegate throws an <see cref="RpcException"/>
/// it is the value, when it throws any other exception the value is null and the metadata exception
/// is populated, and when it throws nothing both are null. A single <see cref="CheckAsync"/> applies
/// the accumulated status and detail expectations and renders the full gRPC outcome on failure.
/// </remarks>
public sealed class GrpcExceptionAssertion : Assertion<RpcException>
{
    private readonly List<(string Key, string? Text, byte[]? Bytes)> _expectedTrailers = new();
    private StatusCode? _expectedStatus;
    private bool _hasDetailCheck;
    private bool _detailContains;
    private string? _expectedDetail;
    private StringComparison _detailComparison;

    /// <summary>Initializes the assertion. Called by the <c>ThrowsGrpcException()</c> entry points.</summary>
    /// <param name="context">The assertion context supplied by TUnit (the mapped exception value).</param>
    /// <param name="expectedStatus">The expected status code, or null to accept any status.</param>
    public GrpcExceptionAssertion(AssertionContext<RpcException> context, StatusCode? expectedStatus) : base(context)
    {
        _expectedStatus = expectedStatus;
    }

    /// <summary>Asserts that the exception's <see cref="Status.Detail"/> exactly equals
    /// <paramref name="expected"/> (ordinal comparison).</summary>
    /// <param name="expected">The expected detail. Must not be null.</param>
    /// <returns>This assertion for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expected"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A detail expectation was already specified.</exception>
    public GrpcExceptionAssertion WithDetail(string expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ThrowIfDetailAlreadySpecified();
        _hasDetailCheck = true;
        _detailContains = false;
        _expectedDetail = expected;
        _detailComparison = StringComparison.Ordinal;
        Context.ExpressionBuilder.Append(string.Concat(".WithDetail(\"", EscapeForExpression(expected), "\")"));
        return this;
    }

    /// <summary>Asserts that the exception's <see cref="Status.Detail"/> contains
    /// <paramref name="substring"/> using <paramref name="comparison"/>.</summary>
    /// <param name="substring">The substring the detail must contain. Must not be null.</param>
    /// <param name="comparison">The comparison used to locate the substring.</param>
    /// <returns>This assertion for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="substring"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A detail expectation was already specified.</exception>
    public GrpcExceptionAssertion WithDetailContaining(string substring, StringComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(substring);
        ThrowIfDetailAlreadySpecified();
        _hasDetailCheck = true;
        _detailContains = true;
        _expectedDetail = substring;
        _detailComparison = comparison;
        Context.ExpressionBuilder.Append(string.Concat(".WithDetailContaining(\"", EscapeForExpression(substring), "\", StringComparison.", comparison.ToString(), ")"));
        return this;
    }

    /// <summary>Asserts the exception's <see cref="RpcException.Trailers"/> contain a text entry at
    /// <paramref name="key"/> whose value equals <paramref name="value"/> (ordinal). Keys are matched
    /// case-insensitively (gRPC lowercases metadata keys). Binary (<c>-bin</c>) entries are never
    /// matched by this overload; use <see cref="WithTrailer(string, ReadOnlySpan{byte})"/> for those.</summary>
    /// <param name="key">The trailer key. Must not be null.</param>
    /// <param name="value">The expected text value. Must not be null.</param>
    /// <returns>This assertion for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> is null.</exception>
    public GrpcExceptionAssertion WithTrailer(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        _expectedTrailers.Add((key, value, null));
        Context.ExpressionBuilder.Append(string.Concat(".WithTrailer(\"", EscapeForExpression(key), "\", \"", EscapeForExpression(value), "\")"));
        return this;
    }

    /// <summary>Asserts the exception's <see cref="RpcException.Trailers"/> contain a binary entry at
    /// <paramref name="key"/> (a <c>-bin</c> key) whose bytes equal <paramref name="value"/>. Text
    /// entries are never matched by this overload; use <see cref="WithTrailer(string, string)"/> for
    /// those. A <see cref="byte"/> array converts to <paramref name="value"/> implicitly.</summary>
    /// <param name="key">The binary trailer key (ends in <c>-bin</c>). Must not be null.</param>
    /// <param name="value">The expected bytes.</param>
    /// <returns>This assertion for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    public GrpcExceptionAssertion WithTrailer(string key, ReadOnlySpan<byte> value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _expectedTrailers.Add((key, null, value.ToArray()));
        Context.ExpressionBuilder.Append(string.Concat(".WithTrailer(\"", EscapeForExpression(key), "\", byte[", value.Length.ToString(CultureInfo.InvariantCulture), "])"));
        return this;
    }

    /// <summary>Asserts the status is <see cref="StatusCode.OK"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsOk() => WithStatus(StatusCode.OK, nameof(IsOk));

    /// <summary>Asserts the status is <see cref="StatusCode.Cancelled"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsCancelled() => WithStatus(StatusCode.Cancelled, nameof(IsCancelled));

    /// <summary>Asserts the status is <see cref="StatusCode.InvalidArgument"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsInvalidArgument() => WithStatus(StatusCode.InvalidArgument, nameof(IsInvalidArgument));

    /// <summary>Asserts the status is <see cref="StatusCode.DeadlineExceeded"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsDeadlineExceeded() => WithStatus(StatusCode.DeadlineExceeded, nameof(IsDeadlineExceeded));

    /// <summary>Asserts the status is <see cref="StatusCode.NotFound"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsNotFound() => WithStatus(StatusCode.NotFound, nameof(IsNotFound));

    /// <summary>Asserts the status is <see cref="StatusCode.AlreadyExists"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsAlreadyExists() => WithStatus(StatusCode.AlreadyExists, nameof(IsAlreadyExists));

    /// <summary>Asserts the status is <see cref="StatusCode.PermissionDenied"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsPermissionDenied() => WithStatus(StatusCode.PermissionDenied, nameof(IsPermissionDenied));

    /// <summary>Asserts the status is <see cref="StatusCode.ResourceExhausted"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsResourceExhausted() => WithStatus(StatusCode.ResourceExhausted, nameof(IsResourceExhausted));

    /// <summary>Asserts the status is <see cref="StatusCode.FailedPrecondition"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsFailedPrecondition() => WithStatus(StatusCode.FailedPrecondition, nameof(IsFailedPrecondition));

    /// <summary>Asserts the status is <see cref="StatusCode.Aborted"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsAborted() => WithStatus(StatusCode.Aborted, nameof(IsAborted));

    /// <summary>Asserts the status is <see cref="StatusCode.Unimplemented"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsUnimplemented() => WithStatus(StatusCode.Unimplemented, nameof(IsUnimplemented));

    /// <summary>Asserts the status is <see cref="StatusCode.Internal"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsInternal() => WithStatus(StatusCode.Internal, nameof(IsInternal));

    /// <summary>Asserts the status is <see cref="StatusCode.Unavailable"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsUnavailable() => WithStatus(StatusCode.Unavailable, nameof(IsUnavailable));

    /// <summary>Asserts the status is <see cref="StatusCode.Unauthenticated"/>.</summary>
    /// <returns>This assertion for chaining.</returns>
    public GrpcExceptionAssertion IsUnauthenticated() => WithStatus(StatusCode.Unauthenticated, nameof(IsUnauthenticated));

    /// <inheritdoc/>
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<RpcException> metadata)
    {
        if (metadata.Exception is not null)
        {
            return Task.FromResult(AssertionResult.Failed(string.Concat(
                "it threw ", metadata.Exception.GetType().Name, ": ", metadata.Exception.Message)));
        }

        var rpc = metadata.Value;
        if (rpc is null)
        {
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));
        }

        if (_expectedStatus is StatusCode expected && rpc.StatusCode != expected)
        {
            return Task.FromResult(AssertionResult.Failed(string.Concat("it threw ", GrpcOutcomeRendering.Describe(rpc))));
        }

        if (_hasDetailCheck && !DetailMatches(rpc))
        {
            return Task.FromResult(AssertionResult.Failed(string.Concat("it threw ", GrpcOutcomeRendering.Describe(rpc))));
        }

        if (_expectedTrailers.Exists(expectation => !TrailerMatches(rpc.Trailers, expectation)))
        {
            return Task.FromResult(AssertionResult.Failed(string.Concat(
                "it threw ", GrpcOutcomeRendering.Describe(rpc), DescribeTrailers(rpc.Trailers))));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    /// <inheritdoc/>
    protected override string GetExpectation()
    {
        var status = _expectedStatus is StatusCode s
            ? string.Concat(" with StatusCode ", s.ToString())
            : string.Empty;

        var detail = string.Empty;
        if (_hasDetailCheck)
        {
            var detailPrefix = _detailContains ? " with detail containing \"" : " with detail \"";
            detail = string.Concat(detailPrefix, _expectedDetail, "\"");
        }

        var trailers = string.Empty;
        if (_expectedTrailers.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (var (key, text, bytes) in _expectedTrailers)
            {
                sb.Append(" with trailer \"").Append(key).Append("\" = ");
                sb.Append(bytes is not null
                    ? string.Concat("byte[", bytes.Length.ToString(CultureInfo.InvariantCulture), "]")
                    : string.Concat("\"", text, "\""));
            }

            trailers = sb.ToString();
        }

        return string.Concat("the gRPC call to throw an RpcException", status, detail, trailers);
    }

    // Matches a single trailer expectation against the exception's trailers. Binary (-bin) entries
    // expose their bytes via ValueBytes; calling .Value on a binary entry throws, so the IsBinary
    // guard is load-bearing: a text expectation only inspects text entries, a binary one only inspects
    // binary entries. Keys are compared case-insensitively because gRPC lowercases metadata keys.
    private static bool TrailerMatches(Metadata trailers, (string Key, string? Text, byte[]? Bytes) expectation)
        => trailers.Any(entry =>
            string.Equals(entry.Key, expectation.Key, StringComparison.OrdinalIgnoreCase)
            && (expectation.Bytes is not null
                ? entry.IsBinary && entry.ValueBytes.AsSpan().SequenceEqual(expectation.Bytes)
                : !entry.IsBinary && string.Equals(entry.Value, expectation.Text, StringComparison.Ordinal)));

    // Renders the trailers for a failure message. Binary entries render as "(binary, N bytes)" rather
    // than their value, since their value is not a string (and reading .Value would throw).
    private static string DescribeTrailers(Metadata trailers)
    {
        if (trailers.Count is 0)
        {
            return "; trailers: (none)";
        }

        var sb = new StringBuilder("; trailers: ");
        var first = true;
        foreach (var entry in trailers)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            first = false;
            sb.Append(entry.Key).Append('=');
            if (entry.IsBinary)
            {
                sb.Append("(binary, ").Append(entry.ValueBytes.Length.ToString(CultureInfo.InvariantCulture)).Append(" bytes)");
            }
            else
            {
                sb.Append('"').Append(entry.Value).Append('"');
            }
        }

        return sb.ToString();
    }

    private GrpcExceptionAssertion WithStatus(StatusCode statusCode, string methodName)
    {
        if (_expectedStatus is not null)
        {
            throw new InvalidOperationException(
                "The expected gRPC status code has already been specified; set it once via ThrowsGrpcException(StatusCode) or a single status shorthand.");
        }

        _expectedStatus = statusCode;
        Context.ExpressionBuilder.Append(string.Concat(".", methodName, "()"));
        return this;
    }

    private void ThrowIfDetailAlreadySpecified()
    {
        if (_hasDetailCheck)
        {
            throw new InvalidOperationException(
                "A detail expectation has already been specified; chain a single WithDetail or WithDetailContaining.");
        }
    }

    // Escapes a user-supplied detail so the rendered assertion chain stays a single, unambiguous
    // line even when the value contains quotes, backslashes, or line breaks.
    private static string EscapeForExpression(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

    private bool DetailMatches(RpcException rpc)
        => _detailContains
            ? rpc.Status.Detail.Contains(_expectedDetail!, _detailComparison)
            : string.Equals(rpc.Status.Detail, _expectedDetail, _detailComparison);
}
