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
            this.context=context;
        }
        public IActionResult Index()
        {
            var categories = context.Companies.ToList();
            
            return View(categories);
        }
        public IActionResult Details(int id)
        {
            var category = context.Categories.Find(id);
            if (category==null)
            {
                return NotFound();
            }
            return View(category);
        }


    }
}
