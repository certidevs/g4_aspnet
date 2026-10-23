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
    public DbSet<TechnicianCategory> TechnicianCategories => Set<TechnicianCategory>();

    /// <summary>Configura las columnas añadidas a la tabla de usuarios de Identity.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(100);
            entity.Property(user => user.AvatarFileName).HasMaxLength(260);
            entity.Property(user => user.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Nombre).HasMaxLength(100);
            entity.Property(c => c.Descripcion).HasMaxLength(500);
            entity.HasIndex(c => c.Nombre).IsUnique();
        });

        // configurar TechnicianCategory
        // moderlBurilder.Empty

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.Property(t => t.Titulo).HasMaxLength(100);
            entity.Property(t => t.Descripcion).HasMaxLength(500);

            // Relación con Category
            entity.HasOne(t => t.Usuario)
                  .WithMany()
                  .HasForeignKey(t => t.UsuarioId);
                              
            // Relación con ApplicationUser (creador)
            entity.HasOne(t => t.Responsable)
                  .WithMany()
                  .HasForeignKey(t => t.ResponsableId);
                  
            entity.HasOne(t => t.Categoria)
                  .WithMany(c => c.Tickets)
                  .HasForeignKey(t => t.CategoriaId);
                  
        });
    }
}
