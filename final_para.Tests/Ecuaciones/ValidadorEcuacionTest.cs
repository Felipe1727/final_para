using final_para.Ecuaciones;

namespace final_para.Tests.Ecuaciones;

public class ValidadorEcuacionTest
{
    private static Ecuacion Crear(Action<EcuacionBuilder> configurar)
    {
        var builder = new EcuacionBuilder()
            .ConTerminos(new Termino("u_xx", true))
            .ConVariablesIndependientes("x");
        configurar(builder);
        return builder.Build();
    }

    [Fact]
    public void Validar_EcuacionMinimaCoherente_EsValida()
    {
        var ec = Crear(_ => { });

        var resultado = ValidadorEcuacion.Validar(ec);

        Assert.True(resultado.EsValida);
        Assert.Empty(resultado.Errores);
    }

    [Fact]
    public void Validar_OrdenDeclaradoMenorQueDetectado_ReportaError()
    {
        var ec = new EcuacionBuilder()
            .ConTerminos(new Termino("u_xxx", true))
            .ConVariablesIndependientes("x")
            .ConOrden(2)
            .Build();

        var resultado = ValidadorEcuacion.Validar(ec);

        Assert.False(resultado.EsValida);
        Assert.Contains(resultado.Errores, e => e.Contains("Orden"));
    }

    [Fact]
    public void Validar_DerivadaTemporalSinFlag_ReportaError()
    {
        var ec = new EcuacionBuilder()
            .ConTerminos(new Termino("u_t", true), new Termino("u_xx", false))
            .ConVariablesIndependientes("x", "t")
            .DependeDelTiempo(false)
            .Build();

        var resultado = ValidadorEcuacion.Validar(ec);

        Assert.False(resultado.EsValida);
        Assert.Contains(resultado.Errores, e => e.Contains("temporal"));
    }

    [Fact]
    public void Validar_DependeDelTiempoSinCondicionInicial_ReportaError()
    {
        var ec = new EcuacionBuilder()
            .ConTerminos(new Termino("u_t", true), new Termino("u_xx", false))
            .ConVariablesIndependientes("x", "t")
            .DependeDelTiempo(true)
            .Build();

        var resultado = ValidadorEcuacion.Validar(ec);

        Assert.False(resultado.EsValida);
        Assert.Contains(resultado.Errores, e => e.Contains("condición inicial"));
    }

    [Fact]
    public void Validar_MarcadaLinealConTerminoNoLineal_ReportaError()
    {
        var ec = new EcuacionBuilder()
            .ConTerminos(new Termino("u_xx ^ 2", true))
            .ConVariablesIndependientes("x")
            .Lineal(true)
            .Build();

        var resultado = ValidadorEcuacion.Validar(ec);

        Assert.False(resultado.EsValida);
        Assert.Contains(resultado.Errores, e => e.Contains("no lineal"));
    }

    [Fact]
    public void Validar_VariableNoDeclarada_ReportaError()
    {
        var ec = new EcuacionBuilder()
            .ConTerminos(new Termino("u_xx + y", true))
            .ConVariablesIndependientes("x")
            .Build();

        var resultado = ValidadorEcuacion.Validar(ec);

        Assert.False(resultado.EsValida);
        Assert.Contains(resultado.Errores, e => e.Contains("'y'"));
    }

    [Fact]
    public void Validar_AcumulaMultiplesErrores()
    {
        var ec = new EcuacionBuilder()
            .ConTerminos(new Termino("u_t", true), new Termino("u_xx ^ 2", false))
            .ConVariablesIndependientes("x", "t")
            .DependeDelTiempo(false)
            .Lineal(true)
            .Build();

        var resultado = ValidadorEcuacion.Validar(ec);

        Assert.False(resultado.EsValida);
        Assert.True(resultado.Errores.Count >= 2);
    }
}
