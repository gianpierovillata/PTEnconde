var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
 * Asegúrate de configurar el webhook en Telegram con la URL correcta y el token secreto.
 */
app.MapPost("/api/v1/webhook/receiveMessage", (string message) => 
{
    // Aquí puedes procesar el mensaje recibido desde Telegram.
    // Por ejemplo, podrías registrar el mensaje o enviarlo a otro servicio.
    Console.WriteLine($"Mensaje recibido: {message}");
    return Results.Ok(new { status = "Message received" });
}).WithName("TelegramWebhook"); 


/* 
 * Endpoint para enviar mensajes a Telegram.
 * Este endpoint puede ser llamado desde tu aplicación para enviar mensajes a un chat específico.
 */
app.MapGet("/api/v1/webhook/sendMessage", (string chatId, string text) => 
{
    // Aquí puedes implementar la lógica para enviar un mensaje a Telegram.
    // Por ejemplo, podrías usar HttpClient para llamar a la API de Telegram.
    Console.WriteLine($"Enviando mensaje a chatId {chatId}: {text}");
    return Results.Ok(new { status = "Message sent" });
}).WithName("SendMessageToTelegram");




app.Run();

