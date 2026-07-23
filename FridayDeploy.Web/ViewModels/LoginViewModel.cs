using System.ComponentModel.DataAnnotations;

namespace FridayDeploy.Web.ViewModels;

public sealed class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
