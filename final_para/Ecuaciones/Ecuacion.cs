using System.Text;
using System.Text.RegularExpressions;

namespace final_para.Ecuaciones;

public class Ecuacion
{
    protected Termino[] Terminos { get; set; }
    protected string[] VariablesDependientes { get; set; }
    protected string[] VariablesIndependientes { get; set; }
    protected byte Orden { get; set; }
    protected string[] CondicionesIniciales { get; set; }
    protected string[] CondicionesFrontera { get; set; }
    protected bool Lineal { get; set; }
    protected Geometria Geometria { get; set; }
    protected bool DependenciaTiempo { get; set; }

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
}