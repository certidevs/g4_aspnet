# ZolveSolutions - HelpDesk

Temática ya elegida: Help Desk.

Repositorio del **Grupo 4** del curso de .NET: aplicación web con **C#, ASP.NET Core MVC, Razor y Entity Framework Core**, desarrollada en equipo con Git y GitHub.

## Qué incluye el esqueleto

Punto de partida común de todos los grupos: **solo la entidad `ApplicationUser` (usuario)** y la infraestructura para que todo arranque a la primera. Las entidades del dominio, sus relaciones, servicios, controladores y vistas las construye el equipo.

| Área | Contenido |
| --- | --- |
| Usuarios | `ApplicationUser` (ASP.NET Core Identity) con nombre visible, avatar, fecha de alta y estado activo; roles `Admin` y `User`. |
| Cuenta | Registro, login por usuario o email, logout y página de acceso denegado. |
| Perfil | Consulta y edición del perfil, avatar y cambio de contraseña. |
| Administración | Panel `/Users` (solo `Admin`): alta, edición, activación y borrado de usuarios. |
| Datos | Entity Framework Core + SQLite (`App_Data/g4.db`), base recreada y datos demo al arrancar. |
| Imágenes | `ImageStorage` para guardar imágenes subidas; hoy solo avatares (añadid vuestras carpetas en `ImageFolder`). |
| Interfaz | Razor + Bootstrap 5 en local, con tema claro/oscuro. |
| Calidad y despliegue | Proyecto de tests xUnit, GitHub Actions (compilar y testear) y `Dockerfile`. |

## Requisitos

- SDK de .NET 10. `global.json` acepta cualquier SDK de la versión 10.0.
- Visual Studio Code y C# Dev Kit. Es el IDE estándar del curso en Windows, macOS y
  Linux.
- Git. Docker es opcional y solo se usa al explicar despliegue.

No instales ASP.NET, C#, SQLite ni Entity Framework por separado: los aporta el SDK o
se restauran como dependencias del proyecto.

## Instalación inicial

Instala .NET 10 SDK, Visual Studio Code y C# Dev Kit una sola vez en el ordenador.

### Windows (PowerShell)

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact
winget install --id Microsoft.VisualStudioCode --exact
winget install --id Git.Git --exact
```

Cierra y abre una terminal nueva; después instala la extensión:

```powershell
code --install-extension ms-dotnettools.csdevkit
```

### macOS (Terminal, con Homebrew)

```bash
brew install dotnet
brew install --cask visual-studio-code
brew install git
code --install-extension ms-dotnettools.csdevkit
```

Si no utilizas Homebrew, instala el SDK de .NET 10 con el instalador oficial de
[macOS](https://dotnet.microsoft.com/download/dotnet/10.0) y añade C# Dev Kit desde
el panel Extensions de VS Code.

### Ubuntu 26.04 (Terminal)

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0 git
sudo snap install code --classic
code --install-extension ms-dotnettools.csdevkit
```

