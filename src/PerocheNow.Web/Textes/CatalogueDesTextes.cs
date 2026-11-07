namespace PerocheNow.Web.Textes;

// Tous les textes du site modifiables depuis l'administration, page par page.
// Le texte d'origine est écrit ici ; l'administration ne garde que ce qui a été modifié.
// Ne sont pas ici : ce qui se modifie dans « En ce moment » (accroche, lignes, prochain objectif),
// ce qui est calculé (heure, dates) et ce qui vient des plateformes (titres de vidéos, publications).

/// <summary>Le menu, le pied de page et les pages d'erreur, communs à tout le site.</summary>
public static class TextesCommuns
{
    public static readonly PageDeTextes Page = new("commun", "Menu et pied de page", "/");

    public static readonly DefinitionTexte MenuMaintenant = Page.Ligne("menu.maintenant", "Menu", "Accueil", "Maintenant", 30);
    public static readonly DefinitionTexte MenuProjet = Page.Ligne("menu.projet", "Menu", "Projet en cours", "NUR24", 30);
    public static readonly DefinitionTexte MenuContenus = Page.Ligne("menu.contenus", "Menu", "Contenus", "Contenus", 30);
    public static readonly DefinitionTexte MenuPartenaires = Page.Ligne("menu.partenaires", "Menu", "Partenaires", "Partenaires", 30);
    public static readonly DefinitionTexte MenuHorsPiste = Page.Ligne("menu.hors-piste", "Menu", "Nom de la rubrique hors piste", "À côté de la piste", 40);
    public static readonly DefinitionTexte Direct = Page.Ligne("direct", "Menu", "Pastille pendant un live", "En direct sur Twitch", 40);

    public static readonly DefinitionTexte RappelProjet = Page.Ligne("rappel.projet", "Rappel du projet (sous le menu)", "Nom du projet", "NUR24", 30);
    public static readonly DefinitionTexte RappelBut = Page.Ligne("rappel.but", "Rappel du projet (sous le menu)", "Le but, en une ligne", "Objectif : les 24 Heures du Nürburgring en 2029", 90);
    public static readonly DefinitionTexte RappelEtiquette = Page.Ligne("rappel.etiquette", "Rappel du projet (sous le menu)", "Devant le prochain objectif", "Prochain objectif", 40);

    public static readonly DefinitionTexte CircuitAide = Page.Ligne("circuit.aide", "Circuits", "Indication sous chaque circuit", "Survolez ou touchez un secteur pour voir l'étape.", 100);
    public static readonly DefinitionTexte CircuitSecteur = Page.Ligne("circuit.secteur", "Circuits", "Mot devant le numéro du secteur", "Secteur", 20);

    public static readonly DefinitionTexte PiedSignature = Page.Paragraphe("pied.signature", "Pied de page", "Signature",
        "Alexandre Peroche, numéro 15. Ce site se construit en public, au fil des projets.", 200);
    public static readonly DefinitionTexte PiedTitreSite = Page.Ligne("pied.titre-site", "Pied de page", "Titre de la colonne du site", "Le site", 40);
    public static readonly DefinitionTexte PiedTitreReseaux = Page.Ligne("pied.titre-reseaux", "Pied de page", "Titre de la colonne des réseaux", "Me suivre", 40);
    public static readonly DefinitionTexte PiedTitrePartenariats = Page.Ligne("pied.titre-partenariats", "Pied de page", "Titre de la colonne partenariats", "Partenariats", 40);
    public static readonly DefinitionTexte PiedGestion = Page.Ligne("pied.gestion", "Pied de page", "Qui gère les partenariats", "Gérés par Trinity Management", 80);
    public static readonly DefinitionTexte PiedDevenirPartenaire = Page.Ligne("pied.devenir-partenaire", "Pied de page", "Lien vers la page partenaires", "Devenir partenaire", 50);

