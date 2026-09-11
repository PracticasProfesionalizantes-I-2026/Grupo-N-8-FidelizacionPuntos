# AGENTS.md — Fidelix API

Contexto operativo para agentes de IA (Claude Code, Copilot, etc.) que
trabajen en este directorio (`src/`). Para contexto de negocio (reglas RN,
requerimientos RF, casos de uso) ver `../Docs/`.

## Qué es esto

API RESTful en .NET 10 / ASP.NET Core, arquitectura N-Tier, para el
sistema de fidelización de puntos Fidelix. Ver `README.md` para el
catálogo completo de endpoints y el diagrama de capas.

## Comandos

```bash
dotnet build                                    # compilar toda la solución
dotnet test                                     # BusinessLogic.Tests + API.IntegrationTests
dotnet run --project API/FidelixAPI.API.csproj  # levantar la API (Scalar en /scalar/v1 en Development)

# Migraciones (siempre con --project/--startup-project explícitos,
# porque el DbContext vive en DataAccess, no en el proyecto de arranque):
dotnet ef migrations add <Nombre> \
  --project DataAccess/FidelixAPI.DataAccess.csproj \
  --startup-project API/FidelixAPI.API.csproj \
  --context FidelixDbContext --output-dir Migrations

# Colección Bruno (requiere el CLI @usebruno/cli, o la app de escritorio):
cd bruno && npx --yes @usebruno/cli run . -r --env Local
```

## Convención de capas (obligatoria, no negociable)

`Controller → Service → Repository → DbContext`. Nunca te saltees un
escalón:

- Los **controllers** (`API/Controllers/`) no acceden a datos ni contienen
  lógica de negocio. Cada acción valida el DTO (atributos `[Required]` /
  `[ApiController]` hacen la mayor parte), llama a un service, y traduce
  las excepciones tipadas a códigos HTTP con **try/catch explícito** (no
  hay middleware global de excepciones — es una decisión pedagógica de la
  cátedra, mantenerla).
- Los **services** (`BusinessLogic/Services/`) contienen toda la
  validación y las reglas de negocio (RN-XX). Nunca escriben
  LINQ-to-entities directamente: siempre a través de un `I...Repository`
  inyectado por constructor. Nunca devuelven entidades de `DataAccess`:
  siempre DTOs de `Shared/DTOs/`, mapeados con un método privado
  `MapToResponseDTO` al final de cada archivo de service.
- Los **repositorios** (`DataAccess/Repositories/`) son el único lugar que
  toca `FidelixDbContext`. Las lecturas usan `AsNoTracking()` salvo que el
  caller vaya a mutar y guardar la misma instancia (ver el patrón
  "fetch con tracking → mutar en memoria → un solo `UpdateAsync`/
  `SaveChangesAsync`" en `PuntosService.DescontarPuntosFifoAsync`).

## Excepciones tipadas

Cada regla de negocio violada lanza una excepción propia de
`Shared/Exceptions/` (una clase por archivo, constructor primario:
`public class XException(string message) : Exception(message);`). Antes
de crear una excepción nueva, revisar si ya existe una que aplique — el
catálogo completo con su HTTP code está en
`../Docs/Business/Plan-Tecnico-API-Fase1.md` (sección 3).

## Al agregar una entidad o campo nuevo

1. Modificar la entidad en `DataAccess/Entities/`.
2. `dotnet ef migrations add <NombreDescriptivo> ...` (comando arriba).
3. Si el campo se expone por API, actualizar el/los DTO correspondientes
   en `Shared/DTOs/` y el `MapToResponseDTO` del service.
4. Si `DbInitializer` siembra esa entidad, actualizar el seed.
5. Actualizar el caso de uso correspondiente en `../Docs/Use cases/` si el
   cambio no estaba contemplado ahí (ver el historial de este mismo
   archivo: `Movimiento.PuntosDisponibles`, `CuentaConCredenciales`,
   `ReglaAcumulacion.DiasVigenciaPuntos` y la entidad `Auditoria` se
   agregaron así, sobre la marcha, documentando el motivo en
   `Plan-Tecnico-API-Fase1.md`).

## Tests

- **`BusinessLogic.Tests`** (xUnit + Moq): un test por flujo principal y
  por excepción típica de cada service, mockeando los repositorios. Nunca
  tocan una base de datos real.
- **`API.IntegrationTests`** (`WebApplicationFactory<Program>`): contra el
  pipeline HTTP real, con una base SQLite en memoria propia por clase de
  test (`FidelixApiFactory`, conexión `:memory:` mantenida abierta). Para
  autenticar en un test, usar el helper `factory.ClienteAutenticadoAsync(email, password)`.

`Program.cs` termina con `public partial class Program;` — no lo borres:
es lo que le permite a `WebApplicationFactory<Program>` referenciar la
app de top-level statements.

## Colección Bruno (`bruno/`)

Un `.bru` por endpoint, con caso de éxito y al menos un caso de error.
Los requests encadenan tokens/IDs entre sí vía `bru.setVar` — ver
`bruno/README.md` para el orden de ejecución. Al agregar un endpoint
nuevo, agregar también su request (y su caso de error más relevante) acá.

## Pendiente para una fase posterior

- Programar los jobs de Sistema (`PuntosService.AplicarVencimientoAsync`
  CU-22, `AplicarBonoCumpleanosAsync` CU-26) con un scheduler real
  (`IHostedService` + `PeriodicTimer`, o Hangfire/Quartz).
- Reemplazar `LoggingEmailSender` (solo loguea) por un proveedor de email
  real para CU-25, implementando `IEmailSender`.
- Paginación en las consultas admin (`CU-20`, `CU-21`) si el volumen de
  datos lo justifica.
