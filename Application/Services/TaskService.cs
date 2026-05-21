using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Application.Mapping;
using ITRockChallenge.Domain;

namespace ITRockChallenge.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskImportService _taskImportService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository taskRepository, ITaskImportService taskImportService, ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _taskImportService = taskImportService;
        _logger = logger;
    }

    public async Task<PagedResponse<TaskResponse>> GetTasksByUserIdAsync(
        string userId, int page, int pageSize, bool? completed, string? search, DateTime? fromDate, DateTime? toDate)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        _logger?.LogInformation("Obteniendo tareas para usuario {UserId} page={Page} pageSize={PageSize} completed={Completed} search={Search} from={FromDate} to={ToDate}",
            userId, page, pageSize, completed, search, fromDate, toDate);

        var (tasks, totalRecords) = await _taskRepository.GetPagedByUserIdAsync(
            userId, page, pageSize, completed, search, fromDate, toDate);

        var taskResponses = tasks.Select(TaskMapper.ToResponse);

        return new PagedResponse<TaskResponse>(taskResponses, page, pageSize, totalRecords);
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string userId)
    {
        _logger?.LogInformation("Creando tarea para usuario {UserId} Title={Title}", userId, request.Title);

        var newTask = new TodoTask
        {
            Title = request.Title,
            Description = request.Description,
            UserId = userId
        };

        var savedTask = await _taskRepository.AddAsync(newTask);

        _logger?.LogInformation("Tarea creada Id={TaskId} UserId={UserId}", savedTask.Id, userId);

        return TaskMapper.ToResponse(savedTask);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string userId)
    {
        var task = await _taskRepository.FindByIdAsync(id);

        if (task == null)
        {
            _logger?.LogWarning("Intento de actualizar tarea inexistente Id={TaskId} por UserId={UserId}", id, userId);
            return null;
        }

        if (task.UserId != userId)
        {
            _logger?.LogWarning("Usuario {UserId} no es dueño de la tarea Id={TaskId}", userId, id);
            return null;
        }

        if (request.Title != null) task.Title = request.Title;
        if (request.Description != null) task.Description = request.Description;
        if (request.Completed != null) task.Completed = request.Completed.Value;

        await _taskRepository.UpdateAsync(task);

        _logger?.LogInformation("Tarea actualizada Id={TaskId} UserId={UserId}", id, userId);

        return TaskMapper.ToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(Guid id, string userId)
    {
        var task = await _taskRepository.FindByIdAsync(id);

        if (task == null)
        {
            _logger?.LogWarning("Intento de eliminar tarea inexistente Id={TaskId} por UserId={UserId}", id, userId);
            return false;
        }

        if (task.UserId != userId)
        {
            _logger?.LogWarning("Usuario {UserId} no es dueño de la tarea Id={TaskId} (eliminar)", userId, id);
            return false;
        }

        await _taskRepository.DeleteAsync(task);

        _logger?.LogInformation("Tarea eliminada Id={TaskId} UserId={UserId}", id, userId);

        return true;
    }

    public Task<ImportResponse> ImportExternalTasksAsync(string currentUserId) =>
        _taskImportService.ImportAsync(currentUserId);
}