using final_para.Estado;

namespace final_para.Aspectos;

public class AspActualizarHistorico
{
    private readonly Historico _historico;

    public AspActualizarHistorico(Historico historico)
    {
        _historico = historico;
    }

    public void EventHandler(object? sender, EstadoSolucion estado)
    {
        _historico.RegistrarEvaluacion(estado);
    }
}