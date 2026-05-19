using final_para.Ecuaciones;
using final_para.Estado;
using final_para.Interfaces;
using final_para.Servicios;

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

        // Detectar nodos frontera y calcular sus valores Dirichlet desde la ecuación.
        bool[] esNodoFrontera = CalcularNodosFrontera(n);
        double[] valoresFrontera = CalcularValoresFronteraPorNodo(n, esNodoFrontera);

        MatrizRigidez = EnsamblarRigidez(n);
        var cargas = ConstruirVectorCargas(n, esNodoFrontera);
        AplicarDirichletLifting(MatrizRigidez, cargas, valoresFrontera, esNodoFrontera);
        var semilla = ConstruirSemillaConFrontera(n, valoresFrontera, esNodoFrontera);

        var solucion = Algoritmo(MatrizRigidez, new[] { cargas, semilla });
        ImponerValoresFrontera(solucion, valoresFrontera, esNodoFrontera);
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
        int n = Malla.Length;
        bool[] esNodoFrontera = CalcularNodosFrontera(n);
        double[] valoresFrontera = CalcularValoresFronteraPorNodo(n, esNodoFrontera);
        var k = EnsamblarRigidez(n);
        var cargas = ConstruirVectorCargas(n, esNodoFrontera);
        AplicarDirichletLifting(k, cargas, valoresFrontera, esNodoFrontera);
        var u = Malla.Select(fila => fila.Length > 0 ? fila[0] : 0.0).ToArray();
        return CalcularResiduo(k, u, cargas);
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

    private static void AplicarDirichletLifting(double[][] K, double[] f, double[] valoresFrontera, bool[] esNodoFrontera)
    {
        int n = K.Length;
        for (int nodo = 0; nodo < n; nodo++)
        {
            if (!esNodoFrontera[nodo]) continue;
            double g = valoresFrontera[nodo];

            for (int i = 0; i < n; i++)
            {
                if (i != nodo) f[i] -= K[i][nodo] * g;
                K[i][nodo] = (i == nodo) ? K[i][nodo] : 0.0;
            }

            for (int j = 0; j < n; j++) K[nodo][j] = 0.0;
            K[nodo][nodo] = 1.0;
            f[nodo] = g;
        }
    }

    private static double[] ConstruirSemillaConFrontera(int n, double[] valoresFrontera, bool[] esNodoFrontera)
    {
        var semilla = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (esNodoFrontera[i]) semilla[i] = valoresFrontera[i];
        }
        return semilla;
    }

    private static void ImponerValoresFrontera(double[] solucion, double[] valoresFrontera, bool[] esNodoFrontera)
    {
        for (int i = 0; i < solucion.Length; i++)
        {
            if (esNodoFrontera[i]) solucion[i] = valoresFrontera[i];
        }
    }

    /// <summary>
    /// Calcula la máscara de nodos frontera. Para mallas cuadradas 2D, son los
    /// nodos del borde del cuadrado de lado √n. Para malla lineal 1D, son los
    /// extremos 0 y n-1.
    /// </summary>
    private bool[] CalcularNodosFrontera(int n)
    {
        var esFrontera = new bool[n];
        if (EsMallaCuadrada(n))
        {
            int lado = (int)Math.Sqrt(n);
            for (int i = 0; i < lado; i++)
            {
                for (int j = 0; j < lado; j++)
                {
                    if (i == 0 || i == lado - 1 || j == 0 || j == lado - 1)
                        esFrontera[i * lado + j] = true;
                }
            }
        }
        else if (n > 0)
        {
            esFrontera[0] = true;
            if (n > 1) esFrontera[n - 1] = true;
        }
        return esFrontera;
    }

    /// <summary>
    /// Para cada nodo frontera, busca en <c>Ecuacion.CondicionesFrontera</c>
    /// una condición compatible con su posición física. Soporta descriptores
    /// simbólicos (xMin/xMax, tMin/tMax, etc.) y también valores numéricos
    /// (p.ej. u(0,t)=...). Si no hay coincidencia, deja 0.
    /// </summary>
    private double[] CalcularValoresFronteraPorNodo(int n, bool[] esNodoFrontera)
    {
        var valores = new double[n];
        if (Ecuacion.CondicionesFrontera == null || Ecuacion.CondicionesFrontera.Length == 0)
            return valores;

        // Determinar nombres de los ejes (var1, var2) según las variables independientes.
        // Por convención, var1 = primera variable espacial, var2 = la segunda (t, y, z).
        string var1 = "x";
        string var2 = "y";
        var vars = Ecuacion.VariablesIndependientes;
        if (vars != null && vars.Length >= 1) var1 = vars[0];
        if (vars != null && vars.Length >= 2) var2 = vars[1];

        // Calcular rangos físicos de la malla para identificar fronteras.
        double minVar1 = double.PositiveInfinity, maxVar1 = double.NegativeInfinity;
        double minVar2 = double.PositiveInfinity, maxVar2 = double.NegativeInfinity;
        bool tieneVar2 = Malla.Length > 0 && Malla[0].Length >= 2;
        for (int i = 0; i < n; i++)
        {
            double v1 = Malla[i][0];
            if (v1 < minVar1) minVar1 = v1;
            if (v1 > maxVar1) maxVar1 = v1;
            if (tieneVar2)
            {
                double v2 = Malla[i][1];
                if (v2 < minVar2) minVar2 = v2;
                if (v2 > maxVar2) maxVar2 = v2;
            }
        }

        const double tol = 1e-9;

        string v1Lower = var1.ToLowerInvariant();
        string v2Lower = var2.ToLowerInvariant();

        var cfPre = new List<(string[] args, string rhs)>(Ecuacion.CondicionesFrontera.Length);
        foreach (var cf in Ecuacion.CondicionesFrontera)
        {
            if (string.IsNullOrWhiteSpace(cf)) continue;
            var (clave, rhs) = SepararCondicion(cf);
            if (string.IsNullOrEmpty(clave)) continue;
            cfPre.Add((ExtraerArgumentosCondicion(clave), rhs));
        }

        var ctx = new Dictionary<string, double>(2);

        for (int i = 0; i < n; i++)
        {
            if (!esNodoFrontera[i]) continue;

            double x = Malla[i][0];
            double y = tieneVar2 ? Malla[i][1] : 0.0;

            bool enVar1Min = Math.Abs(x - minVar1) < tol;
            bool enVar1Max = Math.Abs(x - maxVar1) < tol;
            bool enVar2Min = tieneVar2 && Math.Abs(y - minVar2) < tol;
            bool enVar2Max = tieneVar2 && Math.Abs(y - maxVar2) < tol;

            string? expresionElegida = null;
            foreach (var (args, rhs) in cfPre)
            {
                if (CoincideNodoConCondicion(
                    args, x, y, enVar1Min, enVar1Max, enVar2Min, enVar2Max,
                    v1Lower, v2Lower, minVar1, maxVar1, minVar2, maxVar2, tol))
                {
                    expresionElegida = rhs;
                    break;
                }
            }

            if (expresionElegida == null) continue;

            ctx[var1] = x;
            ctx[var2] = y;
            try
            {
                valores[i] = EvaluadorExpresion.Evaluar(expresionElegida, ctx);
            }
            catch
            {
                valores[i] = 0.0;
            }
        }

        return valores;
    }

    /// <summary>
    /// Separa una condición del tipo "u(xMin,*)=expr" en (lado_izq, lado_der).
    /// Sin "=", asume que la cadena es el descriptor y el rhs es "0".
    /// </summary>
    private static (string clave, string rhs) SepararCondicion(string condicion)
    {
        int idx = condicion.IndexOf('=');
        if (idx < 0) return (condicion.Trim(), "0");
        return (condicion.Substring(0, idx).Trim(), condicion.Substring(idx + 1).Trim());
    }

    private static string[] ExtraerArgumentosCondicion(string clave)
    {
        int ini = clave.IndexOf('(');
        int fin = clave.LastIndexOf(')');
        if (ini < 0 || fin <= ini) return [];
        return clave[(ini + 1)..fin]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool CoincideNodoConCondicion(
        string[] args,
        double x,
        double y,
        bool enVar1Min,
        bool enVar1Max,
        bool enVar2Min,
        bool enVar2Max,
        string var1,
        string var2,
        double minVar1,
        double maxVar1,
        double minVar2,
        double maxVar2,
        double tol)
    {
        if (args.Length == 0) return false;

        bool coincideVar1 = CoincideArgumento(
            args[0], x, enVar1Min, enVar1Max, var1, minVar1, maxVar1, tol);
        if (!coincideVar1) return false;

        if (args.Length < 2) return true;

        return CoincideArgumento(
            args[1], y, enVar2Min, enVar2Max, var2, minVar2, maxVar2, tol);
    }

    private static bool CoincideArgumento(
        string arg,
        double valorNodo,
        bool enMin,
        bool enMax,
        string nombreVar,
        double minVar,
        double maxVar,
        double tol)
    {
        string a = arg.Trim().ToLowerInvariant();
        if (a.Length == 0 || a == "*" || a == nombreVar) return true;

        if (a.Contains(nombreVar + "min")) return enMin;
        if (a.Contains(nombreVar + "max")) return enMax;

        if (double.TryParse(
            a,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out double valorCondicion))
        {
            if (Math.Abs(valorCondicion - minVar) < tol) return enMin;
            if (Math.Abs(valorCondicion - maxVar) < tol) return enMax;
            return Math.Abs(valorNodo - valorCondicion) < tol;
        }

        return false;
    }

    /// <summary>
    /// Ensambla el vector de cargas base para nodos interiores con términos
    /// forzantes constantes. El lifting Dirichlet se aplica después sobre K y f.
    /// </summary>
    private double[] ConstruirVectorCargas(int n, bool[] esNodoFrontera)
    {
        var f = new double[n];
        foreach (var termino in Ecuacion.TerminosForzantes)
        {
            if (double.TryParse(termino.Expresion, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double valor))
            {
                double signo = termino.EsPositivo ? 1.0 : -1.0;
                for (int i = 0; i < n; i++)
                {
                    if (!esNodoFrontera[i]) f[i] += signo * valor;
                }
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
