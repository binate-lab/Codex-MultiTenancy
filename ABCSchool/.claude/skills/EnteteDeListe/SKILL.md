---
name: EnteteDeListe
description: >
  En-tête OFFICIEL des documents IMPRIMÉS du front TrajanEcoleApp / ABCSchool (Blazor WASM +
  MudBlazor) : la feuille A4 avec « RÉPUBLIQUE DE CÔTE D'IVOIRE » + « MENA-DREN : {Ville} » à
  gauche, logo au centre, école + année scolaire à droite, puis une ligne « titre + stats » et
  une date d'édition en pied. Déclenche ce skill dès qu'on construit ou retouche un APERÇU /
  une IMPRESSION d'un document scolaire (liste de classe, liste d'élèves, fiche, bulletin,
  reçu, procès-verbal, état…) et qu'il faut l'en-tête administratif ivoirien — même sans dire
  « en-tête » : « aperçu avant impression », « feuille A4 à imprimer », « mets l'en-tête
  officiel », « République de Côte d'Ivoire / MENA-DREN », « regrouper école + année en haut »,
  « imprimer proprement sans la grille de travail », « choisir / changer le modèle de liste »,
  « plusieurs modèles d'impression », « cahier d'appel / liste de présence », « fiche de notes »,
  « trombinoscope », « imprimer en paysage ». Le skill encode le MARKUP exact, le CSS de
  la feuille A4, le MÉCANISME d'impression (isoler la feuille via une classe body + @media
  print global), le sélecteur multi-modèles (corps variable, orientation portrait/paysage) et
  le formatage des libellés de classe collège (6e1 → 6è 1). N'utilise PAS ce
  skill pour : styliser une grille/champ MudBlazor à l'écran (→ mudblazor-grilles-champs-css) ;
  la logique métier (filtrage, calculs, totaux) ; un export PDF/Excel côté serveur ; un autre
  projet que TrajanEcoleApp/ABCSchool.
---

# En-tête officiel des documents imprimés (front ABCSchool)

## Quand l'utiliser

Sur le front `TrajanEcoleApp` (repo ABCSchool, Blazor WASM), dès qu'on produit un **document
imprimable** destiné à l'administration scolaire ivoirienne : liste de classe, liste d'élèves,
bulletin, reçu, PV, état de présence… Ces documents partagent le **même en-tête officiel** et
le **même mécanisme d'impression**. Ce skill capture le modèle validé par Keita pour la page
`Pages/ListesClasse/ListesClasse.razor` afin de le réappliquer sans repartir de zéro.

Référence vivante = la feuille `.lc-feuille` de la page Liste de classe (commits ABCSchool
`b6f68d4` puis `d185fa0`). En cas de doute, va lire ce fichier ; ce skill en est la synthèse
réutilisable.

## Le principe central

> Un document imprimé n'est PAS la page de travail. On construit une **feuille A4 dédiée**
> (HTML pur, aucun composant MudBlazor dedans) et, à l'impression, on **masque tout sauf la
> feuille** via une classe posée sur `<body>` + une règle `@media print` **globale**
> (`wwwroot/index.html`). C'est le seul moyen fiable : `window.print()` brut imprime la grille
> de travail, les filtres et le reste.

Pourquoi HTML pur dans la feuille : le CSS scopé Blazor s'y applique proprement, et surtout
l'impression n'a rien à démêler (pas de portails/overlays MudBlazor). Cf le pendant à l'écran
[[mudblazor-grilles-champs-css]] pour tout ce qui est grille/champ interactif.

## Anatomie de l'en-tête (haut de la feuille)

Trois zones sur **une seule ligne**, alignées en haut (`align-items: flex-start`) :

1. **Gauche** — autorité de tutelle : `RÉPUBLIQUE DE CÔTE D'IVOIRE` puis en dessous
   `MENA-DREN : {Ville}` (la ville **en gras**, en majuscules).
2. **Centre** — logo de l'école (si présent).
3. **Droite** — nom de l'école (majuscules) + `Année scolaire : {annee}`.

