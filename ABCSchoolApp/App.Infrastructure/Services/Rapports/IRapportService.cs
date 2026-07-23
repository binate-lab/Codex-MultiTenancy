using System;
using System.Threading.Tasks;

namespace App.Infrastructure.Services.Rapports
{
    // Client des rapports mensuels de versements (Scolarite.Api). Renvoie le PDF (null si echec).
    public interface IRapportService
    {
        // Rapport mensuel par eleve (portrait), groupe par niveau.
        Task<byte[]> GetRapportMensuelPdfAsync(
            DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire);

        // Rapport mensuel par classe (paysage), groupe par niveau, colonnes financieres + Recup %.
        Task<byte[]> GetRapportParClassePdfAsync(
            DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire);

        // Bilan par nature des versements (portrait) sur une periode : montant ventile
        // Général (Aff + Naff) / Aff / Naff.
        Task<byte[]> GetRapportParNaturePdfAsync(
            DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire);

        // Bilan par mode de paiement (portrait) sur une periode : montant + part par
        // moyen de paiement (Espèce, Chèque, Virement, Mobile Money…) + camembert.
        Task<byte[]> GetRapportParModePaiementPdfAsync(
            DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire);

        // Rapport de recouvrement (paysage, sans periode — point au jour) : par classe,
        // attendu (echeances echues) vs recouvre, reste, taux + anneau.
        Task<byte[]> GetRapportRecouvrementPdfAsync(
            string ecole, string logoBase64, string ville, string anneeScolaire);
    }
}
