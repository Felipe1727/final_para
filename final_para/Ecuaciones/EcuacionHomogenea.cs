namespace final_para.Ecuaciones;

public class EcuacionHomogenea : Ecuacion
{
    public EcuacionHomogenea(
        string funcion,
        string[] variablesDependientes,
        string[] variablesIndependientes,
        byte orden,
        string[] condicionesIniciales,
        string[] condicionesFrontera,
        bool lineal,
        Geometria geometria,
        bool dependenciaTiempo)
        : base(funcion, variablesDependientes, variablesIndependientes, orden,
            condicionesIniciales, condicionesFrontera, lineal, geometria, dependenciaTiempo)
    { }
}