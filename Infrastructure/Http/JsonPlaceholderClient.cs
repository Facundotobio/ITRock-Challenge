using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;

namespace ITRockChallenge.Infrastructure.Http;

public class JsonPlaceholderClient : IJsonPlaceholderClient
{
    private const string TodosEndpoint = "todos";

    private readonly HttpClient _httpClient;

    public JsonPlaceholderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ExternalTaskDto>> GetTodosAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<ExternalTaskDto>>(TodosEndpoint);

        return response ?? Enumerable.Empty<ExternalTaskDto>();
    }
}
