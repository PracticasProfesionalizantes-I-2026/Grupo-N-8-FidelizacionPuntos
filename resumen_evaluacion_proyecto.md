# Resumen Ejecutivo de Evaluación de Proyecto: Fidelix API

**Evaluador:** Arquitecto de Software Senior, Analista Funcional y Auditor QA  
**Fecha de Evaluación:** 9 de noviembre de 2026  
**Proyecto:** Sistema de Fidelización y Gestión de Puntos "Fidelix"  
**Stack Tecnológico:** .NET 10, ASP.NET Core, Entity Framework Core, SQLite, xUnit  
**Repositorio Evaluado:** Grupo-N-8-FidelizacionPuntos  

---

## 1. Visión General y Madurez del SDLC

El proyecto **Fidelix** evidencia un **nivel de madurez intermedio-avanzado** en la aplicación del Ciclo de Vida de Desarrollo de Software (SDLC). Los alumnos han demostrado una notable dedicación metodológica al cubrir integralmente la cadena de valor del software:
- **Análisis de Negocio:** Especificación exhaustiva de 29 Requerimientos Funcionales (RF-01 a RF-29) y 29 Reglas de Negocio (RN-01 a RN-29).
- **Diseño Funcional:** Elaboración de 26 Casos de Uso estructurados según el estándar formal de Cockburn/RUP, acompañados de un prototipo HTML/CSS interactivo.
- **Implementación Técnica:** Desarrollo de una API RESTful en C# / .NET 10 estructurada bajo un patrón arquitectónico en capas estricto (N-Tier: API, BusinessLogic, DataAccess, Shared) y acompañada de suites completas de pruebas unitarias y de integración.

El estado global de la entrega es **muy satisfactorio y funcional**, alcanzando una cobertura completa de los flujos core requeridos (acumulación de compras, cálculo dinámico de puntos, descuento FIFO, vencimientos programados, bonos de cumpleaños y auditoría de operaciones). 

No obstante, la auditoría identificó áreas de mejora cruciales:
1. **Desalineaciones entre las fases de diseño y prototipado.**
2. **Triplicación y sobre-especificación en el catálogo de Casos de Uso.**
3. **Brechas de concurrencia y seguridad en el código fuente** (como el riesgo de *double spending* en canjes concurrentes, desprotección contra ataques de fuerza bruta en códigos de recuperación y manejo de memoria en tareas batch).

---

## 2. Fortalezas Identificadas

### 2.1 En Análisis y Diseño
- **Definición Rigurosa de Reglas de Negocio:** Reglas de alta complejidad matemática y temporal (algoritmo FIFO `RN-09`, vencimiento a 180 días `RN-21`, diferimiento de puntos pendientes a 15 días `N2`, categoría Gold con multiplicador x1.5 `N1`, y política de mayoría de edad `RN-14`) están documentadas de manera unívoca y sin ambigüedades.
- **Trazabilidad Formal:** Cada uno de los 26 casos de uso posee correspondencia con los requerimientos de negocio, asegurando que ninguna funcionalidad core quede huérfana de justificación de negocio.
- **Prototipo Navegable Funcional:** La existencia de un prototipo autocontenido (`Docs/Prototipo/index.html`) facilitó la validación temprana de flujos de usuario (UX) tanto para el cliente como para el empleado en mostrador y el administrador.

### 2.2 En Arquitectura e Implementación
- **Adherencia Rigurosa a la Separación de Capas (N-Tier):**
  - La dirección de dependencias es unidireccional y limpia: `API → BusinessLogic → DataAccess → Shared`.
  - Los controladores no realizan consultas directas ni manipulan el `DbContext`.
  - Se utiliza el patrón **DTO** de forma estricta tanto para peticiones como para respuestas, impidiendo la exposición accidental de atributos sensibles (`PasswordHash`).
- **Seguridad Criptográfica Sólida en Credenciales:**
  - Implementación propia de **PBKDF2 con SHA-256**, salt criptográfico aleatorio de 16 bytes y 100.000 iteraciones (`PasswordHasher.cs`), empleando `CryptographicOperations.FixedTimeEquals` para prevenir ataques de canal lateral (*timing attacks*).
  - Bloqueo temporal exponencial de cuentas de clientes ante 3 intentos fallidos (`RN-03`, de 30s a 15 minutos).
- **Diseño de Dominio Idempotente y Consistente:**
  - El saldo del cliente **no se desnormaliza en una columna**, sino que se calcula dinámicamente sumando los `PuntosDisponibles` de lotes vigentes y no diferidos (`RN-05`). Esto evita inconsistencias de sincronización por concurrencia.
  - Los procesos batch diarios (`PointsExpirationJob` y `BonoCumpleanosJob`) están diseñados con protecciones de idempotencia (no duplican vencimientos ni bonos anuales ya otorgados).
