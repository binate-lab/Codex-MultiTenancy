---
name: mudblazor-grilles-champs-css
description: >
  Styliser les GRILLES (MudTable) et les CHAMPS (MudTextField/MudSelect/MudNumericField)
  du front Blazor WASM MudBlazor (TrajanEcoleApp / ABCSchool) sans retomber dans les pièges
  CSS récurrents : hauteur de ligne d'une grille, compacter/rapetisser un champ ou un filtre,
  taille de police/largeur d'un champ, traduire ou rétrécir un MudTablePager. Déclenche ce
  skill dès qu'on touche à l'APPARENCE d'une grille ou d'un champ MudBlazor sur ce front —
  et SURTOUT dès que « ma règle CSS ne s'applique pas », « le champ reste trop grand /
  à 22px », « la largeur ne change pas », « aligner les lignes sur X px », « mettre les
  filtres sur une ligne », même sans citer « MudBlazor », « scopé » ou « ::deep ». Ce skill
  encode POURQUOI le CSS scopé Blazor échoue sur les composants MudBlazor et OÙ mettre la
  règle pour qu'elle prenne. N'utilise PAS ce skill pour : de la logique métier (filtrage,
  calcul) ; styliser un élément HTML pur hors MudBlazor ; changer le thème global via le
  MudThemeProvider ; un autre projet que TrajanEcoleApp/ABCSchool.
---

# Styliser grilles & champs MudBlazor (front ABCSchool) sans se battre avec le CSS

## Quand l'utiliser

Sur le front `TrajanEcoleApp` (repo ABCSchool, Blazor WASM + MudBlazor), dès qu'on ajuste
l'**apparence** d'une `MudTable` (hauteur de ligne, densité) ou d'un champ
`MudTextField` / `MudSelect` / `MudNumericField` (police, largeur, hauteur), ou d'un
`MudTablePager` (langue, taille). C'est un terrain miné : une règle CSS « évidente » ne
s'applique souvent pas, pour des raisons **structurelles** expliquées ici. Lis d'abord ce
skill, tu gagneras des allers-retours.

> La leçon centrale, en une phrase : **le CSS scopé Blazor (`.razor.css` + `::deep`)
> atteint les GRILLES mais PAS la racine des champs MudBlazor. Les champs se pilotent
> depuis le CSS GLOBAL (`wwwroot/index.html`).**

## Pourquoi (le modèle mental qui débloque tout)

Le CSS scopé Blazor ajoute un attribut de scope (`b-xxxxx`) aux éléments **rendus dans le
`.razor` de la page**. Un `<div class="ma-grille">` écrit à la main dans la page porte donc
le scope → `.ma-grille ::deep td { … }` compile en `.ma-grille[b-xxxxx] td` et **fonctionne**.

Mais quand tu écris `<MudTextField Class="mon-champ" />`, la racine du champ est rendue par
le **composant MudBlazor**, pas par ta page. Selon les cas elle ne reçoit pas ton scope →
`.mon-champ[b-xxxxx] input` ne matche rien → **ta règle de police/largeur est ignorée**, en
silence. C'est LE piège n°1 (des heures perdues à croire à un bug de valeur alors que la
règle n'était juste jamais appliquée).

**Corollaire** : pour un CHAMP, mets la règle dans le CSS **global** de
`wwwroot/index.html` (bloc `<style>`), qui n'a pas de scope et cible directement
`.mon-champ input`. Pour une GRILLE, le CSS scopé `::deep` via le `<div>` wrapper suffit.

## Où mettre la règle CSS — table de décision

| Tu veux styliser… | Où | Comment |
|---|---|---|
| Hauteur de ligne / cellules d'une **grille** | `.razor.css` scopé | `<div class="xxx-grid">` autour de la `MudTable` + `.xxx-grid ::deep td { … }` |
| **Police / largeur / hauteur** d'un champ (filtre, saisie) | **`index.html`** global | classe dédiée sur le champ + `.acc-window .xxx input { … !important }` |
| **Fond, couleur** d'une cellule de grille | `.razor.css` scopé | `.xxx-grid ::deep td { … }` (marche) |
| **Traduire / dimensionner** un `MudTablePager` | paramètre + `index.html` | `RowsPerPageString="…"` + `.acc-window .mud-table-pagination { … }` |

