# Prueba manual — circuito completo (operador ↔ usuario de Telegram)

Secuencia para validar a mano, de principio a fin, el circuito real de la bandeja:

```
usuario escribe al bot → Telegram → webhook (ngrok) → API → SQL Server → SignalR → bandeja Angular
operador responde en la bandeja → API → Bot API de Telegram → usuario recibe
```

Es la prueba que se corre **con el bot real y una cuenta real de Telegram**, sin simulaciones. Cada paso trae
el resultado esperado y, cuando aplica, la trampa conocida.

- Tiempo estimado: **10 minutos**.
- Requisitos: Docker, el `.env` completo (tokens de Telegram y ngrok), y acceso a una cuenta de Telegram.
- Los comandos con Docker se envuelven en `sg docker -c "..."` porque en esta máquina la shell no tiene
  cargado el grupo `docker`.

> Antes de empezar: `git status` limpio y la última versión del código. Este documento asume el esquema
> actual de `Mensaje` (`Entregado`, `ErrorEntrega`, `TelegramMessageId`).

---

## 0. Arranque

```bash
cd /home/piero/Descargas/PTEnconde

# Validar el compose sin tocar el .env local
docker compose --env-file .env.example config --quiet && echo "compose OK"

# Levantar todo (sqlserver + api + frontend + ngrok-api + ngrok-app)
sg docker -c "docker compose up -d --build"
```

Esperá ~30 segundos a que SQL Server inicialice y la API arranque.

```bash
sg docker -c "docker compose ps"     # los 5 servicios en Up
```

**Trampa — esquema viejo.** La base se crea con `EnsureCreated()`, que **no migra** una base existente. Si
venís de una versión anterior del modelo (sin las columnas de entrega), el paso 1 va a fallar. En ese caso:

```bash
sg docker -c "docker compose down -v"     # borra el volumen
sg docker -c "docker compose up -d --build"
```

> `down -v` **borra todas las conversaciones**. Si tenés datos que te importan, exportalos antes.

---

## 1. Salud local

```bash
curl http://localhost:5088/api/v1/health
# → {"status":"Healthy"}
```

Si esto falla, no sigas: la API no está lista. Mirá `sg docker -c "docker compose logs api --tail 30"`.

---

## 2. Salud pública (túnel de ngrok)

Esta es la URL que usa Telegram para entregar los mensajes. **Si el túnel no responde, ningún mensaje del
bot va a llegar** y Telegram va a reportar `404` en `getWebhookInfo`.

```bash
NGROK_DOMAIN_API=$(grep '^NGROK_DOMAIN_API=' .env | cut -d= -f2)

curl -s -o /dev/null -w "público: %{http_code}\n" \
  -H "ngrok-skip-browser-warning: 1" \
  "https://${NGROK_DOMAIN_API}/api/v1/health"
# → público: 200
```

> No uses `source .env` para esto: algunos valores del `.env` (con espacios) hacen que bash se queje al
> leerlo como script. `grep` + `cut` extrae solo la variable que necesitás.

**Trampa — ngrok free (`ERR_NGROK_6024`).** El header `ngrok-skip-browser-warning` es obligatorio en el
navegador; sin él ngrok devuelve su página de advertencia en vez de la respuesta de la API (y el navegador
lo reporta como un falso error de CORS). El frontend ya lo manda; en `curl` hay que agregarlo a mano, como
arriba.

---

## 3. Verificar el webhook registrado en Telegram

```bash
TOKEN=$(grep '^TELEGRAM_BOT_TOKEN=' .env | cut -d= -f2)

curl -s "https://api.telegram.org/bot${TOKEN}/getWebhookInfo"
```

Esperado:

- `"url"` apunta a `https://<NGROK_DOMAIN_API>/api/telegram/webhook`.
- `"pending_update_count": 0`.
- `"last_error_message"` ausente, o un error **viejo** (`last_error_date` anterior a esta prueba).

Si la URL está vacía o mal, registralo:

```bash
curl -s -F "url=https://${NGROK_DOMAIN_API}/api/telegram/webhook" \
  "https://api.telegram.org/bot${TOKEN}/setWebhook"
# → {"ok":true,"result":true,"description":"Webhook was set"}
```

