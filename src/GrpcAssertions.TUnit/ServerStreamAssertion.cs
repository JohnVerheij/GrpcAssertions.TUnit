using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcAssertions;
using TUnit.Assertions.Core;

namespace GrpcAssertions.TUnit;

/// <summary>
/// Fluent TUnit assertion over the responses of an <see cref="AsyncServerStreamingCall{TResponse}"/>.
/// Constructed by the <c>Streams()</c> entry point on a value assertion source; the chain methods
/// (<c>StreamsAtLeast</c>, <c>StreamsExactly</c>, <c>StreamContains</c>) accumulate expectations and
/// <c>AndStreamItems</c> runs follow-on assertions against the materialized responses.
/// </summary>
/// <remarks>
/// The response stream is read exactly once, when the assertion is awaited: every accumulated
/// expectation, and the optional <c>AndStreamItems</c> callback, runs against the single materialized
/// list. A stream that faults mid-read (an <see cref="RpcException"/> from the reader) fails the
/// assertion with the partial count and the rendered gRPC outcome.
/// </remarks>
/// <typeparam name="TResponse">The streamed response message type.</typeparam>
public sealed class ServerStreamAssertion<TResponse> : Assertion<AsyncServerStreamingCall<TResponse>>
{
    private readonly CancellationToken _cancellationToken;
    private readonly List<(Func<TResponse, bool> Predicate, string? Description)> _contains = new();
    private int? _atLeast;
    private int? _exactly;
    private Func<IReadOnlyList<TResponse>, Task>? _itemsCallback;

    /// <summary>Initializes the assertion. Called by the <c>Streams()</c> entry point.</summary>
    /// <param name="context">The assertion context supplied by TUnit (the streaming call value).</param>
    /// <param name="cancellationToken">The token threaded into the response-stream read.</param>
    public ServerStreamAssertion(AssertionContext<AsyncServerStreamingCall<TResponse>> context, CancellationToken cancellationToken)
        : base(context)
    {
        _cancellationToken = cancellationToken;
    }

    /// <summary>Asserts the stream produces at least <paramref name="count"/> responses.</summary>
    /// <param name="count">The minimum response count. Must be non-negative.</param>
    /// <returns>This assertion for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public ServerStreamAssertion<TResponse> StreamsAtLeast(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _atLeast = count;
        Context.ExpressionBuilder.Append(CultureInfo.InvariantCulture, $".StreamsAtLeast({count})");
        return this;
    }

    /// <summary>Asserts the stream produces exactly <paramref name="count"/> responses.</summary>
    /// <param name="count">The exact response count. Must be non-negative.</param>
    /// <returns>This assertion for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public ServerStreamAssertion<TResponse> StreamsExactly(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _exactly = count;
        Context.ExpressionBuilder.Append(CultureInfo.InvariantCulture, $".StreamsExactly({count})");
        return this;
    }

    /// <summary>Asserts at least one streamed response satisfies <paramref name="predicate"/>.</summary>
    /// <param name="predicate">The response predicate. Must not be null.</param>
    /// <param name="predicateExpression">The source text of <paramref name="predicate"/>, captured by
    /// the compiler for the failure message.</param>
    /// <returns>This assertion for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public ServerStreamAssertion<TResponse> StreamContains(
        Func<TResponse, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? predicateExpression = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _contains.Add((predicate, predicateExpression));
        Context.ExpressionBuilder.Append(string.Concat(".StreamContains(", predicateExpression, ")"));
        return this;
    }

    /// <summary>Runs <paramref name="assertions"/> against the materialized responses after the
    /// accumulated count and content expectations pass. The responses are already read, so the
    /// callback can assert on individual items without re-reading the stream.</summary>
    /// <param name="assertions">The follow-on assertions, applied to the response list. Must not be
    /// null.</param>
    /// <returns>This assertion for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assertions"/> is null.</exception>
    public ServerStreamAssertion<TResponse> AndStreamItems(Func<IReadOnlyList<TResponse>, Task> assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);
        _itemsCallback = assertions;
        Context.ExpressionBuilder.Append(".AndStreamItems(...)");
        return this;
    }

    /// <inheritdoc/>
    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<AsyncServerStreamingCall<TResponse>> metadata)
    {
        if (metadata.Exception is not null)
        {
            return AssertionResult.Failed(string.Concat(
                "it threw ", metadata.Exception.GetType().Name, ": ", metadata.Exception.Message));
        }

        var call = metadata.Value;
        if (call is null)
        {
            return AssertionResult.Failed("the server-streaming call was null");
        }

        var items = new List<TResponse>();
        try
        {
            while (await call.ResponseStream.MoveNext(_cancellationToken).ConfigureAwait(false))
            {
                items.Add(call.ResponseStream.Current);
            }
        }
        catch (RpcException ex)
        {
            return AssertionResult.Failed(string.Concat(
                "the stream faulted after ", items.Count.ToString(CultureInfo.InvariantCulture),
                " item(s) with ", GrpcOutcomeRendering.Describe(ex)));
        }

        var failure = Validate(items);
        if (failure is not null)
        {
            return failure.Value;
        }

        if (_itemsCallback is not null)
        {
            await _itemsCallback(items).ConfigureAwait(false);
        }

        return AssertionResult.Passed;
    }

    /// <inheritdoc/>
    protected override string GetExpectation()
    {
        var clauses = new List<string>();
        if (_atLeast is int min)
        {
            clauses.Add(string.Concat("at least ", min.ToString(CultureInfo.InvariantCulture), " response(s)"));
        }

        if (_exactly is int exact)
        {
            clauses.Add(string.Concat("exactly ", exact.ToString(CultureInfo.InvariantCulture), " response(s)"));
        }

        foreach (var (_, description) in _contains)
        {
            clauses.Add(string.Concat("a response matching ", description));
        }

        return clauses.Count is 0
            ? "the server-streaming call to produce responses"
            : string.Concat("the server-streaming call to produce ", string.Join(" and ", clauses));
    }

    /// <summary>Validates the materialized responses against the accumulated expectations.</summary>
    /// <param name="items">The responses read from the stream.</param>
    /// <returns>A failure result, or <see langword="null"/> when every expectation holds.</returns>
    private AssertionResult? Validate(List<TResponse> items)
    {
        if (_atLeast is int min && items.Count < min)
        {
            return AssertionResult.Failed(string.Concat(
                "the stream produced ", items.Count.ToString(CultureInfo.InvariantCulture),
                " response(s), fewer than the expected minimum of ", min.ToString(CultureInfo.InvariantCulture)));
        }

        if (_exactly is int exact && items.Count != exact)
        {
            return AssertionResult.Failed(string.Concat(
                "the stream produced ", items.Count.ToString(CultureInfo.InvariantCulture),
                " response(s), not the expected ", exact.ToString(CultureInfo.InvariantCulture)));
        }

        foreach (var (predicate, description) in _contains)
        {
            if (!items.Any(predicate))
            {
                return AssertionResult.Failed(string.Concat(
                    "no streamed response matched ", description, " (the stream produced ",
                    items.Count.ToString(CultureInfo.InvariantCulture), " response(s))"));
            }
        }

        return null;
    }
}
