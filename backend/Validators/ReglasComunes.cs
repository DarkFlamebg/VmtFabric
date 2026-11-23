using System.Text;
using FluentValidation;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Validators;

/// <summary>Reglas reutilizables. Las longitudes son las de bd/VmtFabric_BD.sql (S-04).</summary>
public static class ReglasComunes
{
    public const int MaxNombreUsuario = 120;
    public const int MaxCorreo = 150;
    public const int MinPassword = 8;
    public const int MaxPassword = 64;

    // BCrypt ignora todo lo que pase de 72 bytes.
    private const int MaxBytesBcrypt = 72;

    /// <summary>8-64 caracteres, al menos una letra y un dígito.</summary>
    public static IRuleBuilderOptions<T, string> PasswordSegura<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("La contraseña es obligatoria.")
            .Length(MinPassword, MaxPassword).WithMessage($"La contraseña debe tener entre {MinPassword} y {MaxPassword} caracteres.")
            .Must(p => Encoding.UTF8.GetByteCount(p) <= MaxBytesBcrypt).WithMessage("La contraseña es demasiado larga.")
            .Must(p => p.Any(char.IsLetter)).WithMessage("La contraseña debe contener al menos una letra.")
            .Must(p => p.Any(char.IsDigit)).WithMessage("La contraseña debe contener al menos un número.");

    public static IRuleBuilderOptions<T, string> CorreoValido<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(MaxCorreo).WithMessage($"El correo admite como máximo {MaxCorreo} caracteres.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");

    public static IRuleBuilderOptions<T, string> NombreUsuario<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(MaxNombreUsuario).WithMessage($"El nombre admite como máximo {MaxNombreUsuario} caracteres.");

    public static IRuleBuilderOptions<T, byte> RolValido<T>(this IRuleBuilder<T, byte> rule) =>
        rule.Must(r => r is Rol.Administrador or Rol.Gestor).WithMessage("El rol no es válido.");
}
