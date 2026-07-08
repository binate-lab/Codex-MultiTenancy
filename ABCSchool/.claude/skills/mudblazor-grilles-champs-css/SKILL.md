---
name: mudblazor-grilles-champs-css
description: >
  Styliser les GRILLES (MudTable) et les CHAMPS (MudTextField/MudSelect/MudNumericField)
  du front Blazor WASM MudBlazor (TrajanEcoleApp / ABCSchool) sans retomber dans les pièges
  CSS récurrents : hauteur de ligne d'une grille, compacter/rapetisser un champ ou un filtre,
  taille de police/largeur d'un champ, traduire ou rétrécir un MudTablePager. Couvre aussi
  les ergonomies de grille « façon tableur » : barre de défilement horizontale, navigation
  clavier Haut/Bas entre lignes, et copier la valeur de la cellule du dessus (Ctrl+' / ditto).
  Déclenche ce skill dès qu'on touche à l'APPARENCE ou à l'ERGONOMIE CLAVIER d'une grille ou
  d'un champ MudBlazor sur ce front — et SURTOUT dès que « ma règle CSS ne s'applique pas »,
  « le champ reste trop grand / à 22px », « la largeur ne change pas », « aligner les lignes
  sur X px », « mettre les filtres sur une ligne », « scroll horizontal de la grille »,
  « me déplacer au clavier entre les lignes », « copier la cellule du dessus / recopier la
  valeur d'au-dessus », même sans citer « MudBlazor », « scopé » ou « ::deep ». Ce skill
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

## Protocole d'intake — « fais-moi une grille »

Quand l'utilisateur demande de **construire une grille** (typiquement : « fais-moi une grille
avec mudblazor-grilles-champs-css »), NE code PAS tout de suite. Cadrer d'abord évite les
allers-retours. Déroulé :

1. **Poser le contrat en 10 points** ci-dessous, en proposant un **défaut sensé** pour chacun
   (indiqués entre parenthèses). L'utilisateur ne corrige que ce qui diffère — il peut dire
   « défauts partout sauf colonnes X/Y ». Ne transforme pas ça en interrogatoire : propose,
   il ajuste.
2. **Renvoyer un récap validé** dans un tableau à 2 colonnes `| Point | Décision |` avant de
   coder.
3. Une fois le tableau validé, **faire le job** (recettes ci-dessous).

**Le contrat (10 points) :**

