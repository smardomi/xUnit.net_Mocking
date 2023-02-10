using Moq;
using Moq.Protected;
using Range = Moq.Range;

namespace CreditCardApplications.Test
{
    public class CreditCardApplicationEvaluatorShould
    {

        private readonly Mock<IFrequentFlyerNumberValidator> _mockValidator;
        private readonly CreditCardApplicationEvaluator _sut;

        public CreditCardApplicationEvaluatorShould()
        {
            _mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            _mockValidator.SetupAllProperties();
            _mockValidator.Setup(a => a.ServiceInformation.License.LicenseKey)
                          .Returns("OK");
            _mockValidator.Setup(a=>a.IsValid(It.IsAny<string>()))
                          .Returns(true);

            _sut = new CreditCardApplicationEvaluator(_mockValidator.Object);
        }


        [Fact]
        public void AcceptHighIncomeApplications()
        {
            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 100_000
            };

            var decision = _sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.AutoAccepted, decision);
        }


        [Fact]
        public void ReferYoungApplications()
        {
            var application = new CreditCardApplication
            {
                Age = 19
            };

            var decision = _sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        [Fact]
        public void DeclineLowIncomeApplications()
        {
            //Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>(MockBehavior.Loose);

            //mockValidator.Setup(a => a.IsValid(It.IsAny<string>()))
            //              .Returns(true);

            //mockValidator.Setup(a => 
            //                a.IsValid(It.Is<string>(n=>n.Contains("x"))))
            //             .Returns(true);

            //mockValidator.Setup(a =>
            //                a.IsValid(It.IsIn("x","y","z")))
            //             .Returns(true);

            _mockValidator.Setup(a =>
                              a.IsValid(It.IsInRange("a", "z", Range.Inclusive)))
                         .Returns(true);

            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 19_999,
                Age = 42,
                FrequentFlyerNumber = "s"
            };

            CreditCardApplicationDecision decision = _sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);
        }

        [Fact]
        public void ReferWhenLicenseKeyExpired()
        {

            _mockValidator.Setup(a => a.ServiceInformation.License.LicenseKey).Returns(GetLicenseKeyExpiryString);

            var application = new CreditCardApplication
            {
                Age = 42
            };

            CreditCardApplicationDecision decision = _sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        [Fact]
        public void UseDetailedLookupForOlderApplications()
        {
            _mockValidator.Setup(a => a.ServiceInformation.License.LicenseKey).Returns(GetLicenseKeyExpiryString);

            var application = new CreditCardApplication
            {
                Age = 30
            };

            var decision = _sut.Evaluate(application);

            Assert.Equal(ValidationMode.Detailed, _mockValidator.Object.ValidationMode);
        }

        [Fact]
        public void ShouldValidateFrequentFlyerNumberForLowIncomeApplications()
        {
            var application = new CreditCardApplication
            {
                FrequentFlyerNumber = "q1"
            };

            CreditCardApplicationDecision decision = _sut.Evaluate(application);

            _mockValidator.Verify(a => a.IsValid("q"), Times.Never);

        }


        [Fact]
        public void ReferWhenFrequentFlyerValidationError()
        {

            // mockValidator.Setup(a => a.IsValid(It.IsAny<string>()))
            //              .Throws<Exception>();

            _mockValidator.Setup(a => a.IsValid(It.IsAny<string>()))
                .Throws(new Exception("Custom Exception"));


            var application = new CreditCardApplication
            {
                Age = 42
            };

            CreditCardApplicationDecision decision = _sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);

        }


        [Fact]
        public void ReferInvalidFrequentFlyerApplications_Sequence()
        {

            _mockValidator.SetupSequence(a =>
                                                a.IsValid(It.IsAny<string>()))
                         .Returns(false)
                         .Returns(true);


            var application = new CreditCardApplication { Age = 25 };

            CreditCardApplicationDecision firstDecision = _sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, firstDecision);

            CreditCardApplicationDecision secondDecision = _sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, secondDecision);
        }

        [Fact]
        public void ReferFraudRisk()
        {
            var mockFraudLookup = new Mock<FraudLookup?>();

            mockFraudLookup.Protected()
                           .Setup<bool>("CheckApplication", ItExpr.IsAny<CreditCardApplication>())
                           .Returns(true);

            _mockValidator.SetupSequence(a =>
                    a.IsValid(It.IsAny<string>()))
                .Returns(false)
                .Returns(true);


            var sut = new CreditCardApplicationEvaluator(_mockValidator.Object, mockFraudLookup.Object);

            var application = new CreditCardApplication();

            CreditCardApplicationDecision decision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.ReferredToHumanFraudRisk, decision);

        }

        [Fact]
        public void LinqToMocks()
        {

            var mockValidator = Mock.Of<IFrequentFlyerNumberValidator>
            (
                validation => 
                    validation.ServiceInformation.License.LicenseKey == "OK" &&
                    validation.IsValid(It.IsAny<string>()) == true

            );


            var sut = new CreditCardApplicationEvaluator(mockValidator);

            var application = new CreditCardApplication { Age = 25 };

            CreditCardApplicationDecision secondDecision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, secondDecision);
        }

        string GetLicenseKeyExpiryString()
        {
            return "EXPIRED";
        }
    }
}