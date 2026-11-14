# Issues y Deuda Técnica Pendiente (Backlog GitHub)

Este documento contiene el listado priorizado de hallazgos detectados durante la auditoría integral (Documentación, Casos de Uso, Arquitectura y Código Fuente). Se encuentra estructurado con formato checklist (`- [ ]`) listo para ser incorporado al gestor de issues de GitHub.

---

### 🔴 RIESGO ALTO (Crítico para la ejecución y completitud core)
*Incluye bugs bloqueantes, vulnerabilidades de seguridad graves y requerimientos principales o casos de uso core no implementados.*

- [x] **[BUG-CRIT-01] [Concurrencia] Prevenir Doble Gasto (Double Spending) en Canjes de Puntos Concurrentes**
  - **Módulo / Archivo:** `src/BusinessLogic/Services/CanjeService.cs` (Líneas 76–114) y `src/DataAccess/Entities/Movimiento.cs`
  - **Descripción:** La lectura de saldo disponible y el descuento FIFO de lotes se calculan en memoria antes de persistir (`CreateAsync`). Si ingresan dos solicitudes de canje simultáneas para el mismo cliente (ej. doble clic o terminales concurrentes), ambas validan saldo positivo y descuentan puntos sobre las mismas instancias sin control de concurrencia optimista ni bloqueo transaccional.
  - **Acción Correctiva:** Agregar un token de concurrencia optimista en la entidad `Movimiento` (ej. `[ConcurrencyCheck] public int PuntosDisponibles { get; set; }` o una columna `RowVersion`/`xmin` en base de datos) para que EF Core lance `DbUpdateConcurrencyException` ante escrituras simultáneas, o implementar un bloqueo de fila/pesimista explícito en la base de datos durante la transacción de canje.
  - **✅ Resuelto en:** commit `7c6554a` — `Movimiento.PuntosDisponibles` es token de concurrencia optimista; el segundo canje simultáneo se revierte entero y responde 409 (CU-07, flujo 5b).

- [x] **[BUG-CRIT-02] [Integridad / Batch] Evitar Corrupción del ChangeTracker de EF Core ante Excepciones en `AplicarVencimientoAsync`**
  - **Módulo / Archivo:** `src/BusinessLogic/Services/PuntosService.cs` (Líneas 50–86) y `src/DataAccess/Repositories/MovimientoRepository.cs`
  - **Descripción:** Cuando se procesan lotes vencidos agrupados por cliente en el bucle diario, si la llamada a `movimientoRepository.CreateAsync` arroja una excepción, el bloque `catch` restaura los valores en memoria pero la entidad `Movimiento` ya quedó agregada (`EntityState.Added`) al `ChangeTracker` del `DbContext`. En la siguiente iteración exitosa, `SaveChangesAsync()` intentará guardar tanto el nuevo movimiento como el que falló del cliente anterior, causando inconsistencias o fallas en cadena.
  - **Acción Correctiva:** En el bloque `catch`, desasociar explícitamente la entidad fallida del contexto (`context.Entry(entity).State = EntityState.Detached`) o, preferentemente, crear un `IServiceScope` independiente por cada cliente procesado en la tarea en segundo plano.
  - **✅ Resuelto en:** commit `a8ec1f7` — `IUnidadDeTrabajo.DescartarCambios()` desacopla lo `Added` y devuelve lo `Modified` a sus valores originales ante un error; se aplica en vencimiento y bono de cumpleaños.

- [x] **[BUG-CRIT-03] [Escalabilidad / OOM] Eliminar Carga Masiva de Toda la Tabla de Clientes en Memoria en Bono de Cumpleaños**
  - **Módulo / Archivo:** `src/BusinessLogic/Services/PuntosService.cs` (Líneas 99–101)
  - **Descripción:** El método `AplicarBonoCumpleanosAsync` ejecuta `await clienteRepository.GetAllAsync()`, trayendo a memoria RAM todos los registros de clientes de la base de datos para evaluarlos con LINQ to Objects (`CumpleAnios(...)`). En un entorno productivo con miles o millones de clientes, esto provocará saturación de GC, picos de memoria y eventuales caídas por `OutOfMemoryException`.
  - **Acción Correctiva:** Crear un método especializado en `IClienteRepository` (ej. `GetCumpleanerosDelDiaAsync(int mes, int dia)`) que realice el filtrado por fecha directamente en el motor de base de datos SQL o mediante paginación por lotes (*batch chunking*).
  - **✅ Resuelto en:** commit `f791078` — `IClienteRepository.GetActivosNacidosElAsync(mes, dia)` filtra en SQL; la regla del 29/2 queda en el service.

