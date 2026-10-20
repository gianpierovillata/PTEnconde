using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Swagger: generación del documento OpenAPI (SwaggerGen) y UI interactiva (SwaggerUI).
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
            var chatId = message.GetProperty("chat").GetProperty("id").GetInt64();
            
            if (message.TryGetProperty("text", out JsonElement textProperty))
            {
                var text = textProperty.GetString();
                logger.LogInformation($"Mensaje recibido de ChatID {chatId}: {text}");

                // Si el usuario se conecta por primera vez, enviará el comando "/start"
                if (text == "/start")
                {
                    // Aquí ejecutas la lógica de bienvenida o registro en tu sistema
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al procesar el webhook de Telegram");
    }

    // Telegram exige recibir un HTTP 200 OK inmediatamente para no reintentar el envío
    return Results.Ok();
});



app.Run();

/// <summary>
/// Petición de envío de mensaje desde la bandeja (placeholder, se reemplaza en la Fase 5).
/// </summary>
public record SendMessageRequest(string ChatId, string Text);
