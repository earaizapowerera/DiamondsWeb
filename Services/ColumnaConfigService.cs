using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para gestionar configuraciones de columnas de grids.
/// Usa las tablas legacy TablasColumnas y Columnas del VB6.
/// Convención: Ancho = 0 → columna oculta, Ancho > 0 → visible.
/// </summary>
public class ColumnaConfigService
{
    private readonly string _connectionString;
    private readonly ILogger<ColumnaConfigService> _logger;

    public ColumnaConfigService(string connectionString, ILogger<ColumnaConfigService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Obtiene todas las configuraciones guardadas para una vista específica.
    /// </summary>
    public async Task<List<TablaColumnas>> ObtenerConfiguracionesAsync(string vista)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 IdTablaColumnas, Descripcion, Vista, FechaCaptura, FechaUltEdicion, UsuarioId
                    FROM TablasColumnas
                    WHERE Vista = @Vista
                    ORDER BY Descripcion";
        return (await db.QueryAsync<TablaColumnas>(sql, new { Vista = vista })).ToList();
    }

    /// <summary>
    /// Obtiene las columnas de una configuración específica.
    /// Retorna la visibilidad de cada columna (Ancho > 0 = visible).
    /// </summary>
    public async Task<ColumnaConfigResponse?> ObtenerConfiguracionAsync(int idTablaColumnas)
    {
        using var db = CreateConnection();

        var header = await db.QueryFirstOrDefaultAsync<TablaColumnas>(
            "SELECT TOP 1 IdTablaColumnas, Descripcion, Vista FROM TablasColumnas WHERE IdTablaColumnas = @Id",
            new { Id = idTablaColumnas });

        if (header == null) return null;

        var columnas = await db.QueryAsync<ColumnaDetalle>(
            "SELECT TOP 50 IdTablaColumnas, Columna, Ancho FROM Columnas WHERE IdTablaColumnas = @Id",
            new { Id = idTablaColumnas });

        return new ColumnaConfigResponse
        {
            IdTablaColumnas = header.IdTablaColumnas,
            Descripcion = header.Descripcion,
            Vista = header.Vista,
            Columnas = columnas.Select(c => new ColumnaVisibilidad
            {
                Columna = c.Columna,
                Visible = c.Ancho > 0
            }).ToList()
        };
    }

    /// <summary>
    /// Crea una nueva configuración de columnas.
    /// Genera el ID usando la tabla contador (campo tablacolumnas) como el VB6.
    /// </summary>
    public async Task<int> CrearConfiguracionAsync(CrearColumnaConfigRequest request)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        try
        {
            // Obtener siguiente ID del contador (patrón legacy VB6)
            var nextId = await db.QueryFirstAsync<int>(
                "SELECT TOP 1 ISNULL(tablacolumnas, 0) + 1 FROM contador",
                transaction: tx);

            // Actualizar el contador
            await db.ExecuteAsync(
                "UPDATE contador SET tablacolumnas = @NextId",
                new { NextId = nextId },
                transaction: tx);

            // Insertar header
            await db.ExecuteAsync(
                @"INSERT INTO TablasColumnas (IdTablaColumnas, Descripcion, Vista, FechaCaptura, FechaUltEdicion)
                  VALUES (@Id, @Descripcion, @Vista, GETUTCDATE(), GETUTCDATE())",
                new { Id = nextId, request.Descripcion, request.Vista },
                transaction: tx);

            // Insertar columnas (visible = ancho 1000, oculta = ancho 0)
            foreach (var col in request.Columnas)
            {
                await db.ExecuteAsync(
                    @"INSERT INTO Columnas (IdTablaColumnas, Columna, Ancho, FechaCaptura)
                      VALUES (@Id, @Columna, @Ancho, GETUTCDATE())",
                    new { Id = nextId, col.Columna, Ancho = col.Visible ? 1000 : 0 },
                    transaction: tx);
            }

            tx.Commit();
            _logger.LogInformation("Configuración de columnas creada: {Id} - {Desc} para vista {Vista}",
                nextId, request.Descripcion, request.Vista);

            return nextId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Elimina una configuración de columnas (header + detalle).
    /// </summary>
    public async Task<bool> EliminarConfiguracionAsync(int idTablaColumnas)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        try
        {
            await db.ExecuteAsync(
                "DELETE FROM Columnas WHERE IdTablaColumnas = @Id",
                new { Id = idTablaColumnas },
                transaction: tx);

            var deleted = await db.ExecuteAsync(
                "DELETE FROM TablasColumnas WHERE IdTablaColumnas = @Id",
                new { Id = idTablaColumnas },
                transaction: tx);

            tx.Commit();
            _logger.LogInformation("Configuración de columnas eliminada: {Id}", idTablaColumnas);
            return deleted > 0;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
