using LoginAPI.Data;
using LoginAPI.Models;

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
    }
}