- [x] **[BUG-CRIT-04] [Seguridad] Implementar Rate Limiting y Bloqueo por Intentos en Validación de Código de Recuperación**
  - **Módulo / Archivo:** `src/BusinessLogic/Services/RecuperacionService.cs` (Líneas 64–67) y `src/API/Controllers/AuthPersonalController.cs` (Líneas 55–70)
  - **Descripción:** El código de recuperación es un PIN numérico de 6 dígitos (1.000.000 combinaciones) válido por 15 minutos. El endpoint de confirmación (`POST /api/auth/recuperar-contrasena/confirmar`) no posee límite de intentos fallidos, demora progresiva ni rate limiting. Un atacante puede ejecutar ataques automatizados de fuerza bruta para adivinar el código y secuestrar cualquier cuenta (incluyendo administradores).
  - **Acción Correctiva:** Agregar un contador `IntentosFallidos` en la entidad `CodigoRecuperacion`, invalidar el código (`Usado = true`) al alcanzar 3 intentos fallidos y configurar middleware de rate limiting (`Microsoft.AspNetCore.RateLimiting`) sobre el endpoint público.
  - **✅ Resuelto en:** commit `7f5fd8e` — `CodigoRecuperacion.IntentosFallidos` (máx. 3, luego se invalida) + rate limiting por IP con respuesta 429.

- [x] **[BUG-CRIT-05] [Seguridad / DevSecOps] Externalizar Clave Secreta JWT de los Archivos de Configuración**
  - **Módulo / Archivo:** `src/API/appsettings.json` (Línea 13) y `src/API/Program.cs`
  - **Descripción:** La clave criptográfica para la firma de tokens JWT (`"EstaEsLaClaveSecretaParaElJwt..."`) se encuentra escrita en texto plano y versionada en el repositorio. Cualquier usuario con acceso de lectura puede falsificar tokens con rol `Admin` y vulnerar completamente el sistema.
  - **Acción Correctiva:** Remover la clave secreta de `appsettings.json`. Configurar la obtención de la clave a través de variables de entorno del sistema (`JWT__KEY`) o herramientas seguras de gestión de secretos (User Secrets en desarrollo local, Azure Key Vault o AWS Secrets Manager en producción), validando que la aplicación no inicie si la clave no cumple con entropía y longitud mínima.
  - **✅ Resuelto en:** commit `a462b02` — La clave se carga con User Secrets / variable `Jwt__Key`; la API no arranca si falta o tiene menos de 32 bytes.

- [x] **[BUG-CRIT-06] [Seguridad / Identidad] Resolver Conflicto de Recuperación ante Emails Compartidos entre Roles**
  - **Módulo / Archivo:** `src/BusinessLogic/Services/RecuperacionService.cs` (Líneas 86–92)
  - **Descripción:** Las tablas `Clientes`, `Empleados` y `Admins` validan unicidad de email de manera aislada. En `BuscarCuentaActivaAsync`, si un usuario utiliza el mismo email para su cuenta personal de `Cliente` y su cuenta laboral de `Empleado`, la evaluación secuencial siempre devuelve `Roles.Cliente`. La cuenta de `Empleado` queda imposibilitada de restablecer su clave.
  - **Acción Correctiva:** Requerir el `Rol` como campo en el payload de `RecuperacionSolicitudDTO` para resolver la cuenta exacta, o validar la unicidad global de direcciones de correo electrónico entre todos los roles del sistema en el momento de creación/edición.
  - **✅ Resuelto en:** commit `abac58f` — La solicitud de recuperación pide el `Rol` (un empleado puede ser cliente con el mismo email).

