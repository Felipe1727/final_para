# final_para

`final_para` es una libreria para la resolucion de ecuaciones diferenciales parciales (EDP) mediante metodos numericos, con enfoque educativo e investigativo.

El proyecto esta disenado para comparar **algoritmos del metodo FDM** (Finite Difference Method), especificamente esquemas temporales como Forward Euler vs Backward Euler, y para observar la evolucion de las soluciones usando un flujo orientado a eventos (error y costo computacional).

## Objetivo

- Resolver y estudiar EDP con diferentes estrategias numericas.
- Facilitar comparaciones entre algoritmos FDM (Forward Euler vs Backward Euler) bajo una misma arquitectura.
- Trazar el comportamiento del proceso de resolucion mediante eventos e historico de estados.

## Componentes principales

### 1) Modelo de ecuaciones (`Ecuaciones/`)

Define la abstraccion base `Ecuacion` (funcion, variables, orden, condiciones iniciales/de frontera, geometria, linealidad y dependencia temporal), junto con especializaciones para ecuaciones homogeneas y no homogeneas.

### 2) Metodos numericos (`Metodos/`)

La clase base `MetodoNumerico` modela la malla (`double[][]`), metadatos de ejecucion y la ecuacion asociada.  
Sobre ella se construyen:

- `FDM`: configurable con esquema temporal, orden espacial/temporal y un delegate `AlgoritmoFDM` (Forward Euler, Backward Euler, Jacobi, Gauss-Seidel, etc).
- `FEM`: disponible para uso futuro (no utilizado en la comparacion actual).

### 3) Estado y aspectos/eventos (`Estado/`, `Aspectos/`, `Servicios/`)

- `EstadoSolucion` y `EstadoSolucionFDM` representan snapshots de la solucion.
- `PublisherComputo` y `PublisherError` publican eventos segun umbrales globales en `ReglasAspectos`.
- `AspActualizarHistorico` y `Historico` registran la evolucion del proceso para cada algoritmo.
- Interceptores con Castle DynamicProxy (`ServicioInterceptorComputo`, `ServicioInterceptorError`) permiten enganchar llamadas a `Resolver`.

### 4) Parser (`Interfaces/IParse`, `Servicios/ServicioParser`)

Incluye una interfaz de parseo para:

- convertir expresiones a **LaTeX** para visualizacion (`ParseLatex`),
- y convertir expresiones funcionales a una representacion de ecuacion utilizable por los metodos numericos (`ParseFunc`).

## Estado actual del proyecto

El proyecto se encuentra en desarrollo y contiene partes en scaffolding con `NotImplementedException`, principalmente en:

- metodos de resolucion/calculo en `FDM` y `FEM`,
- parseo en `ServicioParser`.

## Tecnologias

- .NET (`net10.0`)
- C#
- Castle.Core (intercepcion por proxy)

## Estructura del repositorio

```text
final_para/
├── final_para.sln
├── final_para/
│   ├── Aspectos/
│   ├── Ecuaciones/
│   ├── Estado/
│   ├── Interfaces/
│   ├── Metodos/
│   └── Servicios/
├── apm.yml
└── apm.lock.yaml
```

## Compilacion y pruebas

Desde la raiz del repositorio:

```bash
dotnet build final_para.sln
dotnet test final_para.sln
```

Para ejecutar una prueba especifica (cuando exista un proyecto de tests):

```bash
dotnet test <ruta-al-proyecto-tests.csproj> --filter "FullyQualifiedName~Namespace.Clase.Test"
```
