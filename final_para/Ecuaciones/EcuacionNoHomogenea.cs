namespace final_para.Ecuaciones;

public class EcuacionNoHomogenea : Ecuacion
{
    private string TerminoExterno { get; set; }

    public EcuacionNoHomogenea(
        string funcion,
        string[] variablesDependientes,
        string[] variablesIndependientes,
        byte orden,
        string[] condicionesIniciales,
        string[] condicionesFrontera,
        bool lineal,
        Geometria geometria,
        bool dependenciaTiempo,
        string terminoExterno)
        : base(funcion, variablesDependientes, variablesIndependientes, orden,
            condicionesIniciales, condicionesFrontera, lineal, geometria, dependenciaTiempo)
    {
        TerminoExterno = terminoExterno;
    }
}