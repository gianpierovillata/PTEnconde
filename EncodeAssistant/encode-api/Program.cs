// =============================================================================
// encode-api — API mínima .NET 10 de la bandeja conversacional (encodelabs)
// Flujo: webhook de Telegram -> SQL Server -> SignalR -> Angular en tiempo real
// =============================================================================

using System.Text.Json.Serialization;
using EncodeAssistant.EncodeApi.Aplication.Services;
using EncodeAssistant.EncodeApi.Hubs;
using EncodeAssistant.EncodeApi.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- OpenAPI / Swagger --------------------------------------------------------
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Encode Assistant API",
        Version = "v1",
        Description = "Bandeja conversacional para operadores de Telegram: webhook de recepción, contactos, mensajes y tiempo real por SignalR."
    });
});

// --- SignalR (tiempo real hacia la bandeja Angular) ---------------------------
builder.Services.AddSignalR();

// --- CORS: solo orígenes permitidos (env CORS_ALLOWED_ORIGINS, coma-separada) --
var orígenesPermitidos = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(orígenesPermitidos.Length > 0 ? orígenesPermitidos : ["http://localhost:4200"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()); // necesario para el negotiate de SignalR
});

// --- Persistencia: SQL Server vía EF Core -------------------------------------
var cadenaSql = builder.Configuration.GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException(
        "Falta ConnectionStrings__SqlServer. Copiá .env.example a .env (ver README) y levantá con docker compose.");

builder.Services.AddDbContext<EncodeDbContext>(options => options.UseSqlServer(cadenaSql));
builder.Services.AddScoped<BandejaService>();

// --- Cliente de la Bot API de Telegram (envío de las respuestas del operador) --
// La URL base sale de TELEGRAM_API_BASE_URL y el token de TELEGRAM_BOT_TOKEN.
builder.Services.AddHttpClient<ITelegramBotClient, TelegramBotClient>();

var app = builder.Build();

// --- Esquema de la base (EnsureCreated: crea la base del ConnectionString) ----
// Reintenta ante el arranque en frío de SQL Server dentro del compose.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EncodeDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (var intento = 1; ; intento++)
    {
        try
        {
            var creado = await db.Database.EnsureCreatedAsync();
            log.LogInformation(creado
                ? "Base de datos creada con el esquema inicial (SQL Server)."
                : "Base de datos ya existente; esquema verificado.");
            break;
        }
        catch (Exception ex) when (intento < 10)
        {
            log.LogWarning("SQL Server no disponible (intento {Intento}/10): {Motivo}. Reintento en 3 s…",
                intento, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

// --- Pipeline -----------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Encode Assistant API v1");
    });
}

app.UseCors("Frontend");

app.MapGet("/", () => "Encode Assistant API is running.").WithName("Root");

// Salud simple para el smoke test (local o vía ngrok).
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "Healthy" })).WithName("HealthCheck");

// -----------------------------------------------------------------------------
// Bandeja: contactos y mensajes (los consume el frontend Angular)
// -----------------------------------------------------------------------------

// Lista de contactos del sidebar, ordenada por último mensaje.
app.MapGet("/api/v1/contacts", async (BandejaService bandeja, CancellationToken ct) =>
{
    var contactos = await bandeja.ListarContactosAsync(ct);
    return Results.Ok(contactos);
});

// Vacía la bandeja completa: borra todos los contactos y sus mensajes.
// El frontend emite "ConversacionEliminada" (con chatId vacío) para que las otras
// pestañas limpien la vista.
app.MapDelete("/api/v1/contacts", async (
    BandejaService bandeja,
    IHubContext<InboxHub> hub,
    CancellationToken ct) =>
{
    var eliminadas = await bandeja.EliminarTodasLasConversacionesAsync(ct);

    await hub.Clients.All.SendAsync("ConversacionEliminada", string.Empty, ct);
    return Results.Ok(new { eliminadas });
});

// Elimina una conversación completa (contacto + mensajes).
app.MapDelete("/api/v1/contacts/{chatId}", async (
    string chatId,
    BandejaService bandeja,
    IHubContext<InboxHub> hub,
    CancellationToken ct) =>
{
    var eliminado = await bandeja.EliminarConversacionAsync(chatId, ct);
    if (!eliminado)
    {
        return Results.NotFound(new { error = $"No existe el contacto '{chatId}'." });
    }

    await hub.Clients.All.SendAsync("ConversacionEliminada", chatId, ct);
    return Results.NoContent();
});

// Historial de una conversación, en orden cronológico.
app.MapGet("/api/v1/contacts/{chatId}/messages", async (string chatId, BandejaService bandeja, CancellationToken ct) =>
{
    var mensajes = await bandeja.ListarMensajesAsync(chatId, ct);
    return Results.Ok(mensajes);
});

// Respuesta del operador: se persiste, se envía al usuario por Telegram y se emite por SignalR.
app.MapPost("/api/v1/contacts/{chatId}/messages", async (
    string chatId,
    [FromBody] RespuestaOperador body,
    BandejaService bandeja,
    ITelegramBotClient telegram,
    IHubContext<InboxHub> hub,
    CancellationToken ct) =>
{
    var texto = body.Text?.Trim();
    if (string.IsNullOrEmpty(texto))
    {
        return Results.BadRequest(new { error = "El campo 'text' es obligatorio." });
    }

    // Persiste + envía a Telegram + guarda el resultado de la entrega.
    var (contacto, mensaje) = await bandeja.ResponderAsync(chatId, texto, telegram, ct);

    await hub.Clients.All.SendAsync("NuevoMensaje", mensaje, ct);
    await hub.Clients.All.SendAsync("ContactoActualizado", contacto, ct);

    // Si Telegram rechazó el envío, el mensaje igual quedó guardado y se avisa en la respuesta.
    // Se devuelve 201 para no perder el registro; la app decide si muestra el aviso.
    if (mensaje.Delivered == false)
    {
        return Results.Created($"/api/v1/contacts/{chatId}/messages/{mensaje.Id}", new
        {
            mensaje.Id,
            mensaje.ContactId,
            mensaje.Text,
            mensaje.Direction,
            mensaje.CreatedAt,
            mensaje.Read,
            mensaje.Delivered,
            mensaje.DeliveryError,
            advertencia = $"El mensaje se guardó, pero Telegram no lo entregó: {mensaje.DeliveryError}"
        });
    }

    return Results.Created($"/api/v1/contacts/{chatId}/messages/{mensaje.Id}", mensaje);
});

