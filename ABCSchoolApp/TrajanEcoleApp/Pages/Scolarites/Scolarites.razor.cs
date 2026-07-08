using System.Globalization;
using App.Infrastructure.Services.Economat;
using App.Infrastructure.Services.Eleves;
using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Scolarites
{
    public partial class Scolarites
    {
        [Inject] private IScolariteEleveService _scolariteEleveService { get; set; } = default!;
        [Inject] private IStructureService _structureService { get; set; } = default!;
        [Inject] private IVersementService _versementService { get; set; } = default!;
        [Inject] private INatureVersementService _natureService { get; set; } = default!;
        [Inject] private IJSRuntime _js { get; set; } = default!;

        // Natures de versement configurables de l'école (module Economat) : alimentent la
        // déroulante « Nature du versement ». On ne garde que les natures visibles (OK),
        // triées par Ordre.
        private List<NatureVersementItem> _natures = new();

        // Libellé de la nature proposée par défaut à l'ouverture d'une saisie = la nature
        // d'inscription si définie, sinon la première visible.
        private string NatureParDefaut() =>
            _natures.FirstOrDefault(n => n.EstInscription)?.Libelle
            ?? _natures.FirstOrDefault()?.Libelle
            ?? string.Empty;

        // Année scolaire en cours (bandeau) — même source que SchoolNavMenu.
        private string _annee = "—";

        // Nom d'affichage et logo (URL/chemin) de l'école active (en-tête du reçu PDF) —
        // même source que SchoolNavMenu (GetMineAsync filtré sur le claim school).
        private string _nomEcole = string.Empty;
        private string _logoEcole = string.Empty;

        // Nom court de l'école (défini à la création) — sert d'EXPÉDITEUR (Sender ID) des SMS.
        private string _nomCourtEts = string.Empty;

        // Référentiel structures de l'école (module Structures de pedagogie-api) :
        // les sélecteurs Niveau/Classe de la grille et du filtre sont alimentés par
        // CE référentiel, plus par une liste figée dans le code.
        private List<string> _niveaux = new();
        private IReadOnlyList<ClasseItem> _classesRef = new List<ClasseItem>();

        // Classes ouvertes pour le niveau donné (cellule Classe d'une ligne).
        private IEnumerable<ClasseItem> ClassesPour(string niveau) =>
            _classesRef.Where(c => c.NiveauCode == niveau);

        // Format monétaire façon Access : « 35 000 F » (espace comme séparateur de milliers).
        private static readonly CultureInfo _fr = CultureInfo.GetCultureInfo("fr-FR");

        // ---- État des filtres (barre de recherche) ----
        private string _fNom = string.Empty;
        private string _fPrenoms = string.Empty;
        private string _fMatricule = string.Empty;
        private string _fNiveau = string.Empty;   // "" = tous
        private string _fClasse = string.Empty;
        private string _fStatut = "Tous";
        private string _fInscrit = "Tous";

        // Élèves de l'école, chargés depuis Scolarite.Api (ScolariteDb) dans OnInitializedAsync.
        private List<EleveScolariteRow> _all = new();

        protected override async Task OnInitializedAsync()
        {
            // École active = claim « school » (= CodeEts) du JWT école-scoped.
            var user = await _applicationStateProvider.GetAuthenticationStateProviderUserAsync();
            var codeEts = user.FindFirst("school")?.Value ?? string.Empty;

            // Année scolaire en cours (bandeau).
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();
            if (annee.IsSuccessful && annee.Data is not null)
            {
                _annee = annee.Data.Libelle;
            }

            // Référentiel structures : niveaux (dans l'ordre configuré) + classes de l'année.
            var cycles = await _structureService.GetCyclesAsync();
            _niveaux = cycles.SelectMany(c => c.Niveaux).Select(n => n.Code).ToList();
            _classesRef = await _structureService.GetClassesAsync(_annee == "—" ? null : _annee);

            // Natures de versement configurables (module Economat) : déroulante de saisie.
            var natures = await _natureService.GetNaturesAsync();
            _natures = natures.Where(n => n.OK).OrderBy(n => n.Ordre).ToList();
            _vNature = NatureParDefaut();

            // Nom de l'école active (en-tête du reçu PDF).
            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                var ecoles = await _schoolService.GetMineAsync();
                if (ecoles.IsSuccessful && ecoles.Data is not null)
                {
                    var ecole = ecoles.Data.FirstOrDefault(s => s.CodeEts == codeEts);
                    _nomEcole = ecole?.Name ?? string.Empty;
                    _logoEcole = ecole?.Logo ?? string.Empty;
                    _nomCourtEts = ecole?.NomCourtEts ?? string.Empty;
                }
            }

            // Chargement des élèves de l'école depuis Scolarite.Api (table Eleve / ScolariteDb).
            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                var eleves = await _scolariteEleveService.GetElevesAsync(codeEts);
                _all = eleves.Select(e => new EleveScolariteRow(
                    e.Id,
                    e.Matricule,
                    e.Telephone,
                    e.Nom,
                    e.Prenom,
                    e.Actif,
                    e.Inscrit,
                    e.Statut,
                    e.Niveau,
                    e.Classe,
                    e.Solde,            // colonne « Net à payer » ≈ solde restant
                    e.FraisScolarite    // colonne « Inscription » ≈ frais (à affiner plus tard)
                )).ToList();
            }

            // Restaure l'état mémorisé de la case « envoyer par SMS ».
            await ChargerPrefSmsAsync();
        }

        // Filtre client sur la source. Tous les critères se cumulent (ET).
        private IEnumerable<EleveScolariteRow> Filtered =>
            _all.Where(e =>
                (string.IsNullOrWhiteSpace(_fNom)
                    || e.Nom.Contains(_fNom, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(_fPrenoms)
                    || e.Prenoms.Contains(_fPrenoms, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(_fMatricule)
                    || Compact(e.Matricule).Contains(Compact(_fMatricule), StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(_fNiveau) || e.Niveau == _fNiveau)
                && (string.IsNullOrWhiteSpace(_fClasse)
                    || e.Classe.Contains(_fClasse, StringComparison.OrdinalIgnoreCase))
                && (_fStatut == "Tous" || e.Statut == _fStatut)
                && (_fInscrit == "Tous" || (_fInscrit == "Oui") == e.Inscrit));

        private void Effacer()
        {
            _fNom = _fPrenoms = _fMatricule = _fNiveau = _fClasse = string.Empty;
            _fStatut = _fInscrit = "Tous";
        }

        private void Fermer() => _navigation.NavigateTo("/ecole");

        // Édition en ligne du téléphone du correspondant : mise à jour immédiate de la
        // ligne puis persistance côté Scolarite.Api (Tuteur.Telephone1). En cas d'échec,
        // on restaure l'ancienne valeur et on prévient l'utilisateur.
        private async Task OnTelChanged(EleveScolariteRow row, string nouveau)
        {
            var ancien = row.TelCorrespondant;
            if (nouveau == ancien) return;

            row.TelCorrespondant = nouveau;

            var ok = await _scolariteEleveService.MajTelephoneCorrespondantAsync(row.Id, nouveau ?? string.Empty);
            if (!ok)
            {
                row.TelCorrespondant = ancien;
                _snackbar.Add("Impossible d'enregistrer le téléphone du correspondant.", Severity.Error);
            }
        }

        // Édition en ligne du statut (Aff/Naff) d'un élève.
        // TODO (à venir) : publier un événement « changement de statut » relié à
        // l'échéancier (recalcul des modalités de versement selon Aff/Naff).
        private void OnStatutChanged(EleveScolariteRow row, string nouveau)
        {
            row.Statut = nouveau;
        }

        // Édition en ligne du niveau d'un élève. La classe est invalidée si elle
        // n'appartient plus au nouveau niveau (cascade du référentiel structures).
        // TODO (à venir) : publier un événement « changement de niveau » relié à
        // l'échéancier (les frais / l'échéancier dépendent du niveau).
        private void OnNiveauChanged(EleveScolariteRow row, string nouveau)
        {
            row.Niveau = nouveau;
            if (!ClassesPour(nouveau).Any(c => c.Libelle == row.Classe))
            {
                row.Classe = string.Empty;
            }
        }

        // Édition en ligne de la classe d'un élève.
        // TODO (à venir) : publier un événement « changement de classe » relié à
        // l'échéancier / aux listes de classe.
        private void OnClasseChanged(EleveScolariteRow row, string nouveau)
        {
            row.Classe = nouveau;
        }

        // ================== Versements de l'élève sélectionné ==================

        // Élève sélectionné (clic sur une ligne de la grille rouge) + son état versements.
        private EleveScolariteRow _sel;
        private ScolariteResume _resume;
        private List<VersementDetailItem> _versements = new();
        private List<EcheanceEleveItem> _echeancier = new();

        // Champs de saisie du sous-form bleu ciel.
        private decimal _vMontant;
        private DateTime? _vDate = DateTime.Today;
        private string _vNature = string.Empty;   // fixé après chargement des natures
        private string _vMode = "Espèce";
        private string _vRef = string.Empty;
        private bool _vEnCours;

        // Id du versement en cours de modification (null = saisie d'un nouveau versement).
        // Quand il est renseigne, le bouton Valider fait une modification (PUT) au lieu
        // d'une creation (POST).
        private Guid? _vEnEdition;

        private async Task OnRowClick(TableRowClickEventArgs<EleveScolariteRow> args)
        {
            _sel = args.Item;
            NouveauVersement();
            await ChargerVersementsAsync();
        }

        // Surligne la ligne sélectionnée dans la grille rouge.
        private string ClasseLigne(EleveScolariteRow row, int _)
            => row == _sel ? "svt-ligne-sel" : string.Empty;

        private async Task ChargerVersementsAsync()
        {
            var data = await _versementService.GetVersementsAsync(_sel.Id);
            AppliquerReponse(data);
        }

        private void AppliquerReponse(VersementsEleveReponse data)
        {
            _resume = data?.Resume;
            _versements = data?.Versements ?? new List<VersementDetailItem>();
            _echeancier = data?.Echeancier ?? new List<EcheanceEleveItem>();

            // Rafraîchit la colonne « Net à payer » de la ligne (reste à payer à jour)
            // et les cases Actif / Inscrit (cochées quand un versement d'inscription passe).
            if (_resume is not null && _sel is not null)
            {
                _sel.NetAPayer = _resume.Reste;
                _sel.Inscription = _resume.FraisScolarite;
            }
            if (data is not null && _sel is not null)
            {
                _sel.Actif = data.Actif;
                _sel.Inscrit = data.Inscrit;
            }
        }

        // « Nouveau » : réinitialise la saisie (date du jour, Espèce, nature Inscription)
        // et sort du mode modification s'il était actif.
        private void NouveauVersement()
        {
            _vMontant = 0;
            _vDate = DateTime.Today;
            _vNature = NatureParDefaut();
            _vMode = "Espèce";
            _vRef = string.Empty;
            _vEnEdition = null;
        }

        // Clic sur « Modifier » d'une ligne du détail : charge ses valeurs dans le sous-form
        // et bascule en mode modification (le bouton Valider fera un PUT).
        private void EditerVersement(VersementDetailItem v)
        {
            _vEnEdition = v.Id;
            _vMontant = v.Montant;
            _vDate = v.DateVersement;
            _vNature = v.Nature;
            _vMode = v.MoyenPaiement;
            _vRef = v.ReferencePaiement == "-" ? string.Empty : v.ReferencePaiement;
        }

        private async Task ValiderVersementAsync()
        {
            if (_sel is null) return;
            if (_vMontant <= 0)
            {
                _snackbar.Add("Saisis d'abord le montant du versement.", Severity.Warning);
                return;
            }

            _vEnCours = true;
            try
            {
                // Mode modification (PUT) si un versement est en édition, sinon création (POST).
                var result = _vEnEdition is Guid id
                    ? await _versementService.UpdateAsync(
                        _sel.Id, id, _vMontant, _vDate, _vNature, _vMode, _vRef)
                    : await _versementService.CreateAsync(
                        _sel.Id, _vMontant, _vDate, _vNature, _vMode, _vRef);

                if (result.IsSuccessful)
                {
                    var verbe = _vEnEdition is null ? "enregistré" : "modifié";
                    _snackbar.Add($"Versement de {Fmt(_vMontant)} {verbe} pour {_sel.Nom} {_sel.Prenoms}.", Severity.Success);
                    AppliquerReponse(result.Data);
                    NouveauVersement();
                }
                else
                {
                    _snackbar.Add(result.Error, Severity.Error);
                }
            }
            finally
            {
                _vEnCours = false;
            }
        }

        // Clic sur « Supprimer » d'une ligne du détail : confirmation puis suppression
        // DÉFINITIVE (le backend rejoue l'imputation de l'échéancier).
        private async Task SupprimerVersementAsync(VersementDetailItem v)
        {
            if (_sel is null) return;

            var ok = await _js.InvokeAsync<bool>("confirm",
                $"Supprimer définitivement ce versement de {Fmt(v.Montant)} du {v.DateVersement:dd/MM/yyyy} ?");
            if (!ok) return;

            _vEnCours = true;
            try
            {
                var result = await _versementService.DeleteAsync(_sel.Id, v.Id);
                if (result.IsSuccessful)
                {
                    _snackbar.Add($"Versement de {Fmt(v.Montant)} supprimé.", Severity.Success);
                    AppliquerReponse(result.Data);
                    // Si on supprimait le versement en cours d'édition, on quitte ce mode.
                    if (_vEnEdition == v.Id) NouveauVersement();
                }
                else
                {
                    _snackbar.Add(result.Error, Severity.Error);
                }
            }
            finally
            {
                _vEnCours = false;
            }
        }

        // ================== Reçu de paiement (PDF) ==================

        private bool _recuEnCours;

        // Télécharge le reçu PDF de l'élève sélectionné (situation du compte :
        // versements + synthèse + échéancier) — calque du reçu Access.
        private async Task TelechargerRecuAsync()
        {
            if (_sel is null) return;

            _recuEnCours = true;
            try
            {
                // Convertit le logo (URL/chemin front) en base64 pour l'embarquer dans le
                // PDF serveur ; null si absent → le reçu se génère sans logo.
                var logoBase64 = string.IsNullOrWhiteSpace(_logoEcole)
                    ? null
                    : await _js.InvokeAsync<string>("trajanImageEnBase64", _logoEcole);

                var pdf = await _versementService.GetRecuPdfAsync(_sel.Id, _nomEcole, logoBase64);
                if (pdf is null || pdf.Length == 0)
                {
                    _snackbar.Add("Reçu indisponible pour cet élève.", Severity.Error);
                    return;
                }

                var nomFichier = $"recu-{Compact(_sel.Matricule)}-{DateTime.Today:yyyyMMdd}.pdf";
                await _js.InvokeVoidAsync("trajanTelechargerFichier",
                    nomFichier, Convert.ToBase64String(pdf), "application/pdf");
            }
            finally
            {
                _recuEnCours = false;
            }
        }

        // ================== Reçu par SMS ==================

        // Clé localStorage de la préférence « case SMS cochée à l'ouverture ».
        private const string SmsPrefKey = "scolarites.sms.coche";

        // État de la case (cochée = envoi SMS autorisé) et verrou d'envoi.
        private bool _smsCoche;
        private bool _smsEnCours;

        // Restaure au chargement la préférence mémorisée (« 1 » = case cochée).
        private async Task ChargerPrefSmsAsync()
        {
            var pref = await _js.InvokeAsync<string>("localStorage.getItem", SmsPrefKey);
            _smsCoche = pref == "1";
        }

        // Clic sur la case : demande si l'état choisi doit être mémorisé pour les
        // prochaines ouvertures de la page (cf. cahier des charges).
        private async Task OnSmsCocheChangedAsync(bool coche)
        {
            _smsCoche = coche;

            if (coche)
            {
                // On vient de cocher → proposer de la garder cochée à l'avenir.
                var oui = await _js.InvokeAsync<bool>("confirm",
                    "Voulez-vous que cette case soit cochée à l'avenir ?");
                if (oui)
                {
                    await _js.InvokeVoidAsync("localStorage.setItem", SmsPrefKey, "1");
                }
                // Sinon : on ne mémorise rien (à la prochaine ouverture, la préférence
                // enregistrée précédemment — ou l'état décoché par défaut — s'applique).
            }
            else
            {
                // On vient de décocher → proposer de la garder décochée à l'avenir.
                var oui = await _js.InvokeAsync<bool>("confirm",
                    "Voulez-vous que cette case soit décochée à l'avenir ?");
                // Oui → décochée à la prochaine ouverture ; sinon → cochée à l'avenir.
                await _js.InvokeVoidAsync("localStorage.setItem", SmsPrefKey, oui ? "0" : "1");
            }
        }

        // Bouton SMS : n'envoie que si la case est cochée (sinon rien ne se passe).
        // Règle actuelle : seul le 1er versement d'inscription déclenche un SMS. Les autres
        // versements d'inscription n'en envoient pas ; les autres natures (Scolarité,
        // Transport…) auront leur propre texte, à définir (cf. _texteSmsPour).
        private async Task EnvoyerSmsAsync()
        {
            if (!_smsCoche) return;   // case décochée → aucun envoi
            if (_sel is null) return;

            // Combien de versements d'inscription déjà enregistrés pour cet élève ?
            // On compare au libellé de LA nature d'inscription configurée (pas au mot figé).
            var libInscription = _natures.FirstOrDefault(n => n.EstInscription)?.Libelle ?? "Inscription";
            var nbInscription = _versements.Count(v => v.Nature == libInscription);
            if (nbInscription == 0)
            {
                _snackbar.Add("Aucun versement d'inscription : pas de SMS pour l'instant.", Severity.Info);
                return;
            }
            if (nbInscription > 1)
            {
                _snackbar.Add("SMS déjà envoyé au 1er versement d'inscription (les suivants n'en envoient pas).", Severity.Info);
                return;
            }

            var destinataire = _sel.TelCorrespondant;
            if (string.IsNullOrWhiteSpace(destinataire))
            {
                _snackbar.Add("Pas de téléphone correspondant : SMS impossible.", Severity.Warning);
                return;
            }

            _smsEnCours = true;
            try
            {
                var texte = TexteSmsPour("Inscription");
                // TODO sms-api : envoyer `texte` à `destinataire`, expéditeur = _nomCourtEts.
                _snackbar.Add($"SMS (exp. {_nomCourtEts}) → {destinataire} : {texte}", Severity.Success);
            }
            finally
            {
                _smsEnCours = false;
            }
        }

        // Texte du SMS selon la nature du versement. Seul « Inscription » est défini pour
        // l'instant ; les autres natures renverront le texte que tu me communiqueras.
        // Tous les messages sont passés à SansAccents (SMS = caractères non accentués).
        private string TexteSmsPour(string nature) => nature switch
        {
            "Inscription" => SansAccents(
                $"Chers parents,\nl'inscription de {_sel?.Nom} {_sel?.Prenoms} en {_sel?.Classe} a bien ete enregistree. \nMerci.\nLA DIRECTION"),
            _ => string.Empty,   // à compléter (texte fourni ultérieurement par nature)
        };

        // Retire les caractères accentués d'un message SMS : « é/è/ê »→« e », « ç »→« c »,
        // « à »→« a »… (décomposition Unicode puis suppression des marques diacritiques).
        private static string SansAccents(string texte)
        {
            if (string.IsNullOrEmpty(texte)) return texte;

            var decompose = texte.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(decompose.Length);
            foreach (var c in decompose)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        // Libellé et couleur du statut d'une échéance (valeurs = noms de l'enum backend).
        private static string AfficherStatut(string statut) => statut switch
        {
            "Paye" => "Payé",
            "Partiel" => "Partiel",
            "NonPaye" => "Non payé",
            _ => statut,
        };

        private static string ClasseStatut(string statut) => statut switch
        {
            "Paye" => "svt-ech-paye",
            "Partiel" => "svt-ech-partiel",
            _ => "svt-ech-nonpaye",
        };

        // Comparaison de matricule insensible aux espaces (« 24 179 400 X » ~ « 24179400X »).
        private static string Compact(string s) => s.Replace(" ", string.Empty);

        private static string Fmt(decimal montant) => montant.ToString("#,0", _fr) + " F";

        // Ligne de la grille rouge (calque des colonnes du formulaire Access « Scolarités »).
        // Statut et Niveau sont modifiables directement dans la grille → propriétés set.
        public sealed class EleveScolariteRow
        {
            public Guid Id { get; }
            public string Matricule { get; }
            public string TelCorrespondant { get; set; }
            public string Nom { get; }
            public string Prenoms { get; }
            public bool Actif { get; set; }           // IsActif — coché quand l'inscription est entamée
            public bool Inscrit { get; set; }         // IsInscrit — idem
            public string Statut { get; set; }
            public string Niveau { get; set; }
            public string Classe { get; set; }
            public decimal NetAPayer { get; set; }   // rafraîchi après chaque versement (reste à payer)
            public decimal Inscription { get; set; } // frais de l'année (échéancier généré)

            public EleveScolariteRow(
                Guid id, string matricule, string telCorrespondant, string nom, string prenoms,
                bool actif, bool inscrit, string statut, string niveau, string classe,
                decimal netAPayer, decimal inscription)
            {
                Id = id;
                Matricule = matricule;
                TelCorrespondant = telCorrespondant;
                Nom = nom;
                Prenoms = prenoms;
                Actif = actif;
                Inscrit = inscrit;
                Statut = statut;
                Niveau = niveau;
                Classe = classe;
                NetAPayer = netAPayer;
                Inscription = inscription;
            }
        }
    }
}
