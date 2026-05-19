using final_para.Algebra.Solvers;
using final_para.Discretizacion.FDM;
using final_para.Ecuaciones;
using final_para.Metodos;
using final_para.Metricas;
using final_para.Tests.Util;
using Xunit;

namespace final_para.Tests;

// Validación numérica FDM modo elíptico.
//
// PDE:  Δu = 0 en [0,1]², con frontera homogénea (Dirichlet u=0).
// Exacta:  u ≡ 0 (solución trivial — pero ahora ES la solución correcta,
//          a diferencia del bug FEM original que daba u≡0 por f mal armado).
//
// Estos tests verifican:
//   • La pipeline FDM elíptica (EsquemaLaplaceFDM + ISolverLineal) corre
//     sin errores y produce una solución consistente con la frontera.
//   • Para Poisson con fuente conocida, la escala de la solución es
//     correcta (no off por h² como con el bug D1 de stencils crudos).
public class LaplaceFDMTests
{
    private static Ecuacion ConstruirPoissonConFuenteUnitaria()
    {
        // -u_xx - u_yy - 1 = 0  ⇒  -Δu = 1 (Poisson con fuente unitaria)
        return new EcuacionBuilder()
            .ConTerminos(
                new Termino("u_xx", EsPositivo: false),
                new Termino("u_yy", EsPositivo: false),
                new Termino("1", EsPositivo: false))
            .ConVariablesIndependientes("x", "y")
            .ConGeometria(Geometria.Eliptica)
            .ConOrden(2)
            .Build();
    }

    [Fact]
    public void PoissonFDM_LU_SolucionPositivaEnInterior()
    {
        // Para -Δu = 1 con u=0 en la frontera del cuadrado unitario:
        // la solución es analíticamente positiva en el interior y el máximo
        // (en el centro) tiende a ≈ 0.0737 para malla fina. Verificamos
        // que la solución no es trivial ni negativa.
        int nx = 21;
        double dx = 1.0 / (nx - 1);
        var malla = MallaBuilder.MallaFDM(nx);
        var ecuacion = ConstruirPoissonConFuenteUnitaria();

        var esquema = new EsquemaLaplaceFDM(malla, ecuacion, dx, dx);
        var fdm = new FDM(
            malla, ecuacion,
            esquema,
            new SolverLU(),
            new ParametrosFDM(dx, dx, 0.0));

        var estado = fdm.Resolver();
        double uCentro = estado.ValorActual[nx / 2][nx / 2];

        Assert.True(uCentro > 0.05 && uCentro < 0.10,
            $"u(centro) = {uCentro:F4} fuera del rango analítico esperado [0.05, 0.10].");
    }

    [Fact]
    public void PoissonFDM_LU_vs_CG_MismaSolucion()
    {
        int nx = 17;
        double dx = 1.0 / (nx - 1);
        var malla = MallaBuilder.MallaFDM(nx);
        var ecuacion = ConstruirPoissonConFuenteUnitaria();

        var esquema = new EsquemaLaplaceFDM(malla, ecuacion, dx, dx);
        var fdmLU = new FDM(malla, ecuacion, esquema, new SolverLU(), new ParametrosFDM(dx, dx, 0.0));
        var fdmCG = new FDM(MallaBuilder.MallaFDM(nx), ecuacion,
            new EsquemaLaplaceFDM(MallaBuilder.MallaFDM(nx), ecuacion, dx, dx),
            new SolverGradienteConjugado(),
            new ParametrosFDM(dx, dx, 0.0),
            new final_para.Algebra.OpcionesSolver(MaxIter: 20_000, Tolerancia: 1e-12));

        var uLU = fdmLU.Resolver().ValorActual;
        var uCG = fdmCG.Resolver().ValorActual;

        double diff = 0.0;
        for (int i = 0; i < uLU.Length; i++)
            for (int j = 0; j < uLU[i].Length; j++)
                diff = Math.Max(diff, Math.Abs(uLU[i][j] - uCG[i][j]));

        Assert.True(diff < 1e-6,
            $"FDM-LU y FDM-CG difieren más de lo esperado: max diff = {diff:E3}.");
    }

    [Fact]
    public void PoissonFDM_ConvergenciaOrden2()
    {
        // -Δu = 1 sobre cuadrado unitario con Dirichlet homogéneo. La
        // solución analítica es una serie infinita; usamos como referencia
        // el valor en el centro u(1/2, 1/2) ≈ 0.073671 obtenido de la
        // serie truncada (suficiente con 6 términos para 5 decimales).
        const double uCentroExacto = 0.0736714;

        int[] tamanos = { 11, 21, 41 };
        var errores = new double[tamanos.Length];

        for (int k = 0; k < tamanos.Length; k++)
        {
            int nx = tamanos[k];
            double dx = 1.0 / (nx - 1);
            var malla = MallaBuilder.MallaFDM(nx);
            var ecuacion = ConstruirPoissonConFuenteUnitaria();
            var fdm = new FDM(
                malla, ecuacion,
                new EsquemaLaplaceFDM(malla, ecuacion, dx, dx),
                new SolverLU(),
                new ParametrosFDM(dx, dx, 0.0));
            var estado = fdm.Resolver();
            double uCentro = estado.ValorActual[nx / 2][nx / 2];
            errores[k] = Math.Abs(uCentro - uCentroExacto);
        }

        // Al duplicar n, h se mitad, error debe caer ≈ 4×. Tolerancia amplia.
        for (int k = 0; k + 1 < errores.Length; k++)
        {
            double ratio = errores[k] / Math.Max(errores[k + 1], 1e-15);
            Assert.True(ratio > 2.5 && ratio < 6.0,
                $"Razón de convergencia fuera de O(h²): " +
                $"e[{tamanos[k]}]={errores[k]:E3}, e[{tamanos[k + 1]}]={errores[k + 1]:E3}, ratio={ratio:F2}.");
        }
    }
}
