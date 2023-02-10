namespace CreditCardApplications
{
    public class CreditCardApplicationEvaluator
    {
        private readonly IFrequentFlyerNumberValidator _numberValidator;
        private readonly FraudLookup? _fraudLookup;

        public  CreditCardApplicationEvaluator(IFrequentFlyerNumberValidator numberValidator,
            FraudLookup? fraudLookup = null)
        {
            _numberValidator = numberValidator ?? throw new ArgumentNullException(nameof(numberValidator));
            _fraudLookup = fraudLookup;
        }


        private const int AutoReferralMaxAge = 20;
        private const int HighIncomeThreshhold = 100_000;
        private const int LowIncomeThreshhold = 20_000;
     
        public CreditCardApplicationDecision Evaluate(CreditCardApplication application)
        {

            if (_fraudLookup != null && _fraudLookup.IsFraudRisk(application))
            {
                return CreditCardApplicationDecision.ReferredToHumanFraudRisk;
            }

            if (application.GrossAnnualIncome >= HighIncomeThreshhold)
            {
                return CreditCardApplicationDecision.AutoAccepted;
            }


            if (_numberValidator.ServiceInformation.License.LicenseKey == "EXPIRED")
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }


            _numberValidator.ValidationMode = application.Age >= 30
                                            ? ValidationMode.Detailed
                                            : ValidationMode.Quick;

            bool isValidFrequentFlyerNumber;

            try
            {
                isValidFrequentFlyerNumber = _numberValidator.IsValid(application.FrequentFlyerNumber);
            }
            catch
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (!isValidFrequentFlyerNumber)
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (application.Age <= AutoReferralMaxAge)
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (application.GrossAnnualIncome < LowIncomeThreshhold)
            {
                return CreditCardApplicationDecision.AutoDeclined;
            }

            return CreditCardApplicationDecision.ReferredToHuman;
        }       
    }
}
