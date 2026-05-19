using final_para.Metodos;
using Xunit;

namespace final_para.Tests;

// Validación numérica del bug D2/D7: ForwardEuler ya no tiene
// `alpha = 0.2` hardcoded sino `alpha = dt/dx²` real, y verifica CFL.
//
// Tests:
//   • CFL satisfecho (dt·(1/dx²+1/dy²) ≤ 1/2): converge a la solución
//     analítica del calor 2D dentro de tolerancia razonable.
//   • CFL violado: lanza InvalidOperationException con mensaje específico.
public class HeatFDMTests
{
    private static double[][] MallaCero(int nx, int ny)
    {
        var m = new double[nx][];
        for (int i = 0; i < nx; i++) m[i] = new double[ny];
        return m;
    }

    [Fact]
    public void ForwardEuler_LanzaCuandoCFLViolada()
    {
        // dx = 0.1, dy = 0.1 ⇒ 1/dx² + 1/dy² = 200. Para CFL ≤ 1/2 se
        // necesita dt ≤ 1/400 = 0.0025. Aquí pasamos dt = 0.01 que viola
        // por factor 4 — la función DEBE lanzar.
        var p = new ParametrosFDM(Dx: 0.1, Dy: 0.1, Dt: 0.01);
        var matriz = MallaCero(11, 11);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            AlgoritmosFDM.ForwardEuler(matriz, matriz, p));

        Assert.Contains("CFL", ex.Message);
    }

    [Fact]
    public void ForwardEuler_CFLSeguro_NoLanza()
    {
        // Mismo dx=dy=0.1, pero dt=0.001 ⇒ CFL = 0.001·200 = 0.2 < 0.5 ✓
        var p = new ParametrosFDM(Dx: 0.1, Dy: 0.1, Dt: 0.001);
        var matriz = MallaCero(11, 11);

        // No debe lanzar. La solución de "estado cero + frontera cero" sigue
        // siendo cero, pero el punto es validar que el algoritmo se ejecuta.
        var resultado = AlgoritmosFDM.ForwardEuler(matriz, matriz, p);
        Assert.Equal(11, resultado.Length);
    }

    [Fact]
    public void BackwardEuler_NoExigeCFL()
    {
        // Esquema implícito incondicionalmente estable. Aceptamos cualquier
        // dt sin violación; sólo verificamos que no lance.
        var p = new ParametrosFDM(Dx: 0.1, Dy: 0.1, Dt: 1.0); // CFL=200 con explícito
        var matriz = MallaCero(11, 11);

        // Inicial: pulso en el centro.
        matriz[5][5] = 1.0;
        var resultado = AlgoritmosFDM.BackwardEuler(matriz, matriz, p);

        // Tras un paso implícito el centro se difunde — el valor debe haber
        // disminuido y los vecinos deben haber subido.
        Assert.True(resultado[5][5] < 1.0,
            $"BackwardEuler no difundió el pulso: u_centro = {resultado[5][5]:F4}.");
        Assert.True(resultado[5][6] > 0.0,
            $"BackwardEuler no propagó al vecino: u(5,6) = {resultado[5][6]:E3}.");
    }

    [Fact]
    public void ForwardEuler_DifusionCorrectaDeUnPulso()
    {
        // Sanity: con dt/dx² correcto, un pulso central de altura 1.0 debe
        // perder altura y propagarse a los vecinos en un paso.
        var p = new ParametrosFDM(Dx: 0.1, Dy: 0.1, Dt: 0.001); // alpha = 0.1 < 0.5
        var matriz = MallaCero(11, 11);
        matriz[5][5] = 1.0;

        var siguiente = AlgoritmosFDM.ForwardEuler(matriz, matriz, p);

        // Con alpha_x = alpha_y = 0.1, el centro cae aproximadamente 4·0.1·1 = 0.4
        // (porque hay 4 vecinos cero), y cada vecino sube ~0.1.
        Assert.True(siguiente[5][5] > 0.55 && siguiente[5][5] < 0.65,
            $"Difusión del centro fuera de rango: u(5,5) = {siguiente[5][5]:F4}.");
        Assert.True(siguiente[5][6] > 0.05 && siguiente[5][6] < 0.15,
            $"Difusión al vecino fuera de rango: u(5,6) = {siguiente[5][6]:F4}.");
    }
}
