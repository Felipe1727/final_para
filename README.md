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
