using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VmtFabric.Api.Models.Constants;
using VmtFabric.Api.Models.Entities;
using VmtFabric.Api.Security;

namespace VmtFabric.Api.Data.Interceptors;

/// <summary>
/// Auditoría automática (S-13 / RNF-07): por cada entidad creada, editada o eliminada en un
/// SaveChanges agrega una fila en <c>Auditoria</c> con el usuario y los cambios en JSON.
/// También completa <c>CreadoPor</c>, <c>ModificadoPor</c> y <c>FechaModificacion</c>.
/// </summary>
public sealed class AuditoriaInterceptor(ICurrentUser currentUser, TimeProvider time) : SaveChangesInterceptor
{
    // Tablas técnicas que no se auditan (o que se auditan con eventos explícitos).
    private static readonly HashSet<Type> TiposExcluidos = [typeof(Auditoria), typeof(RefreshToken), typeof(RecuperacionPassword)];

    // Columnas que cambian en cada login: si solo cambian estas, no se audita la edición.
    private static readonly HashSet<string> PropiedadesIgnoradas =
        ["UltimoAcceso", "IntentosFallidos", "BloqueadoHasta", "FechaModificacion", "ModificadoPor"];

    // Nunca se guarda su valor: solo que cambió.
    private static readonly HashSet<string> PropiedadesSensibles = ["PasswordHash", "TokenHash"];

    private const string Oculto = "***";

    private List<(EntityEntry Entry, string Accion, Dictionary<string, object?> Detalle)>? _pendientes;

    // true mientras corre el SaveChanges interno que guarda las filas de Auditoria.
    private bool _guardandoAuditoria;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Preparar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Preparar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var context = eventData.Context;
        if (context is not null && AgregarRegistros(context))
        {
            _guardandoAuditoria = true;
            try { context.SaveChanges(); }
            finally { _guardandoAuditoria = false; }
        }
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is not null && AgregarRegistros(context))
        {
            _guardandoAuditoria = true;
            try { await context.SaveChangesAsync(cancellationToken); }
            finally { _guardandoAuditoria = false; }
        }
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pendientes = null;
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendientes = null;
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Preparar(DbContext? context)
    {
        if (context is null || _guardandoAuditoria)
            return;

        var ahora = time.GetUtcNow().UtcDateTime;
        var usuarioId = currentUser.UsuarioId;
        var lista = new List<(EntityEntry, string, Dictionary<string, object?>)>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (TiposExcluidos.Contains(entry.Metadata.ClrType))
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    CompletarCreador(entry, usuarioId);
                    lista.Add((entry, AccionAuditoria.Crear, Valores(entry)));
                    break;

                case EntityState.Modified:
                    var cambios = Cambios(entry);
                    if (cambios.Count == 0)
                        continue;
                    CompletarModificacion(entry, usuarioId, ahora);
                    lista.Add((entry, AccionAuditoria.Editar, cambios));
                    break;

                case EntityState.Deleted:
                    lista.Add((entry, AccionAuditoria.Eliminar, []));
                    break;
            }
        }

        _pendientes = lista.Count > 0 ? lista : null;
    }

    /// <summary>Convierte lo pendiente en filas de Auditoria (los Id generados ya existen).</summary>
    private bool AgregarRegistros(DbContext context)
    {
        if (_guardandoAuditoria || _pendientes is null)
            return false;

        var usuarioId = currentUser.UsuarioId;
        foreach (var (entry, accion, detalle) in _pendientes)
        {
            context.Set<Auditoria>().Add(new Auditoria
            {
                UsuarioId = usuarioId,
                Accion = accion,
                Entidad = entry.Metadata.ClrType.Name,
                EntidadId = ClavePrimaria(entry),
                Detalle = detalle.Count > 0 ? JsonSerializer.Serialize(detalle) : null
            });
        }

        _pendientes = null;
        return true;
    }

    private static int? ClavePrimaria(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is not { Properties.Count: 1 })
            return null;

        return entry.Property(key.Properties[0].Name).CurrentValue switch
        {
            int i => i,
            byte b => b,
            short s => s,
            _ => null
        };
    }

    private static void CompletarCreador(EntityEntry entry, int? usuarioId)
    {
        if (usuarioId is null || entry.Metadata.FindProperty("CreadoPor") is null)
            return;

        var prop = entry.Property("CreadoPor");
        if (prop.CurrentValue is null or 0)
            prop.CurrentValue = usuarioId.Value;
    }

    private static void CompletarModificacion(EntityEntry entry, int? usuarioId, DateTime ahora)
    {
        if (entry.Metadata.FindProperty("FechaModificacion") is not null)
            entry.Property("FechaModificacion").CurrentValue = ahora;
        if (usuarioId is not null && entry.Metadata.FindProperty("ModificadoPor") is not null)
            entry.Property("ModificadoPor").CurrentValue = usuarioId.Value;
    }

    private static Dictionary<string, object?> Valores(EntityEntry entry) =>
        entry.Properties
            .Where(p => !p.Metadata.IsPrimaryKey() && !p.Metadata.IsShadowProperty())
            .ToDictionary(p => p.Metadata.Name,
                p => PropiedadesSensibles.Contains(p.Metadata.Name) ? Oculto : p.CurrentValue);

    private static Dictionary<string, object?> Cambios(EntityEntry entry) =>
        entry.Properties
            .Where(p => p.IsModified
                && !PropiedadesIgnoradas.Contains(p.Metadata.Name)
                && !Equals(p.OriginalValue, p.CurrentValue))
            .ToDictionary(p => p.Metadata.Name, p => PropiedadesSensibles.Contains(p.Metadata.Name)
                ? (object?)new { antes = Oculto, despues = Oculto }
                : new { antes = p.OriginalValue, despues = p.CurrentValue });
}
