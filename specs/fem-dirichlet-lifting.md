# Especificación: FEM con Dirichlet lifting (fix solución trivial)

## Resumen

El método FEM actual produce siempre **solución trivial** (toda la malla en
ceros) incluso para problemas con C.F. Dirichlet no nulas. La causa raíz
está en la construcción del sistema lineal `K · u = f`:

1. `ConstruirVectorCargas(n)` arma `f` sólo a partir de
   `Ecuacion.TerminosForzantes`; para una EDP homogénea como Laplace
   `u_xx + u_yy = 0` queda `f ≡ 0`.
2. `ImponerDirichletFrontera(K, lado)` fuerza `K[bd][bd] = 1` y anula las
   filas frontera, pero **no toca `f[bd]`**. El resultado es la ecuación
   `1 · u[bd] = 0` ⇒ `u[bd] = 0` para todo nodo frontera.
3. Los buffers iterativos `actual` y `anterior` arrancan en `new double[n]`
   (ceros) y nunca se siembran con valores de frontera.

La combinación de los tres puntos hace que el solver converja al cero, sin
importar las C.F. que el usuario haya escrito en el wizard.

Esta especificación define el fix mediante **Dirichlet lifting clásico**:
imponer `u[bd] = g(x_bd)` directamente en `f` y sembrar el estado inicial
con esos mismos valores. Sólo aplica a Dirichlet (`u = g`); Neumann/Robin
quedan fuera de alcance en esta iteración.

## Requisitos

### Entrada

- `Ecuacion` con la API dinámica de [[condiciones-dinamicas]]:
  - `CondicionesFrontera`: descriptores `"u(XMin,*)"`, `"u(XMax,*)"`,
    `"u(YMin,*)"`, … cada uno con expresión `g(...)` evaluable.
- Malla FEM: arreglo de nodos `Malla[i] = (x_i, y_i)` con tag de lado
  (`XMin`, `XMax`, `YMin`, `YMax`) para cada nodo frontera.
- Helper `EvaluadorExpresion.Evaluar(expr, vars)` introducido por
  [[condiciones-dinamicas]].

### Cambios en `final_para/Metodos/FEM.cs`

1. **`CalcularValoresFronteraPorNodo()` (nuevo helper privado)**

   Itera nodos frontera, para cada uno:
   - Determina el lado (`XMin`/`XMax`/`YMin`/`YMax`) a partir de las
     coordenadas y los rangos de la malla.
   - Busca el descriptor coincidente en `Ecuacion.CondicionesFrontera`.
   - Evalúa la expresión con `EvaluadorExpresion` pasando las coordenadas
     del nodo (`{"x": Malla[i][0], "y": Malla[i][1]}`).

   Retorna `double[] valoresFronteraPorNodo` (tamaño n, 0 en nodos
   interiores).

2. **`ConstruirVectorCargas(int n, double[] valoresFronteraPorNodo)`**

   - Parte de la suma de `TerminosForzantes` (comportamiento previo).
   - Después **sobrescribe** `f[i] = valoresFronteraPorNodo[i]` para cada
     nodo frontera.

3. **`ImponerDirichletFrontera(K, f, valoresFronteraPorNodo, lado)`**

   Nueva firma: además de fijar `K[bd][bd] = 1` y anular el resto de la
   fila, escribe `f[bd] = valoresFronteraPorNodo[bd]` para mantener la
   consistencia con el lifting.

4. **`Resolver()`** — sembrado inicial:

   ```csharp
   var actual   = new double[n];
   for (int i = 0; i < n; i++)
       if (EsFrontera(i)) actual[i] = valoresFronteraPorNodo[i];
   var anterior = (double[])actual.Clone();
   ```

   Esto evita partir de ceros y acelera la convergencia del solver iterativo
   sobre `K · u = f`.

### Salida

