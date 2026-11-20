namespace VmtFabric.Api.Models.Entities;

public class PeriodoEvaluacion
{
    public int PeriodoEvaluacionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
}
