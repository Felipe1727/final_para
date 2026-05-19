namespace app.Models.ViewModels;

public class ComparacionVM
{
    public double DiferenciaTiempo { get; set; }
    public double DiferenciaError { get; set; }
    public string GanadorTiempo { get; set; } = string.Empty;
    public string GanadorError { get; set; } = string.Empty;
}