    public static readonly DefinitionTexte IntrouvableOnglet = Page.Ligne("introuvable.onglet", "Page introuvable", "Titre de l'onglet", "Page introuvable | peroche.now", 70);
    public static readonly DefinitionTexte IntrouvableTitre = Page.Ligne("introuvable.titre", "Page introuvable", "Titre", "Cette page n'existe pas, ou pas encore.", 100);
    public static readonly DefinitionTexte IntrouvableTexte = Page.Paragraphe("introuvable.texte", "Page introuvable", "Texte",
        "Le site se construit au fil des projets : certaines pages arrivent plus tard.", 300);
    public static readonly DefinitionTexte IntrouvableLien = Page.Ligne("introuvable.lien", "Page introuvable", "Lien de retour", "Revenir à ce que je fais en ce moment", 80);

    public static readonly DefinitionTexte ErreurOnglet = Page.Ligne("erreur.onglet", "Page d'erreur", "Titre de l'onglet", "Erreur | peroche.now", 70);
    public static readonly DefinitionTexte ErreurTitre = Page.Ligne("erreur.titre", "Page d'erreur", "Titre", "Quelque chose a lâché.", 100);
    public static readonly DefinitionTexte ErreurTexte = Page.Paragraphe("erreur.texte", "Page d'erreur", "Texte", "La page n'a pas pu s'afficher. Réessayez dans un instant.", 300);
    public static readonly DefinitionTexte ErreurLien = Page.Ligne("erreur.lien", "Page d'erreur", "Lien de retour", "Revenir à l'accueil", 80);
    public static readonly DefinitionTexte ErreurReference = Page.Ligne("erreur.reference", "Page d'erreur", "Devant la référence de l'incident", "Référence de l'incident :", 60);
}

/// <summary>L'accueil.</summary>
public static class TextesAccueil
{
    public static readonly PageDeTextes Page = new("accueil", "Accueil", "/");

    public static readonly DefinitionTexte Onglet = Page.Ligne("onglet", "Google et partages", "Titre de l'onglet et dans Google", "Alexandre Peroche, en ce moment | peroche.now", 70);
    public static readonly DefinitionTexte Description = Page.Paragraphe("description", "Google et partages", "Description dans Google",
        "Ce que fait Alexandre Peroche en ce moment : NUR24, de la Test Week Nordschleife aux 24 Heures du Nürburgring, et tout ce qui se passe à côté de la piste.", 300);
    public static readonly DefinitionTexte PartageTitre = Page.Ligne("partage.titre", "Google et partages", "Titre d'un lien partagé (réseaux, messageries)", "Alexandre Peroche, en ce moment", 90);
    public static readonly DefinitionTexte PartageDescription = Page.Paragraphe("partage.description", "Google et partages", "Description d'un lien partagé",
        "NUR24 : de la Test Week Nordschleife aux 24 Heures du Nürburgring, en trois saisons.", 200);

    public static readonly DefinitionTexte Lieu = Page.Ligne("ouverture.lieu", "Ouverture (sur la vidéo)", "Où je suis, après l'heure", "en Bretagne", 40);
    public static readonly DefinitionTexte ApresHeure = Page.Ligne("ouverture.apres-heure", "Ouverture (sur la vidéo)", "Phrase après l'heure", "Voici ce que je fais.", 60);
    public static readonly DefinitionTexte Bouton = Page.Ligne("ouverture.bouton", "Ouverture (sur la vidéo)", "Bouton doré", "Suivre le projet NUR24", 40);
    public static readonly DefinitionTexte LienEnCeMoment = Page.Ligne("ouverture.lien", "Ouverture (sur la vidéo)", "Lien à côté du bouton", "Ce que je fais en ce moment", 50);
    public static readonly DefinitionTexte EtiquetteObjectif = Page.Ligne("ouverture.etiquette-objectif", "Ouverture (sur la vidéo)", "Au-dessus du prochain objectif", "Prochain objectif", 40);
    public static readonly DefinitionTexte DateAConfirmer = Page.Ligne("ouverture.date-a-confirmer", "Ouverture (sur la vidéo)", "Quand l'objectif n'a pas de date", "Date à confirmer", 40);

