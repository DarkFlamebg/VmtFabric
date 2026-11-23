using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Security;

public interface ITokenService
{
    /// <summary>JWT firmado (HS256) con los claims del usuario. El Rol debe venir cargado.</summary>
    (string Token, DateTime Expira) CrearAccessToken(Usuario usuario);

    /// <summary>Token aleatorio para el cliente y su hash SHA-256 para guardar en la BD.</summary>
    (string Token, string Hash, DateTime Expira) CrearRefreshToken();

    string HashRefreshToken(string token);
}
