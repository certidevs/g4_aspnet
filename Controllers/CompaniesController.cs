using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using G4AspNet.Data;
using G4AspNet.Models;

namespace G4AspNet.Controllers
{
    public class CompaniesController : Controller
    {
        private readonly ApplicationDbContext context;
        // GET: CompanyController
        public CompaniesController(ApplicationDbContext context)
        {
            this.context = context;
        }
        // GET: /Companies
        public IActionResult Index()
        {
            // Include carga los proyectos de cada empresa para poder mostrar cuántos tiene
            var companies = context.Companies
                .Include(c => c.Projects)
                .OrderBy(c => c.Name)
                .ToList();
            
            return View(companies);
        }

        // GET: /Companies/Details/1
        public IActionResult Details(int id)
        {
            // Incluimos los proyectos y los usuarios de la empresa, que se muestran en el detalle
            var company = context.Companies
                .Include(c => c.Projects)
                .Include(c => c.Users)
                .FirstOrDefault(c => c.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // ===================================================================================
        //  EJEMPLO COMPLETO (issues #10 y #11): Create, Edit, Save y Delete de Company.
        //  Es el modelo a copiar: en los demás controladores el mismo patrón está escrito
        //  dentro de un bloque comentado ("DESCOMENTA DESDE AQUÍ") para que lo ajustéis.
        // ===================================================================================

        // GET: /Companies/Create
        // Muestra el formulario vacío. Crear y Editar comparten la misma vista: Views/Companies/Form.cshtml
        public IActionResult Create()
        {
            // new Company() ya trae IsActive = true (valor por defecto del modelo)
            return View("Form", new Company());
        }

        // GET: /Companies/Edit/1
        // Muestra el mismo formulario, pero relleno con los datos de la empresa.
        // async/await: el servidor no se queda bloqueado mientras la base de datos responde.
        public async Task<IActionResult> Edit(int id)
        {
            // FindAsync busca por clave primaria y devuelve null si no existe
            var company = await context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            return View("Form", company);
        }

        // POST: /Companies/Save
        // Un solo método para crear y para editar: si el Id es 0 la empresa es nueva; si no, ya existe.
        // El formulario envía el Id en un campo oculto. MVC rellena el objeto "company" con los datos del formulario.
        [HttpPost]
        public async Task<IActionResult> Save(Company company)
        {
            // Regla de negocio: el CIF (TaxId) no se puede repetir.
            // Buscamos OTRA empresa (Id distinto) con el mismo CIF; así editar una empresa sin tocar su CIF sí se permite.
            var taxIdRepeated = await context.Companies
                .AnyAsync(c => c.TaxId == company.TaxId && c.Id != company.Id);
            if (taxIdRepeated)
            {
                ModelState.AddModelError(nameof(Company.TaxId), "Ya existe una empresa con ese CIF.");
            }

            // ModelState.IsValid comprueba las validaciones del modelo ([Required], [EmailAddress]...)
            // y los errores que hemos añadido a mano. Si falla, volvemos al formulario con los errores.
            if (!ModelState.IsValid)
            {
                return View("Form", company);
            }

            var isNew = company.Id == 0;
            if (isNew)
            {
                // CREAR: Add marca la empresa nueva para insertarla
                context.Companies.Add(company);
            }
            else
            {
                // EDITAR: cargamos la empresa de la base de datos y le copiamos los campos del formulario
                var existing = await context.Companies.FindAsync(company.Id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.Name = company.Name;
                existing.TaxId = company.TaxId;
                existing.Email = company.Email;
                existing.Phone = company.Phone;
                existing.IsActive = company.IsActive;
            }

            // SaveChangesAsync ejecuta el INSERT o el UPDATE en la base de datos
            await context.SaveChangesAsync();

            // TempData guarda un mensaje para la siguiente petición; el _Layout lo muestra en verde
            TempData["Message"] = isNew ? "Empresa creada correctamente." : "Empresa actualizada correctamente.";

            // Redirigimos (patrón Post-Redirect-Get) para que F5 no vuelva a enviar el formulario
            return RedirectToAction(nameof(Index));
        }

        // POST: /Companies/Delete/1
        // Solo POST: borrar cambia datos, así que nunca se hace con un enlace (GET).
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var company = await context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            // La relación con proyectos y usuarios es Restrict: si borrásemos una empresa que los tiene,
            // la base de datos lanzaría un error (500). Por eso lo comprobamos antes con AnyAsync.
            var hasProjects = await context.Projects.AnyAsync(p => p.CompanyId == id);
            var hasUsers = await context.Users.AnyAsync(u => u.CompanyId == id);
            if (hasProjects || hasUsers)
            {
                // TempData["Error"] lo muestra el _Layout en rojo
                TempData["Error"] = $"No se puede eliminar la empresa «{company.Name}» porque tiene proyectos o usuarios. "
                    + "Puedes desactivarla desde Editar (casilla Activa).";
                return RedirectToAction(nameof(Index));
            }

            context.Companies.Remove(company);
            await context.SaveChangesAsync();

            TempData["Message"] = "Empresa eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
