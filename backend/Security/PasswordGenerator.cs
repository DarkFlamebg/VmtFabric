using System.Security.Cryptography;

namespace VmtFabric.Api.Security;

/// <summary>Contraseñas temporales aleatorias (CSPRNG) que cumplen la política: letras, dígitos y símbolo.</summary>
public static class PasswordGenerator
{
    // Sin caracteres ambiguos (0/O, 1/l/I) para dictarla sin errores.
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnopqrstuvwxyz";
    private const string Digitos = "23456789";
    private const string Simbolos = "!@#$%*?-_";
    private const string Todos = Mayusculas + Minusculas + Digitos + Simbolos;

    public static string Generar(int longitud = 14)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(longitud, 8);

        var chars = new char[longitud];
        chars[0] = Mayusculas[RandomNumberGenerator.GetInt32(Mayusculas.Length)];
        chars[1] = Minusculas[RandomNumberGenerator.GetInt32(Minusculas.Length)];
        chars[2] = Digitos[RandomNumberGenerator.GetInt32(Digitos.Length)];
        chars[3] = Simbolos[RandomNumberGenerator.GetInt32(Simbolos.Length)];
        for (var i = 4; i < longitud; i++)
            chars[i] = Todos[RandomNumberGenerator.GetInt32(Todos.Length)];

        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }
}