| # | Point | À préciser (défaut) |
|---|---|---|
| 1 | Source des données | Service/endpoint + **clé unique par ligne** (nécessaire pour Ctrl+' / navigation clavier) |
| 2 | Page hôte | Dans une page `.acc-window` (look Access, champs forcés à 22px) ou non ? → décide CSS global vs scopé |
| 3 | Colonnes | Pour chacune : libellé, largeur, **lecture seule ou éditable** ; si éditable, le **type** (texte / déroulante + source des options / case / numérique) |
| 4 | Style / hauteur | Hauteur de ligne (**défaut 26px**) ; colonnes spéciales (jaune, MAJUSCULES) |
| 5 | Filtres | Colonnes filtrées ; type (texte souple sans accents / déroulante) ; **cascade** éventuelle ; tous sur une ligne ; **bouton EFFACER** (défaut oui) |
| 6 | Pager | Oui/non ; taille de page ; libellé FR (**« Lignes par page »**) |
| 7 | Actions par ligne | **MudMenu 3-points (MoreVert)** ? quelles actions (Modifier / Supprimer / Copier…) |
| 8 | Ergonomies tableur | Navigation clavier Haut/Bas ? Ctrl+' (copie du dessus) ? Scroll horizontal ? (défaut : non, sauf demande) |
| 9 | Maître → détail | Clic sur une ligne → actionne une **grille B** / un panneau ? si oui, colonnes + source de B |
| 10 | Persistance | Éditions de cellule **sauvées en base** (quel endpoint) ou **en mémoire** ? |

Les recettes qui suivent couvrent chacun de ces points ; les défauts (26px, recherche sans
accents, EFFACER, pager FR) sont ceux du projet.

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

## Recette 4 — Barre de défilement horizontale d'une grille

Pour qu'une grille montre une barre de défilement quand ses colonnes dépassent la largeur
(ex. colonnes ajoutées plus tard), cible le **conteneur** de la MudTable en CSS scopé
(c'est une grille → le scopé marche) :

```css
.acc-grid-eleves ::deep .mud-table-container { overflow-x: auto; }
```

## Recette 5 — Grille « façon tableur » : navigation clavier + copie de la cellule du dessus

Deux ergonomies qui vont ensemble sur une grille éditable : **Flèche Haut/Bas** déplace le
focus d'une ligne à l'autre, et **Ctrl+'** recopie la valeur de la cellule juste au-dessus
(comme le « ditto » d'Excel). Réf. réelle : la grille élèves de `Pages/Scolarites/Scolarites.razor`
+ le handler `svtGrilleEleves` dans `wwwroot/index.html`.

**Le principe qui débloque tout** : le JS gère la DÉTECTION clavier (il connaît la structure
`<tr>`/`<td>` du tableau) mais délègue la MODIFICATION à Blazor. Pourquoi ? Parce que copier
une valeur au niveau du DOM ne met **pas** à jour un `MudSelect` (colonnes Statut/Niveau/Classe) :
sa valeur liée vit côté Blazor, pas dans un `<input>`. Il faut donc modifier le **modèle**.
Le pont : chaque cellule éditable porte `data-col` (la colonne) + `data-eleve-id` (la ligne),
le JS lit ces attributs et rappelle une méthode `[JSInvokable]` qui écrit dans le modèle.

**1. Marquer les cellules éditables** (MudTd propage les attributs `data-*` sur le `<td>`) :

```razor
<MudTd DataLabel="Classe" data-col="Classe" data-eleve-id="@context.Id"> … </MudTd>
```

**2. Le pont Blazor** (code-behind) — `DotNetObjectReference` enregistré au 1er rendu, méthode
`[JSInvokable]`, et `IDisposable` pour libérer la ref :

```csharp
public partial class MaPage : IDisposable
{
    private DotNetObjectReference<MaPage>? _dotnetRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetRef = DotNetObjectReference.Create(this);
            await _js.InvokeVoidAsync("svtGrilleEleves.init", _dotnetRef);
        }
    }

    [JSInvokable]
    public async Task CopierCelluleDuHaut(string sourceId, string cibleId, string col)
    {
        // retrouver les 2 lignes par Id dans la source de la grille, copier la propriété
        // correspondant à `col` (réutilise les handlers d'édition existants pour les effets
        // de bord : persistance du tél, cascade niveau→classe…), puis StateHasChanged().
    }

    public void Dispose() => _dotnetRef?.Dispose();
}
```

**3. Le handler JS** (dans `index.html`, en phase capture) — navigation + détection Ctrl+' :

```js
window.svtGrilleEleves = { _ref: null, init: function (r) { this._ref = r; } };
(function () {
    function celluleColonne(tr, col){ return tr ? tr.querySelector('td[data-col="'+col+'"]') : null; }
    function focusable(td){ return td && (td.querySelector('input:not([type=hidden]):not([disabled])')
                                       || td.querySelector('[tabindex]:not([tabindex="-1"])')); }
    document.addEventListener('keydown', function (e) {
        var td = e.target.closest && e.target.closest('td[data-col]');
        if (!td || !td.closest('.acc-grid-eleves')) return;
        var col = td.getAttribute('data-col'), tr = td.closest('tr');
        if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
            if (document.querySelector('.mud-popover-open .mud-list')) return; // déroulante ouverte : natif
            var voisin = e.key === 'ArrowUp' ? tr.previousElementSibling : tr.nextElementSibling;
            var cible = focusable(celluleColonne(voisin, col));
            if (cible) { e.preventDefault(); e.stopPropagation(); cible.focus(); } // stop = MudSelect n'ouvre pas sa liste
            return;
        }
        if (e.ctrlKey && (e.key === "'" || e.code === 'Digit4')) {   // ' = touche 4 de l'AZERTY
            var tdHaut = celluleColonne(tr.previousElementSibling, col);
            if (!tdHaut) return;                                     // 1re ligne : rien au-dessus
            e.preventDefault(); e.stopPropagation();
            window.svtGrilleEleves._ref && window.svtGrilleEleves._ref
                .invokeMethodAsync('CopierCelluleDuHaut', tdHaut.getAttribute('data-eleve-id'),
                                   td.getAttribute('data-eleve-id'), col).catch(function(){});
        }
    }, true);
})();
```

Points d'attention (tirés du vécu) :
- **`e.stopPropagation()`** dans la branche flèches, sinon le `MudSelect` ouvre sa liste sur
  la même touche. Et **garde `.mud-popover-open .mud-list`** : si une déroulante est déjà
  ouverte, on rend les flèches au comportement natif (choisir une option).
- **Focus des cellules déroulantes** : le vrai `<input>` d'un MudSelect est `type=hidden` →
  vise `[tabindex]` en repli (le div focusable de l'activateur).
- **Bords** : 1re ligne, ou ligne du dessus sur une autre page du pager (non rendue) → pas de
  copie. La détection `previousElementSibling` reste dans le `<tbody>` visible.
- Le handler est **global** (une seule fois dans `index.html`) mais borné à `.acc-grid-eleves`
  (no-op ailleurs) ; adapte ce sélecteur à ta grille.

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
- `Pages/Scolarites/Scolarites.razor(.cs/.css)` — filtres, grille élèves dans `.acc-window`,
  pager, cellules `data-col`/`data-eleve-id`, pont `[JSInvokable] CopierCelluleDuHaut`.
- `wwwroot/index.html` — dans le `<style>` : **toutes les règles globales** de champs/filtres/pager
  et la règle `.acc-window … 22px` ; dans le `<script>` : le handler clavier `svtGrilleEleves`
  (navigation Haut/Bas + Ctrl+').
