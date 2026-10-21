namespace EncodeAssistant.EncodeApi.Domain.Entities
{
    /// <summary>
    /// Mensaje dentro de una conversación de la bandeja.
    /// Dirección: "incoming" (del usuario de Telegram) u "outgoing" (respuesta del operador).
    /// </summary>
    public class Mensaje
    {
        public const string DireccionEntrante = "incoming";
        public const string DireccionSaliente = "outgoing";

        public long Id { get; set; }

        public string ContactoId { get; set; } = string.Empty;

        public Contacto? Contacto { get; set; }

        public string Texto { get; set; } = string.Empty;

        public string Direccion { get; set; } = DireccionEntrante;

        /// <summary>true cuando la conversación fue abierta por el operador (marca de leído).</summary>
        public bool Leido { get; set; }

        /// <summary>
        /// Estado de entrega hacia Telegram, solo para mensajes salientes:
        /// null = no aplica (entrante), false = no se pudo entregar, true = entregado.
        /// </summary>
        public bool? Entregado { get; set; }

        /// <summary>Motivo del fallo de entrega (para diagnóstico y para mostrarlo en la bandeja).</summary>
        public string? ErrorEntrega { get; set; }

        /// <summary>message_id que devolvió Telegram al aceptar el mensaje.</summary>
        public long? TelegramMessageId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
