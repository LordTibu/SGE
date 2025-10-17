using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories;
using SGE.Core.Entities;
using SGE.Infrastructure.Data;

namespace SGE.Infrastructure.Repositories
{
    /// <summary>
    /// Defines a repository that provides data access capabilities for Attendance entities.
    /// Extends the generic Repository base class and implements the IAttendanceRepository interface.
    /// </summary>
    public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttendanceRepository"/> class.
        /// </summary>
        /// <param name="context">The application's database context.</param>
        public AttendanceRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Retrieves attendance records associated with a specific employee from the data source.
        /// </summary>
        /// <param name="employeeId">
        /// The unique identifier of the employee whose attendance records are to be fetched.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation, with a collection of 
        /// <see cref="Attendance"/> entities for the specified employee.
        /// </returns>
        public async Task<IEnumerable<Attendance>> GetByEmployeeAsync(
            int employeeId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId)
                .Include(a => a.Employee)
                .ToListAsync(cancellationToken);
        }
    }
}