    public static readonly DefinitionTexte ProjetContexte = Page.Ligne("projet.contexte", "Le projet en cours", "Au-dessus du nom", "Le projet en cours, depuis 2026", 80);
    public static readonly DefinitionTexte ProjetNom = Page.Ligne("projet.nom", "Le projet en cours", "Nom du projet", "NUR24", 20);
    public static readonly DefinitionTexte ProjetPromesse = Page.Ligne("projet.promesse", "Le projet en cours", "Sous le nom", "De la Test Week aux 24 Heures du Nürburgring, en trois saisons.", 140);
    public static readonly DefinitionTexte ProjetOu = Page.Paragraphe("projet.ou", "Le projet en cours", "Où j'en suis",
        "Où j'en suis : engagé pour une Test Week sur la Nordschleife, et chaque semaine sur iRacing en attendant.", 300);
    public static readonly DefinitionTexte ProjetLien = Page.Ligne("projet.lien", "Le projet en cours", "Lien vers la page du projet", "Suivre le projet NUR24", 50);
    public static readonly DefinitionTexte ProjetLegende = Page.Ligne("projet.legende", "Le projet en cours", "Légende du circuit (les étapes se modifient dans la page NUR24)",
        "Nürburgring, Nordschleife et circuit Grand Prix. 20,8 kilomètres pour la boucle nord seule.", 160);

    public static readonly DefinitionTexte ContenusTitre = Page.Ligne("contenus.titre", "Derniers contenus", "Titre", "Derniers contenus", 50);
    public static readonly DefinitionTexte ContenusLien = Page.Ligne("contenus.lien", "Derniers contenus", "Lien vers tous les contenus", "Tous les contenus", 50);
    public static readonly DefinitionTexte ContenusVide = Page.Paragraphe("contenus.vide", "Derniers contenus", "Quand aucune vidéo n'est disponible",
        "Mes dernières vidéos YouTube et mes formats courts apparaîtront ici d'eux-mêmes.", 300);

    public static readonly DefinitionTexte HorsPisteTitre = Page.Ligne("hors-piste.titre", "À côté de la piste", "Titre", "À côté de la piste", 50);
    public static readonly DefinitionTexte HorsPisteLien = Page.Ligne("hors-piste.lien", "À côté de la piste", "Lien", "Voir les contenus hors piste", 50);
    public static readonly DefinitionTexte HorsPiste1Nom = Page.Ligne("hors-piste.1.nom", "À côté de la piste", "Première activité : nom", "Let's play JRPG", 50);
    public static readonly DefinitionTexte HorsPiste1Texte = Page.Ligne("hors-piste.1.texte", "À côté de la piste", "Première activité : description", "Dragon Quest et les grands JRPG, joués en direct.", 140);
    public static readonly DefinitionTexte HorsPiste2Nom = Page.Ligne("hors-piste.2.nom", "À côté de la piste", "Deuxième activité : nom", "Cartes à collectionner", 50);
    public static readonly DefinitionTexte HorsPiste2Texte = Page.Ligne("hors-piste.2.texte", "À côté de la piste", "Deuxième activité : description", "Ouvertures de boosters et parties sur Magic et Yu-Gi-Oh!.", 140);

    public static readonly DefinitionTexte PartenairesTitre = Page.Ligne("partenaires.titre", "Partenaires", "Titre", "Embarquer dès le départ.", 80);
    public static readonly DefinitionTexte PartenairesTexte = Page.Paragraphe("partenaires.texte", "Partenaires", "Texte",
        "Je n'ai pas encore de palmarès. J'ai une trajectoire, une communauté qui la suit et trois saisons devant moi jusqu'aux 24 Heures du Nürburgring. Les partenaires qui montent maintenant font partie de l'histoire.");
    public static readonly DefinitionTexte PartenairesBouton = Page.Ligne("partenaires.bouton", "Partenaires", "Bouton doré", "Devenir partenaire", 40);
}

/// <summary>La page du projet NUR24.</summary>
public static class TextesNur24
{
    public static readonly PageDeTextes Page = new("nur24", "NUR24", "/nur24");