// Elimina un mensaje puntual de la conversación (limpieza de pruebas o de comandos).
app.MapDelete("/api/v1/messages/{id:long}", async (
    long id,
    EncodeDbContext db,
    CancellationToken ct) =>
{
    var mensaje = await db.Mensajes.FirstOrDefaultAsync(m => m.Id == id, ct);
    if (mensaje is null)
    {
        return Results.NotFound(new { error = $"No existe el mensaje {id}." });
    }

    var contactoId = mensaje.ContactoId;
    db.Mensajes.Remove(mensaje);

    // Si era el último mensaje, se recalcula la vista previa del contacto.
    var contacto = await db.Contactos.FirstOrDefaultAsync(c => c.Id == contactoId, ct);
    if (contacto is not null)
    {
        var ultimo = await db.Mensajes
            .Where(m => m.ContactoId == contactoId && m.Id != id)
            .OrderByDescending(m => m.FechaCreacion)
            .FirstOrDefaultAsync(ct);

        contacto.UltimoTexto = ultimo?.Texto;
        contacto.UltimaFecha = ultimo?.FechaCreacion;
    }

    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

// Marca la conversación como leída (punto 4 de la spec: estado leído).
app.MapPost("/api/v1/contacts/{chatId}/read", async (
    string chatId,
    BandejaService bandeja,
    IHubContext<InboxHub> hub,
    CancellationToken ct) =>
{
    var contacto = await bandeja.MarcarLeidosAsync(chatId, ct);
    if (contacto is null)
    {
        return Results.NotFound(new { error = $"No existe el contacto '{chatId}'." });
    }

    // Se emite para que el contador de no leídos se actualice en todas las pestañas.
    await hub.Clients.All.SendAsync("ContactoActualizado", contacto, ct);
    return Results.Ok(contacto);
});

// -----------------------------------------------------------------------------
// Webhook de Telegram: recibe los updates del bot (GET con challenge + POST)
// -----------------------------------------------------------------------------
app.MapPost("/api/telegram/webhook", async (
    [FromBody] TelegramUpdate update,
    BandejaService bandeja,
    IHubContext<InboxHub> hub,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var mensaje = update.Message ?? update.EditedMessage;
        if (mensaje?.Text is null || mensaje.Chat is null)
        {
            return Results.Ok(); // sin texto no hay nada que guardar; igual se responde 200
        }

        var chatId = mensaje.Chat.Id.ToString();
        var emisor = string.IsNullOrWhiteSpace(mensaje.From?.Username)
            ? mensaje.From?.FirstName ?? chatId
            : $"@{mensaje.From.Username}";
        var nombre = string.IsNullOrWhiteSpace(mensaje.From?.LastName)
            ? mensaje.From?.FirstName
            : $"{mensaje.From.FirstName} {mensaje.From.LastName}";

        logger.LogInformation("Mensaje entrante | ChatId: {ChatId} | Tipo: {Tipo} | Emisor: {Emisor} | Texto: {Texto}",
            chatId, mensaje.Chat.Type, emisor, mensaje.Text);

        // /start es un comando del bot (Telegram lo envía al abrir el chat por primera vez),
        // no un mensaje de la conversación: no se muestra en la bandeja.
        if (mensaje.Text.TrimStart().StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("El usuario {Emisor} ha iniciado el bot con /start (no se muestra en la bandeja)", emisor);
            return Results.Ok();
        }

        // 1) Persistir en SQL Server (crea el contacto si es la primera vez).
        var (contacto, mensajeDto) = await bandeja.GuardarEntranteAsync(chatId, mensaje.Text, nombre, mensaje.From?.Username, ct);

        // 2) Emitir en tiempo real a la bandeja del operador.
        await hub.Clients.All.SendAsync("NuevoMensaje", mensajeDto, ct);
        await hub.Clients.All.SendAsync("ContactoActualizado", contacto, ct);

        return Results.Ok();
    }
    catch (Exception ex)
    {
        // Telegram exige un 200 para no reintentar el envío en bucle;
        // el error queda en los logs para diagnosticarlo.
        logger.LogError(ex, "Error al procesar el webhook de Telegram");
        return Results.Ok();
    }
});

// Hub de SignalR de la bandeja (Angular se conecta a /hubs/inbox).
app.MapHub<InboxHub>("/hubs/inbox");

app.Run();

// --- Modelos de Binding del webhook (subset de la API de Telegram) ------------
record TelegramUpdate(TelegramMessage? Message, TelegramMessage? EditedMessage);

record TelegramMessage(
    [property: JsonPropertyName("message_id")] long MessageId,
    TelegramUser? From,
    TelegramChat? Chat,
    long Date,
    string? Text);

record TelegramUser(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("is_bot")] bool IsBot,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string? LastName,
    string? Username);

record TelegramChat(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type);

/// <summary>Cuerpo de la respuesta del operador: { "text": "…" }.</summary>
record RespuestaOperador(string? Text);
