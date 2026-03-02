using System.Net;
using Moq;
using Moq.Protected;
using PDF_Downloader.Core.Models;
using PDF_Downloader.Core.Services;

namespace PDF_Downloader.Tests;

public class PdfDownloadServiceTests
{

    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    public PdfDownloadServiceTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                // Different status based on request URI
                // Eg. http://example.com/notfound.pdf will return a status code 404
                if (request.RequestUri!.AbsoluteUri.Contains("notfound"))
                    return new HttpResponseMessage(HttpStatusCode.NotFound);

                return new HttpResponseMessage(HttpStatusCode.OK);
            })
            .Verifiable();


        _httpClient = new HttpClient(_mockHandler.Object);
    }


    [Fact]
    public async Task TryDownloadAsync_downloadUrlIsEmpty_ReturnsFalse()
    {
        var row = new LinkRow { IdName = "BR123", MainDownloadLink = "", };

        var pdfDownloadService = new PdfDownloadService(_httpClient);

        bool result = await pdfDownloadService.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, CancellationToken.None);

        Assert.False(result);

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task TryDownloadAsync_downloadUrlExtensionIsNotPDF_ReturnsFalse()
    {
        var row = new LinkRow { IdName = "BR123", MainDownloadLink = "http://example.com/file.xlsx", };

        var pdfDownloadService = new PdfDownloadService(_httpClient);

        bool result = await pdfDownloadService.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, CancellationToken.None);

        Assert.False(result);

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task TryDownloadAsync_HttpRequestStatusCodeNotOK_ReturnsFalse()
    {
        var row = new LinkRow { IdName = "BR123", MainDownloadLink = "http://example.com/notfound.pdf", };

        var pdfDownloadService = new PdfDownloadService(_httpClient);

        bool result = await pdfDownloadService.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, CancellationToken.None);

        Assert.False(result);

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task TryDownloadAsync_FileNameIsEmpty_ReturnsFalse()
    {
        var row = new LinkRow { IdName = "", MainDownloadLink = "http://example.com/file.pdf", };

        var pdfDownloadService = new PdfDownloadService(_httpClient);

        bool result = await pdfDownloadService.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, CancellationToken.None);

        Assert.False(result);

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

}