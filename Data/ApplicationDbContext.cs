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

    /// <summary>
    /// Configura las columnas añadidas a la tabla de usuarios de Identity y las relaciones del dominio.
    /// Cada relación indica su OnDelete: Cascade solo si el hijo no tiene sentido sin el padre,
    /// Restrict si es historial o un dato importante (y siempre con ApplicationUser).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(100);
            entity.Property(user => user.AvatarFileName).HasMaxLength(260);
            entity.Property(user => user.CreatedAt).IsRequired();

            // Usuario -> Empresa (opcional): Restrict, no se borra una empresa que aún tiene usuarios.
            entity.HasOne(user => user.Company)
                  .WithMany(c => c.Users)
                  .HasForeignKey(user => user.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(c => c.TaxId).IsUnique();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            // Proyecto -> Empresa: Restrict, los proyectos (y sus tickets) son historial de la empresa.
            entity.HasOne(p => p.Company)
                  .WithMany(c => c.Projects)
                  .HasForeignKey(p => p.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(2000);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.Property(t => t.Title).HasMaxLength(100);
            entity.Property(t => t.Description).HasMaxLength(2000);

            // Ticket -> Usuario creador: Restrict, siempre con ApplicationUser (el ticket es historial).
            entity.HasOne(t => t.CreatedBy)
                  .WithMany(u => u.CreatedTickets)
                  .HasForeignKey(t => t.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            // Ticket -> Usuario técnico (opcional): Restrict, siempre con ApplicationUser (no SetNull).
            entity.HasOne(t => t.AssignedTo)
                  .WithMany(u => u.AssignedTickets)
                  .HasForeignKey(t => t.AssignedToId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Ticket -> Categoría: Restrict, no se borra una categoría que ya se ha usado en tickets.
            entity.HasOne(t => t.Category)
                  .WithMany(c => c.Tickets)
                  .HasForeignKey(t => t.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Ticket -> Proyecto: Restrict, los tickets son el historial de soporte del proyecto.
            entity.HasOne(t => t.Project)
                  .WithMany(p => p.Tickets)
                  .HasForeignKey(t => t.ProjectId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.Property(c => c.Message)
                .HasMaxLength(2000)
                .IsRequired();

            // Comentario -> Ticket: Cascade, un comentario no tiene sentido sin su ticket.
            entity.HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comentario -> Usuario autor: Restrict, siempre con ApplicationUser (queda el historial de quién escribió).
            entity.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
