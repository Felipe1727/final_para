namespace final_para.Estado;

public class EstadoSolucionFEM : EstadoSolucion
{
    public double[] ValorActual { get; private set; }

    public EstadoSolucionFEM(double residuo, double[] valorActual)
        : base(residuo)
    {
        ValorActual = valorActual;
    }

    public EstadoSolucionFEM(
        double residuo,
        double[] valorActual,
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