Sous l'en-tête, une **ligne titre + stats** : le titre du document à gauche (ex.
`LISTE DE CLASSE : {Classe}`), les statistiques à droite (ex. `Garçons / Filles / Total`),
sur la même ligne (`justify-content: space-between`).

En **pied de liste** : `Édité le : {date}` (aligné à droite).

## Données à câbler (source = école active)

L'école active vient de `_schoolService.GetMineAsync()` filtré sur le claim `school`
(= `CodeEts`). `SchoolResponse` (dans `TrajanEcole.Shared.Library`) expose ce qu'il faut :

- `Name` → nom de l'école (droite)
- `Logo` → logo (centre) — data URL / chemin
- `Ville` → **`MENA-DREN : {Ville}`** (gauche)
- l'année scolaire vient de `_anneeScolaireService.GetAnneeEnCoursAsync()`

Capture ces valeurs dans `OnInitializedAsync` (champs `_nomEcole`, `_logoEcole`,
`_villeEcole`, `_annee`). `RÉPUBLIQUE DE CÔTE D'IVOIRE` et `MENA-DREN` sont du texte fixe ; seule
la **ville** est dynamique.

## Markup de référence (Razor)

À placer dans un overlay d'aperçu (voir plus bas), ou directement dans une feuille dédiée.
Seule la `<div class="lc-feuille">` est conservée à l'impression.

```razor
<div class="lc-feuille">
    <div class="lc-feuille-entete">
        @* Gauche : autorité de tutelle. *@
        <div class="lc-feuille-ministere">
            <div class="lc-feuille-min-titre">RÉPUBLIQUE DE CÔTE D'IVOIRE</div>
            <div class="lc-feuille-dren">MENA-DREN : <b>@_villeEcole</b></div>
        </div>
        @* Centre : logo. *@
        @if (!string.IsNullOrWhiteSpace(_logoEcole))
        {
            <div class="lc-feuille-logo"><img src="@_logoEcole" alt="Logo" /></div>
        }
        @* Droite : école + année. *@
        <div class="lc-feuille-ecole-droite">
            <div class="lc-feuille-nom">@(string.IsNullOrWhiteSpace(_nomEcole) ? "ÉCOLE / ÉTABLISSEMENT" : _nomEcole)</div>
            <div class="lc-feuille-annee">Année scolaire : @_annee</div>
        </div>
    </div>

    @* Ligne titre (à gauche) + stats (à droite). Adapte le titre et les stats au document. *@
    <div class="lc-feuille-barre">
        <div class="lc-feuille-titre">LISTE DE CLASSE@(string.IsNullOrWhiteSpace(_fClasse) ? "" : $" : {FormatClasse(_fClasse)}")</div>
        <div class="lc-feuille-totaux">
            <span>Garçons : <b>@NbGarcons</b></span>
            <span>Filles : <b>@NbFilles</b></span>
            <span>Total : <b>@Total</b></span>
        </div>
    </div>

    @* … corps du document (tableau, etc.) … *@

    <div class="lc-feuille-pied">Édité le : @DateTime.Now.ToString("dd/MM/yyyy")</div>
</div>
```

## CSS de référence (`.razor.css` scopé — c'est de l'HTML pur, le scope fonctionne)

