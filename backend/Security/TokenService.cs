using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VmtFabric.Api.Configuration;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Security;

public sealed class TokenService(IOptions<JwtSettings> options, TimeProvider time) : ITokenService
{
    private readonly JwtSettings _jwt = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTime Expira) CrearAccessToken(Usuario usuario)
    {
        var ahora = time.GetUtcNow().UtcDateTime;
        var expira = ahora.AddMinutes(_jwt.AccessTokenMinutos);

        var claims = new List<Claim>
        {
            new(ClaimNames.UsuarioId, usuario.UsuarioId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimNames.Nombre, usuario.Nombre),
            new(ClaimNames.Correo, usuario.Correo),
            new(ClaimNames.Rol, usuario.Rol.Nombre),
            new(ClaimNames.TokenId, Guid.NewGuid().ToString("N"))
        };
        if (usuario.DebeCambiarPassword)
            claims.Add(new Claim(ClaimNames.DebeCambiarPassword, "true"));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = expira,
            SigningCredentials = new SigningCredentials(ClaveFirma(_jwt.Key), SecurityAlgorithms.HmacSha256)
        });
        return (token, expira);
    }

    public (string Token, string Hash, DateTime Expira) CrearRefreshToken()
    {
        // 256 bits aleatorios; al cliente va el valor, a la BD solo el hash (S-08).
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var expira = time.GetUtcNow().UtcDateTime.AddDays(_jwt.RefreshTokenDias);
        return (token, HashRefreshToken(token), expira);
    }

    public string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static SymmetricSecurityKey ClaveFirma(string key) => new(Encoding.UTF8.GetBytes(key));
}
