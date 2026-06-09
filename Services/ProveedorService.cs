using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// CRUD de proveedores con catálogos asociados.
/// Tablas: Proveedores, vProveedores, DefaultsUtilidad, Divisores, Monedas, TablasJerarquias.
/// </summary>
public class ProveedorService
{
    private readonly string _connectionString;
    private readonly ILogger<ProveedorService> _logger;

    public ProveedorService(string connectionString, ILogger<ProveedorService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ── LIST ──────────────────────────────────────────────────────────

    public async Task<List<ProveedorResumen>> ListarAsync(string? buscar)
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT TOP 50
                   Proveedor, NombreProveedor, Direccion, Telefono, Telefono2,
                   Atiende, CaracteristicaDefault, CostoDefault, Moneda,
                   DefaultUtilidad, UtilizarMoneda, UtilidadExtra, FechaCaptura
            FROM   vProveedores
            WHERE  1=1
                   AND (@Buscar IS NULL
                        OR NombreProveedor LIKE '%' + @Buscar + '%'
                        OR Atiende LIKE '%' + @Buscar + '%'
                        OR Telefono LIKE '%' + @Buscar + '%'
                        OR Direccion LIKE '%' + @Buscar + '%')
            ORDER BY NombreProveedor";

        var result = await conn.QueryAsync<ProveedorResumen>(sql, new { Buscar = buscar });
        return result.AsList();
    }

    // ── GET BY ID ────────────────────────────────────────────────────

    public async Task<ProveedorDetalle?> ObtenerPorIdAsync(int proveedorId)
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT TOP 1
                   p.Proveedor, p.NombreProveedor, p.Direccion, p.Telefono, p.Telefono2,
                   p.Atiende, p.IdDefaultUtilidad, p.IdDefaultUtilidadExtra, p.IdMoneda,
                   p.UtilidadExtra, p.CaracteristicaDefault, p.CostoDefault,
                   p.IdDivisor, p.IdTabla, p.UtilizarMoneda,
                   v.DefaultUtilidad, v.DefaultUtilidadOro, v.DefaultUtilidadGemas,
                   v.DefaultUtilidadReloj,
                   v.Moneda, v.Divisor,
                   dv.Descripcion AS DivisorDescripcion,
                   tj.Descripcion AS TablaDescripcion
            FROM   Proveedores p
            LEFT JOIN vProveedores v ON v.Proveedor = p.Proveedor
            LEFT JOIN Divisores dv ON dv.IdDivisor = p.IdDivisor
            LEFT JOIN TablasJerarquias tj ON tj.IdTabla = p.IdTabla
            WHERE  p.Proveedor = @Id";

        // Map DefaultUtilidadExtra from the extra table if present
        var prov = await conn.QueryFirstOrDefaultAsync<ProveedorDetalle>(sql, new { Id = proveedorId });
        if (prov != null && prov.IdDefaultUtilidadExtra.HasValue && prov.IdDefaultUtilidadExtra > 0)
        {
            var extraSql = @"SELECT TOP 1 DefaultUtilidad FROM DefaultsUtilidad WHERE IdDefaultUtilidad = @ExtraId";
            prov.DefaultUtilidadExtraVal = await conn.QueryFirstOrDefaultAsync<decimal?>(extraSql,
                new { ExtraId = prov.IdDefaultUtilidadExtra });
        }

