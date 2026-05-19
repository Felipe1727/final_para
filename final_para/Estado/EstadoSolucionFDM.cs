namespace final_para.Estado;

public class EstadoSolucionFDM : EstadoSolucion
{
    public double[][] ValorActual { get; private set; }

    public EstadoSolucionFDM(double residuo, double[][] valorActual)
        : base(residuo)
    {
        ValorActual = valorActual;
    }

    public EstadoSolucionFDM(
        double residuo,
        double[][] valorActual,
        int tamanoMalla,
        uint numIteraciones,
        double tiempoSegundos,
        string metodoNombre,
        DateTime? timestampEvento = null)
        : base(residuo, tamanoMalla, numIteraciones, tiempoSegundos, metodoNombre, timestampEvento)
    {
        ValorActual = valorActual;
    }
}
