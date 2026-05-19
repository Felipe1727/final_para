using Castle.DynamicProxy;
using final_para.Aspectos;
using final_para.Ecuaciones;
using final_para.Metodos;

namespace final_para.Servicios;

public class FabricaMetodoNumerico
{
    private readonly ProxyGenerator _generador = new();
    private readonly PublisherComputo _publisherComputo;
    private readonly PublisherError _publisherError;

    public FabricaMetodoNumerico(
        PublisherComputo publisherComputo,
        PublisherError publisherError)
    {
        _publisherComputo = publisherComputo;
        _publisherError = publisherError;
    }

    public FDM CrearFDM(
        double[][] malla,
        Ecuacion ecuacion,
        EsquemaTemporal esquema,
        byte ordenEspacial,
        byte ordenTemporal,
        AlgoritmoFDM algoritmo,
        PublisherComputo? publisherEvolucion = null,
        string? nombreAlgoritmo = null)
    {
        var instancia = new FDM(malla, ecuacion, esquema, ordenEspacial, ordenTemporal, algoritmo, publisherEvolucion, nombreAlgoritmo);
        var argumentos = new object?[] { malla, ecuacion, esquema, ordenEspacial, ordenTemporal, algoritmo, publisherEvolucion, nombreAlgoritmo };
        return (FDM)_generador.CreateClassProxyWithTarget(
            typeof(FDM),
            instancia,
            argumentos,
            CrearInterceptores());
    }

    public FEM CrearFEM(
        double[][] malla,
        Ecuacion ecuacion,
        TipoElemento elemento,
        FuncionBase[] funcionesBase,
        AlgoritmoFEM algoritmo)
    {
        var instancia = new FEM(malla, ecuacion, elemento, funcionesBase, algoritmo);
        var argumentos = new object[] { malla, ecuacion, elemento, funcionesBase, algoritmo };
        return (FEM)_generador.CreateClassProxyWithTarget(
            typeof(FEM),
            instancia,
            argumentos,
            CrearInterceptores());
    }

    private IInterceptor[] CrearInterceptores() =>
        new IInterceptor[]
        {
            new ServicioInterceptorComputo(_publisherComputo),
            new ServicioInterceptorError(_publisherError)
        };
}
