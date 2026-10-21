# ADR-001: Base de datos relacional — SQL Server

**Estado:** Aceptado
**Fecha:** 21/10/2026

## Contexto

La bandeja guarda dos cosas: los **contactos** (usuarios de Telegram, con su nombre, su `@username`, la vista previa del último mensaje y la cantidad de no leídos) y los **mensajes** de cada conversación (texto, dirección `incoming`/`outgoing`, marca de leído, estado de entrega hacia Telegram y fecha).

El modelo es claramente relacional: un contacto tiene N mensajes (clave foránea con borrado en cascada), y la consulta que alimenta el sidebar es "último mensaje por contacto, ordenado por fecha, con contador de no leídos".
El backend es .NET 10 con Clean Architecture, y todo tiene que levantarse con un único `docker compose up`.

## Alternativas consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| **SQL Server** (EF Core `UseSqlServer`) | Integridad referencial y cascadas nativas; transacciones para los contadores de no leídos; índices para las dos consultas calientes (último mensaje por contacto, historial por fecha); proveedor EF Core de primera clase; imagen oficial en Docker; tooling del ecosistema .NET | Contenedor pesado (~2 GB de RAM) y arranque lento; licencia `Developer` no apta para producción; escalado vertical; `sa` como usuario de la app en desarrollo |
| **PostgreSQL** | Licencia libre, maduro, excelente performance; `Npgsql` + EF Core | Fuera del stack Microsoft del proyecto; la imagen oficial pesa bastante; sin ventaja funcional para este modelo |
| **SQLite** (archivo) | Cero infraestructura, ideal para tests | Un solo escritor, sin concurrencia real, y no sirve para una API stateless en contenedor |

## Decisión

**Usar SQL Server 2022 como sistema de registro, con Entity Framework Core 10 como acceso a datos** (`Microsoft.EntityFrameworkCore.SqlServer`).Ya que es mas clara y estable con el ecosistema .NET 10

Motivos:

1. El dominio es relacional y lo ejecutamos con SQl, existen dominios con claves primeras que identifican conectactos como el id del usuario de telegram, el chat y hay foreing keys entre usuario mensajes y chat.
2. Existe consistencia en las transacciines cuando se guarda mensajes, y actualizar los leidos ocurre en transacciones, no queda el contador desfasado cuando se leen los chats.
3. Al usuar .NET EF core es un ORM que funciona bien con la tecnologia y ademas puede sermigrado a otros sistemas en nube como Azure SQL,  LocalDB etc

## Consecuencias

**Ganamos:**

1. Integridad y consistencia al guardar, borrados en cascada entre otros
2. Consultas de datos, a traves del ORM de manera mas eficiente al utilizar la interfaz de EF
3. Un unico motor de BD conocido por el equipo para otras operaciones como backups entre otros
4. El proveedor de EF Core más maduro: menos sorpresas y más foco en el dominio.

**Que desventajes podria tener:**
1.Imagen del contendor algo pesada SQL Server reserva ~2 GB de RAM. en su version developer y arranque lento.
2.Licencia en desarollo y no produccion, pero se puede migrar si es algo empresarial.
 
**Riesgos que quedan:**
1. De momento para pruebas solo esta el usuario sa para las consultas y uso de la bd ya que es para un test 
y no para produccion.
