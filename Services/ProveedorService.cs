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
            INSERT INTO RAZONES_SOCIALES_PROVEEDORES
                (RFC, RazonSocialProveedor, Calle, CodigoPostal, Colonia, Municipio, Estado,
                 FechaCaptura, FechaUltEdicion, IdUsuario)
            VALUES
                (@RFC, @RazonSocialProveedorNombre, @Calle, @CodigoPostal, @Colonia, @Municipio, @Estado,
                 GETUTCDATE(), GETUTCDATE(), @IdUsuario);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

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
            INSERT INTO RAZONES_SOCIALES_PROVEEDORES_PROVEEDORES
                (IdRazonSocialProveedor, Proveedor, FechaCaptura, FechaUltEdicion, IdUsuario)
            VALUES
                (@IdRazonSocial, @Proveedor, GETUTCDATE(), GETUTCDATE(), @IdUsuario);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

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
}
