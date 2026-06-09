using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para gestión de piezas compuestas.
/// Maneja CRUD de compuestas y sus componentes (master-detail).
/// Tablas: compuestas, componentescompuestas, piezas, vpiezas, contador.
/// </summary>
public class CompuestaService
{
    private readonly string _connectionString;
    private readonly ILogger<CompuestaService> _logger;

    public CompuestaService(string connectionString, ILogger<CompuestaService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista todas las piezas compuestas con precio total calculado
    /// </summary>
    public async Task<List<CompuestaResumen>> ObtenerCompuestasAsync(string? buscar)
    {
        const string sql = @"
            SELECT TOP 50
                c.CodigoBarras,
                c.Descripcion,
                c.IdGrupo,
                ISNULL(g.Grupo, '') AS Grupo,
                c.Componentes,
                ISNULL((
                    SELECT SUM(v.Precio)
                    FROM componentescompuestas cc
                    INNER JOIN vpiezas v ON cc.CodigoBarras = v.CodigoBarras
                    WHERE cc.CBPadre = c.CodigoBarras
                ), 0) AS PrecioTotal,
                c.FechaCaptura,
                c.FechaUltEdicion
            FROM compuestas c
            LEFT JOIN grupos g ON c.IdGrupo = g.IdGrupo
            WHERE (@Buscar IS NULL
                OR c.CodigoBarras LIKE '%' + @Buscar + '%'
                OR c.Descripcion LIKE '%' + @Buscar + '%')
            ORDER BY c.FechaUltEdicion DESC";

        using var conn = CreateConnection();
        var result = await conn.QueryAsync<CompuestaResumen>(sql, new { Buscar = buscar });
        return result.ToList();
    }

    /// <summary>
    /// Obtiene el detalle de una compuesta con todos sus componentes
    /// </summary>
    public async Task<CompuestaDetalle?> ObtenerDetalleAsync(string codigoBarras)
    {
        const string sqlCompuesta = @"
            SELECT TOP 1
                c.CodigoBarras, c.Descripcion, c.IdGrupo,
                c.EtiquetaK, c.Linea1, c.Linea2, c.Linea3,
                c.Componentes, c.IdLocalizacion,
                c.FechaCaptura, c.FechaUltEdicion, c.IdUsuario
            FROM compuestas c
            WHERE c.CodigoBarras = @CB";

        const string sqlComponentes = @"
            SELECT TOP 50
                v.CodigoBarras, v.Descripcion, v.Kilates, v.Modelo,
                v.Linea, v.Quilates, v.Color, v.Pureza, v.Corte,
                v.Obs1, v.Obs2, v.Precio, v.Proveedor, v.NumSerie,
                cc.Indice
            FROM vpiezas v
            INNER JOIN componentescompuestas cc ON cc.CodigoBarras = v.CodigoBarras
            WHERE cc.CBPadre = @CB
            ORDER BY cc.Indice";

        using var conn = CreateConnection();
        var compuesta = await conn.QueryFirstOrDefaultAsync<CompuestaDetalle>(
            sqlCompuesta, new { CB = codigoBarras });

        if (compuesta == null) return null;

        var componentes = await conn.QueryAsync<ComponenteDetalle>(
            sqlComponentes, new { CB = codigoBarras });
        compuesta.ListaComponentes = componentes.ToList();

        return compuesta;
    }

    /// <summary>
    /// Busca una pieza disponible (sin padre) para agregarla como componente
    /// </summary>
    public async Task<ComponenteDetalle?> BuscarPiezaDisponibleAsync(
        string codigoBarras, string? cbPadreActual = null)
    {
        const string sql = @"
            SELECT TOP 1
                v.CodigoBarras, v.Descripcion, v.Kilates, v.Modelo,
                v.Linea, v.Quilates, v.Color, v.Pureza, v.Corte,
                v.Obs1, v.Obs2, v.Precio, v.Proveedor, v.NumSerie
            FROM vpiezas v
            INNER JOIN piezas p ON p.CodigoBarras = v.CodigoBarras
            WHERE v.CodigoBarras = @CB
                AND (p.CBPadre IS NULL OR p.CBPadre = @CBPadreActual)";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ComponenteDetalle>(
            sql, new { CB = codigoBarras, CBPadreActual = cbPadreActual });
    }

    /// <summary>
    /// Crea una nueva pieza compuesta con sus componentes.
    /// Genera código de barras automáticamente desde tabla contador.
    /// </summary>
    public async Task<string> CrearCompuestaAsync(CompuestaRequest req, int idUsuario, int idTienda)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Generar código de barras desde contador
            var cbRaw = await conn.ExecuteScalarAsync<int>(
                "SELECT CodigoBarras + 1 FROM contador; UPDATE contador SET CodigoBarras = CodigoBarras + 1",
                transaction: tx);

            var cb = cbRaw.ToString().PadLeft(6, '0');

            // Insertar compuesta
            await conn.ExecuteAsync(@"
                INSERT INTO compuestas
                    (CodigoBarras, Descripcion, IdGrupo, EtiquetaK,
                     Linea1, Linea2, Linea3, Componentes, IdUsuario, IdLocalizacion)
                VALUES
                    (@CB, @Descripcion, @IdGrupo, @EtiquetaK,
                     @Linea1, @Linea2, @Linea3, @Componentes, @IdUsuario, @IdTienda)",
                new
                {
                    CB = cb,
                    req.Descripcion,
                    req.IdGrupo,
                    req.EtiquetaK,
                    req.Linea1,
                    req.Linea2,
                    req.Linea3,
                    Componentes = req.ComponentesCB.Count,
                    IdUsuario = idUsuario,
                    IdTienda = idTienda
                }, tx);

