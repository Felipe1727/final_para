namespace final_para.Ecuaciones;

public abstract class Ecuacion
{
    protected string Funcion { get; set; }
    protected string[] VariablesDependientes { get; set; }
    protected string[] VariablesIndependientes { get; set; }
    protected byte Orden { get; set; }
    protected string[] CondicionesIniciales { get; set; }
    protected string[] CondicionesFrontera { get; set; }
    protected bool Lineal { get; set; }
    protected Geometria Geometria { get; set; }
    protected bool DependenciaTiempo { get; set; }

    protected Ecuacion(
        string funcion,
        string[] variablesDependientes,
        string[] variablesIndependientes,
        byte orden,
        string[] condicionesIniciales,
        string[] condicionesFrontera,
        bool lineal,
        Geometria geometria,
        bool dependenciaTiempo)
    {
        Funcion = funcion;
        VariablesDependientes = variablesDependientes;
        VariablesIndependientes = variablesIndependientes;
        Orden = orden;
        CondicionesIniciales = condicionesIniciales;
        CondicionesFrontera = condicionesFrontera;
        Lineal = lineal;
        Geometria = geometria;
        DependenciaTiempo = dependenciaTiempo;
    }
}