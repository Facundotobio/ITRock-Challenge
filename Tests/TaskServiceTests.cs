using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Application.Services;
using ITRockChallenge.Domain;
using ITRockChallenge.Infrastructure.Data;
using ITRockChallenge.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ITRockChallenge.Tests
{
    public class TaskServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateTaskAsync_CreatesAndReturnsTask()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockExternalClient = new Mock<IJsonPlaceholderClient>();
            var service = new TaskService(new EfTaskRepository(dbContext), mockExternalClient.Object);
            
            var request = new CreateTaskRequest("Test Title", "Test Description");
            var userId = "1";

            // Act
            var response = await service.CreateTaskAsync(request, userId);

            // Assert
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
            // Arrange
            var dbContext = GetInMemoryDbContext();
            dbContext.Tasks.AddRange(
                new TodoTask { Title = "Task 1", Description = "Desc 1", UserId = "1", Completed = true },
                new TodoTask { Title = "Task 2", Description = "Desc 2", UserId = "1", Completed = false },
                new TodoTask { Title = "Task 3", Description = "Desc 3", UserId = "2", Completed = false }
            );
            await dbContext.SaveChangesAsync();

            var mockExternalClient = new Mock<IJsonPlaceholderClient>();
            var service = new TaskService(new EfTaskRepository(dbContext), mockExternalClient.Object);

            // Act
            var result = await service.GetTasksByUserIdAsync("1", 1, 10, null, null, null, null);

            // Assert
            Assert.Equal(2, result.TotalRecords);
            Assert.Equal(2, result.Data.Count());
            Assert.All(result.Data, t => Assert.NotEqual("Task 3", t.Title));
        }

        [Fact]
        public async Task UpdateTaskAsync_WhenTaskDoesNotExist_ReturnsNull()
        {
            // ARRANGE
            // Crear un contexto en memoria limpio y vacío
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"Test_Update_NotExists_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);
            var mockExternalClient = new Mock<IJsonPlaceholderClient>();

            var taskService = new TaskService(new EfTaskRepository(context), mockExternalClient.Object);

            var nonExistentId = Guid.NewGuid();
            var request = new UpdateTaskRequest("Nuevo Titulo", "Nueva Desc", true);

            // ACT
            var result = await taskService.UpdateTaskAsync(nonExistentId, request, "user-facundo");

            // ASSERT
            // debe retornar null para que el controller tire el 404
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTaskAsync_WhenUserIsNotOwner_ReturnsNullAndDoesNotModify()
        {
            // ARRANGE
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"Test_Update_Ownership_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            // Crear tarea real en la BD que le pertenece al "user-verdadero"
            var taskId = Guid.NewGuid();
            var originalTask = new TodoTask
            {
                Id = taskId,
                Title = "Titulo Original",
                Description = "Desc Original",
                Completed = false,
                UserId = "user-verdadero"
            };
            context.Tasks.Add(originalTask);
            await context.SaveChangesAsync();

            var mockExternalClient = new Mock<IJsonPlaceholderClient>();
            var taskService = new TaskService(new EfTaskRepository(context), mockExternalClient.Object);

            // "user-atacante" intenta modificar la tarea del "user-verdadero"
            var request = new UpdateTaskRequest("Titulo Hackeado", "Desc Hackeada", true);

            // ACT
            var result = await taskService.UpdateTaskAsync(taskId, request, "user-atacante");

            // ASSERT
            // Debe retornar null bloqueando la acción
            Assert.Null(result);

            //  Verificar que en la base de datos no haya cambiado nada
            var taskInDb = await context.Tasks.FindAsync(taskId);
            Assert.NotNull(taskInDb);
            Assert.Equal("Titulo Original", taskInDb.Title);
            Assert.Equal("user-verdadero", taskInDb.UserId);
        }

        [Fact]
        public async Task DeleteTaskAsync_WhenTaskDoesNotExist_ReturnsFalse()
        {
            // ARRANGE
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"Test_Delete_NotExists_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);
            var mockExternalClient = new Mock<IJsonPlaceholderClient>();

            var taskService = new TaskService(new EfTaskRepository(context), mockExternalClient.Object);

            var nonExistentId = Guid.NewGuid();

            // ACT
            var result = await taskService.DeleteTaskAsync(nonExistentId, "user-facundo");

            // ASSERT
            // devolver false para que el controller maneje el 404
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteTaskAsync_WhenUserIsNotOwner_ReturnsFalseAndDoesNotDelete()
        {
            // ARRANGE
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"Test_Delete_Ownership_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            // Crear tarea real que pertenece al "user-verdadero"
            var taskId = Guid.NewGuid();
            var originalTask = new TodoTask
            {
                Id = taskId,
                Title = "Tarea Privada",
                Description = "No borrar",
                Completed = false,
                UserId = "user-verdadero"
            };
            context.Tasks.Add(originalTask);
            await context.SaveChangesAsync();

            var mockExternalClient = new Mock<IJsonPlaceholderClient>();
            var taskService = new TaskService(new EfTaskRepository(context), mockExternalClient.Object);

            // "user-atacante" intenta borrar la tarea ajena
            // ACT
            var result = await taskService.DeleteTaskAsync(taskId, "user-atacante");

            // ASSERT
            // retornar false denegando el borrado
            Assert.False(result);

            // Verificar que la tarea siga existiendo en la base de datos
            var taskInDb = await context.Tasks.FindAsync(taskId);
            Assert.NotNull(taskInDb);
            Assert.Equal("user-verdadero", taskInDb.UserId);
        }

        [Fact]
        public async Task ImportExternalTasksAsync_WhenNoTasksMatchUserId1_ReturnsEmptyImportResponse()
        {
            // ARRANGE
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"Test_Import_Empty_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            // la API externa devuelve tareas, pero ninguna tiene UserId: 1
            var fakeExternalTasks = new List<ExternalTaskDto>
            {
                new(2, 201, "Tarea de otro usuario", false),
                new(3, 202, "Otra tarea ajena", true)
            };

            // Mockear cliente externo
            var mockExternalClient = new Mock<IJsonPlaceholderClient>();
            mockExternalClient
                .Setup(client => client.GetTodosAsync())
                .ReturnsAsync(fakeExternalTasks);

            var currentUserId = "user-facundo";
            var taskService = new TaskService(new EfTaskRepository(context), mockExternalClient.Object);

            // ACT
            var result = await taskService.ImportExternalTasksAsync(currentUserId);

            // ASSERT
            // El DTO debe indicar que se importaron 0 tareas
            Assert.NotNull(result);
            Assert.Equal(0, result.ImportedCount);
            Assert.Empty(result.Tasks);

            // Verificar que la base de datos no guardó nada
            var tasksInDb = await context.Tasks.ToListAsync();
            Assert.Empty(tasksInDb);
        }
    }
}
