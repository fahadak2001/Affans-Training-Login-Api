using LoginAPI.Data;
using LoginAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginAPI.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LoginAPIDBContext _dbContext;
        public UserRepository(LoginAPIDBContext dbContext)
        {
            _dbContext = dbContext;
        }
        public User GetByEmail(string email)
        {
            var user = _dbContext.User.Find(email);
            return user;
        }
        public User GetByUserName(string username)
        {
            var user = _dbContext.User.Find(username);
            return user;
        }

        public IEnumerable<User> GetAll()
        {
            return (IEnumerable<User>)_dbContext.User.ToList();
        }

        public void Create(User user)
        {
            bool check = false;
            if (_dbContext.User.Any(u => u.Role == "Admin"))
            {
                check = true;
                user.Role = "User";
            }
            if (check == false)
            {
                user.Role = "Admin";
            }
            _dbContext.User.Add(user);
            _dbContext.SaveChanges();
        }

        public void Update(User user)
        {
            _dbContext.User.Update(user);
            _dbContext.SaveChanges();
        }

        public void Delete(string email)
        {
            var userToDelete = _dbContext.User.Find(email);

            _dbContext.User.Remove(userToDelete);
            _dbContext.SaveChanges();
        }

        public bool UserExists(string email, string userName)
        {
            return _dbContext.User.Any(u => u.Email == email || u.UserName == userName);
        }
    }
}