    public static readonly DefinitionTexte Onglet = Page.Ligne("onglet", "Google", "Titre de l'onglet et dans Google", "NUR24, de la Test Week aux 24 Heures du Nürburgring | peroche.now", 90);
    public static readonly DefinitionTexte Description = Page.Paragraphe("description", "Google", "Description dans Google",
        "NUR24 : le projet d'Alexandre Peroche, de la Test Week sur la Nordschleife aux 24 Heures du Nürburgring en 2029. Où il en est, la route, d'où il part.", 300);

    public static readonly DefinitionTexte Contexte = Page.Ligne("tete.contexte", "Haut de page", "Au-dessus du nom", "Le projet en cours, depuis 2026", 80);
    public static readonly DefinitionTexte Nom = Page.Ligne("tete.nom", "Haut de page", "Nom du projet", "NUR24", 20);
    public static readonly DefinitionTexte Promesse = Page.Ligne("tete.promesse", "Haut de page", "Sous le nom", "De la Test Week aux 24 Heures du Nürburgring, en trois saisons.", 140);
    public static readonly DefinitionTexte Intro = Page.Paragraphe("tete.intro", "Haut de page", "Présentation",
        "La Nordschleife, 20,8 kilomètres et plus de 150 virages. Je ne viens pas du karting de compétition : j'y arrive par la simulation, étape par étape, et je raconte chacune d'elles.");
    public static readonly DefinitionTexte Legende = Page.Ligne("tete.legende", "Haut de page", "Légende du circuit", "La Nordschleife découpée en quatre secteurs : un par saison jusqu'aux 24 Heures.", 160);

    public static readonly DefinitionTexte OuTitre = Page.Ligne("ou.titre", "Où j'en suis", "Titre", "Où j'en suis", 50);
    public static readonly DefinitionTexte OuPhrase = Page.Paragraphe("ou.phrase", "Où j'en suis", "Texte",
        "Je suis engagé pour une Test Week sur la Nordschleife. D'ici là, je la parcours chaque semaine en simulation.", 400);
    public static readonly DefinitionTexte OuEtiquette = Page.Ligne("ou.etiquette", "Où j'en suis", "Au-dessus de la prochaine étape", "Prochaine étape", 40);
    public static readonly DefinitionTexte OuEtape = Page.Ligne("ou.etape", "Où j'en suis", "Prochaine étape", "Test Week Nordschleife", 80);
    public static readonly DefinitionTexte OuDate = Page.Ligne("ou.date", "Où j'en suis", "Date de la prochaine étape", "Date à confirmer", 40);

    public static readonly DefinitionTexte RouteTitre = Page.Ligne("route.titre", "La route", "Titre au-dessus des étapes du circuit", "La route jusqu'en 2029", 60);
    public static readonly DefinitionTexte Route2026Titre = Page.Ligne("route.2026.titre", "La route", "2026 : titre", "Préparation", 60);
    public static readonly DefinitionTexte Route2026Texte = Page.Paragraphe("route.2026.texte", "La route", "2026 : texte",
        "Test Week sur la Nordschleife, tours coachés, sessions Tourist Drive et kilomètres en simulation.", 300);
    public static readonly DefinitionTexte Route2027Titre = Page.Ligne("route.2027.titre", "La route", "2027 : titre", "Premières courses en RCN", 60);
    public static readonly DefinitionTexte Route2027Texte = Page.Paragraphe("route.2027.texte", "La route", "2027 : texte",
        "La seconde partie de la saison RCN, pour apprendre la course sur la Nordschleife.", 300);
    public static readonly DefinitionTexte Route2028Titre = Page.Ligne("route.2028.titre", "La route", "2028 : titre", "Saison RCN complète", 60);
    public static readonly DefinitionTexte Route2028Texte = Page.Paragraphe("route.2028.texte", "La route", "2028 : texte",
        "Toute la saison RCN, et au moins une manche de NLS pour franchir le palier suivant.", 300);
    public static readonly DefinitionTexte Route2029Titre = Page.Ligne("route.2029.titre", "La route", "2029 : titre", "Les 24 Heures du Nürburgring", 60);
    public static readonly DefinitionTexte Route2029Texte = Page.Paragraphe("route.2029.texte", "La route", "2029 : texte",
        "Toutes les manches NLS qui précèdent la course, puis le départ des 24 Heures.", 300);
    public static readonly DefinitionTexte RouteIci = Page.Ligne("route.ici", "La route", "Repère de l'étape en cours", "Vous êtes ici", 30);
    public static readonly DefinitionTexte RouteObjectif = Page.Ligne("route.objectif", "La route", "Repère de la dernière étape", "Objectif", 30);

