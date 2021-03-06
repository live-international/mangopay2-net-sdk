﻿using System;
using System.Collections.Generic;
using System.Linq;
using MangoPay.SDK.Core;
using MangoPay.SDK.Core.Enumerations;
using MangoPay.SDK.Entities;
using MangoPay.SDK.Entities.GET;
using MangoPay.SDK.Entities.POST;
using MangoPay.SDK.Entities.PUT;
using NUnit.Framework;

namespace MangoPay.SDK.Tests
{
    [TestFixture]
    public class ApiIdempotencyTest : BaseTest
    {
        /*[Test]
		public void Test_Idempotency_HooksCreate()
		{
			string key = DateTime.Now.Ticks.ToString();
			HookPostDTO hook = new HookPostDTO("http://test.com", EventType.PAYIN_NORMAL_FAILED);
			Api.Hooks.Create(key, hook);

			var result = Api.Idempotency.Get(key);

			Assert.IsInstanceOf<HookDTO>(result.Resource);
		}*/

        [Test]
        public void Test_Idempotency_CardRegistrationCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            GetNewPayInCardDirect(null, key);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<CardRegistrationDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_ClientCreateBankwireDirect()
        {
            string key = DateTime.Now.Ticks.ToString();
            var bankwireDirectPost =
                new ClientBankWireDirectPostDTO("CREDIT_EUR", new Money {Amount = 1000, Currency = CurrencyIso.EUR});
            Api.Clients.CreateBankWireDirect(key, bankwireDirectPost);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<PayInBankWireDirectDTO>(result.Resource);
        }
        /*
		[Test]
		public void Test_Idempotency_DisputesDocumentCreate()
		{
			string key = DateTime.Now.Ticks.ToString();
			Sort sort = new Sort();
			sort.AddField("CreationDate", SortDirection.desc);
			var clientDisputes = Api.Disputes.GetAll(new Pagination(1, 100), null, sort);
			if (clientDisputes == null || clientDisputes.Count == 0)
				Assert.Fail("INITIALIZATION FAILURE - cannot test disputes");
			DisputeDTO dispute = clientDisputes.FirstOrDefault(x => x.Status == DisputeStatus.PENDING_CLIENT_ACTION || x.Status == DisputeStatus.REOPENED_PENDING_CLIENT_ACTION);
			if (dispute == null)
				Assert.Fail("Cannot test creating dispute document because there's no dispute with expected status in the disputes list.");
			DisputeDocumentPostDTO documentPost = new DisputeDocumentPostDTO(DisputeDocumentType.DELIVERY_PROOF);
			Api.Disputes.CreateDisputeDocument(key, documentPost, dispute.Id);

			var result = Api.Idempotency.Get(key);

			Assert.IsInstanceOf<DisputeDocumentDTO>(result.Resource);
		}*/

