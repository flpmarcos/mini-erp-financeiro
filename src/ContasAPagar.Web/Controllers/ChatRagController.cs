using System.Security.Claims;
using ContasAPagar.Web.Domain.Identity;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

/// <summary>Assistente RAG (Módulo 25): perguntas em linguagem natural sobre o sistema, com fontes.</summary>
public class ChatRagController : BaseController
{
    private readonly IRagService _rag;
    private readonly IDocumentIngestionService _ingestion;

    public ChatRagController(IRagService rag, IDocumentIngestionService ingestion)
    {
        _rag = rag;
        _ingestion = ingestion;
    }

    private RagUsuario Usuario()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var empresaId = int.TryParse(User.FindFirst("EmpresaId")?.Value, out var e) ? e : 1;
        return new RagUsuario(UsuarioAtual, empresaId, roles);
    }

    public IActionResult Index()
    {
        ViewBag.Sugestoes = new[]
        {
            "Quais contas vencem esta semana?",
            "Temos algum fornecedor bloqueado?",
            "O que diz a política sobre pagamentos acima de R$ 10.000?",
            "Explique o fluxo de aprovação deste sistema.",
            "Quais regras de aprovação existem?"
        };
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Perguntar([FromForm] string pergunta)
    {
        if (string.IsNullOrWhiteSpace(pergunta)) return BadRequest("Pergunta vazia.");
        var r = await _rag.PerguntarAsync(pergunta, Usuario());
        return Json(new
        {
            resposta = r.Resposta,
            trechos = r.TrechosUsados,
            fontes = r.Fontes.Select(f => new { f.Fonte, f.Tipo, f.Entidade })
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.Administrar)]
    public async Task<IActionResult> Reindexar()
    {
        var n = await _ingestion.IngerirBaseAsync();
        Sucesso($"Base reindexada: {n} trechos.");
        return RedirectToAction(nameof(Index));
    }
}
