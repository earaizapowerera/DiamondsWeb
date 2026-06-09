using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

public class InventoryService
{
    private readonly string _connectionString;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(string connectionString, ILogger<InventoryService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ══════════════════════════════════════════════
    // PIEZAS (consulta general)
    // ══════════════════════════════════════════════
    public async Task<Pieza?> ObtenerPiezaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Pieza>(@"
            SELECT p.CodigoBarras, p.Descripcion, p.Precio, p.Proveedor, prov.NombreProveedor,
                   p.IdGrupo, g.Grupo, p.Kilates, p.Modelo, p.Linea, p.Quilates, p.Color, p.Pureza, p.Corte,
                   p.NumSerie, p.Obs1, p.Obs2, p.Peso, p.PrecioGramo,
                   p.CBPieza, p.CNPieza, p.DescPieza, p.CBPeso, p.CNPeso, p.DescPeso,
                   p.CBManoObra, p.CNManoObra, p.DescManoObra, p.CBFactura, p.CNFactura, p.DescFactura,
                   p.Utilidad, p.UtilidadExtra, p.Impuesto, p.IdDivisor, p.IdMoneda,
                   p.TCCosto, p.TCCotizacion, p.IdRemision, p.IdFactura,
                   p.IdLocalizacion, p.IdTienda, p.IdUsuario, p.IdStatus,
                   s.NombreStatus AS StatusNombre, p.CBPadre, p.Faltante,
                   p.FechaCaptura, p.FechaUltEdicion
            FROM Piezas p
            LEFT JOIN Proveedores prov ON p.Proveedor = prov.Proveedor
            LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
            LEFT JOIN StatusPiezas s ON p.IdStatus = s.IdStatus
            WHERE p.CodigoBarras = @CB", new { CB = codigoBarras });
    }

    public async Task<string> GenerarCodigoBarrasAsync()
    {
        using var conn = CreateConnection();
        var cb = await conn.ExecuteScalarAsync<int>("SELECT ISNULL(codigobarras,0)+1 FROM contador");
        await conn.ExecuteAsync("UPDATE contador SET codigobarras = codigobarras + 1");
        return cb.ToString("D6");
    }

    // ══════════════════════════════════════════════
    // PIEZAS SENCILLAS (CRUD completo)
    // ══════════════════════════════════════════════
    public async Task<List<Pieza>> ObtenerPiezasSencillasAsync(string? buscar = null, int? idGrupo = null, int? proveedor = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 200 p.CodigoBarras, p.Descripcion, p.Precio, p.Proveedor, prov.NombreProveedor,
                       p.IdGrupo, g.Grupo, p.Kilates, p.Modelo, p.Linea, p.Quilates, p.Color, p.Pureza, p.Corte,
                       p.NumSerie, p.Peso, p.Utilidad, p.UtilidadExtra, p.Impuesto, p.IdDivisor, p.IdMoneda,
                       p.IdStatus, p.FechaCaptura
                    FROM Piezas p
                    LEFT JOIN Proveedores prov ON p.Proveedor = prov.Proveedor
                    LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
                    WHERE p.CBPadre IS NULL";
        if (!string.IsNullOrWhiteSpace(buscar))
            sql += " AND (p.Descripcion LIKE @B OR p.CodigoBarras LIKE @B OR p.Modelo LIKE @B OR p.NumSerie LIKE @B)";
        if (idGrupo.HasValue)
            sql += " AND p.IdGrupo = @IdGrupo";
        if (proveedor.HasValue)
            sql += " AND p.Proveedor = @Proveedor";
        sql += " ORDER BY p.FechaCaptura DESC";
        return (await conn.QueryAsync<Pieza>(sql, new { B = $"%{buscar}%", IdGrupo = idGrupo, Proveedor = proveedor })).ToList();
    }

    public async Task CrearPiezaSencillaAsync(Pieza p)
    {
        p.CodigoBarras = await GenerarCodigoBarrasAsync();
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO Piezas (CodigoBarras, Descripcion, Precio, Proveedor, IdGrupo, Kilates, Modelo, Linea,
                Quilates, Color, Pureza, Corte, NumSerie, Obs1, Obs2, Peso, PrecioGramo,
                CBPieza, CNPieza, DescPieza, CBPeso, CNPeso, DescPeso,
                CBManoObra, CNManoObra, DescManoObra, CBFactura, CNFactura, DescFactura,
                Utilidad, UtilidadExtra, Impuesto, IdDivisor, IdMoneda, TCCosto, TCCotizacion,
                IdLocalizacion, IdTienda, IdUsuario, IdStatus, FechaCaptura, FechaUltEdicion)
            VALUES (@CodigoBarras, @Descripcion, @Precio, @Proveedor, @IdGrupo, @Kilates, @Modelo, @Linea,
                @Quilates, @Color, @Pureza, @Corte, @NumSerie, @Obs1, @Obs2, @Peso, @PrecioGramo,
                @CBPieza, @CNPieza, @DescPieza, @CBPeso, @CNPeso, @DescPeso,
                @CBManoObra, @CNManoObra, @DescManoObra, @CBFactura, @CNFactura, @DescFactura,
                @Utilidad, @UtilidadExtra, @Impuesto, @IdDivisor, @IdMoneda, @TCCosto, @TCCotizacion,
                @IdLocalizacion, @IdTienda, @IdUsuario, @IdStatus, GETDATE(), GETDATE())", p);
    }

    public async Task ActualizarPiezaSencillaAsync(Pieza p)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Piezas SET Descripcion=@Descripcion, Precio=@Precio, Proveedor=@Proveedor, IdGrupo=@IdGrupo,
                Kilates=@Kilates, Modelo=@Modelo, Linea=@Linea, Quilates=@Quilates, Color=@Color, Pureza=@Pureza,
                Corte=@Corte, NumSerie=@NumSerie, Obs1=@Obs1, Obs2=@Obs2, Peso=@Peso, PrecioGramo=@PrecioGramo,
                CBPieza=@CBPieza, CNPieza=@CNPieza, DescPieza=@DescPieza, CBPeso=@CBPeso, CNPeso=@CNPeso, DescPeso=@DescPeso,
                CBManoObra=@CBManoObra, CNManoObra=@CNManoObra, DescManoObra=@DescManoObra,
                CBFactura=@CBFactura, CNFactura=@CNFactura, DescFactura=@DescFactura,
                Utilidad=@Utilidad, UtilidadExtra=@UtilidadExtra, Impuesto=@Impuesto, IdDivisor=@IdDivisor,
                IdMoneda=@IdMoneda, TCCosto=@TCCosto, TCCotizacion=@TCCotizacion,
                IdUsuario=@IdUsuario, FechaUltEdicion=GETDATE()
            WHERE CodigoBarras=@CodigoBarras", p);
    }

