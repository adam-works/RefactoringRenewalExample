namespace LegacyRenewalApp
{
    public interface IPaymentFeeCalculator
    {
        (decimal amount, string notes) CalculatePaymentFee(string paymentMethod, decimal subtotalWithSupportFee);
    }
}
