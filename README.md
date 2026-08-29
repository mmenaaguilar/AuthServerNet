# AuthServerNet

Servidor de autenticación basado en **.NET 10** que proporciona autenticación mediante correo electrónico/contraseña y OAuth con Google y GitHub, utilizando **JWT** para la gestión de sesiones.

## 🚀 Tecnologías

- **.NET 10** — Framework de desarrollo
- **ASP.NET Core Identity** — Gestión de usuarios y roles
- **JWT Bearer Authentication** — Tokens de autenticación
- **Entity Framework Core** — ORM con SQL Server
- **Docker Compose** — Contenedor de SQL Server
- **Swagger** — Documentación interactiva de la API

## 📋 Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- [Docker](https://www.docker.com/products/docker-desktop/) (para ejecutar SQL Server en contenedor)

## 🛠️ Instalación

1. Clona el repositorio:
```bash
git clone <url-del-repositorio>
cd AuthServerNet
```

2. Levanta SQL Server con Docker Compose:
```bash
docker-compose up -d
```

3. Aplica las migraciones de la base de datos:
```bash
dotnet ef database update --project AuthServer.API
```

4. Ejecuta el proyecto:
```bash
dotnet run --project AuthServer.API
```

La API estará disponible en `http://localhost:5260` (o el puerto configurado).

## 📚 Uso de la API

La documentación interactiva está disponible en **Swagger** cuando se ejecuta en modo desarrollo:
```
http://localhost:5260/swagger
```

### Endpoints principales

#### Auth — Correo y contraseña

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/auth/registro` | Registra un nuevo usuario |
| `POST` | `/api/auth/login` | Autentica y retorna un JWT |

**Registro** (`POST /api/auth/registro`)
```json
{
  "nombreCompleto": "Juan Pérez",
  "email": "juan@example.com",
  "password": "Contraseña123"
}
```

**Login** (`POST /api/auth/login`)
```json
{
  "email": "juan@example.com",
  "password": "Contraseña123"
}
```

Respuesta exitosa:
```json
{
  "mensaje": "Login exitoso",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "juan@example.com",
  "nombre": "Juan Pérez"
}
```

#### OAuth — Google y GitHub

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/api/oauth/google/login?redirectUrl=...` | Inicia login con Google |
| `GET` | `/api/oauth/github/login?redirectUrl=...` | Inicia login con GitHub |
| `GET` | `/api/oauth/google/callback` | Callback de Google |
| `GET` | `/api/oauth/github/callback` | Callback de GitHub |

Redirige al proveedor OAuth y, tras la autenticación, redirige al `redirectUrl` con el token JWT y el email como parámetros de query.

#### Perfil

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/api/perfil/perfil` | Retorna la información del usuario autenticado |

Requiere el header `Authorization: Bearer <token>`.

Respuesta:
```json
{
  "mensaje": "¡Acceso autorizado exitosamente!",
  "datosToken": {
    "id": "...",
    "email": "juan@example.com"
  },
  "datosUsuario": {
    "nombreCompleto": "Juan Pérez",
    "fechaCreacion": "2026-08-28T00:00:00Z"
  }
}
```

## ⚙️ Configuración

La configuración se encuentra en `AuthServer.API/appsettings.json`:

| Clave | Descripción |
|-------|-------------|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a SQL Server |
| `JwtSettings:SecretKey` | Clave secreta para firmar los JWT |
| `JwtSettings:Issuer` | Emisor del token |
| `JwtSettings:Audience` | Audiencia del token |
| `JwtSettings:ExpirationInMinutes` | Tiempo de expiración del token |
| `Authentication:Google:ClientId` / `ClientSecret` | Credenciales de OAuth de Google |
| `Authentication:GitHub:ClientId` / `ClientSecret` | Credenciales de OAuth de GitHub |

> **Nota:** Los `appsettings.json` y `appsettings.Development.json` están ignorados por git. Copia `appsettings.Development.json` y configura tus propios valores.

## 🐳 Docker Compose

El archivo `docker-compose.yml` levanta un servidor **SQL Server 2022** en el puerto `1433` con las siguientes credenciales por defecto:

- **Usuario:** `sa`
- **Contraseña:** `PrinceLOG4`
- **Base de datos:** `AuthServerDb`

## 📁 Estructura del proyecto

```
AuthServerNet/
├── AuthServer.API/
│   ├── Controllers/       # Controladores de la API
│   ├── Models/            # Modelos de dominio
│   ├── DTOs/              # Objetos de transferencia de datos
│   ├── Services/          # Servicios de lógica de negocio
│   ├── Data/              # Contexto de base de datos
│   ├── Migrations/        # Migraciones de EF Core
│   └── appsettings.json   # Configuración
├── docker-compose.yml     # Infraestructura de SQL Server
└── README.md
```

## 🔐 Flujo de autenticación

1. **Registro / Login** — El usuario se registra o inicia sesión con email y contraseña, recibiendo un JWT con expiración de 60 minutos.
2. **OAuth** — El usuario autentica con Google o GitHub, se le crea o vincula una cuenta local y recibe un JWT.
3. **Acceso a recursos protegidos** — Se incluye el JWT en el header `Authorization: Bearer <token>` para acceder a endpoints protegidos como `/api/perfil/perfil`.

## 📝 Notas

- La política CORS permite cualquier origen (`AllowAll`) para facilitar el desarrollo frontend.
- Las contraseñas requieren al menos 6 caracteres con un dígito.
- Los secretos de OAuth están almacenados en `appsettings.json` para desarrollo; en producción, migrar a variables de entorno o un gestor de secretos.
- Los `appsettings.json` están en `.gitignore` — no commits de credenciales.
