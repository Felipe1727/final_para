namespace final_para.Estado;

public abstract class EstadoSolucion
{
    public double Residuo { get; protected set; }

    protected EstadoSolucion(double residuo)
    {
        Residuo = residuo;
    }
}