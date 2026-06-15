using System.Security.Claims;

namespace DiamondsWeb.Extensions;

/// <summary>
/// Métodos de extensión para extraer claims de Diamonds de forma segura.
/// Centraliza la lógica de parsing de claims para evitar fallbacks silenciosos a admin (IdUsuario=1).
/// </summary>
public static class ClaimExtensions
{
    /// <summary>
    /// Obtiene el IdUsuario del claim. Lanza UnauthorizedAccessException si no existe o no es válido.
    /// Usar en TODOS los handlers que requieran identificar al usuario (auditoría, writes, etc.).
    /// </summary>
    public static int GetRequiredIdUsuario(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("IdUsuario")?.Value
                    ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(claim, out var id))
            return id;

        throw new UnauthorizedAccessException("IdUsuario claim not found. El usuario no tiene un IdUsuario válido en sus claims.");
    }

    /// <summary>
    /// Obtiene el IdTienda del claim. Si no existe, retorna el defaultValue (1 = tienda local, patrón del VB6).
    /// IdTienda puede no estar en claims si el usuario no tiene tienda asignada.
    /// </summary>
    public static int GetIdTienda(this ClaimsPrincipal user, int defaultValue = 1)
    {
        var claim = user.FindFirst("IdTienda")?.Value;
        return int.TryParse(claim, out var id) ? id : defaultValue;
    }
}
