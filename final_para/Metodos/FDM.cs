using System.Text.RegularExpressions;
using final_para.Aspectos;
using final_para.Ecuaciones;
using final_para.Estado;
using final_para.Interfaces;

namespace final_para.Metodos;

public enum EsquemaTemporal { Explicito, Implicito, CrankNicolson }

public class FDM : MetodoNumerico, IResolver<EstadoSolucionFDM>
{
    private const int MaxIteraciones = 10_000;
    private const double Tolerancia = 1e-8;

    public EsquemaTemporal EsquemaTemporal { get; }
    public byte OrdenEspacial { get; }
    public byte OrdenTemporal { get; }
    public AlgoritmoFDM Algoritmo { get; }

    private readonly PublisherComputo? _publisherEvolucion;

    public FDM(
        double[][] malla,
        Ecuacion ecuacion,
        EsquemaTemporal esquemaTemporal,
        byte ordenEspacial,
        byte ordenTemporal,
        AlgoritmoFDM algoritmo,
        PublisherComputo? publisherEvolucion = null)
        : base(malla, ecuacion)
    {
        EsquemaTemporal = esquemaTemporal;
        OrdenEspacial = ordenEspacial;
        OrdenTemporal = ordenTemporal;
        Algoritmo = algoritmo;
        _publisherEvolucion = publisherEvolucion;
    }

    public virtual EstadoSolucionFDM Resolver()
    {
        TiempoInicio = DateTime.Now;
        NumIteraciones = 0;

        var actual = Clonar(Malla);
        var anterior = Clonar(Malla);
        double residuo = double.MaxValue;
        string metodoNombre = $"FDM-{EsquemaTemporal}";

        while (NumIteraciones < MaxIteraciones && residuo > Tolerancia)
        {
            var siguiente = Algoritmo(actual, anterior);
            residuo = NormaDiferencia(actual, siguiente);
            anterior = actual;
            actual = siguiente;
            NumIteraciones++;

            if (_publisherEvolucion is not null)
            {
                var residuoCapturado = residuo;
                var actualCapturado = actual;
                var iterCapturada = NumIteraciones;
                _publisherEvolucion.Disparar(() => new EstadoSolucionFDM(
                    residuoCapturado,
                    Clonar(actualCapturado),
                    tamanoMalla: Malla.Length,
                    numIteraciones: iterCapturada,
                    tiempoSegundos: (DateTime.Now - TiempoInicio).TotalSeconds,
                    metodoNombre: $"{metodoNombre}-evol"));
            }
        }

        Malla = actual;
        TiempoFin = DateTime.Now;
        return new EstadoSolucionFDM(
            residuo,
            actual,
            tamanoMalla: Malla.Length,
            numIteraciones: NumIteraciones,
            tiempoSegundos: (TiempoFin!.Value - TiempoInicio).TotalSeconds,
            metodoNombre: metodoNombre);
    }

    public override double CalcularCostoComputacional()
    {
        var fin = TiempoFin ?? DateTime.Now;
        return (fin - TiempoInicio).TotalSeconds;
    }

    public override double CalcularError()
    {
        if (Malla.Length < 3 || Malla[0].Length < 3) return 0.0;

        int filas = Malla.Length;
        int cols = Malla[0].Length;
        double sumaCuadrados = 0.0;
        int n = 0;

        var stencils = ConstruirStencils();

        for (int i = 1; i < filas - 1; i++)
        {
            for (int j = 1; j < cols - 1; j++)
            {
                double residuo = 0.0;
                foreach (var termino in Ecuacion.Terminos)
                {
                    double valor = EvaluarTerminoFD(termino, stencils, i, j);
                    residuo += termino.EsPositivo ? valor : -valor;
                }
                sumaCuadrados += residuo * residuo;
                n++;
            }
        }

        return n == 0 ? 0.0 : Math.Sqrt(sumaCuadrados / n);
    }

    private static readonly Regex DerivadaRegex = new(@"u_([a-z]+)", RegexOptions.Compiled);

    private static Dictionary<string, Func<double[][], int, int, double>> ConstruirStencils() =>
        new()
        {
            ["xx"] = (u, i, j) => u[i + 1][j] - 2.0 * u[i][j] + u[i - 1][j],
            ["yy"] = (u, i, j) => u[i][j + 1] - 2.0 * u[i][j] + u[i][j - 1],
            ["x"]  = (u, i, j) => 0.5 * (u[i + 1][j] - u[i - 1][j]),
            ["y"]  = (u, i, j) => 0.5 * (u[i][j + 1] - u[i][j - 1]),
            ["xy"] = (u, i, j) => 0.25 * (u[i + 1][j + 1] - u[i + 1][j - 1] - u[i - 1][j + 1] + u[i - 1][j - 1]),
            ["t"]  = (_, _, _) => 0.0,
            ["tt"] = (_, _, _) => 0.0,
        };

    private double EvaluarTerminoFD(
        Termino termino,
        Dictionary<string, Func<double[][], int, int, double>> stencils,
        int i,
        int j)
    {
        var match = DerivadaRegex.Match(termino.Expresion);
        if (match.Success && stencils.TryGetValue(match.Groups[1].Value, out var stencil))
            return stencil(Malla, i, j);

        if (double.TryParse(termino.Expresion, out double valor))
            return valor;

        return 0.0;
    }

    private static double[][] Clonar(double[][] origen)
    {
        var copia = new double[origen.Length][];
        for (int i = 0; i < origen.Length; i++)
        {
            copia[i] = new double[origen[i].Length];
            Array.Copy(origen[i], copia[i], origen[i].Length);
        }
        return copia;
    }

    private static double NormaDiferencia(double[][] a, double[][] b)
    {
        double suma = 0.0;
        int n = 0;
        for (int i = 0; i < a.Length; i++)
        {
            for (int j = 0; j < a[i].Length; j++)
            {
                double d = a[i][j] - b[i][j];
                suma += d * d;
                n++;
            }
        }
        return n == 0 ? 0.0 : Math.Sqrt(suma / n);
    }
}
