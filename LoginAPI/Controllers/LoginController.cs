using System.Text.Json;
using System.Text;
using LoginAPI.Models;
using LoginAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace LoginAPI.Controllers
{
    [ApiController]
    [Route("api/login")]
    [Produces("application/json")]
    public class LoginController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IDistributedCache _distributedCache;
        public LoginController(IUserService userService, IDistributedCache distributedCache)
        {
            _userService = userService;
            _distributedCache = distributedCache;
        }


        [HttpGet("List")] 
        [Authorize]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var username = User.Identity?.Name;
            string cacheKey = $"user:{username}";
            byte[] cachedUsersBytes = await _distributedCache.GetAsync(cacheKey);

            if (cachedUsersBytes != null)
            {
                var cachedUsers = JsonSerializer.Deserialize<List<User>>(Encoding.UTF8.GetString(cachedUsersBytes));
                return Ok(cachedUsers);
            }


            IEnumerable<User> users = _userService.GetAllUsers();

            var options = new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));


            var usersJson = JsonSerializer.Serialize(users.ToList());
            var usersBytes = Encoding.UTF8.GetBytes(usersJson);
            await _distributedCache.SetAsync(cacheKey, usersBytes, options);


            return Ok(users.ToList());
        }
        [HttpGet("Role")]
        [Authorize]
        public ActionResult<User> GetUser()
        {
            var email = User.Identity?.Name;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is null or empty.");
            }

            var user = _userService.GetUserByEmail(email);
            return Ok(user);
        }

        [HttpPost("Create")]
        public ActionResult<User> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            IEnumerable<User> users = _userService.GetAllUsers();
            foreach (var u in users)
            {
                if ( (u.Email == user.Email) || (u.UserName == user.UserName))
                {
                    return BadRequest("UserName or Email already exists");
                }
            }

            _userService.CreateUser(user);


            string cacheKey = $"user:{"admin@admin.com"}";
            _distributedCache.RemoveAsync(cacheKey);
            return Ok(user);
        }


        [HttpPut("Update")]
        [Authorize]
        public IActionResult UpdateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = _userService.GetUserByEmail(user.Email);
            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.Email = user.Email;
            existingUser.UserName = user.UserName;
            existingUser.Password = user.Password;

            _userService.UpdateUser(existingUser);

            string cacheKey = $"user:{"admin@admin.com"}";
            _distributedCache.RemoveAsync(cacheKey);
            return Ok(existingUser);
        }

        [HttpDelete("Delete/{email}")]
        [Authorize]
        public IActionResult DeleteUser(string email)
        {
            var userToDelete = _userService.GetUserByEmail(email);
            if (userToDelete == null)
            {
                return NotFound();
            }

            _userService.DeleteUser(email);

            string cacheKey = $"user:{"admin@admin.com"}";
            _distributedCache.RemoveAsync(cacheKey);
            return Ok();
        }

        [HttpPost("Login")]
        public ActionResult<object> LoginUser([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _userService.Authenticate(model.UserName, model.Password);

            if (user != null)
            {
                var token = _userService.GenerateJwtToken(user);
                return Ok(new { Token = token });
            }

            return Unauthorized(new { Message = "UserName or Password Incorrect" });
        }
    }
}
