using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering; // lo necesita el bloque comentado de abajo (SelectList)
using G4AspNet.Data;
using G4AspNet.Models;
using Microsoft.AspNetCore.Authorization; // lo necesita el bloque comentado de abajo (Project)
namespace G4AspNet.Controllers;

public class ProjectsController : Controller
{
    private readonly ApplicationDbContext context;

    public ProjectsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    // GET: /Projects
    public IActionResult Index()
    {
        // Include carga la empresa de cada proyecto para poder mostrar su nombre
        var projects = context.Projects
            .Include(p => p.Company)
            .OrderBy(p => p.Name)
            .ToList();
        return View(projects);
    }

    // GET: /Projects/Details/1
    public IActionResult Details(int id)
    {
        // Incluimos la empresa y los tickets del proyecto, que se muestran en el detalle
        var project = context.Projects
            .Include(p => p.Company)
            .Include(p => p.Tickets)
            .FirstOrDefault(p => p.Id == id);
        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

 
   // Pasos: 1) quita este comentario (borra la primera línea y la última, FIN DESCOMENTA), 2) ajusta los campos de tu entidad, 3) descomenta y ajusta Views/Projects/Form.cshtml, 4) descomenta los botones de Index y Details.
   // Modelo a copiar: CompaniesController (ejemplo completo y probado).

    // GET: /Projects/Create
    // Muestra el formulario vacío. Crear y Editar comparten la vista Views/Projects/Form.cshtml
    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Create()
    {
        // El formulario necesita la lista de empresas para su select
        await LoadCompaniesAsync();

        // ResponseTimeHours tiene [Range(1, 720)]: le damos un valor inicial para que el formulario nuevo no empiece en error
        return View("Form", new Project { ResponseTimeHours = 24 });
    }

    // GET: /Projects/Edit/1
    // Muestra el mismo formulario, relleno con los datos del proyecto
    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        // FindAsync busca por clave primaria y devuelve null si no existe
        var project = await context.Projects.FindAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        await LoadCompaniesAsync();
        return View("Form", project);
    }

    // POST: /Projects/Save
    // Crea o actualiza según el Id: 0 = proyecto nuevo, otro valor = proyecto que ya existe
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Save(Project project)
    {
        // El formulario solo envía CompanyId (el select), no el objeto Company. Como Company es no nulo en el modelo,
        // MVC lo marcaría como "obligatorio" y el formulario nunca validaría: quitamos esa validación.
        ModelState.Remove(nameof(Project.Company));

        // Si algo no valida, volvemos al formulario con los errores
        if (!ModelState.IsValid)
        {
            // Hay que volver a cargar el select, porque la lista no viaja en el formulario
            await LoadCompaniesAsync();
            return View("Form", project);
        }

        var isNew = project.Id == 0;
        if (isNew)
        {
            // CREAR
            context.Projects.Add(project);
        }
        else
        {
            // EDITAR: cargamos el proyecto y le copiamos los campos del formulario
            var existing = await context.Projects.FindAsync(project.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = project.Name;
            existing.Description = project.Description;
            existing.SupportLevel = project.SupportLevel;
            existing.ResponseTimeHours = project.ResponseTimeHours;
            existing.IsActive = project.IsActive;
            existing.CompanyId = project.CompanyId;
        }

        await context.SaveChangesAsync();

        // TempData guarda un mensaje para la siguiente petición; el _Layout lo muestra
        TempData["Message"] = isNew ? "Proyecto creado correctamente." : "Proyecto actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Projects/Delete/1
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await context.Projects.FindAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        // La relación Ticket -> Proyecto es Restrict: borrar un proyecto con tickets daría error (500).
        // Lo comprobamos antes con AnyAsync y avisamos con un mensaje.
        var hasTickets = await context.Tickets.AnyAsync(t => t.ProjectId == id);
        if (hasTickets)
        {
            TempData["Error"] = $"No se puede eliminar el proyecto «{project.Name}» porque tiene tickets. "
                + "Puedes desactivarlo desde Editar (casilla Activo).";
            return RedirectToAction(nameof(Index));
        }

        context.Projects.Remove(project);
        await context.SaveChangesAsync();

        TempData["Message"] = "Proyecto eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Rellena el select de empresas del formulario. SelectList(lista, campo del valor, campo del texto)
    private async Task LoadCompaniesAsync()
    {
        var companies = await context.Companies.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name");
    }

}
