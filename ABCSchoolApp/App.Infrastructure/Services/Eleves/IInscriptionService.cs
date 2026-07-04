namespace App.Infrastructure.Services.Eleves
{
    // #5 : compteur de N° Inscription (NumOrdre) par ecole. La source est Scolarite (toutes
    // les creations d'eleves y arrivent via l'event), d'ou un client dedie a Scolarite.Api.
    public interface IInscriptionService
    {
        // Prochain N° d'ordre pour une ecole (max + 1, 1 si premier). null si indisponible.
        Task<int?> GetNextNumOrdreAsync(string codeEts);
    }
}
