using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit
{
    public class BillingCreditForCreditCardMapper : IBillingCreditMapper
    {
        private readonly IBillingRepository _billingRepository;
        private readonly IEncryptionService _encryptionService;

        public BillingCreditForCreditCardMapper(IBillingRepository billingRepository, IEncryptionService encryptionService)
        {
            _billingRepository = billingRepository;
            _encryptionService = encryptionService;
        }

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(AgreementInformation agreementInformation, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, Promotion promotion, CreditCardPayment payment)
        {
            var currentPaymentMethod = await _billingRepository.GetPaymentMethodByUserName(user.Email);

            var buyCreditAgreement = new BillingCreditAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdBillingCountry,
                IdPaymentMethod = (int)user.PaymentMethod,
                IdCCType = currentPaymentMethod.IdCCType,
                CCExpMonth = short.Parse(currentPaymentMethod.CCExpMonth),
                CCExpYear = short.Parse(currentPaymentMethod.CCExpYear),
                CCHolderFullName = currentPaymentMethod.CCHolderFullName,
                CCIdentificationType = currentPaymentMethod.CCType,
                CCIdentificationNumber = CreditCardHelper.ObfuscateNumber(_encryptionService.DecryptAES256(currentPaymentMethod.CCNumber)),
                CCNumber = currentPaymentMethod.CCNumber,
                CCVerification = currentPaymentMethod.CCVerification,
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType) : null,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentPaymentMethod.IdentificationNumber,
                IdPromotion = promotion?.IdPromotion
            };

            DateTime now = DateTime.UtcNow;
            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = now,
                ActivationDate = now,
                Approved = true,
                Payed = true,
                IdUserTypePlan = newUserTypePlan.IdUserTypePlan,
                PlanFee = newUserTypePlan.Fee,
                CreditsQty = newUserTypePlan.EmailQty ?? null,
                ExtraEmailFee = newUserTypePlan.ExtraEmailCost ?? null,
                ExtraCreditsPromotion = promotion?.ExtraCredits,
                DiscountPlanFeePromotion = promotion?.DiscountPercentage
            };

            if (newUserTypePlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
            {
                var planDiscountInformation = await _billingRepository.GetPlanDiscountInformation(agreementInformation.DiscountId);

                buyCreditAgreement.BillingCredit.IdDiscountPlan = agreementInformation.DiscountId != 0 ? agreementInformation.DiscountId : null;
                buyCreditAgreement.BillingCredit.TotalMonthPlan = planDiscountInformation?.MonthPlan;
                buyCreditAgreement.BillingCredit.CurrentMonthPlan =
                    (buyCreditAgreement.BillingCredit.TotalMonthPlan.HasValue
                    && buyCreditAgreement.BillingCredit.TotalMonthPlan.Value > 1
                    && buyCreditAgreement.BillingCredit.Date.Day > 20)
                    ? 0 : 1;
                buyCreditAgreement.BillingCredit.SubscribersQty = newUserTypePlan.SubscribersQty;
            }

            buyCreditAgreement.IdResponsabileBilling = (int)ResponsabileBillingEnum.QBL;

            return buyCreditAgreement;
        }
    }
}