```css
/* Feuille A4 blanche. print-color-adjust:exact garde l'ombrage des en-têtes/lignes paires
   à l'impression. Padding-haut réduit (8mm) = liste remontée. */
.lc-feuille {
    background: #fff; color: #111;
    width: 210mm; max-width: 100%; min-height: 297mm;
    padding: 8mm 12mm 14mm; box-sizing: border-box;
    box-shadow: 0 6px 24px rgba(0,0,0,.35);   /* écran seulement */
    font-size: 11px;
    -webkit-print-color-adjust: exact; print-color-adjust: exact;
}
.lc-feuille-entete {
    display: flex; align-items: flex-start; gap: 14px;
    border-bottom: 2px solid #333; padding-bottom: 8px; margin-bottom: 6px;
}
.lc-feuille-ministere { flex: 1 1 0; min-width: 0; }
.lc-feuille-min-titre { font-weight: 700; font-size: 12px; white-space: nowrap; }
.lc-feuille-dren      { font-size: 11px; color: #444; text-transform: uppercase; }
.lc-feuille-ecole-droite { flex: 1 1 0; min-width: 0; text-align: right; }
.lc-feuille-nom   { font-weight: 700; font-size: 15px; text-transform: uppercase; }
.lc-feuille-annee { font-size: 11px; color: #444; }
.lc-feuille-logo     { flex: 0 0 auto; }
.lc-feuille-logo img { max-height: 60px; max-width: 100px; object-fit: contain; }
.lc-feuille-barre {
    display: flex; align-items: baseline; justify-content: space-between;
    gap: 20px; margin-bottom: 8px;
}
.lc-feuille-titre  { font-weight: 700; font-size: 14px; color: #7a1f1f; letter-spacing: 1px; }
.lc-feuille-totaux { display: flex; gap: 28px; justify-content: flex-end; font-size: 12px; white-space: nowrap; }
.lc-feuille-pied   { margin-top: 10px; text-align: right; font-size: 11px; color: #444; }
```

Note : `white-space: nowrap` sur `.lc-feuille-min-titre` garantit que « RÉPUBLIQUE DE CÔTE
D'IVOIRE » ne se replie pas et reste sur la même ligne que le nom de l'école à droite.

## Mécanisme d'impression (GLOBAL, dans `wwwroot/index.html`)

