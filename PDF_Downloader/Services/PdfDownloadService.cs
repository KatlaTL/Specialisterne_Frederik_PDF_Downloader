namespace PDF_Downloader.Services;


public sealed class PdfDownloadService : IPdfDownloadService
{
    private readonly HttpClient _httpClient;

    public PdfDownloadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> TryDownloadAsync(
        string downloadUrl,
        string outputFolder,
        string idName,
        CancellationToken cancellationToken = default)
    {
        // Reject empty/invalid/local paths before any network call.
        if (string.IsNullOrWhiteSpace(downloadUrl) ||
            !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? downloadLink) ||
            downloadLink.IsFile)
        {
            Console.WriteLine("Null or empty download link");
            return false;
        }

        // Downloader only accepts URLs that end with ".pdf".
        string extension = Path.GetExtension(downloadLink.AbsolutePath);
        if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Not PDF");
            return false;
        }

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                downloadLink,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
                return false;
            }

            string fileName = idName + extension;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            // Ensure target folder exists, then stream directly to disk.
            Directory.CreateDirectory(outputFolder);
            string outputPath = Path.Combine(outputFolder, fileName);

            await using Stream downloadStream = await _httpClient.GetStreamAsync(downloadLink, cancellationToken);
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await downloadStream.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
