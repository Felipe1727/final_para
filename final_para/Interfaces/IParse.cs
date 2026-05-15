using final_para.Ecuaciones;

namespace final_para.Interfaces;

public interface IParse
{
    string ParseLatex(string expresion);
    Ecuacion ParseFunc(string expresion);
}