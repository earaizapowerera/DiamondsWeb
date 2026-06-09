using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// CRUD de TablasJerarquias (master) y Jerarquias (detail).
/// Configura qué renglones aparecen en cada tipo de etiqueta.
/// </summary>
public class JerarquiasService
{
    private readonly string _connectionString;
    private readonly ILogger<JerarquiasService> _logger;

    public JerarquiasService(string connectionString, ILogger<JerarquiasService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ── Master: TablasJerarquias ──────────────────────────────

    public async Task<List<TablaJerarquia>> ObtenerTablasAsync(string? buscar = null)
    {
        var sql = @"
            SELECT TOP 50
                t.IdTabla, t.Descripcion, t.IdUsuario, t.FechaCaptura,
                (SELECT COUNT(*) FROM Jerarquias j WHERE j.IdTabla = t.IdTabla) AS CantidadColumnas
            FROM TablasJerarquias t
            WHERE t.IdTabla > 0
              AND (@Buscar IS NULL OR t.Descripcion LIKE '%' + @Buscar + '%')
            ORDER BY t.IdTabla";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<TablaJerarquia>(sql, new { Buscar = buscar })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tablas de jerarquías");
            throw;
        }
    }

    public async Task<TablaJerarquia?> ObtenerTablaPorIdAsync(int idTabla)
    {
        var sql = @"
            SELECT TOP 1
                t.IdTabla, t.Descripcion, t.IdUsuario, t.FechaCaptura,
                (SELECT COUNT(*) FROM Jerarquias j WHERE j.IdTabla = t.IdTabla) AS CantidadColumnas
            FROM TablasJerarquias t
            WHERE t.IdTabla = @IdTabla";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<TablaJerarquia>(sql, new { IdTabla = idTabla });
    }

    public async Task<int> CrearTablaAsync(string descripcion)
    {
        var sql = @"
            INSERT INTO TablasJerarquias (Descripcion, FechaCaptura)
            VALUES (@Descripcion, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS int)";

        try
        {
            using var conn = CreateConnection();
            var id = await conn.ExecuteScalarAsync<int>(sql, new { Descripcion = descripcion });
            _logger.LogInformation("Tabla de jerarquía creada: Id={Id}, Desc={Desc}", id, descripcion);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tabla de jerarquía: {Desc}", descripcion);
            throw;
        }
    }

    public async Task ActualizarTablaAsync(int idTabla, string descripcion)
    {
        var sql = "UPDATE TablasJerarquias SET Descripcion = @Descripcion WHERE IdTabla = @IdTabla";

        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, new { IdTabla = idTabla, Descripcion = descripcion });
            _logger.LogInformation("Tabla de jerarquía actualizada: Id={Id}", idTabla);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tabla de jerarquía: Id={Id}", idTabla);
            throw;
        }
    }

    public async Task<bool> EliminarTablaAsync(int idTabla)
    {
        // Eliminar en cascada: primero las jerarquías hijas, luego la tabla
        var sql = @"
            DELETE FROM Jerarquias WHERE IdTabla = @IdTabla;
            DELETE FROM TablasJerarquias WHERE IdTabla = @IdTabla";

        try
        {
            using var conn = CreateConnection();
            var rows = await conn.ExecuteAsync(sql, new { IdTabla = idTabla });
            _logger.LogInformation("Tabla de jerarquía eliminada: Id={Id}, Rows={Rows}", idTabla, rows);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar tabla de jerarquía: Id={Id}", idTabla);
            throw;
        }
    }

    // ── Detail: Jerarquias ────────────────────────────────────

    public async Task<List<Jerarquia>> ObtenerJerarquiasAsync(int idTabla)
    {
        var sql = @"
            SELECT TOP 50 IdJerarquia, IdTabla, Columna, FechaCaptura
            FROM Jerarquias
            WHERE IdTabla = @IdTabla
            ORDER BY IdJerarquia";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<Jerarquia>(sql, new { IdTabla = idTabla })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener jerarquías de tabla {IdTabla}", idTabla);
            throw;
        }
    }

    public async Task<int> CrearJerarquiaAsync(int idTabla, string columna)
    {
        var sql = @"
            INSERT INTO Jerarquias (IdTabla, Columna, FechaCaptura)
            VALUES (@IdTabla, @Columna, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS int)";

        try
        {
            using var conn = CreateConnection();
            var id = await conn.ExecuteScalarAsync<int>(sql, new { IdTabla = idTabla, Columna = columna });
            _logger.LogInformation("Jerarquía creada: Id={Id}, Tabla={Tabla}, Col={Col}", id, idTabla, columna);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear jerarquía en tabla {IdTabla}", idTabla);
            throw;
        }
    }

    public async Task ActualizarJerarquiaAsync(int idJerarquia, string columna)
    {
        var sql = "UPDATE Jerarquias SET Columna = @Columna WHERE IdJerarquia = @IdJerarquia";

        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, new { IdJerarquia = idJerarquia, Columna = columna });
            _logger.LogInformation("Jerarquía actualizada: Id={Id}", idJerarquia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar jerarquía: Id={Id}", idJerarquia);
            throw;
        }
    }

    public async Task<bool> EliminarJerarquiaAsync(int idJerarquia)
    {
        var sql = "DELETE FROM Jerarquias WHERE IdJerarquia = @IdJerarquia";

        try
        {
            using var conn = CreateConnection();
            var rows = await conn.ExecuteAsync(sql, new { IdJerarquia = idJerarquia });
            _logger.LogInformation("Jerarquía eliminada: Id={Id}", idJerarquia);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar jerarquía: Id={Id}", idJerarquia);
            throw;
        }
    }
}
