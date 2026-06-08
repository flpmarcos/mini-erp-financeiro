namespace FinFlow.Helpers;

/// <summary>Pagina de resultados para listagens (paginacao server-side).</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Itens { get; init; } = Array.Empty<T>();
    public int TotalItens { get; init; }
    public int Pagina { get; init; }
    public int TamanhoPagina { get; init; }

    public int TotalPaginas => TamanhoPagina <= 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
    public bool TemAnterior => Pagina > 1;
    public bool TemProxima => Pagina < TotalPaginas;
}
