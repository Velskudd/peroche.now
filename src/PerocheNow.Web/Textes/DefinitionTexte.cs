namespace PerocheNow.Web.Textes;

/// <summary>Comment un texte se saisit dans l'administration.</summary>
public enum Saisie
{
    /// <summary>Une ligne : un titre, un bouton, un lien.</summary>
    Ligne,

    /// <summary>Un paragraphe.</summary>
    Paragraphe,

    /// <summary>Une liste : un élément par ligne.</summary>
    Liste,
}

/// <summary>
/// Un texte du site : son repère, où il se trouve, comment l'administration le présente
/// et le texte d'origine, affiché tant qu'il n'a pas été modifié.
/// </summary>
public sealed record DefinitionTexte(
    string Cle,
    string Section,
    string Libelle,
    string ParDefaut,
    int LongueurMax,
    Saisie Saisie);

/// <summary>Les textes d'une page du site, dans l'ordre où ils y apparaissent.</summary>
public sealed class PageDeTextes(string code, string nom, string adresse)
{
    private readonly List<DefinitionTexte> _textes = [];

    /// <summary>Le code de la page dans l'administration (<c>/admin/textes?page=…</c>).</summary>
    public string Code { get; } = code;

    public string Nom { get; } = nom;

    /// <summary>Où voir la page sur le site.</summary>
    public string Adresse { get; } = adresse;

    public IReadOnlyList<DefinitionTexte> Textes => _textes;

    /// <summary>Déclare un texte de la page ; son repère est préfixé par le code de la page.</summary>
    public DefinitionTexte Ligne(string cle, string section, string libelle, string parDefaut, int longueurMax = 120) =>
        Ajouter(cle, section, libelle, parDefaut, longueurMax, Saisie.Ligne);

    public DefinitionTexte Paragraphe(string cle, string section, string libelle, string parDefaut, int longueurMax = 600) =>
        Ajouter(cle, section, libelle, parDefaut, longueurMax, Saisie.Paragraphe);

    public DefinitionTexte Liste(string cle, string section, string libelle, string parDefaut, int longueurMax = 600) =>
        Ajouter(cle, section, libelle, parDefaut, longueurMax, Saisie.Liste);

    private DefinitionTexte Ajouter(string cle, string section, string libelle, string parDefaut, int longueurMax, Saisie saisie)
    {
        var texte = new DefinitionTexte($"{Code}.{cle}", section, libelle, parDefaut, longueurMax, saisie);
        _textes.Add(texte);
        return texte;
    }
}
