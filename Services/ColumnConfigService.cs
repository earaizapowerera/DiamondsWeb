using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para gestionar configuraciones de columnas visibles por usuario y vista.
/// Usa las tablas legacy TABLASCOLUMNAS y COLUMNAS con campo UsuarioId agregado.
/// </summary>
public class ColumnConfigService
{
    private readonly string _connectionString;
    private readonly ILogger<ColumnConfigService> _logger;

    /// <summary>
    /// Catalogo de columnas disponibles por vista web.
    /// Key = nombre de vista, Value = lista de columnas con sus labels y defaults.
    /// </summary>
    private static readonly Dictionary<string, List<ColumnDefinition>> _columnCatalogs = new()
    {
        ["vPiezasWeb"] = new()
        {
            new() { Key = "CodigoBarras",    Label = "Codigo",       DefaultVisible = true,  CssClass = "" },
            new() { Key = "Descripcion",     Label = "Descripcion",  DefaultVisible = true,  CssClass = "" },
            new() { Key = "NombreGrupo",     Label = "Grupo",        DefaultVisible = true,  CssClass = "" },
            new() { Key = "CBTotal",         Label = "C. Bruto",     DefaultVisible = true,  CssClass = "text-end" },
            new() { Key = "CNTotal",         Label = "C. Neto",      DefaultVisible = true,  CssClass = "text-end" },
            new() { Key = "Precio",          Label = "Precio",       DefaultVisible = true,  CssClass = "text-end" },
            new() { Key = "Peso",            Label = "Peso",         DefaultVisible = true,  CssClass = "text-end" },
            new() { Key = "Kilates",         Label = "Kilates",      DefaultVisible = true,  CssClass = "" },
            new() { Key = "Modelo",          Label = "Modelo",       DefaultVisible = true,  CssClass = "" },
            new() { Key = "Linea",           Label = "Linea",        DefaultVisible = true,  CssClass = "" },
            new() { Key = "Quilates",        Label = "Quilates",     DefaultVisible = false, CssClass = "text-end" },
            new() { Key = "Color",           Label = "Color",        DefaultVisible = false, CssClass = "" },
            new() { Key = "Pureza",          Label = "Pureza",       DefaultVisible = false, CssClass = "" },
            new() { Key = "Corte",           Label = "Corte",        DefaultVisible = false, CssClass = "" },
            new() { Key = "NumSerie",        Label = "No. Serie",    DefaultVisible = false, CssClass = "" },
            new() { Key = "NombreProveedor", Label = "Proveedor",    DefaultVisible = false, CssClass = "" },
            new() { Key = "FechaCaptura",    Label = "Fecha",        DefaultVisible = false, CssClass = "" },
        },
        ["vPiezasSencillasWeb"] = new()
        {
            new() { Key = "CodigoBarras",    Label = "Codigo",       DefaultVisible = true,  CssClass = "" },
            new() { Key = "Descripcion",     Label = "Descripcion",  DefaultVisible = true,  CssClass = "" },
            new() { Key = "Grupo",           Label = "Grupo",        DefaultVisible = true,  CssClass = "" },
            new() { Key = "NombreProveedor", Label = "Proveedor",    DefaultVisible = true,  CssClass = "" },
            new() { Key = "Precio",          Label = "Precio",       DefaultVisible = true,  CssClass = "text-end" },
            new() { Key = "IdStatus",        Label = "Status",       DefaultVisible = true,  CssClass = "text-center" },
            new() { Key = "FechaCaptura",    Label = "Fecha",        DefaultVisible = true,  CssClass = "" },
            new() { Key = "Peso",            Label = "Peso",         DefaultVisible = false, CssClass = "text-end" },
            new() { Key = "Kilates",         Label = "Kilates",      DefaultVisible = false, CssClass = "" },
            new() { Key = "Modelo",          Label = "Modelo",       DefaultVisible = false, CssClass = "" },
            new() { Key = "Linea",           Label = "Linea",        DefaultVisible = false, CssClass = "" },
            new() { Key = "CBTotal",         Label = "C. Bruto",     DefaultVisible = false, CssClass = "text-end" },
            new() { Key = "CNTotal",         Label = "C. Neto",      DefaultVisible = false, CssClass = "text-end" },
            new() { Key = "Quilates",        Label = "Quilates",     DefaultVisible = false, CssClass = "text-end" },
            new() { Key = "Color",           Label = "Color",        DefaultVisible = false, CssClass = "" },
            new() { Key = "NumSerie",        Label = "No. Serie",    DefaultVisible = false, CssClass = "" },
        },
    };

