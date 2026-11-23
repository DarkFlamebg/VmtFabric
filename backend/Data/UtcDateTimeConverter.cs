using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace VmtFabric.Api.Data;

/// <summary>Marca como UTC toda fecha leída de la BD; al guardar convierte a UTC si llegara en hora local.</summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
