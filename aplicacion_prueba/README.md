# `aplicacion_prueba`

Programa ejecutable que consume la librería `final_para` como dependencia y demuestra el flujo completo: configurar una `Ecuacion`, crear un método numérico vía `FabricaMetodoNumerico`, resolver, y observar los eventos publicados al `Historico`.

## Cómo correr

Desde la raíz del repositorio:

```bash
dotnet run --project aplicacion_prueba             # corre los tres ejemplos
dotnet run --project aplicacion_prueba -- calor    # solo Ejemplo 1
dotnet run --project aplicacion_prueba -- poisson  # solo Ejemplo 2
dotnet run --project aplicacion_prueba -- comparar # solo Ejemplo 3
```

## Qué demuestra cada ejemplo

| Ejemplo | Comando | Qué demuestra |
|---|---|---|
| `EjemploCalor1D` | `calor` | Ecuación de calor 1D `u_t − u_xx = 0` con pulso senoidal inicial, esquema explícito (`ForwardEuler`). Imprime el perfil final. |
| `EjemploPoisson2D` | `poisson` | Ecuación de Poisson 2D `u_xx + u_yy = 0` con condiciones Dirichlet `u(1,y)=1`. Resuelve con FDM y con FEM (P1 triangular). Imprime mapa de calor ASCII. |
| `EjemploComparativaFDMvsFEM` | `comparar` | Misma EDP elíptica con mallas crecientes (6, 10, 14). Tabla con error y costo computacional por método y tamaño. |

## Salida típica

```
=== Ejemplo 1: Ecuación de calor 1D (u_t - u_xx = 0) ===
  Iteraciones realizadas: 2
  Residuo final: 0.000E+000
  Costo computacional: 0.0010 s
  Error final: 0.000E+000
  Perfil final: 0.000 0.165 0.325 ... 0.000
```

El histórico se actualiza vía interceptores Castle DynamicProxy: la fábrica envuelve cada `FDM`/`FEM` para que el `PublisherComputo` dispare al completar `Resolver()` y `AspActualizarHistorico` lo registre.
