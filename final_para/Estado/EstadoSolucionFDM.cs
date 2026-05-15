namespace final_para.Estado;

public class EstadoSolucionFDM : EstadoSolucion
{
    public double[][] ValorActual { get; private set; }

    public EstadoSolucionFDM(double residuo, double[][] valorActual)
        : base(residuo)
    {
        ValorActual = valorActual;
    }
}