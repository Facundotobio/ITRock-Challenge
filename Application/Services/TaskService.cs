using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Domain;

namespace ITRockChallenge.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IJsonPlaceholderClient _externalClient;

    public TaskService(ITaskRepository taskRepository, IJsonPlaceholderClient externalClient)
    {
        _taskRepository = taskRepository;
        _externalClient = externalClient;
    }

    public async Task<PagedResponse<TaskResponse>> GetTasksByUserIdAsync(
        string userId, int page, int pageSize, bool? completed, string? search, DateTime? fromDate, DateTime? toDate)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var (tasks, totalRecords) = await _taskRepository.GetPagedByUserIdAsync(
            userId, page, pageSize, completed, search, fromDate, toDate);

        var taskResponses = tasks.Select(t =>
            new TaskResponse(t.Id, t.Title, t.Description, t.Completed, t.CreatedAt));

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

        return MapToResponse(savedTask);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string userId)
    {
        var task = await _taskRepository.FindByIdAsync(id);

        if (task == null) return null;
        if (task.UserId != userId) return null;

        if (request.Title != null) task.Title = request.Title;
        if (request.Description != null) task.Description = request.Description;
        if (request.Completed != null) task.Completed = request.Completed.Value;

        await _taskRepository.UpdateAsync(task);

        return MapToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(Guid id, string userId)
    {
        var task = await _taskRepository.FindByIdAsync(id);

        if (task == null) return false;
        if (task.UserId != userId) return false;

        await _taskRepository.DeleteAsync(task);

        return true;
    }

    public async Task<ImportResponse> ImportExternalTasksAsync(string currentUserId)
    {
        var externalTasks = await _externalClient.GetTodosAsync();

        var filteredTasks = externalTasks
            .Where(t => t.UserId == 1)
            .Take(5)
            .ToList();

        if (filteredTasks.Count == 0)
        {
            return new ImportResponse(0, Enumerable.Empty<TaskResponse>());
        }

        var importedTasks = filteredTasks.Select(extTask => new TodoTask
        {
            Title = extTask.Title,
            Description = $"Importada externamente (External ID: {extTask.Id})",
            Completed = extTask.Completed,
            UserId = currentUserId
        }).ToList();

        await _taskRepository.AddRangeAsync(importedTasks);

        var taskResponses = importedTasks.Select(MapToResponse);

        return new ImportResponse(importedTasks.Count, taskResponses);
    }

    private static TaskResponse MapToResponse(TodoTask task) =>
        new(task.Id, task.Title, task.Description, task.Completed, task.CreatedAt);
}
