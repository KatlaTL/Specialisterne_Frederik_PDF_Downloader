using System.Net;
using MiniExcelLibs;
using PDF_Downloader.Core;
using PDF_Downloader.Core.Models;
using PDF_Downloader.Core.Services;
using RichardSzalay.MockHttp;

namespace PDF_Downloader.Tests.Integration;

public class PdfFlowTests
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly IExcelLinkReader _linkReader;
    private readonly IPdfDownloadService _downloadService;
    private readonly DownloadRowProcessor _processor;
    private readonly PdfDownloaderApplication _application;

    public PdfFlowTests()
    {
        _mockHandler = new MockHttpMessageHandler();

        // mockHandler will use the first when() which matches
        // URL specific rules first
        _mockHandler.When(HttpMethod.Get, "http://notfound/*")
            .Respond(req => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        _mockHandler.When(HttpMethod.Get, "https://notfound/*")
            .Respond(req => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        // Generel OK rule
        _mockHandler.When(HttpMethod.Get, "*")
            .Respond(req => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) // fake PDF bytes
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf") }
                }
            }));

        _httpClient = _mockHandler.ToHttpClient();

        _linkReader = new ExcelLinkReader();
        _downloadService = new PdfDownloadService(_httpClient);
        _processor = new DownloadRowProcessor(_downloadService);

        _application = new PdfDownloaderApplication(_linkReader, _processor);
    }

    [Fact]
    public async Task RunAsync_Should_ReadExcel_DownloadLinks_SavePDFs_CreateResultsExcelFile()
    {
        var (tempInputPath, outputPath) = SetupTestFiles("TestData_20_Rows.xlsx");

        try
        {
            await _application.RunAsync(tempInputPath, outputPath);

            var resultFile = Directory.GetFiles(outputPath, "*_download_results.xlsx");
            var pdfFiles = Directory.GetFiles(outputPath, "*.pdf");

            Assert.NotEmpty(resultFile);
            Assert.NotEmpty(pdfFiles);
            Assert.All(pdfFiles, f => Assert.EndsWith(".pdf", f));
        }
        finally
        {
            File.Delete(tempInputPath);
            Directory.Delete(outputPath, true);
        }
    }

    [Fact]
    public async Task RunAsync_Should_HandleEmptyExcelWithHeader_NoDownload_CreateResultsExcelFile()
    {
        var (tempInputPath, outputPath) = SetupTestFiles("TestData_Empty_With_Header.xlsx");

        try
        {
            await _application.RunAsync(tempInputPath, outputPath);

            var resultFile = Directory.GetFiles(outputPath, "*_download_results.xlsx");
            var pdfFiles = Directory.GetFiles(outputPath, "*.pdf");

            Assert.NotEmpty(resultFile);
            Assert.Empty(pdfFiles);
        }
        finally
        {
            File.Delete(tempInputPath);
            Directory.Delete(outputPath, true);
        }
    }

    [Fact]
    public async Task RunAsync_Should_HandleEmptyExcel_NoDownload_CreateResultsExcelFile()
    {
        var (tempInputPath, outputPath) = SetupTestFiles("TestData_Empty.xlsx");

        try
        {
            await _application.RunAsync(tempInputPath, outputPath);

            var resultFile = Directory.GetFiles(outputPath, "*_download_results.xlsx");
            var pdfFiles = Directory.GetFiles(outputPath, "*.pdf");

            Assert.NotEmpty(resultFile);
            Assert.Empty(pdfFiles);
        }
        finally
        {
            File.Delete(tempInputPath);
            Directory.Delete(outputPath, true);
        }
    }

    [Fact]
    public async Task RunAsync_Should_HandleInvalidLinks_NoDownload_CreateResultsExcelFile()
    {
        var (tempInputPath, outputPath) = SetupTestFiles("TestData_Invalid_Links.xlsx");

        try
        {
            await _application.RunAsync(tempInputPath, outputPath);

            var resultFile = Directory.GetFiles(outputPath, "*_download_results.xlsx");
            var pdfFiles = Directory.GetFiles(outputPath, "*.pdf");

            Assert.NotEmpty(resultFile);
            Assert.Empty(pdfFiles);
        }
        finally
        {
            File.Delete(tempInputPath);
            Directory.Delete(outputPath, true);
        }
    }

    [Fact]
    public async Task RunAsync_Should_HandleInvalidLinks_DownloadAlternativeLink_SavePDFs_CreateResultsExcelFileWithDownloadResults()
    {
        var (tempInputPath, outputPath) = SetupTestFiles("TestData_Alternative_Links.xlsx");

        try
        {
            await _application.RunAsync(tempInputPath, outputPath);

            var resultFile = Directory.GetFiles(outputPath, "*_download_results.xlsx");
            var pdfFiles = Directory.GetFiles(outputPath, "*.pdf");

            Assert.NotEmpty(resultFile);

            var rows = MiniExcel.Query<DownloadResultRow>(resultFile.Single()).Skip(1).ToList();

            Assert.Collection(rows,
                r => Assert.Contains("Failed", r.DownloadStatus),
                r => Assert.Contains("Success", r.DownloadStatus),
                r => Assert.Contains("Success", r.DownloadStatus),
                r => Assert.Contains("Failed", r.DownloadStatus),
                r => Assert.Contains("Failed", r.DownloadStatus)
            );

            Assert.NotEmpty(pdfFiles);
            Assert.Equal(2, pdfFiles.Length);
            Assert.All(pdfFiles, f => Assert.EndsWith(".pdf", f));
        }
        finally
        {
            File.Delete(tempInputPath);
            Directory.Delete(outputPath, true);
        }
    }

    private (string tempInputPath, string outputPath) SetupTestFiles(string testFileName)
    {
        var projectDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
        var inputPath = Path.Combine(projectDir, "TestData", testFileName);
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(outputPath);

        var tempInputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xlsx");
        File.Copy(inputPath, tempInputPath, overwrite: true);

        return (tempInputPath, outputPath);
    }
}
