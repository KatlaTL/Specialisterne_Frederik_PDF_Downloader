using PDF_Downloader.Models;
using PDF_Downloader.Services;

namespace PDF_Downloader;

public sealed class PdfDownloaderApplication
{
    // Hard limit for simultaneous row processing/download attempts.
    private const int MaxConcurrentDownloads = 5;
    private readonly IExcelLinkReader _linkReader;
    private readonly DownloadRowProcessor _rowProcessor;

    public PdfDownloaderApplication(IExcelLinkReader linkReader, DownloadRowProcessor rowProcessor)
    {
        _linkReader = linkReader;
        _rowProcessor = rowProcessor;
    }

    public async Task RunAsync(string inputFilePath, string outputFolder, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputFilePath))
        {
            Console.WriteLine($"Input file was not found: {inputFilePath}");
            return;
        }

        // Read all candidate links from the spreadsheet and process with bounded parallelism.
        IReadOnlyList<LinkRow> rows = _linkReader.ReadRows(inputFilePath);
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrentDownloads,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(
            rows,
            parallelOptions,
            async (row, token) => { await _rowProcessor.ProcessAsync(row, outputFolder, token); });
    }
}

