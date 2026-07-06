---
name: mudtable-row-copy-paste
description: >
  Ajoute Copier/Coller d'une ligne dans une grille MudTable (front Blazor WASM MudBlazor
  de TrajanEcoleApp/ABCSchool) : on copie les valeurs éditables d'une ligne et on les colle
  sur une autre, avec persistance backend. Utilise ce skill dès que l'utilisateur veut
  dupliquer/recopier les valeurs d'une ligne vers une autre dans une grille, ajouter
  « Copier »/« Coller » à un menu Actions, ou propager un même jeu de montants/valeurs à
  plusieurs lignes (ex. deux niveaux qui partagent le même barème), même s'il ne dit pas
  explicitement « MudTable » ou « presse-papier ».
---

# Copier / Coller une ligne dans une grille MudTable

## Quand l'utiliser

Une grille `MudTable` de ce projet a des lignes éditables (plusieurs `MudNumericField`/
`MudTextField` par ligne) et une colonne **Actions** avec un `MudMenu`. L'utilisateur veut
recopier d'un coup toutes les valeurs éditables d'une ligne vers une autre — typiquement
quand plusieurs lignes doivent partager les mêmes valeurs (barèmes identiques pour deux
niveaux, etc.). Ce skill ajoute **Copier** et **Coller** au menu, à côté de **Supprimer**.

Implémentation de référence (à lire pour voir le pattern en vrai) :
`ABCSchoolApp/TrajanEcoleApp/Pages/Economat/Echeancier.razor` (+ `.razor.cs`).

## Le principe

- Un **presse-papier** vit dans le code-behind de la page (pas dans la ligne) : un tableau
  (ou objet immuable) des valeurs copiées. `null` = rien de copié.
- Le **ViewModel de ligne** expose deux méthodes symétriques : lire ses cellules éditables
  dans un ordre fixe, et les réécrire depuis un tableau du même ordre.
- **Copier** remplit le presse-papier depuis une ligne (copie indépendante, jamais une
  référence à la ligne source). **Coller** applique le presse-papier à la ligne cible et
  **persiste en base** via le même service que la saisie normale.
- **Coller est désactivé tant que le presse-papier est vide** (`_presse is null`) : sans ça,
  l'utilisateur pourrait « coller du néant ». Il s'active dès le premier Copier.

## Conventions du codebase à respecter

- Termes métier en **français** (`Copier`, `Coller`, `Presse`…), glue technique en anglais.
- Retour utilisateur via `_snackbar.Add(..., Severity.X)` (déjà injecté dans les pages).
- Persistance : réutilise le **service d'update existant** de la page (celui appelé au blur),
  ne réinvente pas d'appel HTTP. Vérifie le résultat avec le helper `Verifier(...)` s'il existe.
- Si la ligne a un **snapshot** pour le dirty-tracking (`FigerSnapshot`/`Restaurer`), fige-le
  après un collage réussi et restaure-le si le backend refuse — exactement comme le blur.

## Recette

### 1. Menu Actions (`.razor`)

Ajoute les deux items dans le `MudMenu` existant. `Coller` porte `Disabled` lié au
presse-papier ; garde `Supprimer` en dernier (action destructrice en bas).

```razor
<MudMenu Icon="@Icons.Material.Filled.MoreVert" Size="Size.Small" Color="Color.Secondary"
         TransformOrigin="Origin.BottomLeft" AnchorOrigin="Origin.BottomLeft">
    <MudMenuItem Icon="@Icons.Material.Filled.ContentCopy"
                 OnClick="@(() => Copier(context))">Copier</MudMenuItem>
    @* Coller reste desactive tant qu'aucune ligne n'a ete copiee. *@
    <MudMenuItem Icon="@Icons.Material.Filled.ContentPaste" Disabled="@(_presse is null)"
                 OnClick="@(() => CollerAsync(context))">Coller</MudMenuItem>
    <MudMenuItem Icon="@Icons.Material.Filled.Delete"
                 OnClick="@(() => SupprimerAsync(context))">Supprimer</MudMenuItem>
</MudMenu>
```

### 2. Presse-papier + handlers (`.razor.cs`)

Le presse-papier est un tableau des valeurs éditables (adapte le type : `decimal[]`,
`string[]`… selon les cellules). `null` par défaut = rien de copié.

```csharp
// Presse-papier : les valeurs copiées. null = rien de copié -> « Coller » désactivé.
private decimal[] _presse;

private void Copier(MaLigne row)
{
    _presse = row.LireValeurs();            // copie neuve, indépendante de la ligne source
    _snackbar.Add($"« {row.Libelle} » copié.", Severity.Info);
}

private async Task CollerAsync(MaLigne row)
{
    if (_presse is null) return;            // garde : rien à coller

    row.AppliquerValeurs(_presse);

    // Persistance immédiate (comme la saisie au blur), restauration si refus.
    var result = await _service.UpdateAsync(row.VersItem());
    if (Verifier(result, $"Collé sur « {row.Libelle} »."))
        row.FigerSnapshot();
    else
        row.Restaurer();
}
```

### 3. Lire/écrire les cellules sur le ViewModel de ligne

Les deux méthodes doivent parcourir les cellules éditables **dans le même ordre**. Renvoie
un tableau neuf à la lecture (pour que le presse-papier soit indépendant de la ligne).

```csharp
// Ordre fixe des cellules éditables (le MÊME dans les deux sens).
private decimal[] Valeurs => [ChampA, ChampB, ChampC /* … */];

public decimal[] LireValeurs() => Valeurs;   // Valeurs construit un tableau neuf à chaque appel

public void AppliquerValeurs(decimal[] v)
{
    ChampA = v[0]; ChampB = v[1]; ChampC = v[2]; /* … même ordre que Valeurs */
}
```

## Pièges à éviter

- **Ne stocke pas la ligne source dans le presse-papier** (`_presse = row`), sinon
  modifier la source après coup changerait ce qui est collé. Copie les **valeurs**.
- **Même ordre** dans `LireValeurs`/`AppliquerValeurs` — une inversion décale silencieusement
  les colonnes.
- **N'oublie pas la persistance** : coller sans appeler le service laisse la base incohérente
  (le collage disparaît au rechargement). Réutilise le service d'update de la page.
- `Disabled` sur `MudMenuItem` est réévalué à l'ouverture du menu : après un Copier, rouvrir
  le menu suffit à activer Coller (Blazor re-rend après le handler).
- Le collage recalcule les colonnes dérivées (ex. `Total`) automatiquement si elles sont
  des propriétés calculées — ne les copie pas.

## Variantes possibles (si demandé)

- **Interdire de coller sur la ligne source** : garder l'`Id` de la ligne copiée et désactiver
  Coller sur cette ligne (`Disabled="@(_presse is null || row.Id == _presseSourceId)"`).
- **Confirmation avant collage** (écrasement) : passer par une `MudMessageBox`/dialog.
- **Coller sur plusieurs lignes** (sélection multiple) : boucler `CollerAsync` sur la sélection.
