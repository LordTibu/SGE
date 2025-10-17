﻿using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Attendances;
using SGE.Application.Interfaces.Services;

namespace SGE.API.Controllers
{
    /// <summary>
    /// Controller for managing employee attendance data.
    /// Provides endpoints for clocking in, clocking out, retrieving attendance records,
    /// and calculating attendance-related data.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AttendancesController : ControllerBase
    {
        private readonly IAttendanceService attendanceService;

        public AttendancesController(IAttendanceService attendanceService)
        {
            this.attendanceService = attendanceService;
        }

        #region ClockIn / ClockOut

        /// <summary>
        /// Records the clock-in time for an employee.
        /// </summary>
        [HttpPost("clock-in")]
        [ProducesResponseType(200, Type = typeof(AttendanceDto))]
        [ProducesResponseType(400)]
        public async Task<ActionResult<AttendanceDto>> ClockIn(
            [FromBody] ClockInOutDto clockInDto,
            CancellationToken cancellationToken)
        {
            try
            {
                var attendance = await attendanceService.ClockInAsync(clockInDto, cancellationToken);
                return Ok(attendance);
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
        /// Records the clock-out time for an employee.
        /// </summary>
        [HttpPost("clock-out")]
        [ProducesResponseType(200, Type = typeof(AttendanceDto))]
        [ProducesResponseType(400)]
        public async Task<ActionResult<AttendanceDto>> ClockOut(
            [FromBody] ClockInOutDto clockOutDto,
            CancellationToken cancellationToken)
        {
            try
            {
                var attendance = await attendanceService.ClockOutAsync(clockOutDto, cancellationToken);
                return Ok(attendance);
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

        #endregion

        #region Attendance CRUD

        /// <summary>
        /// Creates a new attendance record.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(201, Type = typeof(AttendanceDto))]
        [ProducesResponseType(400)]
        public async Task<ActionResult<AttendanceDto>> CreateAttendance(
            [FromBody] AttendanceCreateDto createAttendanceDto,
            CancellationToken cancellationToken)
        {
            try
            {
                var attendance = await attendanceService.CreateAttendanceAsync(createAttendanceDto, cancellationToken);
                return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
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
        /// Retrieves the attendance record for a specific ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(200, Type = typeof(AttendanceDto))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<AttendanceDto>> GetAttendance(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var attendance = await attendanceService.GetAttendanceByIdAsync(id, cancellationToken);
                if (attendance == null)
                    return NotFound($"Attendance record with ID {id} not found");

                return Ok(attendance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// Retrieves the attendance records of a specific employee within an optional date range.
        /// </summary>
        [HttpGet("employee/{employeeId:int}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<AttendanceDto>))]
        public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetEmployeeAttendances(
            int employeeId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var attendances = await attendanceService.GetAttendancesByEmployeeAsync(employeeId, startDate, endDate, cancellationToken);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a list of attendance records for a specific date.
        /// </summary>
        [HttpGet("date/{date:datetime}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<AttendanceDto>))]
        public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendancesByDate(
            DateTime date,
            CancellationToken cancellationToken)
        {
            try
            {
                var attendances = await attendanceService.GetAttendancesByDateAsync(date, cancellationToken);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves today's attendance record for a specific employee.
        /// </summary>
        [HttpGet("employee/{employeeId:int}/today")]
        [ProducesResponseType(200, Type = typeof(AttendanceDto))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<AttendanceDto>> GetTodayAttendance(
            int employeeId,
            CancellationToken cancellationToken)
        {
            try
            {
                var attendance = await attendanceService.GetTodayAttendanceAsync(employeeId, cancellationToken);
                if (attendance == null)
                    return NotFound("No attendance record found for today");

                return Ok(attendance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the total number of hours worked by an employee for a specific month and year.
        /// </summary>
        [HttpGet("employee/{employeeId:int}/hours/{year:int}/{month:int}")]
        [ProducesResponseType(200, Type = typeof(decimal))]
        public async Task<ActionResult<decimal>> GetMonthlyHours(
            int employeeId,
            int year,
            int month,
            CancellationToken cancellationToken)
        {
            try
            {
                var totalHours = await attendanceService.GetMonthlyWorkedHoursAsync(employeeId, year, month, cancellationToken);
                return Ok(totalHours);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion
    }
}
