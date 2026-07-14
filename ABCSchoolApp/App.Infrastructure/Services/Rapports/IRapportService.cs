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
    }
}
