using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using G4AspNet.Models;

namespace G4AspNet.Data;

/// <summary>Recrea la base de datos desde las entidades y prepara roles y cuentas demo.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Proyecto de clase sin migraciones: la base se borra y se vuelve a crear
        // desde las entidades en cada arranque (como create-drop con H2 en Spring).
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var configuration = services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("SeedData:Enabled", true))
        {
            return;
        }

        // 1. Crear Empresas (Compañías) primero, ya que los proyectos y los usuarios dependen de ellas
        if (!await context.Companies.AnyAsync())
        {
            context.Companies.AddRange(
                new Company { Name = "TechCorp Solutions", TaxId = "A12345678", Email = "soporte@techcorp.example", Phone = "910000001" },
                new Company { Name = "Innovación Digital S.L.", TaxId = "B98765432", Email = "contacto@innovaciondigital.example", Phone = "910000002" },
                new Company { Name = "Fundación Horizonte", TaxId = "G11223344", Email = "info@fundacionhorizonte.example", Phone = "910000003" }
            );
            await context.SaveChangesAsync();
        }

        // 2. Crear Proyectos demo (asegurando que tengan un CompanyId válido existente: 1, 2 o 3,
        //    que son las empresas de arriba por orden). Hay distinto nivel de soporte y uno inactivo.
        if (!await context.Projects.AnyAsync())
        {
            context.Projects.AddRange(
                new Project { Name = "Youtube", Description = "Creacion de videos", SupportLevel = SupportLevel.Premium, ResponseTimeHours = 4, CompanyId = 1 },
                new Project { Name = "Gmail", Description = "Creacion de la plataforma de correo", SupportLevel = SupportLevel.Premium, ResponseTimeHours = 2, CompanyId = 2 },
                new Project { Name = "YoutubeMusic", Description = "Creacion de videos musicales", SupportLevel = SupportLevel.Standard, ResponseTimeHours = 3, CompanyId = 1 },
                new Project { Name = "Portal de voluntariado", Description = "Web para gestionar voluntarios y donaciones", SupportLevel = SupportLevel.Basic, ResponseTimeHours = 48, CompanyId = 3 },
                new Project { Name = "Intranet antigua", Description = "Proyecto finalizado, ya sin soporte", SupportLevel = SupportLevel.Basic, ResponseTimeHours = 72, IsActive = false, CompanyId = 2 }
            );
            await context.SaveChangesAsync();
        }

        // 3. Crear Categorías demo
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "Hardware", Description = "Solucion para el hardware" },
                new Category { Name = "Software", Description = "Solucion para el software" },
                new Category { Name = "Otros", Description = "Soluciona el resto" },
                new Category { Name = "Sugerencia de mejora", Description = "Ideas para mejorar el producto o el servicio" },
                new Category { Name = "Gestión de pagos", Description = "Facturas, cobros y métodos de pago" },
                new Category { Name = "Baja y cancelación", Description = "Bajas de usuarios y cancelación de servicios" }
            );
            await context.SaveChangesAsync();
        }

        // 4. Crear Roles y Usuarios de Identity primero para asegurar que existan los IDs
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, RoleNames.User);
        await EnsureRoleAsync(roleManager, RoleNames.Admin);

        // El admin no pertenece a ninguna empresa (CompanyId vacío); cada cliente (rol User) trabaja en una empresa.
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
            role: RoleNames.User,
            companyId: 1);

        await EnsureUserAsync(
            userManager,
            username: "user2",
            email: "user2@g4.local",
            displayName: "Laura Gómez",
            password: "User123!",
            role: RoleNames.User,
            companyId: 2);

        await EnsureUserAsync(
            userManager,
            username: "user3",
            email: "user3@g4.local",
            displayName: "Carlos Ruiz",
            password: "User123!",
            role: RoleNames.User,
            companyId: 3);

        // 5. Crear Tickets demo (solo si no existen y asegurando que las FK apunten a registros reales).
        //    Hay tickets en todos los estados y prioridades, de varios proyectos y categorías.
        //    Cada ticket lo crea un cliente de la empresa dueña del proyecto; el admin hace de técnico
        //    y algunos tickets todavía no tienen técnico asignado (AssignedToId vacío).
        if (!await context.Tickets.AnyAsync())
        {
            // Obtenemos registros reales de la base de datos para evitar errores de claves foráneas
            var youtube = await context.Projects.FirstAsync(p => p.Name == "Youtube");
            var gmail = await context.Projects.FirstAsync(p => p.Name == "Gmail");
            var youtubeMusic = await context.Projects.FirstAsync(p => p.Name == "YoutubeMusic");
            var voluntariado = await context.Projects.FirstAsync(p => p.Name == "Portal de voluntariado");

            var hardware = await context.Categories.FirstAsync(c => c.Name == "Hardware");
            var software = await context.Categories.FirstAsync(c => c.Name == "Software");
            var otros = await context.Categories.FirstAsync(c => c.Name == "Otros");
            var sugerencia = await context.Categories.FirstAsync(c => c.Name == "Sugerencia de mejora");
            var pagos = await context.Categories.FirstAsync(c => c.Name == "Gestión de pagos");
            var baja = await context.Categories.FirstAsync(c => c.Name == "Baja y cancelación");

            var admin = await GetUserAsync(userManager, "admin");
            var user1 = await GetUserAsync(userManager, "user");   // empresa 1: TechCorp Solutions
            var user2 = await GetUserAsync(userManager, "user2");  // empresa 2: Innovación Digital S.L.
            var user3 = await GetUserAsync(userManager, "user3");  // empresa 3: Fundación Horizonte

            var tickets = new[]
            {
                new Ticket
                {
                    Title = "Error al iniciar sesión",
                    Description = "Los usuarios reportan fallo 500 en el login.",
                    Priority = TicketPriority.Medium,
                    Status = TicketStatus.Open,
                    CreatedAt = FechaDemo(11, 4, 9, 30),
                    ProjectId = youtube.Id,
                    CategoryId = software.Id,
                    CreatedById = user1.Id,
                    AssignedToId = admin.Id
                },
                new Ticket
                {
                    Title = "Lentitud en pasarela de pago",
                    Description = "Las peticiones de cobro tardan más de 10 segundos.",
                    Priority = TicketPriority.High,
                    Status = TicketStatus.InProgress,
                    CreatedAt = FechaDemo(11, 3, 12, 15),
                    ProjectId = voluntariado.Id,
                    CategoryId = pagos.Id,
                    CreatedById = user3.Id,
                    AssignedToId = admin.Id
                },
                new Ticket
                {
                    Title = "Actualizar certificado",
                    Description = "El certificado expira la próxima semana en el servidor principal.",
                    Priority = TicketPriority.High,
                    Status = TicketStatus.Resolved,
                    CreatedAt = FechaDemo(10, 30, 9, 0),
                    ProjectId = gmail.Id,
                    CategoryId = software.Id,
                    CreatedById = user2.Id,
                    AssignedToId = admin.Id,
                    ResolvedAt = FechaDemo(11, 2, 17, 0)
                },
                new Ticket
                {
                    Title = "Fallo de diseño responsive en móvil",
                    Description = "El menú hamburguesa se solapa con el logotipo principal.",
                    Priority = TicketPriority.High,
                    Status = TicketStatus.Closed,
                    CreatedAt = FechaDemo(10, 28, 9, 0),
                    ProjectId = youtube.Id,
                    CategoryId = software.Id,
                    CreatedById = user1.Id,
                    AssignedToId = admin.Id,
                    ResolvedAt = FechaDemo(11, 1, 16, 0)
                },
                new Ticket
                {
                    Title = "Añadir campo NIF en formulario",
                    Description = "Requerido por normativa fiscal vigente en el registro de usuarios.",
                    Priority = TicketPriority.Low,
                    Status = TicketStatus.Open,
                    CreatedAt = FechaDemo(11, 2, 10, 45),
                    ProjectId = voluntariado.Id,
                    CategoryId = sugerencia.Id,
                    CreatedById = user3.Id,
                    AssignedToId = admin.Id
                },
                new Ticket
                {
                    Title = "Servidor de correo caído",
                    Description = "El correo de toda la empresa no envía ni recibe mensajes desde las 7:30.",
                    Priority = TicketPriority.Urgent,
                    Status = TicketStatus.InProgress,
                    CreatedAt = FechaDemo(11, 5, 7, 45),
                    ProjectId = gmail.Id,
                    CategoryId = software.Id,
                    CreatedById = user2.Id,
                    AssignedToId = admin.Id
                },
                new Ticket
                {
                    Title = "Cambiar el método de pago de la suscripción",
                    Description = "Queremos pagar con transferencia en lugar de con la tarjeta que caduca.",
                    Priority = TicketPriority.Medium,
                    Status = TicketStatus.WaitingCustomer,
                    CreatedAt = FechaDemo(11, 3, 16, 20),
                    ProjectId = youtubeMusic.Id,
                    CategoryId = pagos.Id,
                    CreatedById = user1.Id,
                    AssignedToId = admin.Id
                },
                new Ticket
                {
                    Title = "Dar de baja la cuenta de un empleado",
                    Description = "Una persona ha dejado la empresa y su cuenta debe desactivarse.",
                    Priority = TicketPriority.Low,
                    Status = TicketStatus.Resolved,
                    CreatedAt = FechaDemo(11, 1, 11, 0),
                    ProjectId = gmail.Id,
                    CategoryId = baja.Id,
                    CreatedById = user2.Id,
                    AssignedToId = admin.Id,
                    ResolvedAt = FechaDemo(11, 2, 9, 30)
                },
                new Ticket
                {
                    Title = "Añadir modo oscuro a la aplicación",
                    Description = "Nos gustaría poder usar la aplicación con un tema oscuro por la noche.",
                    Priority = TicketPriority.Low,
                    Status = TicketStatus.Open,
                    CreatedAt = FechaDemo(11, 5, 8, 10),
                    ProjectId = youtubeMusic.Id,
                    CategoryId = sugerencia.Id,
                    CreatedById = user1.Id
                    // Sin AssignedToId: todavía no tiene técnico asignado.
                },
                new Ticket
                {
                    Title = "El portátil del equipo no arranca",
                    Description = "El portátil de coordinación no enciende y hoy hay una reunión con voluntarios.",
                    Priority = TicketPriority.High,
                    Status = TicketStatus.Open,
                    CreatedAt = FechaDemo(11, 5, 9, 20),
                    ProjectId = voluntariado.Id,
                    CategoryId = hardware.Id,
                    CreatedById = user3.Id
                    // Sin AssignedToId: todavía no tiene técnico asignado.
                },
                new Ticket
                {
                    Title = "Duda sobre la factura de octubre",
                    Description = "El importe de la factura de octubre es mayor que el de septiembre.",
                    Priority = TicketPriority.Medium,
                    Status = TicketStatus.Closed,
                    CreatedAt = FechaDemo(10, 27, 15, 0),
                    ProjectId = youtube.Id,
                    CategoryId = pagos.Id,
                    CreatedById = user1.Id,
                    AssignedToId = admin.Id,
                    ResolvedAt = FechaDemo(10, 29, 12, 0)
                },
                new Ticket
                {
                    Title = "Caída de la web durante la campaña",
                    Description = "La web se queda sin respuesta cuando enviamos el boletín a todos los voluntarios.",
                    Priority = TicketPriority.Urgent,
                    Status = TicketStatus.WaitingCustomer,
                    CreatedAt = FechaDemo(11, 4, 18, 5),
                    ProjectId = voluntariado.Id,
                    CategoryId = otros.Id,
                    CreatedById = user3.Id,
                    AssignedToId = admin.Id
                }
            };

            context.Tickets.AddRange(tickets);
            await context.SaveChangesAsync();
        }

        // 6. Crear Comentarios demo en algunos tickets (los hay públicos y los hay internos, solo del equipo de soporte).
        //    Los comentarios los escriben el cliente que creó el ticket o el admin (que hace de técnico).
        if (!await context.TicketComments.AnyAsync())
        {
            var admin = await GetUserAsync(userManager, "admin");
            var user1 = await GetUserAsync(userManager, "user");
            var user2 = await GetUserAsync(userManager, "user2");
            var user3 = await GetUserAsync(userManager, "user3");

            // Buscamos cada ticket por su título para usar su Id real
            var allTickets = await context.Tickets.ToListAsync();
            int TicketId(string title) => allTickets.First(t => t.Title == title).Id;

            context.TicketComments.AddRange(
                new TicketComment
                {
                    TicketId = TicketId("Error al iniciar sesión"),
                    AuthorId = user1.Id,
                    Message = "Ocurre desde ayer por la tarde, sobre todo con las cuentas nuevas.",
                    CreatedAt = FechaDemo(11, 4, 10, 0)
                },
                new TicketComment
                {
                    TicketId = TicketId("Error al iniciar sesión"),
                    AuthorId = admin.Id,
                    Message = "Revisar los logs de Identity antes de contestar al cliente.",
                    IsInternal = true,
                    CreatedAt = FechaDemo(11, 4, 11, 15)
                },
                new TicketComment
                {
                    TicketId = TicketId("Lentitud en pasarela de pago"),
                    AuthorId = admin.Id,
                    Message = "Hemos detectado consultas lentas en la base de datos y estamos optimizándolas.",
                    CreatedAt = FechaDemo(11, 3, 13, 0)
                },
                new TicketComment
                {
                    TicketId = TicketId("Lentitud en pasarela de pago"),
                    AuthorId = user3.Id,
                    Message = "Gracias. ¿Tenéis una fecha estimada para la solución?",
                    CreatedAt = FechaDemo(11, 3, 13, 30)
                },
                new TicketComment
                {
                    TicketId = TicketId("Lentitud en pasarela de pago"),
                    AuthorId = admin.Id,
                    Message = "Mañana por la mañana desplegamos el cambio y volvemos a medir los tiempos.",
                    CreatedAt = FechaDemo(11, 3, 14, 10)
                },
                new TicketComment
                {
                    TicketId = TicketId("Actualizar certificado"),
                    AuthorId = admin.Id,
                    Message = "Certificado renovado e instalado en el servidor principal.",
                    CreatedAt = FechaDemo(11, 2, 16, 45)
                },
                new TicketComment
                {
                    TicketId = TicketId("Actualizar certificado"),
                    AuthorId = user2.Id,
                    Message = "Confirmado, la web ya muestra el certificado nuevo. Gracias.",
                    CreatedAt = FechaDemo(11, 2, 17, 30)
                },
                new TicketComment
                {
                    TicketId = TicketId("Fallo de diseño responsive en móvil"),
                    AuthorId = user1.Id,
                    Message = "Se ve en pantallas pequeñas, por ejemplo en un iPhone SE.",
                    CreatedAt = FechaDemo(10, 28, 9, 40)
                },
                new TicketComment
                {
                    TicketId = TicketId("Fallo de diseño responsive en móvil"),
                    AuthorId = admin.Id,
                    Message = "Corregido en la última versión: el menú ya no tapa el logotipo.",
                    CreatedAt = FechaDemo(11, 1, 15, 50)
                },
                new TicketComment
                {
                    TicketId = TicketId("Servidor de correo caído"),
                    AuthorId = user2.Id,
                    Message = "Seguimos sin poder enviar correos, es urgente por un envío a clientes.",
                    CreatedAt = FechaDemo(11, 5, 8, 0)
                },
                new TicketComment
                {
                    TicketId = TicketId("Servidor de correo caído"),
                    AuthorId = admin.Id,
                    Message = "El disco del relé de correo está lleno. Liberar espacio y avisar a infraestructura.",
                    IsInternal = true,
                    CreatedAt = FechaDemo(11, 5, 8, 20)
                },
                new TicketComment
                {
                    TicketId = TicketId("Servidor de correo caído"),
                    AuthorId = admin.Id,
                    Message = "Ya estamos trabajando en ello. Os avisamos en cuanto se restablezca el servicio.",
                    CreatedAt = FechaDemo(11, 5, 8, 25)
                },
                new TicketComment
                {
                    TicketId = TicketId("Cambiar el método de pago de la suscripción"),
                    AuthorId = admin.Id,
                    Message = "¿Puede enviarnos el IBAN de la cuenta desde la que quiere hacer la transferencia?",
                    CreatedAt = FechaDemo(11, 4, 9, 0)
                },
                new TicketComment
                {
                    TicketId = TicketId("Dar de baja la cuenta de un empleado"),
                    AuthorId = admin.Id,
                    Message = "La cuenta ya está desactivada y el correo redirigido a su responsable.",
                    CreatedAt = FechaDemo(11, 2, 9, 25)
                },
                new TicketComment
                {
                    TicketId = TicketId("Duda sobre la factura de octubre"),
                    AuthorId = admin.Id,
                    Message = "La diferencia se debe a las licencias adicionales contratadas el 15 de octubre.",
                    CreatedAt = FechaDemo(10, 28, 10, 30)
                },
                new TicketComment
                {
                    TicketId = TicketId("Duda sobre la factura de octubre"),
                    AuthorId = user1.Id,
                    Message = "Entendido, gracias por la explicación.",
                    CreatedAt = FechaDemo(10, 29, 11, 45)
                },
                new TicketComment
                {
                    TicketId = TicketId("Caída de la web durante la campaña"),
                    AuthorId = admin.Id,
                    Message = "Necesitamos los logs del servidor web del día del envío para ver qué ocurre.",
                    CreatedAt = FechaDemo(11, 5, 9, 0)
                }
            );
            await context.SaveChangesAsync();
        }
    }

    /// <summary>Fecha UTC de noviembre/octubre de 2026 para que los datos demo sean legibles.</summary>
    private static DateTime FechaDemo(int mes, int dia, int hora, int minuto)
    {
        return new DateTime(2026, mes, dia, hora, minuto, 0, DateTimeKind.Utc);
    }

    /// <summary>Busca un usuario demo ya creado; si no existiera, el arranque falla con un mensaje claro.</summary>
    private static async Task<ApplicationUser> GetUserAsync(UserManager<ApplicationUser> userManager, string username)
    {
        return await userManager.FindByNameAsync(username)
            ?? throw new InvalidOperationException($"No existe el usuario demo {username}.");
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
        string role,
        int? companyId = null)
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
                IsActive = true,
                CompanyId = companyId
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
