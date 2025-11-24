using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.LeaveRequests;
using SGE.Application.Interfaces.Services;
using SGE.Core.Enums;

namespace SGE.API.Controllers;

/// <summary>
/// Controller for managing employee leave requests.
/// Provides endpoints for creating, retrieving, and updating leave requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController(ILeaveRequestService leaveRequestService) : ControllerBase
{
    /// <summary>
    /// Creates a new leave request.
    /// </summary>
    /// <param name="createDto">The data transfer object containing leave request details.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns the created leave request details.</returns>
    [HttpPost]
    [ProducesResponseType(201, Type = typeof(LeaveRequestDto))]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<LeaveRequestDto>> CreateLeaveRequest(
        [FromBody] LeaveRequestCreateDto createDto, 
        CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest = await leaveRequestService.CreateAsync(createDto, cancellationToken);
            return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.Id }, leaveRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves a leave request by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the leave request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns the leave request details if found.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(200, Type = typeof(LeaveRequestDto))]
    [ProducesResponseType(404)]
    public async Task<ActionResult<LeaveRequestDto>> GetLeaveRequest(int id, CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest = await leaveRequestService.GetByIdAsync(id, cancellationToken);
            
            if (leaveRequest == null)
                return NotFound($"Leave request with ID {id} not found");

            return Ok(leaveRequest);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves all leave requests for a specific employee.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a collection of leave requests for the specified employee.</returns>
    [HttpGet("employee/{employeeId:int}")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<LeaveRequestDto>))]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequestsByEmployee(
        int employeeId, 
        CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequests = await leaveRequestService.GetLeaveRequestsByEmployeeAsync(employeeId, cancellationToken);
            return Ok(leaveRequests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves all leave requests with a specific status.
    /// </summary>
    /// <param name="status">The status to filter by (Pending, Approved, Rejected, Cancelled).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a collection of leave requests with the specified status.</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<LeaveRequestDto>))]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequestsByStatus(
        LeaveStatus status, 
        CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequests = await leaveRequestService.GetLeaveRequestsByStatusAsync(status, cancellationToken);
            return Ok(leaveRequests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves all pending leave requests.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a collection of pending leave requests.</returns>
    [HttpGet("pending")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<LeaveRequestDto>))]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetPendingLeaveRequests(CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequests = await leaveRequestService.GetPendingLeaveRequestsAsync();
            return Ok(leaveRequests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the status of a leave request (approve, reject, or cancel).
    /// </summary>
    /// <param name="id">The unique identifier of the leave request to update.</param>
    /// <param name="updateDto">The data transfer object containing the updated status and manager comments.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns no content if the update was successful.</returns>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateLeaveRequestStatus(
        int id, 
        [FromBody] LeaveRequestUpdateDto updateDto, 
        CancellationToken cancellationToken)
    {
        try
        {
            await leaveRequestService.UpdateStatusAsync(id, updateDto, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the remaining leave days for an employee in a specific year.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="year">The year for which to calculate remaining leave days.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns the number of remaining leave days.</returns>
    [HttpGet("employee/{employeeId:int}/remaining/{year:int}")]
    [ProducesResponseType(200, Type = typeof(int))]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> GetRemainingLeaveDays(
        int employeeId, 
        int year, 
        CancellationToken cancellationToken)
    {
        try
        {
            var remainingDays = await leaveRequestService.GetRemainingLeaveDaysAsync(employeeId, year, cancellationToken);
            return Ok(remainingDays);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if there are conflicting leave requests for an employee within a date range.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="startDate">The start date of the leave period.</param>
    /// <param name="endDate">The end date of the leave period.</param>
    /// <param name="excludeRequestId">An optional leave request ID to exclude from the check.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns true if there's a conflict, false otherwise.</returns>
    [HttpGet("employee/{employeeId:int}/conflicts")]
    [ProducesResponseType(200, Type = typeof(bool))]
    public async Task<ActionResult<bool>> CheckConflictingLeave(
        int employeeId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? excludeRequestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var hasConflict = await leaveRequestService.HasConflictingLeaveAsync(
                employeeId, 
                startDate, 
                endDate, 
                excludeRequestId, 
                cancellationToken);
            
            return Ok(hasConflict);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}

