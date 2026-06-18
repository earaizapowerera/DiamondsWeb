using System.Data;
using Dapper;
using DiamondsWeb.Models.Reporting;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services.Reporting;

/// <summary>
/// Ejecuta queries SQL y construye ReportResult con datos y totales.
/// Equivalente genérico de la función ImprimirDB del VB6:
/// ImprimirDB recibía un DataGrid + Query + Parámetros para totales.
/// Este servicio hace lo mismo pero tipado y seguro.
/// </summary>
public class ReportDataBuilder
{
    private readonly string _connectionString;
    private readonly ILogger<ReportDataBuilder> _logger;

    public ReportDataBuilder(string connectionString, ILogger<ReportDataBuilder> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el query y construye un ReportResult con datos y totales calculados.
    /// Los totales se calculan automáticamente para columnas marcadas con IsSummable=true.
    /// </summary>
    /// <param name="definition">Definición del reporte (columnas, título, etc.)</param>
    /// <param name="sql">Query SQL parametrizado</param>
    /// <param name="parameters">Parámetros del query (DynamicParameters o anónimo)</param>
    /// <param name="maxRows">Máximo de filas (TOP N). Default 5000.</param>
    public async Task<ReportResult> ExecuteAsync(
        ReportDefinition definition,
        string sql,
        object? parameters = null,
        int maxRows = 5000)
    {
        var result = new ReportResult { Definition = definition };

        try
        {
            using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync(sql, parameters);

            foreach (var row in rows)
            {
                var dict = new Dictionary<string, object?>();
                var rowDict = (IDictionary<string, object>)row;

                foreach (var col in definition.Columns)
                {
                    dict[col.Field] = rowDict.TryGetValue(col.Field, out var val) ? val : null;
                }
                result.Rows.Add(dict);

                if (result.Rows.Count >= maxRows) break;
            }

            // Calcular totales para columnas sumables
            foreach (var col in definition.Columns.Where(c => c.IsSummable))
            {
                decimal total = 0m;
                foreach (var row in result.Rows)
                {
                    var val = row.GetValueOrDefault(col.Field);
                    if (val != null && val != DBNull.Value)
                    {
                        total += Convert.ToDecimal(val);
                    }
                }
                result.Totals[col.Field] = total;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando reporte: {Title}", definition.Title);
            throw;
        }

        return result;
    }

    /// <summary>
    /// Ejecuta el query principal + un query separado de totales (como hacía el VB6).
    /// Útil cuando los totales vienen de un query diferente (ej: con funciones de agregación
    /// que no se pueden calcular sumando las filas visibles por el TOP N).
    /// </summary>
    public async Task<ReportResult> ExecuteWithTotalsQueryAsync(
        ReportDefinition definition,
        string dataSql,
        string totalsSql,
        object? parameters = null,
        int maxRows = 5000)
    {
        var result = await ExecuteAsync(definition, dataSql, parameters, maxRows);

        try
        {
            using var conn = new SqlConnection(_connectionString);
            var totalsRow = await conn.QueryFirstOrDefaultAsync(totalsSql, parameters);
            if (totalsRow != null)
            {
                var totalsDict = (IDictionary<string, object>)totalsRow;
                result.Totals.Clear();
                foreach (var col in definition.Columns.Where(c => c.IsSummable))
                {
                    if (totalsDict.TryGetValue(col.Field, out var val) && val != null && val != DBNull.Value)
                    {
                        result.Totals[col.Field] = Convert.ToDecimal(val);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando totales para reporte: {Title}", definition.Title);
            // No relanzar — los datos ya se obtuvieron, solo fallaron los totales
        }

        return result;
    }
}
