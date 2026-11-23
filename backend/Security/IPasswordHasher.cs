namespace VmtFabric.Api.Security;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Devuelve false también si el hash guardado no es válido (p. ej. el marcador del script).</summary>
    bool Verificar(string password, string hash);

    bool NecesitaRehash(string hash);

    /// <summary>Consume el mismo tiempo que una verificación real: evita revelar si un correo existe.</summary>
    void VerificacionFicticia(string password);
}
