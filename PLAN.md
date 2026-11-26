> # GRUPO 4 - ZolveSolutions Help Desk

> ## 1. Resumen

> ZolveSolutions Help Desk es una aplicación web para gestionar incidencias de empresas cliente. Cada ticket pertenece a un proyecto y a una categoría, tiene prioridad y estado, y puede tener una persona asignada y comentarios. La aplicación usa C#, ASP.NET Core MVC, Razor, Identity y Entity Framework Core con SQLite.

> **Integrantes.** Alejandro Fraile del Olmo, Adrián, Juan Rafael, Ismael y Marc.

> | Rol | Qué puede hacer en el código actual | Qué debe poder hacer en el MVP |
> | --- | --- | --- |
> | Cliente (`User`) | Abre tickets y su listado muestra los que ha creado. Puede publicar comentarios con sesión iniciada, con controles de acceso pendientes. | Consulta y comenta los tickets de su empresa. Abre tickets en proyectos activos de su empresa. Solo quien abrió un ticket puede cerrarlo o reabrirlo. |
> | Técnico (`Technician`) | Su listado se filtra por asignación. Tiene acceso al formulario general de edición. | Atiende sus tickets asignados, pide información y registra la solución. |
> | Jefe de equipo (`TeamLeader`) | Existe en Identity, tiene cuenta de prueba y puede usar la edición general y los filtros. Actualmente su listado abarca todos los tickets. | Gestiona los tickets de sus proyectos. Puede asignarlos, autoasignárselos y cambiar la prioridad con un motivo. |
> | Administrador (`Admin`) | Ve todos los tickets, puede crearlos, editarlos y borrarlos, y tiene acceso al panel de usuarios y a la gestión de proyectos. | Mantiene el acceso global, asigna el rol de jefe de equipo y sus proyectos, y gestiona las categorías. |

> ## 2. Qué tenemos hecho

> | Parte | Implementación actual | Código |
> | --- | --- | --- |
> | Cuenta, perfil y usuarios | Registro, inicio y cierre de sesión, edición de perfil y avatar, cambio de contraseña y panel de administración de usuarios. La cuenta inactiva queda bloqueada para iniciar o mantener sesión. | [AccountController](../Controllers/AccountController.cs), [ProfileController](../Controllers/ProfileController.cs), [UsersController](../Controllers/UsersController.cs), [Program.cs](../Program.cs) |
> | Empresas y proyectos | Listados, fichas, alta, edición, borrado y estado activo. Se comprueba el CIF único, se bloquea borrar empresas con usuarios o proyectos y se bloquea borrar proyectos con tickets. Los proyectos guardan el nivel de soporte y las horas de respuesta. | [CompaniesController](../Controllers/CompaniesController.cs), [ProjectsController](../Controllers/ProjectsController.cs), [Project](../Models/Project.cs) |
> | Categorías | Listado con contador de tickets, detalle, alta, edición y borrado. Hay validación de nombre único y bloqueo de borrado si la categoría tiene tickets. | [CategoriesController](../Controllers/CategoriesController.cs), [vistas de categorías](../Views/Categories) |
> | Tickets | Listado, detalle, alta en estado Abierto, edición general y borrado por el administrador. Se guarda el autor y la fecha de apertura. La edición rellena `ResolvedAt` al pasar a Resuelto o Cerrado y la limpia al volver a un estado abierto o de atención. | [TicketsController](../Controllers/TicketsController.cs), [Ticket](../Models/Ticket.cs), [vistas de tickets](../Views/Tickets) |
> | Búsqueda y filtros | Búsqueda por título, descripción y proyecto. Admin y jefe también pueden buscar por ID y filtrar por estado, prioridad, empresa, proyecto, categoría y fechas. | [TicketsController.Index](../Controllers/TicketsController.cs), [listado](../Views/Tickets/Index.cshtml) |
> | Comentarios | Listado, detalle, alta desde el ticket, edición y borrado. El servidor registra autor y fecha. La edición y el borrado se limitan al autor o al administrador. El modelo distingue comentario público y nota interna. | [TicketCommentsController](../Controllers/TicketCommentsController.cs), [TicketComment](../Models/TicketComment.cs) |
> | Datos de demostración | Se crean empresas, proyectos, categorías, tickets, comentarios y cuentas de los cuatro roles. El arranque recrea SQLite y carga esos datos. | [DbInitializer](../Data/DbInitializer.cs) |

