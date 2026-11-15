using System;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcAssertions.TUnit.Tests;

/// <summary>
/// Pins how <see cref="FailureText.OneLineCode"/> puts a predicate's captured source text on one line: between
/// tokens a line break becomes one space, and when the text holds a literal or comment whose line breaks can matter,
/// the line breaks are escaped so the text shows it as written.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class FailureTextTests
{
    [Test]
    [Arguments("s => s.Ok", "s => s.Ok")]
    [Arguments("s =>\n    s.Ok", "s => s.Ok")]
    [Arguments("s =>\r\n    string.Equals(s, \"z\")", "s => string.Equals(s, \"z\")")]
    [Arguments("s => s == '#' ||\n    s.Ok", "s => s == '#' ||\\n    s.Ok")]
    public async Task OneLineCode_JoinsLinesOrEscapesConservatively(string code, string expected, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(FailureText.OneLineCode(code)).IsEqualTo(expected);
    }

    /// <summary>A verbatim string (also an interpolated one) or a raw string literal can hold a line break, a line
    /// comment and a directive end at one, and a block comment can span one: joining the lines would show a different
    /// literal, or comment out the code after a line comment.</summary>
    [Test]
    [Arguments("s => s == \"\"\"\n    first\n    \"\"\"", "s => s == \"\"\"\\n    first\\n    \"\"\"")]
    [Arguments("s => s == @\"a\r\nb\"", "s => s == @\"a\\r\\nb\"")]
    [Arguments("s => s == $@\"{s}\nb\"", "s => s == $@\"{s}\\nb\"")]
    [Arguments("s => s == @$\"{s}\nb\"", "s => s == @$\"{s}\\nb\"")]
    [Arguments("s => // the last one\n    s.Ok", "s => // the last one\\n    s.Ok")]
    [Arguments("s => /* a\n b */ s.Ok", "s => /* a\\n b */ s.Ok")]
    [Arguments("s =>\n#if DEBUG\n    s.Ok\n#endif", "s =>\\n#if DEBUG\\n    s.Ok\\n#endif")]
    public async Task OneLineCode_LiteralOrCommentWithMeaningfulLineBreaks_EscapesThem(string code, string expected, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var oneLine = FailureText.OneLineCode(code);

        // Nothing is dropped or joined: undoing the two escapes gives the source text back.
        await Assert.That(oneLine).IsEqualTo(expected);
        await Assert.That(oneLine.Replace("\\r", "\r", StringComparison.Ordinal).Replace("\\n", "\n", StringComparison.Ordinal))
            .IsEqualTo(code);
    }
}
