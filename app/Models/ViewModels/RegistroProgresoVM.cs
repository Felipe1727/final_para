namespace app.Models.ViewModels;

/// <summary>
/// Representa un evento de progreso capturado durante la resolución.
/// Se usa para almacenar y visualizar la evolución del proceso iterativo.
/// </summary>
public class RegistroProgresoVM
{
    public int Iteracion { get; set; }
    public string MetodoNombre { get; set; } = string.Empty;
    public double Residuo { get; set; }
    public double TiempoSegundos { get; set; }
    public DateTime FechaRegistro { get; set; }
}
