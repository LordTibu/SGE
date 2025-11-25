using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SGE.Application.DTOs.Users;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;
using SGE.Core.Exceptions;

namespace SGE.Application.Services;

/// <summary>
/// Provides authentication and authorization services such as
/// user registration, login, token generation, and token refresh.
/// </summary>
public class AuthService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IMapper mapper
) : IAuthService
{
    /// <summary>
    /// Registers a new user with the provided registration details.
    /// </summary>
    /// <param name="registerDto">The user registration data (username, email, password).</param>
    /// <returns>An <see cref="AuthResponseDto"/> containing access and refresh tokens and user information.</returns>
    /// <exception cref="UserAlreadyExistsException">Thrown when the email or username already exists in the system.</exception>
    /// <exception cref="UserRegistrationException">Thrown when user creation fails due to Identity errors.</exception>
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // Vérifier si l'email existe déjà
        var existingUserByEmail = await userManager.FindByEmailAsync(registerDto.Email);
        if (existingUserByEmail != null)
            throw new UserAlreadyExistsException(registerDto.Email, "email");

        // Vérifier si le username existe déjà
        var existingUserByUsername = await userManager.FindByNameAsync(registerDto.UserName);
        if (existingUserByUsername != null)
            throw new UserAlreadyExistsException(registerDto.UserName, "nom d'utilisateur");

        // Créer l'utilisateur
        var user = mapper.Map<ApplicationUser>(registerDto);
        var result = await userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            throw new UserRegistrationException(errors);
        }

        // Attribuer le rôle User par défaut
        await userManager.AddToRoleAsync(user, "User");

        // Générer les tokens
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await tokenService.CreateRefreshTokenAsync(user.Id);

        // Mettre à jour la date de dernière connexion
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Mapper l'utilisateur vers UserDto
        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = userDto
        };
    }

    /// <summary>
    /// Authenticates a user with the provided login credentials.
    /// </summary>
    /// <param name="loginDto">The login data (email and password).</param>
    /// <returns>An <see cref="AuthResponseDto"/> containing access and refresh tokens and user information.</returns>
    /// <exception cref="InvalidCredentialsException">Thrown when the email or password is incorrect.</exception>
    /// <exception cref="UserNotActiveException">Thrown when the user account is disabled or inactive.</exception>
    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        // Trouver l'utilisateur par email
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, loginDto.Password))
            throw new InvalidCredentialsException();

        // Vérifier si l'utilisateur est actif
        if (!user.IsActive)
            throw new UserNotActiveException();

        // Générer les tokens
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await tokenService.CreateRefreshTokenAsync(user.Id);

        // Mettre à jour la date de dernière connexion
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Mapper l'utilisateur vers UserDto
        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = userDto
        };
    }

    /// <summary>
    /// Refreshes the access token using a valid refresh token.
    /// </summary>
    /// <param name="refreshTokenDto">The refresh token data (access token and refresh token).</param>
    /// <returns>An <see cref="AuthResponseDto"/> containing new access and refresh tokens and user information.</returns>
    /// <exception cref="InvalidRefreshTokenException">Thrown when the refresh token is invalid or expired.</exception>
    /// <exception cref="UserNotActiveException">Thrown when the user account is disabled or inactive.</exception>
    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        // Extraire le principal du token expiré
        var principal = tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new InvalidRefreshTokenException();

        // Valider le refresh token
        var isValidRefreshToken = await tokenService.ValidateRefreshTokenAsync(refreshTokenDto.RefreshToken, userId);
        if (!isValidRefreshToken)
            throw new InvalidRefreshTokenException();

        // Récupérer l'utilisateur
        var user = await userManager.FindByIdAsync(userId);
        if (user is not { IsActive: true })
            throw new UserNotActiveException();

        // Révoquer l'ancien refresh token
        await tokenService.RevokeRefreshTokenAsync(refreshTokenDto.RefreshToken, "Remplacé par un nouveau token");

        // Générer de nouveaux tokens
        var roles = await userManager.GetRolesAsync(user);
        var newAccessToken = tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = await tokenService.CreateRefreshTokenAsync(user.Id);

        // Mettre à jour la date de dernière connexion
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Mapper l'utilisateur vers UserDto
        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            User = userDto
        };
    }

    /// <summary>
    /// Logs out a user by revoking all their active refresh tokens.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns><c>true</c> if logout succeeded, otherwise <c>false</c>.</returns>
    public async Task<bool> LogoutAsync(string userId)
    {
        await tokenService.RevokeAllUserRefreshTokensAsync(userId);
        return true;
    }

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    /// <param name="token">The refresh token string to revoke.</param>
    /// <returns><c>true</c> if the token was revoked successfully, otherwise <c>false</c>.</returns>
    public async Task<bool> RevokeTokenAsync(string token)
    {
        await tokenService.RevokeRefreshTokenAsync(token, "Révoqué manuellement");
        return true;
    }

    /// <summary>
    /// Retrieves the details of the currently authenticated user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A <see cref="UserDto"/> representing the user, or <c>null</c> if not found.</returns>
    public async Task<UserDto?> GetCurrentUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        var roles = await userManager.GetRolesAsync(user);
        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return userDto;
    }

    /// <summary>
    /// Updates the roles assigned to a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose roles are to be updated.</param>
    /// <param name="roles">The list of role names to assign to the user.</param>
    /// <returns><c>true</c> if the update was successful, otherwise <c>false</c>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    public async Task<bool> UpdateUserRolesAsync(string userId, IList<string> roles)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        // Récupérer les rôles actuels
        var currentRoles = await userManager.GetRolesAsync(user);

        // Supprimer tous les rôles actuels
        if (currentRoles.Any())
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        // Ajouter les nouveaux rôles
        if (roles.Any())
        {
            var result = await userManager.AddToRolesAsync(user, roles);
            if (!result.Succeeded)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Updates the information of an existing user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="updateDto">The data transfer object containing the updated user information.</param>
    /// <returns>A <see cref="UserDto"/> containing the updated user information.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    /// <exception cref="UserAlreadyExistsException">Thrown when the email or username already exists for another user.</exception>
    public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserDto updateDto)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        // Vérifier si l'email est modifié et s'il existe déjà
        if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
        {
            var existingUserByEmail = await userManager.FindByEmailAsync(updateDto.Email);
            if (existingUserByEmail != null && existingUserByEmail.Id != userId)
                throw new UserAlreadyExistsException(updateDto.Email, "email");
            
            user.Email = updateDto.Email;
            user.NormalizedEmail = updateDto.Email.ToUpperInvariant();
        }

        // Vérifier si le username est modifié et s'il existe déjà
        if (!string.IsNullOrEmpty(updateDto.UserName) && updateDto.UserName != user.UserName)
        {
            var existingUserByUsername = await userManager.FindByNameAsync(updateDto.UserName);
            if (existingUserByUsername != null && existingUserByUsername.Id != userId)
                throw new UserAlreadyExistsException(updateDto.UserName, "nom d'utilisateur");
            
            user.UserName = updateDto.UserName;
            user.NormalizedUserName = updateDto.UserName.ToUpperInvariant();
        }

        // Mettre à jour les autres propriétés
        if (!string.IsNullOrEmpty(updateDto.FirstName))
            user.FirstName = updateDto.FirstName;

        if (!string.IsNullOrEmpty(updateDto.LastName))
            user.LastName = updateDto.LastName;

        if (updateDto.IsActive.HasValue)
            user.IsActive = updateDto.IsActive.Value;

        if (updateDto.EmployeeId.HasValue)
            user.EmployeeId = updateDto.EmployeeId;

        // Sauvegarder les modifications
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            throw new UserRegistrationException(errors);
        }

        // Récupérer les rôles et mapper vers UserDto
        var roles = await userManager.GetRolesAsync(user);
        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return userDto;
    }

    /// <summary>
    /// Deletes a user from the system.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <returns><c>true</c> if the deletion was successful, otherwise <c>false</c>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        // Révoquer tous les refresh tokens avant suppression
        await tokenService.RevokeAllUserRefreshTokensAsync(userId);

        // Supprimer l'utilisateur
        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }
}

