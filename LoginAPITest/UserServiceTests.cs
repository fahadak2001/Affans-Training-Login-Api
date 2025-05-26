using Moq;
using LoginAPI.Services;
using LoginAPI.Repositories;
using LoginAPI.Models;
using Microsoft.Extensions.Configuration;

namespace LoginAPITest
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly UserService _userService;
        private readonly User _testUser;

        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("11111111111111111111111111111111");
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfiguration.Setup(c => c["Jwt:MinutesToExpire"]).Returns("60");

            _userService = new UserService(_mockUserRepository.Object, _mockConfiguration.Object);

            _testUser = new User { Email = "test@example.com", Password = "password123", UserName = "testuser", Role = "User" };
        }

        [Fact]
        public void Authenticate_ValidEmailAndPassword_ReturnsUser()
        {
            _mockUserRepository.Setup(r => r.GetByEmail(_testUser.Email)).Returns(_testUser);

            var result = _userService.Authenticate(_testUser.Email, _testUser.Password);

            Assert.NotNull(result);
            Assert.Equal(_testUser.Email, result.Email);
            Assert.Equal(_testUser.UserName, result.UserName);
            Assert.Equal(_testUser.Password, result.Password);
            _mockUserRepository.Verify(r => r.GetByEmail(_testUser.Email), Times.Once);
            _mockUserRepository.Verify(r => r.GetByUserName(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Authenticate_ValidUserNameAndPassword_ReturnsUser()
        {
            _mockUserRepository.Setup(r => r.GetByEmail(_testUser.UserName)).Returns(_testUser);

            var result = _userService.Authenticate(_testUser.UserName, _testUser.Password);

            Assert.NotNull(result);
            Assert.Equal(_testUser.Email, result.Email);
            _mockUserRepository.Verify(r => r.GetByEmail(_testUser.UserName), Times.Once);
        }

        [Fact]
        public void Authenticate_InvalidPassword_ReturnsNull()
        {
            _mockUserRepository.Setup(r => r.GetByEmail(_testUser.Email)).Returns(_testUser);

            var result = _userService.Authenticate(_testUser.Email, "wrongpassword");

            Assert.Null(result);
            _mockUserRepository.Verify(r => r.GetByEmail(_testUser.Email), Times.Once);
        }

        [Fact]
        public void Authenticate_UserNotFound_ReturnsNull()
        {
            _mockUserRepository.Setup(r => r.GetByEmail(It.IsAny<string>())).Returns((User)null);
            _mockUserRepository.Setup(r => r.GetByUserName(It.IsAny<string>())).Returns((User)null);

            var result = _userService.Authenticate("nonexistent@example.com", "password");

            Assert.Null(result);
            _mockUserRepository.Verify(r => r.GetByEmail("nonexistent@example.com"), Times.Once);
            _mockUserRepository.Verify(r => r.GetByUserName("nonexistent@example.com"), Times.Once);
        }

        [Fact]
        public void GenerateJwtToken_MissingJwtKey_ThrowsInvalidOperationException()
        {
            var userForTokenTest = new User { Email = "token@example.com", UserName = "tokentestuser", Role = "User" };
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns((string)null);
            var serviceWithMissingKey = new UserService(_mockUserRepository.Object, _mockConfiguration.Object);

            var exception = Assert.Throws<InvalidOperationException>(() => serviceWithMissingKey.GenerateJwtToken(userForTokenTest));
            Assert.Equal("JWT key is not configured.", exception.Message);

            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("11111111111111111111111111111111");
        }

        [Fact]
        public void GetUserByEmail_UserExists_ReturnsUser()
        {
            _mockUserRepository.Setup(r => r.GetByEmail(_testUser.Email)).Returns(_testUser);

            var result = _userService.GetUserByEmail(_testUser.Email);

            Assert.NotNull(result);
            Assert.Equal(_testUser.Email, result.Email);
            _mockUserRepository.Verify(r => r.GetByEmail(_testUser.Email), Times.Once);
        }

        [Fact]
        public void GetUserByEmail_UserDoesNotExist_ReturnsNull()
        {
            _mockUserRepository.Setup(r => r.GetByEmail(It.IsAny<string>())).Returns((User)null);

            var result = _userService.GetUserByEmail("nonexistent@example.com");

            Assert.Null(result);
            _mockUserRepository.Verify(r => r.GetByEmail("nonexistent@example.com"), Times.Once);
        }

        [Fact]
        public void GetUserByUserName_UserExists_ReturnsUser()
        {
            _mockUserRepository.Setup(r => r.GetByUserName(_testUser.UserName)).Returns(_testUser);

            var result = _userService.GetUserByUserName(_testUser.UserName);

            Assert.NotNull(result);
            Assert.Equal(_testUser.UserName, result.UserName);
            _mockUserRepository.Verify(r => r.GetByUserName(_testUser.UserName), Times.Once);
        }

        [Fact]
        public void GetUserByUserName_UserDoesNotExist_ReturnsNull()
        {
            _mockUserRepository.Setup(r => r.GetByUserName(It.IsAny<string>())).Returns((User)null);

            var result = _userService.GetUserByUserName("nonexistentuser");

            Assert.Null(result);
            _mockUserRepository.Verify(r => r.GetByUserName("nonexistentuser"), Times.Once);
        }

        [Fact]
        public void GetAllUsers_AsLongAsTableExists_ReturnsAllUsers()
        {
            var users = new List<User>
            {
                _testUser,
                new User { Email = "admin@example.com", UserName = "admin", Role = "Admin" }
            };
            _mockUserRepository.Setup(r => r.GetAll()).Returns(users);

            var result = _userService.GetAllUsers();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockUserRepository.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void GetAllUsers_NoUsers_ReturnsEmptyList()
        {
            _mockUserRepository.Setup(r => r.GetAll()).Returns(new List<User>());

            var result = _userService.GetAllUsers();

            Assert.NotNull(result);
            Assert.Empty(result);
            _mockUserRepository.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void CreateUser_FirstUser_AssignsAdminRole()
        {
            _mockUserRepository.Setup(r => r.GetAll()).Returns(new List<User>());
            var newUser = new User { Email = "newuser@example.com", UserName = "newuser", Password = "password", Role = "Admin" };

            _userService.CreateUser(newUser);

            Assert.Equal("Admin", newUser.Role);
            _mockUserRepository.Verify(r => r.Create(newUser), Times.Once);
        }

        [Fact]
        public void CreateUser_ExistingAdmin_AssignsUserRole()
        {
            var existingUsers = new List<User> { _testUser };
            _mockUserRepository.Setup(r => r.GetAll()).Returns(existingUsers);
            var newUser = new User { Email = "anotheruser@example.com", UserName = "anotheruser", Password = "password", Role = "User" };

            _userService.CreateUser(newUser);

            Assert.Equal("User", newUser.Role);
            _mockUserRepository.Verify(r => r.Create(newUser), Times.Once);
        }

        [Fact]
        public void UpdateUser_CallsRepositoryUpdate_UserUpdated()
        {
            var userToUpdate = new User { Email = _testUser.Email, UserName = _testUser.UserName, Password = "newpass", Role = _testUser.Role };
            _userService.UpdateUser(userToUpdate);

            _mockUserRepository.Verify(r => r.Update(userToUpdate), Times.Once);
        }

        [Fact]
        public void DeleteUser_CallsRepositoryDelete_UserDeleted()
        {
            _userService.DeleteUser(_testUser.Email);

            _mockUserRepository.Verify(r => r.Delete(_testUser.Email), Times.Once);
        }
    }
}