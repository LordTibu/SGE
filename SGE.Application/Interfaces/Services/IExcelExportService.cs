namespace SGE.Application.Interfaces.Services;

/// <summary>
/// Interface pour les opérations d'exportation Excel.
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Exporte les données des employés vers un fichier Excel.
    /// </summary>
    /// <param name="cancellationToken">Jeton pour surveiller les demandes d'annulation.</param>
    /// <returns>Un tableau d'octets contenant les données du fichier Excel.</returns>
    Task<byte[]> ExportEmployeesToExcelAsync(CancellationToken cancellationToken = default);
}