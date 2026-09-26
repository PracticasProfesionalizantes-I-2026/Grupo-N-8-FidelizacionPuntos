# Plan Técnico — API Fidelix en Arquitectura N-Tier (.NET)

> Paso 1 (Planificación) del prompt de arquitectura de la cátedra. Define la
> estructura base de la API y las **3 entidades principales** del sistema.
> Los casos de uso restantes se implementan después sobre esta base.

## 1. Alcance

Tres entidades principales, con la misma relación que el ejemplo de la
cátedra (Socio / Libro / Préstamo):

| Ejemplo cátedra | Fidelix | Rol |
| --- | --- | --- |
| Socio | **Cliente** | Quien participa del programa de puntos |
| Libro | **Beneficio** | Lo que se ofrece a cambio de puntos |
| Préstamo | **Movimiento** | Entidad hija que relaciona a las otras dos |

Casos de uso modelo implementados, uno de cada tipo:

- **CU-01 Registrarse** — alta con reglas de negocio.
- **CU-04 Consultar saldo** — consulta calculada sobre movimientos.
- **CU-17 ABM de beneficio** — alta, modificación y baja lógica.

## 2. Entidades

Todas heredan de `EntidadBase` (`Id: Guid`, asignado en el repositorio).

**Cliente**: `Nombre`, `Documento` (único), `Email` (único), `PasswordHash`,
`Telefono?`, `FechaNacimiento?`, `FechaRegistro`, `Activo`, `Movimientos`.

**Beneficio**: `Nombre` (único), `Descripcion?`, `CostoPuntos`,
`Categoria` (enum `CategoriaBeneficio`), `Activo`, `Canjes`.

**Movimiento**: `ClienteId` (FK), `Tipo` (enum `TipoMovimiento`: Acumulacion,
Canje, BonoCumpleanos, Vencimiento), `Puntos`, `PuntosDisponibles`, `Fecha`,
`FechaVencimiento?`, `BeneficioId?` (FK, solo en canjes), `Detalle?`.

Relaciones: `Movimiento → Cliente` y `Movimiento → Beneficio`, ambas con
`DeleteBehavior.Restrict` (las bajas son lógicas y nunca borran historial).

**Decisión: el saldo no es una columna.** Se calcula sumando
`PuntosDisponibles` de los lotes no vencidos (RN-05). Así no puede quedar
desincronizado con los vencimientos ni con los canjes.

## 3. Endpoints por capa

| Endpoint | Controller | Service | Repositorio |
| --- | --- | --- | --- |
| `POST /api/clientes/registro` | `ClientesController.Registrar` | `ClienteService.RegistrarAsync` | `IClienteRepository` |
| `GET /api/clientes/{id}/saldo` | `ClientesController.ConsultarSaldo` | `ClienteService.ConsultarSaldoAsync` | `IClienteRepository`, `IMovimientoRepository` |
| `POST /api/admin/beneficios` | `AdminBeneficiosController.Crear` | `BeneficioService.CrearAsync` | `IBeneficioRepository` |
| `PUT /api/admin/beneficios/{id}` | `AdminBeneficiosController.Actualizar` | `BeneficioService.ActualizarAsync` | `IBeneficioRepository` |
| `PATCH /api/admin/beneficios/{id}/desactivar` | `AdminBeneficiosController.Desactivar` | `BeneficioService.DesactivarAsync` | `IBeneficioRepository` |

Sin autenticación en esta etapa: se incorpora con el login (CU-02/09/14).

## 4. Reglas de negocio y excepciones tipadas

| Regla | Dónde | Excepción | HTTP |
| --- | --- | --- | --- |
| RN-01: documento y email de cliente únicos | `ClienteService` | `ClienteDuplicadoException` | 409 |
| RN-02: contraseña de al menos 8 caracteres | `ClienteService` | `PasswordInvalidaException` | 400 |
| Cliente inexistente al consultar saldo | `ClienteService` | `ClienteNotFoundException` | 404 |
| CU-04 4a: falla al leer los movimientos | `ClienteService` | `PersistenceException` | 500 |
| RN-05: los lotes vencidos no suman al saldo | `MovimientoRepository` (filtro) | — | — |
| RN-16: costo en puntos mayor a cero | `BeneficioService` | `CostoInvalidoException` | 400 |
| RN-23: nombre de beneficio único | `BeneficioService` | `BeneficioDuplicadoException` | 409 |
| Beneficio inexistente | `BeneficioService` | `BeneficioNotFoundException` | 404 |
| RN-17: la desactivación es lógica | `BeneficioService` | — | — |

Errores de sintaxis del JSON o DataAnnotations: 400 automático de
`[ApiController]`, antes de llegar al service.

## 5. Fases de desarrollo

1. **Fase 1**: `Shared/` (DTOs, excepciones, enums, `PasswordHasher`) +
   `DataAccess/` (entidades, DbContext, repositorios, migración inicial,
   `DbInitializer`).
2. **Fase 2**: `BusinessLogic/` (interfaces y services) + tests unitarios
   (xUnit + Moq).
3. **Fase 3**: `API/` (controllers) + tests de integración
   (`WebApplicationFactory`) + colección Bruno.
4. **Paso 3**: `README.md` y `AGENTS.md`.
