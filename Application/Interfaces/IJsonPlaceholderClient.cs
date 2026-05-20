using ITRockChallenge.Application.Dtos;

namespace ITRockChallenge.Application.Interfaces
{
    public interface IJsonPlaceholderClient
    {
        Task<IEnumerable<ExternalTaskDto>> GetTodosAsync();
    }
}
