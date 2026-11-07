using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Business.Medias;
using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Medias;

namespace PerocheNow.Business.Tests.Medias;

public sealed class MediasDuSiteTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

    [Fact]
    public async Task Une_photo_envoyee_est_rangee_puis_notee_et_la_copie_suit_la_base()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        var nom = await Banc.Mediateur(portee).EnvoyerAsync(new DeposerMedia("nur24.route.2027", GenreDeMedia.Image, Jpeg));

        var (depose, enregistrementsAuDepot) = Assert.Single(banc.Stockage.Depots);
        Assert.Equal(nom, depose);
        Assert.Equal(0, enregistrementsAuDepot);
        var media = Assert.Single(banc.Medias.Medias);
        Assert.Equal(("nur24.route.2027", nom, "image/jpeg"), (media.Cle, media.Fichier, media.TypeContenu));

        var (copie, enregistrementsAvant) = Assert.Single(banc.Copies.Publications);
        Assert.Equal(NomsDesCopies.MediasDuSite, copie);
        Assert.Equal(1, enregistrementsAvant);
        Assert.Equal(nom, banc.Copies.Copie<MediasPublies>(NomsDesCopies.MediasDuSite)?.Medias["nur24.route.2027"].Fichier);
    }

    [Fact]
    public async Task Renvoyer_une_photo_remplace_le_fichier_de_l_emplacement()
    {
        using var banc = new Banc();
        using (var premiere = banc.NouvellePortee())
            await Banc.Mediateur(premiere).EnvoyerAsync(new DeposerMedia("liens.photo", GenreDeMedia.Image, Jpeg));

        using var seconde = banc.NouvellePortee();
        var nouveau = await Banc.Mediateur(seconde).EnvoyerAsync(new DeposerMedia("liens.photo", GenreDeMedia.Image, (byte[])[.. Jpeg, 0x02]));

        Assert.Equal(nouveau, Assert.Single(banc.Medias.Medias).Fichier);
        Assert.Equal(2, banc.Stockage.Fichiers.Count);
    }

    [Fact]
    public async Task Un_fichier_refuse_n_est_ni_range_ni_note()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        await Assert.ThrowsAsync<RegleMetierException>(() =>
            Banc.Mediateur(portee).EnvoyerAsync(new DeposerMedia("accueil.ouverture.video", GenreDeMedia.Video, Jpeg)));

        Assert.Empty(banc.Stockage.Depots);
        Assert.Empty(banc.Medias.Medias);
        Assert.Empty(banc.Copies.Publications);
    }

    [Fact]
    public async Task Retirer_un_media_rend_le_fichier_d_origine()
    {
        using var banc = new Banc();
        using (var envoi = banc.NouvellePortee())
            await Banc.Mediateur(envoi).EnvoyerAsync(new DeposerMedia("liens.photo", GenreDeMedia.Image, Jpeg));

        using var retrait = banc.NouvellePortee();
        Assert.True(await Banc.Mediateur(retrait).EnvoyerAsync(new RetirerMedia("liens.photo")));

        Assert.Empty(banc.Medias.Medias);
        Assert.Empty(Assert.IsType<MediasPublies>(banc.Copies.Copie<MediasPublies>(NomsDesCopies.MediasDuSite)).Medias);
    }

    [Fact]
    public async Task Retirer_un_emplacement_jamais_remplace_ne_fait_rien()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        Assert.False(await Banc.Mediateur(portee).EnvoyerAsync(new RetirerMedia("liens.photo")));
        Assert.Empty(banc.Copies.Publications);
    }

    [Fact]
    public async Task Les_pages_publiques_lisent_la_copie_et_ne_reconstruisent_qu_une_fois()
    {
        using var banc = new Banc();

        for (var visite = 0; visite < 3; visite++)
        {
            using var portee = banc.NouvellePortee();
            Assert.Empty((await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirMediasPublies())).Medias);
        }

        Assert.Equal(1, banc.Medias.Lectures);
    }

    [Fact]
    public void Le_contenu_n_apparait_jamais_dans_un_journal()
    {
        Assert.DoesNotContain("255", new DeposerMedia("a", GenreDeMedia.Image, Jpeg).ToString(), StringComparison.Ordinal);
    }
}
