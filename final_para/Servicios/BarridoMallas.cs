using final_para.Aspectos;
using final_para.Ecuaciones;
using final_para.Metodos;

namespace final_para.Servicios;

public class BarridoMallas
{
    private readonly FabricaMetodoNumerico _fabrica;

    public BarridoMallas(FabricaMetodoNumerico fabrica)
    {
        _fabrica = fabrica;
    }

    public void EjecutarFDM(
        int[] tamanos,
        Ecuacion ecuacion,
        EsquemaTemporal esquema,
        byte ordenEspacial,
        byte ordenTemporal,
        AlgoritmoFDM algoritmo,
        Func<int, double[][]> generadorMalla,
        PublisherComputo? publisherEvolucion = null)
    {
        foreach (var n in tamanos)
        {
            var malla = generadorMalla(n);
            var metodo = _fabrica.CrearFDM(
                malla, ecuacion, esquema, ordenEspacial, ordenTemporal, algoritmo, publisherEvolucion);
            metodo.Resolver();
        }
    }

    public void EjecutarFEM(
        int[] tamanos,
        Ecuacion ecuacion,
        TipoElemento elemento,
        FuncionBase[] funcionesBase,
        AlgoritmoFEM algoritmo,
        Func<int, double[][]> generadorMalla)
    {
        foreach (var n in tamanos)
        {
            var malla = generadorMalla(n);
            var metodo = _fabrica.CrearFEM(
                malla, ecuacion, elemento, funcionesBase, algoritmo);
            metodo.Resolver();
        }
    }
}
