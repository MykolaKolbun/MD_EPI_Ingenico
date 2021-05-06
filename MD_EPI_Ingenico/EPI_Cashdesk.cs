using SkiData.ElectronicPayment;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD_EPI_Ingenico
{
    public class EPI_Cashdesk : ITerminal
    {
        public string Name => throw new NotImplementedException();

        public string ShortName => throw new NotImplementedException();

        public Settings Settings => throw new NotImplementedException();

        public event ActionEventHandler Action;
        public event ConfirmationEventHandler Confirmation;
        public event ChoiceEventHandler Choice;
        public event DeliveryCheckEventHandler DeliveryCheck;
        public event ErrorOccurredEventHandler ErrorOccurred;
        public event ErrorClearedEventHandler ErrorCleared;
        public event IrregularityDetectedEventHandler IrregularityDetected;
        public event EventHandler TerminalReadyChanged;
        public event JournalizeEventHandler Journalize;
        public event TraceEventHandler Trace;

        public void AllowCards(CardIssuerCollection issuers)
        {
            throw new NotImplementedException();
        }

        public bool BeginInstall(TerminalConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public TransactionResult Credit(TransactionData creditData)
        {
            throw new NotImplementedException();
        }

        public TransactionResult Credit(TransactionData creditData, Card card)
        {
            throw new NotImplementedException();
        }

        public TransactionResult Debit(TransactionData debitData)
        {
            throw new NotImplementedException();
        }

        public TransactionResult Debit(TransactionData debitData, Card card)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void EndInstall()
        {
            throw new NotImplementedException();
        }

        public void ExecuteCommand(int commandId)
        {
            throw new NotImplementedException();
        }

        public void ExecuteCommand(int commandId, object parameterValue)
        {
            throw new NotImplementedException();
        }

        public CommandDefinitionCollection GetCommands()
        {
            throw new NotImplementedException();
        }

        public TransactionResult GetLastTransaction()
        {
            throw new NotImplementedException();
        }

        public Card GetManualCard(Card card)
        {
            throw new NotImplementedException();
        }

        public Card GetManualCard(Card card, string paymentType)
        {
            throw new NotImplementedException();
        }

        public bool IsTerminalReady()
        {
            throw new NotImplementedException();
        }

        public void Notify(int notificationId)
        {
            throw new NotImplementedException();
        }

        public Card OpenInputDialog(IntPtr windowHandle, TransactionType transactionType, Card card)
        {
            throw new NotImplementedException();
        }

        public Receipts RepeatReceipt()
        {
            throw new NotImplementedException();
        }

        public void SetDisplayLanguage(CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        public void SetParameter(Parameter parameter)
        {
            throw new NotImplementedException();
        }

        public bool SupportsCreditCards()
        {
            throw new NotImplementedException();
        }

        public bool SupportsDebitCards()
        {
            throw new NotImplementedException();
        }

        public bool SupportsElectronicPurseCards()
        {
            throw new NotImplementedException();
        }

        public ValidationResult ValidateCard()
        {
            throw new NotImplementedException();
        }

        public ValidationResult ValidateCard(Card card)
        {
            throw new NotImplementedException();
        }

        public TransactionResult Void(TransactionDoneResult debitResultData)
        {
            throw new NotImplementedException();
        }
    }
}
