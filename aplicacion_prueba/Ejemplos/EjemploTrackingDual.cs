using System.Globalization;
using final_para.Aspectos;
using final_para.Ecuaciones;
using final_para.Estado;
using final_para.Metodos;
using final_para.Servicios;

namespace aplicacion_prueba.Ejemplos;

public static class EjemploTrackingDual
{
    private static readonly int[] TamanosBarrido = { 8, 12, 16, 20, 24 };
    private const int NParaEvolucion = 24;

    public static void Ejecutar(bool exportCsv = false)
    {
        Console.WriteLine("=== Ejemplo 4: Tracking dual (comparativo + evolución FDM) ===");
        Console.WriteLine($"  Barrido N: [{string.Join(", ", TamanosBarrido)}]");
        Console.WriteLine($"  Intervalo de evolución FDM: cada {(int)ReglasAspectos.IntervaloEvolucion} iter");
        Console.WriteLine();

        var publisherComparativo = new PublisherComputo(intervalo: 1);
        var publisherEvolucion = new PublisherComputo(intervalo: (int)ReglasAspectos.IntervaloEvolucion);
        var publisherError = new PublisherError();

        var historicoComparativo = new Historico();
        var historicoEvolucion = new Historico();
        publisherComparativo.Evento += new AspActualizarHistorico(historicoComparativo).EventHandler;
        publisherEvolucion.Evento += new AspActualizarHistorico(historicoEvolucion).EventHandler;

        var fabrica = new FabricaMetodoNumerico(publisherComparativo, publisherError);
        var barrido = new BarridoMallas(fabrica);

        var ecuacion = ConstruirLaplace();

        barrido.EjecutarFDM(
            TamanosBarrido, ecuacion,
            EsquemaTemporal.Implicito, 2, 1,
            AlgoritmosFDM.JacobiLaplace,
            GenerarMallaFdm,
            publisherEvolucion);

        barrido.EjecutarFEM(
            TamanosBarrido, ecuacion,
            TipoElemento.Triangular,
            FuncionesBase.TriangularLineales,
            AlgoritmosFEM.Galerkin,
            GenerarMallaFem);

        ImprimirTablaComparativa(historicoComparativo);
        Console.WriteLine();
        ImprimirEvolucionFdm(historicoEvolucion, NParaEvolucion);

        if (exportCsv)
        {
            ExportarCsv("historico-comparativo.csv", historicoComparativo, incluirIter: true);
            ExportarCsv("historico-evolucion.csv", historicoEvolucion, incluirIter: true);
            Console.WriteLine();
            Console.WriteLine("  CSV exportados: historico-comparativo.csv, historico-evolucion.csv");
        }
    }

    private static Ecuacion ConstruirLaplace() =>
        new EcuacionBuilder()
            .ConTerminos(new Termino("u_xx", true), new Termino("u_yy", true))
            .ConVariablesIndependientes("x", "y")
            .ConGeometria(Geometria.Eliptica)
            .Build();

    private static double[][] GenerarMallaFdm(int lado)
    {
        var m = new double[lado][];
        for (int i = 0; i < lado; i++)
        {
            m[i] = new double[lado];
            for (int j = 0; j < lado; j++)
                m[i][j] = j == lado - 1 ? 1.0 : 0.0;
        }
        return m;
    }

    private static double[][] GenerarMallaFem(int lado)
    {
        int total = lado * lado;
        var m = new double[total][];
        for (int i = 0; i < lado; i++)
            for (int j = 0; j < lado; j++)
                m[i * lado + j] = new[] { (double)j / (lado - 1), (double)i / (lado - 1) };
        return m;
    }

    private static int LadoLogico(EstadoSolucion r) =>
        r.MetodoNombre.StartsWith("FEM", StringComparison.Ordinal)
            ? (int)Math.Round(Math.Sqrt(r.TamanoMalla))
            : r.TamanoMalla;

    private static void ImprimirTablaComparativa(Historico historico)
    {
        Console.WriteLine("=== Comparativa FDM vs FEM por tamaño de malla (lado N) ===");
        Console.WriteLine($"  {"N",4} | {"Método",-18} | {"Tiempo (s)",12} | {"Residuo",12} | {"Iter",6}");
        Console.WriteLine($"  {new string('-', 4)} | {new string('-', 18)} | {new string('-', 12)} | {new string('-', 12)} | {new string('-', 6)}");

        var registros = historico.ObtenerRegistros()
            .Select(r => new { Lado = LadoLogico(r), Registro = r })
            .OrderBy(x => x.Lado)
            .ThenBy(x => x.Registro.MetodoNombre);

        foreach (var x in registros)
            Console.WriteLine(
                $"  {x.Lado,4} | {x.Registro.MetodoNombre,-18} | {x.Registro.TiempoSegundos,12:F6} | {x.Registro.Residuo,12:E3} | {x.Registro.NumIteraciones,6}");
    }

    private static void ImprimirEvolucionFdm(Historico historico, int nObjetivo)
    {
        var snapshots = historico.ObtenerRegistros()
            .Where(r => r.TamanoMalla == nObjetivo)
            .OrderBy(r => r.NumIteraciones)
            .ToList();

        Console.WriteLine($"=== Evolución FDM N={nObjetivo} — residuo vs iteración ===");

        if (snapshots.Count == 0)
        {
            Console.WriteLine($"  (sin snapshots para N={nObjetivo})");
            return;
        }

        double maxLog = snapshots.Max(s => Math.Log10(Math.Max(s.Residuo, 1e-300)));
        double minLog = snapshots.Min(s => Math.Log10(Math.Max(s.Residuo, 1e-300)));
        double rango = Math.Max(maxLog - minLog, 1e-9);
        const int anchoMax = 50;

        Console.WriteLine($"  {"iter",6} | {"residuo",12} | barra (log₁₀)");
        foreach (var s in snapshots)
        {
            double l = Math.Log10(Math.Max(s.Residuo, 1e-300));
            int ancho = (int)Math.Round((l - minLog) / rango * anchoMax);
            string barra = new string('█', Math.Max(ancho, 1));
            Console.WriteLine($"  {s.NumIteraciones,6} | {s.Residuo,12:E3} | {barra}");
        }
        Console.WriteLine($"  (escala: log₁₀(residuo) ∈ [{minLog:F2}, {maxLog:F2}])");
    }

    private static void ExportarCsv(string ruta, Historico historico, bool incluirIter)
    {
        var cultura = CultureInfo.InvariantCulture;
        using var writer = new StreamWriter(ruta);
        writer.WriteLine("metodo,N,iter,tiempo_s,residuo,timestamp");
        foreach (var r in historico.ObtenerRegistros())
        {
            writer.WriteLine(string.Join(",",
                r.MetodoNombre,
                r.TamanoMalla.ToString(cultura),
                r.NumIteraciones.ToString(cultura),
                r.TiempoSegundos.ToString("G", cultura),
                r.Residuo.ToString("G", cultura),
                r.TimestampEvento.ToString("O", cultura)));
        }
    }
}
