using MiniExcelLibs.Attributes;

namespace PDF_Downloader.Models;

public sealed class LinkRow
{
    // Primary PDF URL (Excel column AL).
    [ExcelColumnIndex("AL")]
    public string? MainDownloadLink { get; set; }

    // Backup PDF URL (Excel column AM).
    [ExcelColumnIndex("AM")]
    public string? AlternativeDownloadLink { get; set; }
}
