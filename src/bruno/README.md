# Colección Bruno — Fidelix API

Abrir esta carpeta (`src/bruno`) directamente en Bruno como colección, y
seleccionar el entorno **Local** (`http://localhost:5299`, ajustar el puerto
si `dotnet run` levanta en otro).

## Orden recomendado

1. **Clientes**: registro (CU-01) y consulta de saldo (CU-04). El saldo usa
   `clienteDemoId`, el Id fijo del cliente que siembra `DbInitializer`.
2. **Admin/Beneficios**: ABM de beneficios (CU-17). "Crear Beneficio" guarda
   `beneficioAdminId`, que usan "Actualizar" y "Desactivar", así que conviene
   correr la carpeta completa en orden ("Run Folder").

Los requests de registro y alta usan datos fijos: la segunda corrida contra
la misma base da 409 (duplicado). Para empezar de cero, borrar
`src/API/fidelix.db` y volver a levantar la API.

## Qué cubre

Un request por endpoint con su caso de éxito y al menos un flujo de error
(400/404/409), siguiendo la matriz de trazabilidad de cada caso de uso en
`Docs/Use cases/`. Todavía no hay autenticación: cuando se implemente el
login (CU-02/09/14) hay que agregar los requests de login y el token
(`auth: bearer`) en los requests protegidos.
