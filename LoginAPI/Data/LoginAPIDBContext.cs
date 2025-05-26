using Microsoft.EntityFrameworkCore;
using LoginAPI.Models;

namespace LoginAPI.Data
{
    public class LoginAPIDBContext : DbContext
    {
        public DbSet<User> User { get; set; }
        public LoginAPIDBContext(DbContextOptions<LoginAPIDBContext> options) : base(options)
        {

        }
    }
}