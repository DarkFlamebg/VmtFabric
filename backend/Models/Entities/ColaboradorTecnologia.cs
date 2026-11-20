namespace VmtFabric.Api.Models.Entities;

public class ColaboradorTecnologia
{
    public int ColaboradorId { get; set; }
    public int TecnologiaId { get; set; }
    public byte Nivel { get; set; }                 // 1-5
    public decimal? AniosExperiencia { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
    public Tecnologia Tecnologia { get; set; } = null!;
}
