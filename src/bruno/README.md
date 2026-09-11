# Colección Bruno — Fidelix API

Abrir esta carpeta (`src/bruno`) directamente en Bruno como colección, y
seleccionar el entorno **Local** (`http://localhost:5299`, ajustar el puerto
si `dotnet run` levanta en otro).

## Orden recomendado para una corrida completa

La colección encadena tokens y IDs entre requests con variables de entorno
(`bru.setVar`), así que conviene correrla en este orden (o usar "Run Folder"
carpeta por carpeta, en este orden):

1. **Auth**: los 3 "Login" (Cliente/Empleado/Admin) primero — guardan
   `tokenCliente`, `tokenEmpleado` y `tokenAdmin` usados por el resto de la
   colección.
2. **Beneficios**: "Consultar Beneficios Activos" guarda `beneficioIdDescuento`.
3. **Movimientos**: "Registrar Acumulacion" le da saldo al cliente demo.
4. **Canjes**: usa el saldo cargado en el paso anterior.
5. **Clientes**: casos de perfil propio. "Dar de Baja" queda al final
   (`seq: 99`) porque desactiva al cliente demo y rompe las requests
   posteriores que dependen de él.
6. **Admin/***: cada subcarpeta (Clientes, Empleados, Beneficios, Productos,
   ReglasAcumulacion, Movimientos, Auditoria, Reportes) es independiente y
   encadena sus propios IDs (`Crear ...` guarda el Id que usan
   `Actualizar .../Desactivar .../Dar de Baja ...`).

## Qué cubre cada request

Un request por endpoint (26 CU), con el caso de éxito (200/201/204) y al
menos un flujo de error relevante (400/401/403/404/409) por módulo,
siguiendo la matriz de trazabilidad de cada caso de uso en
`Docs/Use cases/`.
