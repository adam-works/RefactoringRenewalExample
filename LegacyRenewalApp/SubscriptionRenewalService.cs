using System;
using System.Text;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IBillingGateway _billingGateway;
        private readonly IDiscountProvider _discountProvider;
        private readonly ISupportFeeCalculator _supportFeeCalculator;
        private readonly IPaymentFeeCalculator _paymentFeeCalculator;
        private readonly ITaxCalculator _taxCalculator;

        // Public parameterless constructor for backward compatibility with LegacyRenewalAppConsumer
        public SubscriptionRenewalService()
            : this(
                new CustomerRepository(),
                new SubscriptionPlanRepository(),
                new BillingGatewayAdapter(),
                new DiscountProvider(),
                new SupportFeeCalculator(),
                new PaymentFeeCalculator(),
                new TaxCalculator())
        {
        }

        // Dependency Injection constructor
        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IBillingGateway billingGateway,
            IDiscountProvider discountProvider,
            ISupportFeeCalculator supportFeeCalculator,
            IPaymentFeeCalculator paymentFeeCalculator,
            ITaxCalculator taxCalculator)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _billingGateway = billingGateway;
            _discountProvider = discountProvider;
            _supportFeeCalculator = supportFeeCalculator;
            _paymentFeeCalculator = paymentFeeCalculator;
            _taxCalculator = taxCalculator;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            ValidateInputs(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = CalculateBaseAmount(plan, seatCount);
            
            var (discountAmount, discountNotes) = _discountProvider.CalculateDiscount(customer, plan, seatCount, baseAmount, useLoyaltyPoints);
            StringBuilder notesBuilder = new StringBuilder(discountNotes);

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notesBuilder.Append("minimum discounted subtotal applied; ");
            }

            var (supportFee, supportNotes) = _supportFeeCalculator.CalculateSupportFee(normalizedPlanCode, includePremiumSupport);
            notesBuilder.Append(supportNotes);

            var (paymentFee, paymentNotes) = _paymentFeeCalculator.CalculatePaymentFee(normalizedPaymentMethod, subtotalAfterDiscount + supportFee);
            notesBuilder.Append(paymentNotes);

            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = _taxCalculator.CalculateTaxAmount(customer, taxBase);
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notesBuilder.Append("minimum invoice amount applied; ");
            }

            var invoice = CreateInvoiceObject(customer, normalizedPlanCode, normalizedPaymentMethod, seatCount, baseAmount, discountAmount, supportFee, paymentFee, taxAmount, finalAmount, notesBuilder.ToString());

            _billingGateway.SaveInvoice(invoice);

            NotifyCustomer(customer, normalizedPlanCode, invoice.FinalAmount);

            return invoice;
        }

        private void ValidateInputs(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            if (customerId <= 0) throw new ArgumentException("Customer id must be positive");
            if (string.IsNullOrWhiteSpace(planCode)) throw new ArgumentException("Plan code is required");
            if (seatCount <= 0) throw new ArgumentException("Seat count must be positive");
            if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method is required");
        }

        private decimal CalculateBaseAmount(SubscriptionPlan plan, int seatCount)
        {
            return (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
        }

        private RenewalInvoice CreateInvoiceObject(Customer customer, string planCode, string paymentMethod, int seatCount, decimal baseAmount, decimal discountAmount, decimal supportFee, decimal paymentFee, decimal taxAmount, decimal finalAmount, string notes)
        {
            return new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customer.Id}-{planCode}",
                CustomerName = customer.FullName,
                PlanCode = planCode,
                PaymentMethod = paymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };
        }

        private void NotifyCustomer(Customer customer, string planCode, decimal finalAmount)
        {
            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body = $"Hello {customer.FullName}, your renewal for plan {planCode} has been prepared. Final amount: {finalAmount:F2}.";
                _billingGateway.SendEmail(customer.Email, subject, body);
            }
        }
    }
}
