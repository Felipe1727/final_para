using final_para.Estado;

namespace final_para.Aspectos;

public class PublisherComputo
{
    private int _contador = 0;
    private readonly int _intervaloFijo;

    public event EventHandler<EstadoSolucion>? Evento;

    public PublisherComputo(int intervalo = 0)
    {
        _intervaloFijo = intervalo;
    }

    private int IntervaloActivo =>
        _intervaloFijo > 0 ? _intervaloFijo : Math.Max(1, (int)ReglasAspectos.IntervaloCosto);

    public void Disparar(EstadoSolucion estado)
    {
        _contador++;
        if (_contador % IntervaloActivo == 0)
            Evento?.Invoke(this, estado);
    }

    public void Disparar(Func<EstadoSolucion> fabricaEstado)
    {
        _contador++;
        if (_contador % IntervaloActivo == 0)
            Evento?.Invoke(this, fabricaEstado());
    }
}
