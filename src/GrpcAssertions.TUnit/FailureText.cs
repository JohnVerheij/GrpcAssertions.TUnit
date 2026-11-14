using System;

namespace GrpcAssertions.TUnit;

/// <summary>
/// Shared helpers for the failure-message text of the assertions in this package. Every user-influenced
/// value (an exception message, a trailer key or value) goes through <see cref="EscapeLineBreaks"/>, so each
/// clause of a failure message stays on one line.
/// </summary>
internal static class FailureText
{
    /// <summary>The line-break characters that <see cref="OneLineCode"/> splits on.</summary>
    private static readonly char[] LineBreaks = ['\r', '\n'];

    /// <summary>Renders source code captured by <c>CallerArgumentExpression</c> (a predicate's text) on one line, as
    /// the code reads: each line break, with the indentation around it, becomes one space. Line breaks in code carry
    /// no meaning, so escaping them would only add noise.</summary>
    /// <param name="code">The captured source text.</param>
    /// <returns>The code on one line.</returns>
    internal static string OneLineCode(string code)
        => string.Join(' ', code.Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    /// <summary>Renders a carriage return as a backslash and <c>r</c>, and a line feed as a backslash and
    /// <c>n</c>, so the value occupies one line.</summary>
    /// <param name="value">The text to escape.</param>
    /// <returns>The text with its line breaks escaped.</returns>
    internal static string EscapeLineBreaks(string value)
        => value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

    /// <summary>Renders an exception that is not a gRPC <see cref="Grpc.Core.RpcException"/> as
    /// <c>it threw TypeName: message</c>, on one line.</summary>
    /// <param name="exception">The exception to render.</param>
    /// <returns>The rendered clause.</returns>
    internal static string DescribeThrown(Exception exception)
        => string.Concat("it threw ", exception.GetType().Name, ": ", EscapeLineBreaks(exception.Message));
}
