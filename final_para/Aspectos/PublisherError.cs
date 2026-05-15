using final_para.Estado;

namespace final_para.Aspectos;

public class PublisherError
{
    public event EventHandler<EstadoSolucion>? Evento;

    public PublisherError() { }

    public void Disparar(EstadoSolucion estado)
    {
        if (estado.Residuo <= ReglasAspectos.IntervaloError)
            Evento?.Invoke(this, estado);
    }
}