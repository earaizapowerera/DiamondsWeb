using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio de consulta y gestion de piezas faltantes.
/// Migrado de frmReporteInventarioFisico.frm (VB6).
/// Tabla principal: PIEZAS (Faltante=1), ComentariosFaltantes, StatusPiezas.
/// </summary>
public class FaltantesService
{
    private readonly string _connectionString;
    private readonly ILogger<FaltantesService> _logger;

    public FaltantesService(string connectionString, ILogger<FaltantesService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Obtiene todas las piezas faltantes con comentarios y status.
    /// Equivale a la vista vfaltantes del legacy VB6.
    /// </summary>
    public async Task<List<PiezaFaltante>> ObtenerFaltantesAsync(
        string? buscar, string? filtroStatus)
    {
        const string sql = @"
            SELECT TOP 500
                p.CodigoBarras,
                p.Descripcion,
                p.Modelo,
                p.Linea,
                p.Kilates,
                p.Peso,
                p.CBTotal,
                p.Precio,
                s.NombreStatus AS Status,
                cf.Comentarios
            FROM PIEZAS p
            LEFT JOIN StatusPiezas s ON p.IdStatus = s.IdStatus
            LEFT JOIN ComentariosFaltantes cf ON p.CodigoBarras = cf.CodigoBarras
            WHERE p.Faltante = 1
              AND p.CodigoBarras > ''
              AND (@Buscar IS NULL OR
                   p.CodigoBarras LIKE '%' + @Buscar + '%' OR
                   p.Descripcion LIKE '%' + @Buscar + '%' OR
                   p.Modelo LIKE '%' + @Buscar + '%')
              AND (@FiltroStatus IS NULL OR s.NombreStatus = @FiltroStatus)
            ORDER BY p.CodigoBarras";

        using var db = CreateConnection();
        var result = await db.QueryAsync<PiezaFaltante>(sql, new { Buscar = buscar, FiltroStatus = filtroStatus });
        return result.ToList();
    }

    /// <summary>
    /// Obtiene estadisticas resumen de faltantes para el dashboard.
    /// </summary>
    public async Task<FaltantesStats> ObtenerEstadisticasAsync()
    {
        const string sql = @"
            SELECT TOP 1
                COUNT(*) AS TotalFaltantes,
                SUM(CASE WHEN cf.Comentarios IS NOT NULL AND cf.Comentarios <> '' THEN 1 ELSE 0 END) AS ConComentarios,
                SUM(CASE WHEN cf.Comentarios IS NULL OR cf.Comentarios = '' THEN 1 ELSE 0 END) AS SinComentarios,
                ISNULL(SUM(p.CBTotal), 0) AS ValorTotal
            FROM PIEZAS p
            LEFT JOIN ComentariosFaltantes cf ON p.CodigoBarras = cf.CodigoBarras
            WHERE p.Faltante = 1 AND p.CodigoBarras > ''";

        using var db = CreateConnection();
        return await db.QuerySingleAsync<FaltantesStats>(sql);
    }

    /// <summary>
    /// Guarda o actualiza el comentario de una pieza faltante.
    /// Replica la logica de cmdComentario_Click del VB6.
    /// </summary>
    public async Task GuardarComentarioAsync(string codigoBarras, string? comentarios)
    {
        const string sql = @"
            DELETE FROM ComentariosFaltantes WHERE CodigoBarras = @CodigoBarras;
            INSERT INTO ComentariosFaltantes (CodigoBarras, Comentarios)
            VALUES (@CodigoBarras, @Comentarios)";

        using var db = CreateConnection();
        await db.ExecuteAsync(sql, new { CodigoBarras = codigoBarras, Comentarios = comentarios ?? "" });
        _logger.LogInformation("Comentario guardado para pieza {CodigoBarras}", codigoBarras);
    }

    /// <summary>
    /// Obtiene la lista de status disponibles para el filtro.
    /// </summary>
    public async Task<List<string>> ObtenerStatusDisponiblesAsync()
    {
        const string sql = @"
            SELECT TOP 20 DISTINCT s.NombreStatus
            FROM PIEZAS p
            INNER JOIN StatusPiezas s ON p.IdStatus = s.IdStatus
            WHERE p.Faltante = 1 AND p.CodigoBarras > ''
            ORDER BY s.NombreStatus";

        using var db = CreateConnection();
        var result = await db.QueryAsync<string>(sql);
        return result.ToList();
    }
}
