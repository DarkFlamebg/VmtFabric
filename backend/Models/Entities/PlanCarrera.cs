using VmtFabric.Api.Models.Constants;

namespace VmtFabric.Api.Models.Entities;

public class PlanCarrera
{
    public int PlanCarreraId { get; set; }
    public int ColaboradorId { get; set; }
    public byte SeniorityActualId { get; set; }
    public byte SeniorityObjetivoId { get; set; }
    public int? CargoObjetivoId { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaObjetivo { get; set; }
    public string Estado { get; set; } = EstadoPlanCarrera.Vigente;
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
    public Seniority SeniorityActual { get; set; } = null!;
    public Seniority SeniorityObjetivo { get; set; } = null!;
    public Cargo? CargoObjetivo { get; set; }
    public ICollection<PlanObjetivo> Objetivos { get; set; } = new List<PlanObjetivo>();
}
