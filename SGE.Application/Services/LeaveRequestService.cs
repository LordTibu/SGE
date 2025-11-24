using AutoMapper;
using SGE.Application.DTOs.LeaveRequests;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;
using SGE.Core.Enums;
using SGE.Core.Exceptions;

namespace SGE.Application.Services;

/// <summary>
/// Provides functionalities for managing employee leave requests.
/// </summary>
public class LeaveRequestService(
    IEmployeeRepository employeeRepository,
    ILeaveRequestRepository leaveRequestRepository,
    IMapper mapper)
    : ILeaveRequestService
{
    private const int AnnualLeaveDaysPerYear = 25; // Nombre de jours de congés annuels par défaut

    /// <summary>
    /// Creates a new leave request asynchronously.
    /// </summary>
    /// <param name="dto">
    /// The data transfer object containing the details of the leave request to be created.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The details of the newly created leave request, wrapped in a LeaveRequestDto.
    /// </returns>
    /// <exception cref="EmployeeNotFoundException">
    /// Thrown if the referenced employee does not exist.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown if the leave request has invalid dates, such as an end date earlier than the start date,
    /// a start date in the past, or conflicting with an existing approved leave request.
    /// </exception>
    public async Task<LeaveRequestDto> CreateAsync(LeaveRequestCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var employee = await employeeRepository.GetByIdAsync(dto.EmployeeId, cancellationToken);

        if (employee is null)
            throw new EmployeeNotFoundException(dto.EmployeeId);

        if (dto.EndDate < dto.StartDate)
            throw new ValidationException("EndDate", "La date de fin doit être supérieure à la date de début.");

        if (dto.StartDate < DateTime.Today)
            throw new ValidationException("StartDate",
                "La date de début doit être supérieure ou égale à la date de jour.");

        var daysRequested = CalculateBusinessDays(dto.StartDate, dto.EndDate);

        var hasConflict = await HasConflictingLeaveAsync(
            dto.EmployeeId,
            dto.StartDate,
            dto.EndDate,
            cancellationToken: cancellationToken);

        if (hasConflict)
            throw new ConflictingLeaveRequestException(dto.StartDate, dto.EndDate);

        var entity = mapper.Map<LeaveRequest>(dto);
        entity.DaysRequested = daysRequested;

        await leaveRequestRepository.AddAsync(entity, cancellationToken);

        return mapper.Map<LeaveRequestDto>(entity);
    }

    /// <summary>
    /// Retrieves the details of a leave request by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the leave request to be retrieved.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The details of the leave request or null if not found.</returns>
    public async Task<LeaveRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await leaveRequestRepository.GetByIdAsync(id, cancellationToken);
        return leaveRequest == null ? null : mapper.Map<LeaveRequestDto>(leaveRequest);
    }

    /// <summary>
    /// Retrieves the leave requests associated with a specific employee asynchronously.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A collection of leave request details.</returns>
    public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByEmployeeAsync(int employeeId,
        CancellationToken cancellationToken = default)
    {
        var leaveRequests = await leaveRequestRepository.GetByEmployeeAsync(employeeId, cancellationToken);
        return mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
    }

    /// <summary>
    /// Retrieves a collection of leave requests based on the specified status asynchronously.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A collection of leave requests matching the specified status.</returns>
    public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByStatusAsync(LeaveStatus status,
        CancellationToken cancellationToken = default)
    {
        var leaveRequests = await leaveRequestRepository.FindAsync(lr => lr.Status == status, cancellationToken);
        return mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
    }

    /// <summary>
    /// Retrieves all leave requests with a status of pending asynchronously.
    /// </summary>
    /// <returns>A collection of pending leave requests.</returns>
    public async Task<IEnumerable<LeaveRequestDto>> GetPendingLeaveRequestsAsync()
    {
        return await GetLeaveRequestsByStatusAsync(LeaveStatus.Pending);
    }

    /// <summary>
    /// Updates the status of an existing leave request asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the leave request to update.</param>
    /// <param name="dto">The updated status and manager comments.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    /// <exception cref="LeaveRequestNotFoundException">Thrown when the leave request is not found.</exception>
    /// <exception cref="InvalidLeaveStatusTransitionException">Thrown when trying to update a non-pending request.</exception>
    public async Task<bool> UpdateStatusAsync(int id, LeaveRequestUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = await leaveRequestRepository.GetByIdAsync(id, cancellationToken);

        if (leaveRequest == null)
            throw new LeaveRequestNotFoundException(id);

        // Seules les demandes en attente peuvent être modifiées
        if (leaveRequest.Status != LeaveStatus.Pending)
            throw new InvalidLeaveStatusTransitionException(leaveRequest.Status.ToString(), dto.Status.ToString());

        // Mettre à jour le statut et les commentaires
        leaveRequest.Status = dto.Status;
        leaveRequest.ManagerComments = dto.ManagerComments;
        leaveRequest.ReviewedAt = DateTime.UtcNow;
        leaveRequest.UpdatedAt = DateTime.UtcNow;
        leaveRequest.UpdatedBy = "Manager"; // TODO: Remplacer par l'utilisateur authentifié

        await leaveRequestRepository.UpdateAsync(leaveRequest, cancellationToken);

        return true;
    }

    /// <summary>
    /// Retrieves the remaining leave days for a specific employee in a given year asynchronously.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="year">The year for which to calculate remaining leave days.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The number of remaining leave days.</returns>
    /// <exception cref="EmployeeNotFoundException">Thrown when the employee is not found.</exception>
    public async Task<int> GetRemainingLeaveDaysAsync(int employeeId, int year,
        CancellationToken cancellationToken = default)
    {
        // Vérifier que l'employé existe
        if (!await employeeRepository.ExistsAsync(employeeId, cancellationToken))
            throw new EmployeeNotFoundException(employeeId);

        // Récupérer toutes les demandes de congés approuvées pour l'année
        var startOfYear = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var approvedLeaveRequests = await leaveRequestRepository.FindAsync(
            lr => lr.EmployeeId == employeeId
                  && lr.Status == LeaveStatus.Approved
                  && lr.LeaveType == LeaveType.Annual // Uniquement les congés annuels
                  && lr.StartDate.Year == year,
            cancellationToken);

        // Calculer le total des jours de congés pris
        var totalDaysTaken = approvedLeaveRequests.Sum(lr => lr.DaysRequested);

        // Retourner les jours restants
        return Math.Max(0, AnnualLeaveDaysPerYear - totalDaysTaken);
    }

    /// <summary>
    /// Checks if there are any conflicting leave requests for an employee within the specified date range.
    /// </summary>
    /// <param name="employeeId">The ID of the employee.</param>
    /// <param name="startDate">The start date of the leave period.</param>
    /// <param name="endDate">The end date of the leave period.</param>
    /// <param name="excludeRequestId">An optional leave request ID to exclude from the check.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if there's a conflict, false otherwise.</returns>
    public async Task<bool> HasConflictingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate,
        int? excludeRequestId = null, CancellationToken cancellationToken = default)
    {
        var existingLeaveRequests = await leaveRequestRepository.FindAsync(
            lr => lr.EmployeeId == employeeId
                  && (lr.Status == LeaveStatus.Pending || lr.Status == LeaveStatus.Approved)
                  && (excludeRequestId == null || lr.Id != excludeRequestId),
            cancellationToken);

        // Vérifier s'il y a un chevauchement de dates
        foreach (var leaveRequest in existingLeaveRequests)
        {
            // Chevauchement si :
            // - La nouvelle demande commence pendant une demande existante
            // - La nouvelle demande se termine pendant une demande existante
            // - La nouvelle demande englobe complètement une demande existante
            if ((startDate >= leaveRequest.StartDate && startDate <= leaveRequest.EndDate) ||
                (endDate >= leaveRequest.StartDate && endDate <= leaveRequest.EndDate) ||
                (startDate <= leaveRequest.StartDate && endDate >= leaveRequest.EndDate))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates the number of business days between two dates (excluding weekends).
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <returns>The number of business days.</returns>
    private int CalculateBusinessDays(DateTime startDate, DateTime endDate)
    {
        int businessDays = 0;
        DateTime current = startDate.Date;

        while (current <= endDate.Date)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                businessDays++;

            current = current.AddDays(1);
        }

        return businessDays;
    }
}