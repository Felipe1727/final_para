using final_para.Interfaces;
using final_para.Ecuaciones;

namespace final_para.Metodos;

// Delegates (modelados como stereotype=Delegate en el diagrama)
public delegate double[][] AlgoritmoFDM(double[][] matriz, double[][] valoresConocidos);
public delegate double[] AlgoritmoFEM(double[][] matriz, double[][] valoresConocidos);
public delegate double FuncionBase(double[] coordenadas);

public abstract class MetodoNumerico
{
    protected double[][] Malla { get; set; }
    protected uint NumIteraciones { get; set; }
    protected DateTime TiempoInicio { get; set; }
    protected Ecuacion Ecuacion { get; set; }

    // IResolver inyectado por DI
    protected IResolver<object> Resolver { get; set; }

    protected MetodoNumerico(
        double[][] malla,
        Ecuacion ecuacion,
        IResolver<object> resolver)
    {
        Malla = malla;
        Ecuacion = ecuacion;
        Resolver = resolver;
        TiempoInicio = DateTime.Now;
    }

    public abstract double CalcularCostoComputacional();
    public abstract double CalcularError();
}