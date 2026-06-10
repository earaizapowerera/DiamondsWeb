using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// CRUD de opciones de pago -- migracion de frmOpcionesPago.frm (VB6).
/// Tabla: OpcionesPago | Vista: vOpcionesPago (JOIN Monedas + Usuarios).
/// </summary>
public class OpcionPagoService
{
    private readonly string _connectionString;
    private readonly ILogger<OpcionPagoService> _logger;

    public OpcionPagoService(string connectionString, ILogger<OpcionPagoService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista todas las opciones de pago con moneda y usuario (vista vOpcionesPago).
    /// </summary>
    public async Task<List<OpcionPago>> ObtenerTodasAsync()
    {
        const string sql = @"
            SELECT TOP 50
                v.IdOpcionPago,
                v.OpcionPago AS Nombre,
                v.IdMoneda,
                v.Moneda AS NombreMoneda,
                v.Logo,
                v.Activa,
                v.FechaCaptura,
                v.FechaUltEdicion,
                v.IdUsuario,
                v.Nombre AS NombreUsuario
            FROM vOpcionesPago v
            ORDER BY v.OpcionPago";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<OpcionPago>(sql)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener opciones de pago");
            throw;
        }
    }

    /// <summary>
    /// Obtiene una opcion de pago por su Id.
    /// </summary>
    public async Task<OpcionPago?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"
            SELECT TOP 1
                v.IdOpcionPago,
                v.OpcionPago AS Nombre,
                v.IdMoneda,
                v.Moneda AS NombreMoneda,
                v.Logo,
                v.Activa,
                v.FechaCaptura,
                v.FechaUltEdicion,
                v.IdUsuario,
                v.Nombre AS NombreUsuario
            FROM vOpcionesPago v
            WHERE v.IdOpcionPago = @Id";

        try
        {
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<OpcionPago>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener opcion de pago {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Crea una opcion de pago nueva.
    /// </summary>
    public async Task<int> CrearAsync(string nombre, int idMoneda, string? logo, bool activa)
    {
        const string sql = @"
            INSERT INTO OpcionesPago (OpcionPago, IdMoneda, Logo, Activa, FechaCaptura, FechaUltEdicion, IdUsuario)
            VALUES (@Nombre, @IdMoneda, @Logo, @Activa, GETUTCDATE(), GETUTCDATE(), 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT)";

        try
        {
            using var conn = CreateConnection();
            var id = await conn.ExecuteScalarAsync<int>(sql, new
            {
                Nombre = nombre.Trim(),
                IdMoneda = idMoneda,
                Logo = logo,
                Activa = activa
            });
            _logger.LogInformation("Opcion de pago creada: Id={Id}, Nombre={Nombre}", id, nombre);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear opcion de pago {Nombre}", nombre);
            throw;
        }
    }

    /// <summary>
    /// Actualiza una opcion de pago existente.
    /// </summary>
    public async Task ActualizarAsync(int id, string nombre, int idMoneda, string? logo, bool activa)
    {
        const string sql = @"
            UPDATE OpcionesPago
            SET OpcionPago = @Nombre,
                IdMoneda = @IdMoneda,
                Logo = @Logo,
                Activa = @Activa,
                FechaUltEdicion = GETUTCDATE()
            WHERE IdOpcionPago = @Id";

        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                Id = id,
                Nombre = nombre.Trim(),
                IdMoneda = idMoneda,
                Logo = logo,
                Activa = activa
            });
            _logger.LogInformation("Opcion de pago actualizada: Id={Id}, Nombre={Nombre}", id, nombre);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar opcion de pago {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Activa o desactiva una opcion de pago.
    /// </summary>
    public async Task CambiarActivaAsync(int id, bool activa)
    {
        const string sql = @"
            UPDATE OpcionesPago
            SET Activa = @Activa,
                FechaUltEdicion = GETUTCDATE()
            WHERE IdOpcionPago = @Id";

        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id, Activa = activa });
            _logger.LogInformation("Opcion de pago {Id} activa={Activa}", id, activa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar estado de opcion de pago {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Elimina una opcion de pago por Id.
    /// </summary>
    public async Task EliminarAsync(int id)
    {
        const string sql = "DELETE FROM OpcionesPago WHERE IdOpcionPago = @Id";

        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id });
            _logger.LogInformation("Opcion de pago eliminada: Id={Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar opcion de pago {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Lista monedas para el dropdown de seleccion.
    /// </summary>
    public async Task<List<MonedaItem>> ObtenerMonedasAsync()
    {
        const string sql = @"
            SELECT TOP 50
                IdMoneda,
                Moneda,
                Extranjera
            FROM Monedas
            ORDER BY Moneda";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<MonedaItem>(sql)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener monedas para dropdown");
            throw;
        }
    }
}
