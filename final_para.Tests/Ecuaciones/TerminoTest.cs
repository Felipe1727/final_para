using final_para.Ecuaciones;

namespace final_para.Tests.Ecuaciones;

public class TerminoTest
{
    [Fact]
    public void Termino_ConservaExpresionYSigno()
    {
        var t = new Termino("u_xx", true);

        Assert.Equal("u_xx", t.Expresion);
        Assert.True(t.EsPositivo);
    }

    [Fact]
    public void Termino_RecordEqualityComparaPorValor()
    {
        var a = new Termino("u_yy", false);
        var b = new Termino("u_yy", false);
        var c = new Termino("u_yy", true);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