- **Auditoría Transversal Desacoplada:**
  - Uso de un filtro global de MVC (`AuditoriaFilter.cs`) que registra automáticamente mutaciones exitosas (POST/PUT/PATCH/DELETE) sin ensuciar la lógica de negocio de los servicios (cumplimiento del principio *Open/Closed* de SOLID).
- **Patrón de Bajas Lógicas Consistente:**
  - El repositorio base (`IRepositorioBase`) deliberadamente omite el método `DeleteAsync` físico, forzando bajas lógicas (`Activo = false`) y relaciones con `DeleteBehavior.Restrict` en Entity Framework Core.

---

## 3. Análisis de Brechas (Gap Analysis)

El análisis comparativo entre la **Documentación de Negocio**, los **Casos de Uso**, el **Prototipo** y el **Código Fuente** reveló las siguientes discrepancias conceptuales:

```
+-----------------------------------+       +------------------------------------+
|     DOCUMENTACIÓN DE NEGOCIO      |       |           CASOS DE USO             |
| - RF-01 y RF-18 duplicados        | <---> | - Triplicación de Login (02,09,14) |
| - RF-25 solo dice "visualizar"    |       | - CU-23: algoritmo FIFO como CU    |
| - Salto en numeración (falta RN22)|       | - 25 de 26 CUs omiten fila NFR     |
+-----------------------------------+       +------------------------------------+
                 ^                                            ^
                 |                                            |
                 v                                            v
+-----------------------------------+       +------------------------------------+
|        PROTOTIPO HTML/JS          |       |           CÓDIGO FUENTE            |
| - ABM de "Categorías de Producto" | <---> | - Categorías son enums fijos       |
| - Dashboard de Administrador      |       | - Sin transacciones/locks (Canjes) |
| - Botones de simulación temporal  |       | - Inyección de SQL/Memoria en OOM  |
+-----------------------------------+       +------------------------------------+
```

### A. Documentación vs. Casos de Uso
1. **Triplicación del Inicio de Sesión:**  
   Se diseñaron tres casos de uso independientes para autenticación (`CU-02` para Clientes, `CU-09` para Empleados y `CU-14` para Admins). Metodológicamente, el login es un objetivo único con variaciones de rol. Irónicamente, en el caso de uso `CU-25 (Recuperación de Contraseña)`, los propios alumnos unificaron a los tres roles en un solo archivo reconociendo que triplicarlo era una mala práctica.
2. **Sobre-especificación de Algoritmos Internos como Casos de Uso:**  
   `CU-23 (Aplicar Método FIFO en Canjes)` fue documentado como un caso de uso independiente. Un algoritmo interno que no interactúa con un actor humano ni expone un punto de entrada del sistema corresponde a una **Regla de Negocio (RN-09)** o a una relación `«include»`, no a un caso de uso de nivel usuario.
3. **Sub-especificación de la Entrega Presencial:**  
   En la documentación, el requerimiento `RF-25` se titula *"Visualizar canjes realizados por clientes"*. Sin embargo, el caso de uso asociado (`CU-11`) y el código implementan una acción transaccional de cambio de estado a entregado (`PATCH /api/canjes/{id}/entregar`) y canje presencial que carecen de un requerimiento funcional explícito de entrega física.
4. **Omisión de Requerimientos No Funcionales (NFRs) en las Plantillas:**  
   A pesar de que la guía de cátedra exige especificar los NFRs en el encabezado de cada caso de uso, **25 de los 26 casos de uso omitieron esta fila**, dejando fuera criterios de latencia (RNF-02, < 2 segundos) y disponibilidad.

### B. Prototipo vs. Backend y Casos de Uso
1. **Funcionalidades "Fantasma" en el Prototipo:**  
   - El prototipo incluye una pantalla completa para el ABM dinámico de **"Categorías de Productos"** (`categorias`, `catNew`, `catEdit`). Sin embargo, en la arquitectura de backend y casos de uso, las categorías son **enums fijos de C#**, y la entidad `Producto` ni siquiera posee un campo de categoría.
   - El prototipo presenta un **Dashboard del Administrador** con métricas globales que no fue especificado en los requerimientos funcionales (RF-05 aplica solo al cliente).
2. **Simuladores Temporales en Interfaz:**  
   El prototipo incluye controles para adelantar el tiempo (+15 días para liberar puntos pendientes N2, +180 días para forzar vencimientos), lo cual es útil para demos pero no refleja la operación productiva de los procesos batch (`ProcesoDiarioJob`).

