using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

public class ConsultaRapidaService
{
    private readonly string _connectionString;
    private readonly ILogger<ConsultaRapidaService> _logger;

    public ConsultaRapidaService(string connectionString, ILogger<ConsultaRapidaService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Busca una pieza por codigo de barras en las 4 fuentes:
    /// existencias, vendidas, canceladas y devoluciones.
    /// </summary>
    public async Task<ConsultaRapidaResultado> BuscarPorCodigoBarrasAsync(string codigoBarras)
    {
        var resultado = new ConsultaRapidaResultado { CodigoBarras = codigoBarras };

        using var conn = CreateConnection();

        // Ejecutar las 4 consultas en un solo round-trip usando QueryMultiple
        var sql = @"
            -- 1. Existencias (vpiezas)
            SELECT TOP 50
                   CodigoBarras, Descripcion, Grupo, Modelo, Linea,
                   Kilates, Quilates, Color, Pureza, Corte,
                   Peso, CBTotal, CNTotal, Moneda, Precio,
                   NumSerie, Obs1, Obs2, Remision, Proveedor, FechaCaptura
              FROM vpiezas
             WHERE CodigoBarras = @CodigoBarras;

            -- 2. Vendidas/Devueltas (vbajaspiezas)
            SELECT TOP 50
                   CodigoBarras, IdNota, NombreCliente, Descripcion, Modelo,
                   Linea, Peso, CBTotal, CNTotal, Precio,
                   Kilates, Quilates, Color, Pureza, FechaCaptura
              FROM vBajasPiezas
             WHERE CodigoBarras = @CodigoBarras;

            -- 3. Canceladas (piezascanceladas)
            SELECT TOP 50
                   CodigoBarras, Descripcion, Modelo, Linea, Peso,
                   CBTotal, CNTotal, Precio, Kilates, IdUsuario,
                   IdStatus, FechaCaptura
              FROM PIEZASCANCELADAS
             WHERE CodigoBarras = @CodigoBarras;

            -- 4. Devoluciones a proveedor
            SELECT TOP 50
                   CodigoBarras, MotivoDevolucion, Remision,
                   FechaDevolucion, IdUsuario
              FROM DEVOLUCIONES
             WHERE CodigoBarras = @CodigoBarras;";

        using var multi = await conn.QueryMultipleAsync(sql, new { CodigoBarras = codigoBarras });

        resultado.Existencias = (await multi.ReadAsync<PiezaExistencia>()).ToList();
        resultado.Vendidas = (await multi.ReadAsync<PiezaVendida>()).ToList();
        resultado.Canceladas = (await multi.ReadAsync<PiezaCancelada>()).ToList();
        resultado.Devoluciones = (await multi.ReadAsync<DevolucionProveedor>()).ToList();

        _logger.LogInformation(
            "Consulta rapida CB={CodigoBarras}: Existencias={Existencias}, Vendidas={Vendidas}, Canceladas={Canceladas}, Devoluciones={Devoluciones}",
            codigoBarras,
            resultado.Existencias.Count,
            resultado.Vendidas.Count,
            resultado.Canceladas.Count,
            resultado.Devoluciones.Count);

        return resultado;
    }
}
