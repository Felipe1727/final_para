using final_para.Aspectos;
using final_para.Estado;

namespace final_para.Tests.Aspectos;

public class HistoricoTest
{
    [Fact]
    public void RegistrarEvaluacion_AgregaAlHistorico()
    {
        var historico = new Historico();

        historico.RegistrarEvaluacion(new EstadoSolucionFDM(0.1, [[1.0]]));
        historico.RegistrarEvaluacion(new EstadoSolucionFDM(0.2, [[2.0]]));

        Assert.Equal(2, historico.ObtenerRegistros().Count);
    }

    [Fact]
    public void ObtenerRegistros_DevuelveReadOnly()
    {
        var historico = new Historico();
        historico.RegistrarEvaluacion(new EstadoSolucionFDM(0.5, [[0.0]]));

        var registros = historico.ObtenerRegistros();

        Assert.IsAssignableFrom<IReadOnlyList<EstadoSolucion>>(registros);
        Assert.IsNotType<List<EstadoSolucion>>(registros);
    }

    [Fact]
    public void ObtenerRegistros_RefleajeMutacionesPosteriores()
    {
        var historico = new Historico();
        var vista = historico.ObtenerRegistros();

        Assert.Empty(vista);

        historico.RegistrarEvaluacion(new EstadoSolucionFDM(0.0, [[0.0]]));

        Assert.Single(vista);
    }
}
