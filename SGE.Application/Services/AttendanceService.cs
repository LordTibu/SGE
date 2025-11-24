using AutoMapper;
using SGE.Application.DTOs.Attendances;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;

namespace SGE.Application.Services
{
    /// <summary>
    /// Provides functionalities related to managing employee attendances,
    /// including clocking in/out, retrieving attendance records, and calculating worked hours.
    /// </summary>
    public class AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        IMapper mapper
    ) : IAttendanceService
    {
        /// <summary>
        /// Registers the clock-in time for an employee. If an attendance record for the employee
        /// on the specified day already exists without a clock-in time, it updates the record.
        /// Otherwise, it creates a new attendance record.
        /// </summary>
        /// <param name="clockInDto">An object containing the employee's ID and clock-in time information.</param>
        /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
        /// <returns>A DTO containing the attendance details after the clock-in operation.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the employee is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the employee has already clocked in on the same day.</exception>
        public async Task<AttendanceDto> ClockInAsync(
            ClockInOutDto clockInDto,
            CancellationToken cancellationToken = default
        )
        {
            if (!await employeeRepository.ExistsAsync(clockInDto.EmployeeId, cancellationToken))
                throw new KeyNotFoundException($"Employee with ID {clockInDto.EmployeeId} not found");

            var date = clockInDto.DateTime.Date;
            var time = clockInDto.DateTime.TimeOfDay;

            // Check if a record already exists for the date
            var existingAttendances = await attendanceRepository.FindAsync(
                a => a.EmployeeId == clockInDto.EmployeeId && a.Date == date,
                cancellationToken
            );
            var attendance = existingAttendances.FirstOrDefault();

            if (attendance != null)
            {
                if (attendance.ClockIn.HasValue)
                    throw new InvalidOperationException("Employee has already clocked in today");

                attendance.ClockIn = time;
                attendance.Notes = (string.IsNullOrEmpty(attendance.Notes) ? "" : attendance.Notes + "; ") + clockInDto.Notes;
                attendance.UpdatedAt = DateTime.UtcNow;
                attendance = await attendanceRepository.UpdateAsync(attendance, cancellationToken);
            }
            else
            {
                attendance = new Attendance
                {
                    EmployeeId = clockInDto.EmployeeId,
                    Date = date,
                    ClockIn = time,
                    Notes = clockInDto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await attendanceRepository.AddAsync(attendance, cancellationToken);
            }

            var dto = mapper.Map<AttendanceDto>(attendance);
            var employee = await employeeRepository.GetByIdAsync(attendance.EmployeeId, cancellationToken);
            if (employee != null)
                dto.EmployeeName = $"{employee.FirstName} {employee.LastName}";
            return dto;
        }

        /// <summary>
        /// Registers the clock-out time for an employee.
        /// </summary>
        /// <param name="clockOutDto">An object containing the employee's ID and clock-out time information.</param>
        /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
        /// <returns>A DTO containing the updated attendance details after the clock-out operation.</returns>
        public async Task<AttendanceDto> ClockOutAsync(
            ClockInOutDto clockOutDto,
            CancellationToken cancellationToken = default
        )
        {
            // Vérifier que l'employé existe
            if (!await employeeRepository.ExistsAsync(clockOutDto.EmployeeId, cancellationToken))
                throw new KeyNotFoundException($"Employee with ID {clockOutDto.EmployeeId} not found");

            var date = clockOutDto.DateTime.Date;
            var time = clockOutDto.DateTime.TimeOfDay;

            // Check if a record exists for this date
            var existingAttendances = await attendanceRepository.FindAsync(
                a => a.EmployeeId == clockOutDto.EmployeeId && a.Date == date,
                cancellationToken
            );
            var attendance = existingAttendances.FirstOrDefault();

            if (attendance == null)
                throw new InvalidOperationException("No clock-in record found for today");

            if (!attendance.ClockIn.HasValue)
                throw new InvalidOperationException("Employee must clock in before clocking out");

            if (attendance.ClockOut.HasValue)
                throw new InvalidOperationException("Employee has already clocked out today");

            attendance.ClockOut = time;
            attendance.Notes += (string.IsNullOrEmpty(attendance.Notes) ? "" : "; ") + clockOutDto.Notes;
            attendance.UpdatedAt = DateTime.UtcNow;

            // Calculer les heures travaillées
            CalculateWorkedHours(attendance);

            // Mettre à jour les informations dans la base de données
            attendance = await attendanceRepository.UpdateAsync(attendance, cancellationToken);
            var dto = mapper.Map<AttendanceDto>(attendance);
            var employee = await employeeRepository.GetByIdAsync(attendance.EmployeeId, cancellationToken);
            if (employee != null)
                dto.EmployeeName = $"{employee.FirstName} {employee.LastName}";
            return dto;
        }

        /// <summary>
        /// Creates a new attendance record for an employee based on the provided details.
        /// </summary>
        public async Task<AttendanceDto> CreateAttendanceAsync(
            AttendanceCreateDto createAttendanceDto,
            CancellationToken cancellationToken = default
        )
        {
            // Vérifier que l'employé existe
            if (!await employeeRepository.ExistsAsync(createAttendanceDto.EmployeeId, cancellationToken))
                throw new KeyNotFoundException($"Employee with ID {createAttendanceDto.EmployeeId} not found");

            var date = DateTime.SpecifyKind(createAttendanceDto.Date.Date, DateTimeKind.Utc);

            // Vérifier s'il existe déjà une entrée pour cette date
            var existing = await attendanceRepository.FindAsync(
                a => a.EmployeeId == createAttendanceDto.EmployeeId && a.Date == date,
                cancellationToken
            );
            if (existing.Any())
                throw new InvalidOperationException("An attendance record for this date already exists");

            var entity = mapper.Map<Attendance>(createAttendanceDto);
            entity.Date = date;
            CalculateWorkedHours(entity);

            await attendanceRepository.AddAsync(entity, cancellationToken);
            var dto = mapper.Map<AttendanceDto>(entity);
            var employee = await employeeRepository.GetByIdAsync(entity.EmployeeId, cancellationToken);
            if (employee != null)
                dto.EmployeeName = $"{employee.FirstName} {employee.LastName}";
            return dto;
        }

        /// <summary>
        /// Retrieves an attendance record by its unique identifier.
        /// </summary>
        public async Task<AttendanceDto?> GetAttendanceByIdAsync(
            int id,
            CancellationToken cancellationToken = default
        )
        {
            var att = await attendanceRepository.GetByIdAsync(id, cancellationToken);
            return att == null ? null : mapper.Map<AttendanceDto>(att);
        }

        /// <summary>
        /// Retrieves attendance records for a specific employee within an optional date range.
        /// </summary>
        public async Task<IEnumerable<AttendanceDto>> GetAttendancesByEmployeeAsync(
            int employeeId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default
        )
        {
            var records = await attendanceRepository.GetByEmployeeAsync(employeeId, cancellationToken);

            if (startDate.HasValue)
            {
                var startUtc = startDate.Value.Date.ToUniversalTime();
                records = records.Where(a => a.Date.Date.ToUniversalTime() >= startUtc);
            }

            if (endDate.HasValue)
            {
                var endUtc = endDate.Value.Date.ToUniversalTime();
                records = records.Where(a => a.Date.Date.ToUniversalTime() <= endUtc);
            }

            var result = new List<AttendanceDto>();
            foreach (var att in records)
            {
                var dto = mapper.Map<AttendanceDto>(att);
                var employee = await employeeRepository.GetByIdAsync(att.EmployeeId, cancellationToken);
                if (employee != null)
                    dto.EmployeeName = $"{employee.FirstName} {employee.LastName}";
                result.Add(dto);
            }

            return result;
        }


        /// <summary>
        /// Retrieves attendance records for all employees on the specified date.
        /// </summary>
        public async Task<IEnumerable<AttendanceDto>> GetAttendancesByDateAsync(
            DateTime date,
            CancellationToken cancellationToken = default
        )
        {
            var dayUtc = date.Date.ToUniversalTime();
            var records = await attendanceRepository.FindAsync(a => a.Date == dayUtc, cancellationToken);

            var result = new List<AttendanceDto>();
            foreach (var att in records)
            {
                var dto = mapper.Map<AttendanceDto>(att);
                var employee = await employeeRepository.GetByIdAsync(att.EmployeeId, cancellationToken);
                if (employee != null)
                    dto.EmployeeName = $"{employee.FirstName} {employee.LastName}";
                result.Add(dto);
            }
            return result;
        }

        /// <summary>
        /// Retrieves the attendance record for an employee for the current date, if available.
        /// </summary>
        public async Task<AttendanceDto?> GetTodayAttendanceAsync(
            int employeeId,
            CancellationToken cancellationToken = default
        )
        {
            var todayUtc = DateTime.UtcNow.Date;
            var records = await attendanceRepository.FindAsync(
                a => a.EmployeeId == employeeId && a.Date == todayUtc,
                cancellationToken
            );

            var attendance = records.FirstOrDefault();
            if (attendance == null)
                return null;

            var dto = mapper.Map<AttendanceDto>(attendance);
            var employee = await employeeRepository.GetByIdAsync(attendance.EmployeeId, cancellationToken);
            if (employee != null)
                dto.EmployeeName = $"{employee.FirstName} {employee.LastName}";

            return dto;
        }

        /// <summary>
        /// Calculates the total hours worked by an employee for a specific month and year.
        /// </summary>
        public async Task<decimal> GetMonthlyWorkedHoursAsync(
            int employeeId,
            int year,
            int month,
            CancellationToken cancellationToken = default
        )
        {
            var records = await attendanceRepository.FindAsync(
                a => a.EmployeeId == employeeId &&
                    a.Date.ToUniversalTime().Year == year &&
                    a.Date.ToUniversalTime().Month == month,
                cancellationToken
            );

            return records.Sum(a => a.WorkedHours);
        }


        /// <summary>
        /// Calculates the total worked hours and overtime hours for an attendance record.
        /// </summary>
        private void CalculateWorkedHours(Attendance attendance)
        {
            if (!attendance.ClockIn.HasValue || !attendance.ClockOut.HasValue)
                return;

            var totalWorked = attendance.ClockOut.Value - attendance.ClockIn.Value;

            // Soustraire la pause
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
