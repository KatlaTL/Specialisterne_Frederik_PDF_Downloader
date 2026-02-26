using MiniExcelLibs;
using PDF_Downloader.Models;

namespace PDF_Downloader.Services;

public sealed class ExcelLinkReader : IExcelLinkReader
{
    public IReadOnlyList<LinkRow> ReadRows(string inputFilePath)
    {
        using var stream = File.OpenRead(inputFilePath);
        // Skip the first row because the sheet contains header data there.
        return stream.Query<LinkRow>().Skip(1).ToList();
    }
}
