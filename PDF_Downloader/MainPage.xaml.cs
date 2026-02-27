using Windows.Storage.Pickers;
using PDF_Downloader.Services;
using CommunityToolkit.Maui.Storage;
using MiniExcelLibs;

namespace PDF_Downloader;

public partial class MainPage : ContentPage
{
    // Reused client prevents socket exhaustion during many downloads.
    private static readonly HttpClient HttpClient = new();
    private readonly PdfDownloaderApplication _application;

    public MainPage()
    {
        InitializeComponent();

        Console.WriteLine("The number of processors " +
                          "on this computer is {0}.",
            Environment.ProcessorCount);
        
        IExcelLinkReader linkReader = new ExcelLinkReader();
        IPdfDownloadService downloadService = new PdfDownloadService(HttpClient);
        var rowProcessor = new DownloadRowProcessor(downloadService);
        _application = new PdfDownloaderApplication(linkReader, rowProcessor);

        InputFileEntry.Text = Path.Combine(AppContext.BaseDirectory, "demo.xlsx");
        OutputFolderEntry.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PDF_Downloader");
    }

    private async void OnRunDownloaderClicked(object? sender, EventArgs e)
    {
        // Read and validate user input from the UI fields.
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
            SetBusyState(true);
            StatusLabel.Text = "Downloading files...";

            // Executes the full downloader workflow (read rows + concurrent downloads).
            await _application.RunAsync(inputFilePath, outputFolderPath);

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

    private void SetBusyState(bool isBusy)
    {
        // Prevent duplicate runs and show progress indicator while work is in flight.
        RunButton.IsEnabled = !isBusy;
        BusyIndicator.IsVisible = isBusy;
        BusyIndicator.IsRunning = isBusy;
    }

    private async void OutputPicker(object? sender, EventArgs e)
    {
            var folder = await CommunityToolkit.Maui.Storage.FolderPicker.PickAsync(default);
            OutputFolderEntry.Text = folder.Folder.Path;
    }

    private async void InputPicker( object? sender, EventArgs e)
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
            return;
        
        var inputText = inputResult.FullPath;
        Console.WriteLine(inputText);
        InputFileEntry.Text = inputText;
    }
}
