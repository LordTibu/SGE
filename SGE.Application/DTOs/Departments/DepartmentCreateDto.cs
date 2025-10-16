namespace SGE.Application.DTOs;

/// <summary>
/// Data Transfer Object (DTO) used for creating a new department.
/// Contains the necessary properties to create a department entity.
/// </summary>
public class DepartmentCreateDto
{
    /// <summary>
    /// Gets or sets the name of the department.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the code that uniquely identifies the department.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the department.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}