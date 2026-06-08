using System.Text;
using System.Xml;
using DiamondsWeb.Models;

namespace DiamondsWeb.Services;

/// <summary>
/// Genera archivos XML compatibles con SPPLD (Portal de Prevención de Lavado de Dinero del SAT)
/// Formato: Anexo 6 - Comercialización de joyas, metales y piedras preciosas
/// Basado en la Resolución DOF 30/08/2013 con reformas al 24/05/2021
/// </summary>
public class SppldXmlService
{
    /// <summary>
    /// Genera el XML del aviso para un mes/año dado con los clientes que superan el umbral
    /// </summary>
    public byte[] GenerarXmlAviso(
        SppldConfig config,
        int mes, int anio,
        List<ClienteAmlResumen> clientes,
        Dictionary<string, List<NotaDetalle>> operacionesPorCliente)
    {
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false), // sin BOM
            OmitXmlDeclaration = false
        };

        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("archivo");

            writer.WriteStartElement("informe");
            writer.WriteElementString("mes_reportado", $"{anio}-{mes:D2}");

            // Sujeto obligado
            writer.WriteStartElement("sujeto_obligado");
            writer.WriteElementString("clave_sujeto_obligado", config.ClaveSujetoObligado);
            writer.WriteElementString("clave_actividad", "VI"); // Fracción VI: joyas, metales, piedras preciosas
            writer.WriteEndElement(); // sujeto_obligado

            int avisoNum = 1;
            foreach (var cliente in clientes)
            {
                var operaciones = operacionesPorCliente.GetValueOrDefault(cliente.NombreCliente)
                                  ?? new List<NotaDetalle>();

                EscribirAviso(writer, config, cliente, operaciones, mes, anio, avisoNum++);
            }

            writer.WriteEndElement(); // informe
            writer.WriteEndElement(); // archivo
            writer.WriteEndDocument();
        }

        return ms.ToArray();
    }

    private void EscribirAviso(
        XmlWriter writer, SppldConfig config,
        ClienteAmlResumen cliente, List<NotaDetalle> operaciones,
        int mes, int anio, int numAviso)
    {
        writer.WriteStartElement("aviso");

        writer.WriteElementString("referencia_aviso",
            $"DW-{anio}{mes:D2}-{numAviso:D4}");
        writer.WriteElementString("prioridad", "Normal");

        // Alerta
        writer.WriteStartElement("alerta");
        writer.WriteElementString("tipo_alerta",
            cliente.RequiereAvisoSAT ? "Operacion relevante" : "Operacion inusual");
        writer.WriteEndElement(); // alerta

        // Persona del aviso (cliente)
        EscribirPersona(writer, "persona_aviso", cliente);

        // Dueño beneficiario (mismo que persona para persona física)
        EscribirPersona(writer, "dueno_beneficiario", cliente);

        // Detalle de operaciones
        writer.WriteStartElement("detalle_operaciones");

        foreach (var op in operaciones)
        {
            writer.WriteStartElement("datos_operacion");
            writer.WriteElementString("fecha_operacion", op.FechaBaja.ToString("yyyy-MM-dd"));
            writer.WriteElementString("codigo_postal", config.CodigoPostalSucursal);
            writer.WriteElementString("nombre_sucursal", config.NombreSucursal);
            writer.WriteElementString("tipo_operacion", "Comercializacion");

            // Liquidación
            writer.WriteStartElement("datos_liquidacion");
            writer.WriteElementString("fecha_pago", op.FechaBaja.ToString("yyyy-MM-dd"));
            writer.WriteElementString("forma_pago", MapearFormaPago(op.FormaPago));
            writer.WriteElementString("instrumento_monetario",
                MapearInstrumentoMonetario(op.FormaPago));
            writer.WriteElementString("moneda", "MXN");
            writer.WriteElementString("monto_operacion", op.Total.ToString("F2"));
            writer.WriteEndElement(); // datos_liquidacion

            writer.WriteEndElement(); // datos_operacion
        }

        writer.WriteEndElement(); // detalle_operaciones
        writer.WriteEndElement(); // aviso
    }

    private void EscribirPersona(XmlWriter writer, string elementName, ClienteAmlResumen cliente)
    {
        writer.WriteStartElement(elementName);
        writer.WriteElementString("tipo_persona", "fisica");

        // Separar nombre en partes
        var partes = (cliente.NombreCliente ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length >= 3)
        {
            // Asume: nombre(s) apellido_paterno apellido_materno
            writer.WriteElementString("nombre", string.Join(" ", partes.Take(partes.Length - 2)));
            writer.WriteElementString("apellido_paterno", partes[^2]);
            writer.WriteElementString("apellido_materno", partes[^1]);
        }
        else if (partes.Length == 2)
        {
            writer.WriteElementString("nombre", partes[0]);
            writer.WriteElementString("apellido_paterno", partes[1]);
            writer.WriteElementString("apellido_materno", "");
        }
        else
        {
            writer.WriteElementString("nombre", cliente.NombreCliente ?? "");
            writer.WriteElementString("apellido_paterno", "");
            writer.WriteElementString("apellido_materno", "");
        }

        writer.WriteElementString("rfc", cliente.RFC ?? "");
        writer.WriteElementString("curp", ""); // No disponible en DB

        // Domicilio
        writer.WriteStartElement("tipo_domicilio");
        writer.WriteStartElement("nacional");
        writer.WriteElementString("calle", "");
        writer.WriteElementString("numero_exterior", "");
        writer.WriteElementString("colonia", "");
        writer.WriteElementString("codigo_postal", "");
        writer.WriteElementString("municipio", "");
        writer.WriteElementString("entidad_federativa", "");
        writer.WriteEndElement(); // nacional
        writer.WriteEndElement(); // tipo_domicilio

        // Teléfono
        writer.WriteStartElement("telefono");
        writer.WriteElementString("clave_pais", "52");
        writer.WriteElementString("numero_telefono", cliente.Telefonos ?? "");
        writer.WriteEndElement(); // telefono

        writer.WriteEndElement(); // persona_aviso / dueno_beneficiario
    }

    private static string MapearFormaPago(string? formaPago)
    {
        if (string.IsNullOrWhiteSpace(formaPago)) return "Otros";
        var fp = formaPago.ToLower().Trim();
        if (fp.Contains("efectivo") || fp.Contains("pesos")) return "Efectivo";
        if (fp.Contains("transfer")) return "Transferencia bancaria";
        if (fp.Contains("visa") || fp.Contains("master") || fp.Contains("tarjeta") || fp.Contains("credit"))
            return "Tarjeta de credito";
        if (fp.Contains("debito") || fp.Contains("debit")) return "Tarjeta de debito";
        if (fp.Contains("cheque")) return "Cheque";
        return "Otros";
    }

    private static string MapearInstrumentoMonetario(string? formaPago)
    {
        if (string.IsNullOrWhiteSpace(formaPago)) return "No aplica";
        var fp = formaPago.ToLower().Trim();
        if (fp.Contains("efectivo") || fp.Contains("pesos")) return "Moneda nacional";
        if (fp.Contains("transfer")) return "Transferencia electronica";
        if (fp.Contains("visa") || fp.Contains("master") || fp.Contains("tarjeta"))
            return "Tarjeta bancaria";
        if (fp.Contains("cheque")) return "Cheque";
        return "No aplica";
    }
}

/// <summary>
/// Configuración del sujeto obligado para el SPPLD
/// </summary>
public class SppldConfig
{
    public string ClaveSujetoObligado { get; set; } = "";
    public string NombreSucursal { get; set; } = "Diamonds";
    public string CodigoPostalSucursal { get; set; } = "";
}
