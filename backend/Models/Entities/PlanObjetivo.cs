namespace VmtFabric.Api.Models.Entities;

public class PlanObjetivo
{
    public int PlanObjetivoId { get; set; }
    public int PlanCarreraId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Objetivo";  // Objetivo, Curso, Certificacion, Promocion
    public byte Peso { get; set; } = 1;
    public DateOnly? FechaLimite { get; set; }
    public bool Completado { get; set; }
    public DateOnly? FechaCompletado { get; set; }

    public PlanCarrera PlanCarrera { get; set; } = null!;
}
