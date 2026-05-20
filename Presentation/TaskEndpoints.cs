using Asp.Versioning;
using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using System.Security.Claims;

namespace ITRockChallenge.Presentation;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/tasks")
            .WithApiVersionSet(apiVersionSet)
            .RequireAuthorization();

        // GET /tasks
        group.MapGet("/", async (ITaskService taskService, HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var tasks = await taskService.GetTasksByUserIdAsync(userId);
            return Results.Ok(tasks);
        })
        .WithName("GetTasks");

        // POST /tasks
        group.MapPost("/", async (CreateTaskRequest request, ITaskService taskService, HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var taskResponse = await taskService.CreateTaskAsync(request, userId);
            return Results.Created($"/tasks/{taskResponse.Id}", taskResponse);
        })
        .WithName("CreateTask")
        .AddEndpointFilter<ValidationFilter<CreateTaskRequest>>();

        // PATCH /tasks/{id}
        group.MapPatch("/{id:guid}", async (Guid id, UpdateTaskRequest request, ITaskService taskService, HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var updatedTask = await taskService.UpdateTaskAsync(id, request, userId);

            if (updatedTask == null)
            {
                return Results.Json(new { message = "Tarea no encontrada o no tiene permisos." }, statusCode: 404);
            }

            return Results.Ok(updatedTask);
        })
        .WithName("UpdateTask")
        .AddEndpointFilter<ValidationFilter<UpdateTaskRequest>>();

        // DELETE /tasks/{id}
        group.MapDelete("/{id:guid}", async (Guid id, ITaskService taskService, HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var success = await taskService.DeleteTaskAsync(id, userId);

            if (!success)
            {
                return Results.Json(new { message = "Tarea no encontrada o no tiene permisos." }, statusCode: 404);
            }

            return Results.NoContent();
        })
        .WithName("DeleteTask");

        // POST /tasks/import
        group.MapPost("/import", async (ITaskService taskService, HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var result = await taskService.ImportExternalTasksAsync(userId);
                return Results.Ok(result);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                // Si Polly agotó los reintentos, devolvemos un error semántico controlado
                return Results.Json(new
                {
                    error = "El servicio externo no responde tras múltiples reintentos. Intente más tarde."
                }, statusCode: 502);
            }
        })
        .WithName("ImportTasks");
    }
}