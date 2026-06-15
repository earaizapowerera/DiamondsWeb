using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio CRUD para registros de asistencia (reloj checador).
/// Tabla: Asistencia. Vista: vAsistencia (JOIN con Usuarios para obtener Nombre).
/// </summary>
public class AsistenciaService
{
    private readonly string _connectionString;
    private readonly ILogger<AsistenciaService> _logger;

    public AsistenciaService(string connectionString, ILogger<AsistenciaService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista registros de asistencia con filtros opcionales.
    /// </summary>
    public async Task<List<AsistenciaItem>> GetAllAsync(
        int? idEmpleado = null,
        string? movimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null)
    {
        const string sql = @"
            SELECT TOP 50
                IdAsistencia, FechaCaptura, IdUsuario, Movimiento, Nombre
            FROM vAsistencia
            WHERE IdAsistencia > 0
              AND (@IdEmpleado IS NULL OR IdUsuario = @IdEmpleado)
              AND (@Movimiento IS NULL OR Movimiento = @Movimiento)
              AND (@FechaDesde IS NULL OR FechaCaptura >= @FechaDesde)
              AND (@FechaHasta IS NULL OR FechaCaptura <= @FechaHasta)
            ORDER BY FechaCaptura DESC";

        using var db = CreateConnection();
        var result = await db.QueryAsync<AsistenciaItem>(sql, new
        {
            IdEmpleado = idEmpleado,
            Movimiento = movimiento,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });
        return result.ToList();
    }

    /// <summary>
    /// Obtiene un registro de asistencia por ID.
    /// </summary>
    public async Task<AsistenciaItem?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT TOP 1
                IdAsistencia, FechaCaptura, IdUsuario, Movimiento, Nombre
            FROM vAsistencia
            WHERE IdAsistencia = @Id";

        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<AsistenciaItem>(sql, new { Id = id });
    }

    /// <summary>
    /// Registra una nueva entrada o salida de empleado.
    /// </summary>
    public async Task<int> CreateAsync(int idUsuario, string movimiento)
    {
        const string sql = @"
            INSERT INTO Asistencia (IdUsuario, Movimiento, FechaCaptura)
            VALUES (@IdUsuario, @Movimiento, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT)";

        using var db = CreateConnection();
        var id = await db.QuerySingleAsync<int>(sql, new
        {
            IdUsuario = idUsuario,
            Movimiento = movimiento
        });

        _logger.LogInformation(
            "Asistencia registrada: Id={Id}, Empleado={Empleado}, Movimiento={Mov}",
            id, idUsuario, movimiento);

        return id;
    }

    /// <summary>
    /// Actualiza un registro de asistencia existente.
    /// </summary>
    public async Task<bool> UpdateAsync(int idAsistencia, int idUsuario, string movimiento)
    {
        const string sql = @"
            UPDATE Asistencia
            SET IdUsuario = @IdUsuario,
                Movimiento = @Movimiento
            WHERE IdAsistencia = @IdAsistencia";

        using var db = CreateConnection();
        var rows = await db.ExecuteAsync(sql, new
        {
            IdAsistencia = idAsistencia,
            IdUsuario = idUsuario,
            Movimiento = movimiento
        });

        _logger.LogInformation("Asistencia actualizada: Id={Id}, Rows={Rows}", idAsistencia, rows);
        return rows > 0;
    }

    /// <summary>
    /// Elimina un registro de asistencia.
    /// </summary>
    public async Task<bool> DeleteAsync(int idAsistencia)
    {
        const string sql = "DELETE FROM Asistencia WHERE IdAsistencia = @IdAsistencia";

        using var db = CreateConnection();
        var rows = await db.ExecuteAsync(sql, new { IdAsistencia = idAsistencia });

        _logger.LogInformation("Asistencia eliminada: Id={Id}, Rows={Rows}", idAsistencia, rows);
        return rows > 0;
    }

    /// <summary>
    /// Lista empleados para el dropdown de seleccion.
    /// </summary>
    public async Task<List<EmpleadoItem>> GetEmpleadosAsync()
    {
        const string sql = @"
            SELECT TOP 50 IdUsuario, Nombre
            FROM Usuarios
            ORDER BY Nombre";

        using var db = CreateConnection();
        var result = await db.QueryAsync<EmpleadoItem>(sql);
        return result.ToList();
    }
}
