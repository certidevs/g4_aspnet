using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using G4AspNet.Models;

namespace G4AspNet.Data;

/// <summary>
/// Contexto de EF Core: configura SQLite y reúne las tablas de la aplicación.
/// Las entidades del dominio del proyecto se añadirán aquí.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>EF Core recibe estas opciones desde Program.cs para conectar con SQLite.</summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Represento en la BBDD las entidades 
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    /// <summary>Configura las columnas añadidas a la tabla de usuarios de Identity.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(100);
            entity.Property(user => user.AvatarFileName).HasMaxLength(260);
            entity.Property(user => user.CreatedAt).IsRequired();

            entity.HasOne(user => user.Company)
                  .WithMany()
                  .HasForeignKey(user => user.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(c => c.TaxId).IsUnique();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasOne(p => p.Company)
                  .WithMany()
                  .HasForeignKey(p => p.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(500);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.Property(t => t.Title).HasMaxLength(100);
            entity.Property(t => t.Description).HasMaxLength(500);

            // Relación con ApplicationUser (creador)
            entity.HasOne(t => t.CreatedBy)
                  .WithMany()
                  .HasForeignKey(t => t.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            // Relación con ApplicationUser (técnico responsable, opcional)
            entity.HasOne(t => t.AssignedTo)
                  .WithMany()
                  .HasForeignKey(t => t.AssignedToId);

            // Relación con Category
            entity.HasOne(t => t.Category)
                  .WithMany(c => c.Tickets)
                  .HasForeignKey(t => t.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Relación con Project
            entity.HasOne(t => t.Project)
                  .WithMany()
                  .HasForeignKey(t => t.ProjectId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        /* Este "modelBuilder" establece que los comentarios pertenezcan a un ticket y a un autor,
        Si se elimina, sus comentarios se quitan también. */

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.Property(c => c.Message)
                .HasMaxLength(2000)
                .IsRequired();

            entity.HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
