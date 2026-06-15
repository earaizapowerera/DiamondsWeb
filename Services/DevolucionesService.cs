using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para buscar piezas vendidas y reestablecerlas al inventario.
/// Migrado de frmDevolucionesCliente.frm (VB6).
/// Usa sp_ReestablecerDevolucion para mover pieza de bajaspiezas a piezas.
/// </summary>
public class DevolucionesService
{
    private readonly string _connectionString;
    private readonly ILogger<DevolucionesService> _logger;

    public DevolucionesService(string connectionString, ILogger<DevolucionesService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Busca una pieza vendida por codigo de barras.
    /// Retorna info de compra (cliente, precio, descuento, forma de pago, tienda).
    /// </summary>
    public async Task<PiezaDevolucion?> BuscarPiezaAsync(string codigoBarras)
    {
        const string sql = @"
            SELECT TOP 1
                pn.codigobarras  AS CodigoBarras,
                pn.Descripcion,
                pn.total         AS Precio,
                pn.idnota        AS IdNota,
                bn.descuento     AS Descuento,
                bn.NombreCliente,
                ISNULL(bn.FechaBaja, bn.FechaCaptura) AS FechaCompra,
                t.NombreTienda   AS Tienda,
                bn.formapago     AS FormaPago,
                CASE WHEN bp.codigobarras IS NOT NULL THEN 1 ELSE 0 END AS EnBajas
            FROM piezasnotas pn
            INNER JOIN bajasnotas bn ON bn.idnota = pn.idnota
            LEFT JOIN bajaspiezas bp ON bp.codigobarras = pn.codigobarras
            INNER JOIN tiendas t ON t.idtienda = bn.idtienda
            WHERE pn.codigobarras = @CodigoBarras";

        using var db = CreateConnection();
        var result = await db.QueryFirstOrDefaultAsync<PiezaDevolucion>(sql, new { CodigoBarras = codigoBarras });

        if (result != null)
        {
            _logger.LogInformation("Pieza {CB} encontrada. EnBajas={EnBajas}, Cliente={Cliente}",
                codigoBarras, result.EnBajas, result.NombreCliente);
        }
        else
        {
            _logger.LogWarning("Pieza {CB} no encontrada en piezasnotas/bajasnotas", codigoBarras);
        }

        return result;
    }

    /// <summary>
    /// Verifica si la pieza ya fue reestablecida previamente.
    /// </summary>
    public async Task<DateTime?> VerificarReestablecimientoPrevioAsync(string codigoBarras)
    {
        const string sql = @"
            SELECT TOP 1 FechaCaptura
            FROM piezasreestablecidas
            WHERE codigobarras = @CodigoBarras
            ORDER BY FechaCaptura DESC";

        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<DateTime?>(sql, new { CodigoBarras = codigoBarras });
    }

    /// <summary>
    /// Reestablece una pieza al inventario usando sp_ReestablecerDevolucion.
    /// Mueve la pieza de bajaspiezas a piezas, de bajasetiquetas a etiquetas,
    /// y registra en piezasreestablecidas.
    /// </summary>
    public async Task<ResultadoReestablecimiento> ReestablecerPiezaAsync(
        string codigoBarras, int idTienda, int idUsuario, string usuario)
    {
        try
        {
            // Verificar que la pieza esta en bajas
            var pieza = await BuscarPiezaAsync(codigoBarras);
            if (pieza == null)
                return new ResultadoReestablecimiento
                {
                    Exito = false,
                    Mensaje = "La pieza no existe en el sistema."
                };

            if (!pieza.EnBajas)
                return new ResultadoReestablecimiento
                {
                    Exito = false,
                    Mensaje = "La pieza no esta en bajas. Puede que ya haya sido devuelta o este en existencia."
                };

            // Verificar si ya fue reestablecida
            var fechaPrevia = await VerificarReestablecimientoPrevioAsync(codigoBarras);
            if (fechaPrevia.HasValue)
                return new ResultadoReestablecimiento
                {
                    Exito = false,
                    Mensaje = $"La pieza ya fue reestablecida el {fechaPrevia.Value:dd/MM/yyyy HH:mm}."
                };

            // Ejecutar SP: sp_ReestablecerDevolucion @CodigoBarras, @Motivo, @IdUsuario, @IdTienda
            // Motivo 'DevCli' = Devolucion de Cliente (mismo que el VB6 original)
            using var db = CreateConnection();
            await db.ExecuteAsync(
                "sp_ReestablecerDevolucion",
                new
                {
                    CodigoBarras = codigoBarras,
                    Motivo = "DevCli",
                    IdUsuario = idUsuario,
                    IdTienda = idTienda
                },
                commandType: CommandType.StoredProcedure);

            _logger.LogInformation(
                "Pieza {CB} reestablecida exitosamente. Tienda={Tienda}, Usuario={Usuario}",
                codigoBarras, idTienda, usuario);

            return new ResultadoReestablecimiento
            {
                Exito = true,
                Mensaje = $"Pieza {codigoBarras} reestablecida al inventario exitosamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reestablecer pieza {CB}", codigoBarras);
            return new ResultadoReestablecimiento
            {
                Exito = false,
                Mensaje = $"Error al reestablecer: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Obtiene las tiendas disponibles para seleccionar destino de reestablecimiento.
    /// </summary>
    public async Task<List<TiendaInfo>> ObtenerTiendasAsync()
    {
        const string sql = @"
            SELECT TOP 50 IdTienda, NombreTienda
            FROM tiendas
            WHERE IdTienda > 0
            ORDER BY NombreTienda";

        using var db = CreateConnection();
        var result = await db.QueryAsync<TiendaInfo>(sql);
        return result.ToList();
    }
}
