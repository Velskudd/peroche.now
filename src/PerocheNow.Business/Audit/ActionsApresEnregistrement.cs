namespace PerocheNow.Business.Audit;

/// <summary>
/// Ce qu'un gestionnaire de commande veut faire une fois ses modifications enregistrées en base
/// (publier une copie de lecture, par exemple). Une action ne part que si l'enregistrement a réussi :
/// une copie ne montre jamais ce que la base n'a pas gardé.
/// <para>Une instance par portée : les actions d'une commande ne débordent pas sur la suivante.</para>
/// </summary>
public sealed class ActionsApresEnregistrement
{
    private readonly List<Func<CancellationToken, Task>> _actions = [];

    public void Ajouter(Func<CancellationToken, Task> action) => _actions.Add(action);

    /// <summary>Lance les actions en attente, dans l'ordre, puis les oublie.</summary>
    /// <exception cref="PublicationIncompleteException">Si une action échoue : la base, elle, est déjà enregistrée.</exception>
    internal async Task ExecuterAsync(CancellationToken annulation)
    {
        var actions = _actions.ToList();
        _actions.Clear();

        try
        {
            foreach (var action in actions)
                await action(annulation);
        }
        catch (Exception erreur) when (erreur is not OperationCanceledException)
        {
            throw new PublicationIncompleteException(erreur);
        }
    }

    /// <summary>La commande a échoué : rien n'a été enregistré, donc rien à publier.</summary>
    internal void Abandonner() => _actions.Clear();
}

/// <summary>
/// La commande est bien enregistrée en base, mais une action qui devait suivre (la mise à jour d'une copie
/// de lecture) a échoué. Recommencer l'opération suffit à remettre la copie d'aplomb.
/// </summary>
public sealed class PublicationIncompleteException(Exception cause)
    : Exception("L'enregistrement a réussi, mais la copie lue par le site n'a pas pu être mise à jour.", cause);
