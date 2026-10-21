using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EncodeAssistant.EncodeApi.Aplication.Services
{
    /// <summary>
    /// Resultado del envío de un mensaje a la Bot API de Telegram.
    /// </summary>
    public record TelegramEnvioResultado(bool Ok, long? MessageId, string? Error, int? ErrorCode)
    {
        public static TelegramEnvioResultado Exito(long? messageId) => new(true, messageId, null, null);

        public static TelegramEnvioResultado Fallo(string error, int? errorCode = null) => new(false, null, error, errorCode);
    }

    /// <summary>
    /// Cliente mínimo de la Bot API de Telegram para enviar mensajes desde la bandeja.
    /// Solo cubre lo que usa la prueba técnica: <c>sendMessage</c>.
    /// </summary>
    public interface ITelegramBotClient
    {
        /// <summary>
        /// Envía un mensaje de texto al chat indicado.
        /// Nunca lanza por errores de Telegram (token inválido, bot bloqueado, chat inexistente):
        /// devuelve un resultado con Ok=false y el motivo, para que el llamador decida qué hacer.
        /// </summary>
        Task<TelegramEnvioResultado> EnviarMensajeAsync(string chatId, string texto, CancellationToken ct = default);
    }

    /// <summary>
    /// Implementación sobre <see cref="HttpClient"/> (registrado con BaseAddress = TELEGRAM_API_BASE_URL).
    /// </summary>
    public class TelegramBotClient : ITelegramBotClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<TelegramBotClient> _logger;
        private readonly string _token;

        public TelegramBotClient(HttpClient http, IConfiguration configuracion, ILogger<TelegramBotClient> logger)
        {
            _http = http;
            _logger = logger;
            _token = configuracion["TELEGRAM_BOT_TOKEN"]?.Trim() ?? string.Empty;

            // La URL base viene de TELEGRAM_API_BASE_URL (por defecto, https://api.telegram.org).
            // Si no está configurada, se asume la oficial.
            var baseUrl = configuracion["TELEGRAM_API_BASE_URL"]?.Trim();
            _http.BaseAddress = new Uri(string.IsNullOrWhiteSpace(baseUrl) ? "https://api.telegram.org" : baseUrl);

            // Telegram puede tardar; no dejamos que un cuelgue bloquee la respuesta al operador.
            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        /// <summary>true si hay token configurado (sin él no se puede enviar nada).</summary>
        public bool TieneToken => !string.IsNullOrWhiteSpace(_token);

        public async Task<TelegramEnvioResultado> EnviarMensajeAsync(string chatId, string texto, CancellationToken ct = default)
        {
            if (!TieneToken)
            {
                const string motivo = "Falta TELEGRAM_BOT_TOKEN en la configuración de la API.";
                _logger.LogError("{Motivo} No se puede enviar el mensaje a Telegram.", motivo);
                return TelegramEnvioResultado.Fallo(motivo);
            }

            var url = $"/bot{_token}/sendMessage";

            try
            {
                var respuesta = await _http.PostAsJsonAsync(
                    url,
                    new { chat_id = chatId, text = texto },
                    SerializacionTelegram.Opciones,
                    ct);

                var cuerpo = await respuesta.Content.ReadFromJsonAsync<TelegramSendMessageResponse>(
                    SerializacionTelegram.Opciones, ct);

                if (respuesta.IsSuccessStatusCode && cuerpo?.Ok == true)
                {
                    return TelegramEnvioResultado.Exito(cuerpo.Result?.MessageId);
                }

                // Telegram responde 200 con ok=false, o un 4xx con el detalle en "description".
                var motivo = cuerpo?.Description
                    ?? $"Telegram respondió {(int)respuesta.StatusCode} {respuesta.ReasonPhrase}.";

                _logger.LogWarning("Telegram rechazó el mensaje para el chat {ChatId} ({Code}): {Motivo}",
                    chatId, cuerpo?.ErrorCode ?? (int)respuesta.StatusCode, motivo);

                return TelegramEnvioResultado.Fallo(motivo, cuerpo?.ErrorCode ?? (int)respuesta.StatusCode);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                const string motivo = "Telegram no respondió en el tiempo esperado.";
                _logger.LogWarning("{Motivo} Chat {ChatId}.", motivo, chatId);
                return TelegramEnvioResultado.Fallo(motivo);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "No se pudo llegar a la Bot API de Telegram (chat {ChatId}).", chatId);
                return TelegramEnvioResultado.Fallo($"No se pudo llegar a Telegram: {ex.Message}");
            }
        }
    }

    /// <summary>Opciones de serialización compartidas con la Bot API.</summary>
    internal static class SerializacionTelegram
    {
        public static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);
    }

    /// <summary>Respuesta de <c>sendMessage</c> (solo los campos que usamos).</summary>
    internal record TelegramSendMessageResponse(
        bool Ok,
        [property: JsonPropertyName("error_code")] int? ErrorCode,
        string? Description,
        TelegramSentMessage? Result);

    internal record TelegramSentMessage(
        [property: JsonPropertyName("message_id")] long MessageId);
}
