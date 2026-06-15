using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para subir, listar y vincular fotos de piezas.
/// Almacena archivos en wwwroot/fotos-piezas/ y registros en PiezasFotos.
/// Soporta uploads desde web (navegador) y mobile (app Diamonds).
/// </summary>
public class FotoService
{
    private readonly string _connectionString;
    private readonly ILogger<FotoService> _logger;
    private readonly string _fotosPath;

    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public FotoService(string connectionString, string fotosPath, ILogger<FotoService> logger)
    {
        _connectionString = connectionString;
        _fotosPath = fotosPath;
        _logger = logger;

        // Asegurar que existe el directorio
        if (!Directory.Exists(_fotosPath))
            Directory.CreateDirectory(_fotosPath);
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ==================== UPLOAD ====================

    /// <summary>
    /// Sube una foto al filesystem y registra en BD.
    /// </summary>
    public async Task<SubirFotoResult> SubirFotoAsync(Stream fileStream, string originalFileName,
        string contentType, long fileSize, int userId, string source = "web")
    {
        try
        {
            // Validar extension
            var ext = Path.GetExtension(originalFileName);
            if (!ExtensionesPermitidas.Contains(ext))
                return new SubirFotoResult { Success = false, Error = $"Extension no permitida: {ext}. Use: {string.Join(", ", ExtensionesPermitidas)}" };

            // Validar tamano
            if (fileSize > MaxFileSize)
                return new SubirFotoResult { Success = false, Error = $"Archivo muy grande ({fileSize / 1024 / 1024}MB). Maximo: {MaxFileSize / 1024 / 1024}MB" };

            // Generar nombre unico
            var storedFileName = $"{Guid.NewGuid()}{ext.ToLower()}";
            var filePath = Path.Combine(_fotosPath, storedFileName);

            // Guardar archivo
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }

            // Registrar en BD
            using var db = CreateConnection();
            var sql = @"INSERT INTO PiezasFotos (FileName, StoredFileName, ContentType, FileSize, UserId, UploadedAt, Source, IsLinked)
                         VALUES (@FileName, @StoredFileName, @ContentType, @FileSize, @UserId, GETUTCDATE(), @Source, 0);
                         SELECT CAST(SCOPE_IDENTITY() AS INT)";
            var id = await db.QuerySingleAsync<int>(sql, new
            {
                FileName = Path.GetFileName(originalFileName),
                StoredFileName = storedFileName,
                ContentType = contentType,
                FileSize = fileSize,
                UserId = userId,
                Source = source
            });

            _logger.LogInformation("Foto subida: {Id} ({FileName}) por usuario {User} desde {Source}",
                id, originalFileName, userId, source);

            return new SubirFotoResult
            {
                Success = true,
                FotoId = id,
                Url = $"/fotos-piezas/{storedFileName}",
                StoredFileName = storedFileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir foto: {FileName}", originalFileName);
            return new SubirFotoResult { Success = false, Error = ex.Message };
        }
    }

    // ==================== LISTAR ====================

    /// <summary>
    /// Obtiene las ultimas N fotos no vinculadas de un usuario (para seleccionar desde web).
    /// </summary>
    public async Task<List<PiezaFoto>> ObtenerFotosRecientesAsync(int userId, int count = 3, string? source = null)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP (@Count) Id, FileName, StoredFileName, ContentType, FileSize,
                     UserId, UploadedAt, Source, CodigoBarras, IsLinked
                     FROM PiezasFotos
                     WHERE UserId = @UserId AND IsLinked = 0
                     AND (@Source IS NULL OR Source = @Source)
                     ORDER BY UploadedAt DESC";
        return (await db.QueryAsync<PiezaFoto>(sql, new { Count = count, UserId = userId, Source = source })).ToList();
    }

    /// <summary>
    /// Obtiene una foto por su ID.
    /// </summary>
    public async Task<PiezaFoto?> ObtenerFotoAsync(int fotoId)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 1 Id, FileName, StoredFileName, ContentType, FileSize,
                     UserId, UploadedAt, Source, CodigoBarras, IsLinked
                     FROM PiezasFotos WHERE Id = @Id";
        return await db.QueryFirstOrDefaultAsync<PiezaFoto>(sql, new { Id = fotoId });
    }

    /// <summary>
    /// Obtiene la foto vinculada a una pieza (por ArchivoFoto/StoredFileName).
    /// </summary>
    public async Task<PiezaFoto?> ObtenerFotoPorNombreAsync(string storedFileName)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 1 Id, FileName, StoredFileName, ContentType, FileSize,
                     UserId, UploadedAt, Source, CodigoBarras, IsLinked
                     FROM PiezasFotos WHERE StoredFileName = @StoredFileName";
        return await db.QueryFirstOrDefaultAsync<PiezaFoto>(sql, new { StoredFileName = storedFileName });
    }

    // ==================== VINCULAR ====================

    /// <summary>
    /// Vincula una foto a una pieza (establece CodigoBarras y IsLinked=1).
    /// </summary>
    public async Task<bool> VincularFotoAsync(int fotoId, string codigoBarras)
    {
        try
        {
            using var db = CreateConnection();

            // Desvincular foto previa de esta pieza (si existe)
            await db.ExecuteAsync(
                "UPDATE PiezasFotos SET IsLinked = 0, CodigoBarras = NULL WHERE CodigoBarras = @CB",
                new { CB = codigoBarras });

            // Vincular la nueva
            await db.ExecuteAsync(
                "UPDATE PiezasFotos SET CodigoBarras = @CB, IsLinked = 1 WHERE Id = @Id",
                new { CB = codigoBarras, Id = fotoId });

            _logger.LogInformation("Foto {Id} vinculada a pieza {CB}", fotoId, codigoBarras);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al vincular foto {Id} a pieza {CB}", fotoId, codigoBarras);
            return false;
        }
    }

    /// <summary>
    /// Vincula una foto por su StoredFileName a una pieza.
    /// </summary>
    public async Task VincularFotoPorNombreAsync(string storedFileName, string codigoBarras)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            "UPDATE PiezasFotos SET CodigoBarras = @CB, IsLinked = 1 WHERE StoredFileName = @SF",
            new { CB = codigoBarras, SF = storedFileName });
    }

    // ==================== ELIMINAR ====================

    /// <summary>
    /// Elimina una foto del filesystem y BD.
    /// </summary>
    public async Task<bool> EliminarFotoAsync(int fotoId)
    {
        try
        {
            using var db = CreateConnection();
            var foto = await ObtenerFotoAsync(fotoId);
            if (foto == null) return false;

            // Eliminar archivo
            var filePath = Path.Combine(_fotosPath, foto.StoredFileName);
            if (File.Exists(filePath))
                File.Delete(filePath);

            // Eliminar registro
            await db.ExecuteAsync("DELETE FROM PiezasFotos WHERE Id = @Id", new { Id = fotoId });

            _logger.LogInformation("Foto eliminada: {Id} ({FileName})", fotoId, foto.FileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar foto {Id}", fotoId);
            return false;
        }
    }
}
