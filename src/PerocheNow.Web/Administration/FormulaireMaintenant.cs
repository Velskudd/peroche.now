using System.ComponentModel.DataAnnotations;
using PerocheNow.Business.Maintenant;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Web.Administration;

/// <summary>
/// Le formulaire « En ce moment » de l'administration. Les règles fines (longueurs, nombre de lignes)
/// restent celles du domaine : ce formulaire ne fait que les annoncer pour aider la saisie.
/// </summary>
public sealed class FormulaireMaintenant
{
    [Required(ErrorMessage = "L'accroche est obligatoire.")]
    [StringLength(StatutDuMoment.LongueurMaxAccroche, ErrorMessage = "L'accroche ne doit pas dépasser {1} caractères.")]
    public string Accroche { get; set; } = string.Empty;

    /// <summary>Toujours trois emplacements ; une ligne laissée vide n'est pas affichée.</summary>
    public LigneFormulaire[] Lignes { get; set; } = Vides();

    [StringLength(Objectif.LongueurMaxLibelle, ErrorMessage = "Le prochain objectif ne doit pas dépasser {1} caractères.")]
    public string? LibelleObjectif { get; set; }

    public DateOnly? DateObjectif { get; set; }

    public static FormulaireMaintenant Depuis(StatutDuMomentDto? statut)
    {
        if (statut is null)
            return new FormulaireMaintenant();

        // Les lignes existantes, complétées par des emplacements vides jusqu'à trois.
        var lignes = statut.Lignes
            .Select(l => new LigneFormulaire { Texte = l.Texte, Rubrique = l.Rubrique })
            .Concat(Vides())
            .Take(StatutDuMoment.NombreMaxLignes)
            .ToArray();

        return new FormulaireMaintenant
        {
            Accroche = statut.Accroche,
            Lignes = lignes,
            LibelleObjectif = statut.ProchainObjectif?.Libelle,
            DateObjectif = statut.ProchainObjectif?.DateCible,
        };
    }

    public DefinirStatutDuMoment EnCommande() => new(
        Accroche,
        Lignes
            .Where(l => !string.IsNullOrWhiteSpace(l.Texte))
            .Select(l => new LigneSaisie(l.Texte!, l.Rubrique))
            .ToList(),
        LibelleObjectif,
        string.IsNullOrWhiteSpace(LibelleObjectif) ? null : DateObjectif);

    private static LigneFormulaire[] Vides() =>
        Enumerable.Range(0, StatutDuMoment.NombreMaxLignes).Select(_ => new LigneFormulaire()).ToArray();
}

public sealed class LigneFormulaire
{
    public string? Texte { get; set; }
    public Rubrique Rubrique { get; set; }
}