---

### 🟡 RIESGO MEDIO (Mantenibilidad y Flujos Secundarios)
*Incluye requerimientos secundarios faltantes, brechas de lógica diseño vs código, o code smells severos (ej. clases Dios, fuerte acoplamiento).*

- [ ] **[SEC-GAP-01] [Seguridad] Implementar Política de Bloqueo por Fuerza Bruta en Logins de Empleados y Administradores**
  - **Módulo / Archivo:** `src/BusinessLogic/Services/AuthPersonalService.cs` (Líneas 22–58) y `Docs/Use cases/cu-09-iniciar-sesion.md` / `cu-14-iniciar-sesion.md`
  - **Descripción:** La regla de negocio `RN-03` (bloqueo tras intentos fallidos) solo fue implementada para cuentas de Clientes en `AuthService.cs`. Las cuentas de `Empleado` y `Admin` permiten intentos infinitos sin bloqueo, dejando desprotegidas las cuentas con mayores privilegios del sistema.
  - **Acción Correctiva:** Extender las columnas `IntentosFallidos` y `BloqueadoHasta` a las entidades `Empleado` y `Admin`, e implementar la lógica de bloqueo temporal proporcional en `AuthPersonalService.cs`.

- [ ] **[DOC-GAP-01] [Diseño / Casos de Uso] Unificar Casos de Uso Fragmentados de Autenticación y Consulta de Canjes**
  - **Módulo / Archivo:** `Docs/Use cases/` (`cu-02`, `cu-09`, `cu-14` y `cu-08`, `cu-13`)
  - **Descripción:** El inicio de sesión está triplicado en tres CUs independientes (`CU-02`, `CU-09`, `CU-14`) contraviniendo el estándar formal de casos de uso (mismo objetivo con variaciones de rol). Igualmente, la consulta de canjes está duplicada en `CU-08` y `CU-13`. En contraste, `CU-25` sí fue unificado correctamente para los 3 roles.
  - **Acción Correctiva:** Reestructurar `CU-02` como un caso de uso unificado de inicio de sesión parametrizado por actor y roles. Readecuar `CU-23 (FIFO)` como una especificación de regla de negocio (`RN-09`) o relación `«include»` en lugar de un caso de uso independiente.

- [ ] **[DOC-GAP-02] [Diseño / Requerimientos] Formalizar Requerimiento Funcional de Canje Presencial y Entrega Física**
  - **Módulo / Archivo:** `Docs/Business/Documentacion-Fidelix.md` (RF-25) y `Docs/Use cases/cu-11-gestionar-canje-de-beneficios.md`
  - **Descripción:** El requerimiento `RF-25` solo contempla la acción de *"visualizar los canjes realizados"*. Sin embargo, el caso de uso `CU-11` y los endpoints de `CanjesController` implementan la entrega transaccional de vouchers y canjes en mostrador.
  - **Acción Correctiva:** Actualizar `Documentacion-Fidelix.md` agregando o corrigiendo `RF-25` para reflejar explícitamente el "Canje presencial en mostrador y registro de entrega de beneficios".

- [ ] **[DOC-GAP-03] [Diseño / Prototipo] Depurar Discrepancias del Prototipo HTML (Pantallas Fantasma de Categorías y Dashboard)**
  - **Módulo / Archivo:** `Docs/Prototipo/index.html` (Líneas de vistas `categorias`, `catNew`, `catEdit` y `dashboardAdmin`)
  - **Descripción:** El prototipo navegable contiene pantallas funcionales para administrar categorías dinámicas de productos, mientras que en el backend y base de datos las categorías son constantes fijas (Enums) y `Producto` no tiene categoría. Además, incluye un dashboard administrativo sin respaldo en la especificación de RFs.
  - **Acción Correctiva:** Alinear el prototipo eliminando las vistas no soportadas de ABM de categorías de productos o, alternativamente, formalizar los casos de uso y modelos de datos necesarios en el backend si se decide mantener la funcionalidad.

