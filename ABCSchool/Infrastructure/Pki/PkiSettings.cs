namespace Infrastructure.Pki
{
    public class PkiSettings
    {
        public string CaCertPath { get; set; }
        public string CaCertPassword { get; set; }
        public int DefaultValidityDays { get; set; } = 365;
        public bool ValiderCertificatAppareil { get; set; } = true;
    }
}
