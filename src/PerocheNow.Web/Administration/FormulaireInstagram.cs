using System.ComponentModel.DataAnnotations;

namespace PerocheNow.Web.Administration;

/// <summary>Le formulaire de l'administration qui branche le compte Instagram.</summary>
public sealed class FormulaireInstagram
{
    [Required(ErrorMessage = "Colle le jeton d'accès Instagram.")]
    public string Jeton { get; set; } = string.Empty;

    /// <summary>Jamais le jeton dans un journal.</summary>
    public override string ToString() => nameof(FormulaireInstagram);
}
