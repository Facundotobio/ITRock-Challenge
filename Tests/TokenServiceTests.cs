using ITRockChallenge.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace ITRockChallenge.Tests
{
    public class TokenServiceTests
    {
        [Fact]
        public void GenerateToken_ReturnsValidJwtToken()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();
            
            mockSection.Setup(x => x["Key"]).Returns("ThisIsAVerySecretKeyForJwtAuthentication123!");
            mockSection.Setup(x => x["Issuer"]).Returns("ITRockIssuer");
            mockSection.Setup(x => x["Audience"]).Returns("ITRockAudience");
            mockSection.Setup(x => x["ExpiryInMinutes"]).Returns("60");

            mockConfig.Setup(x => x.GetSection("Jwt")).Returns(mockSection.Object);

            var tokenService = new TokenService(mockConfig.Object);

            // Act
            var token = tokenService.GenerateToken("admin", "1");

            // Assert
            Assert.False(string.IsNullOrEmpty(token));
            
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal("ITRockIssuer", jwtToken.Issuer);
            Assert.Equal("ITRockAudience", jwtToken.Audiences.First());
            Assert.Contains(jwtToken.Claims, c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier && c.Value == "1");
            Assert.Contains(jwtToken.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Name && c.Value == "admin");
        }
    }
}
