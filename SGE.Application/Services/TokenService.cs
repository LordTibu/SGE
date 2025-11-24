using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;

namespace SGE.Application.Services;

/// <summary>
/// Service for generating, validating, and managing JWT access tokens and refresh tokens.
/// </summary>
public class TokenService(
    IConfiguration configuration,
    JwtSecurityTokenHandler tokenHandler,
    IRefreshTokenRepository refreshTokenRepository) : ITokenService
{
    /// <summary>
    /// Generates a JSON Web Token (JWT) access token for a specified user and their associated roles.
    /// </summary>
    /// <param name="user">The user for whom the access token is being generated.</param>
    /// <param name="roles">The list of roles associated with the user.</param>
    /// <returns>A string representation of the generated JWT access token.</returns>
    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        // Ajout des rôles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["AccessTokenExpiration"]!)),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(secret),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a secure refresh token that can be used to obtain a new access token.
    /// </summary>
    /// <returns>A base64-encoded string representation of the generated refresh token.</returns>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Retrieves the claims principal from a provided expired JSON Web Token (JWT).
    /// This allows for extracting user claims from a token that is no longer valid for authentication but still contains valid claims data.
    /// </summary>
    /// <param name="token">The expired JWT from which to extract the claims principal.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> representing the claims contained in the expired token.</returns>
    /// <exception cref="SecurityTokenException">Thrown if the token is invalid or the algorithm used in the header is not HmacSha256.</exception>
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secret),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = false // Important : on ne valide pas l'expiration ici
        };

        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

        if (validatedToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Token invalide");
        }

        return principal;
    }

    /// <summary>
    /// Creates a refresh token asynchronously for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user for whom the refresh token is being created.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created refresh token entity.</returns>
    public async Task<RefreshToken> CreateRefreshTokenAsync(string userId)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var refreshTokenExpiration = int.Parse(jwtSettings["RefreshTokenExpiration"]!);

        var refreshToken = new RefreshToken
        {
            Token = GenerateRefreshToken(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiration),
            CreatedAt = DateTime.UtcNow
        };

        await refreshTokenRepository.AddAsync(refreshToken);

        return refreshToken;
    }

    /// <summary>
    /// Validates a refresh token for a specific user asynchronously.
    /// </summary>
    /// <param name="token">The refresh token to validate.</param>
    /// <param name="userId">The unique identifier of the user associated with the refresh token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the refresh token is valid and active.</returns>
    public async Task<bool> ValidateRefreshTokenAsync(string token, string userId)
    {
        var refreshToken = await refreshTokenRepository.GetByTokenAsync(token);

        if (refreshToken == null)
            return false;

        if (refreshToken.UserId != userId)
            return false;

        if (refreshToken.IsRevoked)
            return false;

        if (refreshToken.IsExpired)
            return false;

        return true;
    }

    /// <summary>
    /// Revokes a specific refresh token asynchronously.
    /// </summary>
    /// <param name="token">The refresh token to be revoked.</param>
    /// <param name="reason">The reason for revoking the refresh token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RevokeRefreshTokenAsync(string token, string reason)
    {
        var refreshToken = await refreshTokenRepository.GetByTokenAsync(token);

        if (refreshToken == null || refreshToken.IsRevoked)
            return;

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReasonRevoked = reason;

        await refreshTokenRepository.UpdateAsync(refreshToken);
    }

    /// <summary>
    /// Revokes all active refresh tokens for a specific user asynchronously.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose refresh tokens are to be revoked.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RevokeAllUserRefreshTokensAsync(string userId)
    {
        await refreshTokenRepository.RevokeAllUserTokensAsync(userId, "Déconnexion de tous les appareils");
    }
}

