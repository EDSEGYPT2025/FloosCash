using FloosCash.Models;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Operation> Operations { get; set; }
    }
}