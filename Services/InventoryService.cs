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