    public ColumnConfigService(string connectionString, ILogger<ColumnConfigService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    /// <summary>
    /// Obtiene el catalogo de columnas disponibles para una vista.
    /// </summary>
    public List<ColumnDefinition> ObtenerCatalogoColumnas(string vista)
    {
        return _columnCatalogs.TryGetValue(vista, out var cols) ? cols : new();
    }

    /// <summary>
    /// Obtiene la configuracion de columnas del usuario para una vista.
    /// Si no tiene configuracion guardada, retorna los defaults del catalogo.
    /// </summary>
    public async Task<ColumnasUsuarioResponse> ObtenerConfiguracionUsuarioAsync(int idUsuario, string vista)
    {
        var catalogo = ObtenerCatalogoColumnas(vista);
        if (catalogo.Count == 0)
        {
            return new ColumnasUsuarioResponse
            {
                TodasLasColumnas = catalogo,
                ColumnasVisibles = new()
            };
        }

        using var conn = CreateConnection();

        // Buscar configuracion del usuario para esta vista
        var config = await conn.QueryFirstOrDefaultAsync<TablaColumnaConfig>(
            @"SELECT TOP 1 IdTablaColumnas, Descripcion, Vista, UsuarioId, FechaCaptura, FechaUltEdicion
              FROM TABLASCOLUMNAS
              WHERE Vista = @Vista AND UsuarioId = @UsuarioId
              ORDER BY FechaUltEdicion DESC",
            new { Vista = vista, UsuarioId = idUsuario });

        if (config == null)
        {
            // Sin configuracion: retornar defaults
            return new ColumnasUsuarioResponse
            {
                TodasLasColumnas = catalogo,
                ColumnasVisibles = catalogo.Where(c => c.DefaultVisible).Select(c => c.Key).ToList()
            };
        }

        // Cargar columnas guardadas (Ancho > 0 = visible)
        var columnas = (await conn.QueryAsync<ColumnaConfig>(
            @"SELECT TOP 50 IdTablaColumnas, Columna, Ancho
              FROM COLUMNAS
              WHERE IdTablaColumnas = @Id",
            new { Id = config.IdTablaColumnas })).ToList();

        var visibles = columnas.Where(c => c.Ancho > 0).Select(c => c.Columna).ToList();

        return new ColumnasUsuarioResponse
        {
            IdTablaColumnas = config.IdTablaColumnas,
            Descripcion = config.Descripcion,
            TodasLasColumnas = catalogo,
            ColumnasVisibles = visibles
        };
    }

    /// <summary>
    /// Guarda la configuracion de columnas del usuario.
    /// Si ya existe una config para este usuario+vista, la actualiza.
    /// Si no, crea una nueva.
    /// </summary>
    public async Task<int> GuardarConfiguracionAsync(int idUsuario, string vista, string descripcion, List<string> columnasVisibles)
    {
        var catalogo = ObtenerCatalogoColumnas(vista);
        if (catalogo.Count == 0)
            throw new ArgumentException($"Vista no reconocida: {vista}");

        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            // Buscar si ya tiene una configuracion
            var existing = await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 IdTablaColumnas FROM TABLASCOLUMNAS
                  WHERE Vista = @Vista AND UsuarioId = @UsuarioId",
                new { Vista = vista, UsuarioId = idUsuario }, tx);

            int idTablaColumnas;

            if (existing.HasValue)
            {
                idTablaColumnas = existing.Value;

                // Actualizar descripcion y fecha
                await conn.ExecuteAsync(
                    @"UPDATE TABLASCOLUMNAS
                      SET Descripcion = @Descripcion, FechaUltEdicion = GETUTCDATE()
                      WHERE IdTablaColumnas = @Id",
                    new { Descripcion = descripcion, Id = idTablaColumnas }, tx);

                // Eliminar columnas anteriores
                await conn.ExecuteAsync(
                    "DELETE FROM COLUMNAS WHERE IdTablaColumnas = @Id",
                    new { Id = idTablaColumnas }, tx);
            }
            else
            {
                // Obtener siguiente ID del contador
                var nextId = await conn.QuerySingleAsync<int>(
                    "SELECT TOP 1 ISNULL(MAX(IdTablaColumnas), 0) + 1 FROM TABLASCOLUMNAS",
                    transaction: tx);

                idTablaColumnas = nextId;

                // Insertar nueva configuracion
                await conn.ExecuteAsync(
                    @"INSERT INTO TABLASCOLUMNAS (IdTablaColumnas, Descripcion, Vista, FechaCaptura, FechaUltEdicion, UsuarioId)
                      VALUES (@Id, @Descripcion, @Vista, GETUTCDATE(), GETUTCDATE(), @UsuarioId)",
                    new { Id = idTablaColumnas, Descripcion = descripcion, Vista = vista, UsuarioId = idUsuario }, tx);
            }

            // Insertar columnas: visible = Ancho 100, oculta = Ancho 0
            foreach (var col in catalogo)
            {
                var ancho = columnasVisibles.Contains(col.Key) ? 100 : 0;
                await conn.ExecuteAsync(
                    @"INSERT INTO COLUMNAS (IdTablaColumnas, Columna, Ancho, FechaCaptura)
                      VALUES (@Id, @Columna, @Ancho, GETUTCDATE())",
                    new { Id = idTablaColumnas, Columna = col.Key, Ancho = ancho }, tx);
            }

            tx.Commit();
            _logger.LogInformation("Configuracion de columnas guardada. Vista={Vista}, Usuario={UserId}, Id={Id}",
                vista, idUsuario, idTablaColumnas);

            return idTablaColumnas;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Elimina la configuracion del usuario para una vista (vuelve a defaults).
    /// </summary>
    public async Task<bool> EliminarConfiguracionAsync(int idUsuario, string vista)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var config = await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 IdTablaColumnas FROM TABLASCOLUMNAS
                  WHERE Vista = @Vista AND UsuarioId = @UsuarioId",
                new { Vista = vista, UsuarioId = idUsuario }, tx);

            if (!config.HasValue) return false;

            await conn.ExecuteAsync(
                "DELETE FROM COLUMNAS WHERE IdTablaColumnas = @Id",
                new { Id = config.Value }, tx);

            await conn.ExecuteAsync(
                "DELETE FROM TABLASCOLUMNAS WHERE IdTablaColumnas = @Id",
                new { Id = config.Value }, tx);

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Obtiene las configuraciones compartidas (VB6 legacy) para una vista.
    /// </summary>
    public async Task<List<TablaColumnaConfig>> ObtenerConfiguracionesCompartidasAsync(string vistaLegacy)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<TablaColumnaConfig>(
            @"SELECT TOP 50 IdTablaColumnas, Descripcion, Vista, UsuarioId, FechaCaptura, FechaUltEdicion
              FROM TABLASCOLUMNAS
              WHERE Vista = @Vista AND UsuarioId IS NULL
              ORDER BY Descripcion",
            new { Vista = vistaLegacy })).ToList();
    }
}
