using Microsoft.EntityFrameworkCore;
using ReferralManagement.Models;

namespace ReferralManagement.Data
{
    public class ReferralDbContext : DbContext
    {
        public ReferralDbContext(DbContextOptions<ReferralDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Referral> Referrals { get; set; }
        public DbSet<Earning> Earnings { get; set; }
        public DbSet<ReferralLimit> ReferralLimits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Additional configuration if needed
        }
    }
}