Règle simple : **grille → scopé ; champ → global**. En cas de doute, le global `index.html`
marche toujours (c'est le refuge sûr), au prix d'un `!important` + une spécificité suffisante.

## Le piège `.acc-window` (police à 22px)

Les pages « façon Access » sont enveloppées d'un `<div class="acc-window">`. Une règle
GLOBALE dans `index.html` y force **tous** les champs à 22px :

```css
.acc-window .mud-input-slot,
.acc-window .mud-select-input,
.acc-window input,
.acc-window textarea { font-size: 22px !important; }
```

Conséquence : si ton champ « reste trop grand » malgré un `!important`, c'est cette règle
qui gagne (spécificité égale, mais définie **après** la tienne). **Il faut la battre en
spécificité** en préfixant `.acc-window` :

```css
/* PERD (spécificité 0,2,0 = même que .acc-window input, définie avant) */
.acc-ffield input { font-size: 16px !important; }
/* GAGNE (spécificité 0,3,0) */
.acc-window .acc-ffield input { font-size: 16px !important; }
```

Sur les pages **hors** `.acc-window` (ex. Échéancier, Structures, Nature versement), ce
piège n'existe pas : les champs sont à leur taille MudBlazor normale, le scopé suffit.

## Recette 1 — Lignes d'une grille à 26px (le standard du projet)

C'est le patron de référence, calqué sur la grille **Modalités de versement**
(`Pages/Economat/Echeancier.razor` : `.ech-grid`).

1. Enveloppe la `MudTable` d'un `<div class="xxx-grid">` (indispensable : c'est lui qui
   porte le scope, pas la MudTable). Mets la table en `Dense="true"`.
2. Dans le `.razor.css` de la page :

```css
.xxx-grid ::deep td {
    padding: 0 8px !important;   /* padding vertical NUL = clé de la compacité */
    height: 26px !important;
}
.xxx-grid ::deep td .mud-input-slot { min-height: 20px !important; }
```

Références qui font déjà exactement ça : `Echeancier.razor.css` (`.ech-grid`),
`Structures.razor.css` (`.str-grid`, couvre Cycle/Niveau/Classe d'un coup),
`NaturesVersement.razor.css` (`.nat-grid`).

**Cas particulier — grille dans `.acc-window`** (ex. liste élèves de `/scolarites`) : les
champs des cellules sont happés à 22px, ils débordent des 26px. Il faut les compacter en
**global** (`index.html`), via une classe dédiée sur le `<div>` wrapper pour ne pas toucher
les autres grilles qui partagent la même classe générique :

```css
.acc-window .acc-grid-eleves td { height: 26px !important; padding-top:0!important; padding-bottom:0!important; }
.acc-window .acc-grid-eleves td input,
.acc-window .acc-grid-eleves td .mud-input-slot,
.acc-window .acc-grid-eleves td .mud-select-input {
    font-size: 14px !important; min-height: 20px !important;
    padding-top: 0 !important; padding-bottom: 0 !important;
}
```

## Recette 2 — Rapetisser / compacter un champ (filtre ou saisie)

Un `MudTextField`/`MudSelect` dont il faut réduire police / hauteur / largeur.

1. Donne-lui une **classe dédiée** (ne te fie pas au scopé pour ça).
2. Dans `index.html` (bloc `<style>`), cible l'`input` directement, en `!important`, et
   **préfixe `.acc-window`** si la page est en look Access :

```css
/* Police + hauteur (px absolu ; le rem est calculé sur une base parfois gonflée) */
.acc-window .mon-champ input,
.acc-window .mon-champ .mud-input-slot,
.acc-window .mon-champ .mud-select-input {
    font-size: 15px !important; line-height: 1.1 !important;
    padding-top: 0 !important; padding-bottom: 0 !important;
}
.acc-window .mon-champ input { height: 30px !important; }
.acc-window .mon-champ .mud-input.mud-input-outlined,
.acc-window .mon-champ .mud-input-control-input-container { min-height: 30px !important; }

/* Largeur : sur la RACINE du champ (min+width+max pour forcer face à un min-width MudBlazor) */
.acc-window .mon-champ { width: 130px !important; min-width: 130px !important; max-width: 130px !important; }
```

Notes tirées de l'expérience :
- Une **classe commune** (ex. `.acc-ffield` sur tous les filtres) permet d'appliquer
  police+hauteur à tout un groupe ; les largeurs spécifiques (par classe dédiée) doivent
  être définies **après** la règle générale pour l'emporter (spécificité égale → l'ordre
  décide).
