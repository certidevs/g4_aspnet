using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using G4AspNet.Models;

namespace G4AspNet.Data;

/// <summary>Aplica migraciones y prepara roles y cuentas demo de forma idempotente.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Aplica migraciones pendientes de forma automática
        context.Database.Migrate();

        var configuration = services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("SeedData:Enabled", true))
        {
            return;
        }

        // 1. Crear Empresas (Compañías) primero, ya que los proyectos dependen de ellas
        if (!await context.Companies.AnyAsync())
        {
            context.Companies.AddRange(
                new Company { Name = "TechCorp Solutions" },
                new Company { Name = "Innovación Digital S.L." }
            );
            await context.SaveChangesAsync();
        }

        // 2. Crear Proyectos demo (asegurando que tengan un CompanyId válido existente, por ejemplo, 1 o 2)
        if (!await context.Projects.AnyAsync())
        {
            context.Projects.AddRange(
                new Project { Name = "Youtube", Description = "Creacion de videos", ResponseTimeHours = 4, CompanyId = 1 },
                new Project { Name = "Gmail", Description = "Creacion de la plataforma de correo", ResponseTimeHours = 2, CompanyId = 2 },
                new Project { Name = "YoutubeMusic", Description = "Creacion de videos musicales", ResponseTimeHours = 3, CompanyId = 1 }
            );
            await context.SaveChangesAsync();
        }

        // 3. Crear Categorías demo
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "Hardware", Description = "Solucion para el hardware" },
                new Category { Name = "Software", Description = "Solucion para el software" },
                new Category { Name = "Otros", Description = "Soluciona el resto" }
            );
            await context.SaveChangesAsync();
        }

        // 4. Crear Roles y Usuarios de Identity primero para asegurar que existan los IDs
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, RoleNames.User);
        await EnsureRoleAsync(roleManager, RoleNames.Admin);

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(
            userManager,
            username: "admin",
            email: "admin@g4.local",
            displayName: "Administrador",
            password: "Admin123!",
            role: RoleNames.Admin);
            
        await EnsureUserAsync(
            userManager,
            username: "user",
            email: "user@g4.local",
            displayName: "Usuario de demo",
            password: "User123!",
            role: RoleNames.User);

        // 5. Crear Tickets demo (solo si no existen y asegurando que las FK apunten a registros reales)
        if (!await context.Tickets.AnyAsync())
        {
            // Obtenemos IDs reales de la base de datos para evitar errores de claves foráneas
            var project = await context.Projects.FirstAsync();
            var category = await context.Categories.FirstAsync();
            var adminUser = await userManager.FindByNameAsync("admin");
            var demoUser = await userManager.FindByNameAsync("user");

            var tickets = new[]
            {
                new Ticket
                {
                    Title = "Error al iniciar sesión",
                    Description = "Los usuarios reportan fallo 500 en el login.",
                    Priority = TicketPriority.Medium,
                    Status = TicketStatus.Open,
                    ProjectId = project.Id,
                    CategoryId = category.Id,
                    CreatedById = demoUser?.Id ?? "Id1",
                    AssignedToId = adminUser?.Id,
                    ResolvedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Ticket
                {
                    Title = "Lentitud en pasarela de pago",
                    Description = "Las peticiones de cobro tardan más de 10 segundos.",
                    Priority = TicketPriority.High,
                    Status = TicketStatus.InProgress,
                    ProjectId = project.Id,
                    CategoryId = category.Id,
                    CreatedById = demoUser?.Id ?? "Id2",
                    AssignedToId = adminUser?.Id,
                    ResolvedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Ticket
                {
                    Title = "Actualizar certificado",
                    Description = "El certificado expira la próxima semana en el servidor principal.",
                    Priority = TicketPriority.High,
                    Status = TicketStatus.InProgress,
                    ProjectId = project.Id,
                    CategoryId = category.Id,
                    CreatedById = demoUser?.Id ?? "Id3",
                    AssignedToId = adminUser?.Id,
                    ResolvedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Ticket
                {
                    Title = "Fallo de diseño responsive en móvil",
                    Description = "El menú hamburguesa se solapa con el logotipo principal.",
                    Priority = TicketPriority.High,
                    Status = TicketStatus.InProgress,
                    ProjectId = project.Id,
                    CategoryId = category.Id,
                    CreatedById = demoUser?.Id ?? "Id4",
                    AssignedToId = adminUser?.Id,
                    ResolvedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Ticket
                {
                    Title = "Añadir campo NIF en formulario",
                    Description = "Requerido por normativa fiscal vigente en el registro de usuarios.",
                    Priority = TicketPriority.Low,
                    Status = TicketStatus.Open,
                    ProjectId = project.Id,
                    CategoryId = category.Id,
                    CreatedById = demoUser?.Id ?? "Id5",
                    AssignedToId = adminUser?.Id,
                    ResolvedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            context.Tickets.AddRange(tickets);
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el rol {roleName}.");
            }
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string username,
        string email,
        string displayName,
        string password,
        string role)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true,
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el usuario demo {username}.");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo asignar el rol {role} a {username}.");
            }
        }
    }
}