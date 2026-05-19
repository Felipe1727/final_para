using final_para.Aspectos;
using final_para.Estado;

namespace final_para.Tests.Aspectos;

[Collection("Aspectos")]
public class PublisherErrorTest
{
    [Fact]
    public void Disparar_ResiduoBajoUmbral_Dispara()
    {
        var original = ReglasAspectos.IntervaloError;
        try
        {
            ReglasAspectos.IntervaloError = 1e-3;
            var publisher = new PublisherError();
            int eventos = 0;
            publisher.Evento += (_, _) => eventos++;

            publisher.Disparar(new EstadoSolucionFDM(1e-5, [[0.0]]));

            Assert.Equal(1, eventos);
        }
        finally { ReglasAspectos.IntervaloError = original; }
    }

    [Fact]
    public void Disparar_ResiduoEnLimite_Dispara()
    {
        var original = ReglasAspectos.IntervaloError;
        try
        {
            ReglasAspectos.IntervaloError = 1e-3;
            var publisher = new PublisherError();
            int eventos = 0;
            publisher.Evento += (_, _) => eventos++;

            publisher.Disparar(new EstadoSolucionFDM(1e-3, [[0.0]]));

            Assert.Equal(1, eventos);
        }
        finally { ReglasAspectos.IntervaloError = original; }
    }

    [Fact]
    public void Disparar_ResiduoSobreUmbral_NoDispara()
    {
        var original = ReglasAspectos.IntervaloError;
        try
        {
            ReglasAspectos.IntervaloError = 1e-6;
            var publisher = new PublisherError();
            int eventos = 0;
            publisher.Evento += (_, _) => eventos++;

            publisher.Disparar(new EstadoSolucionFDM(1.0, [[0.0]]));

            Assert.Equal(0, eventos);
        }
        finally { ReglasAspectos.IntervaloError = original; }
    }
}
