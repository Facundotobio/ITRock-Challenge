using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Domain;
using ITRockChallenge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ITRockChallenge.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJsonPlaceholderClient _externalClient;

        public TaskService(ApplicationDbContext context, IJsonPlaceholderClient externalClient)
        {
            _context = context;
            _externalClient = externalClient;
        }

        public async Task<IEnumerable<TaskResponse>> GetTasksByUserIdAsync(string userId)
        {
            return await _context.Tasks
                .Where(t => t.UserId == userId)
                .Select(t => new TaskResponse(t.Id, t.Title, t.Description, t.Completed, t.CreatedAt))
                .ToListAsync();
        }

        public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string userId)
        {
            var newTask = new TodoTask
            {
                Title = request.Title,
                Description = request.Description,
                UserId = userId
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            return new TaskResponse(newTask.Id, newTask.Title, newTask.Description, newTask.Completed, newTask.CreatedAt);
        }

        public async Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string userId)
        {
            var task = await _context.Tasks.FindAsync(id);

            // Si no existe, devolvemos null para que el endpoint maneje el 404
            if (task == null) return null;

            // Validamos propiedad (Ownership) antes de modificar nada
            if (task.UserId != userId) return null;

            // Actualización parcial (si vienen nulos en el request, mantenemos el valor actual)
            if (request.Title != null) task.Title = request.Title;
            if (request.Description != null) task.Description = request.Description;
            if (request.Completed != null) task.Completed = request.Completed.Value;

            await _context.SaveChangesAsync();

            return new TaskResponse(task.Id, task.Title, task.Description, task.Completed, task.CreatedAt);
        }

        public async Task<bool> DeleteTaskAsync(Guid id, string userId)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null) return false;
            if (task.UserId != userId) return false; // Protección de datos del usuario

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ImportResponse> ImportExternalTasksAsync(string currentUserId)
        {
            // Llamar a la API externa - Polly intentará ejecutar esto 3 veces de forma invisible si el servidor externo tira 5xx o timeout
            var externalTasks = await _externalClient.GetTodosAsync();

            // Filtrar y tomar las primeras 5 tareas con userId: 1
            var filteredTasks = externalTasks
                .Where(t => t.UserId == 1)
                .Take(5)
                .ToList();

            if (!filteredTasks.Any())
            {
                return new ImportResponse(0, Enumerable.Empty<TaskResponse>());
            }

            var importedTasks = new List<TodoTask>();

            // Mapear y preparar para guardar en la BD
            foreach (var extTask in filteredTasks)
            {
                var newTask = new TodoTask
                {
                    Title = extTask.Title,
                    Description = $"Importada externamente (External ID: {extTask.Id})",
                    Completed = extTask.Completed,
                    UserId = currentUserId
                };

                importedTasks.Add(newTask);
            }

            // Guardar en lote en PostgreSQL
            _context.Tasks.AddRange(importedTasks);
            await _context.SaveChangesAsync();

            // Retornar DTO de respuesta estructurado con el conteo y listado
            var taskResponses = importedTasks.Select(t =>
                new TaskResponse(t.Id, t.Title, t.Description, t.Completed, t.CreatedAt));

            return new ImportResponse(importedTasks.Count, taskResponses);
        }
    }
}
