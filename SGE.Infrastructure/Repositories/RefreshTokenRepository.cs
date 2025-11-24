using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories;
using SGE.Core.Entities;
using SGE.Infrastructure.Data;

namespace SGE.Infrastructure.Repositories;

/// <summary>
/// Provides the implementation for refresh token data access and management operations.
/// </summary>
/// <remarks>
/// This repository extends the base functionality provided by the generic <see cref="Repository{T}"/> class.
/// It focuses specifically on operations related to the <see cref="RefreshToken"/> entity, including
/// retrieving, validating, and revoking refresh tokens.
/// </remarks>
public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    /// <summary>
    /// Initializes a new instance of the RefreshTokenRepository class.
    /// </summary>
    /// <param name="context">The database context used to access refresh token data.</param>
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves a refresh token by its token string.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <param name="cancellationToken">A token to observe for operation cancellation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the refresh token if found, or null if not found.</returns>
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    /// <summary>
    /// Retrieves all active (non-revoked and non-expired) refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose active refresh tokens are to be retrieved.</param>
    /// <param name="cancellationToken">A token to observe for operation cancellation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of active refresh tokens for the specified user.</returns>
    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Revokes all active refresh tokens for a specific user by setting the RevokedAt timestamp and reason.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose refresh tokens should be revoked.</param>
    /// <param name="reason">The reason for revoking the refresh tokens.</param>
    /// <param name="cancellationToken">A token to observe for operation cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RevokeAllUserTokensAsync(string userId, string reason, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = reason;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

