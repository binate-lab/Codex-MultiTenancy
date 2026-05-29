namespace Application.Features.Tenancy
{
    public class UpdateTenantSubscriptionRequest
    {
        public string TenantIdentifier { get; set; }
        public DateTime NewExpiryDate { get; set; }
    }
}
