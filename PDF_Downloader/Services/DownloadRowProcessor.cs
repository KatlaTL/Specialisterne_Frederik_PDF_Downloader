using PDF_Downloader.Models;

namespace PDF_Downloader.Services;

public sealed class DownloadRowProcessor
{
    private readonly IPdfDownloadService _downloadService;

    public DownloadRowProcessor(IPdfDownloadService downloadService)
    {
        _downloadService = downloadService;
    }


    // Behandler én Excel-række og forsøger download via primært link, derefter alternativt link.

    public async Task<bool> ProcessAsync(LinkRow row, string outputFolder, CancellationToken cancellationToken = default)
    {
        // Uden ID kan filen ikke navngives sikkert i output.
        if (string.IsNullOrWhiteSpace(row.IdName))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(row.MainDownloadLink))
        {
            Console.WriteLine(row.IdName);
            Console.WriteLine(row.MainDownloadLink);

            // Første forsøg sker altid på hovedlinket.
            bool downloadedFromMain = await _downloadService.TryDownloadAsync(
                row.MainDownloadLink,
                outputFolder,
                row.IdName,
                cancellationToken);

            // Fallback: prøv alternativt link hvis hovedlink fejler.
            if (!downloadedFromMain && !string.IsNullOrWhiteSpace(row.AlternativeDownloadLink))
            {
                Console.WriteLine($"Main download failed. Trying alt link: {row.AlternativeDownloadLink}");
                
                return await _downloadService.TryDownloadAsync(
                    row.AlternativeDownloadLink, 
                    outputFolder, 
                    row.IdName,
                    cancellationToken);
            }

            return downloadedFromMain;
        }

        // Hvis hovedlink mangler, bruges alternativt link direkte.
        if (!string.IsNullOrWhiteSpace(row.AlternativeDownloadLink))
        {
            return await _downloadService.TryDownloadAsync(
                row.AlternativeDownloadLink,
                outputFolder,
                row.IdName,
                cancellationToken);
        }

        return false;
    }
}
