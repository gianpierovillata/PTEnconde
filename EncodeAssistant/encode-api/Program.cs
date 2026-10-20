using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Swagger: generación del documento OpenAPI (SwaggerGen) y UI interactiva.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Encode Assistant API",
        Version = "v1",
        Description = "Bandeja conversacional para operadores de Telegram: webhook de recepción, envío de mensajes y salud del servicio."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Documento OpenAPI nativo en /openapi/v1.json (solo Development).
    app.MapOpenApi();

    // Swagger UI en /swagger — también solo en Development.
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Encode Assistant API v1");
    });
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Encode Assistant API is running.").WithName("Root");

/*
 * Endpoint para verificar el estado de la API.
 * Puedes usar este endpoint para monitorear la salud de tu aplicación.
 */
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "Healthy" })).WithName("HealthCheck");

/*
 * Endpoint para enviar mensajes a Telegram.
 * PLACEHOLDER: la llamada real a la Bot API se implementa en la Fase 5.
 */
app.MapPost("/api/v1/webhook/sendMessage", (SendMessageRequest request) =>
{
    Console.WriteLine($"Enviando mensaje a chatId {request.ChatId}: {request.Text}");
    return Results.Ok(new { status = "Message sent" });
}).WithName("SendMessageToTelegram");



// Endpoint del Webhook para Telegram
app.MapPost("/api/telegram/webhook", async ([FromBody] JsonElement update, ILogger<Program> logger) =>
{
    try
    {
        // Telegram envía un objeto "Update". El primer mensaje suele ser cuando el usuario inicia el bot.
        if (update.TryGetProperty("message", out JsonElement message))
        {
            var chat = message.GetProperty("chat");
            var chatId = chat.GetProperty("id").GetInt64();          // id de la conversación (el chat)
            var chatType = chat.TryGetProperty("type", out JsonElement typeProp)
                ? typeProp.GetString()
                : "desconocido";

            // "from" puede no venir (canales o admins anónimos), por eso se lee con TryGetProperty.
            long? userId = null;
            string? username = null;
            string? firstName = null;
            string? lastName = null;

            if (message.TryGetProperty("from", out JsonElement from))
            {
                userId = from.GetProperty("id").GetInt64();
                username = from.TryGetProperty("username", out JsonElement u) ? u.GetString() : null;
                firstName = from.TryGetProperty("first_name", out JsonElement f) ? f.GetString() : null;
                lastName = from.TryGetProperty("last_name", out JsonElement l) ? l.GetString() : null;
            }

            // Nombre para mostrar: @usuario, si no, nombre completo, si no, el id.
            var emisor = username is not null ? $"@{username}"
                : firstName is not null ? $"{firstName} {lastName}".Trim()
                : userId is not null ? $"usuario {userId}"
                : "emisor desconocido";

            if (message.TryGetProperty("text", out JsonElement textProperty))
            {
                var text = textProperty.GetString();
                logger.LogInformation(
                    "Mensaje recibido | Conversación (chatId): {ChatId} ({ChatType}) | Emisor: {Emisor} (userId: {UserId}, nombre: {Nombre}) | Texto: {Texto}",
                    chatId, chatType, emisor, userId, $"{firstName} {lastName}".Trim(), text);

                // Si el usuario se conecta por primera vez, enviará el comando "/start"
                if (text == "/start")
                {
                    logger.LogInformation("El usuario {Emisor} ha iniciado el bot con /start", emisor);
                    // Aquí podrías enviar un mensaje de bienvenida o registrar al usuario en tu sistema.
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al procesar el webhook de Telegram");
    }

    // Telegram exige recibir un HTTP 200 OK inmediato para no reintentar el envío
    return Results.Ok();
});



app.Run();

/// <summary>
/// Petición de envío de mensaje desde la bandeja (placeholder, se reemplaza en la Fase 5).
/// </summary>
public record SendMessageRequest(string ChatId, string Text);
