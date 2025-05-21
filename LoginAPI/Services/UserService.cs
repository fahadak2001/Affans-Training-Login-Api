using LoginAPI.Models;
using LoginAPI.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LoginAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository userRepository, IConfiguration configuration) 
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public User Authenticate(string email, string password)
        {
            var user = _userRepository.GetByEmail(email);

            if (user == null)
            {
                user = _userRepository.GetByUserName(email);
                if (user == null || user.Password != password)
                {
                    return null;
                }
            }

            return user;
        }

        public string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT key is not configured.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserName),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public User GetUserByEmail(string email)
        {
            return _userRepository.GetByEmail(email);
        }

        public User GetUserByUserName(string username)
        {
            return _userRepository.GetByUserName(username);
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _userRepository.GetAll();
        }

        public void CreateUser(User user)
        {
            bool check = false;
            IEnumerable<User> users = _userRepository.GetAll();
            foreach (var u in users)
            {
                if ((u.Role == "Admin"))
                {
                    check = true;
                    user.Role = "User";
                    break;
                }
            }
            if (check == false)
            {
                user.Role = "Admin";
            }
            _userRepository.Create(user);
        }

        public void UpdateUser(User user)
        {
            _userRepository.Update(user);
        }

        public void DeleteUser(string email)
        {
            _userRepository.Delete(email);
        }
    }
}
