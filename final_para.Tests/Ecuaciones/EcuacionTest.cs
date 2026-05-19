using final_para.Ecuaciones;

namespace final_para.Tests.Ecuaciones;

public class EcuacionTest
{
    private static Ecuacion Crear(params Termino[] terminos) =>
        new EcuacionBuilder()
            .ConTerminos(terminos)
            .ConVariablesIndependientes("x", "y")
            .Build();

    [Fact]
    public void ConstruirFuncion_FormateaSignosCorrectamente()
    {
        var ec = Crear(
            new Termino("u_xx", true),
            new Termino("u_yy", true),
            new Termino("f", false));

        var resultado = ec.ConstruirFuncion();

        Assert.Equal("u_xx + u_yy - f = 0", resultado);
    }

    [Fact]
    public void ConstruirFuncion_PrimerTerminoNegativo_AnteponeMenos()
    {
        var ec = Crear(new Termino("u_xx", false), new Termino("u_yy", true));

        Assert.Equal("-u_xx + u_yy = 0", ec.ConstruirFuncion());
    }

    [Fact]
    public void EsHomogenea_TodosLosTerminosTienenDerivada_True()
    {
        var ec = Crear(new Termino("u_xx", true), new Termino("u_yy", true));

        Assert.True(ec.EsHomogenea);
    }

    [Fact]
    public void EsHomogenea_HayTerminoSinDerivada_False()
    {
        var ec = Crear(new Termino("u_xx", true), new Termino("f", false));

        Assert.False(ec.EsHomogenea);
    }

    [Fact]
    public void TerminosForzantes_FiltraTerminosSinDerivada()
    {
        var ec = Crear(
            new Termino("u_xx", true),
            new Termino("u_yy", true),
            new Termino("g", false),
            new Termino("5", false));

        var forzantes = ec.TerminosForzantes.ToList();

        Assert.Equal(2, forzantes.Count);
        Assert.Contains(forzantes, t => t.Expresion == "g");
        Assert.Contains(forzantes, t => t.Expresion == "5");
    }
}
