namespace EncodeAssistant.EncodeApi.Domain.Entities
{
    /// <summary>
    /// Contacto de la bandeja: el usuario de Telegram que escribió al bot.
    /// El Id es el chatId de Telegram (clave primaria como string).
    /// </summary>
    public class Contacto
    {
        public string Id { get; set; } = string.Empty;

        public string? Nombre { get; set; }

        public string? Username { get; set; }

        /// <summary>Vista previa del último mensaje (columna del sidebar).</summary>
        public string? UltimoTexto { get; set; }

        public DateTime? UltimaFecha { get; set; }

        /// <summary>Mensajes entrantes aún no leídos; se pone en 0 al abrir la conversación.</summary>
        public int NoLeidos { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

        public List<Mensaje> Mensajes { get; set; } = new();
    }
}
