using final_para.Aspectos;
using final_para.Ecuaciones;
using final_para.Metodos;
using final_para.Servicios;

namespace aplicacion_prueba.Ejemplos;

public static class EjemploPoisson2D
{
    public static void Ejecutar()
    {
        Console.WriteLine("=== Ejemplo 2: Poisson 2D (u_xx + u_yy = 0) — FDM vs FEM ===");

        var ecuacion = new EcuacionBuilder()
            .ConTerminos(new Termino("u_xx", true), new Termino("u_yy", true))
            .ConVariablesIndependientes("x", "y")
            .ConCondicionesFrontera("u(0,y)=0", "u(1,y)=1", "u(x,0)=0", "u(x,1)=0")
            .ConGeometria(Geometria.Eliptica)
            .Build();

        var publisherComputo = new PublisherComputo();
        var publisherError = new PublisherError();
        var historico = new Historico();
        var aspecto = new AspActualizarHistorico(historico);
        publisherComputo.Evento += aspecto.EventHandler;

        var fabrica = new FabricaMetodoNumerico(publisherComputo, publisherError);

        // FDM
        var mallaFdm = ConstruirMallaFdm(16);
        var fdm = fabrica.CrearFDM(
            mallaFdm, ecuacion,
            EsquemaTemporal.Implicito, 2, 1,
            AlgoritmosFDM.JacobiLaplace);
        var estadoFdm = fdm.Resolver();
        Console.WriteLine($"  FDM: residuo={estadoFdm.Residuo:E3}, error={fdm.CalcularError():E3}, costo={fdm.CalcularCostoComputacional():F4}s");

        // FEM
        var mallaFem = ConstruirMallaFem(8);
        var fem = fabrica.CrearFEM(
            mallaFem, ecuacion,
            TipoElemento.Triangular,
            final_para.Metodos.FuncionesBase.TriangularLineales,
            AlgoritmosFEM.Galerkin);
        var estadoFem = fem.Resolver();
        Console.WriteLine($"  FEM: residuo={estadoFem.Residuo:E3}, error={fem.CalcularError():E3}, costo={fem.CalcularCostoComputacional():F4}s");

        Console.WriteLine($"  Eventos registrados en histórico: {historico.ObtenerRegistros().Count}");
        ImprimirMatriz(estadoFdm.ValorActual);
    }

    private static double[][] ConstruirMallaFdm(int lado)
    {
        var m = new double[lado][];
        for (int i = 0; i < lado; i++)
        {
            m[i] = new double[lado];
            for (int j = 0; j < lado; j++)
            {
                if (j == lado - 1) m[i][j] = 1.0;
                else m[i][j] = 0.0;
            }
        }
        return m;
    }

    private static double[][] ConstruirMallaFem(int lado)
    {
        int total = lado * lado;
        var m = new double[total][];
        for (int i = 0; i < lado; i++)
            for (int j = 0; j < lado; j++)
                m[i * lado + j] = new[] { (double)j / (lado - 1), (double)i / (lado - 1) };
        return m;
    }

    private static void ImprimirMatriz(double[][] u)
    {
        Console.WriteLine("  Mapa de calor (solución FDM):");
        for (int i = 0; i < u.Length; i++)
        {
            Console.Write("    ");
            for (int j = 0; j < u[i].Length; j++)
                Console.Write(SimboloIntensidad(u[i][j]));
            Console.WriteLine();
        }
    }

    private static char SimboloIntensidad(double v)
    {
        if (v < 0.1) return ' ';
        if (v < 0.3) return '.';
        if (v < 0.5) return ':';
        if (v < 0.7) return '+';
        if (v < 0.9) return '*';
        return '#';
    }
}
