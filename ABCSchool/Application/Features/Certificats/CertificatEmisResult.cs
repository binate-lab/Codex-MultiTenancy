namespace Application.Features.Certificats
{
    public class CertificatEmisResult
    {
        public Guid CertificatId { get; set; }
        public string PfxBase64 { get; set; }
        public string MotDePasse { get; set; }
        public string Empreinte { get; set; }
        public DateTime ExpireLe { get; set; }
    }
}
