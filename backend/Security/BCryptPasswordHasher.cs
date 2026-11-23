namespace VmtFabric.Api.Security;

/// <summary>BCrypt con factor de trabajo 12 (S-08). BCrypt solo usa los primeros 72 bytes: el validador lo limita.</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int FactorTrabajo = 12;

    // Hash de referencia para igualar tiempos cuando el usuario no existe.
    private static readonly Lazy<string> HashFicticio =
        new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), FactorTrabajo));

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, FactorTrabajo);

    public bool Verificar(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    public bool NecesitaRehash(string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.PasswordNeedsRehash(hash, FactorTrabajo);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return true;
        }
    }

    public void VerificacionFicticia(string password) => Verificar(password, HashFicticio.Value);
}
