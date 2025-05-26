using LoginAPI.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace LoginAPI.Services
{
    public class CachedUserService : ICachedUserService
    {
        private readonly IUserService _decoratedUserService;
        private readonly IDistributedCache _distributedCache;
        private readonly DistributedCacheEntryOptions _cacheOptions;

        public CachedUserService(IUserService decoratedUserService, IDistributedCache distributedCache)
        {
            _decoratedUserService = decoratedUserService;
            _distributedCache = distributedCache;
            _cacheOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));
        }

        public User GetUserByEmail(string email)
        {
            return _decoratedUserService.GetUserByEmail(email);
        }

        public User GetUserByUserName(string username)
        {
            return _decoratedUserService.GetUserByUserName(username);
        }

        public IEnumerable<User> GetAllUsers()
        {
            string cacheKey = $"users:all";
            byte[] cachedUsersBytes = _distributedCache.Get(cacheKey);

            if (cachedUsersBytes != null)
            {
                return JsonSerializer.Deserialize<List<User>>(Encoding.UTF8.GetString(cachedUsersBytes));
            }
            IEnumerable<User> users = _decoratedUserService.GetAllUsers();
            if (users != null)
            {
                var usersJson = JsonSerializer.Serialize(users.ToList());
                var usersBytes = Encoding.UTF8.GetBytes(usersJson);
                _distributedCache.Set(cacheKey, usersBytes, _cacheOptions);
            }
            return users;
        }

        public void CreateUser(User user)
        {
            _decoratedUserService.CreateUser(user);

            _distributedCache.Remove($"users:all");
        }

        public void UpdateUser(User user)
        {
            _decoratedUserService.UpdateUser(user);

            _distributedCache.Remove($"users:all");
            _distributedCache.Remove($"user:email:{user.Email}");
            _distributedCache.Remove($"user:username:{user.UserName}");
        }

        public void DeleteUser(string email)
        {
            _decoratedUserService.DeleteUser(email);

            _distributedCache.Remove($"users:all");
            _distributedCache.Remove($"user:email:{email}");
        }

        public User Authenticate(string username, string password)
        {
            return _decoratedUserService.Authenticate(username, password);
        }

        public string GenerateJwtToken(User user)
        {
            return _decoratedUserService.GenerateJwtToken(user);
        }

        public bool UserExists(string email, string userName)
        {
            return _decoratedUserService.UserExists(email, userName);
        }
    }
}