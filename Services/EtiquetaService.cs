using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para gestión de plantillas de etiquetas.
/// Migra la funcionalidad de frmDisenioEtiquetas.frm (VB6/BarTender).
/// Tablas: diseñosetiquetas (catálogo) y contador (configuración activa).
/// </summary>
public class EtiquetaService
{
    private readonly string _connectionString;
    private readonly ILogger<EtiquetaService> _logger;

    public EtiquetaService(string connectionString, ILogger<EtiquetaService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Obtiene todas las plantillas de etiquetas del catálogo.
    /// </summary>
    public async Task<List<DisenoEtiqueta>> ObtenerPlantillasAsync()
    {
        const string sql = "SELECT TOP 50 IdDiseñoEtiqueta AS IdDisenoEtiqueta, Archivo FROM diseñosetiquetas ORDER BY Archivo";
        using var db = CreateConnection();
        var result = await db.QueryAsync<DisenoEtiqueta>(sql);
        return result.ToList();
    }

    /// <summary>
    /// Obtiene la configuración actual (qué plantilla está activa).
    /// Lee de la tabla contador (registro único).
    /// </summary>
    public async Task<ConfiguracionEtiqueta> ObtenerConfiguracionAsync()
    {
        const string sql = "SELECT TOP 1 IdDiseñoEtiqueta AS IdDisenoEtiqueta, ISNULL(ArchivoEtiquetaCompuesta, '') AS ArchivoEtiquetaCompuesta FROM contador";
        using var db = CreateConnection();
        var config = await db.QueryFirstOrDefaultAsync<ConfiguracionEtiqueta>(sql);
        return config ?? new ConfiguracionEtiqueta();
    }

    /// <summary>
    /// Cambia la plantilla de etiqueta sencilla activa.
    /// Equivalente al Combo1_Click del VB6.
    /// </summary>
    public async Task CambiarPlantillaSencillaAsync(int idDisenoEtiqueta)
    {
        const string sql = "UPDATE contador SET IdDiseñoEtiqueta = @Id";
        using var db = CreateConnection();
        await db.ExecuteAsync(sql, new { Id = idDisenoEtiqueta });
        _logger.LogInformation("Plantilla sencilla cambiada a IdDiseñoEtiqueta={Id}", idDisenoEtiqueta);
    }

    /// <summary>
    /// Actualiza la plantilla compuesta.
    /// Equivalente al cmdActualizar_Click del VB6.
    /// </summary>
    public async Task ActualizarPlantillaCompuestaAsync(string archivoCompuesta)
    {
        const string sql = "UPDATE contador SET ArchivoEtiquetaCompuesta = @Archivo";
        using var db = CreateConnection();
        await db.ExecuteAsync(sql, new { Archivo = archivoCompuesta });
        _logger.LogInformation("Plantilla compuesta actualizada a '{Archivo}'", archivoCompuesta);
    }

    /// <summary>
    /// Agrega una nueva plantilla al catálogo.
    /// </summary>
    public async Task<int> AgregarPlantillaAsync(string archivo)
    {
        const string sql = @"
            INSERT INTO diseñosetiquetas (Archivo) VALUES (@Archivo);
            SELECT SCOPE_IDENTITY();";
        using var db = CreateConnection();
        var id = await db.ExecuteScalarAsync<int>(sql, new { Archivo = archivo });
        _logger.LogInformation("Plantilla creada: Id={Id}, Archivo='{Archivo}'", id, archivo);
        return id;
    }

    /// <summary>
    /// Actualiza el nombre de una plantilla existente.
    /// </summary>
    public async Task ActualizarPlantillaAsync(int idDisenoEtiqueta, string archivo)
    {
        const string sql = "UPDATE diseñosetiquetas SET Archivo = @Archivo WHERE IdDiseñoEtiqueta = @Id";
        using var db = CreateConnection();
        await db.ExecuteAsync(sql, new { Id = idDisenoEtiqueta, Archivo = archivo });
        _logger.LogInformation("Plantilla actualizada: Id={Id}, Archivo='{Archivo}'", idDisenoEtiqueta, archivo);
    }

    /// <summary>
    /// Elimina una plantilla del catálogo.
    /// No permite eliminar la plantilla activa.
    /// </summary>
    public async Task<bool> EliminarPlantillaAsync(int idDisenoEtiqueta)
    {
        // Verificar que no sea la plantilla activa
        var config = await ObtenerConfiguracionAsync();
        if (config.IdDisenoEtiqueta == idDisenoEtiqueta)
        {
            _logger.LogWarning("Intento de eliminar plantilla activa Id={Id}", idDisenoEtiqueta);
            return false;
        }

        const string sql = "DELETE FROM diseñosetiquetas WHERE IdDiseñoEtiqueta = @Id";
        using var db = CreateConnection();
        var rows = await db.ExecuteAsync(sql, new { Id = idDisenoEtiqueta });
        _logger.LogInformation("Plantilla eliminada: Id={Id}, rows={Rows}", idDisenoEtiqueta, rows);
        return rows > 0;
    }

    /// <summary>
    /// Verifica la conexión a BD.
    /// </summary>
    public async Task<string> TestConexionAsync()
    {
        try
        {
            using var db = CreateConnection();
            await db.ExecuteAsync("SELECT 1");
            return "OK";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error de conexión en EtiquetaService");
            return $"Error: {ex.Message}";
        }
    }
}
