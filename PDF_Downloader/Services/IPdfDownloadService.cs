namespace PDF_Downloader.Services;

public interface IPdfDownloadService
{
    Task<bool> TryDownloadAsync(string downloadUrl, string outputFolder, CancellationToken cancellationToken = default);
}
