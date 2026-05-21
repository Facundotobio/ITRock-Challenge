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
    private readonly ILogger<TaskImportService> _logger;

    public TaskImportService(ITaskRepository taskRepository, IJsonPlaceholderClient externalClient, ILogger<TaskImportService> logger)
    {
        _taskRepository = taskRepository;
        _externalClient = externalClient;
        _logger = logger;
    }

    public async Task<ImportResponse> ImportAsync(string currentUserId)
    {
        _logger?.LogInformation("Iniciando importación de tareas externas para UserId={UserId}", currentUserId);

        var externalTasks = await _externalClient.GetTodosAsync();

        var candidateTasks = externalTasks
            .Where(t => t.UserId == ExternalUserIdToImport)
            .Take(MaxTasksToImport)
            .ToList();

        _logger?.LogInformation("Tareas candidatas obtenidas: {Count}", candidateTasks.Count);

        if (candidateTasks.Count == 0)
        {
            _logger?.LogWarning("No hay tareas candidatas para importar.");
            return new ImportResponse(0, Enumerable.Empty<TaskResponse>());
        }

        var alreadyImportedIds = await _taskRepository.GetImportedExternalSourceIdsAsync(currentUserId);

        var tasksToImport = candidateTasks
            .Where(t => !alreadyImportedIds.Contains(t.Id))
            .ToList();

        _logger?.LogInformation("Tareas a importar tras filtrar ya importadas: {Count}", tasksToImport.Count);

        if (tasksToImport.Count == 0)
        {
            _logger?.LogWarning("No hay tareas nuevas para importar para UserId={UserId}", currentUserId);
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

        _logger?.LogInformation("Importación completada. Cantidad importada: {ImportedCount} para UserId={UserId}", importedTasks.Count, currentUserId);

        var taskResponses = importedTasks.Select(TaskMapper.ToResponse);

        return new ImportResponse(importedTasks.Count, taskResponses);
    }
}