using final_para.Ecuaciones;
using final_para.Interfaces;

namespace final_para.Metodos;

public enum EsquemaTemporal { Explicito, Implicito, CrankNicolson }

public class FDM : MetodoNumerico, IResolver<double[][]>
{
    private EsquemaTemporal EsquemaTemporal { get; set; }
    private byte OrdenEspacial { get; set; }
    private byte OrdenTemporal { get; set; }
    private AlgoritmoFDM Algoritmo { get; set; }

    public FDM(
        double[][] malla,
        Ecuacion ecuacion,
        IResolver<object> resolver,
        EsquemaTemporal esquemaTemporal,
        byte ordenEspacial,
        byte ordenTemporal,
        AlgoritmoFDM algoritmo)
        : base(malla, ecuacion, resolver)
    {
        EsquemaTemporal = esquemaTemporal;
        OrdenEspacial = ordenEspacial;
        OrdenTemporal = ordenTemporal;
        Algoritmo = algoritmo;
    }

    public double[][] Resolver()
    {
        // TODO: implementar lógica FDM usando Algoritmo
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
