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
    // REPORTE FALTANTES
    // ══════════════════════════════════════════════
    public async Task<List<PiezaFaltante>> ObtenerFaltantesAsync(string? buscar = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT p.CodigoBarras, p.Descripcion, p.Precio, g.Grupo,
                       p.Modelo, p.Linea, p.Kilates, p.Peso, p.NumSerie,
                       cf.Comentarios AS Comentario
                    FROM Piezas p
                    LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
                    LEFT JOIN ComentariosFaltantes cf ON p.CodigoBarras = cf.CodigoBarras
                    WHERE p.Faltante = 1";
        if (!string.IsNullOrWhiteSpace(buscar))
            sql += @" AND (p.CodigoBarras LIKE @B OR p.Descripcion LIKE @B
                       OR g.Grupo LIKE @B OR cf.Comentarios LIKE @B
                       OR p.Modelo LIKE @B OR p.Linea LIKE @B)";
        sql += " ORDER BY p.CodigoBarras";
        return (await conn.QueryAsync<PiezaFaltante>(sql, new { B = $"%{buscar}%" })).ToList();
    }

    public async Task GuardarComentarioFaltanteAsync(string codigoBarras, string comentario)
    {
        using var conn = CreateConnection();
        var existe = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ComentariosFaltantes WHERE CodigoBarras = @CB", new { CB = codigoBarras });
        if (existe > 0)
            await conn.ExecuteAsync("UPDATE ComentariosFaltantes SET Comentarios = @C WHERE CodigoBarras = @CB",
                new { CB = codigoBarras, C = comentario });
        else
            await conn.ExecuteAsync("INSERT INTO ComentariosFaltantes (CodigoBarras, Comentarios) VALUES (@CB, @C)",
                new { CB = codigoBarras, C = comentario });
    }

    // ══════════════════════════════════════════════
    // PRE BAJAS
    // ══════════════════════════════════════════════

    /// <summary>
    /// Obtiene las pre-bajas del día actual, con JOIN a piezas para obtener la descripción.
    /// </summary>
    public async Task<List<PreBaja>> ObtenerPreBajasDelDiaAsync()
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT TOP 50
                   pb.CodigoBarras,
                   p.Descripcion,
                   pb.IdTipoBaja,
                   pb.FechaCaptura
              FROM PREBAJAS pb
              LEFT JOIN piezas p ON p.CodigoBarras = pb.CodigoBarras
             WHERE CAST(pb.FechaCaptura AS DATE) = CAST(GETUTCDATE() AS DATE)
             ORDER BY pb.FechaCaptura DESC";
        return (await conn.QueryAsync<PreBaja>(sql)).ToList();
    }

    /// <summary>
    /// Busca una pre-baja por código de barras exacto.
    /// </summary>
    public async Task<List<PreBaja>> BuscarPreBajaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT TOP 50
                   pb.CodigoBarras,
                   p.Descripcion,
                   pb.IdTipoBaja,
                   pb.FechaCaptura
              FROM PREBAJAS pb
              LEFT JOIN piezas p ON p.CodigoBarras = pb.CodigoBarras
             WHERE pb.CodigoBarras = @CodigoBarras
             ORDER BY pb.FechaCaptura DESC";
        return (await conn.QueryAsync<PreBaja>(sql, new { CodigoBarras = codigoBarras })).ToList();
    }

    /// <summary>
    /// Registra una pre-baja. Valida que el código tenga al menos 6 dígitos numéricos.
    /// </summary>
    public async Task RegistrarPreBajaAsync(string codigoBarras, int idTipoBaja)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras) || codigoBarras.Length < 6 || !long.TryParse(codigoBarras, out _))
            throw new ArgumentException("El código de barras debe tener al menos 6 dígitos numéricos.");

        if (idTipoBaja != 1 && idTipoBaja != 2)
            throw new ArgumentException("Tipo de baja inválido. Use 1 (Venta) o 2 (Devolución).");

        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO PREBAJAS (CodigoBarras, IdTipoBaja, FechaCaptura) VALUES (@CodigoBarras, @IdTipoBaja, GETUTCDATE())",
            new { CodigoBarras = codigoBarras.Trim(), IdTipoBaja = idTipoBaja });
    }

    /// <summary>
    /// Elimina una pre-baja por código de barras y fecha de captura.
    /// </summary>
    public async Task EliminarPreBajaAsync(string codigoBarras, DateTime fechaCaptura)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM PREBAJAS WHERE CodigoBarras = @CodigoBarras AND FechaCaptura = @FechaCaptura",
            new { CodigoBarras = codigoBarras, FechaCaptura = fechaCaptura });
    }

    // ══════════════════════════════════════════════
    // CAMBIO DE STATUS
    // ══════════════════════════════════════════════

    public async Task<List<StatusPieza>> ObtenerStatusPiezasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<StatusPieza>(
            "SELECT TOP 50 IdStatus, NombreStatus FROM statuspiezas ORDER BY NombreStatus")).ToList();
    }

    public async Task<List<BitacoraStatus>> ObtenerBitacoraStatusAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<BitacoraStatus>(@"
            SELECT TOP 50 b.IdCambioStatus, b.CodigoBarras,
                   b.IdStatusAnterior, sa.NombreStatus AS NombreStatusAnterior,
                   b.IdStatusNuevo, sn.NombreStatus AS NombreStatusNuevo,
                   b.FechaCaptura, b.IdUsuario
            FROM bitacorastatus b
            LEFT JOIN statuspiezas sa ON sa.IdStatus = b.IdStatusAnterior
            LEFT JOIN statuspiezas sn ON sn.IdStatus = b.IdStatusNuevo
            WHERE b.CodigoBarras = @CB
            ORDER BY b.IdCambioStatus DESC",
            new { CB = codigoBarras })).ToList();
    }

    public async Task<string> CambiarStatusPiezaAsync(string codigoBarras, int nuevoStatus, int idUsuario)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = ((SqlConnection)conn).BeginTransaction();
        try
        {
            var statusAnterior = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT TOP 1 IdStatus FROM piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);
            if (statusAnterior is null) return "La pieza no existe.";
            if (statusAnterior == nuevoStatus) return "El nuevo status es igual al actual.";

            await conn.ExecuteAsync(
                "UPDATE piezas SET IdStatus = @S, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB",
                new { S = nuevoStatus, CB = codigoBarras }, tx);
            await conn.ExecuteAsync(@"
                INSERT INTO bitacorastatus (CodigoBarras, IdStatusAnterior, IdStatusNuevo, IdUsuario, FechaCaptura)
                VALUES (@CB, @Ant, @Nuevo, @Usr, GETUTCDATE())",
                new { CB = codigoBarras, Ant = statusAnterior, Nuevo = nuevoStatus, Usr = idUsuario }, tx);

            tx.Commit();
            return $"Status de pieza {codigoBarras} cambiado exitosamente.";
        }
        catch { tx.Rollback(); throw; }
    }

    // ══════════════════════════════════════════════
    // PIEZAS SENCILLAS CRUD
    // ══════════════════════════════════════════════

    /// <summary>
    /// Obtiene una pieza sencilla por codigo de barras con datos de JOIN.
    /// </summary>
    public async Task<Pieza?> ObtenerPiezaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 1
                p.CodigoBarras, p.Descripcion, p.IdRemision, p.IdFactura, p.IdGrupo,
                p.CBPieza, p.DescPieza, p.CNPieza,
                p.Peso, p.PrecioGramo, p.CBPeso, p.DescPeso, p.CNPeso,
                p.CBManoObra, p.DescManoObra, p.CNManoObra, p.DescripcionManoObra,
                p.CBTotal, p.CNTotal,
                p.CBFactura, p.DescFactura, p.CNFactura,
                p.IdMoneda, p.TCCotizacion, p.TCCosto,
                p.Utilidad, p.UtilidadExtra, p.Impuesto, p.Divisor, p.Precio,
                p.Kilates, p.Modelo, p.Linea,
                p.Quilates, p.Color, p.Pureza, p.Corte,
                p.NumSerie, p.Obs1, p.Obs2,
                p.FechaCaptura, p.IdUsuario, p.FechaUltEdicion,
                p.IdDivisor, p.IdTienda, p.IdLocalizacion, p.ArchivoFoto,
                p.faltante AS Faltante, p.IdStatus, p.CBPadre,
                p.Proveedor,
                pr.NombreProveedor, g.Grupo1 AS Grupo
            FROM piezas p
            LEFT JOIN vProveedores pr ON pr.Proveedor = p.Proveedor
            LEFT JOIN grupos g ON g.IdGrupo = p.IdGrupo
            WHERE p.CodigoBarras = @CB";
        return await conn.QueryFirstOrDefaultAsync<Pieza>(sql, new { CB = codigoBarras });
    }

    /// <summary>
    /// Busca piezas sencillas con filtros opcionales.
    /// </summary>
    public async Task<List<Pieza>> ObtenerPiezasSencillasAsync(string? buscar, int? idGrupo, int? proveedor)
    {
        using var conn = CreateConnection();
        var where = "WHERE 1=1";
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            where += " AND (p.CodigoBarras LIKE @Buscar OR p.Descripcion LIKE @Buscar)";
            p.Add("Buscar", $"%{buscar}%");
        }
        if (idGrupo.HasValue)
        {
            where += " AND p.IdGrupo = @IdGrupo";
            p.Add("IdGrupo", idGrupo);
        }
        if (proveedor.HasValue)
        {
            where += " AND p.Proveedor = @Proveedor";
            p.Add("Proveedor", proveedor);
        }

        var sql = $@"SELECT TOP 500
                p.CodigoBarras, p.Descripcion, p.IdGrupo, p.Precio,
                p.FechaCaptura, p.IdStatus, p.Proveedor,
                pr.NombreProveedor, g.Grupo1 AS Grupo
            FROM piezas p
            LEFT JOIN vProveedores pr ON pr.Proveedor = p.Proveedor
            LEFT JOIN grupos g ON g.IdGrupo = p.IdGrupo
            {where}
            ORDER BY p.FechaCaptura DESC";
        return (await conn.QueryAsync<Pieza>(sql, p)).ToList();
    }

    /// <summary>
    /// Crea una nueva pieza sencilla.
    /// </summary>
    public async Task CrearPiezaSencillaAsync(Pieza pieza)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO piezas (CodigoBarras, Descripcion, IdRemision, IdFactura, IdGrupo,
                CBPieza, DescPieza, CNPieza, Peso, PrecioGramo, CBPeso, DescPeso, CNPeso,
                CBManoObra, DescManoObra, CNManoObra, DescripcionManoObra,
                CBTotal, CNTotal, CBFactura, DescFactura, CNFactura,
                IdMoneda, TCCotizacion, TCCosto, Utilidad, UtilidadExtra, Impuesto, Divisor, Precio,
                Kilates, Modelo, Linea, Quilates, Color, Pureza, Corte,
                NumSerie, Obs1, Obs2, FechaCaptura, IdUsuario, FechaUltEdicion,
                faltante, IdStatus, CBPadre, Proveedor)
              VALUES (@CodigoBarras, @Descripcion, @IdRemision, @IdFactura, @IdGrupo,
                @CBPieza, @DescPieza, @CNPieza, @Peso, @PrecioGramo, @CBPeso, @DescPeso, @CNPeso,
                @CBManoObra, @DescManoObra, @CNManoObra, @DescripcionManoObra,
                @CBTotal, @CNTotal, @CBFactura, @DescFactura, @CNFactura,
                @IdMoneda, @TCCotizacion, @TCCosto, @Utilidad, @UtilidadExtra, @Impuesto, @Divisor, @Precio,
                @Kilates, @Modelo, @Linea, @Quilates, @Color, @Pureza, @Corte,
                @NumSerie, @Obs1, @Obs2, GETUTCDATE(), @IdUsuario, GETUTCDATE(),
                0, @IdStatus, @CBPadre, @Proveedor)",
            pieza);
    }

    /// <summary>
    /// Actualiza una pieza sencilla existente.
    /// </summary>
    public async Task ActualizarPiezaSencillaAsync(Pieza pieza)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE piezas SET
                Descripcion = @Descripcion, IdRemision = @IdRemision, IdFactura = @IdFactura,
                IdGrupo = @IdGrupo, CBPieza = @CBPieza, DescPieza = @DescPieza, CNPieza = @CNPieza,
                Peso = @Peso, PrecioGramo = @PrecioGramo, CBPeso = @CBPeso, DescPeso = @DescPeso,
                CNPeso = @CNPeso, CBManoObra = @CBManoObra, DescManoObra = @DescManoObra,
                CNManoObra = @CNManoObra, DescripcionManoObra = @DescripcionManoObra,
                CBTotal = @CBTotal, CNTotal = @CNTotal, CBFactura = @CBFactura,
                DescFactura = @DescFactura, CNFactura = @CNFactura,
                IdMoneda = @IdMoneda, TCCotizacion = @TCCotizacion, TCCosto = @TCCosto,
                Utilidad = @Utilidad, UtilidadExtra = @UtilidadExtra, Impuesto = @Impuesto,
                Divisor = @Divisor, Precio = @Precio,
                Kilates = @Kilates, Modelo = @Modelo, Linea = @Linea,
                Quilates = @Quilates, Color = @Color, Pureza = @Pureza, Corte = @Corte,
                NumSerie = @NumSerie, Obs1 = @Obs1, Obs2 = @Obs2,
                FechaUltEdicion = GETUTCDATE(), IdUsuario = @IdUsuario,
                IdStatus = @IdStatus, Proveedor = @Proveedor
              WHERE CodigoBarras = @CodigoBarras",
            pieza);
    }

    // ══════════════════════════════════════════════
    // ELIMINACION DE PIEZAS (con permisos y bitacora)
    // Reglas de negocio migradas de frmSencillas.frm VB6:
    // 1. Ventana de 2 horas desde FechaCaptura = exento de permisos
    // 2. Si etiqueta impresa = siempre requiere permisos (sin importar tiempo)
    // 3. PermisoUsuarios=1 en tabla usuarios = puede eliminar siempre
    // 4. PermisoUsuarios=0 = requiere autorizacion previa (tabla permisocancelar)
    // 5. Bitacora completa en piezascanceladas con FechaBorrado e IdUsuarioBorrado
    // ══════════════════════════════════════════════

    /// <summary>
    /// Verifica si el usuario tiene permiso para eliminar la pieza y la elimina si procede.
    /// Replica la logica de EliminarPieza() del VB6.
    /// </summary>
    public async Task<EliminarPiezaResult> EliminarPiezaConPermisosAsync(string codigoBarras, int idUsuario)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = ((SqlConnection)conn).BeginTransaction();

        try
        {
            // 1. Verificar que la pieza existe
            var pieza = await conn.QueryFirstOrDefaultAsync<Pieza>(
                "SELECT TOP 1 CodigoBarras, FechaCaptura FROM piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            if (pieza == null)
                return new EliminarPiezaResult { Denegado = true, Mensaje = "La pieza no existe." };

            // 2. Verificar si la etiqueta ya fue impresa
            var etiquetaImpresa = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT TOP 1 1 FROM etiquetasimpresas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            // 3. Verificar ventana de 2 horas (solo si etiqueta NO fue impresa)
            var dentroDeVentana = false;
            if (etiquetaImpresa == null && pieza.FechaCaptura.HasValue)
            {
                var horasTranscurridas = (DateTime.UtcNow - pieza.FechaCaptura.Value).TotalHours;
                dentroDeVentana = horasTranscurridas < 2;
            }

            // 4. Si no esta dentro de ventana, verificar permisos del usuario
            if (!dentroDeVentana)
            {
                var permisoUsuario = await conn.QueryFirstOrDefaultAsync<bool?>(
                    "SELECT TOP 1 PermisoUsuarios FROM usuarios WHERE IdUsuario = @Id",
                    new { Id = idUsuario }, tx);

                if (permisoUsuario != true)
                {
                    // Verificar si existe autorizacion previa en permisocancelar
                    var autorizado = await conn.QueryFirstOrDefaultAsync<int?>(
                        "SELECT TOP 1 1 FROM permisocancelar WHERE CodigoBarras = @CB",
                        new { CB = codigoBarras }, tx);

                    if (autorizado == null)
                    {
                        tx.Rollback();
                        return new EliminarPiezaResult
                        {
                            Denegado = true,
                            Mensaje = etiquetaImpresa != null
                                ? "La etiqueta ya fue impresa. Se requiere autorizacion especial para eliminar esta pieza."
                                : "Han pasado mas de 2 horas desde la captura. Se requiere autorizacion especial para eliminar esta pieza."
                        };
                    }
                }
            }

            // 5. Proceder con eliminacion — copiar a bitacora
            await conn.ExecuteAsync(@"
                INSERT INTO piezascanceladas
                    (CodigoBarras, Descripcion, IdRemision, IdFactura, IdGrupo,
                     CBPieza, DescPieza, CNPieza, Peso, PrecioGramo, CBPeso, DescPeso, CNPeso,
                     CBManoObra, DescManoObra, CNManoObra, DescripcionManoObra,
                     CBTotal, CNTotal, CBFactura, DescFactura, CNFactura,
                     IdMoneda, TCCotizacion, TCCosto, Utilidad, UtilidadExtra, Impuesto, Divisor, Precio,
                     Kilates, Modelo, Linea, Quilates, Color, Pureza, Corte,
                     NumSerie, Obs1, Obs2, FechaCaptura, IdUsuario, FechaUltEdicion,
                     IdDivisor, IdTienda, IdLocalizacion, ArchivoFoto, Faltante, IdStatus, CBPadre,
                     FechaBorrado, IdUsuarioBorrado)
                SELECT CodigoBarras, Descripcion, IdRemision, IdFactura, IdGrupo,
                     CBPieza, DescPieza, CNPieza, Peso, PrecioGramo, CBPeso, DescPeso, CNPeso,
                     CBManoObra, DescManoObra, CNManoObra, DescripcionManoObra,
                     CBTotal, CNTotal, CBFactura, DescFactura, CNFactura,
                     IdMoneda, TCCotizacion, TCCosto, Utilidad, UtilidadExtra, Impuesto, Divisor, Precio,
                     Kilates, Modelo, Linea, Quilates, Color, Pureza, Corte,
                     NumSerie, Obs1, Obs2, FechaCaptura, IdUsuario, FechaUltEdicion,
                     IdDivisor, IdTienda, IdLocalizacion, ArchivoFoto, Faltante, IdStatus, CBPadre,
                     GETUTCDATE(), @UserId
                FROM piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras, UserId = idUsuario }, tx);

            // 6. Eliminar registros relacionados
            await conn.ExecuteAsync(
                "DELETE FROM observaciones WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            // Eliminar etiqueta solo si no hay otras piezas con el mismo codigo
            var otrasPiezas = await conn.QueryFirstOrDefaultAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);
            if (otrasPiezas <= 1)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM etiquetas WHERE CodigoBarras = @CB",
                    new { CB = codigoBarras }, tx);
            }

            // Limpiar autorizacion usada
            await conn.ExecuteAsync(
                "DELETE FROM permisocancelar WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            // 7. Eliminar la pieza
            await conn.ExecuteAsync(
                "DELETE FROM piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            tx.Commit();
            _logger.LogInformation("Pieza eliminada con bitacora: {CB} por usuario {User} (dentroVentana={V})",
                codigoBarras, idUsuario, dentroDeVentana);

            return new EliminarPiezaResult
            {
                Success = true,
                Mensaje = "Pieza eliminada exitosamente y registrada en bitacora."
            };
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error al eliminar pieza {CB}", codigoBarras);
            return new EliminarPiezaResult { Mensaje = $"Error interno: {ex.Message}" };
        }
    }

    /// <summary>
    /// Verifica si el usuario puede eliminar una pieza sin ejecutar la eliminacion.
    /// Util para mostrar/ocultar el boton de eliminar en la UI.
    /// </summary>
    public async Task<(bool Permitido, string Razon)> VerificarPermisoEliminacionAsync(string codigoBarras, int idUsuario)
    {
        using var conn = CreateConnection();

        var pieza = await conn.QueryFirstOrDefaultAsync<Pieza>(
            "SELECT TOP 1 CodigoBarras, FechaCaptura FROM piezas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });
        if (pieza == null) return (false, "Pieza no existe");

        // Etiqueta impresa?
        var etiquetaImpresa = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT TOP 1 1 FROM etiquetasimpresas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });

        // Ventana de 2 horas
        if (etiquetaImpresa == null && pieza.FechaCaptura.HasValue)
        {
            var horas = (DateTime.UtcNow - pieza.FechaCaptura.Value).TotalHours;
            if (horas < 2) return (true, "Dentro de ventana de 2 horas");
        }

        // Verificar permisos
        var permisoUsuario = await conn.QueryFirstOrDefaultAsync<bool?>(
            "SELECT TOP 1 PermisoUsuarios FROM usuarios WHERE IdUsuario = @Id",
            new { Id = idUsuario });
        if (permisoUsuario == true) return (true, "Usuario con permisos");

        // Verificar autorizacion previa
        var autorizado = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT TOP 1 1 FROM permisocancelar WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });
        if (autorizado != null) return (true, "Autorizacion previa existente");

        return (false, "Requiere autorizacion especial");
    }

    /// <summary>
    /// Obtiene la bitacora de piezas canceladas, con filtros opcionales.
    /// </summary>
    public async Task<List<PiezaCancelada>> ObtenerPiezasCanceladasAsync(string? buscar, DateTime? desde, DateTime? hasta)
    {
        using var conn = CreateConnection();
        var where = "WHERE 1=1";
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            where += " AND (pc.CodigoBarras LIKE @Buscar OR pc.Descripcion LIKE @Buscar)";
            p.Add("Buscar", $"%{buscar}%");
        }
        if (desde.HasValue)
        {
            where += " AND pc.FechaBorrado >= @Desde";
            p.Add("Desde", desde.Value);
        }
        if (hasta.HasValue)
        {
            where += " AND pc.FechaBorrado <= @Hasta";
            p.Add("Hasta", hasta.Value);
        }

        var sql = $@"SELECT TOP 50
                pc.CodigoBarras, pc.Descripcion, pc.CBTotal, pc.Precio,
                pc.FechaCaptura, pc.FechaBorrado, pc.IdUsuarioBorrado,
                u.Nombre AS NombreUsuarioBorrado
            FROM piezascanceladas pc
            LEFT JOIN usuarios u ON u.IdUsuario = pc.IdUsuarioBorrado
            {where}
            ORDER BY pc.FechaBorrado DESC";
        return (await conn.QueryAsync<PiezaCancelada>(sql, p)).ToList();
    }

    // ══════════════════════════════════════════════
    // INVENTARIO FISICO
    // ══════════════════════════════════════════════

    /// <summary>
    /// Obtiene registros de inventario físico. SoloHoy=true filtra al día actual.
    /// </summary>
    public async Task<List<RegistroInventarioFisico>> ObtenerRegistrosInventarioAsync(bool soloHoy)
    {
        using var conn = CreateConnection();
        var where = soloHoy ? "AND CAST(inv.FechaCaptura AS DATE) = CAST(GETUTCDATE() AS DATE)" : "";
        var sql = $@"SELECT TOP 500
                inv.Id, inv.CodigoBarras, inv.FechaCaptura, inv.FechaUltEdicion, inv.IdUsuario,
                COALESCE(p.Descripcion, vc.Descripcion, s.Descripcion) AS Descripcion,
                COALESCE(p.CBTotal, vc.Precio, s.Precio) AS Precio,
                CASE WHEN p.CodigoBarras IS NOT NULL THEN 'Pieza'
                     WHEN vc.CodigoBarras IS NOT NULL THEN 'Compuesta'
                     WHEN s.CodigoBarras IS NOT NULL THEN 'Sobrante'
                     ELSE 'Desconocido' END AS Origen
            FROM InventarioFisico inv
            LEFT JOIN piezas p ON p.CodigoBarras = inv.CodigoBarras
            LEFT JOIN vCompuestas vc ON vc.CodigoBarras = inv.CodigoBarras
            LEFT JOIN sobrantes s ON s.CodigoBarras = inv.CodigoBarras
            WHERE 1=1 {where}
            ORDER BY inv.FechaCaptura DESC";
        return (await conn.QueryAsync<RegistroInventarioFisico>(sql)).ToList();
    }

    /// <summary>
    /// Obtiene registros de existencias (alias para ObtenerRegistrosInventarioAsync).
    /// </summary>
    public async Task<List<RegistroInventarioFisico>> ObtenerRegistroExistenciasAsync(bool soloHoy)
        => await ObtenerRegistrosInventarioAsync(soloHoy);

    /// <summary>
    /// Registra una pieza en InventarioFisico por codigo de barras.
    /// </summary>
    public async Task<string> RegistrarInventarioFisicoAsync(string codigoBarras, int idUsuario)
    {
        using var conn = CreateConnection();
        var existe = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM piezas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });
        if (existe == 0)
            return $"Pieza {codigoBarras} no encontrada en catálogo.";
        await conn.ExecuteAsync(
            @"INSERT INTO InventarioFisico (CodigoBarras, FechaCaptura, FechaUltEdicion, IdUsuario)
              VALUES (@CB, GETUTCDATE(), GETUTCDATE(), @Usr)",
            new { CB = codigoBarras, Usr = idUsuario });
        return $"Pieza {codigoBarras} registrada en inventario.";
    }

    /// <summary>
    /// Elimina un registro de inventario fisico por codigo de barras y fecha.
    /// </summary>
    public async Task EliminarRegistroInventarioAsync(string codigoBarras, DateTime fechaCaptura)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM InventarioFisico WHERE CodigoBarras = @CB AND FechaCaptura = @FC",
            new { CB = codigoBarras, FC = fechaCaptura });
    }

    // ══════════════════════════════════════════════
    // FALTANTES
    // ══════════════════════════════════════════════

    /// <summary>
    /// Obtiene piezas faltantes (en catalogo pero no escaneadas en inventario).
    /// </summary>
    public async Task<List<PiezaFaltante>> ObtenerFaltantesAsync(string? buscar)
    {
        using var conn = CreateConnection();
        var searchWhere = string.IsNullOrWhiteSpace(buscar) ? "" :
            "AND (p.CodigoBarras LIKE @Buscar OR p.Descripcion LIKE @Buscar)";
        var sql = $@"SELECT TOP 1000
                p.CodigoBarras, p.Descripcion, p.CBTotal AS Precio,
                g.Grupo, pf.Comentario
            FROM piezas p
            LEFT JOIN grupos g ON g.IdGrupo = p.IdGrupo
            LEFT JOIN piezasfaltantes pf ON pf.CodigoBarras = p.CodigoBarras
            WHERE p.faltante = 1 {searchWhere}
            ORDER BY p.CodigoBarras";
        return (await conn.QueryAsync<PiezaFaltante>(sql,
            new { Buscar = $"%{buscar}%" })).ToList();
    }

    /// <summary>
    /// Guarda o actualiza el comentario de una pieza faltante.
    /// </summary>
    public async Task GuardarComentarioFaltanteAsync(string codigoBarras, string comentario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"IF EXISTS (SELECT 1 FROM piezasfaltantes WHERE CodigoBarras = @CB)
                UPDATE piezasfaltantes SET Comentario = @Com WHERE CodigoBarras = @CB
              ELSE
                INSERT INTO piezasfaltantes (CodigoBarras, Comentario) VALUES (@CB, @Com)",
            new { CB = codigoBarras, Com = comentario });
    }

    // ══════════════════════════════════════════════
    // LOTES REPETIDAS
    // ══════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los lotes de piezas repetidas.
    /// </summary>
    public async Task<List<LoteRepetida>> ObtenerLotesRepetidasAsync()
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 500
                lr.IdLote, lr.CodigoBarras, cr.Descripcion,
                lr.Cantidad, lr.CostoBruto, lr.Descuento, lr.CostoNeto,
                lr.Utilidad, lr.UtilidadExtra, lr.Impuesto, lr.Divisor,
                lr.IdMoneda, m.Moneda, lr.TCCosto, lr.TCCotizacion, lr.Precio,
                lr.IdRemision, lr.IdFactura, lr.FechaCaptura, lr.FechaUltEdicion, lr.IdUsuario,
                pr.NombreProveedor
            FROM LotesRepetidas lr
            LEFT JOIN catalogorepetidas cr ON cr.CodigoBarras = lr.CodigoBarras
            LEFT JOIN Monedas m ON m.IdMoneda = lr.IdMoneda
            LEFT JOIN vProveedores pr ON pr.Proveedor = cr.Proveedor
            ORDER BY lr.FechaCaptura DESC";
        return (await conn.QueryAsync<LoteRepetida>(sql)).ToList();
    }

    // ══════════════════════════════════════════════
    // PIEZAS COMPUESTAS
    // ══════════════════════════════════════════════

    /// <summary>
    /// Obtiene piezas compuestas con filtro opcional.
    /// </summary>
    public async Task<List<PiezaCompuesta>> ObtenerPiezasCompuestasAsync(string? buscar)
    {
        using var conn = CreateConnection();
        var searchWhere = string.IsNullOrWhiteSpace(buscar) ? "" :
            "AND (vc.CodigoBarras LIKE @Buscar OR vc.Descripcion LIKE @Buscar)";
        var sql = $@"SELECT TOP 500
                vc.CodigoBarras, vc.Descripcion, vc.Precio,
                c.EtiquetaK, c.Linea1, c.Linea2, c.Linea3,
                c.Componentes, c.IdUsuario, c.FechaCaptura, c.FechaUltEdicion
            FROM vCompuestas vc
            LEFT JOIN compuestas c ON c.CodigoBarras = vc.CodigoBarras
            WHERE 1=1 {searchWhere}
            ORDER BY vc.CodigoBarras";
        return (await conn.QueryAsync<PiezaCompuesta>(sql,
            new { Buscar = $"%{buscar}%" })).ToList();
    }

    /// <summary>
    /// Obtiene los componentes de una pieza compuesta.
    /// </summary>
    public async Task<List<ComponenteCompuesta>> ObtenerComponentesAsync(string cbPadre)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 100
                cc.CodigoBarras, cc.CBPadre, cc.Indice,
                p.Descripcion, p.CBTotal AS Precio
            FROM ComponentesCompuestas cc
            LEFT JOIN piezas p ON p.CodigoBarras = cc.CodigoBarras
            WHERE cc.CBPadre = @CB
            ORDER BY cc.Indice";
        return (await conn.QueryAsync<ComponenteCompuesta>(sql, new { CB = cbPadre })).ToList();
    }

    /// <summary>
    /// Crea una nueva pieza compuesta y devuelve su codigo de barras.
    /// </summary>
    public async Task<string> CrearPiezaCompuestaAsync(PiezaCompuesta pc)
    {
        using var conn = CreateConnection();
        var cb = await conn.ExecuteScalarAsync<string>(
            @"INSERT INTO compuestas (Descripcion, EtiquetaK, Linea1, Linea2, Linea3,
                Componentes, IdUsuario, FechaCaptura, FechaUltEdicion)
              VALUES (@Desc, @EtK, @L1, @L2, @L3, @Comp, @Usr, GETUTCDATE(), GETUTCDATE());
              SELECT SCOPE_IDENTITY()",
            new
            {
                Desc = pc.Descripcion, EtK = pc.EtiquetaK, L1 = pc.Linea1,
                L2 = pc.Linea2, L3 = pc.Linea3, Comp = pc.Componentes, Usr = pc.IdUsuario
            });
        return cb ?? pc.CodigoBarras;
    }

    /// <summary>
    /// Agrega un componente a una pieza compuesta.
    /// </summary>
    public async Task AgregarComponenteAsync(string cbPadre, string cbComponente, int indice)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO ComponentesCompuestas (CBPadre, CodigoBarras, Indice)
              VALUES (@CBPadre, @CB, @Indice)",
            new { CBPadre = cbPadre, CB = cbComponente, Indice = indice });
    }

    /// <summary>
    /// Remueve un componente de una pieza compuesta.
    /// </summary>
    public async Task RemoverComponenteAsync(string cbPadre, string cbComponente)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM ComponentesCompuestas WHERE CBPadre = @CBPadre AND CodigoBarras = @CB",
            new { CBPadre = cbPadre, CB = cbComponente });
    }

}
