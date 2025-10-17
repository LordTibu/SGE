using ClosedXML.Excel;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.Services;

namespace SGE.Application.Services;

/// <summary>
/// Service responsable de l'exportation des données vers des fichiers Excel en utilisant ClosedXML.
/// </summary>
public class ExcelExportService(IEmployeeRepository employeeRepository) : IExcelExportService
{
    /// <summary>
    /// Exporte tous les employés vers un fichier Excel.
    /// </summary>
    /// <param name="cancellationToken">Jeton pour surveiller les demandes d'annulation.</param>
    /// <returns>Un tableau d'octets contenant les données du fichier Excel.</returns>
    public async Task<byte[]> ExportEmployeesToExcelAsync(CancellationToken cancellationToken = default)
    {
        // Récupérer tous les employés
        var employees = await employeeRepository.GetAllAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Employés");

        // Définir les en-têtes avec style
        worksheet.Cell(1, 1).Value = "firstName";
        worksheet.Cell(1, 2).Value = "lastName";
        worksheet.Cell(1, 3).Value = "email";
        worksheet.Cell(1, 4).Value = "PhoneNumber";
        worksheet.Cell(1, 5).Value = "address";
        worksheet.Cell(1, 6).Value = "position";
        worksheet.Cell(1, 7).Value = "salary";
        worksheet.Cell(1, 8).Value = "hireDate";
        worksheet.Cell(1, 9).Value = "departmentId";
        worksheet.Cell(1, 10).Value = "Status";

        // Styliser les en-têtes
        var headerRange = worksheet.Range(1, 1, 1, 11);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Remplir les données
        int row = 2;
        foreach (var emp in employees)
        {
            worksheet.Cell(row, 1).Value = emp.FirstName;
            worksheet.Cell(row, 2).Value = emp.LastName;
            worksheet.Cell(row, 3).Value = emp.Email;
            worksheet.Cell(row, 4).Value = emp.PhoneNumber;
            worksheet.Cell(row, 5).Value = emp.Address;
            worksheet.Cell(row, 6).Value = emp.Position;
            worksheet.Cell(row, 7).Value = emp.Salary;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 €";
            worksheet.Cell(row, 8).Value = emp.HireDate;
            worksheet.Cell(row, 8).Style.DateFormat.Format = "dd/mm/yyyy";
            worksheet.Cell(row, 9).Value = emp.DepartmentId;
            worksheet.Cell(row, 10).Value = emp.Status;
            row++;
        }

        // Ajouter des bordures à toutes les cellules de données
        var dataRange = worksheet.Range(1, 1, row - 1, 10);
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Auto-ajuster les colonnes
        worksheet.Columns().AdjustToContents();

        // Retourner le fichier Excel en tableau d'octets
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}