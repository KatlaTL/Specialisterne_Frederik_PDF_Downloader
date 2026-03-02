using System.Collections.Generic;
using PDF_Downloader.Core.Models;

namespace PDF_Downloader.Core.Services;

public interface IExcelLinkReader
{
    IReadOnlyList<LinkRow> ReadRows(string inputFilePath);
}
