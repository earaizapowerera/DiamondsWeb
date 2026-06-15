using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Maps UserPortal identity to Diamonds legacy user (Usuarios table).
/// Adds "IdUsuario" and "IdTienda" claims so pages can read them
/// via User.FindFirst("IdUsuario") instead of hardcoding.
/// </summary>
public class DiamondsClaimsTransformation : IClaimsTransformation
{
    private readonly string _connectionString;
    private readonly int _defaultTiendaId;
    private readonly ILogger<DiamondsClaimsTransformation> _logger;

    public DiamondsClaimsTransformation(
        IConfiguration configuration,
        ILogger<DiamondsClaimsTransformation> logger)
    {
        _connectionString = configuration.GetConnectionString("DiamondsDb")!;
        _defaultTiendaId = configuration.GetValue("Diamonds:DefaultTiendaId", 1);
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        if (principal.HasClaim(c => c.Type == "IdUsuario"))
            return principal;

        var username = principal.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return principal;

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            var user = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 IdUsuario FROM Usuarios WHERE Nombre = @Username",
                new { Username = username });

            if (user == null)
            {
                // Try case-insensitive match
                user = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 IdUsuario FROM Usuarios WHERE LOWER(Nombre) = LOWER(@Username)",
                    new { Username = username });
            }

            var identity = (ClaimsIdentity)principal.Identity!;

            if (user != null)
            {
                var idUsuario = (int)user.IdUsuario;
                identity.AddClaim(new Claim("IdUsuario", idUsuario.ToString()));
                _logger.LogDebug("Mapped UserPortal user '{Username}' to Diamonds IdUsuario={Id}",
                    username, idUsuario);
            }
            else
            {
                _logger.LogWarning(
                    "No Diamonds user found for UserPortal username '{Username}'. " +
                    "Pages requiring IdUsuario will fail.", username);
            }

            identity.AddClaim(new Claim("IdTienda", _defaultTiendaId.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve Diamonds user for '{Username}'", username);
        }

        return principal;
    }
}
