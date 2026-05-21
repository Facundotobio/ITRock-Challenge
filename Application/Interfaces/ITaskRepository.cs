using ITRockChallenge.Domain;

namespace ITRockChallenge.Application.Interfaces;

public interface ITaskRepository
{
    Task<(IReadOnlyList<TodoTask> Items, int TotalCount)> GetPagedByUserIdAsync(
        string userId,
        int page,
        int pageSize,
        bool? completed,
        string? search,
        DateTime? fromDate,
        DateTime? toDate);

    Task<TodoTask> AddAsync(TodoTask task);

    Task<TodoTask?> FindByIdAsync(Guid id);

    Task UpdateAsync(TodoTask task);

    Task DeleteAsync(TodoTask task);

    Task AddRangeAsync(IEnumerable<TodoTask> tasks);

    Task<HashSet<int>> GetImportedExternalSourceIdsAsync(string userId);
}
