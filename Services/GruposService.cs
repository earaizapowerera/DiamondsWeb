using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// CRUD para catálogo de Grupos (categorías de productos).
/// Origen VB6: frmGrupos.frm. Tablas: Grupos, vGrupos.
/// </summary>
public class GruposService
{
    private readonly string _connectionString;
    private readonly ILogger<GruposService> _logger;

    public GruposService(string connectionString, ILogger<GruposService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista todos los grupos con nombre de usuario (vista vGrupos).
    /// Opcionalmente filtra por nombre de grupo.
    /// </summary>
    public async Task<List<GrupoItem>> ListarAsync(string? buscar = null)
    {
        using var db = CreateConnection();

        var sql = "SELECT TOP 50 IdGrupo, Grupo, FechaCaptura, IdUsuario, FechaUltEdicion, Nombre FROM vGrupos";
        var where = "WHERE IdGrupo > 0";

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            where += " AND Grupo LIKE @Buscar";
        }

        sql += $" {where} ORDER BY Grupo";

        var result = await db.QueryAsync<GrupoItem>(sql, new { Buscar = $"%{buscar}%" });
        return result.ToList();
    }

    /// <summary>
    /// Obtiene un grupo por su Id.
    /// </summary>
    public async Task<GrupoItem?> ObtenerPorIdAsync(int idGrupo)
    {
        using var db = CreateConnection();

        var sql = "SELECT TOP 1 IdGrupo, Grupo, FechaCaptura, IdUsuario, FechaUltEdicion, Nombre FROM vGrupos WHERE IdGrupo = @IdGrupo";
        return await db.QueryFirstOrDefaultAsync<GrupoItem>(sql, new { IdGrupo = idGrupo });
    }

    /// <summary>
    /// Crea un nuevo grupo. Inserta en ambas conexiones como el VB6 original.
    /// </summary>
    public async Task<int> CrearAsync(string grupo, int idUsuario)
    {
        using var db = CreateConnection();

        var sql = @"INSERT INTO Grupos (Grupo, FechaCaptura, IdUsuario)
                    VALUES (@Grupo, GETUTCDATE(), @IdUsuario);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var id = await db.QuerySingleAsync<int>(sql, new { Grupo = grupo.Trim(), IdUsuario = idUsuario });
        _logger.LogInformation("Grupo creado: Id={Id}, Nombre={Grupo}", id, grupo);
        return id;
    }

    /// <summary>
    /// Actualiza el nombre de un grupo existente.
    /// </summary>
    public async Task<bool> ActualizarAsync(int idGrupo, string grupo, int idUsuario)
    {
        using var db = CreateConnection();

        var sql = @"UPDATE Grupos
                    SET Grupo = @Grupo, FechaUltEdicion = GETUTCDATE(), IdUsuario = @IdUsuario
                    WHERE IdGrupo = @IdGrupo";

        var rows = await db.ExecuteAsync(sql, new { IdGrupo = idGrupo, Grupo = grupo.Trim(), IdUsuario = idUsuario });
        _logger.LogInformation("Grupo actualizado: Id={Id}, Nombre={Grupo}", idGrupo, grupo);
        return rows > 0;
    }

    /// <summary>
    /// Elimina un grupo por Id.
    /// </summary>
    public async Task<bool> EliminarAsync(int idGrupo)
    {
        using var db = CreateConnection();

        var sql = "DELETE FROM Grupos WHERE IdGrupo = @IdGrupo";
        var rows = await db.ExecuteAsync(sql, new { IdGrupo = idGrupo });
        _logger.LogInformation("Grupo eliminado: Id={Id}", idGrupo);
        return rows > 0;
    }

    /// <summary>
    /// Verifica si existe un grupo con el mismo nombre (para evitar duplicados).
    /// </summary>
    public async Task<bool> ExisteNombreAsync(string grupo, int? excluirId = null)
    {
        using var db = CreateConnection();

        var sql = "SELECT TOP 1 COUNT(*) FROM Grupos WHERE Grupo = @Grupo";
        if (excluirId.HasValue)
        {
            sql += " AND IdGrupo <> @ExcluirId";
        }

        var count = await db.QuerySingleAsync<int>(sql, new { Grupo = grupo.Trim(), ExcluirId = excluirId });
        return count > 0;
    }
}
