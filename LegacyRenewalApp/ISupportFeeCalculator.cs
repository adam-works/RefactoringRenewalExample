namespace LegacyRenewalApp
{
    public interface ISupportFeeCalculator
    {
        (decimal amount, string notes) CalculateSupportFee(string planCode, bool includePremiumSupport);
    }
}
