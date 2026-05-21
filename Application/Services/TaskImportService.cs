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

        var candidateTasks = externalTasks
            .Where(t => t.UserId == ExternalUserIdToImport)
            .Take(MaxTasksToImport)
            .ToList();

        if (candidateTasks.Count == 0)
        {
            return new ImportResponse(0, Enumerable.Empty<TaskResponse>());
        }

        var alreadyImportedIds = await _taskRepository.GetImportedExternalSourceIdsAsync(currentUserId);

        var tasksToImport = candidateTasks
            .Where(t => !alreadyImportedIds.Contains(t.Id))
            .ToList();

        if (tasksToImport.Count == 0)
        {
            return new ImportResponse(0, Enumerable.Empty<TaskResponse>());
        }

        var importedTasks = tasksToImport.Select(extTask => new TodoTask
        {
            Title = extTask.Title,
            Description = $"Importada externamente (External ID: {extTask.Id})",
            Completed = extTask.Completed,
            UserId = currentUserId,
            ExternalSourceId = extTask.Id
        }).ToList();

        await _taskRepository.AddRangeAsync(importedTasks);

        var taskResponses = importedTasks.Select(TaskMapper.ToResponse);

        return new ImportResponse(importedTasks.Count, taskResponses);
    }
}
