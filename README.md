# IT Rock Challenge - Task Management API

API RESTful stateless para la gestión de tareas, construida bajo principios de arquitectura limpia y buenas prácticas cloud-native utilizando .NET 8 y PostgreSQL.

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## Respuestas HTTP Consistentes y Semánticas (Rúbrica: Manejo de Errores)

El sistema implementa una estrategia estricta de códigos de estado HTTP semánticos y consistentes:

- Login Exitoso (200 OK): Devuelve el token JWT estructurado.
- Operación Exitosa (200 OK): Retorna los objetos DTO correspondientes.
- Creación Exitosa (201 Created): Incluye la cabecera Location con la ruta de acceso al nuevo recurso.
- Eliminación Exitosa (204 No Content): Confirmación semántica de eliminación sin cuerpo de respuesta.
- Error de Validación de Entrada (400 Bad Request): Cumple con el estándar RFC 7807 (Problem Details) mediante Results.ValidationProblem, detallando qué campos fallaron (FluentValidation).
- Token Ausente o Inválido (401 Unauthorized): Interceptado nativamente a nivel de middleware para proteger las rutas.
- Recurso Inexistente o Sin Permisos (404 Not Found): Para evitar la fuga de información (Information Disclosure), si una tarea no existe o pertenece a otro usuario, la API responde unificado con 404 en lugar de 403.
- Falla en la API Externa (502 Bad Gateway): Si la API externa de JSONPlaceholder falla, se captura mediante un bloque defensivo try-catch y se retorna un código semántico que deslinda la responsabilidad del servidor local.

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## Decisiones Técnicas

1. Estructura en Capas Transversales (Clean Architecture Modificada):
   - Domain: Contiene la entidad pura TodoTask, totalmente aislada de frameworks externos.
   - Application: Orquesta la lógica de negocio a través de interfaces, DTOs inmutables (records) y validadores independientes.
   - Infrastructure: Resuelve el acceso a datos (ApplicationDbContext con EF Core mapeado a PostgreSQL) y las comunicaciones externas (JsonPlaceholderClient), manteniendo el núcleo del sistema limpio.
   - Presentation: Expone los endpoints modulares de forma desacoplada mediante métodos de extensión.

2. Minimal APIs sobre Controladores Tradicionales:
   Se seleccionaron las Minimal APIs de .NET 8 debido a su rendimiento superior (menor sobrecarga de asignaciones en memoria), menor código de infraestructura repetitivo (boilerplate) y alineación perfecta con despliegues cloud-native.

3. Uso de IHttpClientFactory para la Integración Externa:
   En lugar de instanciar HttpClient manualmente (lo cual provoca el error de infraestructura Socket Exhaustion), se inyectó la factoría nativa de .NET para centralizar el ciclo de vida de las conexiones salientes.

4. Validación Desacoplada con FluentValidation y Endpoint Filters:
   Se utilizó FluentValidation en lugar de Data Annotations para mantener las reglas de validación fuera de los DTOs (Solid: Principio de Responsabilidad Única). Estas reglas se ejecutan de manera automática mediante un IEndpointFilter genérico que intercepta las peticiones antes de que toquen la lógica de negocio.

5. Resiliencia Avanzada con Polly (Microsoft.Extensions.Http.Resilience):
   Se acopló Polly al pipeline de comunicaciones del cliente HTTP saliente. Se configuró el manejador estándar de resiliencia de .NET 8, el cual dota a la API de estrategias automáticas de Reintentos con Retroceso Exponencial (Exponential Backoff) y Disyuntor (Circuit Breaker).
   Esto garantiza que la aplicación sea tolerante a fallos transitorios de red con proveedores externos sin interrumpir la experiencia del usuario.
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## Instrucciones de Instalación y Ejecución Local

Prerrequisitos:
- .NET 8 SDK instalado.
- PostgreSQL corriendo localmente.

Paso 1: Configurar la Base de Datos
Renombrá el archivo appsettings.Example.json a appsettings.json y asegurate de que apunte a tu instancia local de PostgreSQL, usando la siguiente estructura en la cadena de conexión:

"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=nombre-de-tu-db;Username=tu-user-name;Password=tu-password"
}

Paso 2: Ejecutar las Migraciones de EF Core
Abrí una terminal en la raíz del proyecto y aplicá las migraciones ejecutando:

dotnet ef database update

Paso 3: Ejecutar la Aplicación
Iniciá el servidor de desarrollo Kestrel con el comando:

dotnet run

La API levantará en el puerto configurado en tu archivo launchSettings.json, por ejemplo: 7271. Podrás acceder a la interfaz interactiva de pruebas en la url: https://localhost:7271/swagger/index.html.

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## Ejecución Alternativa mediante Docker (Entorno Aislado)

Si dispones de Docker instalado, puedes omitir la configuración local de la base de datos y compilar todo el ecosistema de la API y PostgreSQL en contenedores aislados ejecutando el siguiente comando en la raíz del proyecto:

docker-compose up --build

