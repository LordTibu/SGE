using ClosedXML.Excel;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.Services;

namespace SGE.Application.Services;

/// <summary>
/// Service responsible for exporting data to Excel files using ClosedXML.
/// </summary>
public class ExcelExportService(IEmployeeRepository employeeRepository) : IExcelExportService
{
    /// <summary>
    /// Exports all employees to an Excel file.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A byte array containing the Excel file data.</returns>
    public async Task<byte[]> ExportEmployeesToExcelAsync(CancellationToken cancellationToken = default)
    {
        // Récupérer tous les employés
        var employees = await employeeRepository.GetAllAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Employés");

        // Définir les en-têtes avec style
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Prénom";
        worksheet.Cell(1, 3).Value = "Nom";
        worksheet.Cell(1, 4).Value = "Email";
        worksheet.Cell(1, 5).Value = "Téléphone";
        worksheet.Cell(1, 6).Value = "Adresse";
        worksheet.Cell(1, 7).Value = "Poste";
        worksheet.Cell(1, 8).Value = "Salaire";
        worksheet.Cell(1, 9).Value = "Date d'embauche";
        worksheet.Cell(1, 10).Value = "Département ID";
        worksheet.Cell(1, 11).Value = "Statut";

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
            worksheet.Cell(row, 1).Value = emp.Id;
            worksheet.Cell(row, 2).Value = emp.FirstName;
            worksheet.Cell(row, 3).Value = emp.LastName;
            worksheet.Cell(row, 4).Value = emp.Email;
            worksheet.Cell(row, 5).Value = emp.PhoneNumber;
            worksheet.Cell(row, 6).Value = emp.Address;
            worksheet.Cell(row, 7).Value = emp.Position;
            worksheet.Cell(row, 8).Value = emp.Salary;
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00 €";
            worksheet.Cell(row, 9).Value = emp.HireDate;
            worksheet.Cell(row, 9).Style.DateFormat.Format = "dd/mm/yyyy";
            worksheet.Cell(row, 10).Value = emp.DepartmentId;
            worksheet.Cell(row, 11).Value = emp.Status;
            row++;
        }

        // Ajouter des bordures à toutes les cellules de données
        var dataRange = worksheet.Range(1, 1, row - 1, 11);
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Auto-ajuster les colonnes
        worksheet.Columns().AdjustToContents();

        // Retourner le fichier Excel en byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}