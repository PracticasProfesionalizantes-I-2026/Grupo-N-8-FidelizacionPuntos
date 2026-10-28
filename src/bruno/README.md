# Colección Bruno — Fidelix API

Abrir esta carpeta (`src/bruno`) directamente en Bruno como colección, y
seleccionar el entorno **Local** (`http://localhost:5106`, ajustar el puerto
si `dotnet run` levanta en otro).

## Antes de empezar

1. Con la API apagada, borrar `src/API/fidelix.db` para arrancar con los datos
   de prueba completos (el seed solo corre sobre una base vacía).
2. Levantar la API (`dotnet run --project API` desde `src`).
3. Correr las carpetas **en este orden** (clic derecho → "Run"), porque los
   logins guardan los tokens que usan las demás:

| Orden | Carpeta | Casos de uso | Token que usa |
| --- | --- | --- | --- |
| 1 | **Auth** | CU-02, CU-09, CU-14, CU-25 | — (guarda `tokenCliente`, `tokenAdmin`, `tokenEmpleado`) |
| 2 | **Clientes** | CU-01, CU-03, CU-04, CU-05, CU-08, CU-12, CU-13 | `tokenCliente` (y `tokenEmpleado` en los del empleado) |
| 3 | **Admin** | CU-15, CU-16, CU-17, CU-18, CU-19, CU-20, CU-21, CU-22, CU-24, CU-26 | `tokenAdmin` (heredado de la carpeta) |
| 4 | **Movimientos** | CU-10 (N1 Gold x1.5, N2 pendientes) | `tokenEmpleado` |
| 5 | **Canjes** | CU-06, CU-07 (FIFO), CU-11, RN-10 | `tokenCliente` / `tokenEmpleado` |

## Cuentas de prueba (seed)

| Rol | Email | Contraseña |
| --- | --- | --- |
| Cliente (150 puntos) | `cliente.demo@fidelix.local` | `Cliente123!` |
| Cliente Gold | `cliente.gold@fidelix.local` | `Gold1234!` |
| Cliente inactivo | `cliente.inactivo@fidelix.local` | `Inactivo123!` |
| Admin | `admin@fidelix.local` | `Admin123!` |
| Empleado | `empleado@fidelix.local` | `Empleado123!` |
| Empleado inactivo | `empleado.inactivo@fidelix.local` | `Inactivo123!` |

## Notas

* La carpeta **Canjes** da verde una sola vez por base limpia: el canje
  exitoso consume 100 de los 150 puntos del cliente demo.
* Los requests de alta con datos fijos (por ejemplo "Registrar Cliente") dan
  409 en la segunda corrida contra la misma base; los que generan documento o
  nombre al azar se pueden repetir.
* El código de recuperación de contraseña (CU-25) y el aviso de puntos
  vencidos (CU-22) no se envían por email en desarrollo: aparecen en la
  consola de la API como `[EMAIL] ...`.
* Toda la colección se puede correr también desde la terminal con el CLI de
  Bruno: `npx @usebruno/cli run Auth Clientes Admin Movimientos Canjes -r --env Local`.