- [ ] **[CODE-SMELL-01] [Rendimiento / DB] Eliminar Consulta N+1 en Bucle de Asignación de Bono de Cumpleaños**
  - **Módulo / Archivo:** `src/BusinessLogic/Services/PuntosService.cs` (Líneas 114–143)
  - **Descripción:** En `AplicarBonoCumpleanosAsync`, por cada cliente que cumple años se realizan tres viajes de red secuenciales a la base de datos (`ExisteBonoEnAnioAsync`, `CreateAsync` de movimiento y persistencia de auditoría). Si hay 200 cumpleañeros, se emiten 600 queries individuales.
  - **Acción Correctiva:** Reemplazar el bucle individual por consultas en lote: verificar qué clientes ya tienen bono en una sola consulta (`WHERE ClienteId IN (...)`) e insertar los nuevos movimientos en bloque mediante `AddRangeAsync`.

- [x] **[CODE-SMELL-02] [Arquitectura / Repositorio] Subsanar Efecto Colateral Engañoso en `UpdateAsync` de Códigos de Recuperación**
  - **Módulo / Archivo:** `src/BusinessLogic/Services/RecuperacionService.cs` (Líneas 74–81) y `src/DataAccess/Repositories/CodigoRecuperacionRepository.cs`
  - **Descripción:** El método de servicio modifica en memoria una colección completa de códigos previos (`foreach (var c in vigentes) c.Usado = true;`), pero luego invoca `await codigoRepository.UpdateAsync(codigo)` pasando una sola entidad. EF Core persiste todo por el `ChangeTracker`, pero la firma del repositorio oculta la mutación masiva.
  - **Acción Correctiva:** Exponer un método formal `UpdateRangoAsync(IEnumerable<CodigoRecuperacion> codigos)` en la interfaz del repositorio para mantener la transparencia en la manipulación de datos.
  - **✅ Resuelto en:** commit `7f5fd8e` — Resuelto junto con BUG-CRIT-04: `ICodigoRecuperacionRepository.UpdateRangoAsync`.

- [ ] **[ROBUST-01] [Resiliencia HTTP] Estandarizar Try/Catch en Endpoints de Lectura de Controladores**
  - **Módulo / Archivo:** `src/API/Controllers/ClientesController.cs` (Líneas 143–158), `AdminMovimientosController.cs` (Líneas 16–18) y `AdminAuditoriaController.cs` (Líneas 19–20)
  - **Descripción:** Diversos endpoints de consulta llaman al servicio directamente sin bloque `try / catch`. Si ocurre una indisponibilidad de la base de datos (`PersistenceException`), ASP.NET Core retorna un error HTTP 500 no estructurado, violando el formato unificado `ErrorResponseDTO` acordado para la API.
  - **Acción Correctiva:** Homogeneizar los controladores envolviendo la llamada al servicio con el bloque `try/catch` de `PersistenceException` o implementar un `IExceptionFilter` / Middleware global de errores que unifique el contrato de respuesta.

- [ ] **[PERF-01] [Rendimiento / DB] Incorporar Paginación en Consultas de Listados Masivos Administrativos**
  - **Módulo / Archivo:** Repositorios y Controladores en `AdminClientesController`, `AdminEmpleadosController`, `AdminProductosController`, `AdminBeneficiosController` y `AdminMovimientosController`
  - **Descripción:** Todos los endpoints de listado administrativo ejecutan `GetAllAsync()` retornando la totalidad de registros sin parámetros de paginación (`skip`, `take`, `pagina`, `tamanoPagina`).
  - **Acción Correctiva:** Incorporar parámetros de paginación opcionales en los DTOs de consulta y aplicar `.Skip((page - 1) * pageSize).Take(pageSize)` en los repositorios de Entity Framework Core.

---

### 🟢 RIESGO BAJO (Deuda técnica y Mejoras Menores)
*Incluye casos borde, validaciones faltantes, code smells menores y convenciones de formato/nomenclatura.*

