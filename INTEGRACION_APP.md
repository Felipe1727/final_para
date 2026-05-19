# Integración de `final_para` en la Aplicación ASP.NET

## Visión General

`final_para` es una librería .NET para resolución de ecuaciones diferenciales parciales (EDP) integrada en una aplicación web ASP.NET. La integración utiliza tres paradigmas fundamentales:

1. **Paradigma de Objetos** - Modelado de dominio con clases bien definidas
2. **Paradigma de Eventos** - Comunicación asincrónica entre componentes
3. **Paradigma de Aspectos** - Comportamientos transversales mediante interceptores de proxy

---

## 1. Paradigma de Objetos

### 1.1 Arquitectura de Capas

La integración sigue una arquitectura clara de capas:

```
┌─────────────────────────────────────┐
│  Presentación (ASP.NET Views)       │
│  - Wizard (Pasos 1-5)               │
│  - Gráficas Plotly                  │
└─────────────┬───────────────────────┘
              │
┌─────────────▼───────────────────────┐
│  Application (Controllers/Services) │
│  - ServicioResolucion               │
│  - Mapeadores                       │
│  - ViewModels (VM)                  │
└─────────────┬───────────────────────┘
              │
┌─────────────▼───────────────────────┐
│  Domain (final_para)                │
│  - Métodos: FDM, FEM                │
│  - Ecuaciones                       │
│  - Estado y Histórico               │
└─────────────────────────────────────┘
```

### 1.2 Modelos de Dominio Clave

#### **Ecuación** (`final_para.Ecuaciones`)
```csharp
public class Ecuacion
{
    public string Expresion { get; set; }
    public bool EsHomogenea { get; set; }
    public string[] VariablesIndependientes { get; set; }
    public int Orden { get; set; }
}
```
**Responsabilidad**: Representar la EDP como un objeto de dominio.

#### **MetodoNumerico** (`final_para.Metodos`)
Clase base para todos los métodos numéricos:
```csharp
public abstract class MetodoNumerico
{
    public double[][] Malla { get; set; }
    public Ecuacion Ecuacion { get; set; }
    public abstract EstadoSolucion Resolver();
}
```
Subclases: `FDM`, `FEM` - implementan resolución según el algoritmo.

#### **EstadoSolucion** (`final_para.Estado`)
```csharp
public abstract class EstadoSolucion
{
    public double Residuo { get; set; }
    public int TamanoMalla { get; set; }
    public uint NumIteraciones { get; set; }
    public double TiempoSegundos { get; set; }
    public string MetodoNombre { get; set; }  // "Forward Euler", "Backward Euler"
}
```
**Responsabilidad**: Encapsular el estado de la solución en cada paso.

### 1.3 View Models (Puente Objeto-Presentación)

Los ViewModels transforman objetos de dominio a representaciones para la UI:

```csharp
// Entrada: ConfiguracionProblemaVM
public class ConfiguracionProblemaVM
{
    public double XMin, XMax, TMin, TMax { get; set; }
    public int Nx, Nt { get; set; }
    public string AlgoritmoFDM { get; set; }  // "ForwardEuler", "BackwardEuler"
}

// Salida: ResultadoResolucionVM
public class ResultadoResolucionVM
{
    public double[][] MallaFDM { get; set; }
    public double[][] MallaFEM { get; set; }
    public List<EvolucionVM> EvolucionFDM { get; set; }
    public List<EvolucionVM> EvolucionFEM { get; set; }
    public MetricaVM MetricasFDM { get; set; }
    public MetricaVM MetricasFEM { get; set; }
}
```

---

## 2. Paradigma de Eventos

### 2.1 Arquitectura Basada en Eventos

Los eventos en `final_para` permiten que componentes remotos (cliente web) reaccionen a cambios en tiempo real:

```
┌─────────────────────┐
│  Método FDM/FEM     │
│  (Resolver)         │
└──────────┬──────────┘
           │ Itera y converge
           │
┌──────────▼──────────────────┐
│ PublisherComputo (Evento)   │
│ - Dispara cada N iteraciones│
└──────────┬──────────────────┘
           │
      ┌────┼────┐
      │         │
┌─────▼──┐  ┌──▼──────────────────┐
│SignalR │  │AspActualizarHistorico│
│  Hub   │  │   (Guarda evolución) │
└────────┘  └─────────────────────┘
```

