using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Application.Mapping;
using ITRockChallenge.Domain;

namespace ITRockChallenge.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskImportService _taskImportService;

    public TaskService(ITaskRepository taskRepository, ITaskImportService taskImportService)
    {
        _taskRepository = taskRepository;
        _taskImportService = taskImportService;
    }

    public async Task<PagedResponse<TaskResponse>> GetTasksByUserIdAsync(
        string userId, int page, int pageSize, bool? completed, string? search, DateTime? fromDate, DateTime? toDate)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var (tasks, totalRecords) = await _taskRepository.GetPagedByUserIdAsync(
            userId, page, pageSize, completed, search, fromDate, toDate);

        var taskResponses = tasks.Select(TaskMapper.ToResponse);

        return new PagedResponse<TaskResponse>(taskResponses, page, pageSize, totalRecords);
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string userId)
    {
        var newTask = new TodoTask
        {
            Title = request.Title,
            Description = request.Description,
            UserId = userId
        };

        var savedTask = await _taskRepository.AddAsync(newTask);

        return TaskMapper.ToResponse(savedTask);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string userId)
    {
        var task = await _taskRepository.FindByIdAsync(id);

        if (task == null || task.UserId != userId)
        {
            return null;
        }

        if (request.Title != null) task.Title = request.Title;
        if (request.Description != null) task.Description = request.Description;
        if (request.Completed != null) task.Completed = request.Completed.Value;

        await _taskRepository.UpdateAsync(task);

        return TaskMapper.ToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(Guid id, string userId)
    {
        var task = await _taskRepository.FindByIdAsync(id);

        if (task == null || task.UserId != userId)
        {
            return false;
        }

        await _taskRepository.DeleteAsync(task);

        return true;
    }

    public Task<ImportResponse> ImportExternalTasksAsync(string currentUserId) =>
        _taskImportService.ImportAsync(currentUserId);
}
