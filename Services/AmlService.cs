using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio de cálculo y monitoreo de Anti-Lavado de Dinero (AML)
/// Basado en Art. 17, Frac. VI de la LFPIORPI
/// Los montos se obtienen de BAJASPAGOSNOTAS.Importe (no de BAJASNOTAS.Total que está vacío)
/// </summary>
public class AmlService
{
    private readonly string _connectionString;
    private readonly AmlConfig _config;
    private readonly ILogger<AmlService> _logger;

    public AmlService(string connectionString, AmlConfig config, ILogger<AmlService> logger)
    {
        _connectionString = connectionString;
        _config = config;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Calcula las fechas del período de 6 meses para un mes/año dado
    /// </summary>
    private (DateTime desde, DateTime hasta) CalcularPeriodo(int mes, int anio)
    {
        var hasta = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes));
        var desde = hasta.AddMonths(-5);
        desde = new DateTime(desde.Year, desde.Month, 1);
        return (desde, hasta);
    }

    /// <summary>
    /// Obtiene clientes que deben reportarse para un mes dado.
    /// Acumula los 6 meses anteriores al mes seleccionado.
    /// Excluye clientes ya reportados en el mismo mes/año.
    /// </summary>
    /// <summary>
    /// Obtiene el config con UMA ajustado al mes/año seleccionado
    /// </summary>
    private AmlConfig ConfigParaMes(int mes, int anio) => _config.ParaMesAnio(mes, anio);

    public async Task<List<ClienteAmlResumen>> ObtenerClientesParaReporteAsync(
        int mes, int anio, string? buscarCliente, string? nivelAlerta)
    {
        var (fechaDesde, fechaHasta) = CalcularPeriodo(mes, anio);
        var cfg = ConfigParaMes(mes, anio);

        _logger.LogInformation(
            "ObtenerClientes: mes={Mes}, anio={Anio}, periodo={Desde} a {Hasta}, UMA={Uma}, umbral={Umbral}",
            mes, anio, fechaDesde.ToString("yyyy-MM-dd"), fechaHasta.ToString("yyyy-MM-dd"),
            cfg.ValorUMA, cfg.MontoIdentificacion);

        // Excluir notas pagadas 100% en Pesos.
        // Para notas mixtas (Pesos + otra forma), incluir solo si Pesos <= 50% del total.
        // IdOpcionPago=6 es "Pesos" en OPCIONESPAGO.
        var sql = @"
            SELECT TOP 100 ca.NombreCliente, ca.RFC, ca.Telefonos,
                   ca.TotalAcumulado, ca.NumeroOperaciones,
                   ca.PrimeraOperacion, ca.UltimaOperacion,
                   CASE WHEN r.Id IS NOT NULL THEN 1 ELSE 0 END AS YaReportado,
                   r.FechaReporte AS FechaReportePrevio
            FROM (
                SELECT
                    COALESCE(h.NombreCanonical, bn.NombreCliente) AS NombreCliente,
                    MAX(bn.RFC) AS RFC,
                    MAX(bn.Telefonos) AS Telefonos,
                    SUM(bp.Importe) AS TotalAcumulado,
                    COUNT(DISTINCT bn.IdNota) AS NumeroOperaciones,
                    MIN(bn.FechaBaja) AS PrimeraOperacion,
                    MAX(bn.FechaBaja) AS UltimaOperacion
                FROM BAJASPAGOSNOTAS bp
                INNER JOIN BAJASNOTAS bn ON bn.IdNota = bp.IdNota
                LEFT JOIN AML_Homologacion h ON h.NombreOriginal = bn.NombreCliente AND h.Aprobado = 1
                WHERE bn.FechaBaja >= @FechaDesde
                  AND bn.FechaBaja <= @FechaHasta
                  AND bn.NombreCliente IS NOT NULL
                  AND LTRIM(RTRIM(bn.NombreCliente)) <> ''
                  AND bn.IdNota NOT IN (
                      SELECT pn.IdNota
                      FROM BAJASPAGOSNOTAS pn
                      GROUP BY pn.IdNota
                      HAVING ISNULL(SUM(CASE WHEN pn.IdOpcionPago = 6 THEN pn.Importe ELSE 0 END), 0)
                             > SUM(pn.Importe) * 0.5
                  )
                  AND (@BuscarCliente IS NULL OR
                       COALESCE(h.NombreCanonical, bn.NombreCliente) LIKE '%' + @BuscarCliente + '%' OR
                       bn.RFC LIKE '%' + @BuscarCliente + '%' OR
                       bn.Telefonos LIKE '%' + @BuscarCliente + '%')
                GROUP BY COALESCE(h.NombreCanonical, bn.NombreCliente)
                HAVING SUM(bp.Importe) >= @MontoIdentificacion
            ) ca
            LEFT JOIN AML_Reportados r
                ON r.NombreCliente = ca.NombreCliente
                AND r.MesReporte = @Mes AND r.AnioReporte = @Anio
            ORDER BY ca.TotalAcumulado DESC";

        try
        {
            using var conn = CreateConnection();
            conn.Open();
            _logger.LogInformation("Conexión abierta a: {Database}", ((SqlConnection)conn).Database);

            var resultados = (await conn.QueryAsync<ClienteAmlResumen>(sql, new
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                BuscarCliente = string.IsNullOrWhiteSpace(buscarCliente) ? null : buscarCliente,
                MontoIdentificacion = cfg.MontoIdentificacion,
                Mes = mes,
                Anio = anio
            })).ToList();

            _logger.LogInformation("Query retornó {Count} clientes", resultados.Count);

            // Clasificar nivel de alerta
            foreach (var cliente in resultados)
            {
                if (cliente.TotalAcumulado >= cfg.MontoAvisoSAT)
                {
                    cliente.NivelAlerta = "AvisoSAT";
                    cliente.RequiereIdentificacion = true;
                    cliente.RequiereAvisoSAT = true;
                }
                else
                {
                    cliente.NivelAlerta = "Identificacion";
                    cliente.RequiereIdentificacion = true;
                    cliente.RequiereAvisoSAT = false;
                }
            }

            if (!string.IsNullOrEmpty(nivelAlerta) && nivelAlerta != "Todos")
                resultados = resultados.Where(r => r.NivelAlerta == nivelAlerta).ToList();

            return resultados;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar clientes AML para {Mes}/{Anio}", mes, anio);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el detalle de notas/pagos de un cliente en el período de 6 meses
    /// </summary>
    public async Task<List<NotaDetalle>> ObtenerNotasClienteAsync(
        string nombreCliente, int mes, int anio)
    {
        var (fechaDesde, fechaHasta) = CalcularPeriodo(mes, anio);

        // Buscar por nombre exacto O por cualquier variante homologada al nombre canónico
        var sql = @"
            SELECT TOP 500
                bn.IdNota,
                bn.NombreCliente,
                bn.RFC,
                bn.Telefonos,
                SUM(bp.Importe) AS Total,
                bn.FechaBaja,
                bn.FormaPago
            FROM BAJASNOTAS bn
            INNER JOIN BAJASPAGOSNOTAS bp ON bp.IdNota = bn.IdNota
            WHERE (bn.NombreCliente = @NombreCliente
                   OR bn.NombreCliente IN (
                       SELECT NombreOriginal FROM AML_Homologacion
                       WHERE NombreCanonical = @NombreCliente AND Aprobado = 1))
              AND bn.FechaBaja >= @FechaDesde
              AND bn.FechaBaja <= @FechaHasta
            GROUP BY bn.IdNota, bn.NombreCliente, bn.RFC, bn.Telefonos, bn.FechaBaja, bn.FormaPago
            ORDER BY bn.FechaBaja DESC";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<NotaDetalle>(sql, new
            {
                NombreCliente = nombreCliente,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
            })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar notas de {Cliente} para {Mes}/{Anio}",
                nombreCliente, mes, anio);
            throw;
        }
    }

    /// <summary>
    /// Obtiene estadísticas para el mes seleccionado
    /// </summary>
    public async Task<AmlDashboardStats> ObtenerEstadisticasAsync(int mes, int anio)
    {
        var (fechaDesde, fechaHasta) = CalcularPeriodo(mes, anio);
        var cfg = ConfigParaMes(mes, anio);

        var sql = @"
            SELECT TOP 1
                COUNT(*) AS TotalClientes,
                SUM(CASE WHEN TotalAcumulado >= @MontoIdentificacion AND TotalAcumulado < @MontoAvisoSAT THEN 1 ELSE 0 END) AS ClientesIdentificacion,
                SUM(CASE WHEN TotalAcumulado >= @MontoAvisoSAT THEN 1 ELSE 0 END) AS ClientesAvisoSAT,
                ISNULL(SUM(TotalAcumulado), 0) AS MontoTotalVentas,
                ISNULL(SUM(NumOperaciones), 0) AS TotalOperaciones
            FROM (
                SELECT
                    COALESCE(h.NombreCanonical, bn.NombreCliente) AS NombreCliente,
                    SUM(bp.Importe) AS TotalAcumulado,
                    COUNT(DISTINCT bn.IdNota) AS NumOperaciones
                FROM BAJASPAGOSNOTAS bp
                INNER JOIN BAJASNOTAS bn ON bn.IdNota = bp.IdNota
                LEFT JOIN AML_Homologacion h ON h.NombreOriginal = bn.NombreCliente AND h.Aprobado = 1
                WHERE bn.FechaBaja >= @FechaDesde
                  AND bn.FechaBaja <= @FechaHasta
                  AND bn.NombreCliente IS NOT NULL
                  AND LTRIM(RTRIM(bn.NombreCliente)) <> ''
                  AND bn.IdNota NOT IN (
                      SELECT pn.IdNota
                      FROM BAJASPAGOSNOTAS pn
                      GROUP BY pn.IdNota
                      HAVING ISNULL(SUM(CASE WHEN pn.IdOpcionPago = 6 THEN pn.Importe ELSE 0 END), 0)
                             > SUM(pn.Importe) * 0.5
                  )
                GROUP BY COALESCE(h.NombreCanonical, bn.NombreCliente)
            ) sub";

        try
        {
            using var conn = CreateConnection();
            var stats = await conn.QueryFirstOrDefaultAsync<AmlDashboardStats>(sql, new
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                MontoIdentificacion = cfg.MontoIdentificacion,
                MontoAvisoSAT = cfg.MontoAvisoSAT
            }) ?? new AmlDashboardStats();

            _logger.LogInformation(
                "Stats: {Total} clientes, {Ident} identificación, {SAT} aviso SAT, {Monto:C2} ventas totales",
                stats.TotalClientes, stats.ClientesIdentificacion,
                stats.ClientesAvisoSAT, stats.MontoTotalVentas);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas AML para {Mes}/{Anio}", mes, anio);
            throw;
        }
    }

    /// <summary>
    /// Marca un cliente como reportado para un mes/año
    /// </summary>
    public async Task MarcarComoReportadoAsync(string nombreCliente, string? rfc, string? telefonos,
        int mes, int anio, decimal totalAcumulado, int numOperaciones, string nivelAlerta,
        string? reportadoPor, string? observaciones)
    {
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM AML_Reportados
                           WHERE NombreCliente = @NombreCliente
                             AND MesReporte = @Mes AND AnioReporte = @Anio)
                INSERT INTO AML_Reportados (NombreCliente, RFC, Telefonos, MesReporte, AnioReporte,
                    TotalAcumulado, NumeroOperaciones, NivelAlerta, ReportadoPor, Observaciones)
                VALUES (@NombreCliente, @RFC, @Telefonos, @Mes, @Anio,
                    @TotalAcumulado, @NumOperaciones, @NivelAlerta, @ReportadoPor, @Observaciones)";

        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                NombreCliente = nombreCliente,
                RFC = rfc,
                Telefonos = telefonos,
                Mes = mes,
                Anio = anio,
                TotalAcumulado = totalAcumulado,
                NumOperaciones = numOperaciones,
                NivelAlerta = nivelAlerta,
                ReportadoPor = reportadoPor,
                Observaciones = observaciones
            });

            _logger.LogInformation("Cliente {Cliente} marcado como reportado para {Mes}/{Anio}",
                nombreCliente, mes, anio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar como reportado a {Cliente} para {Mes}/{Anio}",
                nombreCliente, mes, anio);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el historial de clientes reportados
    /// </summary>
    public async Task<List<ClienteReportado>> ObtenerHistorialReportadosAsync(
        int? mes = null, int? anio = null)
    {
        var sql = @"
            SELECT TOP 200 Id, NombreCliente, RFC, Telefonos, MesReporte, AnioReporte,
                   TotalAcumulado, NumeroOperaciones, NivelAlerta, FechaReporte,
                   ReportadoPor, Observaciones
            FROM AML_Reportados
            WHERE (@Mes IS NULL OR MesReporte = @Mes)
              AND (@Anio IS NULL OR AnioReporte = @Anio)
            ORDER BY AnioReporte DESC, MesReporte DESC, TotalAcumulado DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<ClienteReportado>(sql, new { Mes = mes, Anio = anio })).ToList();
    }

    /// <summary>
    /// Test simple de conectividad a la base de datos
    /// </summary>
    public async Task<string> TestConexionAsync()
    {
        try
        {
            using var conn = CreateConnection();
            conn.Open();
            var dbName = ((SqlConnection)conn).Database;
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM BAJASNOTAS WHERE NombreCliente IS NOT NULL AND LTRIM(RTRIM(NombreCliente)) <> ''");
            return $"OK - DB: {dbName}, Notas con cliente: {count}";
        }
        catch (Exception ex)
        {
            return $"ERROR - {ex.Message}";
        }
    }

    public AmlConfig ObtenerConfiguracion() => _config;
}

public class AmlDashboardStats
{
    public int TotalClientes { get; set; }
    public int ClientesIdentificacion { get; set; }
    public int ClientesAvisoSAT { get; set; }
    public decimal MontoTotalVentas { get; set; }
    public int TotalOperaciones { get; set; }
}
