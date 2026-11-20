using VmtFabric.Api.Models.Constants;

namespace VmtFabric.Api.Models.Entities;

public class Asignacion
{
    public int AsignacionId { get; set; }
    public int ColaboradorId { get; set; }
    public int ClienteId { get; set; }
    public int ProyectoId { get; set; }
    public int CargoId { get; set; }
    public byte SeniorityId { get; set; }
    public string? LiderCliente { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public byte ModalidadTarifaId { get; set; }
    public decimal ValorCobrar { get; set; }
    public short? HorasMes { get; set; }
    public decimal CostoColaborador { get; set; }
    public decimal MargenPct { get; private set; }  // columna calculada PERSISTED
    public short AvisarDiasAntes { get; set; } = 30;
    public string Estado { get; set; } = EstadoAsignacion.Activa;
    public DateOnly? FechaCierre { get; set; }
    public string? MotivoCierre { get; set; }
    public string? FuncionesInicio { get; set; }
    public string? FuncionesActuales { get; set; }
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
    public Proyecto Proyecto { get; set; } = null!;
    public Cargo Cargo { get; set; } = null!;
    public Seniority Seniority { get; set; } = null!;
    public ModalidadTarifa ModalidadTarifa { get; set; } = null!;
    public ICollection<AsignacionTecnologia> Tecnologias { get; set; } = new List<AsignacionTecnologia>();
    public ICollection<Ingreso> Ingresos { get; set; } = new List<Ingreso>();
}
