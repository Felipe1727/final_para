namespace final_para.Tests.Util;

// Utilidades para los tests de validación numérica.
public static class MallaBuilder
{
    // Genera una malla FEM cuadrada de lado×lado nodos sobre [0,1]×[0,1].
    public static double[][] CuadradoUnitario(int lado)
    {
        int n = lado * lado;
        var malla = new double[n][];
        double h = 1.0 / (lado - 1);
        for (int i = 0; i < lado; i++)
            for (int j = 0; j < lado; j++)
                malla[i * lado + j] = new[] { j * h, i * h };
        return malla;
    }

    // Genera una "malla FDM" 2D (nx × nx) sobre [0,1]×[0,1] con valores
    // iniciales en 0 (placeholder; los tests sólo usan el shape).
    public static double[][] MallaFDM(int nx)
    {
        var m = new double[nx][];
        for (int i = 0; i < nx; i++) m[i] = new double[nx];
        return m;
    }
}
