using app.Hubs;
using app.Models.Mapeadores;
using app.Models.ViewModels;
using final_para.Aspectos;
using final_para.Ecuaciones;
using final_para.Estado;
using final_para.Metodos;
using final_para.Servicios;
using Microsoft.AspNetCore.SignalR;

namespace app.Services;

/// <summary>
/// Orquesta una resolución simultánea con dos algoritmos FDM. Lee la ecuación de sesión,
/// instancia ambos métodos vía FabricaMetodoNumerico (proxy con interceptores), suscribe
/// eventos del PublisherComputo a un Hub SignalR y devuelve un ResultadoResolucionVM.
/// </summary>
public class ServicioResolucion
{
    private readonly GeneradorMalla _generadorMalla;
    private readonly IHubContext<ProgresoHub> _hub;

    public ServicioResolucion(
        GeneradorMalla generadorMalla,
        IHubContext<ProgresoHub> hub)
    {
        _generadorMalla = generadorMalla;
        _hub = hub;
    }

    public async Task<ResultadoResolucionVM> ResolverAsync(
        string sessionId,
        Ecuacion ecuacionBase,
        ConfiguracionProblemaVM config,
        CancellationToken cancellationToken = default)
    {
        var ecuacion = EcuacionMapper.ConConfiguracion(ecuacionBase, config);

        var (mallaFDM, ejeX, ejeT) = _generadorMalla.ConstruirMallaFDM(config);
        var (mallaFDM2, _, _) = _generadorMalla.ConstruirMallaFDM(config);

        var publisherComputo = new PublisherComputo(intervalo: Math.Max(1, config.Nx * config.Nt / 100));
        var publisherError = new PublisherError();
        var fabrica = new FabricaMetodoNumerico(publisherComputo, publisherError);

        // Crear históricos para capturar evolución
        var historicoFDM = new Historico();
        var historicoFEM = new Historico();
        var aspHistoricoFDM = new AspActualizarHistorico(historicoFDM);
        var aspHistoricoFEM = new AspActualizarHistorico(historicoFEM);

        // Suscripción: cada evento del publisher empuja al cliente vía SignalR (grupo = sessionId).
        publisherComputo.Evento += async (_, estado) =>
        {
            // Filtrar eventos a sus respectivos históricos
            if (estado.MetodoNombre.Contains("Forward") || estado.MetodoNombre.Contains("Backward"))
            {
                if (estado.MetodoNombre.StartsWith(ObtenerNombreAlgoritmo(config.AlgoritmoFDM)))
                    aspHistoricoFDM.EventHandler(null, estado);
                else
                    aspHistoricoFEM.EventHandler(null, estado);
            }

            await _hub.Clients.Group(sessionId).SendAsync("ProgresoActualizado", new
            {
                metodo = estado.MetodoNombre,
                iteracion = estado.NumIteraciones,
                residuo = estado.Residuo,
                tiempo = estado.TiempoSegundos
            }, cancellationToken);
        };

        var esquema = ParsearEsquema(config.EsquemaTemporal);
        var algoritmoFDM = ResolverAlgoritmoFDM(config.AlgoritmoFDM);
        var algoritmoFDM2 = AlgoritmoComplementario(algoritmoFDM);
        var nombreAlg1 = ObtenerNombreAlgoritmo(config.AlgoritmoFDM);
        var nombreAlg2 = ObtenerNombreAlgoritmo(ObtenerComplementario(config.AlgoritmoFDM));

        var fdm = fabrica.CrearFDM(mallaFDM, ecuacion, esquema, ordenEspacial: 2, ordenTemporal: 1,
            algoritmoFDM, publisherEvolucion: publisherComputo, nombreAlgoritmo: nombreAlg1);

        var fdm2 = fabrica.CrearFDM(mallaFDM2, ecuacion, esquema, ordenEspacial: 2, ordenTemporal: 1,
            algoritmoFDM2, publisherEvolucion: publisherComputo, nombreAlgoritmo: nombreAlg2);

        // Ejecutar ambos en paralelo. Cada Resolver() es síncrono y bloqueante, así que Task.Run.
        var tareaFDM = Task.Run(() => fdm.Resolver(), cancellationToken);
        var tareaFDM2 = Task.Run(() => fdm2.Resolver(), cancellationToken);

        await Task.WhenAll(tareaFDM, tareaFDM2);

        var estadoFDM = await tareaFDM;
        var estadoFDM2 = await tareaFDM2;

        var resultado = ConstruirResultado(estadoFDM, estadoFDM2, ejeX, ejeT, fdm, fdm2, historicoFDM, historicoFEM);

        await _hub.Clients.Group(sessionId).SendAsync("ResolucionCompleta", resultado.Id, cancellationToken);

        return resultado;
    }

