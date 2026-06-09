using System.Data;
using System.Text;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio de consulta de notas de venta.
/// Migrado de frmConsultaNotas.frm (VB6).
/// Usa queries parametrizados para prevenir SQL injection.
/// </summary>
public class NotasService
{
    private readonly string _connectionString;
    private readonly ILogger<NotasService> _logger;

    public NotasService(string connectionString, ILogger<NotasService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Busca notas con filtros avanzados (replica cmdBusqueda_Click del VB6).
    /// Usa parametros para evitar SQL injection (el original concatenaba strings).
    /// </summary>
    public async Task<List<NotaVenta>> BuscarNotasAsync(NotasFiltro filtro)
    {
        var where = new StringBuilder();
        var parameters = new DynamicParameters();

        // Filtros directos sobre vbajasnotas
        if (filtro.FechaDesde.HasValue)
        {
            where.Append(" AND fechabaja >= @FechaDesde");
            parameters.Add("FechaDesde", filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            where.Append(" AND fechabaja <= @FechaHasta");
            // Incluir todo el dia final
            parameters.Add("FechaHasta", filtro.FechaHasta.Value.Date.AddDays(1).AddSeconds(-1));
        }

        if (!string.IsNullOrWhiteSpace(filtro.NombreCliente))
        {
            where.Append(" AND NombreCliente LIKE @NombreCliente");
            parameters.Add("NombreCliente", $"%{filtro.NombreCliente}%");
        }

        // Subquery sobre vbajaspiezas (filtros de pieza)
        var subWhere = BuildPiezaSubquery(filtro, parameters);
        if (subWhere.Length > 0)
        {
            where.Append($" AND idnota IN (SELECT idnota FROM vbajaspiezas WHERE {subWhere})");
        }

        var whereClause = where.Length > 0 ? "WHERE " + where.ToString()[5..] : "";

        var sql = $@"SELECT TOP 500 IdNota, NombreCliente, Telefonos, Bruto, Descuento, Neto,
                     IdUsuario, FormaPago, FechaCaptura, FechaBaja, Comentarios
                     FROM vbajasnotas {whereClause}
                     ORDER BY IdNota DESC";

        _logger.LogInformation("BuscarNotas: {Where}", whereClause);

        using var db = CreateConnection();
        var result = await db.QueryAsync<NotaVenta>(sql, parameters);
        return result.ToList();
    }

    /// <summary>
    /// Obtiene piezas de una nota (modo sencilla: una sola nota).
    /// Replica el query de Refrescar() optSencilla del VB6.
    /// </summary>
    public async Task<List<PiezaNota>> ObtenerPiezasNotaAsync(int idNota)
    {
        const string sql = @"
            SELECT pn.IdPiezaNota, pn.IdNota, pn.CodigoBarras, pn.Descripcion,
                   pn.Cantidad, pn.Subtotal, pn.Total, pn.FechaBaja,
                   vbp.Proveedor, vbp.CNTotal, vbp.IdMoneda, vbp.Precio
            FROM piezasnotas pn
            LEFT JOIN vbajaspiezas vbp ON vbp.CodigoBarras = pn.CodigoBarras
                AND vbp.IdNota = pn.IdNota
            WHERE pn.IdNota = @IdNota";

        using var db = CreateConnection();
        var result = await db.QueryAsync<PiezaNota>(sql, new { IdNota = idNota });
        return result.ToList();
    }

    /// <summary>
    /// Obtiene los pagos de una nota (bajaspagosnotas + opcionespago).
    /// </summary>
    public async Task<List<PagoNota>> ObtenerPagosNotaAsync(int idNota)
    {
        const string sql = @"
            SELECT bp.IdNota, bp.IdOpcionPago, op.OpcionPago,
                   bp.Importe, bp.TipoCambio, bp.ImporteOriginal, bp.FechaCaptura
            FROM bajaspagosnotas bp
            INNER JOIN opcionespago op ON op.IdOpcionPago = bp.IdOpcionPago
            WHERE bp.IdNota = @IdNota";

        using var db = CreateConnection();
        var result = await db.QueryAsync<PagoNota>(sql, new { IdNota = idNota });
        return result.ToList();
    }

    /// <summary>
    /// Obtiene totales de costo neto agrupados por moneda para una nota.
    /// </summary>
    public async Task<List<CostoNetoPorMoneda>> ObtenerTotalesCostoNetoAsync(int idNota)
    {
        const string sql = @"
            SELECT SUM(vbp.CNTotal) AS CostoNeto, m.Moneda
            FROM vbajaspiezas vbp
            INNER JOIN monedas m ON m.IdMoneda = vbp.IdMoneda
            WHERE vbp.IdNota = @IdNota
            GROUP BY m.Moneda";

        using var db = CreateConnection();
        var result = await db.QueryAsync<CostoNetoPorMoneda>(sql, new { IdNota = idNota });
        return result.ToList();
    }

    /// <summary>
    /// Obtiene la suma de Neto de las notas filtradas.
    /// </summary>
    public async Task<decimal> ObtenerSumaNetoAsync(NotasFiltro filtro)
    {
        var where = new StringBuilder();
        var parameters = new DynamicParameters();

        if (filtro.FechaDesde.HasValue)
        {
            where.Append(" AND fechabaja >= @FechaDesde");
            parameters.Add("FechaDesde", filtro.FechaDesde.Value);
        }
        if (filtro.FechaHasta.HasValue)
        {
            where.Append(" AND fechabaja <= @FechaHasta");
            parameters.Add("FechaHasta", filtro.FechaHasta.Value.Date.AddDays(1).AddSeconds(-1));
        }
        if (!string.IsNullOrWhiteSpace(filtro.NombreCliente))
        {
            where.Append(" AND NombreCliente LIKE @NombreCliente");
            parameters.Add("NombreCliente", $"%{filtro.NombreCliente}%");
        }

        var subWhere = BuildPiezaSubquery(filtro, parameters);
        if (subWhere.Length > 0)
        {
            where.Append($" AND idnota IN (SELECT idnota FROM vbajaspiezas WHERE {subWhere})");
        }

        var whereClause = where.Length > 0 ? "WHERE " + where.ToString()[5..] : "";
        var sql = $"SELECT ISNULL(SUM(Neto), 0) FROM vbajasnotas {whereClause}";

        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<decimal>(sql, parameters);
    }

    /// <summary>
    /// Cancela una nota ejecutando el SP restaurarnota.
    /// Mueve la nota de bajasnotas de vuelta a notas (sesion abierta).
    /// IMPORTANTE: Verifica que no haya sesion abierta del usuario.
    /// </summary>
    public async Task<(bool Success, string Message)> CancelarNotaAsync(int idNota, int idUsuario)
    {
        using var db = CreateConnection();

        // Verificar que no haya sesion abierta (misma logica del VB6)
        var sesionAbierta = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM notas WHERE idusuario = @IdUsuario",
            new { IdUsuario = idUsuario });

        if (sesionAbierta > 0)
        {
            return (false, "Primero se debe cerrar la sesion que tiene el usuario abierta. " +
                           "Solo se permite una sesion por usuario al mismo tiempo. Cierre el punto de venta.");
        }

        // Verificar que la nota exista
        var notaExiste = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM bajasnotas WHERE idnota = @IdNota",
            new { IdNota = idNota });

        if (notaExiste == 0)
        {
            return (false, $"La nota {idNota} no existe o ya fue cancelada.");
        }

        try
        {
            await db.ExecuteAsync("restaurarnota @IdNota", new { IdNota = idNota });
            _logger.LogWarning("Nota {IdNota} cancelada (restaurada) por usuario {IdUsuario}", idNota, idUsuario);
            return (true, $"Nota {idNota} cancelada exitosamente. La sesion fue reabierta.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar nota {IdNota}", idNota);
            return (false, $"Error al cancelar la nota: {ex.Message}");
        }
    }

