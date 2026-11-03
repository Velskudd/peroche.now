// Vidéo de fond de l'accueil. Sans ce script, l'image fixe (poster) reste affichée.
// La vidéo n'est chargée que si le visiteur n'a demandé ni moins d'animations, ni d'économiser ses données.
// Un bouton permet de la mettre en pause (une animation de plus de 5 secondes doit pouvoir s'arrêter).
(() => {
  const video = document.querySelector(".ouverture__video");
  const bouton = document.querySelector(".ouverture__pause");
  if (!video || !video.dataset.src) return;

  const moinsDAnimations = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const economieDeDonnees = navigator.connection && navigator.connection.saveData;
  if (moinsDAnimations || economieDeDonnees) return;

  video.src = video.dataset.src;
  video.play().then(() => {
    video.classList.add("ouverture__video--lecture");
    if (!bouton) return;
    bouton.hidden = false;
    bouton.addEventListener("click", () => {
      const enPause = !video.paused;
      if (enPause) video.pause(); else video.play();
      bouton.setAttribute("aria-pressed", String(enPause));
      bouton.textContent = enPause ? "Relancer la vidéo" : "Mettre la vidéo en pause";
    });
  }).catch(() => {
    // Lecture refusée par le navigateur : l'image fixe reste affichée.
  });
})();
