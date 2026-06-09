using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio de transferencias de mercancía entre tiendas.
/// Migración de frmTransferencias.frm (VB6) a web.
/// Opera directo sobre la BD central (sin replicación local/internet del legacy).
///
/// Modelo de localizaciones:
///   - Cada tienda tiene su localización "home" (IdTienda == IdLocalizacion en localizaciones_tiendas tipo O donde IdTienda=IdLocalizacion)
///   - Las localizaciones de tránsito son pares O/D (origen/destino) compartidas entre dos tiendas
///   - Al enviar: pieza pasa de home → localización de tránsito
///   - Al recibir: pieza pasa de tránsito → home de la tienda receptora
/// </summary>
public class TransferService
{
    private readonly string _connectionString;
    private readonly ILogger<TransferService> _logger;

    public TransferService(string connectionString, ILogger<TransferService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ──────────────────────────────────────────────
    // CONSULTAS
    // ──────────────────────────────────────────────

    public async Task<List<Tienda>> ObtenerTiendasAsync()
    {
        const string sql = "SELECT TOP 10 IdTienda, NombreTienda FROM tiendas WHERE IdTienda > 0 ORDER BY IdTienda";
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Tienda>(sql)).ToList();
    }

    /// <summary>
    /// Piezas individuales (sencillas + compuestas) en tránsito para una tienda.
    /// Réplica de la query del VB6 Refrescar() usando vpiezas + compuestas.
    /// </summary>
    public async Task<List<PiezaEnTransito>> ObtenerPiezasEnTransitoAsync(int idTienda)
    {
        const string sql = @"
            SELECT TOP 200
                p.CodigoBarras, p.Descripcion, p.IdLocalizacion,
                l.NombreLocalizacion,
                ISNULL(p.Precio, 0) AS Precio,
                CAST(p.Proveedor AS VARCHAR(20)) AS Proveedor,
                p.FechaUltEdicion, 'Sencilla' AS TipoPieza
            FROM piezas p
            INNER JOIN localizaciones l ON p.IdLocalizacion = l.IdLocalizacion
            INNER JOIN localizaciones_tiendas lt ON lt.IdLocalizacion = l.IdLocalizacion
            WHERE lt.IdTienda = @Tienda
              AND lt.IdTienda <> lt.IdLocalizacion
              AND p.CBPadre IS NULL
            UNION ALL
            SELECT TOP 200
                c.CodigoBarras, ISNULL(c.Descripcion, '') AS Descripcion,
                c.IdLocalizacion,
                l.NombreLocalizacion,
                0 AS Precio, '' AS Proveedor,
                c.FechaUltEdicion, 'Compuesta' AS TipoPieza
            FROM compuestas c
            INNER JOIN localizaciones l ON c.IdLocalizacion = l.IdLocalizacion
            INNER JOIN localizaciones_tiendas lt ON lt.IdLocalizacion = l.IdLocalizacion
            WHERE lt.IdTienda = @Tienda
              AND lt.IdTienda <> lt.IdLocalizacion
            ORDER BY FechaUltEdicion";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<PiezaEnTransito>(sql, new { Tienda = idTienda })).ToList();
    }