> **Verificación local.** La solución compiló sin advertencias ni errores con `dotnet build G4AspNet.sln --no-restore -m:1`. La página de inicio respondió con HTTP 200 usando una base SQLite temporal. La única prueba xUnit del repositorio pasó y cubre un resultado de subida de imágenes. Las funcionalidades de la tabla se revisaron en el código. El flujo completo del ticket con los cuatro roles queda por probar.

> ## 3. Errores y cosas a medias

> ### Errores

> **Acceso a tickets.** El listado del cliente solo muestra sus tickets, aunque el Excel pide los de toda su empresa. El jefe de equipo ve todos, aunque debería ver los de sus proyectos. El detalle y la edición carecen de una comprobación sobre el ticket concreto, y las fichas de proyectos y categorías también muestran tickets que deben quedar sujetos a esa regla.
> **Apertura de tickets.** El formulario carga todos los proyectos y el guardado carece de controles sobre pertenencia a la empresa y actividad de empresa y proyecto. El formulario también muestra la asignación al crear y el selector ofrece a todos los usuarios.
> **Comentarios y notas internas.** El listado, el detalle del comentario y la ficha del ticket carecen del filtrado necesario. La opción de nota interna está disponible en el formulario sin una comprobación de rol al guardar. El alta comprueba que el ticket existe y que hay sesión, pero queda pendiente verificar quién puede comentar ese ticket.
> **Categorías.** Las acciones de crear, editar y borrar carecen de la restricción a administrador que pide el Excel. El listado muestra esos controles sin distinguir roles.

> ### Cosas a medias

> **Jefe de equipo.** El Excel menciona `AdminTechnician`. El código actual usa `TeamLeader`, carga la cuenta `lider1` y ofrece el rol al editar un usuario. El formulario de alta sigue mostrando solo Cliente y Administrador, y falta vincular al jefe con sus proyectos.
> **Asignación, prioridad y estados.** Los campos se guardan desde una edición general. Quedan pendientes las acciones por rol, la validación del técnico activo, el motivo obligatorio al cambiar prioridad, la solución y las reglas de cierre, reapertura y espera de cliente.
> **Filtros.** El Excel los marca `In progress`. La búsqueda y los filtros avanzados ya están implementados. Falta comprobar su alcance con un jefe vinculado a proyectos concretos.
> **Desactivaciones y plazo de respuesta.** Empresas, proyectos y usuarios ya tienen estado activo. Faltan las consecuencias sobre la apertura de tickets y la asignación al desactivar técnicos. `ResponseTimeHours` ya se guarda por proyecto y queda por calcular el vencimiento y comprobar la primera respuesta.
> **Controles de la interfaz.** El listado de tickets aplica condiciones por rol a varios botones. El detalle aún muestra acciones genéricas y el formulario conserva el aviso TODO de la plantilla.

> ## 4. Qué falta de lógica de negocio para MVP

> Las acciones y los cambios de modelo de esta sección describen el trabajo pendiente.

> ### 4.1 Jefe de equipo y acceso a tickets

> **Qué hace.** Como administrador, quiero asignar jefes a proyectos para que cada jefe gestione las incidencias de su equipo.
> **Reglas.** El administrador ve todos los tickets, el jefe los de sus proyectos, el técnico los asignados y el cliente los de su empresa. Aplicar el mismo alcance al listado, al detalle, al guardado y a cualquier ficha que muestre tickets.
> **Modelo.** Reutilizar `TeamLeader`, `ApplicationUser`, `Project` y la relación del usuario con su empresa. Añadir la relación entre jefe y proyectos.
> **Controlador y acciones.** Completar la gestión del rol y los proyectos desde usuarios o proyectos. Aplicar el alcance en `TicketsController.Index`, `Details`, `Edit` y `Save`, y reutilizarlo en las pantallas relacionadas.
> **Vistas.** Configuración de proyectos del jefe y listado de tickets con filtros acotados a sus proyectos.
> **Responsables.** Alejandro, rol y vinculación. Adrián, acceso por rol. Juan, filtros.

