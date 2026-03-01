using CommunityToolkit.Maui.Storage;
using PDF_Downloader.Models;
using PDF_Downloader.Services;

namespace PDF_Downloader;

public partial class MainPage : ContentPage
{
    private static readonly HttpClient HttpClient = new();
    private readonly PdfDownloaderApplication _application;


    // Opsætter UI-standardværdier og sammensætter appens kerneafhængigheder.

    public MainPage()
    {
        InitializeComponent();

        IExcelLinkReader linkReader = new ExcelLinkReader();
        IPdfDownloadService downloadService = new PdfDownloadService(HttpClient);
        var rowProcessor = new DownloadRowProcessor(downloadService);
        _application = new PdfDownloaderApplication(linkReader, rowProcessor);

        InputFileEntry.Text = Path.Combine(AppContext.BaseDirectory, "demo.xlsx");
        OutputFolderEntry.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PDF_Downloader");
    }


    // Starter hele downloadforløbet fra UI'et.

    private async void OnRunDownloaderClicked(object? sender, EventArgs e)
    {
        string inputFilePath = InputFileEntry.Text?.Trim() ?? string.Empty;
        string outputFolderPath = OutputFolderEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(inputFilePath) || string.IsNullOrWhiteSpace(outputFolderPath))
        {
            StatusLabel.Text = "Please provide both input file path and output folder path.";
            return;
        }

        if (!File.Exists(inputFilePath))
        {
            StatusLabel.Text = $"Input file not found: {inputFilePath}";
            return;
        }

        try
        {
            // Lås UI'et, så brugeren ikke starter processen flere gange parallelt.
            SetBusyState(true);
            StatusLabel.Text = "Downloading files...";
            ProgressCounterLabel.Text = "Processed: 0/0";

            // Progress<T> marshaler opdateringer tilbage til UI-tråden.
            var progress = new Progress<DownloadProgress>(p =>
            {
                ProgressCounterLabel.Text = $"Processed: {p.Completed}/{p.Total}";
            });

            await _application.RunAsync(inputFilePath, outputFolderPath, progress);

            StatusLabel.Text = "Downloader finished.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Downloader failed: {ex.Message}";
        }
        finally
        {
            SetBusyState(false);
        }
    }


    // Slår knap og loading-indikator til/fra mens processen kører.

    private void SetBusyState(bool isBusy)
    {
        RunButton.IsEnabled = !isBusy;
        BusyIndicator.IsVisible = isBusy;
        BusyIndicator.IsRunning = isBusy;
    }


    // Åbner mappevælger og udfylder outputfeltet med valgt mappe.

    private async void OutputPicker(object? sender, EventArgs e)
    {
        var folder = await FolderPicker.PickAsync(default);
        if (folder.IsSuccessful && folder.Folder is not null)
        {
            OutputFolderEntry.Text = folder.Folder.Path;
        }
    }


    // Åbner filvælger begrænset til Excel-filer (.xlsx).

    private async void InputPicker(object? sender, EventArgs e)
    {
        var customFileType =
            new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/xlsx" } },
                { DevicePlatform.WinUI, new[] { ".xlsx" } },
            });

        var inputResult = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Vælg xlsx-fil",
            FileTypes = customFileType,
        });

        if (inputResult == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(inputResult.FullPath))
        {
            StatusLabel.Text = "Selected file has no accessible path.";
            return;
        }

        InputFileEntry.Text = inputResult.FullPath;
    }
}