### 2.2 Sistema de Eventos en `PublisherComputo`

```csharp
public class PublisherComputo
{
    public event EventHandler<EstadoSolucion>? Evento;
    
    private int _contador = 0;
    private readonly int _intervaloFijo;

    public void Disparar(Func<EstadoSolucion> fabricaEstado)
    {
        _contador++;
        if (_contador % IntervaloActivo == 0)
            Evento?.Invoke(this, fabricaEstado());  // Publica a todos los suscriptores
    }
}
```

**Características clave:**
- **Bajo acoplamiento**: Los observadores no conocen al publicador
- **Intervalo configurable**: Solo emite cada N iteraciones para no saturar
- **Lazy evaluation**: Usa factory pattern para construir el estado solo cuando es necesario

### 2.3 Suscriptores del Evento

#### **SignalR Hub (Real-time)**
En `ServicioResolucion.ResolverAsync()`:
```csharp
publisherComputo.Evento += async (_, estado) =>
{
    // Envía progreso al navegador en tiempo real
    await _hub.Clients.Group(sessionId).SendAsync("ProgresoActualizado", new
    {
        metodo = estado.MetodoNombre,
        iteracion = estado.NumIteraciones,
        residuo = estado.Residuo,
        tiempo = estado.TiempoSegundos
    });
};
```

#### **Histórico (Captura de evolución)**
```csharp
var aspHistoricoFDM = new AspActualizarHistorico(historicoFDM);

// Filtrar por algoritmo
if (estado.MetodoNombre.StartsWith("Forward Euler"))
    aspHistoricoFDM.EventHandler(null, estado);
```

### 2.4 Ventajas del Paradigma de Eventos

| Ventaja | Descripción |
|---------|-------------|
| **Desacoplamiento** | La resolución no depende de quién escucha |
| **Extensibilidad** | Agregar nuevos suscriptores sin modificar FDM/FEM |
| **Asincronía natural** | SignalR actualiza el cliente sin bloquear la resolución |
| **Trazabilidad** | Cada evento es un snapshot del estado |

---

## 3. Paradigma de Aspectos

### 3.1 ¿Qué son los Aspectos?

Los aspectos son **comportamientos transversales** que afectan múltiples clases sin modificar su código. En `final_para`, se implementan usando:

1. **Castle DynamicProxy** - Genera proxies dinámicos
2. **Interceptores** - Interceptan llamadas a métodos
3. **Histórico y Reglas** - Almacenan estado de observabilidad

### 3.2 Flujo de Aspecto en FDM

```
┌──────────────────────────────────────┐
│ FabricaMetodoNumerico.CrearFDM()     │
└──────────────┬───────────────────────┘
               │
    ┌──────────▼──────────────┐
    │ new FDM(...) [Real]     │
    └──────────┬──────────────┘
               │
    ┌──────────▼──────────────────────────┐
    │ ProxyGenerator.CreateClassProxy      │
    │ con Interceptores:                   │
    │ - ServicioInterceptorComputo         │
    │ - ServicioInterceptorError           │
    └──────────┬──────────────────────────┘
               │
    ┌──────────▼──────────────┐
    │ FDM [Proxy]             │
    │ Wraps real FDM          │
    └────────────────────────┘
```

### 3.3 Interceptores

#### **ServicioInterceptorComputo**
Intercepta llamadas para publicar eventos de evolución:

```csharp
public class ServicioInterceptorComputo : IInterceptor
{
    private readonly PublisherComputo _publisher;

    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();  // Ejecuta el método real
        
        // Post-processing: emite evento si es Resolver()
        if (invocation.Method.Name == "Resolver" && 
            invocation.ReturnValue is EstadoSolucion estado)
        {
            _publisher.Disparar(() => estado);  // Publica resultado final
        }
    }
}
```

**Responsabilidad**: Notificar al PublisherComputo cuando se resuelve.

#### **ServicioInterceptorError**
Captura excepciones y las publica:

