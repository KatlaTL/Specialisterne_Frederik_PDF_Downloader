using Moq;
using PDF_Downloader.Core;
using PDF_Downloader.Core.Models;
using PDF_Downloader.Core.Services;

namespace PDF_Downloader.Tests;

public class PdfDownloaderApplicationTests
{
    private readonly Mock<IExcelLinkReader> _mockLinkReader;
    private readonly Mock<IPdfDownloadService> _mockDownloadService;
    public PdfDownloaderApplicationTests()
    {
        _mockLinkReader = new Mock<IExcelLinkReader>();
        _mockDownloadService = new Mock<IPdfDownloadService>();
    }

    [Fact]
    public async Task RunAsync_FileDoesNotExists_ReturnEarly()
    {
        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        var pdfDownloaderApplication = new PdfDownloaderApplication(_mockLinkReader.Object, processor);

        await pdfDownloaderApplication.RunAsync("doesNotExists.xlsx", "out");

        _mockLinkReader
            .Verify(x => x.ReadRows(It.IsAny<string>()), Times.Never);

        _mockDownloadService
            .Verify(x => x.TryDownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_EmptyExcel_ProgressIsEqualZero()
    {
        _mockLinkReader
            .Setup(x => x.ReadRows(It.IsAny<string>()))
            .Returns(new List<LinkRow>());

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        var tempFile = Path.GetTempFileName();

        try
        {
            var pdfDownloaderApplication = new PdfDownloaderApplication(_mockLinkReader.Object, processor);

            DownloadProgress? reportedProgress = null;

            await pdfDownloaderApplication.RunAsync(tempFile, "out", new Progress<DownloadProgress>(p => reportedProgress = p));

            Assert.NotNull(reportedProgress);
            Assert.Equal(0, reportedProgress!.Completed);
            Assert.Equal(0, reportedProgress!.Total);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RunAsync_NormalExcel_ProgressIsEqualRows()
    {
        List<LinkRow> mockList = new()
        {
            new LinkRow
        {
            IdName = "BR123",
            MainDownloadLink = "http://example.com/file.pdf",
            AlternativeDownloadLink = "http://example.com/alt.pdf",
        },
        new LinkRow
        {
            IdName = "BR123",
            MainDownloadLink = "http://example.com/file.pdf",
            AlternativeDownloadLink = "http://example.com/alt.pdf",
        },
        new LinkRow
        {
            IdName = "BR123",
            MainDownloadLink = "http://example.com/file.pdf",
            AlternativeDownloadLink = "http://example.com/alt.pdf",
        }
        };

        _mockLinkReader
            .Setup(x => x.ReadRows(It.IsAny<string>()))
            .Returns(mockList);

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        var tempFile = Path.GetTempFileName();

        try
        {
            var pdfDownloaderApplication = new PdfDownloaderApplication(_mockLinkReader.Object, processor);

            DownloadProgress? reportedProgress = null;

            await pdfDownloaderApplication
                .RunAsync(tempFile, "out", new Progress<DownloadProgress>(p => Interlocked.Exchange(ref reportedProgress, p)));

            Assert.NotNull(reportedProgress);
            Assert.Equal(3, reportedProgress!.Completed);
            Assert.Equal(3, reportedProgress!.Total);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RunAsync_ProcessCancelled()
    {
        List<LinkRow> mockList = new()
        {
            new LinkRow { IdName = "BR123", MainDownloadLink = "http://example.com/file.pdf"},
            new LinkRow { IdName = "BR123", MainDownloadLink = "http://example.com/file.pdf"},
            new LinkRow { IdName = "BR123", MainDownloadLink = "http://example.com/file.pdf"},
            new LinkRow { IdName = "BR123", MainDownloadLink = "http://example.com/file.pdf"},
            new LinkRow { IdName = "BR123", MainDownloadLink = "http://example.com/file.pdf"},
            new LinkRow { IdName = "BR123", MainDownloadLink = "http://example.com/file.pdf"},
        };

        _mockLinkReader
            .Setup(x => x.ReadRows(It.IsAny<string>()))
            .Returns(mockList);

        _mockDownloadService
            .Setup(x => x.TryDownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string url, string folder, string idName, CancellationToken ct) =>
            {
                await Task.Delay(1000, ct);
                return true;
            });

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        var tempFile = Path.GetTempFileName();

        try
        {
            var pdfDownloaderApplication = new PdfDownloaderApplication(_mockLinkReader.Object, processor);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(50);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await pdfDownloaderApplication.RunAsync(tempFile, "out", null, cts.Token);
            });
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}