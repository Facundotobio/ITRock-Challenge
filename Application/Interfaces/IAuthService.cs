using ITRockChallenge.Application.Dtos;

namespace ITRockChallenge.Application.Interfaces;

public interface IAuthService
{
    AuthResult? Authenticate(LoginRequest request);
}
