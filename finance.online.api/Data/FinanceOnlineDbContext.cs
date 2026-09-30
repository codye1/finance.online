using Azure;
using finance.online.api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace finance.online.api.Data
{
    public class FinanceOnlineDbContext
        : IdentityDbContext<AppUser>
    {
        public FinanceOnlineDbContext(
            DbContextOptions<FinanceOnlineDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<OperationModel> Operations => Set<OperationModel>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Member>()
    .Property(m => m.Role)
    .HasConversion(
        v => v.ToString().ToLowerInvariant(),
        v => Enum.Parse<MemberRole>(v, true))
    .HasMaxLength(20);

            builder.Entity<Organization>()
    .HasOne(x => x.CreatedBy)
    .WithMany(x => x.CreatedOrganizations)
    .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<OperationModel>()
     .HasOne(x => x.CreatedBy)
     .WithMany(x => x.CreatedOperations)
     .HasForeignKey(x => x.CreatedById)
     .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<OperationModel>()
                .HasOne(x => x.Organization)
                .WithMany(x => x.Operations)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<OperationModel>()
                .HasOne(x => x.Category)
                .WithMany(x => x.Operations)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        }


    }
}