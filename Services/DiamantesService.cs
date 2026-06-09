using System.Data;
using System.Text;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para consulta de diamantes desde la vista vDiamantes.
/// Migración de frmDiamantes.frm (VB6) que usaba AutoBusquedaFlex + ActualizarFLEX.
/// </summary>
public class DiamantesService
{
    private readonly string _connectionString;
    private readonly ILogger<DiamantesService> _logger;

    public DiamantesService(string connectionString, ILogger<DiamantesService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Obtiene diamantes con filtros opcionales.
    /// Replica la funcionalidad de AutoBusquedaFlex sobre vDiamantes.
    /// </summary>
    public async Task<List<Diamante>> ObtenerDiamantesAsync(DiamanteFiltros? filtros = null)
    {
        var sb = new StringBuilder();
        sb.Append(@"SELECT IdLocalizacion, NombreStatus, Corte, Corte2, Quilates,
                           Color, Pureza, Obs2, Precio, Descripcion, Obs1,
                           CodigoBarras, Proveedor, IdTienda, CBPadre, Grupo
                    FROM vDiamantes WHERE 1=1");

        var parameters = new DynamicParameters();

        if (filtros != null)
        {
            if (!string.IsNullOrWhiteSpace(filtros.Busqueda))
            {
                sb.Append(@" AND (Descripcion LIKE @Busqueda
                            OR Obs1 LIKE @Busqueda
                            OR Obs2 LIKE @Busqueda
                            OR CodigoBarras LIKE @Busqueda
                            OR Corte LIKE @Busqueda
                            OR Color LIKE @Busqueda
                            OR Pureza LIKE @Busqueda
                            OR Grupo LIKE @Busqueda)");
                parameters.Add("Busqueda", $"%{filtros.Busqueda}%");
            }

            if (!string.IsNullOrWhiteSpace(filtros.Corte))
            {
                sb.Append(" AND Corte = @Corte");
                parameters.Add("Corte", filtros.Corte);
            }

            if (!string.IsNullOrWhiteSpace(filtros.Color))
            {
                sb.Append(" AND Color = @Color");
                parameters.Add("Color", filtros.Color);
            }

            if (!string.IsNullOrWhiteSpace(filtros.Pureza))
            {
                sb.Append(" AND Pureza = @Pureza");
                parameters.Add("Pureza", filtros.Pureza);
            }

            if (!string.IsNullOrWhiteSpace(filtros.Status))
            {
                sb.Append(" AND NombreStatus = @Status");
                parameters.Add("Status", filtros.Status);
            }

            if (filtros.QuilatesMin.HasValue)
            {
                sb.Append(" AND Quilates >= @QuilatesMin");
                parameters.Add("QuilatesMin", filtros.QuilatesMin.Value);
            }

            if (filtros.QuilatesMax.HasValue)
            {
                sb.Append(" AND Quilates <= @QuilatesMax");
                parameters.Add("QuilatesMax", filtros.QuilatesMax.Value);
            }

            if (filtros.PrecioMin.HasValue)
            {
                sb.Append(" AND Precio >= @PrecioMin");
                parameters.Add("PrecioMin", filtros.PrecioMin.Value);
            }

            if (filtros.PrecioMax.HasValue)
            {
                sb.Append(" AND Precio <= @PrecioMax");
                parameters.Add("PrecioMax", filtros.PrecioMax.Value);
            }
        }

        sb.Append(" ORDER BY NombreStatus, Corte, Quilates, Color DESC, Pureza DESC");

        try
        {
            using var conn = CreateConnection();
            var result = await conn.QueryAsync<Diamante>(sb.ToString(), parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando diamantes con filtros: {@Filtros}", filtros);
            throw;
        }
    }

    /// <summary>
    /// Obtiene los valores distintos de corte para el filtro dropdown
    /// </summary>
    public async Task<List<string>> ObtenerCortesAsync()
    {
        const string sql = @"SELECT Corte FROM vDiamantes
                             GROUP BY Corte ORDER BY Corte";
        try
        {
            using var conn = CreateConnection();
            var result = await conn.QueryAsync<string>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo cortes");
            throw;
        }
    }

    /// <summary>
    /// Obtiene los valores distintos de color para el filtro dropdown
    /// </summary>
    public async Task<List<string>> ObtenerColoresAsync()
    {
        const string sql = @"SELECT Color FROM vDiamantes
                             GROUP BY Color ORDER BY Color";
        try
        {
            using var conn = CreateConnection();
            var result = await conn.QueryAsync<string>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo colores");
            throw;
        }
    }

    /// <summary>
    /// Obtiene los valores distintos de pureza para el filtro dropdown
    /// </summary>
    public async Task<List<string>> ObtenerPurezasAsync()
    {
        const string sql = @"SELECT Pureza FROM vDiamantes
                             GROUP BY Pureza ORDER BY Pureza";
        try
        {
            using var conn = CreateConnection();
            var result = await conn.QueryAsync<string>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo purezas");
            throw;
        }
    }

    /// <summary>
    /// Obtiene los valores distintos de status para el filtro dropdown
    /// </summary>
    public async Task<List<string>> ObtenerStatusAsync()
    {
        const string sql = @"SELECT NombreStatus FROM vDiamantes
                             GROUP BY NombreStatus ORDER BY NombreStatus";
        try
        {
            using var conn = CreateConnection();
            var result = await conn.QueryAsync<string>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo status");
            throw;
        }
    }
}
