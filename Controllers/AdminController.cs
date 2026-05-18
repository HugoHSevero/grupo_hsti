using Agendamentos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Agendamentos.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        // 1. Adicionamos o RoleManager aqui
        private readonly RoleManager<IdentityRole> _roleManager;

        // 2. Atualizamos o construtor para receber o RoleManager
        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(string search)
        {
            var users = _userManager.Users.ToList();

            if (!string.IsNullOrEmpty(search))
            {
                users = users
                    .Where(u => u.Email.Contains(search))
                    .ToList();
            }

            var userList = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userList.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    IsAdmin = roles.Contains("Admin")
                });
            }

            return View(userList);
        }

        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            var currentUser = await _userManager.GetUserAsync(User);

            // Não pode remover o próprio admin
            if (user.Id == currentUser.Id)
            {
                // 1. Criamos a mensagem de aviso aqui!
                TempData["AvisoAdmin"] = "Você não pode remover seus próprios privilégios de Administrador.";
                return RedirectToAction("Index");
            }

            // ... (o resto do código do RoleManager e de trocar as Roles continua igualzinho aqui para baixo) ...
            // =========================================================
            // 3. A MÁGICA: Cria as Roles no banco se elas não existirem
            // =========================================================
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await _roleManager.RoleExistsAsync("Cliente"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Cliente"));
            }
            // =========================================================

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                await _userManager.AddToRoleAsync(user, "Cliente");
            }
            else
            {
                await _userManager.RemoveFromRoleAsync(user, "Cliente");
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            return RedirectToAction("Index");
        }
    }
}