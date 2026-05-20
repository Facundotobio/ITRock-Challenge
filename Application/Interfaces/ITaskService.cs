using ITRockChallenge.Application.Dtos;

namespace ITRockChallenge.Application.Interfaces
{
    public interface ITaskService
    {
        Task<PagedResponse<TaskResponse>> GetTasksByUserIdAsync(string userId, int page, int pageSize, bool? completed, string? search, DateTime? fromDate, DateTime? toDate);
        Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string userId);
        Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string userId);
        Task<bool> DeleteTaskAsync(Guid id, string userId);
        Task<ImportResponse> ImportExternalTasksAsync(string currentUserId);
    }
}
