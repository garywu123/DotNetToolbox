using System.Text;

namespace DotNetToolbox.Data.Csv;

/// <summary>
/// Options for <see cref="CsvWriter"/>.
/// </summary>
public sealed class CsvWriterOptions
{
    /// <summary>Format applied to <see cref="DateTime"/> and <see cref="DateTimeOffset"/> values.</summary>
    /// <remarks>Default: <c>yyyy-MM-dd HH:mm:ss.fffffff</c></remarks>
    public string DateTimeFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fffffff";

    /// <summary>Encoding for the output file.</summary>
    /// <remarks>Default: UTF-8 with BOM.</remarks>
    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>Line terminator.</summary>
    /// <remarks>Default: <c>\r\n</c> (Windows / RFC 4180).</remarks>
    public string NewLine { get; init; } = "\r\n";
}

