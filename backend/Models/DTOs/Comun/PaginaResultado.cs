namespace VmtFabric.Api.Models.DTOs.Comun;

/// <summary>Resultado paginado del lado del servidor (M-08).</summary>
public sealed record PaginaResultado<T>(IReadOnlyList<T> Items, int Total, int Pagina, int TamanoPagina);
