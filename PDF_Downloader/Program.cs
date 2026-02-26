using PDF_Downloader.Services;

namespace PDF_Downloader;

public static class DownloaderRunner
{
    // Console-friendly entry utility that wires dependencies and runs the workflow.
    public static async Task RunAsync(string inputFile, string outputFolder, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();

        IExcelLinkReader linkReader = new ExcelLinkReader();
        IPdfDownloadService downloadService = new PdfDownloadService(httpClient);
        var rowProcessor = new DownloadRowProcessor(downloadService);
        var application = new PdfDownloaderApplication(linkReader, rowProcessor);

        await application.RunAsync(inputFile, outputFolder, cancellationToken);
    }
}


/*
    1.
    UI starts in MainPage.xaml with input path, output path, and Run Downloader.
    
    2.
    Click handler in MainPage.xaml.cs validates paths, sets busy state, then calls _application.RunAsync(...).
    
    3.
    PdfDownloaderApplication.cs reads rows from Excel and processes them with max 5 concurrent workers.
    
    4.
    ExcelLinkReader.cs maps Excel rows to LinkRow objects and skips the header row.
    
    5.
    LinkRow.cs maps Excel AL to main link and AM to fallback link.
    
    6.
    DownloadRowProcessor.cs tries main link first, then fallback link if main fails.
    
    7.
    PdfDownloadService.cs validates URL and .pdf, downloads with HttpClient, and writes file to output folder.
*/