**Trampa — `404 Not Found`.** Si `last_error_message` dice `Wrong response from the webhook: 404 Not Found`
y la fecha es reciente, el túnel de la API está caído aunque el contenedor del webhook siga "Up". Verificá
con el paso 2 y mirá `sg docker -c "docker compose logs ngrok-api --tail 20"`. Un ngrok local (binario fuera
de Docker) con el mismo dominio bloquea al servicio (`ERR_NGROK_334`): solo puede haber un agente por dominio.

---

## 4. Escribirle al bot desde Telegram (camino de entrada)

En la app de Telegram, buscá el bot **@encodechat_bot** y mandale un mensaje de texto. Por ejemplo:

```
prueba manual 1
```

> **No uses `/start` para este paso.** `/start` se registra en el log pero **se descarta**: no crea el
> contacto, no persiste y no emite SignalR. El contacto aparece recién con el primer mensaje real. Si querés
> probar ese comportamiento, mandá `/start` primero y confirmá que no pasa nada, y después un texto normal.

Confirmá que llegó:

```bash
sg docker -c "docker compose logs api --tail 20" | grep "Mensaje entrante"
# → Mensaje entrante | ChatId: <tu chatId> | Tipo: private | Emisor: @<tu usuario> | Texto: prueba manual 1
```

Anotá tu `chatId`: lo vas a usar en los pasos siguientes.

```bash
curl -s http://localhost:5088/api/v1/contacts | python3 -m json.tool
# → [{ "id": "<tu chatId>", "name": "...", "lastMessage": "prueba manual 1", "unreadCount": 1, ... }]
```

**Si el mensaje no aparece ni en el log ni en los contactos:** el update no llegó. Volvé al paso 3 y
revisá `getWebhookInfo` (URL, `last_error_message`) y el túnel (paso 2). El problema está en el camino
Telegram → ngrok, no en la API.

---

## 5. Abrir la bandeja y ver el mensaje en vivo

- Local: `http://localhost:4200`
- Pública: `https://<NGROK_DOMAIN_APP>/`

Esperado:

1. El sidebar lista el contacto con la vista previa `prueba manual 1` y el contador de no leídos en **1**.
2. Hacé clic en el contacto: se carga el historial y el contador vuelve a **0** (marca de leído).
3. **Sin recargar la página**, mandá otro mensaje al bot desde Telegram (`prueba manual 2`): aparece solo,
   como burbuja entrante, y el sidebar se reordena al tope.

**Trampa — sidebar vacío.** Si la consola del navegador habla de CORS o la lista está vacía pero `curl`
funciona, es la advertencia de ngrok free disfrazada (paso 2). El frontend ya manda el header; si igual
falla, revisá `ngrok.interceptor.ts` y el `withUrl` de SignalR.

---

## 6. Responder como operador (camino de salida → Telegram)

Con la conversación abierta, escribí en la caja de abajo y presioná **Enter**. Por ejemplo:

```
respuesta del operador - prueba manual
```

Esperado:

1. La burbuja saliente aparece en la bandeja al instante.
2. **El mensaje llega a tu Telegram** en unos segundos. Confirmalo en la app.
3. La respuesta HTTP (si la inspeccionás) trae `delivered: true`.

También se puede disparar por API, útil para verificar el JSON completo:

```bash
CHAT_ID=<tu chatId>

curl -s -X POST "http://localhost:5088/api/v1/contacts/${CHAT_ID}/messages" \
  -H "Content-Type: application/json" \
  -d '{"text": "respuesta por API"}' | python3 -m json.tool
# → 201 { "direction": "outgoing", "delivered": true, "deliveryError": null, ... }
```

---

## 7. Verificar la persistencia y el estado de entrega

El historial completo queda en la base, con el resultado de cada entrega:

```bash
curl -s "http://localhost:5088/api/v1/contacts/${CHAT_ID}/messages" | python3 -m json.tool
```

Esperado, en orden cronológico:

| `direction` | `delivered` | Nota |
|---|---|---|
| `incoming` | `null` | El campo no aplica a los mensajes entrantes. |
| `outgoing` | `true` | Incluye el `telegramMessageId` que devolvió Telegram. |

Consulta directa a SQL Server (el contenedor no trae `sqlcmd`; se usa la imagen de tools):