- `MallaFEM` con al menos un valor no nulo cuando hay C.F. no triviales.
- Convergencia numérica al perfil esperado para Laplace con C.F. analíticas
  conocidas (tolerancia razonable según `MaxIteraciones` y `Tolerancia`).

### Casos de prueba

```
Caso 1: Laplace 2D con una sola frontera no trivial
  Ecuacion: u_xx + u_yy = 0
  Dominio:  x ∈ [0,1], y ∈ [0,1], N_x = N_y = 20
  C.F.:
    u(XMin,*) = sin(pi*y)
    u(XMax,*) = 0
    u(YMin,*) = 0
    u(YMax,*) = 0
  Esperado:
    - MallaFEM no es idénticamente cero.
    - Fila frontera x=0 tiene perfil sinusoidal en y.
    - Interior decrece de x=0 a x=1 (suave, sin oscilaciones).
    - Comparación con solución analítica
      u(x,y) = sinh(pi*(1-x))/sinh(pi) * sin(pi*y)
      con error L∞ < 5% para N=20.

Caso 2: Laplace con C.F. todas no triviales (placa cuadrada)
  u(XMin,*) = sin(pi*y), u(XMax,*) = sin(pi*y)
  u(YMin,*) = 0,          u(YMax,*) = 0
  Esperado:
    - Simetría u(x,y) = u(1-x,y) (tolerancia numérica).
    - Valor máximo cercano a 1 en (0, 0.5) y (1, 0.5).

Caso 3: Calor 1D existente (regresión)
  Ecuacion: u_t - u_xx = 0
  Comportamiento previo se conserva (1 C.I., 2 C.F. Dirichlet).
```

## Notas de implementación

- **Dirichlet lifting** es la técnica estándar de FEM: en lugar de eliminar
  los grados de libertad frontera del sistema, se imponen directamente
  fijando la fila/columna correspondiente. La consistencia exige que la
  columna se anule (o se pase al RHS); para sistemas simétricos suele
  hacerse simétricamente, pero como aquí usamos solvers iterativos como
  Gauss-Seidel, basta con fijar fila + RHS.
- El término `K[bd][bd] = 1` ya estaba; sólo falta el `f[bd] = g(x_bd)`
  correspondiente.
- Solo Dirichlet en esta iteración. Cuando se añada Neumann, el descriptor
  podría extenderse a `"du/dn(XMin,*)"` y el lifting cambiará por
  contribución de borde al `f`. Eso es trabajo futuro.
- El sembrado inicial de `actual` con valores de frontera es opcional para
  la corrección (el solver convergería de todas formas con `f` correcto),
  pero acelera la convergencia y es buena práctica.
- Mantener el patrón enums + delegates del repositorio: no inyectar lógica
  Dirichlet en `Resolver()` directamente; encapsular en helpers reutilizables.

## Validación

- Test unitario: `CalcularValoresFronteraPorNodo()` con malla 3x3 retorna
  los valores esperados en los 8 nodos frontera.
- Test unitario: `ConstruirVectorCargas(n, valoresBd)` produce `f[bd] =
  valoresBd[bd]` y `f[interior] = sum(TerminosForzantes)`.
- Test de integración: Caso 1 arriba → `MallaFEM` tiene `∑|u_ij| > 0`.
- Test de regresión: el caso calor 1D actual (1 C.I. + 2 C.F.) sigue
  funcionando sin cambios visibles.
- Smoke test end-to-end (`scripts/smoke-test.sh`) verifica que la malla
  FEM resultante tiene al menos un valor numérico distinto de cero.

## Dependencias

- Requiere: [[condiciones-dinamicas]] (nueva estructura de
  `CondicionesFrontera` con descriptores por lado).
- Requiere: [[algoritmos-fem-default]] (base del solver iterativo).
- Beneficia a: [[fem-resolver]] (el `Resolver()` ya no parte de ceros y
  converge antes).
- Beneficia a: [[fem-calcular-error]] (ahora hay solución no trivial contra
  la que medir error).
