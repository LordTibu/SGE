using AutoMapper;
using SGE.Application.DTOs.Attendances;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;

namespace SGE.Application.Services
{
    /// <summary>
    /// Provides functionalities related to managing employee attendances,
    /// including clocking in/out, retrieving attendance records, and calculating worked hours for employees.
    /// </summary>
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository attendanceRepository;
        private readonly IEmployeeRepository employeeRepository;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendanceService"/> class.
        /// </summary>
        public AttendanceService(
            IAttendanceRepository attendanceRepository,
            IEmployeeRepository employeeRepository,
            IMapper mapper)
        {
            this.attendanceRepository = attendanceRepository;
            this.employeeRepository = employeeRepository;
            this.mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task<AttendanceDto> ClockInAsync(
            ClockInOutDto clockInDto,
            CancellationToken cancellationToken = default)
        {
            if (!await employeeRepository.ExistsAsync(clockInDto.EmployeeId, cancellationToken))
                throw new KeyNotFoundException($"Employee with ID {clockInDto.EmployeeId} not found");

            var date = clockInDto.DateTime.Date;
            var time = clockInDto.DateTime.TimeOfDay;

            // TODO:
            // - Check if an attendance record already exists for the date
            // - Create a new attendance record
            // - Update existing record if applicable
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<AttendanceDto> ClockOutAsync(
            ClockInOutDto clockOutDto,
            CancellationToken cancellationToken = default)
        {
            // Verify that the employee exists
            if (!await employeeRepository.ExistsAsync(clockOutDto.EmployeeId, cancellationToken))
                throw new KeyNotFoundException($"Employee with ID {clockOutDto.EmployeeId} not found");

            var date = clockOutDto.DateTime.Date;
            var time = clockOutDto.DateTime.TimeOfDay;

            // TODO: Fetch existing attendance record for the date
            var attendance = new Attendance();

            if (attendance == null)
                throw new InvalidOperationException("No clock-in record found for today");

            if (!attendance.ClockIn.HasValue)
                throw new InvalidOperationException("Employee must clock in before clocking out");

            if (attendance.ClockOut.HasValue)
                throw new InvalidOperationException("Employee has already clocked out today");

            attendance.ClockOut = time;
            attendance.Notes += string.IsNullOrEmpty(attendance.Notes)
                ? clockOutDto.Notes
                : $"; {clockOutDto.Notes}";
            attendance.UpdatedAt = DateTime.UtcNow;

            // Calculate worked hours
            CalculateWorkedHours(attendance);

            // TODO: Update attendance record
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<AttendanceDto> CreateAttendanceAsync(
            AttendanceCreateDto createAttendanceDto,
            CancellationToken cancellationToken = default)
        {
            // TODO:
            throw new NotImplementedException();

            // - Check if employee exists
            // - Check if record already exists for date
            // - Map DTO to entity
            // - Calculate worked hours if both clock-in and clock-out are provided
            // - Save to repository
        }

        /// <inheritdoc/>
        public async Task<AttendanceDto?> GetAttendanceByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var attendance = await attendanceRepository.GetByIdAsync(id, cancellationToken);
            return attendance == null ? null : mapper.Map<AttendanceDto>(attendance);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AttendanceDto>> GetAttendancesByEmployeeAsync(
            int employeeId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AttendanceDto>> GetAttendancesByDateAsync(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<AttendanceDto?> GetTodayAttendanceAsync(
            int employeeId,
            CancellationToken cancellationToken = default)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<decimal> GetMonthlyWorkedHoursAsync(
            int employeeId,
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the total worked hours and overtime hours for the specified attendance record
        /// based on clock-in and clock-out times. Break duration is subtracted if provided.
        /// </summary>
        /// <param name="attendance">
        /// The attendance record containing clock-in, clock-out, and break duration information.
        /// </param>
        private void CalculateWorkedHours(Attendance attendance)
        {
            if (!attendance.ClockIn.HasValue || !attendance.ClockOut.HasValue)
                return;

            var totalWorked = attendance.ClockOut.Value - attendance.ClockIn.Value;

            // Subtract break duration
            if (attendance.BreakDuration.HasValue)
                totalWorked -= attendance.BreakDuration.Value;

            var workedHours = (decimal)totalWorked.TotalHours;
            const decimal normalWorkingHours = 8m;

            if (workedHours <= normalWorkingHours)
            {
                attendance.WorkedHours = Math.Max(0, workedHours);
                attendance.OvertimeHours = 0;
            }
            else
            {
                attendance.WorkedHours = normalWorkingHours;
                attendance.OvertimeHours = workedHours - normalWorkingHours;
            }
        }
    }
}
