# final_para

`final_para` es una libreria para la resolución de ecuaciones diferenciales parciales (EDP) mediante métodos numéricos, con enfoque educativo e investigativo.

El proyecto esta diseñado para comparar enfoques como **FDM** (Finite Difference Method) y **FEM** (Finite Element Method), y para observar la evolución de las soluciones usando un flujo orientado a eventos (error y costo computacional).

## Objetivo

- Resolver y estudiar EDP con diferentes estrategias numéricas.
- Facilitar comparaciones entre metodos (FDM/FEM) bajo una misma arquitectura.
- Trazar el comportamiento del proceso de resolución mediante eventos e histórico de estados.

## Componentes principales

### 1) Modelo de ecuaciones (`Ecuaciones/`)

Define la abstraccion base `Ecuación` (función, variables, orden, condiciones iniciales/de frontera, geometría, linealidad y dependencia temporal), junto con especializaciones para ecuaciones homogeneas y no homogeneas.

### 2) Metodos numéricos (`Metodos/`)

La clase base `MetodoNumerico` modela la malla (`double[][]`), metadatos de ejecución y la ecuación asociada.  
Sobre ella se construyen:

- `FDM`: configurable con esquema temporal, orden espacial/temporal y un delegate `AlgoritmoFDM`.
- `FEM`: configurable con tipo de elemento, funciones base y un delegate `AlgoritmoFEM`.

### 3) Estado y aspectos/eventos (`Estado/`, `Aspectos/`, `Servicios/`)

- `EstadoSolucion`, `EstadoSolucionFDM` y `EstadoSolucionFEM` representan snapshots de la solucion.
- `PublisherComputo` y `PublisherError` publican eventos según umbrales globales en `ReglasAspectos`.
- `AspActualizarHistorico` y `Historico` registran la evolución del proceso.
- Interceptores con Castle DynamicProxy (`ServicioInterceptorComputo`, `ServicioInterceptorError`) permiten enganchar llamadas a `Resolver`.

### 4) Parser (`Interfaces/IParse`, `Servicios/ServicioParser`)

Incluye una interfaz de parseo para:

- convertir expresiones a **LaTeX** para visualización (`ParseLatex`),
- y convertir expresiones funcionales a una representación de ecuacion utilizable por los métodos numéricos (`ParseFunc`).

## Estado actual del proyecto

El proyecto se encuentra en desarrollo y contiene partes en scaffolding con `NotImplementedException`, principalmente en:

- metodos de resolucion/calculo en `FDM` y `FEM`,
- parseo en `ServicioParser`.

## Tecnologías

- .NET (`net10.0`)
- C#
- Castle.Core (intercepcion por proxy)

## Estructura del repositorio

```text
final_para/
├── final_para.sln
├── final_para/         # librería numérica (no modificar desde la app)
│   ├── Aspectos/
│   ├── Ecuaciones/
│   ├── Estado/
│   ├── Interfaces/
│   ├── Metodos/
│   └── Servicios/
├── app/                # ASP.NET Core MVC (wizard FDM vs FEM)
│   ├── Controllers/
│   ├── Hubs/
│   ├── Models/
│   ├── Services/
│   ├── Views/
│   └── wwwroot/
├── apm.yml
└── apm.lock.yaml
```

## Flujo wizard (`app/`)

La aplicación web es un wizard de cinco pasos con rutas independientes. La sesión HTTP guarda el estado entre páginas.

```text
GET /                      → 302 → /Wizard/Ecuacion
GET /Wizard/Ecuacion       (paso 1) entrada y validación LaTeX
GET /Wizard/Configuracion  (paso 2) dominio, malla, CI/CF, algoritmos
GET /Wizard/Progreso       (paso 3) ejecuta FDM y FEM con stream SignalR
GET /Wizard/Resultados     (paso 4) superficie 3D + heatmap + corte
GET /Wizard/Comparacion    (paso 5) tabla, barras, exportar CSV/JSON
POST /Wizard/Reiniciar     limpia la sesión y vuelve al paso 1
```

Endpoints de soporte:

```text
POST /Ecuacion/Parsear         valida la ecuación y la persiste en sesión
GET  /Ecuacion/Plantillas      presets (onda, calor, Laplace, transporte, Burgers)
POST /Wizard/GuardarConfiguracion  persiste la config en sesión antes del paso 3
POST /Resolucion/Iniciar       ejecuta FDM y FEM en paralelo (Task.WhenAll)
GET  /Resolucion/SessionId     id de la sesión, requerido por SignalR
```

Eventos SignalR emitidos por `ServicioResolucion` al grupo `sessionId`:

- `FaseActualizada { fase, estado, porcentaje }` — fases macro (malla, ensamble, resolución, métricas).
- `ProgresoActualizado { metodo, iteracion, residuo, tiempo }` — log por método.
- `ResolucionCompleta resultadoId` — al terminar; el cliente navega a `/Wizard/Resultados`.

## Compilación y pruebas

Desde la raíz del repositorio:

```bash
dotnet build final_para.sln
dotnet test final_para.sln
```

Para ejecutar una prueba específica (cuando exista un proyecto de tests):

```bash
dotnet test <ruta-al-proyecto-tests.csproj> --filter "FullyQualifiedName~Namespace.Clase.Test"
```