```csharp
public void Intercept(IInvocation invocation)
{
    try
    {
        invocation.Proceed();
    }
    catch (Exception ex)
    {
        _publisher.Disparar(() => new ErrorEvento(ex.Message));
        throw;
    }
}
```

### 3.4 Histórico como Aspecto

```csharp
public class AspActualizarHistorico  // Aspecto
{
    private readonly Historico _historico;

    public void EventHandler(object? sender, EstadoSolucion estado)
    {
        _historico.RegistrarEvaluacion(estado);  // Comportamiento transversal
    }
}
```

**Ventaja**: La clase `Historico` no depende de `FDM`. Se conecta dinámicamente via eventos.

### 3.5 Comparación: Con Aspectos vs Sin Aspectos

**Sin Aspectos (Acoplado):**
```csharp
public class FDM
{
    public EstadoSolucionFDM Resolver()
    {
        // ... resolución ...
        _historico.RegistrarEvaluacion(estado);      // Acoplado
        _signalR.EnviarProgreso(estado);              // Acoplado
        _metricas.Registrar(estado.Residuo);         // Acoplado
        return estado;
    }
}
// Problema: FDM conoce detalles de infraestructura
```

**Con Aspectos (Desacoplado):**
```csharp
public class FDM
{
    public EstadoSolucionFDM Resolver()
    {
        // ... solo resolución ...
        return estado;  // Puro, sin dependencias
    }
    // Los interceptores y eventos manejan todo lo demás
}
```

---

## 4. Flujo Integrado: Paso a Paso

### Escenario: Usuario resuelve una ecuación

#### **Paso 1: Entrada → Dominio**
```
Vista 2 (Configuración)
  ↓
ConfiguracionProblemaVM (clase de app)
  ↓
EcuacionMapper.ConConfiguracion()
  ↓
Ecuacion (clase de final_para)
```

#### **Paso 2: Dominio → Métodos**
```csharp
var fdm = fabrica.CrearFDM(
    malla,
    ecuacion,                           // Dominio
    esquema: EsquemaTemporal.Explicito,
    algoritmo: AlgoritmosFDM.ForwardEuler,
    publisherEvolucion: publisherComputo,
    nombreAlgoritmo: "Forward Euler"
);
// fabrica genera un Proxy con aspectos (interceptores)
```

#### **Paso 3: Ejecución con Eventos y Aspectos**
```csharp
var tarea = Task.Run(() => fdm.Resolver());
// Internamente:
//   1. Proxy.Intercept() se activa (aspecto)
//   2. FDM.Resolver() ejecuta (dominio)
//   3. Cada N iteraciones: PublisherComputo.Disparar()
//   4. Suscriptores reaccionan:
//      - AspActualizarHistorico: Guarda en Historico
//      - SignalR Handler: Envía al navegador
//   5. Proxy finaliza, publica resultado final
```

#### **Paso 4: Salida → Presentación**
```csharp
var estado = await tarea;

var resultado = ConstruirResultado(estado);
// Contiene:
//   - Malla de solución (double[][])
//   - Métricas (tiempo, residuo, error)
//   - EvolucionVM[] (tiempo vs residuo del histórico)

return resultado;
// Vista 4 renderiza con Plotly
```

### Diagrama de Flujo Completo