    private static ResultadoResolucionVM ConstruirResultado(
        EstadoSolucionFDM estadoFDM,
        EstadoSolucionFDM estadoFDM2,
        double[] ejeX,
        double[] ejeT,
        FDM fdm,
        FDM fdm2,
        Historico historicoFDM,
        Historico historicoFEM)
    {
        var mallaFDMResultado = estadoFDM.ValorActual;
        var mallaFEMResultado = estadoFDM2.ValorActual;

        var metricasFDM = new MetricaVM
        {
            MetodoNombre = estadoFDM.MetodoNombre,
            TiempoSegundos = estadoFDM.TiempoSegundos,
            Residuo = estadoFDM.Residuo,
            Error = fdm.CalcularError(),
            NumIteraciones = estadoFDM.NumIteraciones,
            TamanoMalla = estadoFDM.TamanoMalla
        };

        var metricasFEM = new MetricaVM
        {
            MetodoNombre = estadoFDM2.MetodoNombre,
            TiempoSegundos = estadoFDM2.TiempoSegundos,
            Residuo = estadoFDM2.Residuo,
            Error = fdm2.CalcularError(),
            NumIteraciones = estadoFDM2.NumIteraciones,
            TamanoMalla = estadoFDM2.TamanoMalla
        };

        var comparacion = new ComparacionVM
        {
            DiferenciaTiempo = metricasFEM.TiempoSegundos - metricasFDM.TiempoSegundos,
            DiferenciaError = metricasFEM.Error - metricasFDM.Error,
            GanadorTiempo = metricasFDM.TiempoSegundos <= metricasFEM.TiempoSegundos
                ? metricasFDM.MetodoNombre : metricasFEM.MetodoNombre,
            GanadorError = metricasFDM.Error <= metricasFEM.Error
                ? metricasFDM.MetodoNombre : metricasFEM.MetodoNombre
        };

        var evolucionFDM = historicoFDM.ObtenerRegistros()
            .Select(e => new EvolucionVM { Tiempo = e.TiempoSegundos, Residuo = e.Residuo })
            .ToList();

        var evolucionFEM = historicoFEM.ObtenerRegistros()
            .Select(e => new EvolucionVM { Tiempo = e.TiempoSegundos, Residuo = e.Residuo })
            .ToList();

        return new ResultadoResolucionVM
        {
            Id = Guid.NewGuid().ToString("N"),
            MallaFDM = mallaFDMResultado,
            MallaFEM = mallaFEMResultado,
            EjeX = ejeX,
            EjeY = ejeT,
            MetricasFDM = metricasFDM,
            MetricasFEM = metricasFEM,
            Comparacion = comparacion,
            EvolucionFDM = evolucionFDM,
            EvolucionFEM = evolucionFEM
        };
    }

    private static EsquemaTemporal ParsearEsquema(string s) => s switch
    {
        "Explicito" => EsquemaTemporal.Explicito,
        "CrankNicolson" => EsquemaTemporal.CrankNicolson,
        _ => EsquemaTemporal.Implicito
    };

    private static AlgoritmoFDM ResolverAlgoritmoFDM(string nombre) => nombre switch
    {
        "JacobiLaplace" => AlgoritmosFDM.JacobiLaplace,
        "GaussSeidel" => AlgoritmosFDM.GaussSeidel,
        "ForwardEuler" => AlgoritmosFDM.ForwardEuler,
        "CrankNicolson" => AlgoritmosFDM.CrankNicolson,
        "CentralDifference2" => AlgoritmosFDM.CentralDifference2,
        "CentralDifference4" => AlgoritmosFDM.CentralDifference4,
        _ => AlgoritmosFDM.BackwardEuler
    };

    private static AlgoritmoFDM AlgoritmoComplementario(AlgoritmoFDM algoritmo) =>
        algoritmo == AlgoritmosFDM.ForwardEuler
            ? AlgoritmosFDM.BackwardEuler
            : AlgoritmosFDM.ForwardEuler;

    private static string ObtenerNombreAlgoritmo(string nombre) => nombre switch
    {
        "ForwardEuler" => "Forward Euler",
        "BackwardEuler" => "Backward Euler",
        _ => nombre
    };

    private static string ObtenerComplementario(string nombre) => nombre switch
    {
        "ForwardEuler" => "BackwardEuler",
        "BackwardEuler" => "ForwardEuler",
        _ => nombre
    };
}
