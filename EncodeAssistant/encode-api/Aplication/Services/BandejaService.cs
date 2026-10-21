using EncodeAssistant.EncodeApi.Domain.Entities;
using EncodeAssistant.EncodeApi.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EncodeAssistant.EncodeApi.Aplication.Services
{
    /// <summary>
    /// DTO de salida para la lista de contactos del sidebar.
    /// Nombres en inglés para coincidir con el modelo Contact del frontend.
    /// </summary>
    public record ContactDto(
        string Id,
        string? Name,
        string? Username,
        string? LastMessage,
        DateTime? LastMessageAt,
        int UnreadCount,
        bool IsActive);

    /// <summary>
    /// DTO de salida para los mensajes de una conversación.
    /// Nombres en inglés para coincidir con el modelo Message del frontend.
    /// </summary>
    public record MessageDto(
        long Id,
        string ContactId,
        string Text,
        string Direction,
        DateTime CreatedAt,
        bool Read);

    /// <summary>
    /// Casos de uso de la bandeja: contactos y mensajes, con persistencia en SQL Server.
    /// </summary>
    public class BandejaService
    {
        private readonly EncodeDbContext _db;

        public BandejaService(EncodeDbContext db)
        {
            _db = db;
        }

        /// <summary>Lista de contactos ordenada por último mensaje (más reciente arriba).</summary>
        public async Task<List<ContactDto>> ListarContactosAsync(CancellationToken ct = default)
        {
            var contactos = await _db.Contactos.AsNoTracking()
                .Where(c => c.Activo)
                .OrderByDescending(c => c.UltimaFecha)
                .ToListAsync(ct);

            return contactos.Select(AContactoDto).ToList();
        }

        /// <summary>Historial completo de una conversación, en orden cronológico.</summary>
        public async Task<List<MessageDto>> ListarMensajesAsync(string contactoId, CancellationToken ct = default)
        {
            var mensajes = await _db.Mensajes.AsNoTracking()
                .Where(m => m.ContactoId == contactoId)
                .OrderBy(m => m.FechaCreacion)
                .ToListAsync(ct);

            return mensajes.Select(AMensajeDto).ToList();
        }

        /// <summary>
        /// Registra un mensaje entrante del usuario de Telegram: crea/actualiza el contacto
        /// (nombre, username, vista previa) y suma +1 a los no leídos.
        /// </summary>
        public async Task<(ContactDto Contacto, MessageDto Mensaje)> GuardarEntranteAsync(
            string chatId, string texto, string? nombre, string? username, CancellationToken ct = default)
        {
            var ahora = DateTime.UtcNow;

            var contacto = await _db.Contactos.FirstOrDefaultAsync(c => c.Id == chatId, ct);
            if (contacto is null)
            {
                contacto = new Contacto { Id = chatId, CreadoEn = ahora };
                _db.Contactos.Add(contacto);
            }

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                contacto.Nombre = nombre;
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                contacto.Username = username;
            }

            contacto.UltimoTexto = texto.Length > 200 ? texto[..200] : texto;
            contacto.UltimaFecha = ahora;
            contacto.NoLeidos += 1;

            var mensaje = new Mensaje
            {
                ContactoId = chatId,
                Texto = texto,
                Direccion = Mensaje.DireccionEntrante,
                Leido = false,
                FechaCreacion = ahora
            };
            _db.Mensajes.Add(mensaje);

            await _db.SaveChangesAsync(ct);

            return (AContactoDto(contacto), AMensajeDto(mensaje));
        }

        /// <summary>
        /// Registra un mensaje saliente del operador. No requiere que el contacto exista
        /// previamente (también se usa desde el endpoint de respuesta directa).
        /// </summary>
        public async Task<(ContactDto Contacto, MessageDto Mensaje)> GuardarSalienteAsync(
            string chatId, string texto, CancellationToken ct = default)
        {
            var ahora = DateTime.UtcNow;

            var contacto = await _db.Contactos.FirstOrDefaultAsync(c => c.Id == chatId, ct);
            if (contacto is null)
            {
                contacto = new Contacto { Id = chatId, CreadoEn = ahora };
                _db.Contactos.Add(contacto);
            }

            contacto.UltimoTexto = texto.Length > 200 ? texto[..200] : texto;
            contacto.UltimaFecha = ahora;

            var mensaje = new Mensaje
            {
                ContactoId = chatId,
                Texto = texto,
                Direccion = Mensaje.DireccionSaliente,
                Leido = true,
                FechaCreacion = ahora
            };
            _db.Mensajes.Add(mensaje);

            await _db.SaveChangesAsync(ct);

            return (AContactoDto(contacto), AMensajeDto(mensaje));
        }

        /// <summary>
        /// Marca la conversación como leída (no leídos en 0 y mensajes entrantes en leído).
        /// Devuelve el contacto actualizado, o null si no existe.
        /// </summary>
        public async Task<ContactDto?> MarcarLeidosAsync(string contactoId, CancellationToken ct = default)
        {
            var contacto = await _db.Contactos.FirstOrDefaultAsync(c => c.Id == contactoId, ct);
            if (contacto is null)
            {
                return null;
            }

            contacto.NoLeidos = 0;

            var pendientes = await _db.Mensajes
                .Where(m => m.ContactoId == contactoId && !m.Leido)
                .ToListAsync(ct);

            foreach (var mensaje in pendientes)
            {
                mensaje.Leido = true;
            }

            await _db.SaveChangesAsync(ct);

            return AContactoDto(contacto);
        }

        /// <summary>
        /// Elimina una conversación completa (el contacto y todos sus mensajes).
        /// Devuelve false si el contacto no existe.
        /// </summary>
        public async Task<bool> EliminarConversacionAsync(string contactoId, CancellationToken ct = default)
        {
            var contacto = await _db.Contactos.FirstOrDefaultAsync(c => c.Id == contactoId, ct);
            if (contacto is null)
            {
                return false;
            }

            // Los mensajes se borran en cascada por la FK configurada en el DbContext.
            _db.Contactos.Remove(contacto);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// Vacía la bandeja: elimina todos los contactos y sus mensajes.
        /// Devuelve cuántas conversaciones se borraron.
        /// </summary>
        public async Task<int> EliminarTodasLasConversacionesAsync(CancellationToken ct = default)
        {
            var contactos = await _db.Contactos.ToListAsync(ct);
            if (contactos.Count == 0)
            {
                return 0;
            }

            _db.Contactos.RemoveRange(contactos);
            await _db.SaveChangesAsync(ct);
            return contactos.Count;
        }

        private static ContactDto AContactoDto(Contacto c) => new(
            c.Id, c.Nombre, c.Username, c.UltimoTexto, EnUtc(c.UltimaFecha), c.NoLeidos, c.Activo);

        private static MessageDto AMensajeDto(Mensaje m) => new(
            m.Id, m.ContactoId, m.Texto, m.Direccion, EnUtc(m.FechaCreacion), m.Leido);

        /// <summary>
        /// SQL Server devuelve DateTime sin Kind; se fuerza UTC para que el JSON
        /// salga con sufijo "Z" y Angular lo muestre en la hora local correcta.
        /// </summary>
        private static DateTime EnUtc(DateTime fecha) =>
            DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

        private static DateTime? EnUtc(DateTime? fecha) =>
            fecha.HasValue ? DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc) : null;
    }
}
