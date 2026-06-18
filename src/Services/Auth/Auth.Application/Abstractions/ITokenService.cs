using Auth.Domain.Entities;

namespace Auth.Application.Abstractions;

public interface ITokenService
{
    /// <summary>
    /// Issues a signed JWT access token for the given user. Returns the token string and its expiration (UTC).
    /// </summary>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically random opaque refresh token.
    /// </summary>
    string GenerateRefreshToken();
}
