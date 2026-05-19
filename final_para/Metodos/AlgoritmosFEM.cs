namespace final_para.Metodos;

public static class AlgoritmosFEM
{
    private const int MaxIteracionesCG = 1000;
    private const double ToleranciaCG = 1e-10;

    public static double[] Galerkin(double[][] matriz, double[][] valoresConocidos)
    {
        var f = valoresConocidos[0];
        return EliminacionGaussiana(matriz, f);
    }

    public static double[] PetrovGalerkin(double[][] matriz, double[][] valoresConocidos)
    {
        int n = matriz.Length;
        if (n == 0) return [];

        double asimetria = 0.0;
        double escala = 0.0;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                asimetria += Math.Abs(matriz[i][j] - matriz[j][i]);
                escala += Math.Abs(matriz[i][j]);
            }
        }
        double tau = escala > 0 ? 0.1 * asimetria / escala : 0.0;

        if (tau < 1e-12) return Galerkin(matriz, valoresConocidos);

        var estabilizada = new double[n][];
        for (int i = 0; i < n; i++)
        {
            estabilizada[i] = new double[n];
            for (int j = 0; j < n; j++)
                estabilizada[i][j] = matriz[i][j];

            estabilizada[i][i] += tau;
            if (i > 0) estabilizada[i][i - 1] -= tau / 2.0;
            if (i < n - 1) estabilizada[i][i + 1] -= tau / 2.0;
        }

        return Galerkin(estabilizada, valoresConocidos);
    }

    public static double[] GradienteConjugado(double[][] matriz, double[][] valoresConocidos)
    {
        int n = matriz.Length;
        var b = valoresConocidos[0];
        var x = new double[n];
        var r = new double[n];
        var p = new double[n];

        for (int i = 0; i < n; i++) r[i] = b[i];
        for (int i = 0; i < n; i++) p[i] = r[i];

        double rsAntiguo = ProductoEscalar(r, r);

        for (int iter = 0; iter < MaxIteracionesCG; iter++)
        {
            var Ap = MultiplicarMatrizVector(matriz, p);
            double alpha = rsAntiguo / ProductoEscalar(p, Ap);

            for (int i = 0; i < n; i++)
            {
                x[i] += alpha * p[i];
                r[i] -= alpha * Ap[i];
            }

            double rsNuevo = ProductoEscalar(r, r);
            if (Math.Sqrt(rsNuevo) < ToleranciaCG) break;

            double beta = rsNuevo / rsAntiguo;
            for (int i = 0; i < n; i++) p[i] = r[i] + beta * p[i];

            rsAntiguo = rsNuevo;
        }

        return x;
    }

    private static double[] EliminacionGaussiana(double[][] A, double[] b)
    {
        int n = A.Length;
        var M = new double[n][];
        for (int i = 0; i < n; i++)
        {
            M[i] = new double[n + 1];
            Array.Copy(A[i], M[i], n);
            M[i][n] = b[i];
        }

        for (int k = 0; k < n; k++)
        {
            int max = k;
            for (int i = k + 1; i < n; i++)
                if (Math.Abs(M[i][k]) > Math.Abs(M[max][k])) max = i;

            (M[k], M[max]) = (M[max], M[k]);

            if (Math.Abs(M[k][k]) < 1e-14) continue;

            for (int i = k + 1; i < n; i++)
            {
                double factor = M[i][k] / M[k][k];
                for (int j = k; j <= n; j++)
                    M[i][j] -= factor * M[k][j];
            }
        }

        var x = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            double suma = M[i][n];
            for (int j = i + 1; j < n; j++)
                suma -= M[i][j] * x[j];
            x[i] = Math.Abs(M[i][i]) < 1e-14 ? 0.0 : suma / M[i][i];
        }

        return x;
    }

    private static double ProductoEscalar(double[] a, double[] b)
    {
        double s = 0.0;
        for (int i = 0; i < a.Length; i++) s += a[i] * b[i];
        return s;
    }

    private static double[] MultiplicarMatrizVector(double[][] A, double[] x)
    {
        int n = A.Length;
        var y = new double[n];
        for (int i = 0; i < n; i++)
        {
            double s = 0.0;
            for (int j = 0; j < x.Length; j++) s += A[i][j] * x[j];
            y[i] = s;
        }
        return y;
    }
}
