using Application.Features.AnneesScolaires;
using Domain.Entities;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AnneesScolaires
{
    public class AnneeScolaireService : IAnneeScolaireService
    {
        private readonly ApplicationDbContext _context;

        public AnneeScolaireService(ApplicationDbContext context)
        {
            _context = context;
        }

        // AnneesScolaires est multi-tenant (filtre automatique) → renvoie l'année
        // active du tenant courant, ou null si aucune n'est marquée AnneeEnCours.
        public async Task<AnneeScolaire> GetAnneeEnCoursAsync()
        {
            return await _context.AnneesScolaires
                .FirstOrDefaultAsync(annee => annee.AnneeEnCours);
        }
    }
}
