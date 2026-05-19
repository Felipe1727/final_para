using final_para.Ecuaciones;
using final_para.Estado;
using final_para.Interfaces;

namespace final_para.Metodos;

public enum TipoElemento { Triangular, Cuadrilateral }

public class FEM : MetodoNumerico, IResolver<EstadoSolucionFEM>
{
    public TipoElemento Elemento { get; }
    public FuncionBase[] FuncionesBase { get; }
    public double[][] MatrizRigidez { get; private set; }
    public AlgoritmoFEM Algoritmo { get; }

    public FEM(
        double[][] malla,
        Ecuacion ecuacion,
        TipoElemento elemento,
        FuncionBase[] funcionesBase,
        AlgoritmoFEM algoritmo)
        : base(malla, ecuacion)
    {
        Elemento = elemento;
        FuncionesBase = funcionesBase;
        MatrizRigidez = [];
        Algoritmo = algoritmo;
    }

    public virtual EstadoSolucionFEM Resolver()
    {
        TiempoInicio = DateTime.Now;
        NumIteraciones = 0;

        int n = Malla.Length;
        MatrizRigidez = EnsamblarRigidez(n);
        var cargas = ConstruirVectorCargas(n);

        var solucion = Algoritmo(MatrizRigidez, new[] { cargas });
        NumIteraciones = 1;

        var residuo = CalcularResiduo(MatrizRigidez, solucion, cargas);
        TiempoFin = DateTime.Now;

        return new EstadoSolucionFEM(
            residuo,
            solucion,
            tamanoMalla: n,
            numIteraciones: NumIteraciones,
            tiempoSegundos: (TiempoFin!.Value - TiempoInicio).TotalSeconds,
            metodoNombre: $"FEM-{Elemento}");
    }

    public override double CalcularCostoComputacional()
    {
        var fin = TiempoFin ?? DateTime.Now;
        return (fin - TiempoInicio).TotalSeconds;
    }

    public override double CalcularError()
    {
        if (MatrizRigidez.Length == 0) return 0.0;
        var cargas = ConstruirVectorCargas(Malla.Length);
        var u = Malla.Select(fila => fila.Length > 0 ? fila[0] : 0.0).ToArray();
        return CalcularResiduo(MatrizRigidez, u, cargas);
    }

    private double[][] EnsamblarRigidez(int n)
    {
        return (Elemento, FuncionesBase.Length) switch
        {
            (TipoElemento.Triangular, 3) when EsMallaCuadrada(n) => EnsamblarTriangularP1(n),
            (TipoElemento.Cuadrilateral, 4) when EsMallaCuadrada(n) => EnsamblarCuadrilateralQ1(n),
            _ => EnsamblarLineal1D(n),
        };
    }

    private bool EsMallaCuadrada(int n)
    {
        int lado = (int)Math.Sqrt(n);
        return lado >= 2 && lado * lado == n && Malla[0].Length >= 2;
    }

    private double[][] EnsamblarLineal1D(int n)
    {
        var K = new double[n][];
        for (int i = 0; i < n; i++) K[i] = new double[n];

        double h = (n > 1 && Malla[0].Length > 0 && Malla[n - 1].Length > 0)
            ? (Malla[n - 1][0] - Malla[0][0]) / (n - 1)
            : 1.0;
        if (Math.Abs(h) < 1e-14) h = 1.0;
        double k = 1.0 / h;

        for (int i = 1; i < n - 1; i++)
        {
            K[i][i] = 2.0 * k;
            K[i][i - 1] = -k;
            K[i][i + 1] = -k;
        }
        K[0][0] = 1.0;
        if (n > 1) K[n - 1][n - 1] = 1.0;
        return K;
    }

    private double[][] EnsamblarTriangularP1(int n)
    {
        int lado = (int)Math.Sqrt(n);
        var K = new double[n][];
        for (int i = 0; i < n; i++) K[i] = new double[n];

        for (int i = 0; i < lado - 1; i++)
        {
            for (int j = 0; j < lado - 1; j++)
            {
                int n00 = i * lado + j;
                int n10 = (i + 1) * lado + j;
                int n01 = i * lado + (j + 1);
                int n11 = (i + 1) * lado + (j + 1);

                EnsamblarElementoTriangular(K, n00, n10, n01);
                EnsamblarElementoTriangular(K, n10, n11, n01);
            }
        }

        ImponerDirichletFrontera(K, lado);
        return K;
    }

