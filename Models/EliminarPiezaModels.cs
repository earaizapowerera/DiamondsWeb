namespace DiamondsWeb.Models;

/// <summary>
/// Resultado de verificar si un usuario puede eliminar una pieza.
/// Replica la logica de EliminarPieza() en frmSencillas.frm (VB6).
/// Reglas:
///   1) Si la etiqueta fue impresa → requiere autorizacion de supervisor
///   2) Si han pasado menos de 2 horas desde FechaCaptura → puede eliminar sin permiso
///   3) Si han pasado 2+ horas → requiere autorizacion de supervisor
///   4) Supervisores (PermisoUsuarios=1) pueden eliminar siempre
///   5) Si existe pre-autorizacion en permisocancelar → puede eliminar
/// </summary>
public class PermisoEliminarResult
{
    public bool PuedeEliminar { get; set; }
    public bool RequiereAutorizacion { get; set; }
    public string? MotivoRequerimiento { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public bool EtiquetaImpresa { get; set; }
    public bool DentroDeVentana { get; set; }
    public bool EsSupervisor { get; set; }
    public bool PreAutorizado { get; set; }
}

/// <summary>
/// Request para eliminar una pieza con validacion de permisos.
/// Si RequiereAutorizacion, debe incluir credenciales de supervisor.
/// </summary>
public class EliminarPiezaRequest
{
    public string CodigoBarras { get; set; } = "";
    public string? Motivo { get; set; }
    public string? SupervisorNombre { get; set; }
    public string? SupervisorPassword { get; set; }
}

/// <summary>
/// Resultado de la operacion de eliminacion.
/// </summary>
public class EliminarPiezaResult
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
    public string? Error { get; set; }
}
