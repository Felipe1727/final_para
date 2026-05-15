using final_para.Estado;

namespace final_para.Aspectos;

public class PublisherComputo
{
    public event EventHandler<EstadoSolucion>? Evento;

    public PublisherComputo() { }

    public void Disparar(EstadoSolucion estado)
    {
        if (estado.Residuo % ReglasAspectos.IntervaloCosto == 0)
            Evento?.Invoke(this, estado);
    }
}