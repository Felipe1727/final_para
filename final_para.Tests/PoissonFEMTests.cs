using final_para.Algebra.Solvers;
using final_para.Discretizacion.FEM;
using final_para.Ecuaciones;
using final_para.Metodos;
using final_para.Metricas;
using final_para.Tests.Util;
using Xunit;

namespace final_para.Tests;

// Validación numérica FEM con método de soluciones manufacturadas (MMS).
//
// PDE:  -Δu = 2π²·sin(πx)·sin(πy)  en [0,1]²
// BC:   u = 0  en la frontera (homogéneo)
// Exacta:  u(x,y) = sin(πx)·sin(πy)
//
// Estos tests son la prueba de que los bugs F1 (vector de cargas
// inconsistente) y F2 (Dirichlet sólo en K) están corregidos: con la
// implementación rota la solución colapsaba a u≡0 (cero error contra
// cero solución, pero también cero residuo significativo). Con la
// corrección, el error L² disminuye como O(h²) al refinar la malla.
public class PoissonFEMTests
{
    private static Ecuacion ConstruirPoissonMMS()
    {
        // -u_xx - u_yy - 2π²·sin(πx)·sin(πy) = 0
        return new EcuacionBuilder()
            .ConTerminos(
                new Termino("u_xx", EsPositivo: false),
                new Termino("u_yy", EsPositivo: false),
                new Termino("2*pi^2*sin(pi*x)*sin(pi*y)", EsPositivo: false))
            .ConVariablesIndependientes("x", "y")
            .ConGeometria(Geometria.Eliptica)
            .ConOrden(2)
            .Build();
    }

    private static double UExacta(double x, double y) => Math.Sin(Math.PI * x) * Math.Sin(Math.PI * y);

    [Theory]
    [InlineData(9)]
    [InlineData(17)]
    [InlineData(33)]
    public void PoissonP1_LU_SolucionNoEsTrivial(int lado)
    {
        var malla = MallaBuilder.CuadradoUnitario(lado);
        var ecuacion = ConstruirPoissonMMS();

        var fem = new FEM(
            malla, ecuacion,
            TipoElemento.Triangular,
            FuncionesBase.TriangularLineales,
            new DiscretizadorFEM(malla, ecuacion, TipoElemento.Triangular),
            new SolverLU());

        var estado = fem.Resolver();

        // La solución no debe ser trivial (todo cero) — eso era el bug.
        double maxAbs = 0.0;
        foreach (var v in estado.ValorActual) maxAbs = Math.Max(maxAbs, Math.Abs(v));
        Assert.True(maxAbs > 0.1,
            $"Solución colapsada a trivial: máx |u| = {maxAbs:E3} (esperado > 0.1).");

        // El máximo del MMS en [0,1]² es 1 (en (1/2, 1/2)). Esperamos que
        // el máximo numérico sea cercano a 1; con malla gruesa hay error.
        Assert.True(maxAbs > 0.5 && maxAbs < 1.5,
            $"Máximo de la solución fuera de rango razonable: {maxAbs:F3}.");
    }

    [Fact]
    public void PoissonP1_LU_ConvergenciaOrden2()
    {
        // Verifica que el error L² decae con orden ≈ h² al refinar la malla.
        // Para P1 sobre cuadrado unitario MMS el orden teórico es exactamente 2.
        int[] lados = { 9, 17, 33 };
        var errores = new double[lados.Length];

        for (int k = 0; k < lados.Length; k++)
        {
            int lado = lados[k];
            var malla = MallaBuilder.CuadradoUnitario(lado);
            var ecuacion = ConstruirPoissonMMS();
            var fem = new FEM(
                malla, ecuacion,
                TipoElemento.Triangular,
                FuncionesBase.TriangularLineales,
                new DiscretizadorFEM(malla, ecuacion, TipoElemento.Triangular),
                new SolverLU());

            var estado = fem.Resolver();
            errores[k] = ErrorL2.FEM(estado.ValorActual, malla, UExacta);
        }

        // Al duplicar n (h pasa a h/2) el error debe caer ≈ 4×. Tolerancia
        // amplia (×2.5 a ×5) porque el centroide es una cuadratura mínima.
        for (int k = 0; k + 1 < errores.Length; k++)
        {
            double ratio = errores[k] / errores[k + 1];
            Assert.True(ratio > 2.5 && ratio < 6.0,
                $"Razón de convergencia fuera de O(h²): error[{lados[k]}]={errores[k]:E3}, " +
                $"error[{lados[k + 1]}]={errores[k + 1]:E3}, ratio={ratio:F2}.");
        }
    }

    [Fact]
    public void PoissonP1_LU_vs_CG_MismaSolucion()
    {
        // FEM con LU directo y con CG iterativo deben dar prácticamente la
        // misma solución (diferencia ≪ tolerancia de CG). Esto valida que
        // la matriz K es simétrica definida positiva — propiedad que se
        // pierde si Dirichlet no limpia la columna (bug F2 pre-Fase 1).
        int lado = 17;
        var malla = MallaBuilder.CuadradoUnitario(lado);
        var ecuacion = ConstruirPoissonMMS();

        var femLU = new FEM(malla, ecuacion, TipoElemento.Triangular,
            FuncionesBase.TriangularLineales,
            new DiscretizadorFEM(malla, ecuacion, TipoElemento.Triangular),
            new SolverLU());
        var femCG = new FEM(malla, ecuacion, TipoElemento.Triangular,
            FuncionesBase.TriangularLineales,
            new DiscretizadorFEM(malla, ecuacion, TipoElemento.Triangular),
            new SolverGradienteConjugado(),
            new final_para.Algebra.OpcionesSolver(MaxIter: 10_000, Tolerancia: 1e-12));

        var uLU = femLU.Resolver().ValorActual;
        var uCG = femCG.Resolver().ValorActual;

        double diff = 0.0;
        for (int i = 0; i < uLU.Length; i++) diff = Math.Max(diff, Math.Abs(uLU[i] - uCG[i]));
        Assert.True(diff < 1e-6,
            $"LU y CG difieren más de lo esperado: max|uLU-uCG| = {diff:E3}.");
    }
}
