using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Application.Services;
using ITRockChallenge.Infrastructure.Data;
using ITRockChallenge.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace ITRockChallenge.Tests
{
    public class TaskEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public TaskEndpointsTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetTasks_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/tasks");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateTask_WithValidRequestAndToken_ReturnsCreated()
        {
            // Arrange
            var mockTaskService = new Mock<ITaskService>();
            var expectedResponse = new TaskResponse(Guid.NewGuid(), "New Task", "Desc", false, DateTime.UtcNow);

            mockTaskService.Setup(s => s.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockTaskService.Object);
                });
            });

            var client = factory.CreateClient();

            // Get token
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "admin", Password = "password123" });
            var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult?.Token);

            var request = new { Title = "New Task", Description = "Desc" };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/tasks", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTask_WithValidRequestAndToken_ReturnsOk()
        {
            // Arrange
            var mockTaskService = new Mock<ITaskService>();
            var taskId = Guid.NewGuid();
            var expectedResponse = new TaskResponse(taskId, "Updated Title", "Updated Desc", true, DateTime.UtcNow);

            // Simula que el servicio actualiza la tarea correctamente
            mockTaskService.Setup(s => s.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateTaskRequest>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockTaskService.Object);
                });
            });

            var client = factory.CreateClient();

            // Login con las credenciales que ya te funcionan
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "admin", Password = "password123" });
            var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult?.Token);

            var request = new { Title = "Updated Title", Description = "Updated Desc", IsCompleted = true };

            // Act
            var response = await client.PatchAsJsonAsync($"/api/v1/tasks/{taskId}", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedResult = await response.Content.ReadFromJsonAsync<TaskResponse>();
            Assert.NotNull(updatedResult);
            Assert.Equal("Updated Title", updatedResult.Title);
            Assert.True(updatedResult.Completed);
        }

        [Fact]
        public async Task DeleteTask_WithValidIdAndToken_ReturnsNoContent()
        {
            // Arrange
            var mockTaskService = new Mock<ITaskService>();
            var taskId = Guid.NewGuid();

            // Simula que el servicio elimina la tarea con éxito (retorna true o completa la tarea)
            mockTaskService.Setup(s => s.DeleteTaskAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockTaskService.Object);
                });
            });

            var client = factory.CreateClient();

            // Login
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "admin", Password = "password123" });
            var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult?.Token);

            // Act
            var response = await client.DeleteAsync($"/api/v1/tasks/{taskId}");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task ImportExternalTasksAsync_ShouldFilterUserId1AndTakeMaximum5()
        {
            // ARRANGE
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"Test_Import_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            // Crear lista ficticia que simule lo que devuelve la API externa
            // 7 tareas del UserId 1 -  probar el límite de 5 y 2 de otros usuarios para probar el filtro
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

            // Mockear cliente externo para que devuelva la lista falsa
            var mockExternalClient = new Mock<IJsonPlaceholderClient>();
            mockExternalClient
                .Setup(client => client.GetTodosAsync())
                .ReturnsAsync(fakeExternalTasks);

            // Instanciar servicio inyectándole los mocks
            var currentUserId = "user-123-facundo";
            var taskService = new TaskService(new EfTaskRepository(context), mockExternalClient.Object);

            // ACT
            var result = await taskService.ImportExternalTasksAsync(currentUserId);

            // ASSERT
            // Validar que la respuesta DTO tenga 5 importadas
            Assert.NotNull(result);
            Assert.Equal(5, result.ImportedCount);
            Assert.Equal(5, result.Tasks.Count());
            Assert.Contains(result.Tasks, t => t.Title == "Tarea 1");
            Assert.Contains(result.Tasks, t => t.Title == "Tarea 5");
            Assert.DoesNotContain(result.Tasks, t => t.Title == "Tarea 6");
            Assert.DoesNotContain(result.Tasks, t => t.Title == "Tarea de otro user");

            var tasksInDb = await context.Tasks.Where(t => t.UserId == currentUserId).ToListAsync();
            Assert.Equal(5, tasksInDb.Count);

            // Verificar que mapeó bien la descripción concatenando el External ID
            Assert.StartsWith("Importada externamente (External ID: 101)", tasksInDb.First().Description);
        }

        [Fact]
        public async Task GetTasks_WhenUserIsUnauthenticated_Returns401Unauthorized()
        {
            // ARRANGE
            // Cliente sin Token para simular usuario no autenticado
            var client = _factory.CreateClient();

            // ACT
            var response = await client.GetAsync("/api/v1/tasks");

            // ASSERT
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateTask_WhenUserIsUnauthenticated_Returns401Unauthorized()
        {
            // ARRANGE
            var request = new { Title = "Nueva Tarea", Description = "Desc" };

            // Cliente sin Token para simular que no está autenticado
            var client = _factory.CreateClient();

            // ACT
            var response = await client.PostAsJsonAsync("/api/v1/tasks", request);

            // ASSERT
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateTask_WithValidRequestAndToken_Returns201CreatedWithLocation()
        {
            // ARRANGE
            var mockTaskService = new Mock<ITaskService>();
            var newTaskId = Guid.NewGuid();
            var expectedResponse = new TaskResponse(newTaskId, "Tarea HTTP", "Descripcion HTTP", false, DateTime.UtcNow);

            // mock para que devuelva la respuesta exitosa
            mockTaskService
                .Setup(s => s.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockTaskService.Object);
                });
            });

            var client = factory.CreateClient();

            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "admin", Password = "password123" });
            var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult?.Token);

            var request = new { Title = "Tarea HTTP", Description = "Descripcion HTTP" };

            // ACT
            var response = await client.PostAsJsonAsync("/api/v1/tasks", request);

            // ASSERT
            // Validar que devuelva 201
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Contains($"/tasks/{newTaskId}", response.Headers.Location.ToString());

            // Validar el cuerpo de la respuesta
            var taskResult = await response.Content.ReadFromJsonAsync<TaskResponse>();
            Assert.NotNull(taskResult);
            Assert.Equal("Tarea HTTP", taskResult.Title);
        }

        [Fact]
        public async Task UpdateTask_WhenUserIsUnauthenticated_Returns401Unauthorized()
        {
            // ARRANGE
            var taskId = Guid.NewGuid();
            var request = new UpdateTaskRequest("Nuevo Titulo", "Nueva Desc", true);

            // Cliente sin Token para forzar la falta de autenticación
            var client = _factory.CreateClient();

            // ACT
            var response = await client.PatchAsJsonAsync($"/api/v1/tasks/{taskId}", request);

            // ASSERT
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTask_WhenServiceReturnsNull_Returns404NotFoundWithCustomMessage()
        {
            // ARRANGE
            var mockTaskService = new Mock<ITaskService>();
            var taskId = Guid.NewGuid();
            var request = new UpdateTaskRequest("Nuevo Titulo", "Nueva Desc", true);

            // Forzar al servicio a devolver null para simular que la tarea no existe o hay falta de permisos
            mockTaskService
                .Setup(s => s.UpdateTaskAsync(taskId, It.IsAny<UpdateTaskRequest>(), It.IsAny<string>()))
                .ReturnsAsync((TaskResponse?)null);

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockTaskService.Object);
                });
            });

            var client = factory.CreateClient();

            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "admin", Password = "password123" });
            var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult?.Token);

            // ACT
            var response = await client.PatchAsJsonAsync($"/api/v1/tasks/{taskId}", request);

            // ASSERT
            // Verificar que devuelva 404
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var jsonResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            Assert.NotNull(jsonResponse);
            Assert.True(jsonResponse.ContainsKey("message"));
            Assert.Equal("Tarea no encontrada o no tiene permisos.", jsonResponse["message"]);
        }

        [Fact]
        public async Task DeleteTask_WhenUserIsUnauthenticated_Returns401Unauthorized()
        {
            // ARRANGE
            var taskId = Guid.NewGuid();

            // Crear cliente sin inyectarle ningún Bearer Token de autenticación
            var client = _factory.CreateClient();

            // ACT
            var response = await client.DeleteAsync($"/api/v1/tasks/{taskId}");

            // ASSERT
            // userId es nulo/vacío y retornar 401
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_WhenServiceReturnsFalse_Returns404NotFoundWithCustomMessage()
        {
            // ARRANGE
            var mockTaskService = new Mock<ITaskService>();
            var taskId = Guid.NewGuid();

            // Forzar servicio a devolver false
            mockTaskService
                .Setup(s => s.DeleteTaskAsync(taskId, It.IsAny<string>()))
                .ReturnsAsync(false);

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockTaskService.Object);
                });
            });

            var client = factory.CreateClient();

            // Login con credenciales válidas para pasar la primera validación del userId
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "admin", Password = "password123" });
            var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult?.Token);

            // ACT
            var response = await client.DeleteAsync($"/api/v1/tasks/{taskId}");

            // ASSERT
            // Verificar que devuelva el código 404
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // Validar que el JSON contenga el texto exacto
            var jsonResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            Assert.NotNull(jsonResponse);
            Assert.True(jsonResponse.ContainsKey("message"));
            Assert.Equal("Tarea no encontrada o no tiene permisos.", jsonResponse["message"]);
        }

        [Fact]
        public async Task ImportTasks_WhenExternalServiceThrowsException_Returns502BadGatewayWithControlledError()
        {
            // ARRANGE
            var mockTaskService = new Mock<ITaskService>();

            // Forzar a lanzar una HttpRequestException para simular que Polly falló
            mockTaskService
                .Setup(s => s.ImportExternalTasksAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Error de conexión simulado tras reintentos de Polly."));

            // Configurar la factoría inyectando el servicio mockeado que va a fallar
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockTaskService.Object);
                });
            });

            var client = factory.CreateClient();

            // Login con credenciales válidas actuales para saltear el Unauthorized
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "admin", Password = "password123" });
            var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult?.Token);

            // ACT
            var response = await client.PostAsync("/api/v1/tasks/import", null);

            // ASSERT
            // Verificar que el código de estado sea el 502 Bad Gateway configurado en el catch
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problemDetails);
            Assert.Equal(502, problemDetails.Status);
            Assert.Equal("El servicio externo no responde tras múltiples reintentos. Intente más tarde.", problemDetails.Detail);
        }
    }
}
