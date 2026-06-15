using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para ponderar (distribuir) costos extra entre piezas de una remisión o factura.
/// Origen VB6: frmPonderacion.frm
/// </summary>
public class PonderacionService
{
    private readonly string _connectionString;
    private readonly ILogger<PonderacionService> _logger;

    public PonderacionService(string connectionString, ILogger<PonderacionService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Cuenta las piezas que se verían afectadas por la ponderación.
    /// </summary>
    public async Task<int> ContarPiezasAfectadasAsync(int? idRemision, int? idFactura, bool soloSinCosto)
    {
        using var conn = CreateConnection();

        var filtroDoc = idRemision.HasValue
            ? "IdRemision = @Id"
            : "IdFactura = @Id";

        var filtroCosto = soloSinCosto
            ? " AND (CBManoObra IS NULL OR CBManoObra = 0)"
            : "";

        var sql = $"SELECT TOP 1 COUNT(*) FROM piezas WHERE {filtroDoc}{filtroCosto}";

        var id = idRemision ?? idFactura!.Value;
        return await conn.ExecuteScalarAsync<int>(sql, new { Id = id });
    }

    /// <summary>
    /// Obtiene un resumen de las piezas que se van a actualizar (para preview).
    /// </summary>
    public async Task<List<PiezaPonderacionPreview>> ObtenerPreviewAsync(int? idRemision, int? idFactura, bool soloSinCosto)
    {
        using var conn = CreateConnection();

        var filtroDoc = idRemision.HasValue
            ? "IdRemision = @Id"
            : "IdFactura = @Id";

        var filtroCosto = soloSinCosto
            ? " AND (CBManoObra IS NULL OR CBManoObra = 0)"
            : "";

        var sql = $@"
            SELECT TOP 50
                   CodigoBarras,
                   Descripcion,
                   ISNULL(CNPieza, 0) AS CNPieza,
                   ISNULL(CNPeso, 0) AS CNPeso,
                   ISNULL(CBManoObra, 0) AS CBManoObra,
                   ISNULL(CNManoObra, 0) AS CNManoObra,
                   DescripcionManoObra,
                   ISNULL(CBTotal, 0) AS CBTotal,
                   ISNULL(CNTotal, 0) AS CNTotal,
                   Precio
              FROM piezas
             WHERE {filtroDoc}{filtroCosto}
             ORDER BY CodigoBarras";

        var id = idRemision ?? idFactura!.Value;
        return (await conn.QueryAsync<PiezaPonderacionPreview>(sql, new { Id = id })).ToList();
    }

    /// <summary>
    /// Ejecuta la ponderación: aplica el porcentaje de costo extra a las piezas y recalcula totales y precio.
    /// Reproduce la lógica de frmPonderacion.cmdRegistrar_Click del VB6.
    /// </summary>
    public async Task<int> EjecutarPonderacionAsync(int? idRemision, int? idFactura, decimal porcentaje, string concepto, bool soloSinCosto)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = ((SqlConnection)conn).BeginTransaction();

        try
        {
            var filtroDoc = idRemision.HasValue
                ? "IdRemision = @Id"
                : "IdFactura = @Id";

            var filtroCosto = soloSinCosto
                ? " AND (CBManoObra IS NULL OR CBManoObra = 0)"
                : "";

            var id = idRemision ?? idFactura!.Value;
            var parametros = new { Id = id, Factor = porcentaje, Concepto = concepto };

            // Paso 1: Calcular mano de obra como porcentaje del costo (pieza + peso)
            // cbmanoobra = (cnpieza + cnpeso) / 100 * factor
            // cnmanoobra = igual (mismo cálculo en VB6 original)
            var sql1 = $@"
                UPDATE piezas SET
                    CBManoObra  = CAST((ISNULL(CNPieza, 0) + ISNULL(CNPeso, 0)) / 100.0 * @Factor AS DECIMAL(18, 2)),
                    CNManoObra  = CAST((ISNULL(CNPieza, 0) + ISNULL(CNPeso, 0)) / 100.0 * @Factor AS DECIMAL(18, 2)),
                    DescManoObra = 0,
                    DescripcionManoObra = @Concepto
                WHERE {filtroDoc}{filtroCosto}";

            var afectadas = await conn.ExecuteAsync(sql1, parametros, tx);

            if (afectadas == 0)
            {
                tx.Rollback();
                return 0;
            }

            // Paso 2: Recalcular totales (bruto y neto)
            // cbtotal = cbpieza + cbpeso + cbmanoobra
            // cntotal = cnpieza + cnpeso + cnmanoobra
            var sql2 = $@"
                UPDATE piezas SET
                    CBTotal = ISNULL(CBPieza, 0) + ISNULL(CBPeso, 0) + ISNULL(CBManoObra, 0),
                    CNTotal = ISNULL(CNPieza, 0) + ISNULL(CNPeso, 0) + ISNULL(CNManoObra, 0)
                WHERE {filtroDoc}{filtroCosto}";

            // Nota: filtroCosto ya no aplica aquí porque paso 1 ya puso valor en CBManoObra.
            // Pero usamos la misma cláusula para mantener consistencia con el scope original.
            // En realidad, el VB6 usaba una lista de códigos de barras capturada antes del update.
            // Aquí usamos un enfoque más limpio: los 3 updates van en transacción.
            // Re-aplicamos sin filtro de costo porque ya se actualizó en paso 1.
            var sql2Final = $@"
                UPDATE piezas SET
                    CBTotal = ISNULL(CBPieza, 0) + ISNULL(CBPeso, 0) + ISNULL(CBManoObra, 0),
                    CNTotal = ISNULL(CNPieza, 0) + ISNULL(CNPeso, 0) + ISNULL(CNManoObra, 0)
                WHERE {filtroDoc}
                  AND DescripcionManoObra = @Concepto";

            await conn.ExecuteAsync(sql2Final, parametros, tx);

            // Paso 3: Recalcular precio de venta
            // Precio = cntotal * utilidad * utilidadextra * impuesto / divisor * tccotizacion
            var sql3 = $@"
                UPDATE piezas SET
                    Precio = CAST(
                        CNTotal
                        * ISNULL(Utilidad, 1)
                        * ISNULL(UtilidadExtra, 1)
                        * ISNULL(Impuesto, 1)
                        / NULLIF(ISNULL(Divisor, 1), 0)
                        * ISNULL(TCCotizacion, 1)
                    AS INT)
                WHERE {filtroDoc}
                  AND DescripcionManoObra = @Concepto";

            await conn.ExecuteAsync(sql3, parametros, tx);

            tx.Commit();

            _logger.LogInformation(
                "Ponderación ejecutada: {Tipo}={Id}, Factor={Factor}%, Concepto={Concepto}, Piezas={Count}, SoloSinCosto={SoloSinCosto}",
                idRemision.HasValue ? "Remisión" : "Factura", id, porcentaje, concepto, afectadas, soloSinCosto);

            return afectadas;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

/// <summary>
/// DTO para preview de piezas que se van a ponderar.
/// </summary>
public class PiezaPonderacionPreview
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal CNPieza { get; set; }
    public decimal CNPeso { get; set; }
    public decimal CBManoObra { get; set; }
    public decimal CNManoObra { get; set; }
    public string? DescripcionManoObra { get; set; }
    public decimal CBTotal { get; set; }
    public decimal CNTotal { get; set; }
    public int Precio { get; set; }
}
