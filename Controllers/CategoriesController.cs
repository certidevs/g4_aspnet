using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using G4AspNet.Data;
namespace G4AspNet.Controllers;

public class CategoriesController : Controller
{

    private readonly ApplicationDbContext context;

    public CategoriesController(ApplicationDbContext context)
    {
        this.context = context;
    }


    public IActionResult Index()
    {

        var categories = context.Categories.ToList();
            return View(categories);
    }


}

