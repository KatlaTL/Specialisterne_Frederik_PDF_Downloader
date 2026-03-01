using System.Data;
using MiniExcelLibs;
using PDF_Downloader.Models;
using PDF_Downloader.Services;

namespace PDF_Downloader;

public sealed class PdfDownloaderApplication
{
    private const int MaxConcurrentDownloads = -1;
    private readonly IExcelLinkReader _linkReader;
    private readonly DownloadRowProcessor _rowProcessor;

    public PdfDownloaderApplication(IExcelLinkReader linkReader, DownloadRowProcessor rowProcessor)
    {
        _linkReader = linkReader;
        _rowProcessor = rowProcessor;
    }
    
    // Læser Excel-rækker, downloader PDF'er parallelt og opdaterer en resultatfil løbende.
    
    public async Task RunAsync(
        string inputFilePath,
        string outputFolder,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputFilePath))
        {
            Console.WriteLine($"Input file was not found: {inputFilePath}");
            return;
        }

        IReadOnlyList<LinkRow> rows = _linkReader.ReadRows(inputFilePath);

        // Opretter outputmappe og klargør sti til resultat-Excel.
        Directory.CreateDirectory(outputFolder);
        string resultsFilePath = Path.Combine(
            outputFolder,
            $"{Path.GetFileNameWithoutExtension(inputFilePath)}_download_results.xlsx");

        // Opret tom resultatfil med korrekte kolonner fra start.
        CreateInitialResultsWorkbook(resultsFilePath);
        progress?.Report(new DownloadProgress(0, rows.Count));

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrentDownloads,
            CancellationToken = cancellationToken
        };

        // resultRows/completedRowFlags gør det muligt at bevare original rækkeorden i output.
        var resultRows = new DownloadResultRow[rows.Count];
        var completedRowFlags = new bool[rows.Count];
        // Semaphore beskytter filskrivning, så kun én tråd skriver ad gangen.
        using var resultFileWriteLock = new SemaphoreSlim(1, 1);
        int completedCount = 0;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, rows.Count),
            parallelOptions,
            async (index, token) =>
            {
                LinkRow row = rows[index];
                bool wasDownloaded = await _rowProcessor.ProcessAsync(row, outputFolder, token);
                // Gem resultatet på samme indeks som inputrækken.
                resultRows[index] = new DownloadResultRow
                {
                    IDName = row.IdName ?? string.Empty,
                    DownloadStatus = wasDownloaded ? "Success" : "Failed"
                };
                completedRowFlags[index] = true;

                int completedRows = Interlocked.Increment(ref completedCount);

                await resultFileWriteLock.WaitAsync(token);
                try
                {
                    // Snapshot indeholder kun de rækker der er færdigbehandlet indtil nu.
                    var snapshot = new List<DownloadResultRow>(completedRows);
                    for (int i = 0; i < resultRows.Length; i++)
                    {
                        if (!completedRowFlags[i])
                        {
                            continue;
                        }

                        DownloadResultRow? completedRow = resultRows[i];
                        if (completedRow is not null)
                        {
                            snapshot.Add(completedRow);
                        }
                    }

                    if (File.Exists(resultsFilePath))
                    {
                        File.Delete(resultsFilePath);
                    }

                    // Skriv hele snapshot-filen igen, så den altid afspejler seneste fremdrift.
                    MiniExcel.SaveAs(resultsFilePath, snapshot);
                }
                finally
                {
                    resultFileWriteLock.Release();
                }

                progress?.Report(new DownloadProgress(completedRows, rows.Count));
            });
    }


    // Opretter en tom resultat-arbejdsbog med forventede kolonner.
 
    private static void CreateInitialResultsWorkbook(string resultsFilePath)
    {
        var initialTable = new DataTable();
        initialTable.Columns.Add(nameof(DownloadResultRow.IDName), typeof(string));
        initialTable.Columns.Add(nameof(DownloadResultRow.DownloadStatus), typeof(string));

        if (File.Exists(resultsFilePath))
        {
            File.Delete(resultsFilePath);
        }

        MiniExcel.SaveAs(resultsFilePath, initialTable);
    }
}
