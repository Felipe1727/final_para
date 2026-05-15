using final_para.Estado;

namespace final_para.Aspectos;

public class Historico
{
    private List<EstadoSolucion> Registros { get; set; } = [];

    public Historico() { }

    public void RegistrarEvaluacion(EstadoSolucion estado)
    {
        Registros.Add(estado);
    }

    public IReadOnlyList<EstadoSolucion> ObtenerRegistros() => Registros.AsReadOnly();
}