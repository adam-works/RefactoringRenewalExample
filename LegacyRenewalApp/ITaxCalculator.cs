namespace LegacyRenewalApp
{
    public interface ITaxCalculator
    {
        decimal CalculateTaxAmount(Customer customer, decimal taxBase);
    }
}
