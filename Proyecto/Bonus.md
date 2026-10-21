# Preguntas bonus (opcional)
Respondé brevemente, en un máximo de un párrafo cada una:

1. ¿Para qué usarías un sistema de caché en una bandeja conversacional?
2. ¿Cómo gestionarías el tráfico de archivos multimedia?

1. Para una bandeja conversacional usaria un sistema de cache como redis para asi cachear datos estaticos como username, id del chat y tenerlo como datos de comparacion para asi saber si el usuario cambio de nombre mediante el id, ademas para el historial de chats que aunque son de persistencia para futuras consultas mediante el chat real no tener que estar llamando a la api cuando ya se tienen chats viejos, asi siendo mas eficiente al momento de usar 


2. EL trafico multimedia mediante stream en la api , ya que el procesamiento de video e imagen cuesta y puede verse reflejado en el redimiento de la app, ademas inconsistencia en tiempo real, tambien por motivos de seguridad mendiente algun sistema de comprobacion de validez del video o imagen