> ### 4.2 Apertura de tickets y desactivación de empresas o proyectos

> **Qué hace.** Como cliente, quiero abrir incidencias en los proyectos activos de mi empresa para solicitar soporte.
> **Reglas.** Mostrar solo proyectos activos de la empresa del cliente. Al guardar, volver a comprobar la pertenencia y que empresa y proyecto sigan activos. Desactivar una empresa o proyecto bloquea nuevas aperturas y conserva el historial y la tramitación de tickets existentes.
> **Modelo.** `ApplicationUser.CompanyId`, `Project.CompanyId`, `Project.IsActive`, `Company.IsActive` y `Ticket.ProjectId`.
> **Controlador y acciones.** Filtrar proyectos en `TicketsController.Create` y validar en `Save`. Mantener en empresas y proyectos los controles de desactivación y las protecciones de borrado.
> **Vistas.** Selector de proyectos acotado y mensaje claro si no hay proyectos elegibles o se desactivó el seleccionado.
> **Responsable.** Adrián, con Alejandro para la parte de empresas.

> ### 4.3 Asignar tickets y justificar cambios de prioridad

> **Qué hace.** Como jefe de equipo, quiero repartir tickets y ajustar su prioridad para ordenar la atención.
> **Reglas.** El jefe solo gestiona sus proyectos. Puede asignar a un técnico activo o a sí mismo. Si cambia la prioridad, debe escribir un motivo. Guardar el cambio y el comentario juntos. Si el motivo está vacío, conservar la prioridad anterior.
> **Modelo.** `Ticket.AssignedToId`, `Ticket.Priority`, `ApplicationUser.IsActive` y `TicketComment` con el autor del cambio.
> **Controlador y acciones.** Añadir acciones de asignación y cambio de prioridad en `TicketsController`, con validación de permisos, destino y motivo en el servidor.
> **Vistas.** Controles de asignación y prioridad para el jefe, con selector de personas elegibles y campo de motivo.
> **Responsables.** Juan, asignación y prioridad. Ismael, comentario de justificación. Alejandro*, controles por rol.

> ### 4.4 Resolver, cerrar, reabrir y pedir información

> **Qué hace.** Como persona asignada, quiero pedir datos y registrar la solución. Como cliente que abrió el ticket, quiero cerrarlo o reabrirlo según el resultado.
> **Reglas.** La persona asignada explica la solución al marcar Resuelto. Solo el creador puede cerrar o reabrir. Cuando la persona asignada pida información, el ticket pasa a Espera de cliente. Una respuesta pública de un cliente de esa empresa lo devuelve a En progreso.
> **Modelo.** Reutilizar `TicketStatus`, `ResolvedAt`, `AssignedToId`, `CreatedById` y `TicketComment` para registrar la solución y las respuestas.
> **Controlador y acciones.** Añadir acciones específicas de resolución, cierre, reapertura y solicitud de información. Coordinar la reactivación con el alta de comentarios públicos.
> **Vistas.** Controles de atención y resolución para la persona asignada, cierre y reapertura para el creador, y formulario para registrar la solución o pedir datos.
> **Responsables.** Juan, resolución, cierre y reapertura. Ismael, espera y reactivación. Alejandro*, controles por rol.

> ### 4.5 Comentarios públicos y notas internas

> **Qué hace.** Como cliente, quiero conversar sobre las incidencias de mi empresa. Como miembro de soporte autorizado, quiero registrar notas internas.
> **Reglas.** Pueden publicar comentarios los clientes de la empresa, la persona asignada y el administrador. Reservar las notas internas al soporte autorizado y ocultarlas al cliente en todas las pantallas.
> **Modelo.** `TicketComment.IsInternal`, `AuthorId` y `TicketId`, junto con las relaciones de empresa y asignación del ticket.
> **Controlador y acciones.** Aplicar permisos en el listado, detalle y guardado de comentarios, y al cargar los comentarios de un ticket. Validar también quién puede cambiar la visibilidad de un comentario.
> **Vistas.** Mostrar la opción de nota interna solo a soporte autorizado y excluir esas notas de la vista del cliente.
> **Responsable.** Ismael, coordinado con Adrián para aplicar el acceso a tickets.