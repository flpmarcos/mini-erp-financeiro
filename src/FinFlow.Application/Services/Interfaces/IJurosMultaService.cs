using FinFlow.Domain.Entities;

namespace FinFlow.Services.Interfaces;

/// <summary>Detalhamento do calculo de encargos por atraso.</summary>
public record CalculoEncargos(
    decimal ValorOriginal,
    int DiasAtraso,
    decimal Multa,
    decimal Juros,
    decimal ValorAtualizado);

/// <summary>Calcula multa e juros de contas vencidas (parametros em FinanceiroOptions).</summary>
public interface IJurosMultaService
{
    /// <summary>Calcula encargos de uma conta em determinada data de referencia (default: hoje).</summary>
    CalculoEncargos Calcular(ContaPagar conta, DateTime? referencia = null);
}
