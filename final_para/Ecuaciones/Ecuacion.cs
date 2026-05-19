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

    public Ecuacion(
        Termino[] terminos,
        string[] variablesDependientes,
        string[] variablesIndependientes,
        byte orden,
        string[] condicionesIniciales,
        string[] condicionesFrontera,
        bool lineal,
        Geometria geometria,
        bool dependenciaTiempo)
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
    }

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

    private IReadOnlyDictionary<string, byte>? _ordenesPorVariableCache;

    /// <summary>
    /// Orden máximo de derivación detectado por variable independiente.
    /// Ej.: Laplace u_xx + u_yy = 0 → { "x": 2, "y": 2 };
    ///      onda u_tt − u_xx = 0 → { "t": 2, "x": 2 };
    ///      calor u_t − u_xx = 0 → { "t": 1, "x": 2 }.
    /// </summary>
    public virtual IReadOnlyDictionary<string, byte> OrdenesPorVariable
    {
        get
        {
            if (_ordenesPorVariableCache is not null) return _ordenesPorVariableCache;

            var ordenes = new Dictionary<string, byte>();
            foreach (var termino in Terminos)
            {
                foreach (Match m in Regex.Matches(termino.Expresion, @"u_([a-z]+)"))
                {
                    var subs = m.Groups[1].Value;
                    var conteo = new Dictionary<char, byte>();
                    foreach (var c in subs)
                    {
                        conteo[c] = (byte)(conteo.GetValueOrDefault(c, (byte)0) + 1);
                    }
                    foreach (var (c, n) in conteo)
                    {
                        var key = c.ToString();
                        if (!ordenes.TryGetValue(key, out var prev) || n > prev)
                            ordenes[key] = n;
                    }
                }
            }

            // Asegurar que toda variable independiente declarada aparezca al menos con 0.
            foreach (var v in VariablesIndependientes)
            {
                if (!ordenes.ContainsKey(v)) ordenes[v] = 0;
            }

            _ordenesPorVariableCache = ordenes;
            return _ordenesPorVariableCache;
        }
    }

    /// <summary>Variables independientes que no son temporales (todas salvo "t").</summary>
    public IEnumerable<string> VariablesEspaciales =>
        VariablesIndependientes.Where(v => !EsTemporal(v));

    /// <summary>Indica si la variable es la temporal del problema.</summary>
    public bool EsTemporal(string v) => v == "t";

    /// <summary>Número de condiciones iniciales requeridas = orden temporal.</summary>
    public int NumCondicionesInicialesRequeridas()
    {
        return OrdenesPorVariable.TryGetValue("t", out var k) ? k : 0;
    }

    /// <summary>
    /// Número de condiciones de frontera Dirichlet requeridas: para cada variable
    /// espacial se exige una condición por extremo del dominio (Min y Max),
    /// es decir orden=k → 2 fronteras por eje espacial (truncado a k=2 como máximo
    /// usable). En la práctica devolvemos `2 * #(variables espaciales)`.
    /// </summary>
    public int NumCondicionesFronteraRequeridas()
    {
        return VariablesEspaciales.Count() * 2;
    }
}