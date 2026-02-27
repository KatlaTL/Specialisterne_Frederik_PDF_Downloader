using System.Collections.Generic;
using PDF_Downloader.Models;

namespace PDF_Downloader.Services;

public interface IExcelLinkReader
{
    IReadOnlyList<LinkRow> ReadRows(string inputFilePath);
}
