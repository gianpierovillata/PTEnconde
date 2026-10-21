# ADR-002: Comunicación en tiempo real — SignalR

**Estado:** Aceptado
**Fecha:** 21/10/2026

## Contexto

La bandeja tiene que **recibir los mensajes en tiempo real**: cuando un usuario escribe al bot de Telegram, el mensaje tiene que aparecer en la pantalla del operador sin que recargue, junto con la vista previa y el contador de no leídos actualizados.

En este proyecto el backend es ASP.NET Core 10 sobre .NET 10 y el frontend es Angular 22, y ambos van detrás de un túnel **ngrok** (HTTPS público) que además sirve los archivos estáticos de la app.


## Alternativas consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| **SignalR** (elegido) | Integración de primera clase en ASP.NET Core (`AddSignalR` + `Hub` + `IHubContext`); **cliente oficial `@microsoft/signalr` para Angular**| Dependencia del protocolo de SignalR (atado a .NET)|
| **WebSocket nativo** (`System.Net.WebSockets` o `ClientWebSocket`) | Protocolo estándar, mínimo overhead, sin dependencias | Hay que implementar a mano: menos integración con el hosting de ASP.NET Core |
| **Socket.IO / Node.js** | Ecosistema maduro, reconexión y rooms resueltas | Obliga a sumar un runtime Node al backend; el stack elegido es .NET; ES/TS en el servidor rompe la coherencia con Clean Architecture en C# |


## Decisión

Usar SignalR (ASP.NET Core) para el canal de tiempo real entre la API y la bandeja Angular.

Motivos:
1. Se integrafacilmente al stack escogido `builder.Services.AddSignalR()` y `app.MapHub<InboxHub>("/hubs/inbox")` creando una clase y usando su interfaz

2. Resilencia en conexiones sin codigo extra, el cliente se construye con `withAutomaticReconnect([0, 2s, 5s, 10s, 30s])` na caída de red o un reinicio de la API no exige reimplementar.

3. Operacion simple ya que el trafico de la app va por el mismo tunel ngrok y el mismo origen , simplificando el cors

## Consecuencias

**Ganamos:**
1. Comunicacion en tiempo real  y vista previo en los chat del operador
2. Reconexion y estabilidad
3. Codigo minimo, unhub vacio y usar la interfaz del cliente 


**Resignamos:**

- **El protocolo es de SignalR:** un cliente que no sea .NET/JS oficial necesita un cliente compatible. Aceptable porque el único cliente es nuestra app Angular.


**Riesgos que quedan:**

1. Sin autenticacion en el hub la spec deja la autenticación fuera de alcance, pero hoy cualquier cliente que conozca la URL del hub puede suscribirse a los eventos.


