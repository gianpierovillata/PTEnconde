## Proyecto EncodeAssistant

## Objetivo principal

Construir una bandeja web donde un operador recibe, visualiza y responde en tiempo real los mensajes de texto que los usuarios envían a un bot de Telegram.

## Stack Tecnologico
| Área | Producto | Tecnologia |
|---|---|---|
| Backend | Api Minimal - WebHook | .Net 10
| Frontend | App de mensajeria para operadores centralizada | Angular ultima version estable 
| Base de datos | Base de datos para mantener persistencia   | Sql Server
| Mensajería | Menasajeria colas de mensajes | RabbitMQ para el procesamiento de los mensajes entrantes. |
| Tiempo real | Comunicación en tiempo real entre backend y frontend. Tecnología a elección. | Signal R .NET
| Telegram | Bot propio (creado con BotFather) y webhook registrado automáticamente en Telegram al levantar la solución, usando la URL pública de ngrok. | Telegram botfather
| Exposición pública | Ngrok para exponer el webhook y apps | Ngrok
| Ejecución | Docker Compose. La solución debe levantarse con `docker compose up` | Docker 
   