Comandos Útiles de Mantenimiento y Resolución de Problemas
En caso de realizar modificaciones en el código fuente de C#, actualizar dependencias o experimentar problemas con la caché de compilación, utiliza los siguientes comandos:

1. Recompilación forzada ignorando cachés previas
Si el contenedor no refleja los cambios recientes de tus archivos o del Program.cs, fuerza a Docker a reconstruir todo desde cero:

docker-compose up --build --force-recreate

2. Limpieza total del motor de compilación (BuildKit)
Si el compilador de .NET (CSC) arroja errores de archivos fantasmas no encontrados (como el archivo Dockerfile indexado erróneamente en el .csproj), limpia por completo la caché interna de Docker ejecutando:

docker builder prune -f

3. Detener y limpiar los contenedores
Para apagar el entorno y remover los contenedores del sistema de forma limpia:

docker-compose down

Inicialización de la Base de Datos (Migraciones)
El contenedor de PostgreSQL se iniciará de forma aislada exponiendo el puerto estándar 5432:5432. Una vez que los contenedores estén corriendo (up), debes impactar el esquema de datos ejecutando las migraciones desde tu terminal local:

Restaurar dependencias locales:

dotnet restore

Aplicar las migraciones de Entity Framework Core:

dotnet ef database update

Nota: Asegúrate de tener instalada la herramienta global de EF Core (dotnet tool install --global dotnet-ef). Una vez completado, podrás acceder a la documentación interactiva en http://localhost:7271/swagger/index.html.

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## Ejemplos de Uso de los Endpoints (Comandos de Prueba)

1. Autenticación (POST /api/v1/auth/login)
curl -X POST https://localhost:7271/api/v1/auth/login -H "Content-Type: application/json" -d "{"username": "admin", "password": "password123"}"

2. Obtener Tareas (GET /api/v1/tasks)
curl -X GET https://localhost:7271/api/v1/tasks -H "Authorization: Bearer INYECTAR_TOKEN"

3. Crear una Tarea (POST /api/v1/tasks)
curl -X POST https://localhost:7271/api/v1/tasks -H "Authorization: Bearer INYECTAR_TOKEN" -H "Content-Type: application/json" -d "{"title": "Nueva tarea", "description": "Hacer el challenge"}"

4. Actualizar una Tarea (PATCH /api/v1/tasks/{id})
curl -X PATCH https://localhost:7271/api/v1/tasks/TU_GUID_AQUI -H "Authorization: Bearer INYECTAR_TOKEN" -H "Content-Type: application/json" -d "{"completed": true}"

5. Eliminar una Tarea (DELETE /api/v1/tasks/{id})
curl -X DELETE https://localhost:7271/api/v1/tasks/TU_GUID_AQUI -H "Authorization: Bearer INYECTAR_TOKEN"

6. Importar Tareas Externas (POST /api/v1/tasks/import)
curl -X POST https://localhost:7271/api/v1/tasks/import -H "Authorization: Bearer INYECTAR_TOKEN"

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## Pruebas de Integración (Insomnia / Postman)

Para facilitar la evaluación y auditoría de los endpoints expuestos en producción, se ha adjuntado una colección de pruebas lista para usar en la raíz del proyecto: 
`ITRockChallenge_Insomnia_Collection.json`

Esta colección cuenta con variables globales de entorno y encadenamiento automatizado de tokens de autenticación.

### Pasos para Importar y Utilizar la Colección:

1. **Importar el archivo:**
   * Abrí **Insomnia** (o Postman).
   * Hacé clic en el botón de **Import** (o *Preferences > Data > Import Data*).
   * Seleccioná el archivo `ITRockChallenge_Insomnia_Collection.json` de la raíz del proyecto.

2. **Configurar el Entorno (Environment):**
   * Al importar, se creará el entorno base automáticamente. Asegurate de que la variable `base_url` apunte correctamente a nuestro servicio web en producción:
     `https://itrock-challenge.onrender.com`

3. **Secuencia de Pruebas Automatizada:**
   * **Paso 1 - Login (`1. Authentication`):** Ejecutá en primer lugar el request `POST Login - Success` con las credenciales estáticas preconfiguradas (`admin` / `password123`).
      Al recibir la respuesta `200 OK`, un tag dinámico capturará el token JWT de forma automática.
   * **Paso 2 - CRUD (`2. Tasks CRUD`):** Ya podés ejecutar los endpoints de obtener, crear, actualizar o borrar tareas. No es necesario copiar y pegar el token a mano;
      viajará inyectado transparentemente como cabecera `Bearer` gracias al tag dinámico.
   * **Paso 3 - Integración Externa (`3. External Integration`):** Ejecutá el endpoint de importación (`POST Import External Tasks`).
      Este servicio se comunicará con la API externa de JSONPlaceholder de forma invisible, procesará las primeras 5 tareas y las guardará en la base de datos PostgreSQL de producción. 
      Podés verificar el impacto volviendo a ejecutar el listado completo de tareas.