- [ ] **[BUG-MIN-01] [Auditoría] Capturar `EntidadId` en Bajas Voluntarias de Cliente sin Payload**
  - **Módulo / Archivo:** `src/API/Filters/AuditoriaFilter.cs` (Líneas 55–66) y `src/API/Controllers/ClientesController.cs`
  - **Descripción:** `AuditoriaFilter.ObtenerEntidadId` busca un parámetro `"id"` en la ruta o un objeto en la respuesta. Al darse de baja el cliente autenticado (`DELETE /api/clientes/me`), el ID proviene del token y la acción responde `204 NoContent`. Como resultado, la auditoría se guarda con `EntidadId = null`.
  - **Acción Correctiva:** En `AuditoriaFilter`, si la ruta coincide con un endpoint `/me` y no hay ID en la respuesta, tomar el ID del claim `NameIdentifier` del usuario autenticado.

- [ ] **[BUG-MIN-02] [Validación] Rechazar Strings Compuestos Exclusivamente por Espacios en Blanco en Registro**
  - **Módulo / Archivo:** `src/Shared/DTOs/Cliente/ClienteCreateDTO.cs` (Líneas 16–23)
  - **Descripción:** El atributo `[Required]` en `ClienteCreateDTO` permite registrar nombres con solo espacios (`"   "`). En cambio, `ClienteUpdateDTO` ya cuenta con `[RegularExpression(@".*\S.*")]`.
  - **Acción Correctiva:** Agregar la expresión regular `[RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede estar vacío ni contener solo espacios")]` al DTO de creación.

- [ ] **[DOC-GAP-04] [Documentación] Corregir la Numeración y Título de Reglas de Negocio en la Documentación Funcional**
  - **Módulo / Archivo:** `Docs/Business/Documentacion-Fidelix.md` (Sección 3.2, Reglas de Negocio)
  - **Descripción:** La numeración de reglas de negocio salta de `RN-21` (vencimiento de puntos) a `RN-23` (nombre único de beneficio), omitiendo por completo el identificador `RN-22`.
  - **Acción Correctiva:** Renumerar o registrar formalmente la regla `RN-22` para mantener correlatividad en la matriz de trazabilidad documental.

- [ ] **[DOC-GAP-05] [Casos de Uso] Completar la Fila de Requerimientos No Funcionales en las Plantillas de Casos de Uso**
  - **Módulo / Archivo:** `Docs/Use cases/` (`cu-01.md` y `cu-03.md` a `cu-26.md`)
  - **Descripción:** 25 de los 26 casos de uso no incluyeron la fila "Requerimientos no funcionales" exigida en la guía oficial de especificación (`GUIA-Especificacion-Casos-de-Uso.md`), omitiendo referencias a latencia (RNF-02) y disponibilidad.
  - **Acción Correctiva:** Incorporar en la tabla de encabezado de cada caso de uso los RNF aplicables (ej. RNF-01 Seguridad, RNF-02 Latencia < 2s).

- [ ] **[SMELL-03] [Diseño / DTO] Eliminar Redundancia de Clases en DTOs de Creación de Canjes**
  - **Módulo / Archivo:** `src/Shared/DTOs/Canje/CanjeCreateDTO.cs` y `CanjePresencialCreateDTO.cs`
  - **Descripción:** Ambas clases definen exactamente el mismo campo `public int? BeneficioId { get; set; }` con idénticas validaciones por DataAnnotations.
  - **Acción Correctiva:** Unificar en un solo DTO o hacer que `CanjePresencialCreateDTO` herede de `CanjeCreateDTO`.

- [ ] **[SMELL-04] [Optimización de Memoria] Implementar Streaming Directo en Generación de Reportes CSV**
  - **Módulo / Archivo:** `src/API/Formatters/ReporteCsvWriter.cs` (Líneas 22–49)
  - **Descripción:** La generación del archivo CSV acumula todo el contenido en un `StringBuilder` y ejecuta `.ToArray()` sobre un `byte[]` en memoria antes de enviar la respuesta HTTP.
  - **Acción Correctiva:** Escribir directamente sobre el stream de la respuesta HTTP (`context.Response.BodyWriter` o `StreamWriter`) para procesar grandes volúmenes sin sobrecarga de memoria en el servidor.

