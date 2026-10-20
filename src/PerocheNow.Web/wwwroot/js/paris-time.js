// Garde la ligne « Samedi 17 octobre, 13 h 32 en Bretagne. » à l'heure de Paris.
// Le serveur l'écrit déjà au rendu : ce script ne fait que la tenir à jour si la page reste ouverte.
(() => {
  const target = document.querySelector("[data-paris-time]");
  if (!target || !window.Intl) return;

  const suffix = target.dataset.suffix || "";
  const timeZone = { timeZone: "Europe/Paris" };

  const update = () => {
    try {
      const now = new Date();
      const day = now.toLocaleDateString("fr-FR", { ...timeZone, weekday: "long", day: "numeric", month: "long" });
      const time = now.toLocaleTimeString("fr-FR", { ...timeZone, hour: "2-digit", minute: "2-digit" }).replace(":", " h ");
      target.textContent = `${day.charAt(0).toUpperCase()}${day.slice(1)}, ${time}${suffix}`;
    } catch {
      // Le texte rendu par le serveur reste affiché.
    }
  };

  update();
  setInterval(update, 15000);
})();
