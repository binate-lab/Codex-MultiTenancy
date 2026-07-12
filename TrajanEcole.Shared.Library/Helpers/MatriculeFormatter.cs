using System.Linq;
using System.Text;

namespace TrajanEcole.Shared.Library.Helpers
{
    // Mise en forme du matricule national façon Access, partagée par toutes les vues qui
    // l'affichent (listes de classe, reçu de paiement…) pour rester cohérent.
    public static class MatriculeFormatter
    {
        // « 22654456M » -> « 22 654 456 M » : chiffres groupés par 3 depuis la droite, lettre(s)
        // de contrôle finale séparée(s). Format inattendu : renvoyé compact (sans espaces).
        public static string Format(string mat)
        {
            if (string.IsNullOrWhiteSpace(mat)) return mat ?? string.Empty;
            var compact = mat.Replace(" ", string.Empty);

            // Sépare la lettre de contrôle finale (partie non chiffrée) des chiffres de tête.
            var i = compact.Length;
            while (i > 0 && !char.IsDigit(compact[i - 1])) i--;
            var digits = compact[..i];
            var suffixe = compact[i..];

            if (digits.Length == 0 || !digits.All(char.IsDigit))
                return compact;

            var sb = new StringBuilder();
            for (var k = 0; k < digits.Length; k++)
            {
                if (k > 0 && (digits.Length - k) % 3 == 0) sb.Append(' ');
                sb.Append(digits[k]);
            }

            return string.IsNullOrEmpty(suffixe) ? sb.ToString() : $"{sb} {suffixe}";
        }
    }
}