            // Insertar componentes
            for (int i = 0; i < req.ComponentesCB.Count; i++)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO componentescompuestas (CodigoBarras, CBPadre, Indice)
                    VALUES (@CompCB, @CB, @Indice)",
                    new { CompCB = req.ComponentesCB[i], CB = cb, Indice = i + 1 }, tx);
            }

            // Actualizar piezas con referencia al padre
            if (req.ComponentesCB.Any())
            {
                await conn.ExecuteAsync(@"
                    UPDATE piezas SET FechaUltEdicion = GETUTCDATE(), CBPadre = @CB
                    WHERE CodigoBarras IN (
                        SELECT CodigoBarras FROM componentescompuestas WHERE CBPadre = @CB
                    )", new { CB = cb }, tx);
            }

            tx.Commit();
            _logger.LogInformation("Compuesta creada: {CB} con {N} componentes", cb, req.ComponentesCB.Count);
            return cb;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Actualiza una compuesta existente y sus componentes.
    /// Elimina componentes anteriores y reinserta los nuevos.
    /// </summary>
    public async Task ActualizarCompuestaAsync(CompuestaRequest req, int idUsuario)
    {
        if (string.IsNullOrEmpty(req.CodigoBarras))
            throw new ArgumentException("CodigoBarras requerido para actualizar");

        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Actualizar datos de la compuesta
            await conn.ExecuteAsync(@"
                UPDATE compuestas SET
                    Descripcion = @Descripcion,
                    IdGrupo = @IdGrupo,
                    EtiquetaK = @EtiquetaK,
                    Linea1 = @Linea1,
                    Linea2 = @Linea2,
                    Linea3 = @Linea3,
                    Componentes = @Componentes,
                    IdUsuario = @IdUsuario,
                    FechaUltEdicion = GETUTCDATE()
                WHERE CodigoBarras = @CB",
                new
                {
                    CB = req.CodigoBarras,
                    req.Descripcion,
                    req.IdGrupo,
                    req.EtiquetaK,
                    req.Linea1,
                    req.Linea2,
                    req.Linea3,
                    Componentes = req.ComponentesCB.Count,
                    IdUsuario = idUsuario
                }, tx);

            // Liberar piezas antiguas (quitar referencia padre)
            await conn.ExecuteAsync(@"
                UPDATE piezas SET CBPadre = NULL
                WHERE CBPadre = @CB",
                new { CB = req.CodigoBarras }, tx);

            // Eliminar componentes anteriores
            await conn.ExecuteAsync(@"
                DELETE componentescompuestas WHERE CBPadre = @CB",
                new { CB = req.CodigoBarras }, tx);

            // Insertar nuevos componentes
            for (int i = 0; i < req.ComponentesCB.Count; i++)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO componentescompuestas (CodigoBarras, CBPadre, Indice)
                    VALUES (@CompCB, @CB, @Indice)",
                    new { CompCB = req.ComponentesCB[i], CB = req.CodigoBarras, Indice = i + 1 }, tx);
            }

            // Reasignar padre a las piezas
            if (req.ComponentesCB.Any())
            {
                await conn.ExecuteAsync(@"
                    UPDATE piezas SET FechaUltEdicion = GETUTCDATE(), CBPadre = @CB
                    WHERE CodigoBarras IN (
                        SELECT CodigoBarras FROM componentescompuestas WHERE CBPadre = @CB
                    )", new { CB = req.CodigoBarras }, tx);
            }

            tx.Commit();
            _logger.LogInformation("Compuesta actualizada: {CB}", req.CodigoBarras);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Elimina una compuesta, libera sus componentes y registra la separación
    /// </summary>
    public async Task EliminarCompuestaAsync(string codigoBarras, int idUsuario)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Liberar piezas (quitar referencia padre)
            await conn.ExecuteAsync(@"
                UPDATE piezas SET CBPadre = NULL
                WHERE CBPadre = @CB",
                new { CB = codigoBarras }, tx);

            // Registrar separación para auditoría
            await conn.ExecuteAsync(@"
                INSERT INTO componentesseparadas (CBPadre, CodigoBarras, IdUsuario)
                SELECT CBPadre, CodigoBarras, @IdUsuario
                FROM componentescompuestas
                WHERE CBPadre = @CB",
                new { CB = codigoBarras, IdUsuario = idUsuario }, tx);

            // Eliminar componentes
            await conn.ExecuteAsync(@"
                DELETE componentescompuestas WHERE CBPadre = @CB",
                new { CB = codigoBarras }, tx);

            // Eliminar compuesta
            await conn.ExecuteAsync(@"
                DELETE compuestas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            tx.Commit();
            _logger.LogInformation("Compuesta eliminada: {CB}", codigoBarras);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Obtiene catálogo de grupos para dropdown
    /// </summary>
    public async Task<List<GrupoCatalogo>> ObtenerGruposAsync()
    {
        const string sql = "SELECT TOP 50 IdGrupo, Grupo FROM grupos ORDER BY Grupo";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<GrupoCatalogo>(sql);
        return result.ToList();
    }
}
