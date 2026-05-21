using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Application.Services;
using ITRockChallenge.Domain;
using ITRockChallenge.Infrastructure.Data;
using ITRockChallenge.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ITRockChallenge.Tests;

public class TaskServiceTests
{
    private static ApplicationDbContext GetInMemoryDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

    private static TaskService CreateTaskService(ApplicationDbContext context) =>
        new(new EfTaskRepository(context), Mock.Of<ITaskImportService>());

    [Fact]
    public async Task CreateTaskAsync_CreatesAndReturnsTask()
    {
        var dbContext = GetInMemoryDbContext();
        var service = CreateTaskService(dbContext);

        var request = new CreateTaskRequest("Test Title", "Test Description");
        var userId = "1";

        var response = await service.CreateTaskAsync(request, userId);

        Assert.NotNull(response);
        Assert.Equal("Test Title", response.Title);
        Assert.Equal("Test Description", response.Description);
        Assert.False(response.Completed);

        var dbTask = await dbContext.Tasks.FindAsync(response.Id);
        Assert.NotNull(dbTask);
        Assert.Equal(userId, dbTask.UserId);
    }

    [Fact]
    public async Task GetTasksByUserIdAsync_ReturnsPagedTasks()
    {
        var dbContext = GetInMemoryDbContext();
        dbContext.Tasks.AddRange(
            new TodoTask { Title = "Task 1", Description = "Desc 1", UserId = "1", Completed = true },
            new TodoTask { Title = "Task 2", Description = "Desc 2", UserId = "1", Completed = false },
            new TodoTask { Title = "Task 3", Description = "Desc 3", UserId = "2", Completed = false }
        );
        await dbContext.SaveChangesAsync();

        var service = CreateTaskService(dbContext);

        var result = await service.GetTasksByUserIdAsync("1", 1, 10, null, null, null, null);

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());
        Assert.All(result.Data, t => Assert.NotEqual("Task 3", t.Title));
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenTaskDoesNotExist_ReturnsNull()
    {
        using var context = GetInMemoryDbContext();
        var taskService = CreateTaskService(context);

        var result = await taskService.UpdateTaskAsync(
            Guid.NewGuid(), new UpdateTaskRequest("Nuevo Titulo", "Nueva Desc", true), "user-facundo");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenUserIsNotOwner_ReturnsNullAndDoesNotModify()
    {
        using var context = GetInMemoryDbContext();
        var taskId = Guid.NewGuid();
        context.Tasks.Add(new TodoTask
        {
            Id = taskId,
            Title = "Titulo Original",
            Description = "Desc Original",
            Completed = false,
            UserId = "user-verdadero"
        });
        await context.SaveChangesAsync();

        var taskService = CreateTaskService(context);

        var result = await taskService.UpdateTaskAsync(
            taskId, new UpdateTaskRequest("Titulo Hackeado", "Desc Hackeada", true), "user-atacante");

        Assert.Null(result);

        var taskInDb = await context.Tasks.FindAsync(taskId);
        Assert.NotNull(taskInDb);
        Assert.Equal("Titulo Original", taskInDb.Title);
        Assert.Equal("user-verdadero", taskInDb.UserId);
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenTaskDoesNotExist_ReturnsFalse()
    {
        using var context = GetInMemoryDbContext();
        var taskService = CreateTaskService(context);

        var result = await taskService.DeleteTaskAsync(Guid.NewGuid(), "user-facundo");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenUserIsNotOwner_ReturnsFalseAndDoesNotDelete()
    {
        using var context = GetInMemoryDbContext();
        var taskId = Guid.NewGuid();
        context.Tasks.Add(new TodoTask
        {
            Id = taskId,
            Title = "Tarea Privada",
            Description = "No borrar",
            Completed = false,
            UserId = "user-verdadero"
        });
        await context.SaveChangesAsync();

        var taskService = CreateTaskService(context);

        var result = await taskService.DeleteTaskAsync(taskId, "user-atacante");

        Assert.False(result);

        var taskInDb = await context.Tasks.FindAsync(taskId);
        Assert.NotNull(taskInDb);
        Assert.Equal("user-verdadero", taskInDb.UserId);
    }
}
