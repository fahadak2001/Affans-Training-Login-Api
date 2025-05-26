using LoginAPI.Models;

namespace LoginAPI.Services
{
    public interface IUserService
    {
        User GetUserByEmail(string email);
        User GetUserByUserName(string username);
        IEnumerable<User> GetAllUsers();
        void CreateUser(User user);
        void UpdateUser(User user);
        void DeleteUser(string email);
        User Authenticate(string username, string password);
        string GenerateJwtToken(User user);
        bool UserExists(string email, string userName);
    }
}
