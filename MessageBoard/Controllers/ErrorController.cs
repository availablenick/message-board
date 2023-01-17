using Microsoft.AspNetCore.Mvc;

namespace MessageBoard.Controllers;

public class ErrorController : Controller
{
    [HttpGet]
    [Route("/forbidden", Name = "ForbiddenPage")]
    public IActionResult Forbidden()
    {
        return View("403");
    }
}
