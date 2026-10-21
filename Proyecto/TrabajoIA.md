# Trabajo con IA

Para la realizacion de este proyecto EncodeAsisstant he dedico utilizar diferentes herramientas de IA

1- Herramientas para desarrollo de codigo, analisis y agentes con Opencode por su facilidad de uso, poseer diferentes modelos free y su parecido a otras herramientas como 
Claude Code, En este Agente de codigo he implementando la mayoria de los prompts de desarrollo de los requisitosa funcionales y no
funcionales, ademas de la verificacion de errores, y sistema de versionado (algunos de manera manual mediante la terminal, y otros automatico permitiendo que el agente haga los commits de los avances necesarios), tambien implementando verficaciones del estado de los contenedores docker y verificacion de conexiones remotas con las app, la api y telegram
Otro detalle que el agente permite establecer diferentes situaciones para desarrollo con peguntas acerca del uso de que tecnologia o solucion es mas optima o recomendada

2- ChatGPT version Web para consultas y analisis general, documentacion y preguntas o dudas acerca de algunas tecnologias

3- Agente Copilot en VSCode como Agente y verificacion de codigo desarrollado o dudas en cuanto a una implementacion de configuraciones del IDE con la IA

A continuacion se muestran algunos prompts, para ver otros ubicar la carpeta Proyectos/Prompts.md

## Prompt 1
Quiero que analices el proyecto que vamos a estar realizando, vamos a disenar es una app en angular con su ultima version lts y un backend api .Net 10 mediante un webhook para gestionar una comunicacion real entre los usuarios con el bot y los operadores de enconde labs, en una app cliente. El sistema de mensajeria de usuarios a operadores, sera a traves de telegram (cliente del usuario) mediante el bot y el webhook se encagara de gestionar las peticiones de mensajes hacia la api del backend. Tanto la app en agular como la api backend se van a desplegar en contenedores
docker cada uno siendo un servicio separado peor gestionado mediante docker-compose Quiero que siempre tengas presente las mejores practicas de serguridad y despliegue en los contenedores, ademas agregaremos la posibilidad de exponerlos median grok a trabaes de url publicas que seran mostradas en los archivos .env de variables de entorno estas variables no se subiran al repo de github creado por temas de seguridad pero si tendremos un .envexample con los nombres de las variables de entorno pero datos falsos.  Ahora quiero que analices el archivo que estaen la carpeta Proyecto que indica todos los requisitos funcionales y no funcionales del proyecto de igual forma siempre ten pensiente el readme.md y el archivo de agents.md para entender el proyecto, los cambios, la seguridad y las necesidades funcionales que se van a ir desarrollando.

Hagamos un plan de trabajo y creamos un archivo md solo para esto quye vamos a ir actualizando segun se vaya imiplementando funcionalidades

## Prompt 4
Evalua la app encondechatapp y quiero que crees una pagina de inicio, va a tener un ´panel lateral y un panel central, en el lateral va a aparecer usuarios y en el central el usuario y una conversacion de chat, de momento solo crea la pagina principal en css, angular 22 componentes y opcion de servicios sin implementar, muestrame el plan para hacerlo sin crear codigo aun, luego vamos a dockernizar esta app en angular , luego para los estilos quiero que te bases en la pagina que te voy a pasar, la idea es mantener unos estilos muy parecidos, sin transiciones, muy minimalista pero con los colores de la web https://encodelabs.com.ar/es/

# Prompt 6
ok en base a lo que tenemos de la api, vamos a desarrollar la parte de mostrar el listado de usuarios en la parte lateral y el mensaje recibo en la parte central en la app de angular, asi cada vez que un usuario envia un mensaje se muestre en la aplicacion , la bandeja de entrada sera el lado izquierdo, por ahora solo mostrar el  usuario con algun icono de usuario y al dar click mostrar en la parte central el usuario y el mensaje enviado,  que se mantenga los mensajes enviados, usa la bd de docker creada,  al final voy a revisar primero que este funcionando todo y luego verificas el correcto estado de los dos proyectos api y front








 