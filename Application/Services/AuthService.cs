using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;

namespace ITRockChallenge.Application.Services;

public class AuthService : IAuthService
{
    public AuthResult? Authenticate(LoginRequest request)
    {
        // Credenciales estáticas requeridas por el enunciado del challenge
        if (request.Username == "admin" && request.Password == "password123")
        {
            return new AuthResult(request.Username, "1");
        }

        return null;
    }
}
