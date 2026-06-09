using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// CRUD de piezas estándar reutilizables (Catálogo de Repetidas).
/// Genera código de barras auto-incremental via tabla 'contador'.
/// Tablas: CatalogoRepetidas, Etiquetas, contador, vCatalogoRepetidas.
/// </summary>
public class CatalogoRepetidasService
{
    private readonly string _connectionString;
    private readonly ILogger<CatalogoRepetidasService> _logger;

    /// <summary>
    /// Prefijo de tienda para códigos de barras (IdTienda en el sistema legacy).
    /// Default "0" para ambiente web.
    /// </summary>
    private const string TiendaPrefijo = "0";

    public CatalogoRepetidasService(string connectionString, ILogger<CatalogoRepetidasService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista todas las piezas repetidas desde la vista vCatalogoRepetidas.
    /// Soporta búsqueda por texto en descripción, proveedor o grupo.
    /// </summary>
    public async Task<List<RepetidaItem>> ListarAsync(string? buscar)
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT TOP 50 CodigoBarras, Descripcion, Proveedor, IdGrupo,
                   Kilates, Precio, FechaCaptura, FechaUltEdicion, IdUsuario,
                   IdDivisor, NombreProveedor, Grupo, Divisor, DescDivisor
            FROM vCatalogoRepetidas
            WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            sql += @"
              AND (Descripcion LIKE @Buscar
                OR NombreProveedor LIKE @Buscar
                OR Grupo LIKE @Buscar
                OR CodigoBarras LIKE @Buscar)";
        }

        sql += " ORDER BY FechaCaptura DESC";

        var items = await conn.QueryAsync<RepetidaItem>(sql, new
        {
            Buscar = $"%{buscar}%"
        });

