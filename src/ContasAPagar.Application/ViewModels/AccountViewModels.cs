using System.ComponentModel.DataAnnotations;

namespace ContasAPagar.Web.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage = "Informe o e-mail")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    public bool Lembrar { get; set; }
    public string? ReturnUrl { get; set; }
}
