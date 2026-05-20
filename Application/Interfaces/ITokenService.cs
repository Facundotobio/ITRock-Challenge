namespace ITRockChallenge.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(string username, string userId);
    }
}