    // ══════════════════════════════════════════════
    // PIEZAS COMPUESTAS
    // ══════════════════════════════════════════════
    public async Task<List<PiezaCompuesta>> ObtenerPiezasCompuestasAsync(string? buscar = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT c.CodigoBarras, c.Descripcion, c.IdGrupo, c.EtiquetaK,
                       c.Linea1, c.Linea2, c.Linea3, c.Componentes,
                       (SELECT SUM(p.Precio) FROM Piezas p INNER JOIN ComponentesCompuestas cc ON p.CodigoBarras=cc.CodigoBarras WHERE cc.CBPadre=c.CodigoBarras) AS PrecioTotal,
                       c.IdUsuario, c.FechaCaptura
                    FROM Compuestas c WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(buscar))
            sql += " AND (c.Descripcion LIKE @B OR c.CodigoBarras LIKE @B)";
        sql += " ORDER BY c.FechaCaptura DESC";
        return (await conn.QueryAsync<PiezaCompuesta>(sql, new { B = $"%{buscar}%" })).ToList();
    }

    public async Task<List<ComponenteCompuesta>> ObtenerComponentesAsync(string cbPadre)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<ComponenteCompuesta>(@"
            SELECT cc.CodigoBarras, cc.CBPadre, cc.Indice, p.Descripcion, p.Precio,
                   p.Kilates, p.Modelo, p.Linea, p.Quilates, p.Color, p.Pureza, p.Corte,
                   p.Obs1, p.Obs2, prov.NombreProveedor, p.NumSerie
            FROM ComponentesCompuestas cc
            INNER JOIN Piezas p ON cc.CodigoBarras = p.CodigoBarras
            LEFT JOIN Proveedores prov ON p.Proveedor = prov.Proveedor
            WHERE cc.CBPadre = @CB ORDER BY cc.Indice", new { CB = cbPadre })).ToList();
    }

    public async Task<string> CrearPiezaCompuestaAsync(PiezaCompuesta pc)
    {
        pc.CodigoBarras = await GenerarCodigoBarrasAsync();
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO Compuestas (CodigoBarras, Descripcion, IdGrupo, EtiquetaK, Linea1, Linea2, Linea3, Componentes, IdUsuario, IdLocalizacion)
            VALUES (@CodigoBarras, @Descripcion, @IdGrupo, @EtiquetaK, @Linea1, @Linea2, @Linea3, @Componentes, @IdUsuario, 1)",
            pc);
        return pc.CodigoBarras;
    }

    public async Task AgregarComponenteAsync(string cbPadre, string cbComponente, int indice)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO ComponentesCompuestas (CodigoBarras, CBPadre, Indice) VALUES (@CB, @Padre, @Idx)",
            new { CB = cbComponente, Padre = cbPadre, Idx = indice });
        await conn.ExecuteAsync(
            "UPDATE Piezas SET CBPadre = @Padre, FechaUltEdicion = GETDATE() WHERE CodigoBarras = @CB",
            new { CB = cbComponente, Padre = cbPadre });
    }

    public async Task RemoverComponenteAsync(string cbPadre, string cbComponente)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM ComponentesCompuestas WHERE CBPadre = @Padre AND CodigoBarras = @CB",
            new { Padre = cbPadre, CB = cbComponente });
        await conn.ExecuteAsync(
            "UPDATE Piezas SET CBPadre = NULL, FechaUltEdicion = GETDATE() WHERE CodigoBarras = @CB",
            new { CB = cbComponente });
    }

    // ══════════════════════════════════════════════
    // INVENTARIO FÍSICO
    // ══════════════════════════════════════════════
    public async Task<List<RegistroInventarioFisico>> ObtenerRegistrosInventarioAsync(bool soloHoy = false)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT i.CodigoBarras, p.Descripcion, p.Precio, i.IdUsuario, i.FechaCaptura
                    FROM InventarioFisico i
                    LEFT JOIN Piezas p ON i.CodigoBarras = p.CodigoBarras";
        if (soloHoy) sql += " WHERE CAST(i.FechaCaptura AS DATE) = CAST(GETDATE() AS DATE)";
        sql += " ORDER BY i.FechaCaptura DESC";
        return (await conn.QueryAsync<RegistroInventarioFisico>(sql)).ToList();
    }

    public async Task<string> RegistrarInventarioFisicoAsync(string codigoBarras, int idUsuario)
    {
        using var conn = CreateConnection();
        // Verificar si existe la pieza
        var existe = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Piezas WHERE CodigoBarras = @CB", new { CB = codigoBarras });

        if (existe == 0)
            return "Pieza no encontrada en el sistema";

        // Verificar si es compuesta y registrar componentes
        var componentes = await conn.QueryAsync<string>(
            "SELECT CodigoBarras FROM ComponentesCompuestas WHERE CBPadre = @CB", new { CB = codigoBarras });

        foreach (var comp in componentes)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO InventarioFisico (CodigoBarras, IdUsuario, FechaCaptura)
                VALUES (@CB, @IdUsuario, GETDATE())", new { CB = comp, IdUsuario = idUsuario });
            await conn.ExecuteAsync("UPDATE Piezas SET Faltante = 0 WHERE CodigoBarras = @CB", new { CB = comp });
        }

        await conn.ExecuteAsync(@"
            INSERT INTO InventarioFisico (CodigoBarras, IdUsuario, FechaCaptura)
            VALUES (@CB, @IdUsuario, GETDATE())", new { CB = codigoBarras, IdUsuario = idUsuario });
        await conn.ExecuteAsync("UPDATE Piezas SET Faltante = 0 WHERE CodigoBarras = @CB", new { CB = codigoBarras });

        var numComp = componentes.Count();
        return numComp > 0 ? $"Registrada pieza compuesta con {numComp} componentes" : "Registrada";
    }

    public async Task EliminarRegistroInventarioAsync(string codigoBarras, DateTime fechaCaptura)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            DELETE FROM InventarioFisico WHERE CodigoBarras = @CB AND FechaCaptura = @Fecha",
            new { CB = codigoBarras, Fecha = fechaCaptura });
    }

    // ══════════════════════════════════════════════
    // REPORTE FALTANTES
    // ══════════════════════════════════════════════
    public async Task<List<PiezaFaltante>> ObtenerFaltantesAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<PiezaFaltante>(@"
            SELECT p.CodigoBarras, p.Descripcion, p.Precio, prov.NombreProveedor, g.Grupo,
                   cf.Comentario
            FROM Piezas p
            LEFT JOIN Proveedores prov ON p.Proveedor = prov.Proveedor
            LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
            LEFT JOIN ComentariosFaltantes cf ON p.CodigoBarras = cf.CodigoBarras
            WHERE p.Faltante = 1
            ORDER BY p.CodigoBarras")).ToList();
    }

    public async Task GuardarComentarioFaltanteAsync(string codigoBarras, string comentario)
    {
        using var conn = CreateConnection();
        var existe = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ComentariosFaltantes WHERE CodigoBarras = @CB", new { CB = codigoBarras });
        if (existe > 0)
            await conn.ExecuteAsync("UPDATE ComentariosFaltantes SET Comentario = @C WHERE CodigoBarras = @CB",
                new { CB = codigoBarras, C = comentario });
        else
            await conn.ExecuteAsync("INSERT INTO ComentariosFaltantes (CodigoBarras, Comentario) VALUES (@CB, @C)",
                new { CB = codigoBarras, C = comentario });
    }

    // ══════════════════════════════════════════════
    // TRANSFERENCIAS
    // ══════════════════════════════════════════════
    public async Task<List<Tienda>> ObtenerTiendasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Tienda>("SELECT IdTienda, NombreTienda FROM Tiendas ORDER BY NombreTienda")).ToList();
    }

    public async Task<List<Transferencia>> ObtenerTransferenciasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Transferencia>(@"
            SELECT lt.CodigoBarras, p.Descripcion, lt.TiendaOrigen, t1.NombreTienda AS NombreTiendaOrigen,
                   lt.TiendaDestino, t2.NombreTienda AS NombreTiendaDestino, lt.IdUsuario, lt.FechaTransferencia
            FROM log_transferencias lt
            LEFT JOIN Piezas p ON lt.CodigoBarras = p.CodigoBarras
            LEFT JOIN Tiendas t1 ON lt.TiendaOrigen = t1.IdTienda
            LEFT JOIN Tiendas t2 ON lt.TiendaDestino = t2.IdTienda
            ORDER BY lt.FechaTransferencia DESC")).ToList();
    }

    public async Task<string> TransferirPiezaAsync(string codigoBarras, int tiendaDestino, int idUsuario)
    {
        using var conn = CreateConnection();
        var pieza = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT CodigoBarras, IdTienda, IdLocalizacion FROM Piezas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });
        if (pieza == null) return "Pieza no encontrada";

        int tiendaOrigen = pieza.IdTienda ?? 0;

        // Buscar localización destino
        var locDest = await conn.ExecuteScalarAsync<int?>(@"
            SELECT IdLocalizacion FROM localizaciones_tiendas
            WHERE IdTienda = @Destino AND Tipo = 'D' LIMIT 1",
            new { Destino = tiendaDestino });

        await conn.ExecuteAsync(@"
            UPDATE Piezas SET IdTienda = @Destino, FechaUltEdicion = GETDATE() WHERE CodigoBarras = @CB",
            new { CB = codigoBarras, Destino = tiendaDestino });

        await conn.ExecuteAsync(@"
            INSERT INTO log_transferencias (CodigoBarras, TiendaOrigen, TiendaDestino, IdUsuario, FechaTransferencia)
            VALUES (@CB, @Origen, @Destino, @IdUsuario, GETDATE())",
            new { CB = codigoBarras, Origen = tiendaOrigen, Destino = tiendaDestino, IdUsuario = idUsuario });

        return "Transferencia completada";
    }

    // ══════════════════════════════════════════════
    // REGISTRO DE EXISTENCIAS
    // ══════════════════════════════════════════════
    public async Task<List<RegistroInventarioFisico>> ObtenerRegistroExistenciasAsync(bool soloHoy = false)
    {
        return await ObtenerRegistrosInventarioAsync(soloHoy);
    }

    // ══════════════════════════════════════════════
    // CAMBIO DE STATUS
    // ══════════════════════════════════════════════
    public async Task<List<StatusPieza>> ObtenerStatusPiezasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<StatusPieza>(
            "SELECT IdStatus, NombreStatus FROM StatusPiezas ORDER BY NombreStatus")).ToList();
    }

    public async Task<string> CambiarStatusPiezaAsync(string codigoBarras, int nuevoStatus, int idUsuario)
    {
        using var conn = CreateConnection();
        var pieza = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT IdStatus FROM Piezas WHERE CodigoBarras = @CB", new { CB = codigoBarras });
        if (pieza == null) return "Pieza no encontrada";

        int statusAnterior = pieza.IdStatus ?? 0;

        await conn.ExecuteAsync("UPDATE Piezas SET IdStatus = @S WHERE CodigoBarras = @CB",
            new { CB = codigoBarras, S = nuevoStatus });

        var idCambio = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO BitacoraStatus (CodigoBarras, IdStatusAnterior, IdStatusNuevo, IdUsuario, FechaCambio)
            OUTPUT INSERTED.IdCambioStatus
            VALUES (@CB, @Ant, @Nuevo, @IdUsuario, GETDATE())",
            new { CB = codigoBarras, Ant = statusAnterior, Nuevo = nuevoStatus, IdUsuario = idUsuario });

        return $"Status cambiado. Registro #{idCambio}";
    }

    public async Task<List<BitacoraStatus>> ObtenerBitacoraStatusAsync(string? codigoBarras = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT b.IdCambioStatus, b.CodigoBarras, b.IdStatusAnterior, b.IdStatusNuevo,
                       sa.NombreStatus AS StatusAnterior, sn.NombreStatus AS StatusNuevo,
                       b.IdUsuario, b.FechaCambio
                    FROM BitacoraStatus b
                    LEFT JOIN StatusPiezas sa ON b.IdStatusAnterior = sa.IdStatus
                    LEFT JOIN StatusPiezas sn ON b.IdStatusNuevo = sn.IdStatus";
        if (!string.IsNullOrWhiteSpace(codigoBarras))
            sql += " WHERE b.CodigoBarras = @CB";
        sql += " ORDER BY b.FechaCambio DESC";
        return (await conn.QueryAsync<BitacoraStatus>(sql, new { CB = codigoBarras })).ToList();
    }

    // ══════════════════════════════════════════════
    // PRE BAJAS
    // ══════════════════════════════════════════════
    public async Task<List<PreBaja>> ObtenerPreBajasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<PreBaja>(@"
            SELECT pb.CodigoBarras, pb.IdTipoBaja, p.Descripcion, pb.FechaCaptura
            FROM PreBajas pb
            LEFT JOIN Piezas p ON pb.CodigoBarras = p.CodigoBarras
            WHERE CAST(pb.FechaCaptura AS DATE) = CAST(GETDATE() AS DATE)
            ORDER BY pb.FechaCaptura DESC")).ToList();
    }

    public async Task<string> CrearPreBajaAsync(string codigoBarras, int tipoBaja)
    {
        using var conn = CreateConnection();
        var existe = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Piezas WHERE CodigoBarras = @CB", new { CB = codigoBarras });
        if (existe == 0) return "Pieza no encontrada";

        await conn.ExecuteAsync(
            "INSERT INTO PreBajas (CodigoBarras, IdTipoBaja, FechaCaptura) VALUES (@CB, @Tipo, GETDATE())",
            new { CB = codigoBarras, Tipo = tipoBaja });
        return "Pre-baja registrada";
    }

    // ══════════════════════════════════════════════
    // LOTES REPETIDAS
    // ══════════════════════════════════════════════
    public async Task<List<LoteRepetida>> ObtenerLotesRepetidasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<LoteRepetida>(@"
            SELECT lr.IdLote, lr.CodigoBarras, cr.Descripcion, lr.Cantidad, lr.Precio,
                   lr.Proveedor, prov.NombreProveedor, lr.IdRemision, lr.IdFactura,
                   lr.CostoBruto, lr.CostoNeto, lr.Utilidad, lr.UtilidadExtra,
                   lr.Impuesto, lr.Divisor, lr.IdMoneda, lr.TCCosto, lr.TCCotizacion, lr.FechaCaptura
            FROM LotesRepetidas lr
            LEFT JOIN CatalogoRepetidas cr ON lr.CodigoBarras = cr.CodigoBarras
            LEFT JOIN Proveedores prov ON lr.Proveedor = prov.Proveedor
            ORDER BY lr.FechaCaptura DESC")).ToList();
    }
}
