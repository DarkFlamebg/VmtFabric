namespace VmtFabric.Api.Models.Entities;

public class EvaluacionCriterio
{
    public int EvaluacionCriterioId { get; set; }
    public int EvaluacionId { get; set; }
    public byte Orden { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Peso { get; set; } = 1;
    public byte? Puntaje { get; set; }
    public string? Comentario { get; set; }

    public Evaluacion Evaluacion { get; set; } = null!;
}
