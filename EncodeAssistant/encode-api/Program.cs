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
 * Endpoint para recibir mensajes de Telegram a través del webhook.
 * PLACEHOLDER: por ahora solo registra el payload. La validación del secret
 * (X-Telegram-Bot-Api-Secret-Token), la publicación en RabbitMQ y la persistencia
 * se implementan en las fases 3 y 5.
 */
app.MapPost("/api/v1/webhook/receiveMessage", (TelegramWebhookPayload payload) =>
{
    Console.WriteLine($"Mensaje recibido del chat {payload.ChatId}: {payload.Text}");
    return Results.Ok(new { status = "Message received" });
}).WithName("TelegramWebhook");

/*
 * Endpoint para enviar mensajes a Telegram.
 * PLACEHOLDER: la llamada real a la Bot API se implementa en la Fase 5.
 */
app.MapPost("/api/v1/webhook/sendMessage", (SendMessageRequest request) =>
{
    Console.WriteLine($"Enviando mensaje a chatId {request.ChatId}: {request.Text}");
    return Results.Ok(new { status = "Message sent" });
}).WithName("SendMessageToTelegram");

app.Run();

/// <summary>
/// Payload mínimo del webhook de Telegram (placeholder, se reemplaza en la Fase 5).
/// </summary>
public record TelegramWebhookPayload(string ChatId, string Text);

/// <summary>
/// Petición de envío de mensaje desde la bandeja (placeholder, se reemplaza en la Fase 5).
/// </summary>
public record SendMessageRequest(string ChatId, string Text);