    private void EnsamblarElementoTriangular(double[][] K, int n1, int n2, int n3)
    {
        double x1 = Malla[n1][0], y1 = Malla[n1][1];
        double x2 = Malla[n2][0], y2 = Malla[n2][1];
        double x3 = Malla[n3][0], y3 = Malla[n3][1];

        double area = 0.5 * Math.Abs((x2 - x1) * (y3 - y1) - (x3 - x1) * (y2 - y1));
        if (area < 1e-14) return;

        double[] b = { y2 - y3, y3 - y1, y1 - y2 };
        double[] c = { x3 - x2, x1 - x3, x2 - x1 };
        int[] nodos = { n1, n2, n3 };

        for (int p = 0; p < 3; p++)
        {
            for (int q = 0; q < 3; q++)
            {
                double ke = (b[p] * b[q] + c[p] * c[q]) / (4.0 * area);
                K[nodos[p]][nodos[q]] += ke;
            }
        }
    }

    private double[][] EnsamblarCuadrilateralQ1(int n)
    {
        int lado = (int)Math.Sqrt(n);
        var K = new double[n][];
        for (int i = 0; i < n; i++) K[i] = new double[n];

        double inv3 = 1.0 / Math.Sqrt(3.0);
        double[][] puntosGauss =
        {
            new[] { -inv3, -inv3 },
            new[] {  inv3, -inv3 },
            new[] {  inv3,  inv3 },
            new[] { -inv3,  inv3 },
        };

        for (int i = 0; i < lado - 1; i++)
        {
            for (int j = 0; j < lado - 1; j++)
            {
                int[] nodos =
                {
                    i * lado + j,
                    (i + 1) * lado + j,
                    (i + 1) * lado + (j + 1),
                    i * lado + (j + 1),
                };
                EnsamblarElementoCuadrilateral(K, nodos, puntosGauss);
            }
        }

        ImponerDirichletFrontera(K, lado);
        return K;
    }

    private void EnsamblarElementoCuadrilateral(double[][] K, int[] nodos, double[][] puntosGauss)
    {
        var ke = new double[4][];
        for (int i = 0; i < 4; i++) ke[i] = new double[4];

        foreach (var pg in puntosGauss)
        {
            double xi = pg[0], eta = pg[1];
            double[] dphi_dxi  = { -0.25 * (1 - eta),  0.25 * (1 - eta), 0.25 * (1 + eta), -0.25 * (1 + eta) };
            double[] dphi_deta = { -0.25 * (1 - xi),  -0.25 * (1 + xi),  0.25 * (1 + xi),   0.25 * (1 - xi)  };

            double j11 = 0, j12 = 0, j21 = 0, j22 = 0;
            for (int k = 0; k < 4; k++)
            {
                double xk = Malla[nodos[k]][0];
                double yk = Malla[nodos[k]][1];
                j11 += xk * dphi_dxi[k];
                j12 += xk * dphi_deta[k];
                j21 += yk * dphi_dxi[k];
                j22 += yk * dphi_deta[k];
            }
            double detJ = j11 * j22 - j12 * j21;
            if (Math.Abs(detJ) < 1e-14) continue;

            double[] dphi_dx = new double[4];
            double[] dphi_dy = new double[4];
            for (int k = 0; k < 4; k++)
            {
                dphi_dx[k] = ( j22 * dphi_dxi[k] - j12 * dphi_deta[k]) / detJ;
                dphi_dy[k] = (-j21 * dphi_dxi[k] + j11 * dphi_deta[k]) / detJ;
            }

            double absDet = Math.Abs(detJ);
            for (int p = 0; p < 4; p++)
                for (int q = 0; q < 4; q++)
                    ke[p][q] += absDet * (dphi_dx[p] * dphi_dx[q] + dphi_dy[p] * dphi_dy[q]);
        }

        for (int p = 0; p < 4; p++)
            for (int q = 0; q < 4; q++)
                K[nodos[p]][nodos[q]] += ke[p][q];
    }

    private static void ImponerDirichletFrontera(double[][] K, int lado)
    {
        for (int i = 0; i < lado; i++)
        {
            for (int j = 0; j < lado; j++)
            {
                if (i == 0 || i == lado - 1 || j == 0 || j == lado - 1)
                {
                    int n = i * lado + j;
                    for (int k = 0; k < K.Length; k++) K[n][k] = 0.0;
                    K[n][n] = 1.0;
                }
            }
        }
    }

    private double[] ConstruirVectorCargas(int n)
    {
        var f = new double[n];
        foreach (var termino in Ecuacion.TerminosForzantes)
        {
            if (double.TryParse(termino.Expresion, out double valor))
            {
                double signo = termino.EsPositivo ? 1.0 : -1.0;
                for (int i = 0; i < n; i++) f[i] += signo * valor;
            }
        }
        return f;
    }

    private static double CalcularResiduo(double[][] K, double[] u, double[] f)
    {
        double suma = 0.0;
        for (int i = 0; i < u.Length; i++)
        {
            double Ku = 0.0;
            for (int j = 0; j < u.Length; j++)
                Ku += K[i][j] * u[j];
            double r = Ku - f[i];
            suma += r * r;
        }
        return Math.Sqrt(suma);
    }
}
