using Azure;
using finance.online.api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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


        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<OperationModel> Operations => Set<OperationModel>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

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