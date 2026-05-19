namespace final_para.Ecuaciones;

public class EcuacionBuilder
{
    private Termino[] _terminos = [];
    private string[] _variablesDependientes = ["u"];
    private string[] _variablesIndependientes = [];
    private byte _orden = 2;
    private string[] _condicionesIniciales = [];
    private string[] _condicionesFrontera = [];
    private bool _lineal = true;
    private Geometria _geometria = Geometria.Eliptica;
    private bool _dependenciaTiempo = false;

    public EcuacionBuilder ConTerminos(params Termino[] terminos)
    {
        _terminos = terminos;
        return this;
    }

    public EcuacionBuilder ConVariablesDependientes(params string[] vars)
    {
        _variablesDependientes = vars;
        return this;
    }

    public EcuacionBuilder ConVariablesIndependientes(params string[] vars)
    {
        _variablesIndependientes = vars;
        return this;
    }

    public EcuacionBuilder ConOrden(byte orden)
    {
        _orden = orden;
        return this;
    }

    public EcuacionBuilder ConCondicionesIniciales(params string[] condiciones)
    {
        _condicionesIniciales = condiciones;
        return this;
    }

    public EcuacionBuilder ConCondicionesFrontera(params string[] condiciones)
    {
        _condicionesFrontera = condiciones;
        return this;
    }

    public EcuacionBuilder Lineal(bool valor = true)
    {
        _lineal = valor;
        return this;
    }

    public EcuacionBuilder ConGeometria(Geometria geometria)
    {
        _geometria = geometria;
        return this;
    }

    public EcuacionBuilder DependeDelTiempo(bool valor = true)
    {
        _dependenciaTiempo = valor;
        return this;
    }

    public Ecuacion Build()
    {
        if (_terminos.Length == 0)
            throw new InvalidOperationException("Debe especificar al menos un Termino con ConTerminos().");

        if (_variablesIndependientes.Length == 0)
            throw new InvalidOperationException("Debe especificar al menos una variable independiente.");

        return new Ecuacion(
            terminos: _terminos,
            variablesDependientes: _variablesDependientes,
            variablesIndependientes: _variablesIndependientes,
            orden: _orden,
            condicionesIniciales: _condicionesIniciales,
            condicionesFrontera: _condicionesFrontera,
            lineal: _lineal,
            geometria: _geometria,
            dependenciaTiempo: _dependenciaTiempo);
    }
}