        return prov;
    }

    // ── CREATE ───────────────────────────────────────────────────────

    public async Task<int> CrearAsync(ProveedorDetalle prov)
    {
        using var conn = CreateConnection();

        // Get next available Id
        var nextId = await conn.QueryFirstAsync<int>("SELECT TOP 1 ISNULL(MAX(Proveedor), 0) + 1 FROM Proveedores");

        var sql = @"
            INSERT INTO Proveedores
                (Proveedor, NombreProveedor, Direccion, Telefono, Telefono2,
                 Atiende, IdDefaultUtilidad, IdDefaultUtilidadExtra, IdMoneda,
                 UtilidadExtra, CaracteristicaDefault, CostoDefault,
                 IdDivisor, IdTabla, UtilizarMoneda, IdUsuario, FechaCaptura)
            VALUES
                (@Proveedor, @NombreProveedor, @Direccion, @Telefono, @Telefono2,
                 @Atiende, @IdDefaultUtilidad, @IdDefaultUtilidadExtra, @IdMoneda,
                 @UtilidadExtra, @CaracteristicaDefault, @CostoDefault,
                 @IdDivisor, @IdTabla, @UtilizarMoneda, 1, GETUTCDATE())";

        await conn.ExecuteAsync(sql, new
        {
            Proveedor = nextId,
            prov.NombreProveedor,
            prov.Direccion,
            prov.Telefono,
            prov.Telefono2,
            prov.Atiende,
            prov.IdDefaultUtilidad,
            IdDefaultUtilidadExtra = prov.IdDefaultUtilidadExtra ?? (int?)null,
            IdMoneda = prov.IdMoneda ?? 1,
            prov.UtilidadExtra,
            prov.CaracteristicaDefault,
            prov.CostoDefault,
            prov.IdDivisor,
            prov.IdTabla,
            prov.UtilizarMoneda
        });

        _logger.LogInformation("Proveedor creado: Id={Id}, Nombre={Nombre}", nextId, prov.NombreProveedor);
        return nextId;
    }

    // ── UPDATE ───────────────────────────────────────────────────────

    public async Task ActualizarAsync(ProveedorDetalle prov)
    {
        using var conn = CreateConnection();
        var sql = @"
            UPDATE Proveedores SET
                NombreProveedor = @NombreProveedor,
                Direccion = @Direccion,
                Telefono = @Telefono,
                Telefono2 = @Telefono2,
                Atiende = @Atiende,
                IdDefaultUtilidad = @IdDefaultUtilidad,
                IdDefaultUtilidadExtra = @IdDefaultUtilidadExtra,
                IdMoneda = @IdMoneda,
                UtilidadExtra = @UtilidadExtra,
                CaracteristicaDefault = @CaracteristicaDefault,
                CostoDefault = @CostoDefault,
                IdDivisor = @IdDivisor,
                IdTabla = @IdTabla,
                UtilizarMoneda = @UtilizarMoneda,
                FechaUltEdicion = GETUTCDATE()
            WHERE Proveedor = @Proveedor";

        await conn.ExecuteAsync(sql, new
        {
            prov.Proveedor,
            prov.NombreProveedor,
            prov.Direccion,
            prov.Telefono,
            prov.Telefono2,
            prov.Atiende,
            prov.IdDefaultUtilidad,
            IdDefaultUtilidadExtra = prov.IdDefaultUtilidadExtra ?? (int?)null,
            IdMoneda = prov.IdMoneda ?? 1,
            prov.UtilidadExtra,
            prov.CaracteristicaDefault,
            prov.CostoDefault,
            prov.IdDivisor,
            prov.IdTabla,
            prov.UtilizarMoneda
        });

        _logger.LogInformation("Proveedor actualizado: Id={Id}, Nombre={Nombre}", prov.Proveedor, prov.NombreProveedor);
    }

    // ── DELETE ────────────────────────────────────────────────────────

    public async Task<bool> EliminarAsync(int proveedorId)
    {
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM Proveedores WHERE Proveedor = @Id",
            new { Id = proveedorId });

        if (rows > 0)
            _logger.LogInformation("Proveedor eliminado: Id={Id}", proveedorId);

        return rows > 0;
    }

    // ── CATÁLOGOS ────────────────────────────────────────────────────

    public async Task<List<DefaultUtilidadItem>> ObtenerDefaultsUtilidadAsync()
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 50 IdDefaultUtilidad, DefaultUtilidad, DefaultUtilidadGemas, DefaultUtilidadReloj
                    FROM DefaultsUtilidad ORDER BY IdDefaultUtilidad";
        return (await conn.QueryAsync<DefaultUtilidadItem>(sql)).AsList();
    }

    public async Task<List<CatalogoItem>> ObtenerMonedasAsync()
    {
        using var conn = CreateConnection();
        var sql = "SELECT TOP 50 IdMoneda AS Id, Moneda AS Texto FROM Monedas ORDER BY Moneda";
        return (await conn.QueryAsync<CatalogoItem>(sql)).AsList();
    }

    public async Task<List<DivisorItem>> ObtenerDivisoresAsync()
    {
        using var conn = CreateConnection();
        var sql = "SELECT TOP 50 IdDivisor, Divisor, Descripcion FROM Divisores ORDER BY Descripcion";
        return (await conn.QueryAsync<DivisorItem>(sql)).AsList();
    }

    public async Task<List<CatalogoItem>> ObtenerTablasJerarquiasAsync()
    {
        using var conn = CreateConnection();
        var sql = "SELECT TOP 50 IdTabla AS Id, Descripcion AS Texto FROM TablasJerarquias ORDER BY Descripcion";
        return (await conn.QueryAsync<CatalogoItem>(sql)).AsList();
    }

    public async Task<int> ContarProveedoresAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstAsync<int>("SELECT TOP 1 COUNT(*) FROM Proveedores");
    }
}