```
┌─────────────────────────────────────────────┐
│ Vista 1-2 (Usuario ingresa ecuación)        │
└────────┬────────────────────────────────────┘
         │ ConfiguracionProblemaVM
         ▼
┌─────────────────────────────────────────────┐
│ ServicioResolucion.ResolverAsync()          │
│ - Crea Ecuacion                             │
│ - Crea PublisherComputo                     │
│ - Crea Históricos                           │
│ - Suscribe SignalR + AspActualizarHistorico│
└────────┬────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────┐
│ FabricaMetodoNumerico.CrearFDM()            │
│ - new FDM(...)  [Objeto real]               │
│ - ProxyGenerator.CreateClassProxy()         │
│ - Envuelve con interceptores                │
└────────┬────────────────────────────────────┘
         │ FDM [Proxy]
         ▼
┌─────────────────────────────────────────────┐
│ fdm.Resolver()  [Con aspecto interceptador] │
│ ┌───────────────────────────────────────┐   │
│ │ ServicioInterceptorComputo.Intercept()│   │
│ │ → invocation.Proceed()                │   │
│ │ → FDM.Resolver() [Real]               │   │
│ │ ┌─────────────────────────────────┐   │   │
│ │ │ Loop de convergencia            │   │   │
│ │ │ Iter 1:  PublisherComputo.Disparar() │   │
│ │ │          ├─→ AspActualizarHistorico │   │
│ │ │          │   └─→ Historico [Aspecto]│   │
│ │ │          └─→ SignalR                │   │
│ │ │                └─→ Cliente (web)    │   │
│ │ │ Iter 2:  ... (idem)                │   │
│ │ │ ...                                 │   │
│ │ │ Convergencia → return EstadoSolución│   │
│ │ └─────────────────────────────────┘   │   │
│ └───────────────────────────────────────┘   │
└────────┬────────────────────────────────────┘
         │ EstadoSolucion
         ▼
┌─────────────────────────────────────────────┐
│ ConstruirResultado()                        │
│ - Obtener EvolucionVM[] de Historico        │
│ - Calcular métricas                         │
│ - Crear ResultadoResolucionVM               │
└────────┬────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────┐
│ Vista 4 (Resultados)                        │
│ - Gráficas 3D: superficie solución          │
│ - Gráficas 2D: heatmap + cortes             │
│ - Gráficas evolución: tiempo vs residuo     │
│ - Métricas: tiempo, error, iteraciones      │
└─────────────────────────────────────────────┘
```

---

## 5. Patrones de Diseño Aplicados

| Patrón | Ubicación | Propósito |
|--------|-----------|----------|
| **Factory** | `FabricaMetodoNumerico` | Crear métodos con configuración compleja |
| **Strategy** | `AlgoritmoFDM` (delegate) | Variar algoritmo sin cambiar FDM |
| **Observer** | `PublisherComputo` + suscriptores | Notificar cambios de estado |
| **Proxy** | `DynamicProxy` + interceptores | Inyectar aspectos sin modificar clases |
| **Template Method** | `MetodoNumerico.Resolver()` | Estructura común con detalles en subclases |
| **Mapper** | `EcuacionMapper` | Transformar entre capas |

---

## 6. Puntos Clave de Integración

### 6.1 Separación de Responsabilidades

```
final_para               app (ASP.NET)
────────────────────────────────────────
Ecuaciones          ←→  Controllers
Métodos             ←→  Services
Estado              ←→  ViewModels
Eventos             ←→  SignalR Hubs
Aspectos            ←→  Castle Proxy
```

### 6.2 Flujo de Datos

**Entrada:**
```
ConfiguracionProblemaVM 
  → EcuacionMapper 
    → Ecuacion (final_para)
```

**Procesamiento:**
```
FDM/FEM.Resolver()
  → PublisherComputo.Evento
    → [AspActualizarHistorico, SignalR]
      → Historico, Cliente Web
```

**Salida:**
```
EstadoSolucion + Historico
  → ResultadoResolucionVM
    → Vista Resultados
```

### 6.3 Extensibilidad

Para agregar una nueva métrica de observabilidad:

```csharp
// 1. Crear aspecto
public class AspCalcularMetrica
{
    public void EventHandler(object? sender, EstadoSolucion estado)
    {
        // Hacer cálculo
    }
}

// 2. Suscribir en ServicioResolucion
var aspMetrica = new AspCalcularMetrica();
publisherComputo.Evento += aspMetrica.EventHandler;

// ✅ Listo - Sin modificar FDM, Factory ni Controllers
```

---

## 7. Conclusión

La integración de `final_para` en la aplicación demuestra cómo tres paradigmas complementarios crean un sistema robusto:

- **Objetos**: Modelan el dominio con claridad y expresividad
- **Eventos**: Desacoplan componentes y permiten reactividad
- **Aspectos**: Inyectan comportamientos transversales sin contaminar el core

Este diseño permite que `final_para` sea:
- ✅ Independiente de la UI
- ✅ Fácil de testear
- ✅ Simple de extender
- ✅ Reutilizable en otros contextos

