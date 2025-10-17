namespace SGE.Application.DTOs;

/// <summary>
/// Data Transfer Object (DTO) used for updating an existing department.
/// Contains the editable properties of a department entity.
/// </summary>
public class DepartmentUpdateDto
{
    /// <summary>
    /// Gets or sets the name of the department.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the department.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}