# AGENTS.md — Fidelix API

Contexto operativo para agentes de IA (Claude Code, Codex, Copilot, etc.)
que trabajen en este directorio (`src/`). Para contexto de negocio (reglas
RN, requerimientos RF, casos de uso) ver `../Docs/`.

## ⚠️ Regla principal: trabajo académico

Fidelix es un trabajo de facultad (Desarrollo de Software, ICES). La
estructura inicial y los casos de uso modelo **CU-01, CU-04 y CU-17** se
generaron con el prompt de la cátedra; **todos los demás casos de uso los
escribe el alumno**.

- **No implementes casos de uso nuevos** (ni services, controllers,
  repositorios, entidades o tests de otros CU), aunque parezca lo más rápido.
- El rol del agente es **guiar**: explicar conceptos, señalar en qué CU
  modelo inspirarse, revisar el código que escribe el alumno y marcar
  errores o faltantes. Fragmentos chicos solo para ilustrar sintaxis o una
  API del framework, nunca el caso de uso armado.
- Si un pedido implica escribir un CU completo, frenar y preguntar.

## Qué es esto

API RESTful en .NET 10 / ASP.NET Core, arquitectura N-Tier, para el sistema
de fidelización de puntos Fidelix. Hoy cubre 3 entidades (`Cliente`,
`Beneficio`, `Movimiento`) y 3 casos de uso. Ver `README.md` para el
catálogo de endpoints.

## Comandos

```bash
dotnet build                                    # compilar toda la solución
dotnet test                                     # BusinessLogic.Tests + API.IntegrationTests
dotnet run --project API/FidelixAPI.API.csproj  # levantar la API (Scalar en /scalar/v1 en Development)

# Migraciones (el DbContext vive en DataAccess, no en el proyecto de arranque):
dotnet ef migrations add <Nombre> \
  --project DataAccess/FidelixAPI.DataAccess.csproj \
  --startup-project API/FidelixAPI.API.csproj \
  --output-dir Migrations

# Colección Bruno (CLI @usebruno/cli, o la app de escritorio), con la API levantada:
cd bruno && npx --yes @usebruno/cli run -r --env Local
```

## Convención de capas (obligatoria)

`Controller → Service → Repository → DbContext`. Nunca saltear un escalón.

- **Controllers** (`API/Controllers/`): no acceden a datos ni tienen
  lógica de negocio. La sintaxis del DTO la valida `[ApiController]` con
  DataAnnotations (400 automático); la acción llama a un service y traduce
  las excepciones tipadas a HTTP con **try/catch explícito** (no hay
  middleware global de excepciones: es una decisión de la cátedra).
- **Services** (`BusinessLogic/Services/`): toda la validación y las
  reglas de negocio (RN-XX). No usan LINQ-to-entities: acceden a datos solo
  a través de un `I...Repository` inyectado por constructor. Nunca
  devuelven entidades: siempre DTOs de `Shared/DTOs/`, mapeados con un
  método privado `MapToResponseDTO`.
- **Repositorios** (`DataAccess/Repositories/`): único lugar que toca
  `FidelixDbContext`. Asignan el `Guid` en `CreateAsync`. Lecturas de solo
  lectura con `AsNoTracking()`. Devuelven `null` si no encuentran algo: el
  service decide si eso es un error.
- **DbContext**: relaciones hijas con `DeleteBehavior.Restrict` (las bajas
  del dominio son lógicas, `Activo = false`) e índices únicos para las
  reglas de unicidad.

## Excepciones tipadas

Una clase por archivo en `Shared/Exceptions/`, con constructor primario
(`public class XException(string message) : Exception(message);`) y un
comentario que diga a qué código HTTP se mapea:

| Tipo de error | Código | Ejemplos |
| --- | --- | --- |
| No encontrado | 404 | `ClienteNotFoundException`, `BeneficioNotFoundException` |
| Validación de negocio | 400 | `PasswordInvalidaException`, `CostoInvalidoException` |
| Conflicto / duplicado | 409 | `ClienteDuplicadoException`, `BeneficioDuplicadoException` |
| Falla de persistencia | 500 | `PersistenceException` |

## Convenciones de código

- **Cada función/método lleva un comentario XML (`/// <summary>`)** que
  explique qué hace, incluidos los tests y los métodos privados.
- Nombres de tests según la matriz de trazabilidad de cada CU en
  `../Docs/Use cases/` (ej. `RegistrarClienteAsync_WithWeakPassword_ThrowsPasswordInvalidaException`).
- Cada flujo del CU (principal y alternativos) tiene al menos un test
  unitario (xUnit + Moq, sin base de datos) y uno de integración
  (`WebApplicationFactory`, SQLite en memoria).

## Pendientes conocidos (a resolver por el alumno)

- **Autenticación**: no hay JWT todavía. Cuando se implemente el login
  (CU-02/09/14) hay que proteger `AdminBeneficiosController` con
  `[Authorize(Roles = "Admin")]` y pasar el saldo a
  `GET api/clientes/me/saldo` (Id del cliente tomado del token).
