namespace LegacyRenewalApp
{
    public class SupportFeeCalculator : ISupportFeeCalculator
    {
        public (decimal amount, string notes) CalculateSupportFee(string planCode, bool includePremiumSupport)
        {
            decimal supportFee = 0m;
            string notes = string.Empty;

            if (includePremiumSupport)
            {
                if (planCode == "START")
                {
                    supportFee = 250m;
                }
                else if (planCode == "PRO")
                {
                    supportFee = 400m;
                }
                else if (planCode == "ENTERPRISE")
                {
                    supportFee = 700m;
                }

                notes += "premium support included; ";
            }

            return (supportFee, notes);
        }
    }
}