Para otra distribución Linux, sigue el instalador oficial de
[.NET para Linux](https://learn.microsoft.com/dotnet/core/install/linux) y, una vez
instalado VS Code, ejecuta el último comando.

Comprueba la instalación desde esta carpeta:

```bash
dotnet --version
```

Debe mostrar una versión que empiece por `10.0`.
Si el comando `code` no se reconoce, abre VS Code e instala C# Dev Kit desde
**Extensions**.

## Primer arranque

Clona el repositorio y abre **su carpeta** (no una carpeta superior) en VS Code. C# Dev Kit carga automáticamente `G4AspNet.sln`.

```bash
git clone https://github.com/certidevs/g4_aspnet.git
cd g4_aspnet
code .
dotnet restore G4AspNet.sln
dotnet tool restore
dotnet run --project G4AspNet.csproj --launch-profile http
```

Abre `http://localhost:5107`. El primer arranque crea `App_Data/g4.db`, crea las tablas y añade los usuarios demo:

- `admin` / `Admin123!` (administrador, hace de técnico de soporte en los datos demo)
- `user` / `User123!` (cliente de TechCorp Solutions)
- `user2` / `User123!` (cliente de Innovación Digital S.L.)
- `user3` / `User123!` (cliente de Fundación Horizonte)

## Trabajo en equipo en GitHub

Todo el grupo tiene permiso de escritura en este repositorio y **se trabaja directamente en `main`**. Para no pisaros el trabajo:

1. **Haced `git pull` antes de empezar a trabajar y siempre antes de cada `git push`.**
2. **Un commit pequeño por cada cambio**, con un mensaje claro (`feat: ...`, `fix: ...`, `docs: ...`), y subidlo pronto con `git push`.
3. **No toquéis la entidad, el controlador o las vistas de otra persona sin avisarle.** En la tabla *Entidades y quién las hace* está quién hace cada cosa.
4. **No hagáis `git push --force`.**
5. Quien ya sepa usar ramas puede usarlas (una rama por tarea y fusión a `main`), pero no es obligatorio.
6. Repartid el trabajo con **Issues** (una persona por issue) y, si queréis, un tablero de **Projects**.

### Base de datos en desarrollo

Este proyecto **no usa migraciones**. Al arrancar, la aplicación borra `App_Data/g4.db`, la vuelve a crear a partir de las entidades y carga los datos demo de `Data/DbInitializer.cs` (es el equivalente a `create-drop` con H2 en Spring Boot). Por eso:

- Si cambias una entidad o una relación, **solo tienes que volver a ejecutar la aplicación**.
- Si haces `git pull` con cambios de otras personas, **solo tienes que volver a ejecutarla**.
- No hay que ejecutar `dotnet ef` ni borrar la base a mano.
- Los datos que crees a mano al probar se pierden al reiniciar. Lo que deba existir siempre se añade a `DbInitializer`.

## Entidades y quién las hace

Cada entidad tiene su controlador (en `Controllers/`) y su carpeta de vistas (en `Views/`, con el mismo nombre que el controlador). Hoy ya funcionan `Index` y `Details` de todas. **`Company` es el ejemplo completo** (crear, editar y borrar, para copiar); en el resto, `Create`, `Edit`, `Save`, `Delete` y la vista `Form` ya están escritos pero **comentados** entre los marcadores `DESCOMENTA` de cada controlador y vista: descomentadlos y ajustadlos a vuestra entidad. Los responsables son los asignados en las issues de GitHub.

| Entidad | Controlador | Vistas |
| --- | --- | --- |
| `Company` (empresa cliente) | `CompaniesController`: #10, Fraile256 | `Views/Companies`: #11, Fraile256 |
| `Project` (proyecto) | `ProjectsController`: #12, apemarr | `Views/Projects`: #13, apemarr |
| `Category` (categoría del ticket) | `CategoriesController`: #14, marcochandotrabajo-oss | `Views/Categories`: #15, marcochandotrabajo-oss |
| `Ticket` (incidencia) | `TicketsController`: #16, JuanR88 | `Views/Tickets`: #17, apemarr |
| `TicketComment` (comentario de ticket) | `TicketCommentsController`: #18, LusmaelTk | `Views/TicketComments`: #19, LusmaelTk |

Responsables de las entidades según las issues: `Company` #2 (JuanR88), `Project` #5 (apemarr), `Ticket` y `Category` #6 (JuanR88). `TicketComment` no tiene issue de entidad. Los datos demo (`Data/DbInitializer.cs`) son la issue #20 (marcochandotrabajo-oss). Las entidades de fase 2 (`TechnicianCategory` #7 y `Attachment` #21) todavía no se crean. La issue #9 (regenerar la migración) ya no hace falta: el proyecto no usa migraciones.

## Comandos de trabajo diario

Todos se ejecutan desde la raíz de este repositorio.

```bash
# Restaurar dependencias NuGet y herramientas locales tras clonar o actualizar
dotnet restore G4AspNet.sln
dotnet tool restore

# Ejecutar la aplicación
dotnet run --project G4AspNet.csproj --launch-profile http

# Ejecutar con recarga al guardar archivos
dotnet watch --project G4AspNet.csproj run --launch-profile http

# Compilar y ejecutar los tests
dotnet build G4AspNet.sln
dotnet test G4AspNet.sln
```

### Entity Framework y SQLite

La base de datos se recrea al arrancar (ver *Base de datos en desarrollo*): no hay migraciones que crear ni aplicar.

### CI y Docker

[GitHub Actions](.github/workflows/build-and-test.yml) ejecuta restauración, compilación Release y tests en cada `push` a `main` y en cada *pull request* hacia `main`.

Docker es opcional:

```bash
docker build -t g4_aspnet:local .
docker run --rm -p 10000:10000 g4_aspnet:local
```

El contenedor queda disponible en `http://localhost:10000`.

## IDEs

En VS Code, selecciona el perfil `http` y pulsa F5 para depurar. Las tareas `G4: compilar` y `G4: ejecutar tests` están disponibles en **Terminal → Run Task**. En Windows, Visual Studio 2026 también abre `G4AspNet.sln`; consulta [la guía de IDEs](docs/IDE-SETUP.md).
