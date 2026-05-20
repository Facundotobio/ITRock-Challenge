using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;

namespace ITRockChallenge.Infrastructure.Http
{
    public class JsonPlaceholderClient : IJsonPlaceholderClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public JsonPlaceholderClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<ExternalTaskDto>> GetTodosAsync()
        {
            var client = _httpClientFactory.CreateClient();

            // Llamada a la API externa
            var response = await client.GetFromJsonAsync<IEnumerable<ExternalTaskDto>>("https://jsonplaceholder.typicode.com/todos");

            return response ?? Enumerable.Empty<ExternalTaskDto>();
        }
    }
}
