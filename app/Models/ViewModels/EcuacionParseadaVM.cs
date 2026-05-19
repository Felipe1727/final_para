namespace app.Models.ViewModels;

public class EcuacionParseadaVM
{
    public string TextoPlano { get; set; } = string.Empty;
    public string Latex { get; set; } = string.Empty;
    public string[] VariablesIndependientes { get; set; } = [];
    public string[] VariablesDependientes { get; set; } = [];
    public byte Orden { get; set; }
    public bool EsValida { get; set; }
    public IReadOnlyList<string> Errores { get; set; } = [];
}
