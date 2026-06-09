using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

public class InventoryService
{
    private readonly string _connectionString;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(string connectionString, ILogger<InventoryService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ══════════════════════════════════════════════
    // REPORTE FALTANTES
    // ══════════════════════════════════════════════
    public async Task<List<PiezaFaltante>> ObtenerFaltantesAsync(string? buscar = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT p.CodigoBarras, p.Descripcion, p.Precio, g.Grupo,
                       p.Modelo, p.Linea, p.Kilates, p.Peso, p.NumSerie,
                       cf.Comentarios AS Comentario
                    FROM Piezas p
                    LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
                    LEFT JOIN ComentariosFaltantes cf ON p.CodigoBarras = cf.CodigoBarras
                    WHERE p.Faltante = 1";
        if (!string.IsNullOrWhiteSpace(buscar))
            sql += @" AND (p.CodigoBarras LIKE @B OR p.Descripcion LIKE @B
                       OR g.Grupo LIKE @B OR cf.Comentarios LIKE @B
                       OR p.Modelo LIKE @B OR p.Linea LIKE @B)";
        sql += " ORDER BY p.CodigoBarras";
        return (await conn.QueryAsync<PiezaFaltante>(sql, new { B = $"%{buscar}%" })).ToList();
    }

    public async Task GuardarComentarioFaltanteAsync(string codigoBarras, string comentario)
    {
        using var conn = CreateConnection();
        var existe = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ComentariosFaltantes WHERE CodigoBarras = @CB", new { CB = codigoBarras });
        if (existe > 0)
            await conn.ExecuteAsync("UPDATE ComentariosFaltantes SET Comentarios = @C WHERE CodigoBarras = @CB",
                new { CB = codigoBarras, C = comentario });
        else
            await conn.ExecuteAsync("INSERT INTO ComentariosFaltantes (CodigoBarras, Comentarios) VALUES (@CB, @C)",
                new { CB = codigoBarras, C = comentario });
    }

    // ══════════════════════════════════════════════
    // PRE BAJAS
    // ══════════════════════════════════════════════

    /// <summary>
    /// Obtiene las pre-bajas del día actual, con JOIN a piezas para obtener la descripción.
    /// </summary>
    public async Task<List<PreBaja>> ObtenerPreBajasDelDiaAsync()
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT TOP 50
                   pb.CodigoBarras,
                   p.Descripcion,
                   pb.IdTipoBaja,
                   pb.FechaCaptura
              FROM PREBAJAS pb
              LEFT JOIN piezas p ON p.CodigoBarras = pb.CodigoBarras
             WHERE CAST(pb.FechaCaptura AS DATE) = CAST(GETUTCDATE() AS DATE)
             ORDER BY pb.FechaCaptura DESC";
        return (await conn.QueryAsync<PreBaja>(sql)).ToList();
    }

    /// <summary>
    /// Busca una pre-baja por código de barras exacto.
    /// </summary>
    public async Task<List<PreBaja>> BuscarPreBajaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT TOP 50
                   pb.CodigoBarras,
                   p.Descripcion,
                   pb.IdTipoBaja,
                   pb.FechaCaptura
              FROM PREBAJAS pb
              LEFT JOIN piezas p ON p.CodigoBarras = pb.CodigoBarras
             WHERE pb.CodigoBarras = @CodigoBarras
             ORDER BY pb.FechaCaptura DESC";
        return (await conn.QueryAsync<PreBaja>(sql, new { CodigoBarras = codigoBarras })).ToList();
    }

    /// <summary>
    /// Registra una pre-baja. Valida que el código tenga al menos 6 dígitos numéricos.
    /// </summary>
    public async Task RegistrarPreBajaAsync(string codigoBarras, int idTipoBaja)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras) || codigoBarras.Length < 6 || !long.TryParse(codigoBarras, out _))
            throw new ArgumentException("El código de barras debe tener al menos 6 dígitos numéricos.");

        if (idTipoBaja != 1 && idTipoBaja != 2)
            throw new ArgumentException("Tipo de baja inválido. Use 1 (Venta) o 2 (Devolución).");

        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO PREBAJAS (CodigoBarras, IdTipoBaja, FechaCaptura) VALUES (@CodigoBarras, @IdTipoBaja, GETUTCDATE())",
            new { CodigoBarras = codigoBarras.Trim(), IdTipoBaja = idTipoBaja });
    }

    /// <summary>
    /// Elimina una pre-baja por código de barras y fecha de captura.
    /// </summary>
    public async Task EliminarPreBajaAsync(string codigoBarras, DateTime fechaCaptura)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM PREBAJAS WHERE CodigoBarras = @CodigoBarras AND FechaCaptura = @FechaCaptura",
            new { CodigoBarras = codigoBarras, FechaCaptura = fechaCaptura });
    }
}
