// Garde la ligne « Samedi 17 octobre, 13 h 32 en Bretagne. » à l'heure de Paris.
// Le serveur l'écrit déjà au rendu : ce script ne fait que la tenir à jour si la page reste ouverte.
(() => {
  const cible = document.querySelector("[data-heure-paris]");
  if (!cible || !window.Intl) return;

  const suite = cible.dataset.suite || "";
  const fuseau = { timeZone: "Europe/Paris" };

  const mettreAJour = () => {
    try {
      const maintenant = new Date();
      const jour = maintenant.toLocaleDateString("fr-FR", { ...fuseau, weekday: "long", day: "numeric", month: "long" });
      const heure = maintenant.toLocaleTimeString("fr-FR", { ...fuseau, hour: "2-digit", minute: "2-digit" }).replace(":", " h ");
      cible.textContent = `${jour.charAt(0).toUpperCase()}${jour.slice(1)}, ${heure}${suite}`;
    } catch {
      // Le texte rendu par le serveur reste affiché.
    }
  };

  mettreAJour();
  setInterval(mettreAJour, 15000);
})();
