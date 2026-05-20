using ITRockChallenge.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ITRockChallenge.Tests
{
    public class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthEndpointsTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkAndToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new { Username = "admin", Password = "password123" };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.NotNull(authResponse);
            Assert.False(string.IsNullOrEmpty(authResponse.Token));
            Assert.Equal("admin", authResponse.Username);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new { Username = "admin", Password = "wrongpassword" };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
