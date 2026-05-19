using final_para.Ecuaciones;

namespace final_para.Metodos;

public delegate double[][] AlgoritmoFDM(double[][] matriz, double[][] valoresConocidos);
public delegate double[] AlgoritmoFEM(double[][] matriz, double[][] valoresConocidos);
public delegate double FuncionBase(double[] coordenadas);

public abstract class MetodoNumerico
{
    protected double[][] Malla { get; set; }
    protected uint NumIteraciones { get; set; }
    protected DateTime TiempoInicio { get; set; }
    protected DateTime? TiempoFin { get; set; }
    protected Ecuacion Ecuacion { get; set; }

    protected MetodoNumerico(double[][] malla, Ecuacion ecuacion)
    {
        Malla = malla;
        Ecuacion = ecuacion;
        TiempoInicio = DateTime.Now;
    }

    public abstract double CalcularCostoComputacional();
    public abstract double CalcularError();
}