    /// <summary>
    /// Lotes de repetidas en tránsito para una tienda.
    /// </summary>
    public async Task<List<LoteEnTransito>> ObtenerRepetidasEnTransitoAsync(int idTienda)
    {
        const string sql = @"
            SELECT TOP 50
                lr.IdLote, lr.CodigoBarras, lr.Cantidad,
                ISNULL(cr.Descripcion, '') AS Descripcion,
                l.NombreLocalizacion, lr.IdTienda
            FROM lotesrepetidas lr
            INNER JOIN catalogorepetidas cr ON cr.CodigoBarras = lr.CodigoBarras
            INNER JOIN localizaciones l ON lr.IdLocalizacion = l.IdLocalizacion
            INNER JOIN localizaciones_tiendas lt ON lt.IdLocalizacion = lr.IdLocalizacion
            WHERE lt.IdTienda = @Tienda
              AND lt.IdLocalizacion <> @Tienda
              AND lr.Cantidad > 0
            ORDER BY lr.IdLote DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<LoteEnTransito>(sql, new { Tienda = idTienda })).ToList();
    }

    /// <summary>
    /// Log de transferencias recientes.
    /// </summary>
    public async Task<List<LogTransferencia>> ObtenerLogRecienteAsync(int limite = 30)
    {
        var sql = $@"
            SELECT TOP {limite}
                lt.CodigoBarras, lt.LocalizacionOrigen,
                ISNULL(lo.NombreLocalizacion, CAST(lt.LocalizacionOrigen AS VARCHAR)) AS NombreOrigen,
                lt.LocalizacionDestino,
                ISNULL(ld.NombreLocalizacion, CAST(lt.LocalizacionDestino AS VARCHAR)) AS NombreDestino,
                lt.IdUsuario, lt.FechaCaptura, lt.Cantidad
            FROM log_transferencias lt
            LEFT JOIN localizaciones lo ON lt.LocalizacionOrigen = lo.IdLocalizacion
            LEFT JOIN localizaciones ld ON lt.LocalizacionDestino = ld.IdLocalizacion
            ORDER BY lt.FechaCaptura DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<LogTransferencia>(sql)).ToList();
    }

    // ──────────────────────────────────────────────
    // OPERACIONES
    // ──────────────────────────────────────────────

    /// <summary>
    /// Encuentra la localización de tránsito entre tienda origen y destino.
    /// Busca un IdLocalizacion compartido donde origen tiene tipo 'O' y destino 'D'.
    /// </summary>
    private async Task<int?> BuscarLocalizacionTransitoAsync(IDbConnection conn, IDbTransaction? tx,
        int idTiendaOrigen, int idTiendaDestino)
    {
        const string sql = @"
            SELECT TOP 1 o.IdLocalizacion
            FROM localizaciones_tiendas o
            INNER JOIN localizaciones_tiendas d ON o.IdLocalizacion = d.IdLocalizacion
            WHERE o.IdTienda = @Origen AND o.Tipo LIKE 'O%'
              AND d.IdTienda = @Destino AND d.Tipo LIKE 'D%'";

        return await conn.QueryFirstOrDefaultAsync<int?>(sql,
            new { Origen = idTiendaOrigen, Destino = idTiendaDestino }, tx);
    }

    /// <summary>
    /// Determina si un código de barras es pieza sencilla, compuesta, o no existe.
    /// </summary>
    private async Task<string> DeterminarTipoPiezaAsync(IDbConnection conn, IDbTransaction? tx, string codigoBarras)
    {
        var countPiezas = await conn.ExecuteScalarAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM piezas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras }, tx);
        if (countPiezas > 0) return "Sencilla";

        var countCompuestas = await conn.ExecuteScalarAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM compuestas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras }, tx);
        if (countCompuestas > 0) return "Compuesta";

        return "NoExiste";
    }

    /// <summary>
    /// Enviar pieza individual (sencilla o compuesta) a otra tienda.
    /// </summary>
    public async Task<TransferResult> EnviarPiezaAsync(string codigoBarras, int idTiendaOrigen,
        int idTiendaDestino, int idUsuario)
    {
        if (idTiendaOrigen == idTiendaDestino)
            return TransferResult.Error("No se puede enviar a la misma tienda.");

        using var conn = CreateConnection();
        conn.Open();

        var tipoPieza = await DeterminarTipoPiezaAsync(conn, null, codigoBarras);
        if (tipoPieza == "NoExiste")
            return TransferResult.Error($"No existe la pieza '{codigoBarras}'.");

        // Verificar que existe la tienda destino
        var tiendaExiste = await conn.ExecuteScalarAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM tiendas WHERE IdTienda = @T", new { T = idTiendaDestino });
        if (tiendaExiste == 0)
            return TransferResult.Error("No existe la tienda destino.");

        var locTransito = await BuscarLocalizacionTransitoAsync(conn, null, idTiendaOrigen, idTiendaDestino);
        if (locTransito == null)
            return TransferResult.Error("No se encontró la localización de tránsito entre esas tiendas.");

        try
        {
            using var tx = conn.BeginTransaction();

            if (tipoPieza == "Sencilla")
            {
                await EnviarSencillaAsync(conn, tx, codigoBarras, locTransito.Value, idTiendaOrigen, idUsuario);
            }
            else
            {
                await EnviarCompuestaAsync(conn, tx, codigoBarras, locTransito.Value, idTiendaOrigen, idUsuario);
            }

            tx.Commit();
            _logger.LogInformation("Pieza {CB} ({Tipo}) enviada de tienda {Origen} a tránsito loc {Loc}",
                codigoBarras, tipoPieza, idTiendaOrigen, locTransito.Value);

            return TransferResult.Ok($"Pieza {tipoPieza.ToLower()} '{codigoBarras}' enviada correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar pieza {CB}", codigoBarras);
            return TransferResult.Error($"Error al enviar: {ex.Message}");
        }
    }

    private async Task EnviarSencillaAsync(IDbConnection conn, IDbTransaction tx,
        string codigoBarras, int locDestino, int idTiendaOrigen, int idUsuario)
    {
        // Actualizar localización en piezas y etiquetas
        await conn.ExecuteAsync(@"
            UPDATE piezas SET IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB;
            UPDATE etiquetas SET IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB;",
            new { Loc = locDestino, CB = codigoBarras }, tx);

        // Log de transferencia
        await conn.ExecuteAsync(@"
            INSERT INTO log_transferencias (CodigoBarras, LocalizacionOrigen, LocalizacionDestino, IdUsuario, FechaCaptura, Cantidad)
            VALUES (@CB, @Origen, @Destino, @Usuario, GETUTCDATE(), 1)",
            new { CB = codigoBarras, Origen = idTiendaOrigen, Destino = locDestino, Usuario = idUsuario }, tx);
    }

    private async Task EnviarCompuestaAsync(IDbConnection conn, IDbTransaction tx,
        string codigoBarras, int locDestino, int idTiendaOrigen, int idUsuario)
    {
        // Actualizar la pieza compuesta padre
        await conn.ExecuteAsync(@"
            UPDATE compuestas SET IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB",
            new { Loc = locDestino, CB = codigoBarras }, tx);

        // Log de la compuesta
        await conn.ExecuteAsync(@"
            INSERT INTO log_transferencias (CodigoBarras, LocalizacionOrigen, LocalizacionDestino, IdUsuario, FechaCaptura, Cantidad)
            VALUES (@CB, @Origen, @Destino, @Usuario, GETUTCDATE(), 1)",
            new { CB = codigoBarras, Origen = idTiendaOrigen, Destino = locDestino, Usuario = idUsuario }, tx);

        // Enviar cada componente recursivamente
        var componentes = (await conn.QueryAsync<string>(
            "SELECT TOP 50 CodigoBarras FROM componentescompuestas WHERE CBPadre = @CB",
            new { CB = codigoBarras }, tx)).ToList();

        foreach (var comp in componentes)
        {
            await conn.ExecuteAsync(@"
                UPDATE piezas SET CBPadre = @Padre, IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB;
                UPDATE etiquetas SET IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB;",
                new { Padre = codigoBarras, Loc = locDestino, CB = comp }, tx);
        }
    }

    /// <summary>
    /// Recibir pieza individual (sencilla o compuesta) en la tienda.
    /// Al recibir, la pieza se mueve del tránsito a la localización "home" de la tienda receptora.
    /// La localización home es aquella donde IdTienda = IdLocalizacion en localizaciones_tiendas.
    /// En este sistema: tienda 1→loc 1 (Santa Fe), tienda 2→loc 2 (Pabellón), tienda 3→loc 3 (Molière).
    /// </summary>
    public async Task<TransferResult> RecibirPiezaAsync(string codigoBarras, int idTiendaReceptora, int idUsuario)
    {
        using var conn = CreateConnection();
        conn.Open();

        var tipoPieza = await DeterminarTipoPiezaAsync(conn, null, codigoBarras);
        if (tipoPieza == "NoExiste")
            return TransferResult.Error($"La pieza '{codigoBarras}' no existe.");

        // Verificar que la pieza está en tránsito hacia esta tienda
        var enTransito = await VerificarEnTransitoAsync(conn, codigoBarras, idTiendaReceptora, tipoPieza);
        if (!enTransito)
            return TransferResult.Error($"La pieza '{codigoBarras}' no está en tránsito hacia esta tienda.");

        // La localización "home" es el IdTienda mismo (coincide con IdLocalizacion para tiendas 1,2,3)
        var locHome = idTiendaReceptora;

        try
        {
            using var tx = conn.BeginTransaction();

            if (tipoPieza == "Sencilla")
            {
                await conn.ExecuteAsync(@"
                    UPDATE piezas SET IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB;
                    UPDATE etiquetas SET IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB;",
                    new { Loc = locHome, CB = codigoBarras }, tx);
            }
            else
            {
                // Recibir compuesta + componentes
                await conn.ExecuteAsync(@"
                    UPDATE compuestas SET IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB",
                    new { Loc = locHome, CB = codigoBarras }, tx);

                var componentes = (await conn.QueryAsync<string>(
                    "SELECT TOP 50 CodigoBarras FROM componentescompuestas WHERE CBPadre = @CB",
                    new { CB = codigoBarras }, tx)).ToList();

                foreach (var comp in componentes)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE piezas SET IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB;
                        UPDATE etiquetas SET IdLocalizacion = @Loc, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB;",
                        new { Loc = locHome, CB = comp }, tx);
                }
            }

            tx.Commit();
            _logger.LogInformation("Pieza {CB} ({Tipo}) recibida en tienda {Tienda}",
                codigoBarras, tipoPieza, idTiendaReceptora);
            return TransferResult.Ok($"Pieza {tipoPieza.ToLower()} '{codigoBarras}' recibida correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al recibir pieza {CB}", codigoBarras);
            return TransferResult.Error($"Error al recibir: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica si la pieza está en una localización de tránsito con destino a esta tienda.
    /// Una pieza está en tránsito hacia tienda X si su IdLocalizacion coincide con un registro
    /// en localizaciones_tiendas donde IdTienda=X y Tipo='D'.
    /// </summary>
    private async Task<bool> VerificarEnTransitoAsync(IDbConnection conn, string codigoBarras,
        int idTiendaReceptora, string tipoPieza)
    {
        string tabla = tipoPieza == "Sencilla" ? "piezas" : "compuestas";
        string sql = $@"
            SELECT TOP 1 COUNT(*) FROM {tabla} t
            INNER JOIN localizaciones_tiendas lt ON t.IdLocalizacion = lt.IdLocalizacion
            WHERE t.CodigoBarras = @CB AND lt.IdTienda = @Tienda AND lt.Tipo LIKE 'D%'";

        var count = await conn.ExecuteScalarAsync<int>(sql, new { CB = codigoBarras, Tienda = idTiendaReceptora });
        return count > 0;
    }

    /// <summary>
    /// Enviar piezas repetidas por cantidad a otra tienda.
    /// Crea un nuevo lote en tránsito y ejecuta sp_transferirrepetidas para decrementar del origen (PEPS).
    /// </summary>
    public async Task<TransferResult> EnviarRepetidasAsync(
        string codigoBarras, int cantidad, int idTiendaOrigen, int idTiendaDestino, int idUsuario)
    {
        if (idTiendaOrigen == idTiendaDestino)
            return TransferResult.Error("No se puede enviar a la misma tienda.");

        if (cantidad <= 0)
            return TransferResult.Error("La cantidad debe ser mayor a 0.");

        using var conn = CreateConnection();
        conn.Open();

        // Verificar catálogo
        var existeCatalogo = await conn.ExecuteScalarAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM catalogorepetidas WHERE CodigoBarras = @CB", new { CB = codigoBarras });
        if (existeCatalogo == 0)
            return TransferResult.Error($"No existe la pieza repetida '{codigoBarras}' en el catálogo.");

        // Verificar stock disponible en la tienda origen
        var stockDisponible = await conn.ExecuteScalarAsync<int?>(
            "SELECT TOP 1 ISNULL(SUM(Cantidad), 0) FROM lotesrepetidas WHERE CodigoBarras = @CB AND IdLocalizacion = @Loc",
            new { CB = codigoBarras, Loc = idTiendaOrigen });
        if ((stockDisponible ?? 0) < cantidad)
            return TransferResult.Error($"Stock insuficiente. Disponible: {stockDisponible ?? 0}, solicitado: {cantidad}.");

        var locTransito = await BuscarLocalizacionTransitoAsync(conn, null, idTiendaOrigen, idTiendaDestino);
        if (locTransito == null)
            return TransferResult.Error("No se encontró la localización de tránsito.");

        try
        {
            using var tx = conn.BeginTransaction();

            // Obtener nuevo IdLote del contador (formato VB6: string concat IdTienda + lote)
            var nuevoLote = await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 lote + 1 FROM contador", transaction: tx);
            await conn.ExecuteAsync("UPDATE contador SET lote = lote + 1", transaction: tx);

            // VB6 usa: IdLoteMandar = CStr(IdTienda) + CStr(Rs(0)) → concatenación de strings
            // Ej: tienda=1, lote=57 → "157", tienda=2, lote=57 → "257"
            var idLoteStr = $"{idTiendaOrigen}{nuevoLote}";
            if (!int.TryParse(idLoteStr, out var idLote))
                idLote = idTiendaOrigen * 100000 + nuevoLote; // fallback seguro

            // Crear lote en tránsito
            await conn.ExecuteAsync(@"
                INSERT INTO lotesrepetidas (IdLote, CodigoBarras, Cantidad, IdMoneda, IdUsuario, IdTienda, IdLocalizacion)
                VALUES (@IdLote, @CB, @Cantidad, 1, @Usuario, @Tienda, @Loc)",
                new { IdLote = idLote, CB = codigoBarras, Cantidad = cantidad,
                    Usuario = idUsuario, Tienda = idTiendaOrigen, Loc = locTransito.Value }, tx);

            // Log de transferencia
            await conn.ExecuteAsync(@"
                INSERT INTO log_transferencias (CodigoBarras, LocalizacionOrigen, LocalizacionDestino, IdUsuario, FechaCaptura, Cantidad)
                VALUES (@CB, @Origen, @Destino, @Usuario, GETUTCDATE(), @Cantidad)",
                new { CB = codigoBarras, Origen = idTiendaOrigen, Destino = locTransito.Value,
                    Usuario = idUsuario, Cantidad = cantidad }, tx);

            // Ejecutar SP para decrementar stock del origen (PEPS)
            await conn.ExecuteAsync("EXEC sp_transferirrepetidas @CB, @Cantidad",
                new { CB = codigoBarras, Cantidad = cantidad }, tx);

            tx.Commit();
            _logger.LogInformation("Repetidas {CB} x{Cant} enviadas de tienda {O} a tránsito, lote {Lote}",
                codigoBarras, cantidad, idTiendaOrigen, idLote);

            return TransferResult.Ok($"Lote de {cantidad} piezas '{codigoBarras}' enviado. IdLote: {idLote}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar repetidas {CB} x{Cant}", codigoBarras, cantidad);
            return TransferResult.Error($"Error al enviar repetidas: {ex.Message}");
        }
    }

    /// <summary>
    /// Recibir un lote de piezas repetidas en la tienda.
    /// VB6 requiere que se reciba la cantidad exacta del lote (no parcial).
    /// </summary>
    public async Task<TransferResult> RecibirRepetidasAsync(int idLote, int cantidadConfirmada,
        int idTiendaReceptora, int idUsuario)
    {
        try
        {
            using var conn = CreateConnection();
            conn.Open();

            var lote = await conn.QueryFirstOrDefaultAsync<LoteEnTransito>(
                "SELECT TOP 1 IdLote, CodigoBarras, Cantidad, IdTienda FROM lotesrepetidas WHERE IdLote = @Lote",
                new { Lote = idLote });
            if (lote == null)
                return TransferResult.Error("El lote no existe.");

            if (lote.Cantidad != cantidadConfirmada)
                return TransferResult.Error(
                    $"No se puede recibir una cantidad distinta a la mandada ({lote.Cantidad}). " +
                    "Si se mandó mal, el lote se debe recibir en la tienda origen y mandarse nuevamente.");

            // Actualizar localización del lote a la tienda receptora
            await conn.ExecuteAsync(
                "UPDATE lotesrepetidas SET IdLocalizacion = @Loc WHERE IdLote = @Lote",
                new { Loc = idTiendaReceptora, Lote = idLote });

            _logger.LogInformation("Lote {Lote} ({CB} x{Cant}) recibido en tienda {Tienda}",
                idLote, lote.CodigoBarras, lote.Cantidad, idTiendaReceptora);

            return TransferResult.Ok($"Lote {idLote} recibido ({lote.Cantidad} piezas de '{lote.CodigoBarras}').");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al recibir lote {Lote}", idLote);
            return TransferResult.Error($"Error al recibir: {ex.Message}");
        }
    }
}
