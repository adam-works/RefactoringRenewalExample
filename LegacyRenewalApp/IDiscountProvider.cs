namespace LegacyRenewalApp
{
    public interface IDiscountProvider
    {
        (decimal amount, string notes) CalculateDiscount(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints);
    }
}
