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
/// Orquesta una resolución simultánea con FDM y FEM. Lee la ecuación de sesión,
/// instancia los métodos vía FabricaMetodoNumerico (proxy con interceptores), suscribe
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

        var (mallaFDM, ejeX, ejeT, nombreEje2) = _generadorMalla.ConstruirMallaFDM(config, ecuacion);
        var (mallaFEM, ladoFEM, _) = _generadorMalla.ConstruirMallaFEM(config, ecuacion);

        var n1 = mallaFDM.Length;
        var n2 = n1 > 0 ? mallaFDM[0].Length : 0;
        var publisherComputo = new PublisherComputo(intervalo: Math.Max(1, (n1 * n2) / 100));
        var publisherError = new PublisherError();
        var fabrica = new FabricaMetodoNumerico(publisherComputo, publisherError);

        // Suscripción: cada evento del publisher empuja al cliente vía SignalR (grupo = sessionId).
        publisherComputo.Evento += async (_, estado) =>
        {
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
        var tipoElemento = ParsearTipoElemento(config.TipoElemento);
        var funcionesBase = SeleccionarFuncionesBase(tipoElemento);
        var algoritmoFEM = ResolverAlgoritmoFEM(config.AlgoritmoFEM);

        var fdm = fabrica.CrearFDM(mallaFDM, ecuacion, esquema, ordenEspacial: 2, ordenTemporal: 1,
            algoritmoFDM, publisherEvolucion: publisherComputo);

        var fem = fabrica.CrearFEM(mallaFEM, ecuacion, tipoElemento, funcionesBase, algoritmoFEM);

        // Ejecutar ambos en paralelo. Cada Resolver() es síncrono y bloqueante, así que Task.Run.
        var tareaFDM = Task.Run(() => fdm.Resolver(), cancellationToken);
        var tareaFEM = Task.Run(() => fem.Resolver(), cancellationToken);

        await Task.WhenAll(tareaFDM, tareaFEM);

        var estadoFDM = await tareaFDM;
        var estadoFEM = await tareaFEM;

        var resultado = ConstruirResultado(estadoFDM, estadoFEM, ejeX, ejeT, ladoFEM, fdm, fem, nombreEje2);

        await _hub.Clients.Group(sessionId).SendAsync("ResolucionCompleta", resultado.Id, cancellationToken);

        return resultado;
    }

    private static ResultadoResolucionVM ConstruirResultado(
        EstadoSolucionFDM estadoFDM,
        EstadoSolucionFEM estadoFEM,
        double[] ejeX,
        double[] ejeT,
        int ladoFEM,
        FDM fdm,
        FEM fem,
        string nombreEje2)
    {
        var mallaFDMResultado = estadoFDM.ValorActual;
        var mallaFEMResultado = ReorganizarFEM(estadoFEM.ValorActual, ladoFEM);

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
            MetodoNombre = estadoFEM.MetodoNombre,
            TiempoSegundos = estadoFEM.TiempoSegundos,
            Residuo = estadoFEM.Residuo,
            Error = fem.CalcularError(),
            NumIteraciones = estadoFEM.NumIteraciones,
            TamanoMalla = estadoFEM.TamanoMalla
        };

        var comparacion = new ComparacionVM
        {
            DiferenciaTiempo = metricasFEM.TiempoSegundos - metricasFDM.TiempoSegundos,
            DiferenciaError = metricasFEM.Error - metricasFDM.Error,
            GanadorTiempo = metricasFDM.TiempoSegundos <= metricasFEM.TiempoSegundos ? "FDM" : "FEM",
            GanadorError = metricasFDM.Error <= metricasFEM.Error ? "FDM" : "FEM"
        };

        return new ResultadoResolucionVM
        {
            Id = Guid.NewGuid().ToString("N"),
            MallaFDM = mallaFDMResultado,
            MallaFEM = mallaFEMResultado,
            EjeX = ejeX,
            EjeY = ejeT,
            NombreEje2 = nombreEje2,
            MetricasFDM = metricasFDM,
            MetricasFEM = metricasFEM,
            Comparacion = comparacion
        };
    }

    private static double[][] ReorganizarFEM(double[] solucion, int lado)
    {
        if (lado * lado != solucion.Length)
        {
            // Fallback: devolver como una sola fila.
            return new double[][] { solucion };
        }

        var malla = new double[lado][];
        for (int i = 0; i < lado; i++)
        {
            malla[i] = new double[lado];
            for (int j = 0; j < lado; j++)
                malla[i][j] = solucion[i * lado + j];
        }
        return malla;
    }

    private static EsquemaTemporal ParsearEsquema(string s) => s switch
    {
        "Explicito" => EsquemaTemporal.Explicito,
        "CrankNicolson" => EsquemaTemporal.CrankNicolson,
        _ => EsquemaTemporal.Implicito
    };

    private static TipoElemento ParsearTipoElemento(string s) => s switch
    {
        "Triangular" => TipoElemento.Triangular,
        _ => TipoElemento.Cuadrilateral
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

    private static AlgoritmoFEM ResolverAlgoritmoFEM(string nombre) => nombre switch
    {
        "PetrovGalerkin" => AlgoritmosFEM.PetrovGalerkin,
        "GradienteConjugado" => AlgoritmosFEM.GradienteConjugado,
        _ => AlgoritmosFEM.Galerkin
    };

    private static FuncionBase[] SeleccionarFuncionesBase(TipoElemento tipo) => tipo switch
    {
        TipoElemento.Triangular => FuncionesBase.TriangularLineales,
        _ => FuncionesBase.CuadrilateralBilineales
    };
}
