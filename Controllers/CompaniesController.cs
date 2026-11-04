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
        public IActionResult Index()
        {
            var companies = context.Companies.ToList();
            
            return View(companies);
        }
        public IActionResult Details(int id)
        {
            var company = context.Companies.FirstOrDefault(c => c.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }


    }
}
