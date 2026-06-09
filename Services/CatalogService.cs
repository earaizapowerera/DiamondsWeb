using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio CRUD para catálogos de Diamonds (DefaultsUtilidad, etc.)
/// </summary>
public class CatalogService
{
    private readonly string _connectionString;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(string connectionString, ILogger<CatalogService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ── DEFAULTS UTILIDAD ────────────────────────────────────────

    public async Task<List<DefaultUtilidad>> ObtenerDefaultsUtilidadAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultUtilidad>(
            @"SELECT d.IdDefaultUtilidad,
                     d.DefaultUtilidad AS DefaultUtilidadGeneral,
                     d.DefaultUtilidadGemas,
                     d.DefaultUtilidadReloj,
                     d.IdUsuario,
                     u.Nombre AS NombreUsuario,
                     d.FechaCaptura
              FROM DefaultsUtilidad d
              INNER JOIN Usuarios u ON u.IdUsuario = d.IdUsuario
              WHERE d.IdDefaultUtilidad > 0
              ORDER BY d.FechaCaptura DESC"
        )).ToList();
    }

    public async Task<DefaultUtilidad?> ObtenerDefaultUtilidadAsync(int id)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DefaultUtilidad>(
            @"SELECT d.IdDefaultUtilidad,
                     d.DefaultUtilidad AS DefaultUtilidadGeneral,
                     d.DefaultUtilidadGemas,
                     d.DefaultUtilidadReloj,
                     d.IdUsuario,
                     u.Nombre AS NombreUsuario,
                     d.FechaCaptura
              FROM DefaultsUtilidad d
              INNER JOIN Usuarios u ON u.IdUsuario = d.IdUsuario
              WHERE d.IdDefaultUtilidad = @Id",
            new { Id = id });
    }

    public async Task<int> CrearDefaultUtilidadAsync(
        decimal utilidad, decimal utilidadGemas, decimal utilidadReloj, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO DefaultsUtilidad
                (DefaultUtilidad, DefaultUtilidadGemas, DefaultUtilidadReloj, IdUsuario, FechaCaptura)
              OUTPUT INSERTED.IdDefaultUtilidad
              VALUES (@Utilidad, @UtilidadGemas, @UtilidadReloj, @IdUsuario, GETUTCDATE())",
            new
            {
                Utilidad = utilidad,
                UtilidadGemas = utilidadGemas,
                UtilidadReloj = utilidadReloj,
                IdUsuario = idUsuario
            });
    }

    public async Task ActualizarDefaultUtilidadAsync(
        int id, decimal utilidad, decimal utilidadGemas, decimal utilidadReloj, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE DefaultsUtilidad
              SET DefaultUtilidad = @Utilidad,
                  DefaultUtilidadGemas = @UtilidadGemas,
                  DefaultUtilidadReloj = @UtilidadReloj,
                  IdUsuario = @IdUsuario
              WHERE IdDefaultUtilidad = @Id",
            new
            {
                Id = id,
                Utilidad = utilidad,
                UtilidadGemas = utilidadGemas,
                UtilidadReloj = utilidadReloj,
                IdUsuario = idUsuario
            });
    }

    public async Task EliminarDefaultUtilidadAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM DefaultsUtilidad WHERE IdDefaultUtilidad = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // DEFAULTS UTILIDAD EXTRA
    // ══════════════════════════════════════════════
    public async Task<List<DefaultUtilidadExtra>> ObtenerDefaultsUtilidadExtraAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultUtilidadExtra>(@"
            SELECT TOP 50 IdDefaultUtilidadExtra, DefaultUtilidadExtra AS DefaultUtilidadExtra1,
                   IdUsuario, Nombre AS NombreUsuario, FechaCaptura
            FROM vDefaultsUtilidadExtra
            ORDER BY FechaCaptura DESC")).ToList();
    }

    public async Task<int> CrearDefaultUtilidadExtraAsync(decimal utilidadExtra, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO DefaultsUtilidadExtra (DefaultUtilidadExtra, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdDefaultUtilidadExtra VALUES (@UtilidadExtra, @IdUsuario, GETUTCDATE())",
            new { UtilidadExtra = utilidadExtra, IdUsuario = idUsuario });
    }

    public async Task ActualizarDefaultUtilidadExtraAsync(int id, decimal utilidadExtra, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE DefaultsUtilidadExtra
              SET DefaultUtilidadExtra = @UtilidadExtra, IdUsuario = @IdUsuario, FechaCaptura = GETUTCDATE()
              WHERE IdDefaultUtilidadExtra = @Id",
            new { Id = id, UtilidadExtra = utilidadExtra, IdUsuario = idUsuario });
    }

    public async Task EliminarDefaultUtilidadExtraAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM DefaultsUtilidadExtra WHERE IdDefaultUtilidadExtra = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // UTILIDAD EXTRA POR PRECIO/GRAMO
    // Tabla real: Id, PrecioGramoDesde, PrecioGramoHasta, DefaultUtilidadExtra, FechaCaptura, IdUsuario, rowguid
    // ══════════════════════════════════════════════
    public async Task<List<UtilidadExtraPrecioGramo>> ObtenerUtilidadExtraPrecioGramoAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<UtilidadExtraPrecioGramo>(@"
            SELECT Id AS IdUtilidadExtra, PrecioGramoDesde, PrecioGramoHasta,
                   DefaultUtilidadExtra AS UtilidadExtra, IdUsuario, FechaCaptura
            FROM UtilidadExtra_PrecioGramo ORDER BY PrecioGramoDesde")).ToList();
    }

    public async Task<int> CrearUtilidadExtraPrecioGramoAsync(decimal desde, decimal hasta, decimal utilidad, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO UtilidadExtra_PrecioGramo (PrecioGramoDesde, PrecioGramoHasta, DefaultUtilidadExtra, IdUsuario, FechaCaptura)
            OUTPUT INSERTED.Id
            VALUES (@Desde, @Hasta, @Utilidad, @IdUsuario, GETUTCDATE())",
            new { Desde = desde, Hasta = hasta, Utilidad = utilidad, IdUsuario = idUsuario });
    }

    public async Task ActualizarUtilidadExtraPrecioGramoAsync(int id, decimal desde, decimal hasta, decimal utilidad)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE UtilidadExtra_PrecioGramo
            SET PrecioGramoDesde = @Desde, PrecioGramoHasta = @Hasta, DefaultUtilidadExtra = @Utilidad
            WHERE Id = @Id",
            new { Id = id, Desde = desde, Hasta = hasta, Utilidad = utilidad });
    }

    public async Task EliminarUtilidadExtraPrecioGramoAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM UtilidadExtra_PrecioGramo WHERE Id = @Id", new { Id = id });
    }

    public async Task<bool> ExisteRangoSolapadoAsync(decimal desde, decimal hasta, int? excluirId = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 1 1 FROM UtilidadExtra_PrecioGramo
                    WHERE PrecioGramoDesde < @Hasta AND PrecioGramoHasta > @Desde";
        if (excluirId.HasValue)
            sql += " AND Id <> @ExcluirId";
        var result = await conn.QueryFirstOrDefaultAsync<int?>(sql,
            new { Desde = desde, Hasta = hasta, ExcluirId = excluirId });
        return result.HasValue;
    }

    // ══════════════════════════════════════════════
    // TABLAS DE JERARQUÍAS
    // ══════════════════════════════════════════════
    public async Task<List<TablaJerarquia>> ObtenerTablasJerarquiasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<TablaJerarquia>(
            "SELECT IdTablaJerarquia, Descripcion, IdUsuario FROM TablasJerarquias ORDER BY Descripcion")).ToList();
    }

    public async Task<List<Jerarquia>> ObtenerJerarquiasAsync(int idTabla)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Jerarquia>(
            "SELECT IdJerarquia, IdTablaJerarquia, Columna, Orden FROM Jerarquias WHERE IdTablaJerarquia = @Id ORDER BY Orden",
            new { Id = idTabla })).ToList();
    }

    public async Task<int> CrearTablaJerarquiaAsync(string descripcion, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO TablasJerarquias (Descripcion, IdUsuario) OUTPUT INSERTED.IdTablaJerarquia VALUES (@Desc, @IdUsuario)",
            new { Desc = descripcion, IdUsuario = idUsuario });
    }

    public async Task EliminarTablaJerarquiaAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Jerarquias WHERE IdTablaJerarquia = @Id", new { Id = id });
        await conn.ExecuteAsync("DELETE FROM TablasJerarquias WHERE IdTablaJerarquia = @Id", new { Id = id });
    }

    public async Task<int> CrearJerarquiaAsync(int idTabla, string columna, int orden)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Jerarquias (IdTablaJerarquia, Columna, Orden) OUTPUT INSERTED.IdJerarquia VALUES (@IdTabla, @Columna, @Orden)",
            new { IdTabla = idTabla, Columna = columna, Orden = orden });
    }

    public async Task EliminarJerarquiaAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Jerarquias WHERE IdJerarquia = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // DISEÑO ETIQUETAS
    // ══════════════════════════════════════════════
    public async Task<List<DisenioEtiqueta>> ObtenerDiseniosEtiquetasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DisenioEtiqueta>(
            "SELECT IdDisenio, Descripcion, ArchivoEtiqueta, ArchivoEtiquetaCompuesta FROM DisenosEtiquetas ORDER BY Descripcion")).ToList();
    }

    // ══════════════════════════════════════════════
    // DIAMANTES (vista vdiamantes)
    // ══════════════════════════════════════════════
    public async Task<List<DiamanteLista>> ObtenerDiamantesAsync(string? buscar = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 500 CodigoBarras, Descripcion, Quilates, Color, Pureza, Corte, Obs1, Obs2, Precio, NombreProveedor
                    FROM vdiamantes
                    WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(buscar))
            sql += " AND (Descripcion LIKE @B OR CodigoBarras LIKE @B OR Color LIKE @B OR Pureza LIKE @B)";
        sql += " ORDER BY CodigoBarras";
        return (await conn.QueryAsync<DiamanteLista>(sql, new { B = $"%{buscar}%" })).ToList();
    }
}
