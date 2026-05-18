using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agendamentos.Controllers
{
    public class TesteController : Controller
    {
        [Authorize(Roles = "Admin")]
        public IActionResult AdminArea()
        {
            return Content("Área do ADMIN");
        }

        [Authorize(Roles = "Cliente")]
        public IActionResult PacienteArea()
        {
            return Content("Área do CLIENTE");
        }
    }
}