    public static readonly DefinitionTexte ParcoursTitre = Page.Ligne("parcours.titre", "D'où je pars", "Titre", "D'où je pars", 50);
    public static readonly DefinitionTexte ParcoursIntro = Page.Paragraphe("parcours.intro", "D'où je pars", "Texte",
        "Un parcours atypique, que chacun peut suivre et dans lequel on peut se reconnaître.", 300);
    public static readonly DefinitionTexte ParcoursEtapes = Page.Liste("parcours.etapes", "D'où je pars", "Les étapes, une par ligne (la dernière est l'étape actuelle)",
        "Karting de location\nSimulation\nCourses d'endurance de 24 heures en ligne\nTourist Drives sur la Nordschleife\nTours coachés\nTest Week");
    public static readonly DefinitionTexte ParcoursDetails = Page.Liste("parcours.details", "D'où je pars", "Le texte de chaque étape dans sa bulle, une ligne par étape, dans le même ordre",
        "Les premiers tours, en karting de location : pas de compétition, juste l'envie de rouler.\n"
        + "La simulation pour apprendre les circuits, les trajectoires et la régularité, chaque semaine.\n"
        + "Des courses de 24 heures en ligne, en équipe : les relais, le trafic et la gestion de l'effort.\n"
        + "Mes premiers tours réels de la Nordschleife, en Tourist Drive.\n"
        + "Des tours avec un coach, pour apprendre la piste en vrai.\n"
        + "La prochaine étape : une semaine de roulage encadré sur la Nordschleife.", 1200);
    public static readonly DefinitionTexte ParcoursLegende = Page.Ligne("parcours.legende", "D'où je pars", "Légende du circuit", "Chaque secteur, une étape déjà parcourue.", 160);

    public static readonly DefinitionTexte CarnetTitre = Page.Ligne("carnet.titre", "Le carnet de route", "Titre", "Le carnet de route", 50);
    public static readonly DefinitionTexte CarnetLien = Page.Ligne("carnet.lien", "Le carnet de route", "Lien vers les contenus", "Tout le carnet de route", 50);
    public static readonly DefinitionTexte CarnetVide = Page.Paragraphe("carnet.vide", "Le carnet de route", "Quand aucune publication #NUR24 n'est disponible",
        "Un Reel par semaine sur l'avancement du projet, marqué #NUR24 : les prochains apparaîtront ici d'eux-mêmes.", 300);
    public static readonly DefinitionTexte CarnetAttente = Page.Ligne("carnet.attente", "Le carnet de route", "Devant le lien Instagram", "En attendant, ils sont sur", 80);
    public static readonly DefinitionTexte CarnetLienInstagram = Page.Ligne("carnet.lien-instagram", "Le carnet de route", "Lien Instagram", "Instagram", 40);

    public static readonly DefinitionTexte PartenairesTitre = Page.Ligne("partenaires.titre", "Partenaires", "Titre", "Votre logo sur la voiture, dès 2027.", 80);
    public static readonly DefinitionTexte PartenairesTexte = Page.Paragraphe("partenaires.texte", "Partenaires", "Texte",
        "Le projet cherche de vrais partenaires commerciaux, pas du mécénat : une image de pilote construite en public, une communauté qui suit chaque étape et trois saisons de visibilité.");
    public static readonly DefinitionTexte PartenairesBouton = Page.Ligne("partenaires.bouton", "Partenaires", "Bouton doré", "Devenir partenaire", 40);
}

