using ITRockChallenge.Application.Dtos;

namespace ITRockChallenge.Application.Interfaces;

public interface ITaskImportService
{
    Task<ImportResponse> ImportAsync(string currentUserId);
}
