using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FidelixAPI.BusinessLogic.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de ITokenService </summary>
public class JwtTokenService(IConfiguration configuration) : ITokenService
{
    /// <summary>
    /// Genera un token JWT firmado con el id y el rol como claims
    /// CU-02, flujo principal, paso 4.
    /// </summary>
    public string GenerarToken(Guid id, string rol)
    {
        var claveSecreta = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta la clave del token en la configuracion.");

        var expiracionMinutos = int.Parse(configuration["Jwt:ExpiracionMinutos"]
            ?? throw new InvalidOperationException("Falta Jwt: Expiracion en la configuracion."));

        var claims = new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, rol)
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiracionMinutos),
            signingCredentials: credenciales
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
