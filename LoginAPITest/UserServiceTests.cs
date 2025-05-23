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

        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("11111111111111111111111111111111");
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfiguration.Setup(c => c["Jwt:MinutesToExpire"]).Returns("60");

            _userService = new UserService(_mockUserRepository.Object, _mockConfiguration.Object);
        }

        [Fact]
        public void Authenticate_ValidEmailAndPassword_ReturnsUser()
        {
            var user = new User { Email = "test@example.com", Password = "password123", UserName = "testuser", Role = "User" };
            _mockUserRepository.Setup(r => r.GetByEmail("test@example.com")).Returns(user);

            var result = _userService.Authenticate("test@example.com", "password123");

            Assert.NotNull(result);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.UserName, result.UserName);
            Assert.Equal(user.Password, result.Password);
            _mockUserRepository.Verify(r => r.GetByEmail("test@example.com"), Times.Once);
            _mockUserRepository.Verify(r => r.GetByUserName(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Authenticate_ValidUserNameAndPassword_ReturnsUser()
        {
            var user = new User { Email = "test@example.com", Password = "password123", UserName = "testuser", Role = "User" };
            _mockUserRepository.Setup(r => r.GetByEmail("testuser")).Returns((User)null);
            _mockUserRepository.Setup(r => r.GetByUserName("testuser")).Returns(user);

            var result = _userService.Authenticate("testuser", "password123");

            Assert.NotNull(result);
            Assert.Equal(user.Email, result.Email);
            _mockUserRepository.Verify(r => r.GetByEmail("testuser"), Times.Once);
            _mockUserRepository.Verify(r => r.GetByUserName("testuser"), Times.Once);
        }

        [Fact]
        public void Authenticate_InvalidPassword_ReturnsNull()
        {
            var user = new User { Email = "test@example.com", Password = "password123", UserName = "testuser", Role = "User" };
            _mockUserRepository.Setup(r => r.GetByEmail("test@example.com")).Returns(user);

            var result = _userService.Authenticate("test@example.com", "wrongpassword");

            Assert.Null(result);
            _mockUserRepository.Verify(r => r.GetByEmail("test@example.com"), Times.Once);
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
            var user = new User { Email = "test@example.com", UserName = "testuser", Role = "User" };
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns((string)null);
            var serviceWithMissingKey = new UserService(_mockUserRepository.Object, _mockConfiguration.Object);

            var exception = Assert.Throws<InvalidOperationException>(() => serviceWithMissingKey.GenerateJwtToken(user));
            Assert.Equal("JWT key is not configured.", exception.Message);
        }


        [Fact]
        public void GetUserByEmail_UserExists_ReturnsUser()
        {
            var user = new User { Email = "test@example.com", UserName = "testuser", Role = "User" };
            _mockUserRepository.Setup(r => r.GetByEmail("test@example.com")).Returns(user);

            var result = _userService.GetUserByEmail("test@example.com");

            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
            _mockUserRepository.Verify(r => r.GetByEmail("test@example.com"), Times.Once);
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
            var user = new User { Email = "test@example.com", UserName = "testuser", Role = "User" };
            _mockUserRepository.Setup(r => r.GetByUserName("testuser")).Returns(user);

            var result = _userService.GetUserByUserName("testuser");

            Assert.NotNull(result);
            Assert.Equal("testuser", result.UserName);
            _mockUserRepository.Verify(r => r.GetByUserName("testuser"), Times.Once);
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
        public void GetAllUsers_ReturnsAllUsers()
        {
            var users = new List<User>
            {
                new User { Email = "user1@example.com", UserName = "user1", Role = "User" },
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
            var newUser = new User { Email = "newuser@example.com", UserName = "newuser", Password = "password", Role = null };

            _userService.CreateUser(newUser);

            Assert.Equal("Admin", newUser.Role);
            _mockUserRepository.Verify(r => r.Create(newUser), Times.Once);
            _mockUserRepository.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void CreateUser_ExistingAdmin_AssignsUserRole()
        {
            var existingUsers = new List<User> { new User { Email = "admin@example.com", UserName = "admin", Role = "Admin" } };
            _mockUserRepository.Setup(r => r.GetAll()).Returns(existingUsers);
            var newUser = new User { Email = "anotheruser@example.com", UserName = "anotheruser", Password = "password", Role = null };

            _userService.CreateUser(newUser);

            Assert.Equal("User", newUser.Role);
            _mockUserRepository.Verify(r => r.Create(newUser), Times.Once);
            _mockUserRepository.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void CreateUser_WithProvidedRole_RoleIsOverwritten()
        {
            var existingUsers = new List<User> { new User { Email = "admin@example.com", UserName = "admin", Role = "Admin" } };
            _mockUserRepository.Setup(r => r.GetAll()).Returns(existingUsers);
            var newUser = new User { Email = "anotheruser@example.com", UserName = "anotheruser", Password = "password", Role = "SuperAdmin" };

            _userService.CreateUser(newUser);

            Assert.Equal("User", newUser.Role);
            _mockUserRepository.Verify(r => r.Create(newUser), Times.Once);
            _mockUserRepository.Verify(r => r.GetAll(), Times.Once);
        }


        [Fact]
        public void UpdateUser_CallsRepositoryUpdate()
        {
            var userToUpdate = new User { Email = "update@example.com", UserName = "updateuser", Password = "newpass", Role = "User" };

            _userService.UpdateUser(userToUpdate);

            _mockUserRepository.Verify(r => r.Update(userToUpdate), Times.Once);
        }

        [Fact]
        public void DeleteUser_CallsRepositoryDelete()
        {
            string emailToDelete = "delete@example.com";

            _userService.DeleteUser(emailToDelete);

            _mockUserRepository.Verify(r => r.Delete(emailToDelete), Times.Once);
        }
    }
}