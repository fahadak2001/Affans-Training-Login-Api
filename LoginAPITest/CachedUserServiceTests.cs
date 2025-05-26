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
        private readonly User _testUser;

        public CachedUserServiceTests()
        {
            _mockDecoratedUserService = new Mock<IUserService>();
            _mockDistributedCache = new Mock<IDistributedCache>();
            _cachedUserService = new CachedUserService(
                _mockDecoratedUserService.Object,
                _mockDistributedCache.Object
            );

            _testUser = new User { Email = "test@example.com", UserName = "testuser", Password = "password123", Role = "User" };
        }

        [Fact]
        public void GetUserByEmail_DelegatesToDecoratedService_ReturnsUser()
        {
            _mockDecoratedUserService.Setup(s => s.GetUserByEmail(_testUser.Email)).Returns(_testUser);

            var result = _cachedUserService.GetUserByEmail(_testUser.Email);

            Assert.Equal(_testUser, result);
            _mockDecoratedUserService.Verify(s => s.GetUserByEmail(_testUser.Email), Times.Once);
            _mockDistributedCache.Verify(c => c.Get(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetUserByUserName_DelegatesToDecoratedService_ReturnsUser()
        {
            _mockDecoratedUserService.Setup(s => s.GetUserByUserName(_testUser.UserName)).Returns(_testUser);

            var result = _cachedUserService.GetUserByUserName(_testUser.UserName);

            Assert.Equal(_testUser, result);
            _mockDecoratedUserService.Verify(s => s.GetUserByUserName(_testUser.UserName), Times.Once);
            _mockDistributedCache.Verify(c => c.Get(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetAllUsers_CacheHit_ReturnsCachedUsers()
        {
            var cachedUsers = new List<User>
            {
                _testUser,
                new User { Email = "cached2@example.com", UserName = "cacheduser2" }
            };
            var cachedUsersJson = JsonSerializer.Serialize(cachedUsers);
            var cachedUsersBytes = Encoding.UTF8.GetBytes(cachedUsersJson);

            _mockDistributedCache.Setup(c => c.Get("users:all")).Returns(cachedUsersBytes);

            var result = _cachedUserService.GetAllUsers();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal(_testUser.Email, result.First().Email);
            _mockDistributedCache.Verify(c => c.Get("users:all"), Times.Once);
            _mockDecoratedUserService.Verify(s => s.GetAllUsers(), Times.Never);
        }

        [Fact]
        public void GetAllUsers_CacheMiss_FetchesFromDecoratedServiceAndCaches()
        {
            var usersFromService = new List<User>
            {
                _testUser,
                new User { Email = "service2@example.com", UserName = "serviceuser2" }
            };

            _mockDistributedCache.Setup(c => c.Get("users:all")).Returns((byte[])null);
            _mockDecoratedUserService.Setup(s => s.GetAllUsers()).Returns(usersFromService);

            var result = _cachedUserService.GetAllUsers();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal(_testUser.Email, result.First().Email);
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
        public void CreateUser_DelegatesToDecoratedServiceAndClearsCache_UserCreated()
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
        public void UpdateUser_DelegatesToDecoratedServiceAndClearsCache_UserUpdated()
        {
            var updatedUser = new User { Email = _testUser.Email, UserName = _testUser.UserName, Role = "UpdatedRole" };

            _cachedUserService.UpdateUser(updatedUser);

            _mockDecoratedUserService.Verify(s => s.UpdateUser(updatedUser), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("users:all"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove($"user:email:{updatedUser.Email}"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove($"user:username:{updatedUser.UserName}"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("user:admin@admin.com"), Times.Once);
        }

        [Fact]
        public void DeleteUser_DelegatesToDecoratedServiceAndClearsCache_UserDeleted()
        {
            string emailToDelete = _testUser.Email;

            _cachedUserService.DeleteUser(emailToDelete);

            _mockDecoratedUserService.Verify(s => s.DeleteUser(emailToDelete), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("users:all"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove($"user:email:{emailToDelete}"), Times.Once);
            _mockDistributedCache.Verify(c => c.Remove("user:admin@admin.com"), Times.Once);
        }
    }
}