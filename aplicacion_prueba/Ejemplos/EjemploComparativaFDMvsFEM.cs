using final_para.Aspectos;
using final_para.Ecuaciones;
using final_para.Metodos;
using final_para.Servicios;

namespace aplicacion_prueba.Ejemplos;

public static class EjemploComparativaFDMvsFEM
{
    public static void Ejecutar()
    {
        Console.WriteLine("=== Ejemplo 3: Comparativa FDM vs FEM por tamaño de malla ===");
        Console.WriteLine($"  {"Lado",6} {"Método",8} {"Error",14} {"Costo (s)",12}");

        var ecuacion = new EcuacionBuilder()
            .ConTerminos(new Termino("u_xx", true), new Termino("u_yy", true))
            .ConVariablesIndependientes("x", "y")
            .ConGeometria(Geometria.Eliptica)
            .Build();

        var publisherComputo = new PublisherComputo();
        var publisherError = new PublisherError();
        var fabrica = new FabricaMetodoNumerico(publisherComputo, publisherError);

        foreach (var lado in new[] { 6, 10, 14 })
        {
            var (errF, costoF) = MedirFdm(fabrica, ecuacion, lado);
            Console.WriteLine($"  {lado,6} {"FDM",8} {errF,14:E3} {costoF,12:F4}");

            var (errE, costoE) = MedirFem(fabrica, ecuacion, lado);
            Console.WriteLine($"  {lado,6} {"FEM",8} {errE,14:E3} {costoE,12:F4}");
        }
    }

    private static (double error, double costo) MedirFdm(FabricaMetodoNumerico fabrica, Ecuacion ec, int lado)
    {
        var malla = new double[lado][];
        for (int i = 0; i < lado; i++)
        {
            malla[i] = new double[lado];
            for (int j = 0; j < lado; j++)
                malla[i][j] = j == lado - 1 ? 1.0 : 0.0;
        }

        var fdm = fabrica.CrearFDM(malla, ec, EsquemaTemporal.Implicito, 2, 1, AlgoritmosFDM.JacobiLaplace);
        fdm.Resolver();
        return (fdm.CalcularError(), fdm.CalcularCostoComputacional());
    }

    private static (double error, double costo) MedirFem(FabricaMetodoNumerico fabrica, Ecuacion ec, int lado)
    {
        int total = lado * lado;
        var malla = new double[total][];
        for (int i = 0; i < lado; i++)
            for (int j = 0; j < lado; j++)
                malla[i * lado + j] = new[] { (double)j / (lado - 1), (double)i / (lado - 1) };

        var fem = fabrica.CrearFEM(
            malla, ec,
            TipoElemento.Triangular,
            final_para.Metodos.FuncionesBase.TriangularLineales,
            AlgoritmosFEM.Galerkin);
        fem.Resolver();
        return (fem.CalcularError(), fem.CalcularCostoComputacional());
    }
}
