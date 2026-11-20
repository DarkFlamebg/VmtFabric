using VmtFabric.Api.Models.Constants;

namespace VmtFabric.Api.Models.Entities;

public class Ingreso
{
    public int IngresoId { get; set; }
    public int AsignacionId { get; set; }
    public DateOnly Periodo { get; set; }           // primer día del mes
    public decimal Monto { get; set; }
    public decimal Costo { get; set; }
    public string Estado { get; set; } = EstadoIngreso.Proyectado;
    public string? NumeroFactura { get; set; }
    public DateOnly? FechaCobro { get; set; }
    public string? Observacion { get; set; }
    public int RegistradoPor { get; set; }
    public DateTime FechaRegistro { get; set; }

    public Asignacion Asignacion { get; set; } = null!;
}
