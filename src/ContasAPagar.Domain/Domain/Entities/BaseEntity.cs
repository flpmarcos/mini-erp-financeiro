namespace ContasAPagar.Web.Domain.Entities;

/// <summary>Campos comuns de auditoria leve presentes em todas as entidades.</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}
