using LoginAPI.Models;

namespace LoginAPI.Repositories
{
    public interface IUserRepository
    {
        User GetByEmail(string email);
        User GetByUserName(string username);
        IEnumerable<User> GetAll();
        void Create(User user);
        void Update(User user);
        void Delete(string email);
    }
}
