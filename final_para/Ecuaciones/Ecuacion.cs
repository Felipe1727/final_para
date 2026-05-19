using System.Text;
using System.Text.RegularExpressions;

namespace final_para.Ecuaciones;

public class Ecuacion
{
    public Termino[] Terminos { get; protected set; }
    public string[] VariablesDependientes { get; protected set; }
    public string[] VariablesIndependientes { get; protected set; }
    public byte Orden { get; protected set; }
    public string[] CondicionesIniciales { get; protected set; }
    public string[] CondicionesFrontera { get; protected set; }
    public bool Lineal { get; protected set; }
    public Geometria Geometria { get; protected set; }
    public bool DependenciaTiempo { get; protected set; }

    // Orden máximo de derivación por cada variable independiente.
    // ej. Laplace u_xx + u_yy = 0  -> { "x": 2, "y": 2 }
    //     onda    u_tt - u_xx = 0  -> { "t": 2, "x": 2 }
    //     calor   u_t  - u_xx = 0  -> { "t": 1, "x": 2 }
    public IReadOnlyDictionary<string, byte> OrdenesPorVariable { get; protected set; }

    public Ecuacion(
        Termino[] terminos,
        string[] variablesDependientes,
        string[] variablesIndependientes,
        byte orden,
        string[] condicionesIniciales,
        string[] condicionesFrontera,
        bool lineal,
        Geometria geometria,
        bool dependenciaTiempo,
        IReadOnlyDictionary<string, byte>? ordenesPorVariable = null)
    {
        Terminos = terminos;
        VariablesDependientes = variablesDependientes;
        VariablesIndependientes = variablesIndependientes;
        Orden = orden;
        CondicionesIniciales = condicionesIniciales;
        CondicionesFrontera = condicionesFrontera;
        Lineal = lineal;
        Geometria = geometria;
        DependenciaTiempo = dependenciaTiempo;
        OrdenesPorVariable = ordenesPorVariable ?? ConstruirOrdenesFallback(variablesIndependientes, orden);
    }

    // Fallback usado cuando un caller antiguo no provee OrdenesPorVariable.
    // Asume el mismo Orden global para cada variable independiente; mantiene compat
    // pero no es precisa para ecuaciones mixtas como el calor.
    private static IReadOnlyDictionary<string, byte> ConstruirOrdenesFallback(
        string[] variablesIndependientes,
        byte orden)
    {
        var dict = new Dictionary<string, byte>();
        foreach (var v in variablesIndependientes)
        {
            dict[v] = orden;
        }
        return dict;
    }

    public IEnumerable<string> VariablesEspaciales =>
        VariablesIndependientes.Where(v => v != "t");

    public bool EsTemporal(string v) => v == "t";

    public int NumCondicionesInicialesRequeridas() =>
        OrdenesPorVariable.TryGetValue("t", out var k) ? k : 0;

    public int NumCondicionesFronteraRequeridas() =>
        VariablesEspaciales.Sum(v => (int)OrdenesPorVariable[v]);

    public string ConstruirFuncion()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Terminos.Length; i++)
        {
            if (i == 0)
            {
                if (!Terminos[i].EsPositivo) sb.Append("-");
            }
            else
            {
                sb.Append(Terminos[i].EsPositivo ? " + " : " - ");
            }
            sb.Append(Terminos[i].Expresion);
        }
        sb.Append(" = 0");
        return sb.ToString();
    }

    public bool EsHomogenea =>
        Terminos.All(t => Regex.IsMatch(t.Expresion, @"\bu_[a-z]+\b"));

    public IEnumerable<Termino> TerminosForzantes =>
        Terminos.Where(t => !Regex.IsMatch(t.Expresion, @"\bu_[a-z]+\b"));
}
