using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio de homologación de nombres de clientes.
/// Detecta duplicados por: teléfono, normalización de texto y distancia Levenshtein.
/// </summary>
public class HomologacionService
{
    private readonly string _connectionString;
    private readonly ILogger<HomologacionService> _logger;

    // Patrones para limpiar notas/comentarios del nombre
    private static readonly Regex PatronNotas = new(
        @"\s*/\s*(descuento|desc\.?|promocion|conocid[ao]|autorizo|clave|vale|amex|banamex|bancomer|12 meses|3 meses|6 mese?s?).*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PatronNotasGeneral = new(
        @"\s*/\s*\w.*$",
        RegexOptions.Compiled);

    // Teléfonos inválidos que no sirven para matching
    private static readonly HashSet<string> TelefonosInvalidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "S/Numero", "s/t", "S/T", "", "0", "00", "000", "0000"
    };

    public HomologacionService(string connectionString, ILogger<HomologacionService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    #region Normalización de nombres

    /// <summary>
    /// Normaliza un nombre para comparación:
    /// - Quita notas/comentarios (/ descuento, / promoción, etc.)
    /// - Reemplaza 0→O al inicio de palabra
    /// - Quita acentos
    /// - Trim, lowercase, normaliza espacios
    /// - Expande abreviaciones comunes (Ma.→Maria, Ma Carmen→Maria del Carmen)
    /// </summary>
    public static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return string.Empty;

        var n = nombre.Trim();

        // 1. Quitar notas/comentarios después de /
        n = PatronNotas.Replace(n, "");
        // Si aún tiene / con texto, quitar también
        n = PatronNotasGeneral.Replace(n, "");

        // 2. Quitar caracteres especiales al inicio (comillas, apóstrofes sueltos)
        n = n.TrimStart('\'', '"', ' ');

        // 3. Reemplazar 0 (cero) por O al inicio de palabra
        n = Regex.Replace(n, @"\b0([a-zA-Z])", "O$1");

        // 4. Quitar acentos
        n = RemoverAcentos(n);

        // 5. Lowercase
        n = n.ToLowerInvariant();

        // 6. Normalizar espacios
        n = Regex.Replace(n, @"\s+", " ").Trim();

        // 7. Quitar puntos después de abreviaciones
        n = n.Replace(".", " ");
        n = Regex.Replace(n, @"\s+", " ").Trim();

        // 8. Expandir abreviaciones comunes
        n = ExpandirAbreviaciones(n);

        // 9. Normalizar espacios finales
        n = Regex.Replace(n, @"\s+", " ").Trim();

        return n;
    }

    private static string RemoverAcentos(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizedString.Length);
        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string ExpandirAbreviaciones(string nombre)
    {
        // Orden importa: más específico primero
        var abreviaciones = new (string patron, string reemplazo)[]
        {
            (@"\bma del carmen\b", "maria del carmen"),
            (@"\bma carmen\b", "maria del carmen"),
            (@"\bmaricarmen\b", "maria del carmen"),
            (@"\bmari carmen\b", "maria del carmen"),
            (@"\bmary carmen\b", "maria del carmen"),
            (@"\bma eugenia\b", "maria eugenia"),
            (@"\bma euenia\b", "maria eugenia"),
            (@"\bma elena\b", "maria elena"),
            (@"\bma helena\b", "maria elena"),
            (@"\bma luisa\b", "maria luisa"),
            (@"\bma luiza\b", "maria luisa"),
            (@"\bma pilar\b", "maria pilar"),
            (@"\bma teresa\b", "maria teresa"),
            (@"\bma victoria\b", "maria victoria"),
            (@"\bma concepcion\b", "maria concepcion"),
            (@"\bma consepcion\b", "maria concepcion"),
            (@"\bma guadalupe\b", "maria guadalupe"),
            (@"\bma fernanda\b", "maria fernanda"),
            (@"\bma de\b", "maria de"),
            (@"\bma\b", "maria"),  // genérico al final
            (@"\bsra\b", "senora"),
            (@"\bsr\b", "senor"),
        };

        foreach (var (patron, reemplazo) in abreviaciones)
        {
            nombre = Regex.Replace(nombre, patron, reemplazo);
        }

        return nombre;
    }

    /// <summary>
    /// Calcula la distancia Levenshtein entre dos strings
    /// </summary>
    public static int DistanciaLevenshtein(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length, m = t.Length;
        // Optimización: si la diferencia de longitud es mayor que el umbral, no calcular
        if (Math.Abs(n - m) > 5) return Math.Abs(n - m);

        var d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    /// <summary>
    /// Calcula similitud entre 0 y 1 basada en Levenshtein
    /// </summary>
    public static decimal CalcularSimilitud(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        int maxLen = Math.Max(a.Length, b.Length);
        if (maxLen == 0) return 1;
        int dist = DistanciaLevenshtein(a, b);
        return 1m - ((decimal)dist / maxLen);
    }

    /// <summary>
    /// Normaliza un teléfono para comparación
    /// </summary>
    public static string NormalizarTelefono(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono) || TelefonosInvalidos.Contains(telefono.Trim()))
            return string.Empty;

        // Solo dejar dígitos
        var digits = new string(telefono.Where(char.IsDigit).ToArray());

        // Si empieza con 044 o 045 (prefijos móvil obsoletos), quitar
        if (digits.StartsWith("044") || digits.StartsWith("045"))
            digits = digits[3..];

        // Si empieza con 52 y tiene más de 10 dígitos (código país), quitar
        if (digits.StartsWith("52") && digits.Length > 10)
            digits = digits[2..];

        // Si empieza con 1 y tiene 11 dígitos (larga distancia nacional), quitar el 1
        if (digits.StartsWith("1") && digits.Length == 11)
            digits = digits[1..];

        return digits.Length >= 7 ? digits : string.Empty;
    }

    #endregion

    #region Detección de duplicados

    /// <summary>
    /// Ejecuta la detección completa de duplicados.
    /// Fase 1: Por teléfono (nombres que comparten teléfono y son similares).
    /// Fase 2: Por Levenshtein (nombres muy similares sin coincidencia de teléfono).
    /// </summary>
    public async Task<ResultadoDeteccion> DetectarDuplicadosAsync()
    {
        _logger.LogInformation("Iniciando detección de duplicados...");

        // Obtener todos los nombres distintos con sus teléfonos y cantidad de notas
        var nombresRaw = await ObtenerNombresDistintosAsync();
        _logger.LogInformation("Total nombres distintos: {Count}", nombresRaw.Count);

        // Obtener los nombres que ya están en la tabla de homologación
        var existentes = await ObtenerHomologacionesExistentesAsync();
        var nombresYaHomologados = new HashSet<string>(
            existentes.Select(e => e.NombreOriginal),
            StringComparer.OrdinalIgnoreCase);

        // Obtener el siguiente GrupoId
        int nextGrupoId = existentes.Any() ? existentes.Max(e => e.GrupoId) + 1 : 1;

        var gruposNuevos = new List<(string canonical, List<(string nombre, string metodo, decimal confianza)> variantes)>();

        // Fase 1: Agrupar por teléfono normalizado
        var porTelefono = nombresRaw
            .Where(n => !string.IsNullOrEmpty(NormalizarTelefono(n.Telefonos)))
            .GroupBy(n => NormalizarTelefono(n.Telefonos))
            .Where(g => g.Select(x => x.NombreCliente).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2)
            .ToList();

        _logger.LogInformation("Grupos con teléfono compartido: {Count}", porTelefono.Count);

        foreach (var grupoTel in porTelefono)
        {
            var nombresUnicos = grupoTel
                .Select(x => x.NombreCliente.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Dentro de cada grupo de teléfono, encontrar clusters de nombres similares
            var clusters = ClusterizarNombres(nombresUnicos, 0.65m);

            foreach (var cluster in clusters.Where(c => c.Count >= 2))
            {
                // El nombre canónico es el más largo (usualmente el más completo)
                var canonical = cluster.OrderByDescending(n => n.Length).First();

                var variantes = cluster
                    .Where(n => !n.Equals(canonical, StringComparison.OrdinalIgnoreCase))
                    .Where(n => !nombresYaHomologados.Contains(n))
                    .Select(n =>
                    {
                        var sim = CalcularSimilitud(
                            NormalizarNombre(n),
                            NormalizarNombre(canonical));
                        return (nombre: n, metodo: "telefono", confianza: Math.Max(sim, 0.70m));
                    })
                    .ToList();

                if (variantes.Any())
                    gruposNuevos.Add((canonical, variantes));
            }
        }

        // Fase 2: Levenshtein puro (sin coincidencia de teléfono)
        var nombresParaLev = nombresRaw
            .Select(n => n.NombreCliente.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(n => !nombresYaHomologados.Contains(n))
            .Where(n => !gruposNuevos.SelectMany(g => g.variantes.Select(v => v.nombre))
                .Contains(n, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // Para Levenshtein, solo comparar nombres con al menos 2 palabras y normalización cercana
        var nombresNormalizados = nombresParaLev
            .Where(n => NormalizarNombre(n).Split(' ').Length >= 2)
            .Select(n => (original: n, normalizado: NormalizarNombre(n)))
            .Where(x => x.normalizado.Length >= 5)
            .OrderBy(x => x.normalizado)
            .ToList();

        _logger.LogInformation("Nombres para Levenshtein: {Count}", nombresNormalizados.Count);

        var usadosEnLev = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Agrupar por nombres normalizados idénticos primero
        var porNormalizado = nombresNormalizados
            .GroupBy(x => x.normalizado)
            .Where(g => g.Count() >= 2)
            .ToList();

        foreach (var grupo in porNormalizado)
        {
            var items = grupo.ToList();
            var canonical = items.OrderByDescending(x => x.original.Length).First().original;
            var variantes = items
                .Where(x => !x.original.Equals(canonical, StringComparison.OrdinalIgnoreCase))
                .Where(x => !usadosEnLev.Contains(x.original))
                .Select(x => (nombre: x.original, metodo: "normalizacion", confianza: 0.95m))
                .ToList();

            if (variantes.Any())
            {
                gruposNuevos.Add((canonical, variantes));
                foreach (var v in variantes) usadosEnLev.Add(v.nombre);
                usadosEnLev.Add(canonical);
            }
        }

        // Levenshtein para nombres cercanos (distancia <= 2 sobre normalizado)
        for (int i = 0; i < nombresNormalizados.Count; i++)
        {
            if (usadosEnLev.Contains(nombresNormalizados[i].original)) continue;

            var cluster = new List<(string original, string normalizado)> { nombresNormalizados[i] };

            for (int j = i + 1; j < nombresNormalizados.Count; j++)
            {
                if (usadosEnLev.Contains(nombresNormalizados[j].original)) continue;

                var dist = DistanciaLevenshtein(
                    nombresNormalizados[i].normalizado,
                    nombresNormalizados[j].normalizado);

                if (dist <= 2 && dist > 0)
                {
                    var sim = CalcularSimilitud(
                        nombresNormalizados[i].normalizado,
                        nombresNormalizados[j].normalizado);
                    if (sim >= 0.85m)
                        cluster.Add(nombresNormalizados[j]);
                }
            }

            if (cluster.Count >= 2)
            {
                var canonical = cluster.OrderByDescending(x => x.original.Length).First().original;
                var variantes = cluster
                    .Where(x => !x.original.Equals(canonical, StringComparison.OrdinalIgnoreCase))
                    .Select(x =>
                    {
                        var sim = CalcularSimilitud(x.normalizado,
                            NormalizarNombre(canonical));
                        return (nombre: x.original, metodo: "levenshtein", confianza: sim);
                    })
                    .ToList();

                if (variantes.Any())
                {
                    gruposNuevos.Add((canonical, variantes));
                    foreach (var v in variantes) usadosEnLev.Add(v.nombre);
                    usadosEnLev.Add(canonical);
                }
            }
        }

        // Insertar los grupos nuevos en la BD
        int insertados = 0;
        foreach (var grupo in gruposNuevos)
        {
            foreach (var variante in grupo.variantes)
            {
                try
                {
                    await InsertarHomologacionAsync(
                        variante.nombre, grupo.canonical, nextGrupoId,
                        variante.metodo, variante.confianza);
                    insertados++;
                }
                catch (Exception ex)
                {
                    // Puede fallar por UNIQUE constraint si ya existe
                    _logger.LogWarning("No se pudo insertar {Nombre}: {Error}",
                        variante.nombre, ex.Message);
                }
            }
            // También insertar el nombre canónico apuntando a sí mismo
            try
            {
                await InsertarHomologacionAsync(
                    grupo.canonical, grupo.canonical, nextGrupoId,
                    grupo.variantes.First().metodo, 1.0m);
            }
            catch { /* Ya existe */ }

            nextGrupoId++;
        }

        var resultado = new ResultadoDeteccion
        {
            GruposDetectados = gruposNuevos.Count,
            NombresAfectados = insertados,
            GruposNuevos = gruposNuevos.Count,
            GruposExistentes = existentes.Select(e => e.GrupoId).Distinct().Count(),
            Mensaje = $"Se detectaron {gruposNuevos.Count} grupos nuevos con {insertados} nombres."
        };

        _logger.LogInformation("Detección completada: {Resultado}", resultado.Mensaje);
        return resultado;
    }

    /// <summary>
    /// Agrupa nombres por similitud usando normalización + Levenshtein
    /// </summary>
    private List<List<string>> ClusterizarNombres(List<string> nombres, decimal umbralSimilitud)
    {
        var clusters = new List<List<string>>();
        var usado = new HashSet<int>();

        for (int i = 0; i < nombres.Count; i++)
        {
            if (usado.Contains(i)) continue;

            var cluster = new List<string> { nombres[i] };
            var normI = NormalizarNombre(nombres[i]);

            for (int j = i + 1; j < nombres.Count; j++)
            {
                if (usado.Contains(j)) continue;

                var normJ = NormalizarNombre(nombres[j]);
                var sim = CalcularSimilitud(normI, normJ);

                if (sim >= umbralSimilitud)
                {
                    cluster.Add(nombres[j]);
                    usado.Add(j);
                }
            }

            usado.Add(i);
            clusters.Add(cluster);
        }

        return clusters;
    }

    #endregion

    #region CRUD de homologaciones

    private async Task<List<NombreClienteRaw>> ObtenerNombresDistintosAsync()
    {
        var sql = @"
            SELECT TOP 50000
                bn.NombreCliente,
                MAX(bn.Telefonos) AS Telefonos,
                MAX(bn.RFC) AS RFC,
                COUNT(*) AS CantidadNotas
            FROM BAJASNOTAS bn
            WHERE bn.NombreCliente IS NOT NULL
              AND LTRIM(RTRIM(bn.NombreCliente)) <> ''
            GROUP BY bn.NombreCliente
            ORDER BY bn.NombreCliente";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<NombreClienteRaw>(sql)).ToList();
    }

    private async Task<List<VarianteNombre>> ObtenerHomologacionesExistentesAsync()
    {
        var sql = @"SELECT TOP 50000 Id, NombreOriginal, NombreCanonical, GrupoId,
                           MetodoDeteccion, Confianza, Aprobado, Rechazado,
                           FechaCreacion, FechaAprobacion, AprobadoPor
                    FROM AML_Homologacion
                    ORDER BY GrupoId, NombreOriginal";
        using var conn = CreateConnection();
        return (await conn.QueryAsync<VarianteNombre>(sql)).ToList();
    }

    private async Task InsertarHomologacionAsync(
        string nombreOriginal, string nombreCanonical, int grupoId,
        string metodoDeteccion, decimal confianza)
    {
        var sql = @"INSERT INTO AML_Homologacion
                        (NombreOriginal, NombreCanonical, GrupoId, MetodoDeteccion, Confianza)
                    VALUES (@NombreOriginal, @NombreCanonical, @GrupoId, @MetodoDeteccion, @Confianza)";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            NombreOriginal = nombreOriginal,
            NombreCanonical = nombreCanonical,
            GrupoId = grupoId,
            MetodoDeteccion = metodoDeteccion,
            Confianza = confianza
        });
    }

    /// <summary>
    /// Obtiene los grupos de homologación pendientes de aprobación (paginados)
    /// </summary>
    public async Task<(List<GrupoHomologacion> grupos, int totalGrupos)> ObtenerGruposPendientesAsync(
        int pagina = 1, int pageSize = 20, string? buscar = null)
    {
        // Obtener los grupos pendientes (no aprobados ni rechazados)
        var sqlGrupos = @"
            SELECT TOP 500 h.GrupoId, h.NombreCanonical, h.MetodoDeteccion,
                   AVG(h.Confianza) AS ConfianzaPromedio,
                   COUNT(*) AS TotalVariantes
            FROM AML_Homologacion h
            WHERE h.Aprobado = 0 AND h.Rechazado = 0
              AND (@Buscar IS NULL OR h.NombreCanonical LIKE '%' + @Buscar + '%'
                   OR h.NombreOriginal LIKE '%' + @Buscar + '%')
            GROUP BY h.GrupoId, h.NombreCanonical, h.MetodoDeteccion
            HAVING COUNT(*) >= 2
            ORDER BY AVG(h.Confianza) DESC, h.NombreCanonical";

        using var conn = CreateConnection();
        var todosGrupos = (await conn.QueryAsync<dynamic>(sqlGrupos, new
        {
            Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar
        })).ToList();

        int totalGrupos = todosGrupos.Count;
        var gruposPagina = todosGrupos.Skip((pagina - 1) * pageSize).Take(pageSize).ToList();

        if (!gruposPagina.Any())
            return (new List<GrupoHomologacion>(), totalGrupos);

        var grupoIds = gruposPagina.Select(g => (int)g.GrupoId).ToList();

        // Obtener las variantes de estos grupos
        var sqlVariantes = @"
            SELECT TOP 500 h.Id, h.NombreOriginal, h.NombreCanonical, h.GrupoId,
                   h.MetodoDeteccion, h.Confianza, h.Aprobado, h.Rechazado,
                   h.FechaCreacion, h.FechaAprobacion, h.AprobadoPor,
                   ISNULL(cnt.CantidadNotas, 0) AS CantidadNotas
            FROM AML_Homologacion h
            LEFT JOIN (
                SELECT NombreCliente, COUNT(*) AS CantidadNotas
                FROM BAJASNOTAS
                WHERE NombreCliente IS NOT NULL
                GROUP BY NombreCliente
            ) cnt ON cnt.NombreCliente = h.NombreOriginal
            WHERE h.GrupoId IN @GrupoIds
            ORDER BY h.GrupoId, h.Confianza DESC";

        var variantes = (await conn.QueryAsync<VarianteNombre>(sqlVariantes, new { GrupoIds = grupoIds })).ToList();

        var resultado = gruposPagina.Select(g => new GrupoHomologacion
        {
            GrupoId = (int)g.GrupoId,
            NombreCanonical = (string)g.NombreCanonical,
            MetodoDeteccion = (string)g.MetodoDeteccion,
            ConfianzaPromedio = (decimal)g.ConfianzaPromedio,
            Aprobado = false,
            Variantes = variantes.Where(v => v.GrupoId == (int)g.GrupoId).ToList(),
            TotalRegistrosBajasNotas = variantes
                .Where(v => v.GrupoId == (int)g.GrupoId)
                .Sum(v => v.CantidadNotas)
        }).ToList();

        return (resultado, totalGrupos);
    }

    /// <summary>
    /// Obtiene los grupos ya aprobados
    /// </summary>
    public async Task<List<GrupoHomologacion>> ObtenerGruposAprobadosAsync(string? buscar = null)
    {
        var sql = @"
            SELECT TOP 200 h.GrupoId, h.NombreCanonical, h.MetodoDeteccion,
                   AVG(h.Confianza) AS ConfianzaPromedio
            FROM AML_Homologacion h
            WHERE h.Aprobado = 1
              AND (@Buscar IS NULL OR h.NombreCanonical LIKE '%' + @Buscar + '%'
                   OR h.NombreOriginal LIKE '%' + @Buscar + '%')
            GROUP BY h.GrupoId, h.NombreCanonical, h.MetodoDeteccion
            HAVING COUNT(*) >= 2
            ORDER BY h.NombreCanonical";

        using var conn = CreateConnection();
        var grupos = (await conn.QueryAsync<dynamic>(sql, new
        {
            Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar
        })).ToList();

        if (!grupos.Any()) return new List<GrupoHomologacion>();

        var grupoIds = grupos.Select(g => (int)g.GrupoId).ToList();

        var sqlVariantes = @"
            SELECT TOP 500 h.Id, h.NombreOriginal, h.NombreCanonical, h.GrupoId,
                   h.MetodoDeteccion, h.Confianza, h.Aprobado,
                   ISNULL(cnt.CantidadNotas, 0) AS CantidadNotas
            FROM AML_Homologacion h
            LEFT JOIN (
                SELECT NombreCliente, COUNT(*) AS CantidadNotas
                FROM BAJASNOTAS
                WHERE NombreCliente IS NOT NULL
                GROUP BY NombreCliente
            ) cnt ON cnt.NombreCliente = h.NombreOriginal
            WHERE h.GrupoId IN @GrupoIds
            ORDER BY h.GrupoId, h.Confianza DESC";

        var variantes = (await conn.QueryAsync<VarianteNombre>(sqlVariantes, new { GrupoIds = grupoIds })).ToList();

        return grupos.Select(g => new GrupoHomologacion
        {
            GrupoId = (int)g.GrupoId,
            NombreCanonical = (string)g.NombreCanonical,
            MetodoDeteccion = (string)g.MetodoDeteccion,
            ConfianzaPromedio = (decimal)g.ConfianzaPromedio,
            Aprobado = true,
            Variantes = variantes.Where(v => v.GrupoId == (int)g.GrupoId).ToList()
        }).ToList();
    }

    /// <summary>
    /// Aprueba un grupo completo de homologación
    /// </summary>
    public async Task AprobarGrupoAsync(int grupoId, string? nombreCanonical, string aprobadoPor)
    {
        var sql = @"
            UPDATE AML_Homologacion
            SET Aprobado = 1, FechaAprobacion = GETUTCDATE(), AprobadoPor = @AprobadoPor
            WHERE GrupoId = @GrupoId AND Rechazado = 0";

        using var conn = CreateConnection();

        // Si se proporcionó un nombre canónico diferente, actualizarlo primero
        if (!string.IsNullOrWhiteSpace(nombreCanonical))
        {
            await conn.ExecuteAsync(
                "UPDATE AML_Homologacion SET NombreCanonical = @NombreCanonical WHERE GrupoId = @GrupoId",
                new { NombreCanonical = nombreCanonical, GrupoId = grupoId });
        }

        await conn.ExecuteAsync(sql, new { GrupoId = grupoId, AprobadoPor = aprobadoPor });

        _logger.LogInformation("Grupo {GrupoId} aprobado por {AprobadoPor}", grupoId, aprobadoPor);
    }

    /// <summary>
    /// Rechaza un grupo completo de homologación
    /// </summary>
    public async Task RechazarGrupoAsync(int grupoId)
    {
        var sql = "UPDATE AML_Homologacion SET Rechazado = 1 WHERE GrupoId = @GrupoId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { GrupoId = grupoId });
        _logger.LogInformation("Grupo {GrupoId} rechazado", grupoId);
    }

    /// <summary>
    /// Obtiene estadísticas de homologación
    /// </summary>
    public async Task<HomologacionStats> ObtenerEstadisticasAsync()
    {
        var sql = @"
            SELECT TOP 1
                (SELECT COUNT(DISTINCT GrupoId) FROM AML_Homologacion WHERE Rechazado = 0) AS TotalGrupos,
                (SELECT COUNT(DISTINCT GrupoId) FROM AML_Homologacion WHERE Aprobado = 1) AS GruposAprobados,
                (SELECT COUNT(DISTINCT GrupoId) FROM AML_Homologacion WHERE Aprobado = 0 AND Rechazado = 0) AS GruposPendientes,
                (SELECT COUNT(*) FROM AML_Homologacion WHERE Aprobado = 1 AND NombreOriginal <> NombreCanonical) AS NombresHomologados,
                (SELECT COUNT(*) FROM AML_Homologacion WHERE Aprobado = 0 AND Rechazado = 0) AS NombresPendientes,
                (SELECT COUNT(DISTINCT NombreCliente) FROM BAJASNOTAS WHERE NombreCliente IS NOT NULL AND LTRIM(RTRIM(NombreCliente)) <> '') AS NombresDistintosOriginal";

        using var conn = CreateConnection();
        var stats = await conn.QueryFirstOrDefaultAsync<HomologacionStats>(sql) ?? new HomologacionStats();

        // Calcular nombres post-homologación
        var sqlPost = @"
            SELECT TOP 1 COUNT(DISTINCT COALESCE(h.NombreCanonical, bn.NombreCliente)) AS Cnt
            FROM (
                SELECT DISTINCT NombreCliente
                FROM BAJASNOTAS
                WHERE NombreCliente IS NOT NULL AND LTRIM(RTRIM(NombreCliente)) <> ''
            ) bn
            LEFT JOIN AML_Homologacion h ON h.NombreOriginal = bn.NombreCliente AND h.Aprobado = 1";

        stats.NombresDistintosPostHomologacion = await conn.ExecuteScalarAsync<int>(sqlPost);

        return stats;
    }

    /// <summary>
    /// Limpia todas las sugerencias rechazadas (para re-detección)
    /// </summary>
    public async Task LimpiarRechazadosAsync()
    {
        var sql = "DELETE FROM AML_Homologacion WHERE Rechazado = 1";
        using var conn = CreateConnection();
        var deleted = await conn.ExecuteAsync(sql);
        _logger.LogInformation("Se eliminaron {Count} registros rechazados", deleted);
    }

    /// <summary>
    /// Aprueba todos los grupos pendientes con confianza >= umbral
    /// </summary>
    public async Task AprobarTodosConConfianzaAsync(decimal umbralConfianza, string aprobadoPor)
    {
        var sql = @"
            UPDATE AML_Homologacion
            SET Aprobado = 1, FechaAprobacion = GETUTCDATE(), AprobadoPor = @AprobadoPor
            WHERE Aprobado = 0 AND Rechazado = 0 AND Confianza >= @Umbral";

        using var conn = CreateConnection();
        var updated = await conn.ExecuteAsync(sql, new { Umbral = umbralConfianza, AprobadoPor = aprobadoPor });
        _logger.LogInformation("Se aprobaron {Count} registros con confianza >= {Umbral}", updated, umbralConfianza);
    }

    #endregion
}
