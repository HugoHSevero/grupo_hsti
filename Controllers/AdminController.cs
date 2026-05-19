using Agendamentos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            // Busca todos os usuários do banco
            var query = _userManager.Users.AsQueryable();

            // Se houver texto na busca, filtra de forma inteligente
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    (u.FirstName != null && u.FirstName.Contains(search)) ||
                    (u.LastName != null && u.LastName.Contains(search)) ||
                    u.Email.Contains(search));
            }

            var users = await query.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userViewModels.Add(new UserViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    IsAdmin = roles.Contains("Admin")
                });
            }

            return View(userViewModels);
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

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!User.IsInRole("Admin"))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(id);
            var currentUser = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            // Segurança máxima: Impedir o Admin de se autoexcluir
            if (user.Id == currentUser.Id)
            {
                TempData["AvisoAdmin"] = "Você não pode excluir a sua própria conta de Administrador.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SucessoAdmin"] = "Usuário excluído com sucesso do sistema.";
            }
            else
            {
                TempData["AvisoAdmin"] = "Ocorreu um erro ao tentar excluir o usuário.";
            }

            return RedirectToAction("Index");
        }
    }
}