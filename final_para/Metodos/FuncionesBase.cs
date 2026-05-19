namespace final_para.Metodos;

public static class FuncionesBase
{
    public static double LinealTriangulo1(double[] coordenadas)
    {
        double xi = coordenadas[0];
        double eta = coordenadas.Length > 1 ? coordenadas[1] : 0.0;
        return 1.0 - xi - eta;
    }

    public static double LinealTriangulo2(double[] coordenadas)
    {
        return coordenadas[0];
    }

    public static double LinealTriangulo3(double[] coordenadas)
    {
        return coordenadas.Length > 1 ? coordenadas[1] : 0.0;
    }

    public static double BilinealCuad1(double[] coordenadas)
    {
        double xi = coordenadas[0];
        double eta = coordenadas.Length > 1 ? coordenadas[1] : 0.0;
        return 0.25 * (1 - xi) * (1 - eta);
    }

    public static double BilinealCuad2(double[] coordenadas)
    {
        double xi = coordenadas[0];
        double eta = coordenadas.Length > 1 ? coordenadas[1] : 0.0;
        return 0.25 * (1 + xi) * (1 - eta);
    }

    public static double BilinealCuad3(double[] coordenadas)
    {
        double xi = coordenadas[0];
        double eta = coordenadas.Length > 1 ? coordenadas[1] : 0.0;
        return 0.25 * (1 + xi) * (1 + eta);
    }

    public static double BilinealCuad4(double[] coordenadas)
    {
        double xi = coordenadas[0];
        double eta = coordenadas.Length > 1 ? coordenadas[1] : 0.0;
        return 0.25 * (1 - xi) * (1 + eta);
    }

    public static double CuadraticaTriangulo1(double[] coordenadas)
    {
        double xi = coordenadas[0];
        double eta = coordenadas.Length > 1 ? coordenadas[1] : 0.0;
        double L1 = 1.0 - xi - eta;
        return L1 * (2 * L1 - 1);
    }

    public static double CuadraticaTriangulo2(double[] coordenadas)
    {
        double xi = coordenadas[0];
        return xi * (2 * xi - 1);
    }

    public static double CuadraticaTriangulo3(double[] coordenadas)
    {
        double eta = coordenadas.Length > 1 ? coordenadas[1] : 0.0;
        return eta * (2 * eta - 1);
    }

    public static FuncionBase[] TriangularLineales =>
        [LinealTriangulo1, LinealTriangulo2, LinealTriangulo3];

    public static FuncionBase[] CuadrilateralBilineales =>
        [BilinealCuad1, BilinealCuad2, BilinealCuad3, BilinealCuad4];

    public static FuncionBase[] TriangularCuadraticas =>
        [CuadraticaTriangulo1, CuadraticaTriangulo2, CuadraticaTriangulo3];
}
