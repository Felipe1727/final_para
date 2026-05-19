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

    // Stub temporal: Unit 1 lo sustituirá por un cálculo per-variable real.
    // Mientras tanto se asigna el Orden global a cada variable independiente.
    public virtual IReadOnlyDictionary<string, byte> OrdenesPorVariable =>
        VariablesIndependientes.ToDictionary(v => v, _ => Orden);

    public IEnumerable<string> VariablesEspaciales =>
        VariablesIndependientes.Where(v => v != "t");

    public bool EsTemporal(string v) => v == "t";

    public int NumCondicionesInicialesRequeridas()
    {
        if (!DependenciaTiempo) return 0;
        return OrdenesPorVariable.TryGetValue("t", out var ord) ? ord : Orden;
    }

    public int NumCondicionesFronteraRequeridas() =>
        VariablesEspaciales.Sum(v => OrdenesPorVariable.TryGetValue(v, out var ord) ? (int)ord : (int)Orden);
}