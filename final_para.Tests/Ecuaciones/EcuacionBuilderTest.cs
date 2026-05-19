using final_para.Ecuaciones;

namespace final_para.Tests.Ecuaciones;

public class EcuacionBuilderTest
{
    [Fact]
    public void Build_CadenaFluidaConTodosLosCampos_PoblaPropiedades()
    {
        var ecuacion = new EcuacionBuilder()
            .ConTerminos(new Termino("u_xx", true), new Termino("u_t", false))
            .ConVariablesDependientes("u")
            .ConVariablesIndependientes("x", "t")
            .ConOrden(2)
            .ConCondicionesIniciales("u(x,0)=0")
            .ConCondicionesFrontera("u(0,t)=0", "u(1,t)=0")
            .Lineal(true)
            .ConGeometria(Geometria.Parabolica)
            .DependeDelTiempo(true)
            .Build();

        Assert.Equal(2, ecuacion.Terminos.Length);
        Assert.Equal(new[] { "u" }, ecuacion.VariablesDependientes);
        Assert.Equal(new[] { "x", "t" }, ecuacion.VariablesIndependientes);
        Assert.Equal((byte)2, ecuacion.Orden);
        Assert.Single(ecuacion.CondicionesIniciales);
        Assert.Equal(2, ecuacion.CondicionesFrontera.Length);
        Assert.True(ecuacion.Lineal);
        Assert.Equal(Geometria.Parabolica, ecuacion.Geometria);
        Assert.True(ecuacion.DependenciaTiempo);
    }

    [Fact]
    public void Build_SoloMinimo_AplicaDefaults()
    {
        var ecuacion = new EcuacionBuilder()
            .ConTerminos(new Termino("u_xx", true))
            .ConVariablesIndependientes("x")
            .Build();

        Assert.Equal((byte)2, ecuacion.Orden);
        Assert.True(ecuacion.Lineal);
        Assert.Equal(Geometria.Eliptica, ecuacion.Geometria);
        Assert.False(ecuacion.DependenciaTiempo);
        Assert.Equal(new[] { "u" }, ecuacion.VariablesDependientes);
    }

    [Fact]
    public void Build_SinTerminos_Lanza()
    {
        var builder = new EcuacionBuilder().ConVariablesIndependientes("x");

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_SinVariablesIndependientes_Lanza()
    {
        var builder = new EcuacionBuilder().ConTerminos(new Termino("u_xx", true));

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }
}
