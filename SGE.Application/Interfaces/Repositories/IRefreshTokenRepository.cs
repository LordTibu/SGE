using SGE.Core.Entities;

namespace SGE.Application.Interfaces.Repositories;

/// <summary>
/// Represents a repository interface for handling data access operations specific to RefreshToken entities.
/// Provides methods for managing refresh tokens including retrieval, validation, and revocation.
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>
    /// Asynchronously retrieves a refresh token by its token string.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to observe the cancellation request.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the refresh token if found, or null if not found.</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all active refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose active refresh tokens are to be retrieved.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to observe the cancellation request.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of active refresh tokens for the specified user.</returns>
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously revokes all active refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose refresh tokens should be revoked.</param>
    /// <param name="reason">The reason for revoking the refresh tokens.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to observe the cancellation request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RevokeAllUserTokensAsync(string userId, string reason, CancellationToken cancellationToken = default);
}

