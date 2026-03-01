using MiniExcelLibs.Attributes;

namespace PDF_Downloader.Models;

public sealed class LinkRow
{
    // Primær PDF-URL (Excel-kolonne AL).
    [ExcelColumnIndex("AL")]
    public string? MainDownloadLink { get; set; }

    // Alternativ PDF-URL (Excel-kolonne AM).
    [ExcelColumnIndex("AM")]
    public string? AlternativeDownloadLink { get; set; }
    
    // BRnum fra xlsx-fil (Excel-kolonne A).
    [ExcelColumnIndex("A")]
    public string? IdName { get; set; }
}