/// <summary>La page des contenus.</summary>
public static class TextesContenus
{
    public static readonly PageDeTextes Page = new("contenus", "Contenus", "/contenus");

    public static readonly DefinitionTexte Onglet = Page.Ligne("onglet", "Google", "Titre de l'onglet et dans Google", "Contenus | peroche.now", 70);
    public static readonly DefinitionTexte Description = Page.Paragraphe("description", "Google", "Description dans Google",
        "Les vidéos YouTube, formats courts et publications Instagram d'Alexandre Peroche : NUR24, la Nordschleife, et tout ce qui se passe à côté de la piste.", 300);

    public static readonly DefinitionTexte Titre = Page.Ligne("tete.titre", "Haut de page", "Titre", "Contenus", 50);
    public static readonly DefinitionTexte Intro = Page.Paragraphe("tete.intro", "Haut de page", "Présentation",
        "Mes vidéos, mes formats courts et mes publications Instagram, repris automatiquement dès leur publication.", 300);
    public static readonly DefinitionTexte Lives = Page.Ligne("tete.lives", "Haut de page", "Devant le lien Twitch", "Les lives, eux, se passent sur", 80);
    public static readonly DefinitionTexte LienTwitch = Page.Ligne("tete.lien-twitch", "Haut de page", "Lien Twitch", "Twitch", 40);

    public static readonly DefinitionTexte Indisponible = Page.Paragraphe("indisponible", "Quand rien n'est disponible", "Texte",
        "Les contenus ne sont pas disponibles pour le moment. Ils sont toujours sur mes réseaux :", 300);

    public static readonly DefinitionTexte VideosTitre = Page.Ligne("videos.titre", "Vidéos", "Titre", "Vidéos", 50);
    public static readonly DefinitionTexte VideosLien = Page.Ligne("videos.lien", "Vidéos", "Lien vers la chaîne", "La chaîne YouTube", 50);
    public static readonly DefinitionTexte InstagramTitre = Page.Ligne("instagram.titre", "Instagram", "Titre", "Sur Instagram", 50);
    public static readonly DefinitionTexte InstagramLien = Page.Ligne("instagram.lien", "Instagram", "Lien vers le compte", "Le compte Instagram", 50);
    public static readonly DefinitionTexte CourtsTitre = Page.Ligne("courts.titre", "Formats courts", "Titre", "Formats courts", 50);
}

/// <summary>La page partenaires.</summary>
public static class TextesPartenaires
{
    public static readonly PageDeTextes Page = new("partenaires", "Partenaires", "/partenaires");

    public static readonly DefinitionTexte Onglet = Page.Ligne("onglet", "Google", "Titre de l'onglet et dans Google", "Devenir partenaire de NUR24 | peroche.now", 70);
    public static readonly DefinitionTexte Description = Page.Paragraphe("description", "Google", "Description dans Google",
        "Embarquer dès le départ : devenez partenaire de NUR24, trois saisons jusqu'aux 24 Heures du Nürburgring. Partenariats gérés par Trinity Management.", 300);

    public static readonly DefinitionTexte Titre = Page.Ligne("tete.titre", "Haut de page", "Titre", "Embarquer dès le départ.", 80);
    public static readonly DefinitionTexte Texte = Page.Paragraphe("tete.texte", "Haut de page", "Texte",
        "Je n'ai pas encore de palmarès. J'ai une trajectoire, une communauté qui la suit et trois saisons devant moi jusqu'aux 24 Heures du Nürburgring. NUR24 cherche de vrais partenaires commerciaux, pas du mécénat : les marques qui montent maintenant font partie de l'histoire.");
    public static readonly DefinitionTexte BoutonDossier = Page.Ligne("bouton-dossier", "Haut de page", "Bouton du dossier partenaire", "Télécharger le dossier partenaire", 50);
    public static readonly DefinitionTexte LienContact = Page.Ligne("lien-contact", "Haut de page", "Lien pour écrire", "Écrire à Trinity Management", 50);

