namespace final_para.Estado;

public abstract class EstadoSolucion
{
    public double Residuo { get; protected set; }
    public int TamanoMalla { get; }
    public uint NumIteraciones { get; }
    public double TiempoSegundos { get; }
    public string MetodoNombre { get; }
    public DateTime TimestampEvento { get; }

    protected EstadoSolucion(
        double residuo,
        int tamanoMalla = 0,
        uint numIteraciones = 0,
        double tiempoSegundos = 0.0,
        string metodoNombre = "",
        DateTime? timestampEvento = null)
    {
        Residuo = residuo;
        TamanoMalla = tamanoMalla;
        NumIteraciones = numIteraciones;
        TiempoSegundos = tiempoSegundos;
        MetodoNombre = metodoNombre;
        TimestampEvento = timestampEvento ?? DateTime.Now;
    }
}
