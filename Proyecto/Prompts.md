## Prompt 1
Quiero que analices el proyecto que vamos a estar realizando, vamos a disenar es una app en angular con su ultima version lts y un backend api .Net 10 mediante un webhook para gestionar una comunicacion real entre los usuarios con el bot y los operadores de enconde labs, en una app cliente. El sistema de mensajeria de usuarios a operadores, sera a traves de telegram (cliente del usuario) mediante el bot y el webhook se encagara de gestionar las peticiones de mensajes hacia la api del backend. Tanto la app en agular como la api backend se van a desplegar en contenedores
docker cada uno siendo un servicio separado peor gestionado mediante docker-compose Quiero que siempre tengas presente las mejores practicas de serguridad y despliegue en los contenedores, ademas agregaremos la posibilidad de exponerlos median grok a trabaes de url publicas que seran mostradas en los archivos .env de variables de entorno estas variables no se subiran al repo de github creado por temas de seguridad pero si tendremos un .envexample con los nombres de las variables de entorno pero datos falsos.  Ahora quiero que analices el archivo que estaen la carpeta Proyecto que indica todos los requisitos funcionales y no funcionales del proyecto de igual forma siempre ten pensiente el readme.md y el archivo de agents.md para entender el proyecto, los cambios, la seguridad y las necesidades funcionales que se van a ir desarrollando.

Hagamos un plan de trabajo y creamos un archivo md solo para esto quye vamos a ir actualizando segun se vaya imiplementando funcionalidades


## Prompt 2
En base al proyecto vamos a dockernizar la aplicacion de la api en .net 10 y crearemos un dockerfile correspondiente, que va a leer las variables de entorno del archivo .env, asi mismo crear una bd en docker de sqlserver con el dockerfile, finalmente ajustar las variables de entorno necesarias y crear el docker-compose para levantar la api y la bd en servicios separados

# Prompt 3
Lavantar los servicios en docker con docker-compose. Verficar si los enpoints creados de test, root y health estan fucionando mediante
llamados http curl (get,post) a los enpoint expuestos

# Promt 4
Evalua la app encondechatapp y quiero que crees una pagina de inicio, va a tener un ´panel lateral y un panel central, en el lateral va a aparecer usuarios y en el central el usuario y una conversacion de chat, de momento solo crea la pagina principal en css, angular 22 componentes y opcion de servicios sin implementar, muestrame el plan para hacerlo sin crear codigo aun, luego vamos a dockernizar esta app en angular , luego para los estilos quiero que te bases en la pagina que te voy a pasar, la idea es mantener unos estilos muy parecidos, sin transiciones, muy minimalista pero con los colores de la web https://encodelabs.com.ar/es/





