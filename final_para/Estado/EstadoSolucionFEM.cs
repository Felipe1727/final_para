namespace final_para.Estado;

public class EstadoSolucionFEM : EstadoSolucion
{
    public double[] ValorActual { get; private set; }

    public EstadoSolucionFEM(double residuo, double[] valorActual)
        : base(residuo)
    {
        ValorActual = valorActual;
    }
}