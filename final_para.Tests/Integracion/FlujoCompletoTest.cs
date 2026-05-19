using final_para.Aspectos;
using final_para.Ecuaciones;
using final_para.Estado;
using final_para.Metodos;
using final_para.Servicios;

namespace final_para.Tests.Integracion;

[Collection("Aspectos")]
public class FlujoCompletoTest
{
    private static Ecuacion CrearEcuacionLaplace() =>
        new EcuacionBuilder()
            .ConTerminos(new Termino("u_xx", true), new Termino("u_yy", true))
            .ConVariablesIndependientes("x", "y")
            .ConGeometria(Geometria.Eliptica)
            .Build();

    private static double[][] CrearMallaCuadrada(int lado, double valorFrontera)
    {
        var malla = new double[lado][];
        for (int i = 0; i < lado; i++)
        {
            malla[i] = new double[lado];
            for (int j = 0; j < lado; j++)
            {
                bool frontera = i == 0 || i == lado - 1 || j == 0 || j == lado - 1;
                malla[i][j] = frontera ? valorFrontera : 0.0;
            }
        }
        return malla;
    }

    [Fact]
    public void FabricaCrearFDM_Resolver_RegistraEnHistorico()
    {
        var intervaloOriginal = ReglasAspectos.IntervaloCosto;
        try
        {
            ReglasAspectos.IntervaloCosto = 1;
            var publisherComputo = new PublisherComputo();
            var publisherError = new PublisherError();
            var historico = new Historico();
            var aspecto = new AspActualizarHistorico(historico);
            publisherComputo.Evento += aspecto.EventHandler;

            var fabrica = new FabricaMetodoNumerico(publisherComputo, publisherError);
            var malla = CrearMallaCuadrada(5, 1.0);
            var fdm = fabrica.CrearFDM(
                malla,
                CrearEcuacionLaplace(),
                EsquemaTemporal.Explicito,
                ordenEspacial: 2,
                ordenTemporal: 1,
                AlgoritmosFDM.JacobiLaplace);

            var estado = fdm.Resolver();

            Assert.NotNull(estado);
            Assert.True(historico.ObtenerRegistros().Count >= 1);
        }
        finally { ReglasAspectos.IntervaloCosto = intervaloOriginal; }
    }

    [Fact]
    public void FabricaCrearFEM_Resolver_RegistraEnHistorico()
    {
        var intervaloOriginal = ReglasAspectos.IntervaloCosto;
        try
        {
            ReglasAspectos.IntervaloCosto = 1;
            var publisherComputo = new PublisherComputo();
            var publisherError = new PublisherError();
            var historico = new Historico();
            var aspecto = new AspActualizarHistorico(historico);
            publisherComputo.Evento += aspecto.EventHandler;

            var fabrica = new FabricaMetodoNumerico(publisherComputo, publisherError);
            var malla = CrearMalla1D(10);
            var fem = fabrica.CrearFEM(
                malla,
                CrearEcuacionLaplace(),
                TipoElemento.Triangular,
                final_para.Metodos.FuncionesBase.TriangularLineales,
                AlgoritmosFEM.Galerkin);

            var estado = fem.Resolver();

            Assert.NotNull(estado);
            Assert.True(historico.ObtenerRegistros().Count >= 1);
        }
        finally { ReglasAspectos.IntervaloCosto = intervaloOriginal; }
    }

    [Fact]
    public void FDMSinFabrica_NoActivaPublishers()
    {
        var intervaloOriginal = ReglasAspectos.IntervaloCosto;
        try
        {
            ReglasAspectos.IntervaloCosto = 1;
            var publisherComputo = new PublisherComputo();
            int eventos = 0;
            publisherComputo.Evento += (_, _) => eventos++;

            var fdm = new FDM(
                CrearMallaCuadrada(5, 1.0),
                CrearEcuacionLaplace(),
                EsquemaTemporal.Explicito,
                2, 1,
                AlgoritmosFDM.JacobiLaplace);

            fdm.Resolver();

            Assert.Equal(0, eventos);
        }
        finally { ReglasAspectos.IntervaloCosto = intervaloOriginal; }
    }

    private static double[][] CrearMalla1D(int n)
    {
        var m = new double[n][];
        for (int i = 0; i < n; i++) m[i] = new[] { (double)i / (n - 1) };
        return m;
    }
}
