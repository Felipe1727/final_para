using final_para.Aspectos;
using final_para.Ecuaciones;
using final_para.Metodos;
using final_para.Servicios;

namespace aplicacion_prueba.Ejemplos;

public static class EjemploCalor1D
{
    public static void Ejecutar()
    {
        Console.WriteLine("=== Ejemplo 1: Ecuación de calor 1D (u_t - u_xx = 0) ===");

        var ecuacion = new EcuacionBuilder()
            .ConTerminos(new Termino("u_t", true), new Termino("u_xx", false))
            .ConVariablesIndependientes("x", "t")
            .ConCondicionesIniciales("u(x,0)=sin(pi*x)")
            .ConCondicionesFrontera("u(0,t)=0", "u(1,t)=0")
            .ConGeometria(Geometria.Parabolica)
            .DependeDelTiempo(true)
            .Build();

        var publisherComputo = new PublisherComputo();
        var publisherError = new PublisherError();
        var historico = new Historico();
        var aspecto = new AspActualizarHistorico(historico);
        publisherComputo.Evento += aspecto.EventHandler;
        publisherError.Evento += aspecto.EventHandler;

        var malla = GenerarPulsoInicial(filas: 1, cols: 20);

        var fabrica = new FabricaMetodoNumerico(publisherComputo, publisherError);
        var fdm = fabrica.CrearFDM(
            malla, ecuacion,
            EsquemaTemporal.Explicito,
            ordenEspacial: 2, ordenTemporal: 1,
            AlgoritmosFDM.ForwardEuler);

        var estado = fdm.Resolver();

        Console.WriteLine($"  Iteraciones realizadas: {historico.ObtenerRegistros().Count}");
        Console.WriteLine($"  Residuo final: {estado.Residuo:E3}");
        Console.WriteLine($"  Costo computacional: {fdm.CalcularCostoComputacional():F4} s");
        Console.WriteLine($"  Error final: {fdm.CalcularError():E3}");
        ImprimirPerfil(estado.ValorActual[0]);
    }

    private static double[][] GenerarPulsoInicial(int filas, int cols)
    {
        var m = new double[filas][];
        for (int i = 0; i < filas; i++)
        {
            m[i] = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                double x = (double)j / (cols - 1);
                m[i][j] = j == 0 || j == cols - 1 ? 0.0 : Math.Sin(Math.PI * x);
            }
        }
        return m;
    }

    private static void ImprimirPerfil(double[] fila)
    {
        Console.Write("  Perfil final: ");
        for (int j = 0; j < fila.Length; j++)
            Console.Write($"{fila[j]:F3} ");
        Console.WriteLine();
    }
}
