using VmtFabric.Api.Models.Constants;

namespace VmtFabric.Api.Models.Entities;

public class Evaluacion
{
    public int EvaluacionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int PeriodoEvaluacionId { get; set; }
    public int ColaboradorId { get; set; }
    public int EvaluadorUsuarioId { get; set; }
    public byte EscalaMin { get; set; } = 1;
    public byte EscalaMax { get; set; } = 5;
    public DateOnly? FechaLimite { get; set; }
    public string Estado { get; set; } = EstadoEvaluacion.Pendiente;
    public decimal? PuntajeFinal { get; set; }      // 0-100
    public string? ComentarioGeneral { get; set; }
    public DateTime? FechaInicioRespuesta { get; set; }
    public DateTime? FechaCompletada { get; set; }
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }

    public PeriodoEvaluacion Periodo { get; set; } = null!;
    public Colaborador Colaborador { get; set; } = null!;
    public Usuario Evaluador { get; set; } = null!;
    public ICollection<EvaluacionCriterio> Criterios { get; set; } = new List<EvaluacionCriterio>();
}
