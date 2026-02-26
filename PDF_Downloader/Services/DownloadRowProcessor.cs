using PDF_Downloader.Models;

namespace PDF_Downloader.Services;

public sealed class DownloadRowProcessor
{
    private readonly IPdfDownloadService _downloadService;

    public DownloadRowProcessor(IPdfDownloadService downloadService)
    {
        _downloadService = downloadService;
    }

    public async Task ProcessAsync(LinkRow row, string outputFolder, CancellationToken cancellationToken = default)
    {
        // Prefer the main link. If it fails, try the alternative link as fallback.
        if (!string.IsNullOrWhiteSpace(row.MainDownloadLink))
        {
            Console.WriteLine(row.MainDownloadLink);

            bool downloadedFromMain = await _downloadService.TryDownloadAsync(
                row.MainDownloadLink,
                outputFolder,
                cancellationToken);

            if (!downloadedFromMain && !string.IsNullOrWhiteSpace(row.AlternativeDownloadLink))
            {
                Console.WriteLine($"Main download failed. Trying alt link: {row.AlternativeDownloadLink}");
                await _downloadService.TryDownloadAsync(row.AlternativeDownloadLink, outputFolder, cancellationToken);
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(row.AlternativeDownloadLink))
        {
            await _downloadService.TryDownloadAsync(row.AlternativeDownloadLink, outputFolder, cancellationToken);
        }
    }
}
