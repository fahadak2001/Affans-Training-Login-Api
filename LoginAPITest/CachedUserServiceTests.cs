using Moq;
using LoginAPI.Services;
using LoginAPI.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace LoginAPITest
{
    public class CachedUserServiceTests
    {
        private readonly Mock<IUserService> _mockDecoratedUserService;
        private readonly Mock<IDistributedCache> _mockDistributedCache;
        private readonly CachedUserService _cachedUserService;

        public CachedUserServiceTests()
        {
            _mockDecoratedUserService = new Mock<IUserService>();
            _mockDistributedCache = new Mock<IDistributedCache>();
            _cachedUserService = new CachedUserService(
                _mockDecoratedUserService.Object,
                _mockDistributedCache.Object
            );
        }

        [Fact]
        public void GetUserByEmail_DelegatesToDecoratedService()
        {
            var user = new User { Email = "test@example.com", UserName = "testuser" };
            _mockDecoratedUserService.Setup(s => s.GetUserByEmail("test@example.com")).Returns(user);

            var result = _cachedUserService.GetUserByEmail("test@example.com");

            Assert.Equal(user, result);
            _mockDecoratedUserService.Verify(s => s.GetUserByEmail("test@example.com"), Times.Once);
            _mockDistributedCache.Verify(c => c.Get(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetUserByUserName_DelegatesToDecoratedService()
        {
            var user = new User { Email = "test@example.com", UserName = "testuser" };
            _mockDecoratedUserService.Setup(s => s.GetUserByUserName("testuser")).Returns(user);

            var result = _cachedUserService.GetUserByUserName("testuser");

            Assert.Equal(user, result);
            _mockDecoratedUserService.Verify(s => s.GetUserByUserName("testuser"), Times.Once);
            _mockDistributedCache.Verify(c => c.Get(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetAllUsers_CacheHit_ReturnsCachedUsers()
        {
            var cachedUsers = new List<User>
            {
                new User { Email = "cached1@example.com", UserName = "cacheduser1" },
                new User { Email = "cached2@example.com", UserName = "cacheduser2" }
            };
            var cachedUsersJson = JsonSerializer.Serialize(cachedUsers);
            var cachedUsersBytes = Encoding.UTF8.GetBytes(cachedUsersJson);

            _mockDistributedCache.Setup(c => c.Get("users:all")).Returns(cachedUsersBytes);

            var result = _cachedUserService.GetAllUsers();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("cached1@example.com", result.First().Email);
            _mockDistributedCache.Verify(c => c.Get("users:all"), Times.Once);
            _mockDecoratedUserService.Verify(s => s.GetAllUsers(), Times.Never);
        }

        [Fact]
        public void GetAllUsers_CacheMiss_FetchesFromDecoratedServiceAndCaches()
        {
            var usersFromService = new List<User>
            {
                new User { Email = "service1@example.com", UserName = "serviceuser1" },
                new User { Email = "service2@example.com", UserName = "serviceuser2" }
            };

            _mockDistributedCache.Setup(c => c.Get("users:all")).Returns((byte[])null);
            _mockDecoratedUserService.Setup(s => s.GetAllUsers()).Returns(usersFromService);

            var result = _cachedUserService.GetAllUsers();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("service1@example.com", result.First().Email);
            _mockDistributedCache.Verify(c => c.Get("users:all"), Times.Once);
            _mockDecoratedUserService.Verify(s => s.GetAllUsers(), Times.Once);
            _mockDistributedCache.Verify(c => c.Set("users:all", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public void GetAllUsers_CacheMiss_DecoratedServiceReturnsNull_DoesNotCache()
        {
            _mockDistributedCache.Setup(c => c.Get("users:all")).Returns((byte[])null);
            _mockDecoratedUserService.Setup(s => s.GetAllUsers()).Returns((IEnumerable<User>)null);

            var result = _cachedUserService.GetAllUsers();

            Assert.Null(result);
            _mockDistributedCache.Verify(c => c.Get("users:all"), Times.Once);
            _mockDecoratedUserService.Verify(s => s.GetAllUsers(), Times.Once);
            _mockDistributedCache.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        }

        [Fact]
        public void CreateUser_DelegatesToDecoratedServiceAndClearsCache()
        {
            var newUser = new User { Email = "new@example.com", UserName = "newuser" };

            _cachedUserService.CreateUser(newUser);

            _mockDecoratedUserService.Verify(s => s.CreateUser(newUser), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("users:all"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove($"user:email:{newUser.Email}"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove($"user:username:{newUser.UserName}"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("user:admin@admin.com"), Times.Once);
        }

        [Fact]
        public void UpdateUser_DelegatesToDecoratedServiceAndClearsCache()
        {
            var updatedUser = new User { Email = "update@example.com", UserName = "updateduser" };

            _cachedUserService.UpdateUser(updatedUser);

            _mockDecoratedUserService.Verify(s => s.UpdateUser(updatedUser), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("users:all"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove($"user:email:{updatedUser.Email}"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove($"user:username:{updatedUser.UserName}"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("user:admin@admin.com"), Times.Once);
        }

        [Fact]
        public void DeleteUser_DelegatesToDecoratedServiceAndClearsCache()
        {
            string emailToDelete = "delete@example.com";

            _cachedUserService.DeleteUser(emailToDelete);

            _mockDecoratedUserService.Verify(s => s.DeleteUser(emailToDelete), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("users:all"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove($"user:email:{emailToDelete}"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("user:admin@admin.com"), Times.Once);
        }        

        [Fact]
        public void Authenticate_DelegatesToDecoratedService()
        {
            var user = new User { Email = "auth@example.com", UserName = "authuser", Password = "password" };
            _mockDecoratedUserService.Setup(s => s.Authenticate("authuser", "password")).Returns(user);

            var result = _cachedUserService.Authenticate("authuser", "password");

            Assert.Equal(user, result);
            _mockDecoratedUserService.Verify(s => s.Authenticate("authuser", "password"), Times.Once);
        }

        [Fact]
        public void GenerateJwtToken_DelegatesToDecoratedService()
        {
            var user = new User { Email = "jwt@example.com", UserName = "jwtuser" };
            string expectedToken = "some.jwt.token";
            _mockDecoratedUserService.Setup(s => s.GenerateJwtToken(user)).Returns(expectedToken);

            var result = _cachedUserService.GenerateJwtToken(user);

            Assert.Equal(expectedToken, result);
            _mockDecoratedUserService.Verify(s => s.GenerateJwtToken(user), Times.Once);
        }
    }
}