```bash
PW=$(grep '^SQLSERVER_SA_PASSWORD=' .env | cut -d= -f2)

sg docker -c "docker run --rm --network enconde-inbox_backend mcr.microsoft.com/mssql-tools \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P '$PW' -C -d EncodeDb -Q \
  \"SET NOCOUNT ON; SELECT Id, ContactoId, Direccion, Entregado, TelegramMessageId, \
  LEFT(ISNULL(ErrorEntrega,'-'),40) AS Error FROM Mensajes ORDER BY Id;\""
```

El `TelegramMessageId` es la prueba dura: confirma que Telegram **aceptó** el mensaje, no un `true` optimista.

---

## 8. Probar el camino de fallo (mensaje no entregado)

La API nunca pierde un mensaje por un fallo de Telegram: lo guarda igual y registra el motivo. Se prueba con
un `chatId` que no existe:

```bash
curl -s -X POST http://localhost:5088/api/v1/contacts/999999001/messages \
  -H "Content-Type: application/json" \
  -d '{"text": "prueba de fallo"}' | python3 -m json.tool
```

Esperado — **sigue siendo `201`**, no un error:

```json
{
  "direction": "outgoing",
  "delivered": false,
  "deliveryError": "Bad Request: chat not found",
  "advertencia": "El mensaje se guardó, pero Telegram no lo entregó: Bad Request: chat not found"
}
```

Y quedó persistido, con el error guardado:

```bash
curl -s http://localhost:5088/api/v1/contacts/999999001/messages | python3 -m json.tool
# → el mensaje figura con "delivered": false y "deliveryError"
```

Limpiá ese contacto de prueba:

```bash
curl -s -o /dev/null -w "DELETE: %{http_code}\n" -X DELETE http://localhost:5088/api/v1/contacts/999999001
# → DELETE: 204
```

---

## 9. Probar el borrado de conversaciones (opcional)

Desde la UI: **Vaciar** (cabecera del sidebar), la **papelera** de una tarjeta o **Eliminar** (cabecera del
chat). Las tres piden confirmación en un diálogo modal (que también se cierra con Escape o **Cancelar**).

Por API:

```bash
curl -s -o /dev/null -w "una: %{http_code}\n" -X DELETE "http://localhost:5088/api/v1/contacts/${CHAT_ID}"
curl -s -X DELETE http://localhost:5088/api/v1/contacts
# → {"eliminadas": n}
```

> Borrar la bandeja **no** borra el historial del lado de Telegram ni desregistra el webhook: la conversación
> vuelve a aparecer cuando el usuario le escribe otra vez al bot.

---

## 10. Apagado

```bash
sg docker -c "docker compose down"        # conserva la base (recomendado)
# sg docker -c "docker compose down -v"   # además borra la base (empezar de cero)
```

---

## Resumen: qué valida cada paso

| # | Valida |
|---|---|
| 1 | La API responde localmente. |
| 2 | El túnel público está vivo (Telegram puede entregar). |
| 3 | El webhook está registrado y sin errores recientes. |
| 4 | Telegram → ngrok → API → SQL Server (mensaje entrante persistido). |
| 5 | API → SignalR → Angular en tiempo real, con marca de leído. |
| 6 | Operador → API → Bot API → **el usuario recibe en Telegram**. |
| 7 | Persistencia del historial y estado de entrega (`telegramMessageId`). |
| 8 | Fallo de entrega: se guarda igual, con el motivo, y responde `201`. |
| 9 | Borrado de conversaciones y sincronización del sidebar. |
| 10 | Apagado ordenado (con o sin borrar la base). |

## Checklist de verificación

- [ ] `GET /api/v1/health` responde `{"status":"Healthy"}` (local y público).
- [ ] `getWebhookInfo` sin errores recientes y `pending_update_count: 0`.
- [ ] Un mensaje real al bot aparece en el sidebar **sin recargar**, con el contador de no leídos.
- [ ] Abrir el contacto marca los mensajes como leídos (contador a 0).
- [ ] La respuesta del operador llega **al Telegram real** del usuario.
- [ ] `GET .../messages` muestra `delivered: null` (entrante) y `delivered: true` + `telegramMessageId` (saliente).
- [ ] Un envío a un chat inexistente devuelve `201`, `delivered: false` y queda persistido con el error.
- [ ] Borrar una conversación y la bandeja actualiza el sidebar (y las otras pestañas abiertas).
