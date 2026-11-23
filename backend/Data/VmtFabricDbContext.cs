using Microsoft.EntityFrameworkCore;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data;

/// <summary>
/// Contexto EF Core en modo "database-first": el esquema lo define bd/VmtFabric_BD.sql.
/// No se usan migraciones; el mapeo debe mantenerse alineado con el script.
/// </summary>
public class VmtFabricDbContext(DbContextOptions<VmtFabricDbContext> options) : DbContext(options)
{
    // Seguridad
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RecuperacionPassword> RecuperacionesPassword => Set<RecuperacionPassword>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Catálogos
    public DbSet<EstadoColaborador> EstadosColaborador => Set<EstadoColaborador>();
    public DbSet<Seniority> Seniorities => Set<Seniority>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Tecnologia> Tecnologias => Set<Tecnologia>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<ModalidadTarifa> ModalidadesTarifa => Set<ModalidadTarifa>();

    // Talento
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<ColaboradorTecnologia> ColaboradorTecnologias => Set<ColaboradorTecnologia>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
    public DbSet<AsignacionTecnologia> AsignacionTecnologias => Set<AsignacionTecnologia>();
    public DbSet<Ingreso> Ingresos => Set<Ingreso>();
    public DbSet<Documento> Documentos => Set<Documento>();

    // Desarrollo, avisos y auditoría
    public DbSet<PlanCarrera> PlanesCarrera => Set<PlanCarrera>();
    public DbSet<PlanObjetivo> PlanObjetivos => Set<PlanObjetivo>();
    public DbSet<PeriodoEvaluacion> PeriodosEvaluacion => Set<PeriodoEvaluacion>();
    public DbSet<Evaluacion> Evaluaciones => Set<Evaluacion>();
    public DbSet<EvaluacionCriterio> EvaluacionCriterios => Set<EvaluacionCriterio>();
    public DbSet<CriterioPlantilla> CriteriosPlantilla => Set<CriterioPlantilla>();
    public DbSet<Aviso> Avisos => Set<Aviso>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VmtFabricDbContext).Assembly);
    }

    /// <summary>
    /// Las columnas DATETIME2 guardan UTC (SYSUTCDATETIME), pero SQL Server no guarda la zona:
    /// sin esto se leen como Kind=Unspecified y el JSON sale sin "Z", y el navegador las tomaría como hora local.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
