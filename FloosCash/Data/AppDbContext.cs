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
        public DbSet<User> Users { get; set; }

        // ثم أضف دالة OnModelCreating لإنشاء حساب المدير الافتراضي
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // إنشاء حساب مدير افتراضي لتتمكن من الدخول أول مرة
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                FullName = "مدير النظام",
                Username = "admin",
                Password = "123", // يمكنك تغييرها لاحقاً من داخل النظام
                Role = "Admin",
                IsActive = true
            });
        }
    }
}