namespace final_para.Metodos;

public static class AlgoritmosFDM
{
    public static double[][] JacobiLaplace(double[][] matriz, double[][] valoresConocidos)
    {
        int filas = matriz.Length;
        int cols = matriz[0].Length;
        var resultado = new double[filas][];

        for (int i = 0; i < filas; i++)
        {
            resultado[i] = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                if (EsFrontera(i, j, filas, cols))
                {
                    resultado[i][j] = matriz[i][j];
                }
                else
                {
                    resultado[i][j] = 0.25 * (
                        matriz[i + 1][j] +
                        matriz[i - 1][j] +
                        matriz[i][j + 1] +
                        matriz[i][j - 1]);
                }
            }
        }

        return resultado;
    }

    public static double[][] GaussSeidel(double[][] matriz, double[][] valoresConocidos)
    {
        int filas = matriz.Length;
        int cols = matriz[0].Length;
        var resultado = ClonarMatriz(matriz);

        for (int i = 1; i < filas - 1; i++)
        {
            for (int j = 1; j < cols - 1; j++)
            {
                resultado[i][j] = 0.25 * (
                    resultado[i + 1][j] +
                    resultado[i - 1][j] +
                    resultado[i][j + 1] +
                    resultado[i][j - 1]);
            }
        }

        return resultado;
    }

    public static double[][] ForwardEuler(double[][] matriz, double[][] valoresConocidos)
    {
        int filas = matriz.Length;
        int cols = matriz[0].Length;
        var resultado = new double[filas][];

        double alpha = 0.2;

        for (int i = 0; i < filas; i++)
        {
            resultado[i] = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                if (EsFrontera(i, j, filas, cols))
                {
                    resultado[i][j] = matriz[i][j];
                }
                else
                {
                    double laplaciano =
                        matriz[i + 1][j] + matriz[i - 1][j] +
                        matriz[i][j + 1] + matriz[i][j - 1] -
                        4.0 * matriz[i][j];
                    resultado[i][j] = matriz[i][j] + alpha * laplaciano;
                }
            }
        }

        return resultado;
    }

    public static double[][] BackwardEuler(double[][] matriz, double[][] valoresConocidos)
    {
        int filas = matriz.Length;
        if (filas == 0) return matriz;
        int cols = matriz[0].Length;

        const double r = 1.0;
        const double denominador = 1.0 + 4.0 * r;
        const int barridos = 20;

        var nuevo = ClonarMatriz(matriz);

        for (int s = 0; s < barridos; s++)
        {
            for (int i = 1; i < filas - 1; i++)
            {
                for (int j = 1; j < cols - 1; j++)
                {
                    nuevo[i][j] = (matriz[i][j] + r * (
                        nuevo[i + 1][j] + nuevo[i - 1][j] +
                        nuevo[i][j + 1] + nuevo[i][j - 1])) / denominador;
                }
            }
        }

        return nuevo;
    }

    public static double[][] CrankNicolson(double[][] matriz, double[][] valoresConocidos)
    {
        var explicito = ForwardEuler(matriz, valoresConocidos);
        var implicito = GaussSeidel(matriz, valoresConocidos);

        int filas = matriz.Length;
        int cols = matriz[0].Length;
        var resultado = new double[filas][];

        for (int i = 0; i < filas; i++)
        {
            resultado[i] = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                resultado[i][j] = 0.5 * (explicito[i][j] + implicito[i][j]);
            }
        }

        return resultado;
    }

    public static double[][] CentralDifference2(double[][] matriz, double[][] valoresConocidos)
    {
        int filas = matriz.Length;
        if (filas == 0) return matriz;
        int cols = matriz[0].Length;
        const double dt = 0.1;
        var resultado = new double[filas][];

        for (int i = 0; i < filas; i++)
        {
            resultado[i] = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                if (EsFrontera(i, j, filas, cols))
                {
                    resultado[i][j] = matriz[i][j];
                }
                else
                {
                    double uxx = matriz[i + 1][j] - 2.0 * matriz[i][j] + matriz[i - 1][j];
                    double uyy = matriz[i][j + 1] - 2.0 * matriz[i][j] + matriz[i][j - 1];
                    resultado[i][j] = matriz[i][j] + dt * (uxx + uyy);
                }
            }
        }

        return resultado;
    }

    public static double[][] CentralDifference4(double[][] matriz, double[][] valoresConocidos)
    {
        int filas = matriz.Length;
        int cols = matriz[0].Length;
        var resultado = new double[filas][];

        for (int i = 0; i < filas; i++)
        {
            resultado[i] = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                if (i < 2 || i >= filas - 2 || j < 2 || j >= cols - 2)
                {
                    resultado[i][j] = matriz[i][j];
                }
                else
                {
                    double dxx = (-matriz[i + 2][j] + 16 * matriz[i + 1][j] - 30 * matriz[i][j]
                                + 16 * matriz[i - 1][j] - matriz[i - 2][j]) / 12.0;
                    double dyy = (-matriz[i][j + 2] + 16 * matriz[i][j + 1] - 30 * matriz[i][j]
                                + 16 * matriz[i][j - 1] - matriz[i][j - 2]) / 12.0;
                    resultado[i][j] = matriz[i][j] + 0.1 * (dxx + dyy);
                }
            }
        }

        return resultado;
    }

    private static bool EsFrontera(int i, int j, int filas, int cols) =>
        i == 0 || i == filas - 1 || j == 0 || j == cols - 1;

    private static double[][] ClonarMatriz(double[][] origen)
    {
        var copia = new double[origen.Length][];
        for (int i = 0; i < origen.Length; i++)
        {
            copia[i] = new double[origen[i].Length];
            Array.Copy(origen[i], copia[i], origen[i].Length);
        }
        return copia;
    }
}
