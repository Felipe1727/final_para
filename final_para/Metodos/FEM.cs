using final_para.Ecuaciones;
using final_para.Interfaces;

namespace final_para.Metodos;

public enum TipoElemento { Triangular, Cuadrilateral, Tetrahedral }

public class FEM : MetodoNumerico, IResolver<double[]>
{
    private TipoElemento Elemento { get; set; }
    private FuncionBase[] FuncionesBase { get; set; }
    private double[][] MatrizRigidez { get; set; }
    private AlgoritmoFEM Algoritmo { get; set; }

    public FEM(
        double[][] malla,
        Ecuacion ecuacion,
        IResolver<object> resolver,
        TipoElemento elemento,
        FuncionBase[] funcionesBase,
        AlgoritmoFEM algoritmo)
        : base(malla, ecuacion, resolver)
    {
        Elemento = elemento;
        FuncionesBase = funcionesBase;
        MatrizRigidez = [];
        Algoritmo = algoritmo;
    }

    public double[] Resolver()
    {
        // TODO: implementar lógica FEM usando Algoritmo y FuncionesBase
        throw new NotImplementedException();
    }

    public override double CalcularCostoComputacional()
    {
        throw new NotImplementedException();
    }

    public override double CalcularError()
    {
        throw new NotImplementedException();
    }
}