using System.Globalization;
using App.Infrastructure.Services.Economat;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Economat
{
    public partial class ZonesTransport
    {
        [Inject] private IZoneTransportService _zoneService { get; set; } = default!;
        [Inject] private IJSRuntime _js { get; set; } = default!;

        private static readonly CultureInfo _fr = CultureInfo.GetCultureInfo("fr-FR");

        private string _annee = "—";
        private List<ZoneRow> _lignes = new();

        private string _nouvelleZone = string.Empty;
        private string _nouveauNom = string.Empty;
        private bool _enCours;

        protected override async Task OnInitializedAsync()
        {
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();
            if (annee.IsSuccessful && annee.Data is not null)
                _annee = annee.Data.Libelle;

            await ChargerAsync();
        }

        private async Task ChargerAsync()
        {
            var zones = await _zoneService.GetZonesAsync(_annee == "—" ? null : _annee);
            _lignes = zones.OrderBy(z => z.Zone).Select(z => new ZoneRow(z)).ToList();
        }

        private static string FormaterMontant(decimal montant) => montant.ToString("#,0", _fr) + " F";

        private async Task AjouterAsync()
        {
            if (string.IsNullOrWhiteSpace(_nouvelleZone))
            {
                _snackbar.Add("Saisis d'abord le nom de la zone.", Severity.Warning);
                return;
            }
            if (_annee == "—")
            {
                _snackbar.Add("Année scolaire en cours indisponible.", Severity.Error);
                return;
            }

            _enCours = true;
            try
            {
                var result = await _zoneService.CreateAsync(_annee, _nouvelleZone.Trim(), _nouveauNom.Trim());
                if (result.IsSuccessful)
                {
                    _snackbar.Add($"Zone « {_nouvelleZone.Trim()} » ajoutée.", Severity.Success);
                    _nouvelleZone = string.Empty;
                    _nouveauNom = string.Empty;
                    await ChargerAsync();
                }
                else
                {
                    _snackbar.Add(result.Error, Severity.Error);
                }
            }
            finally { _enCours = false; }
        }

        // Enregistre une ligne au blur d'une cellule (ou au coche Visible) si elle a changé.
        private async Task EnregistrerSiModifieeAsync(ZoneRow row)
        {
            if (!row.EstModifiee) return;

            var result = await _zoneService.UpdateAsync(row.VersItem());
            if (result.IsSuccessful)
                row.FigerSnapshot();
            else
            {
                row.Restaurer();
                _snackbar.Add(result.Error, Severity.Error);
            }
        }

        private async Task SupprimerAsync(ZoneRow row)
        {
            var ok = await _js.InvokeAsync<bool>("confirm", $"Supprimer la zone « {row.Zone} » ?");
            if (!ok) return;

            _enCours = true;
            try
            {
                var result = await _zoneService.DeleteAsync(row.Id);
                if (result.IsSuccessful)
                {
                    _snackbar.Add($"Zone « {row.Zone} » supprimée.", Severity.Success);
                    await ChargerAsync();
                }
                else
                {
                    _snackbar.Add(result.Error, Severity.Error);
                }
            }
            finally { _enCours = false; }
        }

        // ------------------- ViewModel mutable de la grille -------------------
        public sealed class ZoneRow
        {
            public ZoneRow(ZoneTransportItem z)
            {
                Id = z.Id; Zone = z.Zone; NomZone = z.NomZone; OK = z.OK;
                Septembre = z.Septembre; Octobre = z.Octobre; Novembre = z.Novembre; Decembre = z.Decembre;
                Janvier = z.Janvier; Fevrier = z.Fevrier; Mars = z.Mars; Avril = z.Avril; Mai = z.Mai;
                FigerSnapshot();
            }

            public Guid Id { get; }
            public string Zone { get; }
            public string NomZone { get; set; }
            public bool OK { get; set; }
            public decimal Septembre { get; set; }
            public decimal Octobre { get; set; }
            public decimal Novembre { get; set; }
            public decimal Decembre { get; set; }
            public decimal Janvier { get; set; }
            public decimal Fevrier { get; set; }
            public decimal Mars { get; set; }
            public decimal Avril { get; set; }
            public decimal Mai { get; set; }

            public decimal TotalCalcule => Septembre + Octobre + Novembre + Decembre
                                         + Janvier + Fevrier + Mars + Avril + Mai;

            private (string, bool, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal) _snapshot;
            private (string, bool, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal) Etat()
                => (NomZone, OK, Septembre, Octobre, Novembre, Decembre, Janvier, Fevrier, Mars, Avril, Mai);

            public bool EstModifiee => Etat() != _snapshot;
            public void FigerSnapshot() => _snapshot = Etat();
            public void Restaurer()
                => (NomZone, OK, Septembre, Octobre, Novembre, Decembre, Janvier, Fevrier, Mars, Avril, Mai) = _snapshot;

            public ZoneTransportItem VersItem() => new()
            {
                Id = Id, Zone = Zone, NomZone = NomZone, OK = OK,
                Septembre = Septembre, Octobre = Octobre, Novembre = Novembre, Decembre = Decembre,
                Janvier = Janvier, Fevrier = Fevrier, Mars = Mars, Avril = Avril, Mai = Mai,
            };
        }
    }
}
