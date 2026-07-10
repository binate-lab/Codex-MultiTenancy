namespace App.Infrastructure.Services.Eleves
{
    // Clé de contrôle du matricule national — copie front de l'algorithme (miroir de
    // Pedagogie.Eleves.Services.MatriculeCle). Permet l'auto-complétion de la clé et la
    // génération côté client, sans round-trip. Vérifié 7/7 sur de vrais matricules.
    public static class MatriculeCle
    {
        private const string TA = "000010001100111011111111011111";
        private const string TB = "123456012345654327543217765431";
        private const string TC = "ZYXWVUTSRQPNMLKJHGFEDCBA";   // 24 lettres, sans I ni O
        private const string POS = "01201201";
        private static readonly Random _rng = new();

        // Lettre de clé pour les 8 chiffres.
        public static char Cle(string base8)
        {
            int val3 = 0, val8 = 0;
            int kk = base8[0] == '0' ? 2 : 1;          // 1er chiffre ignoré s'il vaut 0
            for (int k = 8; k >= kk; k--)              // positions 8..kk (1-based)
            {
                int i = POS[k - 1] - '0';
                int j = base8[k - 1] - '0';
                val3 += TA[10 * i + j] - '0';
                val8 += TB[10 * i + j] - '0';
            }
            int v3 = val3 % 3;
            int v8 = (val8 % 8) + 1;
            return TC[8 * v3 + v8 - 1];
        }

        // Vrai si le matricule est « 8 chiffres + lettre de clé cohérente ».
        public static bool EstValide(string? matricule)
        {
            if (string.IsNullOrWhiteSpace(matricule) || matricule.Length != 9)
                return false;
            for (int i = 0; i < 8; i++)
                if (matricule[i] < '0' || matricule[i] > '9')
                    return false;
            return char.ToUpperInvariant(matricule[8]) == Cle(matricule[..8]);
        }

        // Matricule aléatoire valide : année 16..25 + 6 chiffres + clé.
        public static string Generer(int annee = 0)
        {
            if (annee == 0) annee = 16 + _rng.Next(0, 10);
            string b = annee.ToString("D2") + _rng.Next(0, 1_000_000).ToString("D6");
            return b + Cle(b);
        }
    }
}
