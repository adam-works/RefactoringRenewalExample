using System;

namespace LegacyRenewalApp
{
    public class PaymentFeeCalculator : IPaymentFeeCalculator
    {
        public (decimal amount, string notes) CalculatePaymentFee(string paymentMethod, decimal subtotalWithSupportFee)
        {
            decimal paymentFee = 0m;
            string notes = string.Empty;

            if (paymentMethod == "CARD")
            {
                paymentFee = subtotalWithSupportFee * 0.02m;
                notes += "card payment fee; ";
            }
            else if (paymentMethod == "BANK_TRANSFER")
            {
                paymentFee = subtotalWithSupportFee * 0.01m;
                notes += "bank transfer fee; ";
            }
            else if (paymentMethod == "PAYPAL")
            {
                paymentFee = subtotalWithSupportFee * 0.035m;
                notes += "paypal fee; ";
            }
            else if (paymentMethod == "INVOICE")
            {
                paymentFee = 0m;
                notes += "invoice payment; ";
            }
            else
            {
                throw new ArgumentException("Unsupported payment method");
            }

            return (paymentFee, notes);
        }
    }
}
