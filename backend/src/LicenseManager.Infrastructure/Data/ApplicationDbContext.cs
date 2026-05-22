using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<LicenseDomain> LicenseDomains => Set<LicenseDomain>();
    public DbSet<LicenseDevice> LicenseDevices => Set<LicenseDevice>();
    public DbSet<LicenseActivation> LicenseActivations => Set<LicenseActivation>();
    public DbSet<LicenseValidation> LicenseValidations => Set<LicenseValidation>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<LicenseFeatureMapping> LicenseFeatureMappings => Set<LicenseFeatureMapping>();
    public DbSet<ProductFeature> ProductFeatures => Set<ProductFeature>();
    public DbSet<ProductVersion> ProductVersions => Set<ProductVersion>();
    public DbSet<UpdateDownload> UpdateDownloads => Set<UpdateDownload>();
    public DbSet<LicenseHistory> LicenseHistory => Set<LicenseHistory>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApiLog> ApiLogs => Set<ApiLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names to match PostgreSQL schema
        modelBuilder.Entity<AdminUser>().ToTable("admin_users");
        modelBuilder.Entity<LoginHistory>().ToTable("login_history");
        modelBuilder.Entity<Customer>().ToTable("license_customers");
        modelBuilder.Entity<Product>().ToTable("license_products");
        modelBuilder.Entity<License>().ToTable("licenses");
        modelBuilder.Entity<LicenseDomain>().ToTable("license_domains");
        modelBuilder.Entity<LicenseDevice>().ToTable("license_devices");
        modelBuilder.Entity<LicenseActivation>().ToTable("license_activations");
        modelBuilder.Entity<LicenseValidation>().ToTable("license_validations");
        modelBuilder.Entity<Feature>().ToTable("license_features");
        modelBuilder.Entity<LicenseFeatureMapping>().ToTable("license_feature_mappings");
        modelBuilder.Entity<ProductFeature>().ToTable("product_features");
        modelBuilder.Entity<ProductVersion>().ToTable("product_versions");
        modelBuilder.Entity<UpdateDownload>().ToTable("update_downloads");
        modelBuilder.Entity<LicenseHistory>().ToTable("license_history");
        modelBuilder.Entity<SupportTicket>().ToTable("support_tickets");
        modelBuilder.Entity<TicketComment>().ToTable("ticket_comments");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
        modelBuilder.Entity<ApiLog>().ToTable("api_logs");

        // Configure naming convention for PostgreSQL (snake_case)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }

        // Configure relationships and constraints
        ConfigureRelationships(modelBuilder);
        
        // Configure indexes
        ConfigureIndexes(modelBuilder);
        
        // Configure value conversions for enums
        ConfigureEnums(modelBuilder);
    }

    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // License -> Customer
        modelBuilder.Entity<License>()
            .HasOne(l => l.Customer)
            .WithMany(c => c.Licenses)
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // License -> Product
        modelBuilder.Entity<License>()
            .HasOne(l => l.Product)
            .WithMany(p => p.Licenses)
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // LicenseDomain -> License
        modelBuilder.Entity<LicenseDomain>()
            .HasOne(ld => ld.License)
            .WithMany(l => l.Domains)
            .HasForeignKey(ld => ld.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        // LicenseDevice -> License
        modelBuilder.Entity<LicenseDevice>()
            .HasOne(ld => ld.License)
            .WithMany(l => l.Devices)
            .HasForeignKey(ld => ld.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        // LicenseActivation -> License
        modelBuilder.Entity<LicenseActivation>()
            .HasOne(la => la.License)
            .WithMany(l => l.Activations)
            .HasForeignKey(la => la.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        // LicenseValidation -> License
        modelBuilder.Entity<LicenseValidation>()
            .HasOne(lv => lv.License)
            .WithMany(l => l.Validations)
            .HasForeignKey(lv => lv.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        // LicenseFeatureMapping -> License and Feature
        modelBuilder.Entity<LicenseFeatureMapping>()
            .HasOne(lfm => lfm.License)
            .WithMany(l => l.FeatureMappings)
            .HasForeignKey(lfm => lfm.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LicenseFeatureMapping>()
            .HasOne(lfm => lfm.Feature)
            .WithMany(f => f.LicenseFeatureMappings)
            .HasForeignKey(lfm => lfm.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductFeature -> Product and Feature
        modelBuilder.Entity<ProductFeature>()
            .HasOne(pf => pf.Product)
            .WithMany(p => p.ProductFeatures)
            .HasForeignKey(pf => pf.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductFeature>()
            .HasOne(pf => pf.Feature)
            .WithMany(f => f.ProductFeatures)
            .HasForeignKey(pf => pf.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductVersion -> Product
        modelBuilder.Entity<ProductVersion>()
            .HasOne(pv => pv.Product)
            .WithMany(p => p.Versions)
            .HasForeignKey(pv => pv.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // SupportTicket -> Customer
        modelBuilder.Entity<SupportTicket>()
            .HasOne(st => st.Customer)
            .WithMany(c => c.SupportTickets)
            .HasForeignKey(st => st.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // TicketComment -> SupportTicket
        modelBuilder.Entity<TicketComment>()
            .HasOne(tc => tc.Ticket)
            .WithMany(st => st.Comments)
            .HasForeignKey(tc => tc.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // LoginHistory -> AdminUser
        modelBuilder.Entity<LoginHistory>()
            .HasOne(lh => lh.User)
            .WithMany(au => au.LoginHistory)
            .HasForeignKey(lh => lh.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // LicenseHistory -> License
        modelBuilder.Entity<LicenseHistory>()
            .HasOne(lh => lh.License)
            .WithMany(l => l.History)
            .HasForeignKey(lh => lh.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Unique constraints
        modelBuilder.Entity<License>()
            .HasIndex(l => l.LicenseKey)
            .IsUnique();

        modelBuilder.Entity<License>()
            .HasIndex(l => l.ActivationToken)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.CustomerCode)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.ProductCode)
            .IsUnique();

        modelBuilder.Entity<Feature>()
            .HasIndex(f => f.FeatureCode)
            .IsUnique();

        modelBuilder.Entity<AdminUser>()
            .HasIndex(au => au.Email)
            .IsUnique();

        // Performance indexes
        modelBuilder.Entity<License>()
            .HasIndex(l => l.Status);

        modelBuilder.Entity<License>()
            .HasIndex(l => l.ExpiryDate);

        modelBuilder.Entity<LicenseValidation>()
            .HasIndex(lv => lv.CreatedAt);

        modelBuilder.Entity<LicenseActivation>()
            .HasIndex(la => la.CreatedAt);
    }

    private void ConfigureEnums(ModelBuilder modelBuilder)
    {
        // Enums are stored as integers in PostgreSQL
        modelBuilder.Entity<License>()
            .Property(l => l.LicenseType)
            .HasConversion<int>();

        modelBuilder.Entity<License>()
            .Property(l => l.Status)
            .HasConversion<int>();

        modelBuilder.Entity<LicenseActivation>()
            .Property(la => la.ActivationType)
            .HasConversion<int>();

        modelBuilder.Entity<LicenseValidation>()
            .Property(lv => lv.ValidationResult)
            .HasConversion<int>();

        modelBuilder.Entity<AdminUser>()
            .Property(au => au.Role)
            .HasConversion<int>();

        modelBuilder.Entity<SupportTicket>()
            .Property(st => st.Status)
            .HasConversion<int>();

        modelBuilder.Entity<SupportTicket>()
            .Property(st => st.Priority)
            .HasConversion<int>();

        modelBuilder.Entity<AuditLog>()
            .Property(al => al.Action)
            .HasConversion<int>();
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
