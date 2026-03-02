namespace PDF_Downloader.Core.Services;


public sealed class PdfDownloadService : IPdfDownloadService
{
    private readonly HttpClient _httpClient;

    public PdfDownloadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    // Validerer URL, henter PDF'en og gemmer den på disk.

    public async Task<bool> TryDownloadAsync(
        string downloadUrl,
        string outputFolder,
        string idName,
        CancellationToken cancellationToken = default)
    {
        // Afviser tomme/ugyldige links samt lokale fil-URL'er.
        if (string.IsNullOrWhiteSpace(downloadUrl) ||
            !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? downloadLink) ||
            downloadLink.IsFile)
        {
            Console.WriteLine("Null or empty download link");
            return false;
        }

        // Hent kun PDF-filer.
        string extension = Path.GetExtension(downloadLink.AbsolutePath);
        if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Not PDF");
            return false;
        }

        // CHANGE 
        // Removed the check for IsNullOrWhiteSpace on filename as it was redundant because extension would always be .pdf
        // Check if idName is IsNullOrWhiteSpace instead 
        if (string.IsNullOrWhiteSpace(idName))
        {
            return false;
        }

        try
        {
            // Letvægtskald til statuskodekontrol før selve stream-download.
            using HttpResponseMessage response = await _httpClient.GetAsync(
                downloadLink,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
                return false;
            }

            // Filnavn baseres på BRnum.
            string fileName = idName + extension;

            Directory.CreateDirectory(outputFolder);
            string outputPath = Path.Combine(outputFolder, fileName);

            // Stream direkte til disk for at undgå unødigt memory-forbrug.
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
