# IT Rock Challenge - Task Management API

API RESTful stateless para la gestión de tareas, construida bajo principios de arquitectura limpia y buenas prácticas cloud-native utilizando .NET 8 y PostgreSQL.

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## Seguridad y datos sensibles

Este proyecto al ser un challenge técnico con credenciales de demostración intencionales no representa un modelo listo para producción sin endurecimiento adicional.

## Qué es deliberado del enunciado (no es una fuga accidental)

| Elemento                                               | Ubicación                             | Motivo                            |
=====================================================================================================================================
| Usuario `admin` / contraseña `password123`             | `AuthService`, README, Postman        | Requisito funcional del challenge |
| `userId` fijo `"1"` en el JWT del admin | `AuthService`| Permite probar la importación externa |
| Credenciales en ejemplos `curl` y colección Postman    | Documentación                         | Facilitar evaluación local        |

## Antes de desplegar a un entorno real

Cambiar obligatoriamente: clave JWT, credenciales de base de datos, usuario/contraseña de login (o integrar un proveedor de identidad), y restringir Swagger a entornos de desarrollo si aplica.

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## Decisiones Técnicas

1. Estructura en Capas 
   - Domain
   - Application
   - Infrastructure
   - Presentation

2. Minimal APIs:
   Se seleccionaron las Minimal APIs de .NET 8 debido a su rendimiento, menor código de infraestructura repetitivo y alineación perfecta con despliegues cloud-native.

3. Uso de IHttpClientFactory para la Integración Externa:
   En lugar de instanciar HttpClient manualmente (lo cual provoca el error de infraestructura Socket Exhaustion), se inyectó la factoría nativa de .NET para centralizar el ciclo de vida de las conexiones salientes.

4. Validación Desacoplada con FluentValidation y Endpoint Filters:
   Se utilizó FluentValidation en lugar de Data Annotations para mantener las reglas de validación fuera de los DTOs (Solid: Principio de Responsabilidad Única). Estas reglas se ejecutan de manera automática mediante un IEndpointFilter genérico que intercepta las peticiones antes de que toquen la lógica de negocio.

5. Cliente HTTP tipado para la integración externa (IHttpClientFactory + Polly):
   Se usa un único `AddHttpClient<IJsonPlaceholderClient, JsonPlaceholderClient>`: el `HttpClient` tipado se inyecta en `JsonPlaceholderClient`, evitando Socket Exhaustion y garantizando que Polly y la configuración apliquen al mismo pipeline.
   Se aplicaron reintentos con retroceso exponencial y circuit breaker. Si la API externa falla tras agotar los reintentos, `GlobalExceptionHandler` traduce `HttpRequestException` / `TaskCanceledException` en `502 Bad Gateway`.

6. Importación idempotente
    - Solo considera tareas externas con `userId == 1` (requisito del challenge).
    - Toma como máximo **5** candidatas.
    - Si el usuario ya importó una tarea con el mismo `ExternalSourceId`, **se omite** en llamadas posteriores.
    - Una segunda importación sin tareas nuevas devuelve `importedCount: 0`.

7. Persistencia con EF Core + PostgreSQL (Code First):
   `ApplicationDbContext` mapea la entidad `TodoTask` a PostgreSQL. El esquema se versiona con migraciones en `/Migrations` (`dotnet ef database update`).

8. Autenticación y autorización con JWT:
    Login en `/api/v1/auth/login` genera un token Bearer (`ITokenService`). Los endpoints de tareas exigen `RequireAuthorization()`; el `userId` se obtiene del claim `NameIdentifier`.

9. Versionado de API en la URL:
    `Asp.Versioning` expone rutas `/api/v{version}/...` (v1 por defecto). Swagger genera documentación por versión.

10. Versionado de ensamblado (deploy):
    `Version.props` define `Version`, `AssemblyVersion` y `FileVersion`, importado por `ITRockChallenge.csproj` para trazabilidad del binario desplegado.

11. Documentación interactiva con Swagger:
    Para probar endpoints desde la UI.

12. Manejo global de errores:
    `GlobalExceptionHandler` (`IExceptionHandler`) centraliza fallos no controlados.

13. Pruebas con xUnit:
    Tests con Moq: unitarios (`TaskService`, `TaskImportService`, `TokenService`) y de integración HTTP (`WebApplicationFactory` en endpoints de auth y tasks).

14. Logging nativo por consola
    Se usa el proveedor nativo `Microsoft.Extensions.Logging` con salida por consola y ventana de depuración.

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

Si dispones de Docker instalado, puedes omitir la configuración local de la base de datos y compilar todo el ecosistema de la API y PostgreSQL en contenedores aislados.

## Configuración del archivo `.env`

En la raíz del proyecto creá un archivo `.env`. `docker-compose.yml` lo consume para PostgreSQL, JWT y la URL de JSONPlaceholder.

Ejemplo:

```env
DB_USER=postgres
DB_PASSWORD=tu_password_seguro
DB_NAME=ITRockChallengeDB
JWT_SECRET_KEY=clave_jwt_de_al_menos_32_caracteres_para_firma_hmac
JSON_PLACEHOLDER_BASE_URL=URL_DE_JSONPLACEHOLDER
```

