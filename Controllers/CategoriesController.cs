using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using G4AspNet.Data;
using G4AspNet.Models; // lo necesita el bloque comentado de abajo (Category)
namespace G4AspNet.Controllers;

public class CategoriesController : Controller
{

    private readonly ApplicationDbContext context;

    public CategoriesController(ApplicationDbContext context)
    {
        this.context = context;
    }

    // GET: /Categories
    public IActionResult Index()
    {
        // Include carga los tickets de cada categoría para poder mostrar cuántos hay
        var categories = context.Categories
            .Include(c => c.Tickets)
            .OrderBy(c => c.Name)
            .ToList();
        return View(categories);
    }

    // GET: /Categories/Details/1
    public IActionResult Details(int id)
    {
        var category = context.Categories
            .Include(c => c.Tickets)
            .FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }
    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new Category());
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // FindAsync busca por clave primaria y devuelve null si no existe
        var category = await context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        return View("Form", category);
    }
    [HttpPost]
    public async Task<IActionResult> Save(Category category)
    {
        // Regla de la issue: el nombre no se puede repetir (la base de datos tiene un índice único).
        // Buscamos OTRA categoría (Id distinto) con el mismo nombre.
        var nameRepeated = await context.Categories
            .AnyAsync(c => c.Name == category.Name && c.Id != category.Id);
        if (nameRepeated)
        {
            ModelState.AddModelError(nameof(Category.Name), "Ya existe una categoría con ese nombre.");
        }

        // Si algo no valida, volvemos al formulario con los errores
        if (!ModelState.IsValid)
        {
            return View("Form", category);
        }

        var isNew = category.Id == 0;
        if (isNew)
        {
            // CREAR
            context.Categories.Add(category);
        }
        else
        {
            // EDITAR: cargamos la categoría y le copiamos los campos del formulario
            var existing = await context.Categories.FindAsync(category.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = category.Name;
            existing.Description = category.Description;
        }

        await context.SaveChangesAsync();

        // TempData guarda un mensaje para la siguiente petición; el _Layout lo muestra
        TempData["Message"] = isNew ? "Categoría creada correctamente." : "Categoría actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        // La relación Ticket -> Categoría es Restrict: borrar una categoría con tickets daría error (500).
        // Lo comprobamos antes con AnyAsync y avisamos con un mensaje.
        var hasTickets = await context.Tickets.AnyAsync(t => t.CategoryId == id);
        if (hasTickets)
        {
            TempData["Error"] = $"No se puede eliminar la categoría «{category.Name}» porque tiene tickets.";
            return RedirectToAction(nameof(Index));
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync();

        TempData["Message"] = "Categoría eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }


}
