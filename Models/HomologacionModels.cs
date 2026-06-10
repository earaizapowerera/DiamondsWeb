namespace DiamondsWeb.Models;

/// <summary>
/// Grupo de nombres que se detectaron como la misma persona.
/// Cada grupo tiene un nombre canónico y N variantes.
/// </summary>
public class GrupoHomologacion
{
    public int GrupoId { get; set; }
    public string NombreCanonical { get; set; } = string.Empty;
    public List<VarianteNombre> Variantes { get; set; } = new();
    public string MetodoDeteccion { get; set; } = string.Empty;
    public decimal ConfianzaPromedio { get; set; }
    public bool Aprobado { get; set; }
    public int TotalRegistrosBajasNotas { get; set; }
}

public class VarianteNombre
{
    public int Id { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreCanonical { get; set; } = string.Empty;
    public int GrupoId { get; set; }
    public string MetodoDeteccion { get; set; } = string.Empty;
    public decimal Confianza { get; set; }
    public bool Aprobado { get; set; }
    public bool Rechazado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? AprobadoPor { get; set; }
    public int CantidadNotas { get; set; }
}

/// <summary>
/// Nombre extraído de BAJASNOTAS con su teléfono para matching
/// </summary>
public class NombreClienteRaw
{
    public string NombreCliente { get; set; } = string.Empty;
    public string? Telefonos { get; set; }
    public string? RFC { get; set; }
    public int CantidadNotas { get; set; }
}

/// <summary>
/// Resultado de la detección de duplicados
/// </summary>
public class ResultadoDeteccion
{
    public int GruposDetectados { get; set; }
    public int NombresAfectados { get; set; }
    public int GruposNuevos { get; set; }
    public int GruposExistentes { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>
/// Estadísticas de homologación
/// </summary>
public class HomologacionStats
{
    public int TotalGrupos { get; set; }
    public int GruposAprobados { get; set; }
    public int GruposPendientes { get; set; }
    public int NombresHomologados { get; set; }
    public int NombresPendientes { get; set; }
    public int NombresDistintosOriginal { get; set; }
    public int NombresDistintosPostHomologacion { get; set; }
}
