using System.ComponentModel.DataAnnotations;

namespace app.Models.ViewModels;

public class EcuacionInputVM
{
    [Required(ErrorMessage = "La ecuación es obligatoria.")]
    [Display(Name = "Ecuación")]
    public string TextoPlano { get; set; } = string.Empty;
}
