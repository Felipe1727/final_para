# Especificación: Condiciones de resolución dinámicas

## Resumen

Hoy el pipeline (form → VM → mapper → malla → solver) asume rígidamente
**1 condición inicial (C.I.) + 2 condiciones de frontera (C.F.)** sin importar
la ecuación. Esto produce configuraciones imposibles de expresar:

- Laplace `u_xx + u_yy = 0`: necesita **4 C.F.** y **0 C.I.**
- Onda 1D `u_tt − u_xx = 0`: necesita **2 C.I.** y **2 C.F.**
- Calor 1D `u_t − u_xx = 0`: necesita **1 C.I.** y **2 C.F.** (caso actual, único soportado)

Esta especificación introduce el modelo dinámico: la cantidad y descriptor de
cada condición se deriva del **orden de derivadas por variable independiente**
de la ecuación. Una variable temporal `t` con derivada de orden `k` exige `k`
C.I. (`u`, `u_t`, ..., hasta `u_(t…k−1)`). Una variable espacial con derivada
de orden `k` exige `k` C.F. (típicamente `k=2`, una en cada extremo).

Esta iteración cubre **solo Dirichlet** (`u = g(...)`). Neumann/Robin quedan
fuera de alcance.

## Requisitos

### Entrada

- `Ecuacion` con un nuevo campo `OrdenesPorVariable`:
  ```csharp
  public IReadOnlyDictionary<string, byte> OrdenesPorVariable { get; protected set; }
  // ej. Laplace: { "x": 2, "y": 2 }
  // ej. onda:   { "t": 2, "x": 2 }
  // ej. calor:  { "t": 1, "x": 2 }
  ```
- `ConfiguracionProblemaVM` con estructura dinámica basada en diccionarios:
  ```csharp
  public Dictionary<string, double> Min { get; set; } = new();
  public Dictionary<string, double> Max { get; set; } = new();
  public Dictionary<string, int>    N   { get; set; } = new();
  public Dictionary<string, string> CondicionesIniciales { get; set; } = new();
  public Dictionary<string, string> CondicionesFrontera  { get; set; } = new();
  public string EsquemaTemporal { get; set; }
  public string AlgoritmoFDM    { get; set; }
  public string TipoElemento    { get; set; }
  public string AlgoritmoFEM    { get; set; }
  ```
  - Claves de `Min/Max/N`: una por variable independiente (`"x"`, `"y"`, `"t"`).
  - Claves de `CondicionesIniciales`: descriptores `"u(x,0)"`, `"u_t(x,0)"`, …
    (sólo cuando hay variable temporal `t`).
  - Claves de `CondicionesFrontera`: descriptores `"u(XMin,*)"`, `"u(XMax,*)"`,
    `"u(YMin,*)"`, … por cada variable espacial.

### Helpers en `Ecuacion`

```csharp
IEnumerable<string> VariablesEspaciales
    => VariablesIndependientes.Where(v => v != "t");

bool EsTemporal(string v) => v == "t";

int NumCondicionesInicialesRequeridas()
    => OrdenesPorVariable.GetValueOrDefault("t", (byte)0);

int NumCondicionesFronteraRequeridas()
    => VariablesEspaciales.Sum(v => (int)OrdenesPorVariable[v]);
```

### Salida

- Validación coherente: `ValidadorEcuacion` rechaza configuraciones con
  número incorrecto de C.I. o C.F. (delegado a la spec [[validador-ecuacion]]
  con la nueva regla dinámica).
- Mapeo coherente: `EcuacionMapper.ConConfiguracion` arma listas de
  expresiones a partir de los diccionarios en orden determinístico
  (alfabético por clave).
- Render coherente: la vista del paso 2 (Configuración) genera inputs por
  variable y por descriptor en lugar de los tres campos fijos actuales.

### Casos de prueba

```
Caso 1: Laplace 2D (estacionario, sin tiempo)
  Ecuacion:           u_xx + u_yy = 0
  OrdenesPorVariable: { "x": 2, "y": 2 }
  NumCondicionesInicialesRequeridas() = 0
  NumCondicionesFronteraRequeridas()  = 4
  Descriptores C.F. esperados:
    "u(XMin,*)", "u(XMax,*)", "u(YMin,*)", "u(YMax,*)"

Caso 2: Onda 1D
  Ecuacion:           u_tt - u_xx = 0
  OrdenesPorVariable: { "t": 2, "x": 2 }
  NumCondicionesInicialesRequeridas() = 2
  NumCondicionesFronteraRequeridas()  = 2
  Descriptores C.I. esperados:
    "u(x,0)", "u_t(x,0)"
  Descriptores C.F. esperados:
    "u(XMin,*)", "u(XMax,*)"

Caso 3: Calor 1D (caso actualmente soportado)
  Ecuacion:           u_t - u_xx = 0
  OrdenesPorVariable: { "t": 1, "x": 2 }
  NumCondicionesInicialesRequeridas() = 1
  NumCondicionesFronteraRequeridas()  = 2
  Descriptores C.I. esperados:
    "u(x,0)"
  Descriptores C.F. esperados:
    "u(XMin,*)", "u(XMax,*)"
```

## Notas de implementación

- `OrdenesPorVariable` se calcula en `ServicioParser` recorriendo los
  subíndices de cada `u_*` en los términos y contando la multiplicidad por
  carácter de variable. Ejemplo: `u_xxy` aporta `x:2, y:1`. El `Orden`
  global existente (máximo orden) se conserva como `max` sobre los valores.
- Mantener compatibilidad hacia atrás: el constructor de `Ecuacion` acepta
  `OrdenesPorVariable` opcional (`null`); si es `null` y existe
  `VariablesIndependientes`, se construye un diccionario uniforme con el
  `Orden` global por cada variable (suficiente para callers viejos).
- Orden determinístico al iterar diccionarios en el mapper y la vista:
  alfabético por clave (`Min/Max/N` por variable, descriptores de
  condiciones por nombre). Esto evita salidas no reproducibles entre runs.
- El descriptor humano de C.F. usa `XMin/XMax` (o `YMin/YMax`, etc.) como
  literal — la vista renderiza el rango numérico correspondiente al lado del
  campo para que el usuario sepa qué valor toma esa frontera.
- Helper compartido nuevo: `final_para/Servicios/EvaluadorExpresion.cs`
  para evaluar expresiones como `"sin(pi*x)"` o `"x*(1-x)"` en un punto.
  Lo consumen `GeneradorMalla`, `EcuacionMapper` y `FEM`.

## Validación

- Test unitario `Ecuacion.OrdenesPorVariable` para Laplace, onda y calor.
- Test unitario `Ecuacion.NumCondiciones*Requeridas()` para los 3 casos.
- Test de integración: `ServicioParser.ParseFunc("u_xx + u_yy = 0")` produce
  `OrdenesPorVariable = { "x": 2, "y": 2 }`.
- Test de regresión: el caso calor 1D actual sigue funcionando.
- Smoke test end-to-end: `scripts/smoke-test.sh` (ver
  [[fem-dirichlet-lifting]] para los criterios de valor no trivial).

## Dependencias

- Habilita: [[fem-dirichlet-lifting]] (necesita el nuevo descriptor de C.F.)
- Beneficia a: [[validador-ecuacion]] (nuevas reglas dinámicas)
- Beneficia a: [[ecuacion-builder]] (el builder puede recibir `OrdenesPorVariable`)
