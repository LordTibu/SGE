using AutoMapper;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using SGE.Application.DTOs;
using SGE.Application.DTOs.Employees;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;

namespace SGE.Application.Services;

public class EmployeeService(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository,
    IMapper mapper,
    ILogger<EmployeeService> logger) :
    IEmployeeService
{
    /// <summary>
    /// Asynchronously retrieves all employees from the repository and maps them to a collection of EmployeeDto.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of EmployeeDto objects.</returns>
    public async Task<IEnumerable<EmployeeDto>>
        GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await
            employeeRepository.GetAllAsync(cancellationToken);
        return mapper.Map<IEnumerable<EmployeeDto>>(list);
    }

    /// <summary>
    /// Asynchronously retrieves an employee by their unique identifier and maps it to an EmployeeDto.
    /// </summary>
    /// <param name="id">The unique identifier of the employee.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an EmployeeDto object if found; otherwise, null.
    /// </returns>
    public async Task<EmployeeDto?> GetByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        var emp = await employeeRepository.GetByIdAsync(id,
            cancellationToken);
        return emp == null ? null : mapper.Map<EmployeeDto>(emp);
    }

    /// <summary>
    /// Asynchronously retrieves an employee by their email address and maps it to an EmployeeDto.
    /// </summary>
    /// <param name="email">The email address of the employee to retrieve.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the EmployeeDto if found; otherwise, null.</returns>
    public async Task<EmployeeDto?> GetByEmailAsync(string email,
        CancellationToken cancellationToken = default)
    {
        var emp = await employeeRepository.GetByEmailAsync(email,
            cancellationToken);
        return emp == null ? null : mapper.Map<EmployeeDto>(emp);
    }

    /// <summary>
    /// Asynchronously retrieves employees belonging to a specific department and maps them to a collection of EmployeeDto.
    /// </summary>
    /// <param name="departmentId">The unique identifier of the department whose employees should be retrieved.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of EmployeeDto objects associated with the specified department.</returns>
    public async Task<IEnumerable<EmployeeDto>> GetByDepartmentAsync(int departmentId,
        CancellationToken cancellationToken = default)
    {
        var list = await employeeRepository.GetByDepartmentAsync(departmentId, cancellationToken);
        return mapper.Map<IEnumerable<EmployeeDto>>(list);
    }

    /// <summary>
    /// Asynchronously creates a new employee in the repository based on the provided data transfer object.
    /// </summary>
    /// <param name="dto">The data transfer object containing details of the employee to be created.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created EmployeeDto object.</returns>
    /// <exception cref="ApplicationException">Thrown if the specified department does not exist or if the email is already associated with another employee.</exception>
    public async Task<EmployeeDto> CreateAsync(EmployeeCreateDto dto, CancellationToken cancellationToken = default)
    {
        var department = await
            departmentRepository.GetByIdAsync(dto.DepartmentId, cancellationToken);
        if (department == null)
            throw new ApplicationException("Il n'existe aucun departement avec cet identifiant");
        var existingEmployee = await
            employeeRepository.GetByEmailAsync(dto.Email, cancellationToken);
        if (existingEmployee != null)
            throw new ApplicationException("Cet email existe déjà pour un autre employée");
        var entity = mapper.Map<Employee>(dto);
        await employeeRepository.AddAsync(entity, cancellationToken);
        return mapper.Map<EmployeeDto>(entity);
    }

    /// <summary>
    /// Asynchronously updates an employee's information in the repository using the provided data.
    /// </summary>
    /// <param name="id">The unique identifier of the employee to update.</param>
    /// <param name="dto">An object containing the updated details of the employee.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the update operation was successful.</returns>
    public async Task<bool> UpdateAsync(int id, EmployeeUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await employeeRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;

        mapper.Map(dto, entity);
        await employeeRepository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    /// <summary>
    /// Asynchronously deletes an employee by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the employee to be deleted.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the deletion was successful.</returns>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await employeeRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;

        await employeeRepository.DeleteAsync(entity.Id, cancellationToken);
        return true;
    }

    /// <summary>
    /// Imports employees from an Excel file stream asynchronously.
    /// </summary>
    /// <param name="fileStream">The Excel file stream to import from.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the import result with created count and any errors.</returns>
    public async Task<ImportResultDto> ImportFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting employees import from Excel");
            using var workbook = new XLWorkbook(fileStream);
            var ws = workbook.Worksheets.Worksheet(1);
            var result = new ImportResultDto();

            // Vérification des en-têtes
            var expectedHeaders = new[] { "FirstName", "LastName", "Email", "PhoneNumber", "Address", "Position", "Salary", "HireDate", "DepartmentId" };
            var actualHeaders = new string[9];
            for (int i = 1; i <= 9; i++)
            {
                actualHeaders[i - 1] = ws.Cell(1, i).GetString();
            }
            
            var missingHeaders = new List<string>();
            for (int i = 0; i < expectedHeaders.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(actualHeaders[i]) || !actualHeaders[i].Equals(expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                {
                    missingHeaders.Add($"Colonne {GetColumnLetter(i + 1)}: attendu '{expectedHeaders[i]}', reçu '{actualHeaders[i]}'");
                }
            }
            
            if (missingHeaders.Any())
            {
                return new ImportResultDto
                {
                    CreatedCount = 0,
                    Errors = new List<ImportErrorDto>
                    {
                        new() { RowNumber = 1, Message = $"En-têtes incorrects: {string.Join("; ", missingHeaders)}" }
                    }
                };
            }

            // Expect header row at row 1
            var firstDataRow = 2;
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            logger.LogInformation("Worksheet: {SheetName}, lastRow: {LastRow}", ws.Name, lastRow);

            for (var rowNum = firstDataRow; rowNum <= lastRow; rowNum++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning("Import cancelled at row {RowNum}", rowNum);
                    break;
                }

                try
                {
                    var row = ws.Row(rowNum);
                    
                    // Vérification des colonnes obligatoires avec messages clairs
                    var firstName = row.Cell(1).GetString();
                    var lastName = row.Cell(2).GetString();
                    var email = row.Cell(3).GetString();
                    
                    // Vérification des champs requis
                    var missingFields = new List<string>();
                    if (string.IsNullOrWhiteSpace(firstName)) missingFields.Add("FirstName (colonne A)");
                    if (string.IsNullOrWhiteSpace(lastName)) missingFields.Add("LastName (colonne B)");
                    if (string.IsNullOrWhiteSpace(email)) missingFields.Add("Email (colonne C)");
                    
                    if (missingFields.Any())
                    {
                        result.Errors.Add(new ImportErrorDto { 
                            RowNumber = rowNum, 
                            Message = $"Colonnes manquantes ou vides: {string.Join(", ", missingFields)}" 
                        });
                        continue;
                    }
                    
                    // Vérification de l'email dupliqué AVANT de traiter les autres champs
                    var exists = await employeeRepository.GetByEmailAsync(email, cancellationToken);
                    if (exists != null)
                    {
                        result.Errors.Add(new ImportErrorDto { 
                            RowNumber = rowNum, 
                            Message = $"L'employé avec l'email '{email}' existe déjà dans la base de données" 
                        });
                        continue;
                    }
                    
                    // Lecture des autres champs avec gestion d'erreurs
                    var phone = row.Cell(4).GetString();
                    var address = row.Cell(5).GetString();
                    var position = row.Cell(6).GetString();
                    
                    // Vérification du salaire
                    decimal salary;
                    try
                    {
                        salary = row.Cell(7).GetValue<decimal>();
                        if (salary < 0)
                        {
                            result.Errors.Add(new ImportErrorDto { 
                                RowNumber = rowNum, 
                                Message = "Le salaire (colonne G) doit être un nombre positif" 
                            });
                            continue;
                        }
                    }
                    catch
                    {
                        result.Errors.Add(new ImportErrorDto { 
                            RowNumber = rowNum, 
                            Message = "Le salaire (colonne G) doit être un nombre valide (ex: 50000)" 
                        });
                        continue;
                    }
                    
                    // Vérification de la date d'embauche
                    DateTime hireDate;
                    try
                    {
                        hireDate = DateTime.SpecifyKind(row.Cell(8).GetValue<DateTime>(), DateTimeKind.Utc);
                        if (hireDate > DateTime.UtcNow)
                        {
                            result.Errors.Add(new ImportErrorDto { 
                                RowNumber = rowNum, 
                                Message = "La date d'embauche (colonne H) ne peut pas être dans le futur" 
                            });
                            continue;
                        }
                    }
                    catch
                    {
                        result.Errors.Add(new ImportErrorDto { 
                            RowNumber = rowNum, 
                            Message = "La date d'embauche (colonne H) doit être une date valide (ex: 2023-01-15)" 
                        });
                        continue;
                    }
                    
                    // Vérification du DepartmentId
                    int departmentId;
                    try
                    {
                        departmentId = row.Cell(9).GetValue<int>();
                        if (departmentId <= 0)
                        {
                            result.Errors.Add(new ImportErrorDto { 
                                RowNumber = rowNum, 
                                Message = $"L'ID du département (colonne I) doit être un nombre positif (reçu: {departmentId})" 
                            });
                            continue;
                        }
                    }
                    catch
                    {
                        result.Errors.Add(new ImportErrorDto { 
                            RowNumber = rowNum, 
                            Message = "L'ID du département (colonne I) doit être un nombre entier valide (ex: 1, 2, 3...)" 
                        });
                        continue;
                    }
                    
                    // Vérification que le département existe
                    var department = await departmentRepository.GetByIdAsync(departmentId, cancellationToken);
                    if (department == null)
                    {
                        result.Errors.Add(new ImportErrorDto { 
                            RowNumber = rowNum, 
                            Message = $"Le département avec l'ID {departmentId} n'existe pas dans la base de données" 
                        });
                        continue;
                    }

                    // Création de l'entité
                    var entity = new Employee
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        PhoneNumber = phone,
                        Address = address,
                        Position = position,
                        Salary = salary,
                        HireDate = hireDate,
                        DepartmentId = departmentId,
                        CreatedBy = "System Import",
                        UpdatedBy = "System Import"
                    };

                    await employeeRepository.AddAsync(entity, cancellationToken);
                    result.CreatedCount++;
                    logger.LogInformation("Row {RowNum} imported: {Email}", rowNum, email);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error importing row {RowNum}", rowNum);
                    var errorMessage = ex.InnerException?.Message ?? ex.Message;
                    result.Errors.Add(new ImportErrorDto { RowNumber = rowNum, Message = $"Erreur: {errorMessage}" });
                    // continue with next row
                }
            }

            logger.LogInformation("Employees import completed. Created: {Count}", result.CreatedCount);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error during Excel import");
            return new ImportResultDto
            {
                CreatedCount = 0,
                Errors = new List<ImportErrorDto>
                {
                    new() { RowNumber = 0, Message = $"Erreur globale: {ex.Message}" }
                }
            };
        }
    }

    private static string GetColumnLetter(int columnNumber)
    {
        string columnLetter = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            columnLetter = (char)('A' + columnNumber % 26) + columnLetter;
            columnNumber /= 26;
        }
        return columnLetter;
    }
}