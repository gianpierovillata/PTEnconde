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

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
