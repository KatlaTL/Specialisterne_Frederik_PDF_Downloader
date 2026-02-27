using System.Threading;
using System.Threading.Tasks;

namespace PDF_Downloader.Services;

public interface IPdfDownloadService
{
    Task<bool> TryDownloadAsync(string downloadUrl, string outputFolder, String idName, CancellationToken cancellationToken = default);
}
