using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para el Visor de Fotografías / CBO.
/// Controla la visibilidad de piezas en el catálogo fotográfico.
/// Origen VB6: frmOcultar.frm (frmCBO) — Consultas2.vbp.
/// </summary>
public class VisorFotografiasService
{
    private readonly string _connectionString;
    private readonly ILogger<VisorFotografiasService> _logger;

    public VisorFotografiasService(string connectionString, ILogger<VisorFotografiasService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Obtiene todas las piezas con su estado de visibilidad (0/1) desde la vista vfotografias.
    /// Soporta filtro por texto (CodigoBarras, Descripcion, Grupo, Modelo).
    /// </summary>
    public async Task<List<PiezaCbo>> ObtenerPiezasAsync(string? buscar = null)
    {
        using var db = CreateConnection();

        var sql = @"SELECT TOP 500
                        Visible, CodigoBarras, Descripcion, Grupo,
                        Kilates, Modelo, Precio, cb AS Cb
                    FROM vfotografias";

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            sql += @" WHERE CodigoBarras LIKE @Buscar
                         OR Descripcion LIKE @Buscar
                         OR Grupo LIKE @Buscar
                         OR Modelo LIKE @Buscar";
        }

        sql += " ORDER BY CodigoBarras";

        var param = new { Buscar = $"%{buscar}%" };
        return (await db.QueryAsync<PiezaCbo>(sql, param)).ToList();
    }

    /// <summary>
    /// Guarda el estado de visibilidad de las piezas en la tabla cbo.
    /// Patrón replace-on-save: DELETE + INSERT condicional (igual que VB6).
    /// </summary>
    public async Task<int> GuardarVisibilidadAsync(List<string> codigosVisibles)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        try
        {
            // Borrar todos los registros actuales de cbo
            await db.ExecuteAsync("DELETE FROM cbo", transaction: tx);

            if (codigosVisibles.Count > 0)
            {
                // Insertar solo los que tienen Visible=1
                var sql = "INSERT INTO cbo (cb) VALUES (@Cb)";
                var rows = await db.ExecuteAsync(sql,
                    codigosVisibles.Select(c => new { Cb = c }),
                    transaction: tx);

                tx.Commit();
                _logger.LogInformation("CBO guardado: {Count} piezas visibles", rows);
                return rows;
            }

            tx.Commit();
            return 0;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error al guardar visibilidad CBO");
            throw;
        }
    }

    /// <summary>
    /// Establece visibilidad masiva (1 o 0) para todos los registros filtrados.
    /// </summary>
    public async Task<int> EstablecerTodosAsync(bool visible, string? buscar = null)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        try
        {
            if (visible)
            {
                // Insertar en cbo todas las piezas que no estén ya
                var sql = @"INSERT INTO cbo (cb)
                            SELECT p.CodigoBarras FROM piezas p
                            WHERE NOT EXISTS (SELECT 1 FROM cbo c WHERE c.cb = p.CodigoBarras)";

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    sql = @"INSERT INTO cbo (cb)
                            SELECT v.cb FROM vfotografias v
                            WHERE v.Visible = 0
                              AND (v.CodigoBarras LIKE @Buscar
                                   OR v.Descripcion LIKE @Buscar
                                   OR v.Grupo LIKE @Buscar
                                   OR v.Modelo LIKE @Buscar)";
                }

                var rows = await db.ExecuteAsync(sql, new { Buscar = $"%{buscar}%" }, transaction: tx);
                tx.Commit();
                _logger.LogInformation("CBO: {Count} piezas marcadas como visibles", rows);
                return rows;
            }
            else
            {
                // Quitar de cbo (borrar)
                string sql;
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    sql = @"DELETE c FROM cbo c
                            INNER JOIN vfotografias v ON v.cb = c.cb
                            WHERE v.CodigoBarras LIKE @Buscar
                               OR v.Descripcion LIKE @Buscar
                               OR v.Grupo LIKE @Buscar
                               OR v.Modelo LIKE @Buscar";
                }
                else
                {
                    sql = "DELETE FROM cbo";
                }

                var rows = await db.ExecuteAsync(sql, new { Buscar = $"%{buscar}%" }, transaction: tx);
                tx.Commit();
                _logger.LogInformation("CBO: {Count} piezas desmarcadas", rows);
                return rows;
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error en operación masiva CBO");
            throw;
        }
    }
}
