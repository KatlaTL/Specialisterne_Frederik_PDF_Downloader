using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;


var inputFile = "demo.xlsx";
var outputFolder = "";

var httpClient = new HttpClient();


using (var stream = File.OpenRead(inputFile))
{
    var rows = stream.Query<Demo>().ToList();
    var rowNumber = rows.Count;
    
    for (int i = 1; i < rowNumber; i++)
    {
        Console.WriteLine(rows[i].A);
        string mainDownloadLink = rows[i].A;
        string altDownloadLink = rows[i].B;

        if (!string.IsNullOrWhiteSpace(mainDownloadLink))
        {
            var downloadedFromMain = await TryDownloadPdfAsync(httpClient, mainDownloadLink);

            if (!downloadedFromMain && !string.IsNullOrWhiteSpace(altDownloadLink))
            {
                Console.WriteLine($"Main download failed. Trying alt link: {altDownloadLink}");
                await TryDownloadPdfAsync(httpClient, altDownloadLink);
            }
        }
        else if (!string.IsNullOrWhiteSpace(altDownloadLink))
        {
            await TryDownloadPdfAsync(httpClient, altDownloadLink);
        }
    }
}

static async Task<bool> TryDownloadPdfAsync(HttpClient httpClient, string downloadUrl)
{
    if (string.IsNullOrWhiteSpace(downloadUrl) ||
        !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? downloadLink) ||
        downloadLink.IsFile)
    {
        return false;
    }

    string extension = Path.GetExtension(downloadLink.AbsolutePath);
    if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    try
    {
        using HttpResponseMessage response = await httpClient.GetAsync(downloadLink);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        string fileName = Path.GetFileName(downloadLink.LocalPath);

        await using var downloadStream = await httpClient.GetStreamAsync(downloadLink);
        await using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        await downloadStream.CopyToAsync(fileStream);
        await fileStream.FlushAsync();
        return true;
    }
    catch
    {
        return false;
    }
}



public class Demo
{
    [ExcelColumnIndex("AL")]
    public string A { get; set; }
    [ExcelColumnIndex("AM")]
    public string B { get; set; }
}
