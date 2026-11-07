// Circuits interactifs : un secteur (ou son étape dans la liste) ouvre sa bulle.
// Souris : la bulle suit le survol. Toucher ou clic : la bulle reste ouverte jusqu'au clic suivant.
// Clavier : Tab ouvre la bulle du secteur atteint, Échap la ferme.
(() => {
  document.querySelectorAll("[data-circuit]").forEach((circuit) => {
    const secteurs = [...circuit.querySelectorAll(".secteur")];
    const etapes = [...circuit.querySelectorAll(".circuit__etape")];
    const bulles = [...circuit.querySelectorAll(".bulle")];
    let ouverte = -1;
    let epinglee = false;
    let minuterie = 0;

    // La position de chaque bulle vient du serveur, en pourcentage du dessin.
    bulles.forEach((bulle) => {
      bulle.style.setProperty("--x", `${bulle.dataset.x}%`);
      bulle.style.setProperty("--y", `${bulle.dataset.y}%`);
    });

    const marquer = (index, actif) => {
      [secteurs[index], etapes[index]].forEach((element) => {
        if (!element) return;
        element.classList.toggle("est-actif", actif);
        element.setAttribute("aria-expanded", String(actif));
      });
    };

    const fermer = () => {
      clearTimeout(minuterie);
      if (ouverte < 0) return;
      bulles[ouverte].hidden = true;
      marquer(ouverte, false);
      circuit.classList.remove("circuit--ouvert");
      ouverte = -1;
      epinglee = false;
    };

    // Une bulle qui déborderait de l'écran s'ouvre de l'autre côté du secteur.
    const recadrer = (bulle) => {
      if (getComputedStyle(bulle).position !== "absolute") return;
      const marge = 12;
      const rect = bulle.getBoundingClientRect();
      if (rect.right > window.innerWidth - marge && bulle.classList.contains("bulle--droite")) {
        bulle.classList.replace("bulle--droite", "bulle--gauche");
      } else if (rect.left < marge && bulle.classList.contains("bulle--gauche")) {
        bulle.classList.replace("bulle--gauche", "bulle--droite");
      }
    };

    // Une photo en portrait ou carrée se place à côté du texte plutôt qu'au-dessus : jamais recadrée.
    bulles.forEach((bulle) => {
      const photo = bulle.querySelector(".bulle__photo");
      if (!photo) return;
      const disposer = () => {
        if (!photo.naturalWidth) return;
        bulle.classList.toggle("bulle--photo-cote", photo.naturalWidth / photo.naturalHeight <= 1.25);
        if (!bulle.hidden) recadrer(bulle);
      };
      if (photo.complete) disposer();
      else photo.addEventListener("load", disposer, { once: true });
    });

    const ouvrir = (index, epingler) => {
      clearTimeout(minuterie);
      if (ouverte !== index) {
        fermer();
        bulles[index].hidden = false;
        recadrer(bulles[index]);
        marquer(index, true);
        circuit.classList.add("circuit--ouvert");
        ouverte = index;
      }
      epinglee = epingler;
    };

    const basculer = (index) => {
      if (ouverte === index && epinglee) fermer();
      else ouvrir(index, true);
    };

    const fermerBientot = () => {
      if (epinglee) return;
      clearTimeout(minuterie);
      minuterie = setTimeout(fermer, 220);
    };

    const brancher = (element, index) => {
      element.addEventListener("pointerenter", (e) => {
        if (e.pointerType === "mouse" && !epinglee) ouvrir(index, false);
      });
      element.addEventListener("pointerleave", (e) => {
        if (e.pointerType === "mouse") fermerBientot();
      });
      element.addEventListener("click", () => basculer(index));
    };

    secteurs.forEach((secteur, index) => {
      brancher(secteur, index);
      secteur.addEventListener("focus", () => {
        if (ouverte !== index) ouvrir(index, false);
      });
      secteur.addEventListener("keydown", (e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          basculer(index);
        }
      });
    });
    etapes.forEach((etape, index) => brancher(etape, index));

    // La souris peut passer du secteur à sa bulle sans la refermer.
    bulles.forEach((bulle) => {
      bulle.addEventListener("pointerenter", () => clearTimeout(minuterie));
      bulle.addEventListener("pointerleave", (e) => {
        if (e.pointerType === "mouse") fermerBientot();
      });
    });

    circuit.addEventListener("focusout", (e) => {
      if (!circuit.contains(e.relatedTarget) && !epinglee) fermer();
    });
    document.addEventListener("click", (e) => {
      if (!circuit.contains(e.target)) fermer();
    });
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape") fermer();
    });
  });
})();
