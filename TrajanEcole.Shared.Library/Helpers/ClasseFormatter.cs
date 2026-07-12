using System.Text.RegularExpressions;

namespace TrajanEcole.Shared.Library.Helpers
{
    // Mise en forme du libellé de classe pour l'affichage/impression, partagée par les vues
    // (listes, reçu…) pour rester cohérent.
    public static class ClasseFormatter
    {
        // Classes de COLLÈGE (cycle 1) « 6e1 / 5e1 / 4e3 / 3e2 » -> « 6è 1 / 5è 1 / 4è 3 / 3è 2 »
        // (e -> è + espace avant la subdivision). Le 2nd cycle (2nde, 1ere, TleA1, TleD3…) n'est
        // PAS concerné : le motif exige un chiffre de niveau 3–6 suivi de « e » puis d'une
        // subdivision chiffrée (ou fin de chaîne).
        public static string Format(string classe)
        {
            if (string.IsNullOrWhiteSpace(classe)) return classe ?? string.Empty;
            return Regex.Replace(classe.Trim(), @"^([3-6])e(?=\d|$)", "$1è ").Trim();
        }
    }
}