| Variable                    | Uso en Docker                                                                     |
==================================================================================================================
| `DB_USER`                   | Usuario de PostgreSQL (`POSTGRES_USER`)                                           |
| `DB_PASSWORD`               | Contraseña de PostgreSQL (`POSTGRES_PASSWORD`)                                    |
| `DB_NAME`                   | Nombre de la base (`POSTGRES_DB` y `Database=` en la connection string de la API) |
| `JWT_SECRET_KEY`            | Firma del token (`Jwt__Key` en la API)                                            |
| `JSON_PLACEHOLDER_BASE_URL` | URL base del cliente externo (`JsonPlaceholderSettings__BaseUrl`).                |

Luego, en la raíz del proyecto:

```bash
docker-compose up --build
```

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

## Colección de pruebas (Insomnia / Postman)

Para facilitar la evaluación y auditoría de los endpoints expuestos en producción, se ha adjuntado una colección de pruebas lista para usar en la raíz del proyecto: 
`ITRockChallenge.postman_collection.json.`

Esta colección cuenta con variables globales de entorno y encadenamiento automatizado de tokens de autenticación.

### Pasos para Importar y Utilizar la Colección:

1. **Importar el archivo:**
   * Abrí **Insomnia** (o Postman).
   * Hacé clic en el botón de **Import** (o *Preferences > Data > Import Data*).
   * Seleccioná el archivo `ITRockChallenge.postman_collection.json.` de la raíz del proyecto.

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
      Podés verificar el impacto volviendo a ejecutar el listado completo de tareas. En llamadas posteriores solo importa tareas externas que el usuario aún no tiene (idempotencia).

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

##  Paginación en Listado de Tareas (`GET /tasks`)

Para garantizar el rendimiento de la base de datos, el endpoint de obtención de tareas implementa Paginación del lado del Servidor a través de query parameters opcionales.

La consulta se procesa de forma eficiente directamente en PostgreSQL utilizando operadores de evaluación diferida (`Skip` y `Take`), evitando la carga masiva de registros en memoria.

## Parámetros de Consulta (Query Params)

Al realizar la petición, podés adjuntar los siguientes parámetros opcionales en la URL:

| Parámetro | Tipo  | Descripción                                 | Valor por Defecto |
======================================================================================
| `page`    | `int` | Número de la página que se desea recuperar. | `1`               |
| `pageSize`| `int` | Cantidad máxima de registros por página.    | `10`              |

## Estructura de la Respuesta

El endpoint retorna un objeto estructurado que incluye la metadata necesaria para que el frontend pueda construir controles de paginación dinámicos:

json:
{
  "data": [
    {
      "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
      "title": "Ejemplo de tarea paginada",
      "description": "Esta es una descripción de prueba",
      "completed": false,
      "createdAt": "2026-05-20T14:30:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 1,
  "totalRecords": 45,
  "totalPages": 5
}

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## Filtros Avanzados Dinámicos en Listado de Tareas (`GET /tasks`)

El endpoint permite combinar los siguientes filtros opcionales para segmentar los resultados de manera precisa:

| Parámetro  | Tipo      | Descripción                                                                                       | Ejemplo               |
=====================================================================================================================================================
| `completed`| `bool`    | Filtra las tareas según su estado (`true` para completadas, `false` para pendientes).             | `?completed=false`    |
| `search`   | `string`  | Texto de búsqueda libre. Evalúa coincidencias en **título** o **descripción** (Case-Insensitive). | `?search=Render`      |
| `fromDate` | `DateTime`| Limita los resultados a tareas creadas a partir de esta fecha (inclusive).                        | `?fromDate=2026-05-01`|
| `toDate`   | `DateTime`| Limita los resultados a tareas creadas hasta esta fecha (inclusive).                              | `?toDate=2026-05-31`  |

Eficiencia de Servidor: Los filtros son dinámicos. Si un parámetro se omite o viaja vacío, la API no lo incluye en la sentencia `WHERE` de SQL,
optimizando los tiempos de respuesta del motor de base de datos.

## Ejemplos de Combinación (Filtros + Paginación)

Buscar tareas pendientes que contengan la palabra "desafio":
    GET [https://itrock-challenge.onrender.com/api/v1/tasks?search=desafio&completed=false]

Obtener la segunda página de tareas completadas creadas en mayo de 2026 (bloques de 5):
    GET [https://itrock-challenge.onrender.com/api/v1/tasks?completed=true&fromDate=2026-05-01&toDate=2026-05-31&page=2&pageSize=5]

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

### Manejo Global de Errores
Para proteger la integridad del servidor, se implementó la interfaz moderna `IExceptionHandler` de .NET.
Centralización de Fallas: Cualquier excepción no controlada en la capa de servicios o infraestructura es interceptada automáticamente.
Respuestas Estandarizadas: El cliente/frontend nunca recibe un error crudo ni el *stack trace* expuesto.
Mapeo de Errores de Infraestructura: Si la API de terceros falla y la política de resiliencia de Polly agota sus reintentos, el manejador captura las excepciones de red (`HttpRequestException` / `TaskCanceledException`) y las traduce automáticamente en un código `502 Bad Gateway` con un mensaje controlado, previniendo la caída del servidor.

## Pruebas Unitarias y de Integración con xUnit

El proyecto cuenta con pruebas unitarias y de integración que cubren la lógica de negocio, la persistencia de datos y el comportamiento de los endpoints de la API.

## Cómo Ejecutar las Pruebas

Para correr la suite de testing completa desde la consola, desde la raíz del proyecto ejecutá los siguientes comandos:

1. Limpiar artefactos previos de compilación:
   dotnet clean

2. Ejecutar todos los tests:
   dotnet test

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