        return items.ToList();
    }

    /// <summary>
    /// Obtiene una pieza por su código de barras
    /// </summary>
    public async Task<RepetidaItem?> ObtenerPorCodigoAsync(string codigoBarras)
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT TOP 1 CodigoBarras, Descripcion, Proveedor, IdGrupo,
                   Kilates, Precio, FechaCaptura, FechaUltEdicion, IdUsuario,
                   IdDivisor, NombreProveedor, Grupo, Divisor, DescDivisor
            FROM vCatalogoRepetidas
            WHERE CodigoBarras = @CodigoBarras";

        return await conn.QueryFirstOrDefaultAsync<RepetidaItem>(sql, new { CodigoBarras = codigoBarras });
    }

    /// <summary>
    /// Genera un nuevo código de barras usando la tabla 'contador'.
    /// Formato: {TiendaPrefijo}{secuencia de 5 dígitos} (6 chars total).
    /// Usa transacción para atomicidad.
    /// </summary>
    private async Task<string> GenerarCodigoBarrasAsync(IDbConnection conn, IDbTransaction tx)
    {
        var nextCode = await conn.ExecuteScalarAsync<int>(
            "SELECT ISNULL(CodigoBarras, 0) + 1 FROM contador", transaction: tx);

        await conn.ExecuteAsync(
            "UPDATE contador SET CodigoBarras = CodigoBarras + 1", transaction: tx);

        var secuencia = nextCode.ToString().PadLeft(5, '0');
        return TiendaPrefijo + secuencia;
    }

    /// <summary>
    /// Crea una nueva pieza repetida con código de barras auto-generado.
    /// También inserta registro en tabla Etiquetas (comportamiento legacy).
    /// </summary>
    public async Task<string> CrearAsync(RepetidaForm form)
    {
        using var conn = (SqlConnection)CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var codigoBarras = await GenerarCodigoBarrasAsync(conn, tx);

            // INSERT en CatalogoRepetidas
            await conn.ExecuteAsync(@"
                INSERT INTO CatalogoRepetidas
                    (CodigoBarras, Descripcion, Proveedor, IdGrupo, Kilates,
                     Precio, FechaCaptura, FechaUltEdicion, IdUsuario, IdDivisor)
                VALUES
                    (@CodigoBarras, @Descripcion, @Proveedor, @IdGrupo, @Kilates,
                     @Precio, GETUTCDATE(), GETUTCDATE(), 1, @IdDivisor)",
                new
                {
                    CodigoBarras = codigoBarras,
                    form.Descripcion,
                    form.Proveedor,
                    form.IdGrupo,
                    form.Kilates,
                    form.Precio,
                    form.IdDivisor
                }, tx);

            // INSERT en Etiquetas (comportamiento legacy del VB6)
            await conn.ExecuteAsync(@"
                INSERT INTO Etiquetas
                    (CodigoBarras, IdLocalizacion, IdTienda, Descripcion,
                     FechaCaptura, IdUsuario, FechaUltEdicion, Precio, IdTabla)
                VALUES
                    (@CodigoBarras, 0, 0, @Descripcion,
                     GETUTCDATE(), 1, GETUTCDATE(), @Precio, 0)",
                new
                {
                    CodigoBarras = codigoBarras,
                    form.Descripcion,
                    form.Precio
                }, tx);

            tx.Commit();

            _logger.LogInformation("Pieza repetida creada: {Codigo} - {Desc}",
                codigoBarras, form.Descripcion);

            return codigoBarras;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Actualiza una pieza repetida existente por su código de barras
    /// </summary>
    public async Task ActualizarAsync(RepetidaForm form)
    {
        using var conn = CreateConnection();

        await conn.ExecuteAsync(@"
            UPDATE CatalogoRepetidas SET
                Descripcion = @Descripcion,
                Proveedor = @Proveedor,
                IdGrupo = @IdGrupo,
                Kilates = @Kilates,
                Precio = @Precio,
                IdDivisor = @IdDivisor,
                FechaUltEdicion = GETUTCDATE()
            WHERE CodigoBarras = @CodigoBarras",
            new
            {
                form.CodigoBarras,
                form.Descripcion,
                form.Proveedor,
                form.IdGrupo,
                form.Kilates,
                form.Precio,
                form.IdDivisor
            });

        _logger.LogInformation("Pieza repetida actualizada: {Codigo}", form.CodigoBarras);
    }

    /// <summary>
    /// Elimina una pieza repetida y su etiqueta asociada
    /// </summary>
    public async Task EliminarAsync(string codigoBarras)
    {
        using var conn = (SqlConnection)CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync(
                "DELETE FROM Etiquetas WHERE CodigoBarras = @CodigoBarras",
                new { CodigoBarras = codigoBarras }, tx);

            await conn.ExecuteAsync(
                "DELETE FROM CatalogoRepetidas WHERE CodigoBarras = @CodigoBarras",
                new { CodigoBarras = codigoBarras }, tx);

            tx.Commit();

            _logger.LogInformation("Pieza repetida eliminada: {Codigo}", codigoBarras);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Obtiene la lista de proveedores para dropdown
    /// </summary>
    public async Task<List<CatalogoDropdownItem>> ObtenerProveedoresAsync()
    {
        using var conn = CreateConnection();

        var items = await conn.QueryAsync<CatalogoDropdownItem>(
            "SELECT TOP 50 Proveedor AS Id, NombreProveedor AS Nombre FROM Proveedores ORDER BY NombreProveedor");

        return items.ToList();
    }

    /// <summary>
    /// Obtiene todos los proveedores (sin límite de 50) para dropdown searchable
    /// </summary>
    public async Task<List<CatalogoDropdownItem>> ObtenerTodosProveedoresAsync()
    {
        using var conn = CreateConnection();

        var items = await conn.QueryAsync<CatalogoDropdownItem>(
            "SELECT Proveedor AS Id, NombreProveedor AS Nombre FROM Proveedores ORDER BY NombreProveedor");

        return items.ToList();
    }

    /// <summary>
    /// Obtiene la lista de grupos para dropdown
    /// </summary>
    public async Task<List<CatalogoDropdownItem>> ObtenerGruposAsync()
    {
        using var conn = CreateConnection();

        var items = await conn.QueryAsync<CatalogoDropdownItem>(
            "SELECT IdGrupo AS Id, Grupo AS Nombre FROM Grupos ORDER BY Grupo");

        return items.ToList();
    }

    /// <summary>
    /// Obtiene la lista de divisores para dropdown
    /// </summary>
    public async Task<List<DivisorDropdownItem>> ObtenerDivisoresAsync()
    {
        using var conn = CreateConnection();

        var items = await conn.QueryAsync<DivisorDropdownItem>(
            "SELECT IdDivisor, Divisor, Descripcion FROM Divisores ORDER BY Descripcion");

        return items.ToList();
    }
}