    public static readonly DefinitionTexte PourquoiTitre = Page.Ligne("pourquoi.titre", "Pourquoi maintenant", "Titre", "Pourquoi maintenant", 50);
    public static readonly DefinitionTexte Argument1Titre = Page.Ligne("pourquoi.1.titre", "Pourquoi maintenant", "Premier argument : titre", "Une trajectoire, pas un palmarès", 80);
    public static readonly DefinitionTexte Argument1Texte = Page.Paragraphe("pourquoi.1.texte", "Pourquoi maintenant", "Premier argument : texte",
        "Karting de location, simulation, Tourist Drives, puis la Nordschleife. Un parcours que chacun peut suivre, et dans lequel on peut se reconnaître.", 400);
    public static readonly DefinitionTexte Argument1Lien = Page.Ligne("pourquoi.1.lien", "Pourquoi maintenant", "Premier argument : lien", "D'où je pars", 50);
    public static readonly DefinitionTexte Argument2Titre = Page.Ligne("pourquoi.2.titre", "Pourquoi maintenant", "Deuxième argument : titre", "Trois saisons de visibilité", 80);
    public static readonly DefinitionTexte Argument2Texte = Page.Paragraphe("pourquoi.2.texte", "Pourquoi maintenant", "Deuxième argument : texte",
        "2027 en RCN, 2028 en RCN et NLS, 2029 jusqu'aux 24 Heures du Nürburgring. Chaque saison est une nouvelle étape, racontée de l'intérieur.", 400);
    public static readonly DefinitionTexte Argument2Lien = Page.Ligne("pourquoi.2.lien", "Pourquoi maintenant", "Deuxième argument : lien", "La route jusqu'en 2029", 50);
    public static readonly DefinitionTexte Argument3Titre = Page.Ligne("pourquoi.3.titre", "Pourquoi maintenant", "Troisième argument : titre", "Une communauté qui suit chaque étape", 80);
    public static readonly DefinitionTexte Argument3Texte = Page.Paragraphe("pourquoi.3.texte", "Pourquoi maintenant", "Troisième argument : texte",
        "Au moins deux lives Twitch par semaine, des clips chaque jour sur toutes les plateformes et un Reel hebdomadaire sur l'avancement du projet.", 400);
    public static readonly DefinitionTexte Argument3Lien = Page.Ligne("pourquoi.3.lien", "Pourquoi maintenant", "Troisième argument : lien", "Voir les contenus", 50);

    public static readonly DefinitionTexte VisibiliteTitre = Page.Ligne("visibilite.titre", "Où votre marque apparaît", "Titre", "Où votre marque apparaît", 60);
    public static readonly DefinitionTexte Visibilite1Titre = Page.Ligne("visibilite.1.titre", "Où votre marque apparaît", "Premier emplacement : titre", "Sur la voiture et la combinaison", 80);
    public static readonly DefinitionTexte Visibilite1Texte = Page.Ligne("visibilite.1.texte", "Où votre marque apparaît", "Premier emplacement : texte", "En course, en roulage et sur chaque photo du projet.", 200);
    public static readonly DefinitionTexte Visibilite2Titre = Page.Ligne("visibilite.2.titre", "Où votre marque apparaît", "Deuxième emplacement : titre", "Dans les lives et les contenus", 80);
    public static readonly DefinitionTexte Visibilite2Texte = Page.Ligne("visibilite.2.texte", "Où votre marque apparaît", "Deuxième emplacement : texte", "Sur l'habillage des lives Twitch et dans les vidéos et Reels du projet.", 200);
    public static readonly DefinitionTexte Visibilite3Titre = Page.Ligne("visibilite.3.titre", "Où votre marque apparaît", "Troisième emplacement : titre", "Ici, sur peroche.now", 80);
    public static readonly DefinitionTexte Visibilite3Texte = Page.Ligne("visibilite.3.texte", "Où votre marque apparaît", "Troisième emplacement : texte", "Parmi les partenaires du projet en cours.", 200);
    public static readonly DefinitionTexte VisibiliteNote = Page.Paragraphe("visibilite.note", "Où votre marque apparaît", "Note sous les emplacements",
        "Bientôt, un configurateur vous montrera votre logo sur la livrée, en temps réel.", 300);
    public static readonly DefinitionTexte VisibiliteNoteDossier = Page.Ligne("visibilite.note-dossier", "Où votre marque apparaît", "Ajout quand le dossier est en ligne",
        "Les formules détaillées sont dans le dossier partenaire.", 200);

