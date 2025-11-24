using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories;
using SGE.Core.Entities;
using SGE.Infrastructure.Data;

namespace SGE.Infrastructure.Repositories;

/// <summary>
/// Provides the implementation for leave request data access and management operations.
/// </summary>
/// <remarks>
/// This repository extends the base functionality provided by the generic <see cref="Repository{T}"/> class.
/// It focuses specifically on operations related to the <see cref="LeaveRequest"/> entity, including retrieving
/// leave requests associated with a specific employee.
/// </remarks>
public class LeaveRequestRepository : Repository<LeaveRequest>, ILeaveRequestRepository
{
    /// <summary>
    /// Initializes a new instance of the LeaveRequestRepository class.
    /// </summary>
    /// <param name="context">The database context used to access leave request data.</param>
    public LeaveRequestRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves a leave request by its ID, including the related employee.
    /// </summary>
    /// <param name="id">The unique identifier of the leave request.</param>
    /// <param name="cancellationToken">A token to observe for operation cancellation.</param>
    /// <returns>The leave request with the specified ID, or null if not found.</returns>
    public override async Task<LeaveRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(lr => lr.Employee)
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves a collection of leave requests associated with a specific employee.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee whose leave requests are to be retrieved.</param>
    /// <param name="cancellationToken">A token to observe for operation cancellation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="IEnumerable{T}"/> of <see cref="LeaveRequest"/> objects associated with the specified employee.</returns>
    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(lr => lr.EmployeeId == employeeId)
            .Include(lr => lr.Employee)
            .ToListAsync(cancellationToken);
    }
}