    /// <summary>
    /// Construye la parte WHERE del subquery de piezas con parametros.
    /// </summary>
    private static StringBuilder BuildPiezaSubquery(NotasFiltro filtro, DynamicParameters parameters)
    {
        var sub = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(filtro.CodigoBarras))
        {
            sub.Append(" AND codigobarras LIKE @CB");
            parameters.Add("CB", $"{filtro.CodigoBarras}%");
        }
        if (!string.IsNullOrWhiteSpace(filtro.Proveedor))
        {
            sub.Append(" AND proveedor = @Proveedor");
            parameters.Add("Proveedor", filtro.Proveedor);
        }
        if (!string.IsNullOrWhiteSpace(filtro.DescripcionPieza))
        {
            sub.Append(" AND descripcion LIKE @DescPieza");
            parameters.Add("DescPieza", $"%{filtro.DescripcionPieza}%");
        }
        if (!string.IsNullOrWhiteSpace(filtro.Grupo))
        {
            sub.Append(" AND grupo LIKE @Grupo");
            parameters.Add("Grupo", $"%{filtro.Grupo}%");
        }
        if (!string.IsNullOrWhiteSpace(filtro.IdLocalizacion))
        {
            sub.Append(" AND idlocalizacion = @IdLoc");
            parameters.Add("IdLoc", filtro.IdLocalizacion);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Modelo))
        {
            sub.Append(" AND modelo LIKE @Modelo");
            parameters.Add("Modelo", $"%{filtro.Modelo}%");
        }
        if (!string.IsNullOrWhiteSpace(filtro.Serie))
        {
            sub.Append(" AND numserie LIKE @Serie");
            parameters.Add("Serie", $"%{filtro.Serie}%");
        }
        if (filtro.PesoDesde.HasValue)
        {
            sub.Append(" AND peso >= @PesoDesde");
            parameters.Add("PesoDesde", filtro.PesoDesde.Value);
        }
        if (filtro.PesoHasta.HasValue)
        {
            sub.Append(" AND peso <= @PesoHasta");
            parameters.Add("PesoHasta", filtro.PesoHasta.Value);
        }
        if (filtro.QuilatesDesde.HasValue)
        {
            sub.Append(" AND quilates >= @QuilatesDesde");
            parameters.Add("QuilatesDesde", filtro.QuilatesDesde.Value);
        }
        if (filtro.QuilatesHasta.HasValue)
        {
            sub.Append(" AND quilates <= @QuilatesHasta");
            parameters.Add("QuilatesHasta", filtro.QuilatesHasta.Value);
        }

        // Quitar el " AND " inicial si hay contenido
        if (sub.Length > 0)
        {
            sub.Remove(0, 5); // Remove leading " AND "
        }

        return sub;
    }
}
