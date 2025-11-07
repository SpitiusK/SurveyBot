namespace SurveyBot.Core.DTOs.Common;

/// <summary>
/// Enumeration for export file formats.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Comma-separated values format.
    /// </summary>
    Csv = 0,

    /// <summary>
    /// Microsoft Excel format.
    /// </summary>
    Excel = 1,

    /// <summary>
    /// JavaScript Object Notation format.
    /// </summary>
    Json = 2
}
