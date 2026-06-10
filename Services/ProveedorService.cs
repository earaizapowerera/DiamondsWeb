using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para CRUD de Razones Sociales de Proveedores y asignaciones N:N.
/// Tablas: RAZONES_SOCIALES_PROVEEDORES, RAZONES_SOCIALES_PROVEEDORES_PROVEEDORES, PROVEEDORES
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

    // ─── Razones Sociales ─────────────────────────────────────────────

    public async Task<List<RazonSocialProveedor>> ObtenerRazonesSocialesAsync(string? buscar = null)
    {
        var sql = @"
            SELECT TOP 200
                IdRazonSocialProveedor,
                RFC,
                RazonSocialProveedor AS RazonSocialProveedorNombre,
                Calle, CodigoPostal, Colonia, Municipio, Estado,
                FechaCaptura, FechaUltEdicion, IdUsuario
            FROM RAZONES_SOCIALES_PROVEEDORES
            WHERE (@Buscar IS NULL
                OR RazonSocialProveedor LIKE '%' + @Buscar + '%'
                OR RFC LIKE '%' + @Buscar + '%')
            ORDER BY RazonSocialProveedor";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<RazonSocialProveedor>(sql, new
        {
            Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar
        })).ToList();
    }

    public async Task<RazonSocialProveedor?> ObtenerRazonSocialPorIdAsync(int id)
    {
        var sql = @"
            SELECT TOP 1
                IdRazonSocialProveedor,
                RFC,
                RazonSocialProveedor AS RazonSocialProveedorNombre,
                Calle, CodigoPostal, Colonia, Municipio, Estado,
                FechaCaptura, FechaUltEdicion, IdUsuario
            FROM RAZONES_SOCIALES_PROVEEDORES
            WHERE IdRazonSocialProveedor = @Id";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<RazonSocialProveedor>(sql, new { Id = id });
    }

    public async Task<int> CrearRazonSocialAsync(RazonSocialProveedor rs)
    {
        var sql = @"
            DECLARE @NewId INT = (SELECT ISNULL(MAX(IdRazonSocialProveedor), 0) + 1 FROM RAZONES_SOCIALES_PROVEEDORES);
            INSERT INTO RAZONES_SOCIALES_PROVEEDORES
                (IdRazonSocialProveedor, RFC, RazonSocialProveedor, Calle, CodigoPostal, Colonia, Municipio, Estado,
                 FechaCaptura, FechaUltEdicion, IdUsuario)
            VALUES
                (@NewId, @RFC, @RazonSocialProveedorNombre, @Calle, @CodigoPostal, @Colonia, @Municipio, @Estado,
                 GETUTCDATE(), GETUTCDATE(), @IdUsuario);
            SELECT @NewId;";

        using var conn = CreateConnection();
        var newId = await conn.QuerySingleAsync<int>(sql, rs);
        _logger.LogInformation("Razón social creada: Id={Id}, Nombre={Nombre}", newId, rs.RazonSocialProveedorNombre);
        return newId;
    }

    public async Task ActualizarRazonSocialAsync(RazonSocialProveedor rs)
    {
        var sql = @"
            UPDATE RAZONES_SOCIALES_PROVEEDORES SET
                RFC = @RFC,
                RazonSocialProveedor = @RazonSocialProveedorNombre,
                Calle = @Calle,
                CodigoPostal = @CodigoPostal,
                Colonia = @Colonia,
                Municipio = @Municipio,
                Estado = @Estado,
                FechaUltEdicion = GETUTCDATE(),
                IdUsuario = @IdUsuario
            WHERE IdRazonSocialProveedor = @IdRazonSocialProveedor";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, rs);
        _logger.LogInformation("Razón social actualizada: Id={Id}", rs.IdRazonSocialProveedor);
    }

    public async Task<bool> EliminarRazonSocialAsync(int id)
    {
        // Verificar si tiene asignaciones
        var sqlCheck = "SELECT TOP 1 COUNT(*) FROM RAZONES_SOCIALES_PROVEEDORES_PROVEEDORES WHERE IdRazonSocialProveedor = @Id";
        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(sqlCheck, new { Id = id });
        if (count > 0)
        {
            _logger.LogWarning("No se puede eliminar razón social {Id}: tiene {Count} asignaciones", id, count);
            return false;
        }

        var sql = "DELETE FROM RAZONES_SOCIALES_PROVEEDORES WHERE IdRazonSocialProveedor = @Id";
        await conn.ExecuteAsync(sql, new { Id = id });
        _logger.LogInformation("Razón social eliminada: Id={Id}", id);
        return true;
    }

    // ─── Asignaciones N:N ─────────────────────────────────────────────

    public async Task<List<RazonSocialProveedorAsignacion>> ObtenerAsignacionesAsync(
        int? idRazonSocial = null, string? buscar = null)
    {
        var sql = @"
            SELECT TOP 200
                v.Id, v.IdRazonSocialProveedor, v.Proveedor,
                v.NombreProveedor, v.RazonSocialProveedor AS RazonSocialProveedorNombre,
                v.FechaCaptura, v.FechaUltEdicion, v.IdUsuario
            FROM vRazonesSocialesProveedoresProveedores v
            WHERE (@IdRazonSocial IS NULL OR v.IdRazonSocialProveedor = @IdRazonSocial)
              AND (@Buscar IS NULL
                   OR v.NombreProveedor LIKE '%' + @Buscar + '%'
                   OR v.RazonSocialProveedor LIKE '%' + @Buscar + '%')
            ORDER BY v.RazonSocialProveedor, v.NombreProveedor";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<RazonSocialProveedorAsignacion>(sql, new
        {
            IdRazonSocial = idRazonSocial,
            Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar
        })).ToList();
    }

    public async Task<int> CrearAsignacionAsync(int idRazonSocial, int proveedor, int? idUsuario)
    {
        // Verificar duplicado
        var sqlCheck = @"SELECT TOP 1 COUNT(*) FROM RAZONES_SOCIALES_PROVEEDORES_PROVEEDORES
                         WHERE IdRazonSocialProveedor = @IdRS AND Proveedor = @Prov";
        using var conn = CreateConnection();
        var exists = await conn.ExecuteScalarAsync<int>(sqlCheck,
            new { IdRS = idRazonSocial, Prov = proveedor });
        if (exists > 0)
        {
            _logger.LogWarning("Asignación duplicada: RS={RS}, Prov={Prov}", idRazonSocial, proveedor);
            return -1;
        }

        var sql = @"
            DECLARE @NewId INT = (SELECT ISNULL(MAX(Id), 0) + 1 FROM RAZONES_SOCIALES_PROVEEDORES_PROVEEDORES);
            INSERT INTO RAZONES_SOCIALES_PROVEEDORES_PROVEEDORES
                (Id, IdRazonSocialProveedor, Proveedor, FechaCaptura, FechaUltEdicion, IdUsuario)
            VALUES
                (@NewId, @IdRazonSocial, @Proveedor, GETUTCDATE(), GETUTCDATE(), @IdUsuario);
            SELECT @NewId;";

        var newId = await conn.QuerySingleAsync<int>(sql, new
        {
            IdRazonSocial = idRazonSocial,
            Proveedor = proveedor,
            IdUsuario = idUsuario
        });

        _logger.LogInformation("Asignación creada: Id={Id}, RS={RS}, Prov={Prov}",
            newId, idRazonSocial, proveedor);
        return newId;
    }

    public async Task EliminarAsignacionAsync(int id)
    {
        var sql = "DELETE FROM RAZONES_SOCIALES_PROVEEDORES_PROVEEDORES WHERE Id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
        _logger.LogInformation("Asignación eliminada: Id={Id}", id);
    }

    public async Task EliminarAsignacionAsync(int idRazonSocial, int proveedor)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM RAZONES_SOCIALES_PROVEEDORES_PROVEEDORES WHERE IdRazonSocialProveedor = @IdRS AND Proveedor = @Prov",
            new { IdRS = idRazonSocial, Prov = proveedor });
        _logger.LogInformation("Asignación eliminada: RS={RS}, Prov={Prov}", idRazonSocial, proveedor);
    }

    // ─── Catálogos para dropdowns ─────────────────────────────────────

    public async Task<List<ProveedorSimple>> ObtenerProveedoresAsync()
    {
        var sql = @"SELECT TOP 500 Proveedor, NombreProveedor
                    FROM PROVEEDORES ORDER BY NombreProveedor";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<ProveedorSimple>(sql)).ToList();
    }

    public async Task<List<RazonSocialProveedor>> ObtenerRazonesSocialesParaComboAsync()
    {
        var sql = @"SELECT TOP 500
                        IdRazonSocialProveedor,
                        RazonSocialProveedor AS RazonSocialProveedorNombre,
                        RFC
                    FROM RAZONES_SOCIALES_PROVEEDORES
                    ORDER BY RazonSocialProveedor";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<RazonSocialProveedor>(sql)).ToList();
    }

    // ─── Proveedores CRUD (Pages/Proveedores) ────────────────────────

    public async Task<List<ProveedorResumen>> ListarAsync(string? buscar = null)
    {
        var sql = @"
            SELECT TOP 200 p.Proveedor, p.NombreProveedor, p.Atiende, p.Telefono, p.Telefono2,
                   ISNULL(p.CaracteristicaDefault, 'Oro') AS CaracteristicaDefault,
                   ISNULL(p.CostoDefault, 'Pieza') AS CostoDefault,
                   p.UtilizarMoneda, m.Moneda AS Moneda,
                   CAST(du.DefaultUtilidad AS VARCHAR) AS DefaultUtilidad
            FROM PROVEEDORES p
            LEFT JOIN Monedas m ON m.IdMoneda = p.IdMoneda
            LEFT JOIN DefaultsUtilidad du ON du.IdDefaultUtilidad = p.IdDefaultUtilidad
            WHERE (@Buscar IS NULL
                OR p.NombreProveedor LIKE '%' + @Buscar + '%'
                OR p.Atiende LIKE '%' + @Buscar + '%'
                OR p.Telefono LIKE '%' + @Buscar + '%'
                OR p.Direccion LIKE '%' + @Buscar + '%')
            ORDER BY p.NombreProveedor";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<ProveedorResumen>(sql, new
        {
            Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar
        })).ToList();
    }

    public async Task<int> ContarProveedoresAsync()
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PROVEEDORES");
    }

    public async Task<ProveedorDetalle?> ObtenerPorIdAsync(int id)
    {
        var sql = @"
            SELECT TOP 1 p.Proveedor, p.NombreProveedor, p.Direccion, p.Telefono, p.Telefono2, p.Atiende,
                   ISNULL(p.CaracteristicaDefault, 'Oro') AS CaracteristicaDefault,
                   ISNULL(p.CostoDefault, 'Pieza') AS CostoDefault,
                   p.IdDefaultUtilidad, p.IdDefaultUtilidadExtra, p.IdMoneda,
                   p.UtilizarMoneda, p.UtilidadExtra, ISNULL(p.IdDivisor, 1) AS IdDivisor,
                   ISNULL(p.IdTabla, 2) AS IdTabla
            FROM PROVEEDORES p
            WHERE p.Proveedor = @Id";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ProveedorDetalle>(sql, new { Id = id });
    }

    public async Task<int> CrearAsync(ProveedorDetalle prov)
    {
        var sql = @"
            INSERT INTO PROVEEDORES (NombreProveedor, Direccion, Telefono, Telefono2, Atiende,
                CaracteristicaDefault, CostoDefault, IdDefaultUtilidad, IdDefaultUtilidadExtra,
                IdMoneda, UtilizarMoneda, UtilidadExtra, IdDivisor, IdTabla, FechaCaptura)
            VALUES (@NombreProveedor, @Direccion, @Telefono, @Telefono2, @Atiende,
                @CaracteristicaDefault, @CostoDefault, @IdDefaultUtilidad, @IdDefaultUtilidadExtra,
                @IdMoneda, @UtilizarMoneda, @UtilidadExtra, @IdDivisor, @IdTabla, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var conn = CreateConnection();
        return await conn.QuerySingleAsync<int>(sql, prov);
    }

    public async Task ActualizarAsync(ProveedorDetalle prov)
    {
        var sql = @"
            UPDATE PROVEEDORES SET
                NombreProveedor = @NombreProveedor, Direccion = @Direccion,
                Telefono = @Telefono, Telefono2 = @Telefono2, Atiende = @Atiende,
                CaracteristicaDefault = @CaracteristicaDefault, CostoDefault = @CostoDefault,
                IdDefaultUtilidad = @IdDefaultUtilidad, IdDefaultUtilidadExtra = @IdDefaultUtilidadExtra,
                IdMoneda = @IdMoneda, UtilizarMoneda = @UtilizarMoneda,
                UtilidadExtra = @UtilidadExtra, IdDivisor = @IdDivisor, IdTabla = @IdTabla
            WHERE Proveedor = @Proveedor";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, prov);
    }

    public async Task<bool> EliminarAsync(int id)
    {
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM PROVEEDORES WHERE Proveedor = @Id", new { Id = id });
        return rows > 0;
    }

    public async Task<List<DefaultUtilidadItem>> ObtenerDefaultsUtilidadAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultUtilidadItem>(
            @"SELECT IdDefaultUtilidad, DefaultUtilidad, DefaultUtilidadGemas, DefaultUtilidadReloj
              FROM DefaultsUtilidad WHERE IdDefaultUtilidad > 0
              ORDER BY IdDefaultUtilidad")).ToList();
    }

    public async Task<List<CatalogoItem>> ObtenerMonedasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<CatalogoItem>(
            "SELECT IdMoneda AS Id, Moneda AS Texto FROM Monedas ORDER BY Moneda")).ToList();
    }

    public async Task<List<DivisorItem>> ObtenerDivisoresAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DivisorItem>(
            @"SELECT IdDivisor, Divisor, Descripcion, IdUsuario, FechaCaptura, FechaUltEdicion
              FROM Divisores ORDER BY Descripcion")).ToList();
    }

    public async Task<List<CatalogoItem>> ObtenerTablasJerarquiasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<CatalogoItem>(
            "SELECT IdTabla AS Id, Descripcion AS Texto FROM TablasJerarquias ORDER BY Descripcion")).ToList();
    }
}
