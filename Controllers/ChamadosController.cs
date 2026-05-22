using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Agendamentos.Data;
// (Se houver outros usings de Models, mantenha-os aqui)

public class ChamadosController : Controller
{
    private readonly ApplicationDbContext _context;

    public ChamadosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // LISTA
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var chamados = await _context.Chamados
            .Where(c => c.UsuarioId == userId)
            // 1. ADICIONADO AQUI: Ordena os chamados do usuário do mais novo pro mais antigo
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync();

        return View(chamados);
    }

    // LISTA TODOS OS CHAMADOS
    public async Task<IActionResult> Backlog(StatusChamado? status, string search)
    {
        if (!User.IsInRole("Admin"))
            return Unauthorized();

        var chamados = _context.Chamados
            .Include(c => c.Usuario)
            .AsQueryable();

        // Filtro 1: Status
        if (status.HasValue)
        {
            chamados = chamados.Where(c => c.Status == status.Value);
        }

        // Filtro 2: Busca por texto (Título do chamado ou Nome/Sobrenome do usuário)
        if (!string.IsNullOrEmpty(search))
        {
            chamados = chamados.Where(c =>
                c.Titulo.Contains(search) ||
                (c.Usuario != null && c.Usuario.FirstName.Contains(search)) ||
                (c.Usuario != null && c.Usuario.LastName.Contains(search))
            );
        }

        // Passamos o texto de busca para a tela para a caixinha não ficar em branco após buscar
        ViewData["CurrentSearch"] = search;

        return View(await chamados.OrderBy(c => c.DataCriacao).ToListAsync());
    }
    // CREATE (GET)
    public IActionResult Create()
    {
        return View();
    }

    // CREATE (POST)
    [HttpPost]
    public async Task<IActionResult> Create(Chamado chamado)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        chamado.UsuarioId = userId;
        chamado.Status = StatusChamado.Aberto;

        // ATENÇÃO: Definição da prioridade ANTES de salvar no banco!
        chamado.Prioridade = PrioridadeChamado.Baixa;

        // 1. Pega a hora global neutra
        var horaGlobal = DateTime.UtcNow;
        TimeZoneInfo fusoHorarioBrasil;

        // 2. Tenta pegar o fuso do Linux (Railway), se falhar, pega o do Windows (Seu PC local)
        try
        {
            fusoHorarioBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            fusoHorarioBrasil = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }

        // 3. Converte a hora global para a hora local do Brasil
        chamado.DataCriacao = TimeZoneInfo.ConvertTimeFromUtc(horaGlobal, fusoHorarioBrasil);

        // 4. Salva no banco de dados com a hora e a prioridade corretas
        _context.Add(chamado);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(int id)
    {
        var chamado = await _context.Chamados
            .Include(c => c.Usuario)
            .Include(c => c.Mensagens)
            .ThenInclude(m => m.Usuario)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chamado == null)
            return NotFound();

        return View(chamado);
    }

    [HttpPost]
    public async Task<IActionResult> AlterarStatus(int id, StatusChamado status)
    {
        var chamado = await _context.Chamados.FindAsync(id);

        if (chamado == null)
            return NotFound();

        chamado.Status = status;

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = id });
    }

    [HttpPost]
    public async Task<IActionResult> EnviarMensagem(int chamadoId, string conteudo)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Pega a hora global neutra (Livre de fuso horário)
        var horaGlobal = DateTime.UtcNow;
        TimeZoneInfo fusoHorarioBrasil;

        // 2. Tenta pegar o fuso do Linux (Railway), se falhar, pega o do Windows (Seu PC local)
        try
        {
            fusoHorarioBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            fusoHorarioBrasil = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }

        var mensagem = new MensagemChamado
        {
            ChamadoId = chamadoId,
            Conteudo = conteudo,

            // 3. Aplica a hora matematicamente convertida para o Brasil
            DataEnvio = TimeZoneInfo.ConvertTimeFromUtc(horaGlobal, fusoHorarioBrasil),

            UsuarioId = userId
        };

        _context.MensagensChamado.Add(mensagem);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = chamadoId });
    }
    [HttpPost]
    public async Task<IActionResult> Deletar(int id)
    {
        // Segurança máxima: Se não for Admin, barra na hora
        if (!User.IsInRole("Admin"))
        {
            return Unauthorized();
        }

        var chamado = await _context.Chamados
            .Include(c => c.Mensagens)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chamado == null)
            return NotFound();

        // Remove as mensagens atreladas antes de apagar o chamado
        if (chamado.Mensagens != null && chamado.Mensagens.Any())
        {
            _context.MensagensChamado.RemoveRange(chamado.Mensagens);
        }

        _context.Chamados.Remove(chamado);
        await _context.SaveChangesAsync();

        // Como apenas admin apaga, sempre redireciona para o Backlog
        return RedirectToAction("Backlog");
    }
}