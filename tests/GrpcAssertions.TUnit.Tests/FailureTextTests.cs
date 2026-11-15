using System;
using System.Text;
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
    [Arguments("s => s == @\"first\\nsecond\nthird\"", "s => s == @\"first\\\\nsecond\\nthird\"")]
    [Arguments("s => s == \"\"\"\n    C:\\temp\n    \"\"\"", "s => s == \"\"\"\\n    C:\\\\temp\\n    \"\"\"")]
    [Arguments("s => s == \"\\t\" // a tab\n    || s.Ok", "s => s == \"\\\\t\" // a tab\\n    || s.Ok")]
    public async Task OneLineCode_LiteralOrCommentWithMeaningfulLineBreaks_EscapesThem(string code, string expected, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var oneLine = FailureText.OneLineCode(code);

        // A backslash is doubled before the line breaks are escaped, so a line break and a backslash followed by n
        // stay apart, and decoding the text gives the source back.
        await Assert.That(oneLine).IsEqualTo(expected);
        await Assert.That(Decode(oneLine)).IsEqualTo(code);
    }

    /// <summary>Without a literal or comment whose line breaks matter, the code is not escaped, so a backslash in it
    /// reads as written.</summary>
    [Test]
    public async Task OneLineCode_JoinedLines_KeepBackslashesAsWritten(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(FailureText.OneLineCode("s =>\n    s == \"a\\tb\"")).IsEqualTo("s => s == \"a\\tb\"");
    }

    /// <summary>Reverses the escaping: a backslash followed by <c>r</c> or <c>n</c> is a carriage return or a line
    /// feed, and a backslash followed by another backslash is one backslash.</summary>
    /// <param name="text">The escaped text.</param>
    /// <returns>The source text.</returns>
    private static string Decode(string text)
    {
        var source = new StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] is not '\\')
            {
                source.Append(text[i]);
                i++;
                continue;
            }

            source.Append(text[i + 1] switch
            {
                'r' => '\r',
                'n' => '\n',
                var other => other,
            });
            i += 2;
        }

        return source.ToString();
    }
}
