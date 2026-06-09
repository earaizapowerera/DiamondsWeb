using System.Data;
using System.Text;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para consultar piezas vendidas/dadas de baja desde vBajasPiezas.
/// Migración de frmConsultaBajas.frm (VB6).
/// </summary>
public class BajasService
{
    private readonly string _connectionString;
    private readonly ILogger<BajasService> _logger;

    public BajasService(string connectionString, ILogger<BajasService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Busca piezas en vBajasPiezas con filtros opcionales.
    /// Replica la lógica de AutoBusqueda del VB6.
    /// </summary>
    public async Task<List<BajaPiezaItem>> BuscarPiezasAsync(
        string? buscarTexto,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        string? grupo,
        int pagina = 1,
        int tamanioPagina = 50)
    {
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(buscarTexto))
        {
            where.Append(@" AND (CodigoBarras LIKE @Buscar
                OR Descripcion LIKE @Buscar
                OR Modelo LIKE @Buscar
                OR Linea LIKE @Buscar
                OR NombreCliente LIKE @Buscar
                OR Obs2 LIKE @Buscar
                OR NumSerie LIKE @Buscar)");
            parameters.Add("Buscar", $"%{buscarTexto.Trim()}%");
        }

        if (fechaDesde.HasValue)
        {
            where.Append(" AND FechaBaja >= @FechaDesde");
            parameters.Add("FechaDesde", fechaDesde.Value);
        }

        if (fechaHasta.HasValue)
        {
            where.Append(" AND FechaBaja <= @FechaHasta");
            parameters.Add("FechaHasta", fechaHasta.Value);
        }

        if (!string.IsNullOrWhiteSpace(grupo))
        {
            where.Append(" AND Grupo = @Grupo");
            parameters.Add("Grupo", grupo.Trim());
        }

        var offset = (pagina - 1) * tamanioPagina;
        parameters.Add("Offset", offset);
        parameters.Add("TamanioPagina", tamanioPagina);

        var sql = $@"
            SELECT CodigoBarras, Descripcion, Modelo, Linea, Precio,
                   NombreCliente, FechaBaja, Obs2,
                   IdNota, Peso, PrecioGramo, Kilates, Quilates,
                   Color, Pureza, Corte, NumSerie, Obs1,
                   Grupo, Moneda, FechaCaptura
            FROM vBajasPiezas
            {where}
            ORDER BY FechaBaja DESC, CodigoBarras
            OFFSET @Offset ROWS FETCH NEXT @TamanioPagina ROWS ONLY";

        try
        {
            using var conn = CreateConnection();
            var resultado = (await conn.QueryAsync<BajaPiezaItem>(sql, parameters)).ToList();
            _logger.LogInformation("BuscarPiezas: {Count} resultados (pag {Pag})", resultado.Count, pagina);
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error buscando piezas en vBajasPiezas");
            throw;
        }
    }

    /// <summary>
    /// Obtiene conteo y suma de precios para los mismos filtros.
    /// Replica txtPiezas y txtSuma del VB6.
    /// </summary>
    public async Task<BajasStats> ObtenerStatsAsync(
        string? buscarTexto,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        string? grupo)
    {
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(buscarTexto))
        {
            where.Append(@" AND (CodigoBarras LIKE @Buscar
                OR Descripcion LIKE @Buscar
                OR Modelo LIKE @Buscar
                OR Linea LIKE @Buscar
                OR NombreCliente LIKE @Buscar
                OR Obs2 LIKE @Buscar
                OR NumSerie LIKE @Buscar)");
            parameters.Add("Buscar", $"%{buscarTexto.Trim()}%");
        }

        if (fechaDesde.HasValue)
        {
            where.Append(" AND FechaBaja >= @FechaDesde");
            parameters.Add("FechaDesde", fechaDesde.Value);
        }

        if (fechaHasta.HasValue)
        {
            where.Append(" AND FechaBaja <= @FechaHasta");
            parameters.Add("FechaHasta", fechaHasta.Value);
        }

        if (!string.IsNullOrWhiteSpace(grupo))
        {
            where.Append(" AND Grupo = @Grupo");
            parameters.Add("Grupo", grupo.Trim());
        }

        var sql = $@"
            SELECT TOP 1
                ISNULL(COUNT(*), 0) AS TotalPiezas,
                ISNULL(SUM(CAST(Precio AS DECIMAL(18,2))), 0) AS SumaPrecio
            FROM vBajasPiezas
            {where}";

        try
        {
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<BajasStats>(sql, parameters)
                   ?? new BajasStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo stats de vBajasPiezas");
            throw;
        }
    }

    /// <summary>
    /// Obtiene la lista de grupos distintos para el filtro dropdown.
    /// </summary>
    public async Task<List<string>> ObtenerGruposAsync()
    {
        const string sql = "SELECT DISTINCT TOP 50 Grupo FROM vBajasPiezas WHERE Grupo IS NOT NULL ORDER BY Grupo";
        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<string>(sql)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo grupos");
            throw;
        }
    }

    /// <summary>
    /// Cuenta el total de registros con los filtros aplicados (para paginación).
    /// </summary>
    public async Task<int> ContarPiezasAsync(
        string? buscarTexto,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        string? grupo)
    {
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(buscarTexto))
        {
            where.Append(@" AND (CodigoBarras LIKE @Buscar
                OR Descripcion LIKE @Buscar
                OR Modelo LIKE @Buscar
                OR Linea LIKE @Buscar
                OR NombreCliente LIKE @Buscar
                OR Obs2 LIKE @Buscar
                OR NumSerie LIKE @Buscar)");
            parameters.Add("Buscar", $"%{buscarTexto.Trim()}%");
        }

        if (fechaDesde.HasValue)
        {
            where.Append(" AND FechaBaja >= @FechaDesde");
            parameters.Add("FechaDesde", fechaDesde.Value);
        }

        if (fechaHasta.HasValue)
        {
            where.Append(" AND FechaBaja <= @FechaHasta");
            parameters.Add("FechaHasta", fechaHasta.Value);
        }

        if (!string.IsNullOrWhiteSpace(grupo))
        {
            where.Append(" AND Grupo = @Grupo");
            parameters.Add("Grupo", grupo.Trim());
        }

        var sql = $"SELECT TOP 1 COUNT(*) FROM vBajasPiezas {where}";
        try
        {
            using var conn = CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error contando piezas");
            throw;
        }
    }
}
