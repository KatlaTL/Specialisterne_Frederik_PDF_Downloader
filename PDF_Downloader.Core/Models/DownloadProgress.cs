namespace PDF_Downloader.Core.Models;

public sealed class DownloadProgress
{

    public int Completed { get; }

    public int Total { get; }


    public DownloadProgress(int completed, int total)
    {
        Completed = completed;
        Total = total;
    }
}
