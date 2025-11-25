using System.ComponentModel.DataAnnotations;

namespace SGE.Application.DTOs.Attendances;

/// <summary>
/// DTO for updating an existing attendance record.
/// </summary>
public class AttendanceUpdateDto
{
    /// <summary>
    /// The ID of the employee.
    /// </summary>
    [Required(ErrorMessage = "L'ID de l'employé est requis.")]
    public int EmployeeId { get; set; }

    /// <summary>
    /// The date of attendance.
    /// </summary>
    [Required(ErrorMessage = "La date est requise.")]
    public DateTime Date { get; set; }

    /// <summary>
    /// The clock-in time.
    /// </summary>
    public TimeSpan? ClockInTime { get; set; }

    /// <summary>
    /// The clock-out time.
    /// </summary>
    public TimeSpan? ClockOutTime { get; set; }

    /// <summary>
    /// Optional notes or comments about the attendance.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Les notes ne peuvent pas dépasser 500 caractères.")]
    public string? Notes { get; set; }
}