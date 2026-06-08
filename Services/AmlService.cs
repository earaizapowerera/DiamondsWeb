using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio de cálculo y monitoreo de Anti-Lavado de Dinero (AML)
/// Basado en Art. 17, Frac. VI de la LFPIORPI
/// </summary>
public class AmlService
{
    private readonly string _connectionString;
    private readonly AmlConfig _config;

    public AmlService(string connectionString, AmlConfig config)
    {
        _connectionString = connectionString;
        _config = config;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Obtiene el resumen de clientes con acumulados en el período de 6 meses,
    /// agrupados por el campo seleccionado (NombreCliente, RFC, o Telefonos)
    /// </summary>
    public async Task<List<ClienteAmlResumen>> ObtenerResumenClientesAsync(AmlFiltros filtros)
    {
        var fechaHasta = filtros.FechaHasta ?? DateTime.UtcNow;
        var fechaDesde = filtros.FechaDesde ?? fechaHasta.AddMonths(-_config.MesesAcumulacion);
        var agrupador = filtros.AgrupadorCliente ?? "NombreCliente";

        // Validar que el agrupador sea un campo permitido
        if (agrupador != "NombreCliente" && agrupador != "RFC" && agrupador != "Telefonos")
            agrupador = "NombreCliente";

        var sql = $@"
            SELECT
                {agrupador} AS NombreCliente,
                MAX(RFC) AS RFC,
                MAX(Telefonos) AS Telefonos,
                SUM(ISNULL(Total, 0)) AS TotalAcumulado,
                COUNT(*) AS NumeroOperaciones,
                MIN(FechaBaja) AS PrimeraOperacion,
                MAX(FechaBaja) AS UltimaOperacion
            FROM BAJASNOTAS
            WHERE FechaBaja >= @FechaDesde
              AND FechaBaja <= @FechaHasta
              AND {agrupador} IS NOT NULL
              AND LTRIM(RTRIM({agrupador})) <> ''
              AND (@BuscarCliente IS NULL OR
                   NombreCliente LIKE '%' + @BuscarCliente + '%' OR
                   RFC LIKE '%' + @BuscarCliente + '%' OR
                   Telefonos LIKE '%' + @BuscarCliente + '%')
            GROUP BY {agrupador}
            HAVING SUM(ISNULL(Total, 0)) > 0
            ORDER BY SUM(ISNULL(Total, 0)) DESC";

        using var conn = CreateConnection();
        var resultados = (await conn.QueryAsync<ClienteAmlResumen>(sql, new
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            BuscarCliente = string.IsNullOrWhiteSpace(filtros.BuscarCliente) ? null : filtros.BuscarCliente
        })).ToList();

        // Clasificar nivel de alerta
        foreach (var cliente in resultados)
        {
            if (cliente.TotalAcumulado >= _config.MontoAvisoSAT)
            {
                cliente.NivelAlerta = "AvisoSAT";
                cliente.RequiereIdentificacion = true;
                cliente.RequiereAvisoSAT = true;
            }
            else if (cliente.TotalAcumulado >= _config.MontoIdentificacion)
            {
                cliente.NivelAlerta = "Identificacion";
                cliente.RequiereIdentificacion = true;
                cliente.RequiereAvisoSAT = false;
            }
            else
            {
                cliente.NivelAlerta = "Normal";
                cliente.RequiereIdentificacion = false;
                cliente.RequiereAvisoSAT = false;
            }
        }

        // Filtrar por nivel de alerta si se especificó
        if (!string.IsNullOrEmpty(filtros.NivelAlerta) && filtros.NivelAlerta != "Todos")
        {
            resultados = resultados.Where(r => r.NivelAlerta == filtros.NivelAlerta).ToList();
        }

        return resultados;
    }

    /// <summary>
    /// Obtiene el detalle de notas/ventas de un cliente específico en el período
    /// </summary>
    public async Task<List<NotaDetalle>> ObtenerNotasClienteAsync(
        string nombreCliente, DateTime fechaDesde, DateTime fechaHasta, string agrupador = "NombreCliente")
    {
        if (agrupador != "NombreCliente" && agrupador != "RFC" && agrupador != "Telefonos")
            agrupador = "NombreCliente";

        var sql = $@"
            SELECT
                IdNota,
                NombreCliente,
                RFC,
                Telefonos,
                ISNULL(Total, 0) AS Total,
                FechaBaja,
                FormaPago
            FROM BAJASNOTAS
            WHERE {agrupador} = @NombreCliente
              AND FechaBaja >= @FechaDesde
              AND FechaBaja <= @FechaHasta
            ORDER BY FechaBaja DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<NotaDetalle>(sql, new
        {
            NombreCliente = nombreCliente,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        })).ToList();
    }

    /// <summary>
    /// Obtiene estadísticas generales del dashboard
    /// </summary>
    public async Task<AmlDashboardStats> ObtenerEstadisticasAsync(DateTime fechaDesde, DateTime fechaHasta)
    {
        var sql = @"
            ;WITH ClienteAcumulados AS (
                SELECT
                    NombreCliente,
                    SUM(ISNULL(Total, 0)) AS TotalAcumulado,
                    COUNT(*) AS NumOperaciones
                FROM BAJASNOTAS
                WHERE FechaBaja >= @FechaDesde
                  AND FechaBaja <= @FechaHasta
                  AND NombreCliente IS NOT NULL
                  AND LTRIM(RTRIM(NombreCliente)) <> ''
                GROUP BY NombreCliente
                HAVING SUM(ISNULL(Total, 0)) > 0
            )
            SELECT
                COUNT(*) AS TotalClientes,
                SUM(CASE WHEN TotalAcumulado >= @MontoIdentificacion THEN 1 ELSE 0 END) AS ClientesIdentificacion,
                SUM(CASE WHEN TotalAcumulado >= @MontoAvisoSAT THEN 1 ELSE 0 END) AS ClientesAvisoSAT,
                SUM(TotalAcumulado) AS MontoTotalVentas,
                SUM(NumOperaciones) AS TotalOperaciones
            FROM ClienteAcumulados";

        using var conn = CreateConnection();
        var stats = await conn.QueryFirstOrDefaultAsync<AmlDashboardStats>(sql, new
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            MontoIdentificacion = _config.MontoIdentificacion,
            MontoAvisoSAT = _config.MontoAvisoSAT
        });

        return stats ?? new AmlDashboardStats();
    }

    /// <summary>
    /// Obtiene la configuración actual de umbrales
    /// </summary>
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
