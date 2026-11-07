using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Medias;

namespace PerocheNow.Domain.Tests.Medias;

public sealed class RegleDesMediasTests
{
    internal static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    internal static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    internal static readonly byte[] WebP = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBPVP8 "u8];
    internal static readonly byte[] Mp4 = [0x00, 0x00, 0x00, 0x20, .. "ftypisom"u8, 0x00, 0x00, 0x02, 0x00];

    [Fact]
    public void Une_photo_est_reconnue_a_sa_signature_et_nommee_d_apres_son_emplacement()
    {
        var fichier = RegleDesMedias.Verifier("nur24.route.2026", GenreDeMedia.Image, Jpeg);

        Assert.Equal("image/jpeg", fichier.TypeContenu);
        Assert.Equal(Jpeg.Length, fichier.Taille);
        Assert.Matches("^nur24.route.2026-[0-9a-f]{12}\\.jpg$", fichier.Nom);
        Assert.True(RegleDesMedias.EstUnNomDeFichier(fichier.Nom));
    }

    [Fact]
    public void Png_webp_et_mp4_sont_reconnus()
    {
        Assert.Equal("image/png", RegleDesMedias.Verifier("a", GenreDeMedia.Image, Png).TypeContenu);
        Assert.Equal("image/webp", RegleDesMedias.Verifier("a", GenreDeMedia.Image, WebP).TypeContenu);
        Assert.Equal("video/mp4", RegleDesMedias.Verifier("a", GenreDeMedia.Video, Mp4).TypeContenu);
    }

    [Fact]
    public void Deux_fichiers_differents_ont_deux_noms_differents()
    {
        var premier = RegleDesMedias.Verifier("accueil.ouverture.image", GenreDeMedia.Image, Jpeg);
        var second = RegleDesMedias.Verifier("accueil.ouverture.image", GenreDeMedia.Image, [.. Jpeg, 0x01]);

        Assert.NotEqual(premier.Nom, second.Nom);
    }

    [Fact]
    public void Une_video_a_la_place_d_une_photo_est_refusee()
    {
        Assert.Throws<RegleMetierException>(() => RegleDesMedias.Verifier("a", GenreDeMedia.Image, Mp4));
        Assert.Throws<RegleMetierException>(() => RegleDesMedias.Verifier("a", GenreDeMedia.Video, Jpeg));
    }

    [Fact]
    public void Un_fichier_inconnu_ou_vide_est_refuse()
    {
        Assert.Throws<RegleMetierException>(() => RegleDesMedias.Verifier("a", GenreDeMedia.Image, "<svg></svg>"u8.ToArray()));
        Assert.Throws<RegleMetierException>(() => RegleDesMedias.Verifier("a", GenreDeMedia.Image, []));
    }

    [Fact]
    public void Une_photo_trop_lourde_est_refusee()
    {
        var lourde = new byte[RegleDesMedias.TailleMaxImage + 1];
        Jpeg.CopyTo(lourde, 0);

        Assert.Throws<RegleMetierException>(() => RegleDesMedias.Verifier("a", GenreDeMedia.Image, lourde));
    }

    [Theory]
    [InlineData("accueil.ouverture.image-0123456789ab.jpg", true)]
    [InlineData("../copies/acces-instagram.json", false)]
    [InlineData("a/b.jpg", false)]
    [InlineData("A.JPG", false)]
    [InlineData("", false)]
    public void Seuls_les_noms_ranges_par_le_site_sont_acceptes(string nom, bool accepte)
    {
        Assert.Equal(accepte, RegleDesMedias.EstUnNomDeFichier(nom));
    }
}
