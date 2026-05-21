using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Application.Services;
using ITRockChallenge.Infrastructure.Data;
using ITRockChallenge.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ITRockChallenge.Tests;

public class TaskImportServiceTests
{
    [Fact]
    public async Task ImportAsync_WhenNoTasksMatchUserId1_ReturnsEmptyImportResponse()
    {
        using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"Test_Import_Empty_{Guid.NewGuid()}")
                .Options);

        var mockExternalClient = new Mock<IJsonPlaceholderClient>();
        mockExternalClient
            .Setup(client => client.GetTodosAsync())
            .ReturnsAsync(new List<ExternalTaskDto>
            {
                new(2, 201, "Tarea de otro usuario", false),
                new(3, 202, "Otra tarea ajena", true)
            });

        var importService = new TaskImportService(
            new EfTaskRepository(context), mockExternalClient.Object);

        var result = await importService.ImportAsync("user-facundo");

        Assert.Equal(0, result.ImportedCount);
        Assert.Empty(result.Tasks);
        Assert.Empty(await context.Tasks.ToListAsync());
    }

    [Fact]
    public async Task ImportAsync_ShouldFilterUserId1AndTakeMaximum5()
    {
        using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"Test_Import_{Guid.NewGuid()}")
                .Options);

        var fakeExternalTasks = new List<ExternalTaskDto>
        {
            new(1, 101, "Tarea 1", false),
            new(1, 102, "Tarea 2", true),
            new(1, 103, "Tarea 3", false),
            new(1, 104, "Tarea 4", false),
            new(1, 105, "Tarea 5", true),
            new(1, 106, "Tarea 6", false),
            new(1, 107, "Tarea 7", false),
            new(2, 108, "Tarea de otro user", false),
            new(3, 109, "Tarea de otro user 2", false)
        };

        var mockExternalClient = new Mock<IJsonPlaceholderClient>();
        mockExternalClient
            .Setup(client => client.GetTodosAsync())
            .ReturnsAsync(fakeExternalTasks);

        var currentUserId = "user-123-facundo";
        var importService = new TaskImportService(
            new EfTaskRepository(context), mockExternalClient.Object);

        var result = await importService.ImportAsync(currentUserId);

        Assert.Equal(5, result.ImportedCount);
        Assert.Equal(5, result.Tasks.Count());
        Assert.Contains(result.Tasks, t => t.Title == "Tarea 1");
        Assert.Contains(result.Tasks, t => t.Title == "Tarea 5");
        Assert.DoesNotContain(result.Tasks, t => t.Title == "Tarea 6");
        Assert.DoesNotContain(result.Tasks, t => t.Title == "Tarea de otro user");

        var tasksInDb = await context.Tasks.Where(t => t.UserId == currentUserId).ToListAsync();
        Assert.Equal(5, tasksInDb.Count);
        Assert.StartsWith("Importada externamente (External ID: 101)", tasksInDb.First().Description);
    }
}