- Le bouton « effacer » (`Clearable`) et l'unité `rem` sont des faux-amis : préfère des
  **px absolus** et vise l'`input`, le `.mud-input-slot` ET le `.mud-select-input`.
- Réf : les filtres de `Pages/Scolarites/Scolarites.razor` + le bloc `<style>` de
  `wwwroot/index.html` (chercher `.acc-fmatricule`, `.acc-fnumordre`, `.acc-ffield`).

## Recette 3 — MudTablePager (traduction + taille)

- **Traduire** : paramètres sur `<MudTablePager>` — `RowsPerPageString="Lignes par page"`
  (« Rows per page »). (`InfoFormat` pour le « 1-5 of 34 » si besoin.)
- **Taille / hauteur** (le sélecteur « 5 » est happé à 22px dans `.acc-window`) — global :

```css
.acc-window .mud-table-pagination input,
.acc-window .mud-table-pagination .mud-select-input { font-size: 14px !important; }
.acc-window .mud-table-pagination,
.acc-window .mud-table-pagination .mud-toolbar { min-height: 40px !important; height: 40px !important; }
```

## Cadence de rechargement (sinon tu testes dans le vide)

Le piège qui fait croire qu'« aucune règle ne marche » : le mauvais rechargement.

| Ce que tu as changé | Ce qu'il faut faire |
|---|---|
| `wwwroot/index.html` (CSS global) | **Ctrl+F5** (hard refresh) — index.html est resservi frais |
| Markup `.razor` ou **CSS scopé** `.razor.css` | **Rebuild de l'app** dans VS — le bundle scopé (`TrajanEcoleApp.styles.css`) ne se régénère pas toujours au simple hot-reload |
| Projection / DTO **backend** (Scolarite.Api) | **Rebuild du conteneur** : `build-local.ps1 scolarite` (fait le `docker compose up`) |

VS verrouille souvent les DLL (`MSB3027`/`MSB3021` au `dotnet build`) tant que le front
tourne : ce n'est pas une erreur de code, juste le rebuild à faire depuis VS.

## Diagnostic DevTools (tranche les débats en 30 s)

Quand une taille est « bizarre » sans raison, **compare le `font-size` computed de deux
éléments voisins** dans DevTools (F12 → Computed). Exemple réel : le « 5 » du pager à 22px
alors que « 1-5 of 34 » juste à côté était à 14px → ça a pointé la règle `.acc-window`
qui gonflait tous les *champs* (pas le texte). Regarde aussi si ta règle apparaît dans
l'onglet **Styles** (absente = pas chargée/scope raté ; barrée = battue en spécificité).

## Fichiers de référence (à lire pour voir le pattern en vrai)

- `Pages/Economat/Echeancier.razor` + `.razor.css` — **la grille 26px canonique** (`.ech-grid`).
- `Pages/Structures/Structures.razor.css` — `.str-grid` (3 grilles d'un coup).
- `Pages/Economat/NaturesVersement.razor(.css)` — wrapper `.nat-grid` ajouté après coup.
- `Pages/Scolarites/Scolarites.razor(.cs/.css)` — filtres, grille élèves dans `.acc-window`, pager.
- `wwwroot/index.html` (bloc `<style>`) — **toutes les règles globales** de champs/filtres/pager
  et la règle `.acc-window … 22px` à connaître.
