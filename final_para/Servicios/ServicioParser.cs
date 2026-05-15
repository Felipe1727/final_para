using final_para.Interfaces;
using final_para.Ecuaciones;

namespace NumericalSolver.Servicios.Servicios;

public class ServicioParser : IParse
{
    private readonly Dictionary<string, string> _regexLatex;
    private readonly Dictionary<string, string> _regexFunc;

    public ServicioParser(
        Dictionary<string, string> regexLatex,
        Dictionary<string, string> regexFunc)
    {
        _regexLatex = regexLatex;
        _regexFunc = regexFunc;
    }

    public string ParseLatex(string expresion)
    {
        // TODO: aplicar transformaciones de _regexLatex sobre expresion
        throw new NotImplementedException();
    }

    public Ecuacion ParseFunc(string expresion)
    {
        // TODO: parsear expresion con _regexFunc y construir un objeto Ecuacion
        throw new NotImplementedException();
    }
}