### C. Casos de Uso vs. Código Fuente
1. **Asimetría de Seguridad en Fuerza Bruta:**  
   Mientras que `CU-02` y `AuthService.cs` implementan bloqueo temporal de cuenta ante intentos fallidos para clientes (`RN-03`), en `CU-09`, `CU-14` y `AuthPersonalService.cs` (empleados y administradores) **no existe ningún bloqueo por intentos fallidos**. Los roles con mayores privilegios del sistema quedaron más vulnerables a ataques de diccionario.
2. **Permisos Reales de Endpoints vs. Actores Declarados:**  
   En `CU-04 (Consultar Saldo)` y `CU-06 (Consultar Beneficios)`, los casos de uso declaran como actor exclusivo al `Cliente`. No obstante, el código fuente en `ClientesController` y `BeneficiosController` autoriza adecuadamente a los roles `Empleado` y `Admin` para dar soporte en mostrador.

---

## 4. Conclusión de Arquitectura y Calidad de Código

### 4.1 Evaluación de Clean Code y Buenas Prácticas
- **Nomenclatura y Convenciones:** La nomenclatura en español para el dominio (`Cliente`, `Movimiento`, `Beneficio`) combinada con convenciones en inglés para la infraestructura (`Repository`, `Controller`, `Service`, `DTO`) se mantiene consistente en toda la solución.
- **Principio de Responsabilidad Única (SRP):** Los servicios de negocio están bien particionados (`ClienteService`, `CanjeService`, `PuntosService`, `MovimientoService`, `RecuperacionService`). No se observan clases "Dios" que concentren toda la operativa.
- **Clean Architecture & Inversión de Dependencias (DIP):** Todos los componentes de negocio y controladores consumen abstracciones (`IClienteRepository`, `ITokenService`, etc.), lo que permitió una suite de pruebas unitarias desacoplada y eficiente con fakes/mocks.

### 4.2 Deuda Técnica y Riesgos Críticos Detectados
A pesar de la excelente estructura general, se detectaron riesgos de severidad alta que comprometen la solidez del sistema en un entorno de producción:
1. **Condición de Carrera en Canjes (Double Spending):** En `CanjeService.cs`, la validación de saldo y la deducción FIFO ocurren en memoria sin locks ni control de concurrencia optimista (`RowVersion`). Dos peticiones concurrentes de un cliente pueden canjear más puntos de los que realmente tiene.
2. **Corrupción del `ChangeTracker` en Procesos Batch:** En `PuntosService.cs`, si falla la persistencia del vencimiento de un cliente, la entidad `Movimiento` permanece en estado `Added` dentro del contexto EF Core, contaminando las transacciones de los clientes siguientes.
3. **Peligro de Out-of-Memory (OOM):** En el cálculo diario de cumpleaños, se invoca `clienteRepository.GetAllAsync()` cargando la totalidad de clientes del sistema en memoria RAM para filtrarlos con LINQ to Objects.
4. **Ataque de Fuerza Bruta en Recuperación de Claves:** El código de 6 dígitos generado por `RecuperacionService.cs` tiene 15 minutos de vigencia pero carece de un límite de intentos fallidos o rate limiting, permitiendo ataques automatizados para adivinarlo.
5. **Clave JWT en Texto Plano:** La clave secreta de firma de tokens está versionada en `appsettings.json`, permitiendo la falsificación de tokens de administrador si el repositorio es accesible.

---

## 5. Dictamen del Auditor

| Dimensión Evaluada | Calificación | Justificación |
|---|:---:|---|
| **Comcomitancia y Trazabilidad (Diseño)** | **8.5 / 10** | Trazabilidad completa; penalizada levemente por triplicación de login y omisión de NFRs en plantillas. |
| **Arquitectura de Software** | **9.0 / 10** | Excelente desacoplamiento en 4 capas, inyección de dependencias, DTOs y persistencia limpia. |
| **Calidad de Código y Clean Code** | **8.5 / 10** | Código legible, ordenado, comentado y consistente. Detección de duplicación de boilerplate en controladores. |
| **Seguridad y Robustez en Concurrencia** | **6.5 / 10** | Buen algoritmo PBKDF2; pero penalizado por falta de control de concurrencia en canjes, clave JWT hardcodeada y ausencia de rate limit en recuperación. |
| **Nota Conceptual Global** | **MUY BUENO (8.5)** | Proyecto con bases de ingeniería de software muy sólidas, listo para pasar a un estándar productivo tras corregir los issues críticos listados. |

