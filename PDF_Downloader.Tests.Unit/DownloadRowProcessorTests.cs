using Moq;
using PDF_Downloader.Core.Models;
using PDF_Downloader.Core.Services;

namespace PDF_Downloader.Tests;

public class DownloadRowProcessorTests
{
    private readonly Mock<IPdfDownloadService> _mockDownloadService;
    public DownloadRowProcessorTests()
    {
        _mockDownloadService = new Mock<IPdfDownloadService>();
    }

    [Fact]
    public async Task ProcessAsync_MainLinkSucceeds_ReturnsTrue_AltLinkNotCalled()
    {
        var row = new LinkRow
        {
            IdName = "BR123",
            MainDownloadLink = "http://example.com/file.pdf",
            AlternativeDownloadLink = "http://example.com/alt.pdf",
        };

        _mockDownloadService
            .Setup(x => x.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        bool result = await processor.ProcessAsync(row, "out", CancellationToken.None);

        Assert.True(result);

        _mockDownloadService
            .Verify(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_MainLinkFails_ReturnsFalse_AltLinkSucceeds_ReturnsTrue()
    {
        var row = new LinkRow
        {
            IdName = "BR123",
            MainDownloadLink = "http://example.com/file.pdf",
            AlternativeDownloadLink = "http://example.com/alt.pdf",
        };

        _mockDownloadService
            .Setup(x => x.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockDownloadService
            .Setup(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        bool result = await processor.ProcessAsync(row, "out", CancellationToken.None);

        Assert.True(result);

        _mockDownloadService
            .Verify(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_MainLinkFails_ReturnsFalse_AltLinkIsEmptyAndIsNotCalled()
    {
        var row = new LinkRow
        {
            IdName = "BR123",
            MainDownloadLink = "http://example.com/file.pdf",
            AlternativeDownloadLink = "",
        };

        _mockDownloadService
            .Setup(x => x.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        bool result = await processor.ProcessAsync(row, "out", CancellationToken.None);

        Assert.False(result);

        _mockDownloadService
            .Verify(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_MainLinkFails_ReturnsFalse_AltLinkFails_ReturnsFails()
    {
        var row = new LinkRow
        {
            IdName = "BR123",
            MainDownloadLink = "http://example.com/file.pdf",
            AlternativeDownloadLink = "http://example.com/alt.pdf",
        };

        _mockDownloadService
            .Setup(x => x.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockDownloadService
            .Setup(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        bool result = await processor.ProcessAsync(row, "out", CancellationToken.None);

        Assert.False(result);

        _mockDownloadService
            .Verify(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_MissingIdName_ReturnsFails()
    {
        var row = new LinkRow
        {
            IdName = "",
            MainDownloadLink = "http://example.com/file.pdf",
            AlternativeDownloadLink = "http://example.com/alt.pdf",
        };

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        bool result = await processor.ProcessAsync(row, "out", CancellationToken.None);

        Assert.False(result);

        _mockDownloadService
             .Verify(x => x.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Never);
        _mockDownloadService
             .Verify(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_MainLinkMissing_AltLinkSucceeds_ReturnsTrue()
    {
        var row = new LinkRow
        {
            IdName = "BR123",
            MainDownloadLink = "",
            AlternativeDownloadLink = "http://example.com/alt.pdf",
        };

        _mockDownloadService
            .Setup(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        bool result = await processor.ProcessAsync(row, "out", CancellationToken.None);

        Assert.False(result);

        _mockDownloadService
            .Verify(x => x.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Never);
        _mockDownloadService
            .Verify(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_MainLinkMissing_AltLinkMissing_ReturnsTrue()
    {
        var row = new LinkRow
        {
            IdName = "BR123",
            MainDownloadLink = "",
            AlternativeDownloadLink = "",
        };

        var processor = new DownloadRowProcessor(_mockDownloadService.Object);

        bool result = await processor.ProcessAsync(row, "out", CancellationToken.None);

        Assert.False(result);

        _mockDownloadService
            .Verify(x => x.TryDownloadAsync(row.MainDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Never);
        _mockDownloadService
            .Verify(x => x.TryDownloadAsync(row.AlternativeDownloadLink, "out", row.IdName, It.IsAny<CancellationToken>()), Times.Never);
    }
}