    public static readonly DefinitionTexte ContactTitre = Page.Ligne("contact.titre", "Contact", "Titre", "Parlons-en.", 60);
    public static readonly DefinitionTexte ContactTexte = Page.Paragraphe("contact.texte", "Contact", "Texte",
        "Les partenariats sont gérés par Trinity Management, qui vous répond et prépare la proposition avec vous.", 400);
    public static readonly DefinitionTexte ContactAttente = Page.Ligne("contact.attente", "Contact", "Tant qu'il n'y a pas d'adresse de contact",
        "L'adresse de contact sera publiée ici très bientôt.", 200);
}

/// <summary>La page de liens, à mettre en bio des réseaux.</summary>
public static class TextesLiens
{
    public static readonly PageDeTextes Page = new("liens", "Page de liens", "/liens");

    public static readonly DefinitionTexte Onglet = Page.Ligne("onglet", "Google", "Titre de l'onglet et dans Google", "Alexandre Peroche, tous mes liens | peroche.now", 70);
    public static readonly DefinitionTexte Description = Page.Paragraphe("description", "Google", "Description dans Google",
        "Tous les liens d'Alexandre Peroche : ce qu'il fait en ce moment, ses réseaux, le projet NUR24 jusqu'aux 24 Heures du Nürburgring.", 300);

    public static readonly DefinitionTexte Nom = Page.Ligne("nom", "Haut de page", "Nom", "Alexandre Peroche", 60);
    public static readonly DefinitionTexte Devise = Page.Ligne("devise", "Haut de page", "Devise", "Essayer, c'est déjà réussir.", 100);

    public static readonly DefinitionTexte CarteEtiquette = Page.Ligne("carte.etiquette", "Carte « En ce moment »", "Étiquette", "En ce moment", 40);
    public static readonly DefinitionTexte CarteObjectif = Page.Ligne("carte.objectif", "Carte « En ce moment »", "Au-dessus du prochain objectif", "Prochain objectif", 40);
    public static readonly DefinitionTexte CarteVers = Page.Ligne("carte.vers", "Carte « En ce moment »", "Lien vers le site", "Tout suivre sur peroche.now", 60);

    public static readonly DefinitionTexte GroupeReseaux = Page.Ligne("groupe.reseaux", "Boutons", "Titre des réseaux", "Me suivre partout", 60);
    public static readonly DefinitionTexte GroupeProjet = Page.Ligne("groupe.projet", "Boutons", "Titre du projet", "Le projet NUR24", 60);
    public static readonly DefinitionTexte BoutonProjet = Page.Ligne("bouton.projet", "Boutons", "Bouton du projet", "De la Test Week aux 24 Heures", 60);
    public static readonly DefinitionTexte BoutonPartenaire = Page.Ligne("bouton.partenaire", "Boutons", "Bouton partenaire", "Devenir partenaire", 60);
}

/// <summary>Toutes les pages de textes, dans l'ordre de l'administration.</summary>
public static class CatalogueDesTextes
{
    public static IReadOnlyList<PageDeTextes> Pages { get; } =
    [
        TextesAccueil.Page,
        TextesNur24.Page,
        TextesContenus.Page,
        TextesPartenaires.Page,
        TextesLiens.Page,
        TextesCommuns.Page,
    ];

    public static PageDeTextes? Page(string? code) =>
        Pages.FirstOrDefault(page => string.Equals(page.Code, code, StringComparison.Ordinal));
}
