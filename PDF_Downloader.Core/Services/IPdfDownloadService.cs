using System.Threading;
using System.Threading.Tasks;

namespace PDF_Downloader.Core.Services;

public interface IPdfDownloadService
{
    Task<bool> TryDownloadAsync(string downloadUrl, string outputFolder, String idName, CancellationToken cancellationToken = default);
}
