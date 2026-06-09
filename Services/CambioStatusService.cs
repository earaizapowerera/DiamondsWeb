using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

public class CambioStatusService
{
    private readonly string _connStr;
    private readonly ILogger<CambioStatusService> _logger;

    public CambioStatusService(string connectionString, ILogger<CambioStatusService> logger)
    {
        _connStr = connectionString;
        _logger = logger;
    }

    /// <summary>Obtiene todos los status de piezas (catálogo)</summary>
    public async Task<List<StatusPieza>> ObtenerStatusAsync()
    {
        using var conn = new SqlConnection(_connStr);
        var result = await conn.QueryAsync<StatusPieza>(
            "SELECT TOP 50 IdStatus, NombreStatus FROM statuspiezas ORDER BY NombreStatus");
        return result.ToList();
    }

    /// <summary>Busca pieza por código de barras con su status actual y fecha de último cambio</summary>
    public async Task<PiezaStatus?> BuscarPiezaAsync(string codigoBarras)
    {
        using var conn = new SqlConnection(_connStr);
        var pieza = await conn.QueryFirstOrDefaultAsync<PiezaStatus>(@"
            SELECT TOP 1
                p.CodigoBarras,
                p.Descripcion,
                p.IdStatus,
                s.NombreStatus,
                (SELECT MAX(FechaCaptura) FROM bitacorastatus WHERE CodigoBarras = p.CodigoBarras) AS FechaUltimoCambio
            FROM piezas p
            INNER JOIN statuspiezas s ON s.IdStatus = p.IdStatus
            WHERE p.CodigoBarras = @CodigoBarras",
            new { CodigoBarras = codigoBarras });
        return pieza;
    }

    /// <summary>Cambia el status de una pieza e inserta en bitácora</summary>
    public async Task<int> CambiarStatusAsync(string codigoBarras, int nuevoStatusId, int userId)
    {
        using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            // Obtener status anterior
            var statusAnterior = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT TOP 1 IdStatus FROM piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            if (statusAnterior is null)
                throw new InvalidOperationException("La pieza no existe.");

            if (statusAnterior == nuevoStatusId)
                throw new InvalidOperationException("El nuevo status es igual al actual.");

            // Actualizar pieza
            await conn.ExecuteAsync(@"
                UPDATE piezas
                SET IdStatus = @NuevoStatus, FechaUltEdicion = GETUTCDATE()
                WHERE CodigoBarras = @CB",
                new { NuevoStatus = nuevoStatusId, CB = codigoBarras }, tx);

            // Insertar en bitácora (FechaCaptura NOT NULL, no tiene DEFAULT en la tabla legacy)
            var idCambio = await conn.QueryFirstAsync<int>(@"
                INSERT INTO bitacorastatus (CodigoBarras, IdStatusAnterior, IdStatusNuevo, IdUsuario, FechaCaptura)
                VALUES (@CB, @Anterior, @Nuevo, @User, GETUTCDATE());
                SELECT SCOPE_IDENTITY();",
                new { CB = codigoBarras, Anterior = statusAnterior, Nuevo = nuevoStatusId, User = userId }, tx);

            tx.Commit();
            _logger.LogInformation("Status cambiado: {CB} de {Ant} a {Nuevo}, IdCambio={Id}",
                codigoBarras, statusAnterior, nuevoStatusId, idCambio);
            return idCambio;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>Grid de piezas fuera de Exhibición, opcionalmente filtrado por status</summary>
    public async Task<List<PiezaEnStatus>> ObtenerPiezasEnStatusAsync(int? filtroStatusId = null)
    {
        using var conn = new SqlConnection(_connStr);
        var sql = @"
            SELECT TOP 200
                p.CodigoBarras,
                p.Descripcion,
                s.NombreStatus,
                (SELECT MAX(FechaCaptura) FROM bitacorastatus WHERE CodigoBarras = p.CodigoBarras) AS UltimoCambio
            FROM piezas p
            INNER JOIN statuspiezas s ON s.IdStatus = p.IdStatus
            WHERE p.IdStatus <> 1";

        if (filtroStatusId.HasValue)
            sql += " AND p.IdStatus = @FiltroStatus";

        sql += " ORDER BY s.NombreStatus, UltimoCambio DESC";

        var result = await conn.QueryAsync<PiezaEnStatus>(sql, new { FiltroStatus = filtroStatusId });
        return result.ToList();
    }

    /// <summary>Bitácora de cambios recientes de una pieza</summary>
    public async Task<List<BitacoraStatus>> ObtenerBitacoraAsync(string? codigoBarras = null)
    {
        using var conn = new SqlConnection(_connStr);
        var sql = @"
            SELECT TOP 50
                b.IdCambioStatus,
                b.CodigoBarras,
                b.IdStatusAnterior,
                sa.NombreStatus AS NombreStatusAnterior,
                b.IdStatusNuevo,
                sn.NombreStatus AS NombreStatusNuevo,
                b.FechaCaptura,
                b.IdUsuario
            FROM bitacorastatus b
            LEFT JOIN statuspiezas sa ON sa.IdStatus = b.IdStatusAnterior
            LEFT JOIN statuspiezas sn ON sn.IdStatus = b.IdStatusNuevo";

        if (!string.IsNullOrWhiteSpace(codigoBarras))
            sql += " WHERE b.CodigoBarras = @CB";

        sql += " ORDER BY b.IdCambioStatus DESC";

        var result = await conn.QueryAsync<BitacoraStatus>(sql, new { CB = codigoBarras });
        return result.ToList();
    }
}
