using final_para.Aspectos;
using final_para.Estado;

namespace final_para.Tests.Aspectos;

[Collection("Aspectos")]
public class PublisherComputoTest
{
    [Fact]
    public void Disparar_IntervaloUno_DispararCadaInvocacion()
    {
        var intervaloOriginal = ReglasAspectos.IntervaloCosto;
        try
        {
            ReglasAspectos.IntervaloCosto = 1;
            var publisher = new PublisherComputo();
            int eventos = 0;
            publisher.Evento += (_, _) => eventos++;

            for (int i = 0; i < 5; i++)
                publisher.Disparar(new EstadoSolucionFDM(0.1, [[0.0]]));

            Assert.Equal(5, eventos);
        }
        finally { ReglasAspectos.IntervaloCosto = intervaloOriginal; }
    }

    [Fact]
    public void Disparar_IntervaloTres_DispararCadaTercerLlamada()
    {
        var intervaloOriginal = ReglasAspectos.IntervaloCosto;
        try
        {
            ReglasAspectos.IntervaloCosto = 3;
            var publisher = new PublisherComputo();
            int eventos = 0;
            publisher.Evento += (_, _) => eventos++;

            for (int i = 0; i < 9; i++)
                publisher.Disparar(new EstadoSolucionFDM(0.1, [[0.0]]));

            Assert.Equal(3, eventos);
        }
        finally { ReglasAspectos.IntervaloCosto = intervaloOriginal; }
    }

    [Fact]
    public void Disparar_SinSuscriptores_NoLanza()
    {
        var publisher = new PublisherComputo();
        var ex = Record.Exception(() => publisher.Disparar(new EstadoSolucionFDM(0.0, [[0.0]])));
        Assert.Null(ex);
    }
}