Deux blocs à ajouter dans `index.html` (à côté de `lcImprimerFiche` / `lc-print-fiche` qui
font déjà pareil pour la Fiche Élève — c'est le patron à copier) :

1. Dans le `<style>` interne, une règle `@media print` qui isole la feuille :

```css
@media print {
    body.lc-print-liste * { visibility: hidden !important; }
    body.lc-print-liste .lc-feuille,
    body.lc-print-liste .lc-feuille * { visibility: visible !important; }
    body.lc-print-liste .lc-feuille {
        position: absolute; left: 0; top: 0; width: 100%;
        min-height: 0; box-shadow: none;
    }
}
```

2. Dans le `<script>`, le helper qui pose la classe le temps du print :

```js
window.lcImprimerListe = function () {
    document.body.classList.add('lc-print-liste');
    window.print();
    document.body.classList.remove('lc-print-liste');
};
```

Côté page, le bouton « Imprimer » appelle `await _js.InvokeVoidAsync("lcImprimerListe")`.
Pour un **nouveau** type de document, clone ce couple avec un nom dédié (ex.
`bulletin` → classe `b-print-bulletin` + helper `bImprimerBulletin`) pour ne pas interférer
avec la liste de classe.

## Aperçu avant impression (modal WYSIWYG)

Keita veut voir le document **avant** d'imprimer. Enveloppe la `.lc-feuille` dans un overlay
plein écran (HTML pur), avec une petite barre d'outils Imprimer / Fermer **hors** de la
feuille (elle sera masquée à l'impression). Clic sur le fond = fermer ;
`@onclick:stopPropagation="true"` sur le panneau pour ne pas fermer en cliquant dedans. Voir
`.lc-preview-overlay` / `.lc-preview-panel` / `.lc-preview-toolbar` dans
`ListesClasse.razor(.css)` pour le gabarit complet.

## Formatage des libellés de classe (collège)

Les classes de **cycle 1 (collège)** stockées « 6e1 / 5e1 / 4e3 / 3e2 » s'impriment
« 6è 1 / 5è 1 / 4è 3 / 3è 2 » (e→è + espace avant la subdivision). Le **2nd cycle** (2nde,
1ere, TleA1, TleD3…) ne doit **pas** être touché. Regex ciblée sur un chiffre de niveau 3–6 :

```csharp
private static string FormatClasse(string classe)
{
    if (string.IsNullOrWhiteSpace(classe)) return classe ?? string.Empty;
    return System.Text.RegularExpressions.Regex
        .Replace(classe.Trim(), @"^([3-6])e(?=\d|$)", "$1è ")
        .Trim();
}
```

## Plusieurs modèles sur une même feuille (corps variable)

Une même page d'aperçu peut proposer **plusieurs modèles** de document : seuls le **corps** et
le **titre** (et parfois l'**orientation**) changent ; l'en-tête officiel, les stats G/F/Total
et le mécanisme d'impression restent communs. Choix du modèle = une déroulante dans la barre
d'outils de l'aperçu (hors feuille, donc non imprimée). Réf : page Liste de classe, enum
`ModeleListe { Classe, Affectes, Appel, Notes, Trombinoscope }`.

Modèles livrés (à réutiliser / imiter) :
- **Liste de classe** / **Liste des affectés** — tableau état civil (N° · Matricule · Nom &
  Prénoms · Sexe · Date · Lieu · Nationalité). « Affectés » = même corps mais source filtrée
  sur `Statut == "Aff"` en plus des filtres de la page.
- **Liste d'appel** = **LISTE DE PRÉSENCE**, **paysage** : cahier hebdomadaire N° · Matricule ·
  Nom & Prénoms + `JoursAppel` × `CreneauxAppel` (5 jours Lun→Ven × 9 créneaux 7à8…17à18),
  cases vides à cocher, + bande « SEMAINE DU : …/…/… AU : …/…/… » et « Prof. Principal : … »
  (lignes à remplir à la main, on n'a pas ces données). Créneaux rendus verticalement
  (`cr.Replace(" ", "<br>")` en `MarkupString`), colonnes `table-layout: fixed` très étroites.
- **Fiche de notes** — N° · Matricule · Nom & Prénoms · Sexe + 7 colonnes vides (`c-vide`).
- **Trombinoscope**, **paysage** — grille de cartes (`.lc-tromb`) photo (ratio 35×45) + Nom &
  Prénoms + Matricule ; `break-inside: avoid` sur les cartes.

Règles clés :
- **Tous les corps itèrent la MÊME source filtrée** (`ElevesImpression` = `Filtered` trié), pour
  que chaque modèle respecte la barre de filtres et que les stats soient cohérentes.
- Matricule présent sur appel + notes pour **distinguer les homonymes** ; Matricule/Nom
  resserrés (`font-size`, `width`) sur ces modèles larges pour laisser la place aux colonnes.

### Orientation portrait / paysage

Les modèles larges (appel, trombinoscope) s'impriment en **paysage**. Deux leviers :
1. Écran : classe modificatrice sur la feuille et le panneau — `.lc-feuille--paysage { width:
   297mm; min-height: 210mm; }`, `.lc-preview-panel--paysage { width: 297mm; }`.
2. Impression : le helper JS bascule `@page` en paysage le temps du print, via une balise
   `<style>` injectée puis retirée (car `@page` ne se scope pas à une classe body) :

```js
window.lcImprimerListe = function (paysage) {
    document.body.classList.add('lc-print-liste');
    var orient = null;
    if (paysage) {
        orient = document.createElement('style');
        orient.textContent = '@page { size: A4 landscape; margin: 8mm; }';
        document.head.appendChild(orient);
    }
    window.print();
    if (orient) { orient.remove(); }
    document.body.classList.remove('lc-print-liste');
};
```

Côté page : `await _js.InvokeVoidAsync("lcImprimerListe", ModeleEstPaysage);`.

## Pièges à éviter

- **Ne pas** faire `window.print()` sur la page de travail : ça imprime la grille rouge, les
  filtres et l'overlay. Toujours passer par la feuille A4 isolée + classe body.
- **Ne pas** mettre de composants MudBlazor dans `.lc-feuille` : ils cassent l'isolation
  d'impression (portails) et le rendu papier. HTML pur + `<table>` simple.
- Les fonds/ombrages ne s'impriment que si `print-color-adjust: exact` est posé sur la feuille.
- `box-shadow` de la feuille = décor d'écran ; il est neutralisé dans la règle `@media print`.
- Textes fixes vs dynamiques : `RÉPUBLIQUE DE CÔTE D'IVOIRE` et `MENA-DREN` sont fixes ; la
  **ville** vient de `SchoolResponse.Ville`. Si un jour la DREN doit différer de la ville de
  l'école, il faudra une vraie source (champ dédié), pas `Ville`.
