using System;
using Grpc.Core;

namespace GrpcAssertions;

/// <summary>
/// Framework-agnostic rendering of gRPC outcomes for assertion failure messages. Keeping the
/// rendering in the core package lets consumer-authored gRPC assertions (in any test framework)
/// produce diagnostics identical in shape to the <c>GrpcAssertions.TUnit</c> adapter.
/// </summary>
public static class GrpcOutcomeRendering
{
    /// <summary>
    /// The maximum number of UTF-16 code units of a status detail rendered before truncation. Detail
    /// strings longer than this are cut to this length and suffixed with a horizontal-ellipsis
    /// character (<c>U+2026</c>), matching the truncation convention used across the assertion family.
    /// A cut never splits a surrogate pair: when a pair straddles the limit, the whole pair is dropped and the
    /// rendered detail is one unit shorter.
    /// </summary>
    public const int MaxDetailLength = 200;

    /// <summary>
    /// Renders a gRPC <see cref="RpcException"/> as a single-line <c>RpcException with StatusCode
    /// X, Detail "..."</c> fragment for use in a failure message.
    /// </summary>
    /// <param name="exception">The exception to render. Must not be null.</param>
    /// <returns>The rendered fragment.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
    public static string Describe(RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return string.Concat("RpcException with ", DescribeStatus(exception.StatusCode, exception.Status.Detail));
    }

    /// <summary>
    /// Renders a status code and detail as a single-line <c>StatusCode X, Detail "..."</c>
    /// fragment, with the detail truncated to <see cref="MaxDetailLength"/> UTF-16 code units. A carriage return
    /// or line feed in the detail is rendered as the two-character escape (backslash and <c>r</c> or <c>n</c>), so
    /// the fragment is always one line.
    /// </summary>
    /// <param name="statusCode">The gRPC status code.</param>
    /// <param name="detail">The status detail. A null value is rendered as an empty string.</param>
    /// <returns>The rendered fragment.</returns>
    public static string DescribeStatus(StatusCode statusCode, string? detail)
        => string.Concat(
            "StatusCode ",
            statusCode.ToString(),
            ", Detail \"",
            Truncate(detail ?? string.Empty),
            "\"");

    // Cuts the detail to MaxDetailLength UTF-16 units on a code-point boundary, escapes its line breaks and
    // appends the ellipsis when it was cut.
    private static string Truncate(string value)
    {
        if (value.Length <= MaxDetailLength)
        {
            return EscapeLineBreaks(value);
        }

        // A cut between the two halves of a surrogate pair would leave a lone high surrogate.
        var length = char.IsHighSurrogate(value[MaxDetailLength - 1]) && char.IsLowSurrogate(value[MaxDetailLength])
            ? MaxDetailLength - 1
            : MaxDetailLength;
        return string.Concat(EscapeLineBreaks(value.AsSpan(0, length).ToString()), "…");
    }

    private static string EscapeLineBreaks(string value)
        => value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
