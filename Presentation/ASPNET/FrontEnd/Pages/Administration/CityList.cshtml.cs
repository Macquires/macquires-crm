using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ASPNET.FrontEnd.Pages.Administration;

public class CityListModel : PageModel
{
    public IActionResult OnGet() => Redirect("/Administration/GlobalSettings?tab=cities");
}
