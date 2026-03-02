using MiniExcelLibs;
using PDF_Downloader.Core.Models;

namespace PDF_Downloader.Core.Services;

public sealed class ExcelLinkReader : IExcelLinkReader
{

    // Læser linkrækker fra Excel-filen og springer overskriftsrækken over.

    public IReadOnlyList<LinkRow> ReadRows(string inputFilePath)
    {
        using var stream = File.OpenRead(inputFilePath);
        return stream.Query<LinkRow>().Skip(1).ToList();
    }
}
