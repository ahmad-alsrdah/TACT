using TACT.DTOs;
using TACT.Models;

namespace TACT.Services;

public interface IJwtService
{
    AuthResponseDto GenerateAuthTokens(User user);
    string GenerateSecureRefreshToken();
}