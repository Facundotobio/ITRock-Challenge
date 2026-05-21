using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Application.Mapping;
using ITRockChallenge.Domain;

namespace ITRockChallenge.Application.Services;

public class TaskImportService : ITaskImportService
{
    private const int ExternalUserIdToImport = 1;
    private const int MaxTasksToImport = 5;

    private readonly ITaskRepository _taskRepository;
    private readonly IJsonPlaceholderClient _externalClient;

    public TaskImportService(ITaskRepository taskRepository, IJsonPlaceholderClient externalClient)
    {
        _taskRepository = taskRepository;
        _externalClient = externalClient;
    }

    public async Task<ImportResponse> ImportAsync(string currentUserId)
    {
        var externalTasks = await _externalClient.GetTodosAsync();

        var filteredTasks = externalTasks
            .Where(t => t.UserId == ExternalUserIdToImport)
            .Take(MaxTasksToImport)
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

        var taskResponses = importedTasks.Select(TaskMapper.ToResponse);

        return new ImportResponse(importedTasks.Count, taskResponses);
    }
}
