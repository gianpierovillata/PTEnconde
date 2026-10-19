## Prompt 1
Quiero que analices el proyecto que vamos a estar realizando, vamos a disenar es una app en angular con su ultima version lts y un backend api .Net 10 mediante un webhook para gestionar una comunicacion real entre los usuarios con el bot y los operadores de enconde labs, en una app cliente. El sistema de mensajeria de usuarios a operadores, sera a traves de telegram (cliente del usuario) mediante el bot y el webhook se encagara de gestionar las peticiones de mensajes hacia la api del backend. Tanto la app en agular como la api backend se van a desplegar en contenedores
docker cada uno siendo un servicio separado peor gestionado mediante docker-compose Quiero que siempre tengas presente las mejores practicas de serguridad y despliegue en los contenedores, ademas agregaremos la posibilidad de exponerlos median grok a trabaes de url publicas que seran mostradas en los archivos .env de variables de entorno estas variables no se subiran al repo de github creado por temas de seguridad pero si tendremos un .envexample con los nombres de las variables de entorno pero datos falsos.  Ahora quiero que analices el archivo que estaen la carpeta Proyecto que indica todos los requisitos funcionales y no funcionales del proyecto de igual forma siempre ten pensiente el readme.md y el archivo de agents.md para entender el proyecto, los cambios, la seguridad y las necesidades funcionales que se van a ir desarrollando.

Hagamos un plan de trabajo y creamos un archivo md solo para esto quye vamos a ir actualizando segun se vaya imiplementando funcionalidades


## Prompt 2
En base al proyecto vamos a dockernizar la aplicacion de la api en .net 10 y crearemos un dockerfile correspondiente, que va a leer las variables de entorno del archivo .env, asi mismo crear una bd en docker de sqlserver con el dockerfile, finalmente ajustar las variables de entorno necesarias y crear el docker-compose para levantar la api y la bd en servicios separados