        [Test]
        public void Test_Idempotency_DisputesRepudiationCreateSettlement()
        {
            string key = DateTime.Now.Ticks.ToString();
            Sort sort = new Sort();
            sort.AddField("CreationDate", SortDirection.desc);
            var clientDisputes = Api.Disputes.GetAll(new Pagination(1, 100), null, sort);
            if (clientDisputes == null || clientDisputes.Count == 0)
                Assert.Fail("INITIALIZATION FAILURE - cannot test disputes");
            DisputeDTO dispute = clientDisputes.FirstOrDefault(x =>
                x.Status == DisputeStatus.CLOSED && x.DisputeType == DisputeType.NOT_CONTESTABLE);
            if (dispute == null)
                Assert.Fail(
                    "Cannot test creating settlement transfer because there's no closed disputes in the disputes list.");
            string repudiationId = Api.Disputes.GetTransactions(dispute.Id, new Pagination(1, 1), null)[0].Id;
            var repudiation = Api.Disputes.GetRepudiation(repudiationId);
            SettlementTransferPostDTO post = new SettlementTransferPostDTO(repudiation.AuthorId,
                new Money {Currency = CurrencyIso.EUR, Amount = 1}, new Money {Currency = CurrencyIso.EUR, Amount = 0});
            Api.Disputes.CreateSettlementTransfer(key, post, repudiationId);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<SettlementDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_MandateCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            string bankAccountId = this.GetJohnsAccount().Id;
            string returnUrl = "http://test.test";
            MandatePostDTO mandatePost = new MandatePostDTO(bankAccountId, CultureCode.EN, returnUrl);
            Api.Mandates.Create(key, mandatePost);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<MandateDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_PayinsBankwireDirectCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            WalletDTO wallet = this.GetJohnsWallet();
            UserNaturalDTO user = this.GetJohn();
            PayInBankWireDirectPostDTO payIn = new PayInBankWireDirectPostDTO(user.Id, wallet.Id,
                new Money {Amount = 10000, Currency = CurrencyIso.EUR},
                new Money {Amount = 0, Currency = CurrencyIso.EUR});
            payIn.CreditedWalletId = wallet.Id;
            payIn.AuthorId = user.Id;
            Api.PayIns.CreateBankWireDirect(key, payIn);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<PayInBankWireDirectDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_PayinsCardDirectCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            UserNaturalDTO john = this.GetJohn();
            WalletPostDTO wallet =
                new WalletPostDTO(new List<string> {john.Id}, "WALLET IN EUR WITH MONEY", CurrencyIso.EUR);
            var johnsWallet = this.Api.Wallets.Create(wallet);
            CardRegistrationPostDTO cardRegistrationPost =
                new CardRegistrationPostDTO(johnsWallet.Owners[0], CurrencyIso.EUR);
            CardRegistrationDTO cardRegistration = this.Api.CardRegistrations.Create(cardRegistrationPost);
            CardRegistrationPutDTO cardRegistrationPut = new CardRegistrationPutDTO();
            cardRegistrationPut.RegistrationData = this.GetPaylineCorrectRegistartionData(cardRegistration);
            cardRegistration = this.Api.CardRegistrations.Update(cardRegistrationPut, cardRegistration.Id);
            CardDTO card = this.Api.Cards.Get(cardRegistration.CardId);
            PayInCardDirectPostDTO payIn = new PayInCardDirectPostDTO(cardRegistration.UserId, cardRegistration.UserId,
                new Money {Amount = 1000, Currency = CurrencyIso.EUR},
                new Money {Amount = 0, Currency = CurrencyIso.EUR},
                johnsWallet.Id, "http://test.com", card.Id);
            payIn.CardType = card.CardType;
            Api.PayIns.CreateCardDirect(key, payIn);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<PayInCardDirectDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_PayinsCardWebCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            WalletDTO wallet = this.GetJohnsWallet();
            UserNaturalDTO user = this.GetJohn();
            PayInCardWebPostDTO payIn = new PayInCardWebPostDTO(user.Id,
                new Money {Amount = 1000, Currency = CurrencyIso.EUR},
                new Money {Amount = 0, Currency = CurrencyIso.EUR}, wallet.Id, "https://test.com", CultureCode.FR,
                CardType.CB_VISA_MASTERCARD);
            Api.PayIns.CreateCardWeb(key, payIn);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<PayInCardWebDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_PayinsCreateRefunds()
        {
            string key = DateTime.Now.Ticks.ToString();
            PayInDTO payIn = this.GetNewPayInCardDirect();
            RefundDTO refund = this.GetNewRefundForPayIn(payIn, key);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<RefundDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_PayinsDirectDebitCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            WalletDTO wallet = this.GetJohnsWallet();
            UserNaturalDTO user = this.GetJohn();
            PayInDirectDebitPostDTO payIn = new PayInDirectDebitPostDTO(user.Id,
                new Money {Amount = 10000, Currency = CurrencyIso.EUR},
                new Money {Amount = 100, Currency = CurrencyIso.EUR}, wallet.Id, "http://www.mysite.com/returnURL/",
                CultureCode.FR, DirectDebitType.GIROPAY);
            payIn.TemplateURLOptions = new TemplateURLOptions {PAYLINE = "https://www.maysite.com/payline_template/"};
            payIn.Tag = "DirectDebit test tag";
            Api.PayIns.CreateDirectDebit(key, payIn);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<PayInDirectDebitDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_PayinsMandateDirectDebitCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            WalletDTO wallet = this.GetJohnsWallet();
            UserNaturalDTO user = this.GetJohn();
            string bankAccountId = this.GetJohnsAccount().Id;
            string returnUrl = "http://test.test";
            MandatePostDTO mandatePost = new MandatePostDTO(bankAccountId, CultureCode.EN, returnUrl);
            MandateDTO mandate = this.Api.Mandates.Create(mandatePost);

            /*	
			 *	! IMPORTANT NOTE !
			 *	
			 *	In order to make this test pass, at this place you have to set a breakpoint,
			 *	navigate to URL the mandate.RedirectURL property points to and click "CONFIRM" button.
			 * 
			 */
            PayInMandateDirectPostDTO payIn = new PayInMandateDirectPostDTO(user.Id,
                new Money {Amount = 10000, Currency = CurrencyIso.EUR},
                new Money {Amount = 0, Currency = CurrencyIso.EUR}, wallet.Id, "http://test.test", mandate.Id);
            Api.PayIns.CreateMandateDirectDebit(key, payIn);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<PayInMandateDirectDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_PayinsPreauthorizedDirectCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            CardPreAuthorizationDTO cardPreAuthorization = this.GetJohnsCardPreAuthorization();
            WalletDTO wallet = this.GetJohnsWalletWithMoney();
            UserNaturalDTO user = this.GetJohn();
            PayInPreauthorizedDirectPostDTO payIn = new PayInPreauthorizedDirectPostDTO(user.Id,
                new Money {Amount = 10000, Currency = CurrencyIso.EUR},
                new Money {Amount = 0, Currency = CurrencyIso.EUR}, wallet.Id, cardPreAuthorization.Id);
            payIn.SecureModeReturnURL = "http://test.com";
            Api.PayIns.CreatePreauthorizedDirect(key, payIn);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<PayInPreauthorizedDirectDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_PayoutsBankwireCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            WalletDTO wallet = this.GetJohnsWallet();
            UserNaturalDTO user = this.GetJohn();
            BankAccountDTO account = this.GetJohnsAccount();
            PayOutBankWirePostDTO payOut = new PayOutBankWirePostDTO(user.Id, wallet.Id,
                new Money {Amount = 10, Currency = CurrencyIso.EUR}, new Money {Amount = 5, Currency = CurrencyIso.EUR},
                account.Id, "Johns bank wire ref");
            payOut.Tag = "DefaultTag";
            payOut.CreditedUserId = user.Id;
            Api.PayOuts.CreateBankWire(key, payOut);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<PayOutBankWireDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_PreauthorizationCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            GetJohnsCardPreAuthorization(key);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<CardPreAuthorizationDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_TransfersCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            WalletDTO walletWithMoney = this.GetJohnsWalletWithMoney();
            UserNaturalDTO user = this.GetJohn();
            WalletPostDTO walletPost =
                new WalletPostDTO(new List<string> {user.Id}, "WALLET IN EUR FOR TRANSFER", CurrencyIso.EUR);
            WalletDTO wallet = this.Api.Wallets.Create(walletPost);
            TransferPostDTO transfer = new TransferPostDTO(user.Id, user.Id,
                new Money {Amount = 100, Currency = CurrencyIso.EUR},
                new Money {Amount = 0, Currency = CurrencyIso.EUR}, walletWithMoney.Id, wallet.Id);
            transfer.Tag = "DefaultTag";
            Api.Transfers.Create(key, transfer);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<TransferDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_TransfersCreateRefunds()
        {
            string key = DateTime.Now.Ticks.ToString();
            TransferDTO transfer = this.GetNewTransfer();
            UserNaturalDTO user = this.GetJohn();
            RefundTransferPostDTO refund = new RefundTransferPostDTO(user.Id);
            Api.Transfers.CreateRefund(key, transfer.Id, refund);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<RefundDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_UboDeclarationCreate()
        {
            string key = DateTime.Now.Ticks.ToString();

            var refusedReasons = new UboRefusedReasonType[]
            {
                UboRefusedReasonType.MISSING_UBO,
                UboRefusedReasonType.DECLARATION_DO_NOT_MATCH_UBO_INFORMATION
            };

            var userLegal = Api.Users.Create(CreateUserLegalPost());

            Api.UboDeclarations.CreateUboDeclaration(key, userLegal.Id);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<UboDeclarationDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_UsersCreateBankAccountsCa()
        {
            string key = DateTime.Now.Ticks.ToString();
            var john = this.GetJohn();
            var account = new BankAccountCaPostDTO(john.FirstName + " " + john.LastName, john.Address, "TestBankName",
                "123", "12345", "234234234234");
            Api.Users.CreateBankAccountCa(key, john.Id, account);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<BankAccountCaDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_UsersCreateBankAccountsGb()
        {
            string key = DateTime.Now.Ticks.ToString();
            var john = this.GetJohn();
            var account = new BankAccountGbPostDTO(john.FirstName + " " + john.LastName, john.Address, "63956474");
            account.SortCode = "200000";
            Api.Users.CreateBankAccountGb(key, john.Id, account);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<BankAccountGbDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_UsersCreateBankAccountsIban()
        {
            string key = DateTime.Now.Ticks.ToString();
            UserNaturalDTO john = this.GetJohn();
            var account = new BankAccountIbanPostDTO(john.FirstName + " " + john.LastName, john.Address,
                "FR7618829754160173622224154");
            account.UserId = john.Id;
            account.BIC = "CMBRFR2BCME";
            Api.Users.CreateBankAccountIban(key, john.Id, account);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<BankAccountIbanDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_UsersCreateBankAccountsOther()
        {
            string key = DateTime.Now.Ticks.ToString();
            var john = this.GetJohn();
            var account = new BankAccountOtherPostDTO(john.FirstName + " " + john.LastName, john.Address,
                "234234234234", "BINAADADXXX");
            account.Type = BankAccountType.OTHER;
            account.Country = CountryIso.FR;
            Api.Users.CreateBankAccountOther(key, john.Id, account);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<BankAccountOtherDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_UsersCreateBankAccountsUs()
        {
            string key = DateTime.Now.Ticks.ToString();
            var john = this.GetJohn();
            var account = new BankAccountUsPostDTO(john.FirstName + " " + john.LastName, john.Address, "234234234234",
                "234334789");
            Api.Users.CreateBankAccountUs(key, john.Id, account);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<BankAccountUsDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_UsersCreateKycDocument()
        {
            string key = DateTime.Now.Ticks.ToString();
            String johnsId = this.GetJohn().Id;
            Api.Users.CreateKycDocument(key, johnsId, KycDocumentType.IDENTITY_PROOF);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<KycDocumentDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_UsersCreateLegals()
        {
            string key = DateTime.Now.Ticks.ToString();
            var userPost = new UserLegalPostDTO("email@email.org", "SomeOtherSampleOrg", LegalPersonType.BUSINESS,
                "RepFName", "RepLName", new DateTime(1975, 12, 21, 0, 0, 0), CountryIso.FR, CountryIso.FR);
            Api.Users.Create(key, userPost);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<UserLegalDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_UsersCreateNaturals()
        {
            string key = DateTime.Now.Ticks.ToString();
            UserNaturalPostDTO user = new UserNaturalPostDTO("john.doe@sample.org", "John", "Doe",
                new DateTime(1975, 12, 21, 0, 0, 0), CountryIso.FR, CountryIso.FR);
            user.Occupation = "programmer";
            user.IncomeRange = 3;
            user.Address = new Address
            {
                AddressLine1 = "Address line 1", AddressLine2 = "Address line 2", City = "City",
                Country = CountryIso.PL, PostalCode = "11222", Region = "Region"
            };
            user.Capacity = CapacityType.NORMAL;

            Api.Users.Create(key, user);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<UserNaturalDTO>(result.Resource);
        }

        [Test]
        public void Test_Idempotency_WalletsCreate()
        {
            string key = DateTime.Now.Ticks.ToString();
            var john = this.GetJohn();
            var wallet = new WalletPostDTO(new List<string> {john.Id}, "WALLET IN EUR", CurrencyIso.EUR);
            Api.Wallets.Create(key, wallet);

            var result = Api.Idempotency.Get(key);

            Assert.IsInstanceOf<WalletDTO>(result.Resource);
        }
    }
}