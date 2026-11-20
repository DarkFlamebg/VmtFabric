namespace VmtFabric.Api.Models.Entities;

public class EstadoColaborador
{
    public const byte Asignado = 1;
    public const byte Disponible = 2;
    public const byte Inactivo = 3;

    public byte EstadoColaboradorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
