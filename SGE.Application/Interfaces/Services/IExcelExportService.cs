namespace SGE.Application.Interfaces.Services;

/// <summary>
/// Interface for Excel export operations.
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Exports employees data to an Excel file.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A byte array containing the Excel file data.</returns>
    Task<byte[]> ExportEmployeesToExcelAsync(CancellationToken cancellationToken = default);
}