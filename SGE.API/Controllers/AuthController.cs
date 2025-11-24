using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Users;
using SGE.Application.Interfaces.Services;

namespace SGE.API.Controllers;

/// <summary>
/// Controller for managing authentication and authorization operations.
/// Provides endpoints for user registration, login, token refresh, and logout.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="registerDto">The registration data containing user information.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns the authentication response with access token, refresh token, and user information.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(200, Type = typeof(AuthResponseDto))]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterDto registerDto,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(registerDto);
        return Ok(result);
    }

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    /// <param name="loginDto">The login credentials (email and password).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns the authentication response with access token, refresh token, and user information.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(200, Type = typeof(AuthResponseDto))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto loginDto,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(loginDto);
        return Ok(result);
    }

    /// <summary>
    /// Refreshes the access token using a valid refresh token.
    /// </summary>
    /// <param name="refreshTokenDto">The refresh token data containing the expired access token and refresh token.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a new authentication response with updated tokens.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(200, Type = typeof(AuthResponseDto))]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(
        [FromBody] RefreshTokenDto refreshTokenDto,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(refreshTokenDto);
        return Ok(result);
    }

    /// <summary>
    /// Logs out the current user by revoking all their refresh tokens.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to log out.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a success message.</returns>
    [HttpPost("logout/{userId}")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> Logout(
        string userId,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(userId);
        return Ok(new { message = "Déconnexion réussie" });
    }

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    /// <param name="token">The refresh token to revoke.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a success message.</returns>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> RevokeToken(
        [FromBody] string token,
        CancellationToken cancellationToken)
    {
        await authService.RevokeTokenAsync(token);
        return Ok(new { message = "Token révoqué avec succès" });
    }

    /// <summary>
    /// Retrieves the current authenticated user's information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns the user information.</returns>
    [HttpGet("me/{userId}")]
    [Authorize]
    [ProducesResponseType(200, Type = typeof(UserDto))]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(userId);
        if (user == null)
            return NotFound("Utilisateur non trouvé");

        return Ok(user);
    }
}

