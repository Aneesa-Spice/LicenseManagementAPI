using LicensingAPI.Models.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LicensingAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        //// DbSets
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Policy> Policies { get; set; } = null!;
        public DbSet<License> Licenses { get; set; } = null!;
        public DbSet<AppSetting> AppSettings { get; set; } = null!;
        public DbSet<Activation> Activations { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<UserLicense> UserLicenses { get; set; } = null!;
        //public DbSet<CompanyUser> CompanyUser { get; set; } = null!;
        //public DbSet<LicenseEvent> LicenseEvent { get; set; } = null!;
        //public DbSet<TrialSession> TrialSession { get; set; } = null!;
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ////AppSetting
            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired().HasMaxLength(100);
            });

            //// Policy
            modelBuilder.Entity<Policy>(entity =>
            {
                entity.HasKey(e => e.PolicyId);
                entity.HasIndex(e => e.ProviderPolicyId).IsUnique();

                entity.HasOne(p => p.Product)
                      .WithMany(pr => pr.Policies)
                      .HasForeignKey(p => p.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //// Product
            modelBuilder.Entity<Product>(entity =>
            {
                //entity.HasKey(e => e.ProductId);
                //entity.HasIndex(e => e.ProviderProductId).IsUnique();
                //entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
                //entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
                entity.HasKey(x => x.ProductId);

                entity.Property(x => x.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Description)
                    .HasMaxLength(1000);

                entity.Property(x => x.ProviderProductId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.HasMany(x => x.Licenses)
                    .WithOne(x => x.Product)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            //// Company
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.CompanyId);

                entity.Property(x => x.CompanyName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Address)
                    .HasMaxLength(500);

                entity.Property(x => x.ContactEmail)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.ContactPhone)
                    .HasMaxLength(50);

                //entity.HasMany(x => x.Users)
                //    .WithOne(x => x.Company)
                //    .HasForeignKey(x => x.CompanyId)
                //    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.Licenses)
                    .WithOne(x => x.Company)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
                //entity.HasKey(e => e.CompanyId);
                //entity.HasIndex(e => e.CompanyName).IsUnique();
                //entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
            });

            //// License
            modelBuilder.Entity<License>(entity =>
            {
                entity.HasKey(x => x.LicenseId);

                entity.Property(x => x.LicenseKey)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(x => x.ProviderLicenseId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.MaxUsers)
                    .IsRequired();

                entity.Property(x => x.MaxMachines)
                    .IsRequired();

                entity.HasIndex(x => x.LicenseKey)
                    .IsUnique();

                entity.HasOne(x => x.Company)
                    .WithMany(x => x.Licenses)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.Product)
                      .WithMany(p => p.Licenses)
                      .HasForeignKey(l => l.ProductId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(x => x.Activations)
                    .WithOne(x => x.License)
                    .HasForeignKey(x => x.LicenseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(l => l.Policy)
                      .WithMany(p => p.Licenses)
                      .HasForeignKey(l => l.PolicyId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(l => l.Status)
                      .HasMaxLength(20);
            });

            //// UserLicense
            modelBuilder.Entity<UserLicense>(entity =>
            {
                entity.HasKey(e => e.UserLicenseId);

                // ✅ one email per license
                entity.HasIndex(e => new { e.UserEmail, e.LicenseId }).IsUnique();

                entity.HasOne(ul => ul.License)
                      .WithMany(l => l.UserLicenses)
                      .HasForeignKey(ul => ul.LicenseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ul => ul.Company)
                      .WithMany(c => c.UserLicenses)
                      .HasForeignKey(ul => ul.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //// Activations
            modelBuilder.Entity<Activation>(entity =>
            {
                entity.HasKey(x => x.ActivationId);

                entity.HasOne(x => x.License)
                      .WithMany(x => x.Activations)
                      .HasForeignKey(x => x.LicenseId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.UserLicense)
                      .WithMany(ul => ul.Activations)
                      .HasForeignKey(x => x.UserLicenseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.ProviderMachineId);
                entity.HasIndex(x => new { x.LicenseId, x.UserEmail, x.MachineName });
            });

            ////AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                //entity.HasOne(al => al.User)
                //      .WithMany(u => u.AuditLogs)
                //      .HasForeignKey(al => al.UserId)
                //      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(al => al.Action).IsRequired().HasMaxLength(100);
                entity.Property(al => al.EntityName).IsRequired().HasMaxLength(100);
            });

            
        }
    }
}
