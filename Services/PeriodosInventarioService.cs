using System.Data;
using ClosedXML.Excel;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// CRUD de períodos de inventario físico -- migración de frmRegistroPeriodos.frm (VB6).
/// Tabla: InventariosFisicos | Join con Usuarios para nombre.
/// Antes de crear un período, ejecuta sp_mandarafaltantes (actualiza vista vfaltantes).
/// </summary>
public class PeriodosInventarioService
{
    private readonly string _connectionString;
    private readonly ILogger<PeriodosInventarioService> _logger;

    public PeriodosInventarioService(string connectionString, ILogger<PeriodosInventarioService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista todos los períodos con nombre de usuario.
    /// </summary>
    public async Task<List<PeriodoInventarioDetalle>> ListarAsync(string? buscar = null)
    {
        var searchWhere = string.IsNullOrWhiteSpace(buscar)
            ? ""
            : "AND (u.Nombre LIKE '%' + @Buscar + '%' OR CONVERT(VARCHAR, p.PeriodoDesde, 103) LIKE '%' + @Buscar + '%')";

        var sql = $@"
            SELECT TOP 200
                p.IdPeriodo,
                p.PeriodoDesde,
                p.PeriodoHasta,
                p.FechaCaptura,
                p.FechaUltEdicion,
                p.IdUsuario,
                u.Nombre AS NombreUsuario
            FROM InventariosFisicos p
            LEFT JOIN Usuarios u ON u.IdUsuario = p.IdUsuario
            WHERE 1=1 {searchWhere}
            ORDER BY p.IdPeriodo DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<PeriodoInventarioDetalle>(sql, new
        {
            Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar
        })).ToList();
    }

    /// <summary>
    /// Obtiene un período por su Id.
    /// </summary>
    public async Task<PeriodoInventarioDetalle?> ObtenerPorIdAsync(int idPeriodo)
    {
        const string sql = @"
            SELECT TOP 1
                p.IdPeriodo,
                p.PeriodoDesde,
                p.PeriodoHasta,
                p.FechaCaptura,
                p.FechaUltEdicion,
                p.IdUsuario,
                u.Nombre AS NombreUsuario
            FROM InventariosFisicos p
            LEFT JOIN Usuarios u ON u.IdUsuario = p.IdUsuario
            WHERE p.IdPeriodo = @IdPeriodo";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PeriodoInventarioDetalle>(sql, new { IdPeriodo = idPeriodo });
    }

    /// <summary>
    /// Crea un nuevo período. Ejecuta sp_mandarafaltantes antes del INSERT (igual que VB6).
    /// En VB6 el SP se ejecuta de forma independiente al INSERT, así que si falla no bloquea la creación.
    /// </summary>
    public async Task<int> CrearAsync(DateTime periodoDesde, DateTime? periodoHasta, int idUsuario)
    {
        using var conn = CreateConnection();

        // VB6 ejecuta sp_mandarafaltantes de forma independiente antes del INSERT
        try
        {
            await conn.ExecuteAsync("EXEC sp_mandarafaltantes", commandTimeout: 120);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "sp_mandarafaltantes falló (no bloquea la creación del período)");
        }

        const string sql = @"
            INSERT INTO InventariosFisicos (PeriodoDesde, PeriodoHasta, FechaCaptura, FechaUltEdicion, IdUsuario)
            VALUES (@PeriodoDesde, @PeriodoHasta, GETUTCDATE(), GETUTCDATE(), @IdUsuario);
            SELECT CAST(SCOPE_IDENTITY() AS INT)";

        var id = await conn.ExecuteScalarAsync<int>(sql, new
        {
            PeriodoDesde = periodoDesde,
            PeriodoHasta = periodoHasta,
            IdUsuario = idUsuario
        });

        _logger.LogInformation("Período de inventario creado: Id={Id}, Desde={Desde}, Hasta={Hasta}",
            id, periodoDesde, periodoHasta);
        return id;
    }

    /// <summary>
    /// Actualiza un período existente. Actualiza FechaUltEdicion automáticamente.
    /// </summary>
    public async Task ActualizarAsync(int idPeriodo, DateTime periodoDesde, DateTime? periodoHasta, int idUsuario)
    {
        const string sql = @"
            UPDATE InventariosFisicos
            SET PeriodoDesde = @PeriodoDesde,
                PeriodoHasta = @PeriodoHasta,
                FechaUltEdicion = GETUTCDATE(),
                IdUsuario = @IdUsuario
            WHERE IdPeriodo = @IdPeriodo";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            IdPeriodo = idPeriodo,
            PeriodoDesde = periodoDesde,
            PeriodoHasta = periodoHasta,
            IdUsuario = idUsuario
        });
        _logger.LogInformation("Período de inventario actualizado: Id={Id}", idPeriodo);
    }

    /// <summary>
    /// Elimina un período por Id.
    /// </summary>
    public async Task EliminarAsync(int idPeriodo)
    {
        const string sql = "DELETE FROM InventariosFisicos WHERE IdPeriodo = @IdPeriodo";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { IdPeriodo = idPeriodo });
        _logger.LogInformation("Período de inventario eliminado: Id={Id}", idPeriodo);
    }

    /// <summary>
    /// Cuenta el total de períodos registrados.
    /// </summary>
    public async Task<int> ContarAsync()
    {
        const string sql = "SELECT TOP 1 COUNT(*) FROM InventariosFisicos";

        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql);
    }

    /// <summary>
    /// Exporta los períodos como archivo Excel (.xlsx).
    /// </summary>
    public async Task<byte[]> ExportarExcelAsync()
    {
        var registros = await ListarAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Periodos Inventario");

        // Encabezados
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "Periodo Desde";
        ws.Cell(1, 3).Value = "Periodo Hasta";
        ws.Cell(1, 4).Value = "Fecha Captura";
        ws.Cell(1, 5).Value = "Ultima Edicion";
        ws.Cell(1, 6).Value = "Usuario";

        var hdr = ws.Range(1, 1, 1, 6);
        hdr.Style.Font.Bold = true;
        hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#2d3436");
        hdr.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < registros.Count; i++)
        {
            var r = registros[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = r.IdPeriodo;
            ws.Cell(row, 2).Value = r.PeriodoDesde?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell(row, 3).Value = r.PeriodoHasta?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell(row, 4).Value = r.FechaCaptura?.ToString("dd/MM/yyyy HH:mm") ?? "";
            ws.Cell(row, 5).Value = r.FechaUltEdicion?.ToString("dd/MM/yyyy HH:mm") ?? "";
            ws.Cell(row, 6).Value = r.NombreUsuario ?? "";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
