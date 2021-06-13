using SkiData.ElectronicPayment;
using SkiData.Parking.ElectronicPayment.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD_EPI_Ingenico
{
    public class EPI_Cashdesk : ITerminal, ICardHandling2, ISettlement
    {

        #region Fields
        private Settings _settings = new Settings();
        private TerminalConfiguration _termConfig;
        public static System.Timers.Timer timeCheckTimer = new System.Timers.Timer(60000);
        DeviceType deviceType;
        string deviceID;
        public static string machineID;
        bool settelmentDone = false;
        public TransactionResult lastTransaction;
        public TransactionData transactionDataContainer;
        private const byte merchID = 1;
        private string _userId = String.Empty;
        private string _userName = String.Empty;
        private string _shiftId = String.Empty;
        private bool isTerminalReady = true;
        private bool _activated = false;
        private bool _inTransaction = false;
        bool transactionCanceled = false;
        string terminalID = String.Empty;
        IngenicoLib reader;
        #endregion

        #region Constructor
        public EPI_Cashdesk()
        {
            this._settings.AllowsCancel = false;
            this._settings.AllowsCredit = false;
            this._settings.AllowsRepeatReceipt = false;
            this._settings.AllowsValidateCard = true;
            this._settings.AllowsVoid = true;
            this._settings.CanSetCardData = true;
            this._settings.HasCardReader = true;
            this._settings.IsContactless = false;
            this._settings.MayPrintReceiptOnRejection = false;
            this._settings.NeedsSkidataChipReader = false;
            this._settings.RequireReceiptPrinter = false;
        }
        #endregion

        #region IDisposable members

        private bool disposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">if set to <see langword="true"/> the managed resources will be disposed.</param>
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    //if (_controlDialog != null)
                    //    _controlDialog.CloseForm();
                }
            }
            this.disposed = true;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Terminal"/> is reclaimed by garbage collection.
        /// </summary>
        ~EPI_Cashdesk()
        {
            this.Dispose(false);
        }
        #endregion


        #region ITerminal

        public string Name => "MD_Ingenico.Terminal";

        public string ShortName => "MD_Terminal";

        public Settings Settings
        {
            get { return this.TerminalSettings; }
        }

        private Settings TerminalSettings
        {
            get { return this._settings; }
        }

        public bool BeginInstall(TerminalConfiguration configuration)
        {
            _termConfig = configuration;
            bool done = false;
            reader = new IngenicoLib();
            this.deviceType = configuration.DeviceType;
            this.deviceID = configuration.DeviceId;
            terminalID = configuration.CommunicationChannel;
            CreateFolders();
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write("Begin Install");
            int error = reader.Init();
            log.Write($"Begin Install: Init result: {error}");
            if (error == 0)
            {
                done = true;
            }
            return done;
        }

        public void EndInstall()
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            isTerminalReady = true;
            log.Write("EndInstall()");
        }

        public void Notify(int notificationId)
        { }

        public Card OpenInputDialog(IntPtr windowHandle, TransactionType transactionType, Card card)
        {
            return card;
        }

        public Receipts RepeatReceipt()
        {
            return new Receipts(new Receipt(reader.GetReceipt()));
        }

        public void SetDisplayLanguage(CultureInfo cultureInfo)
        {
        }

        public void SetParameter(Parameter parameter)
        {
        }

        public bool SupportsCreditCards()
        {
            return false;
        }

        public bool SupportsCustomerCards()
        {
            return false;
        }

        public bool SupportsDebitCards()
        {
            return true;
        }

        public bool SupportsElectronicPurseCards()
        {
            return true;
        }

        public void AllowCards(CardIssuerCollection issuers)
        {
            
        }

        public bool IsTerminalReady()
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write($"IsTerminalReady({isTerminalReady.ToString()})");
            return isTerminalReady;
        }

        public void Cancel()
        {
            
        }

        public TransactionResult Credit(TransactionData creditData)
        {
            return Debit(creditData);
        }

        public TransactionResult Credit(TransactionData creditData, Card card)
        {
            return Debit(creditData);
        }

        public TransactionResult Debit(TransactionData debitData)
        {
            _inTransaction = true;
            transactionCanceled = false;
            transactionDataContainer = debitData;
            bool transactionResultDone = false;
            lastTransaction = new TransactionFailedResult(TransactionType.Debit, DateTime.Now);
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write($"Debit(Amount: {debitData.Amount}, RefID: {debitData.ReferenceId})");
            SQLConnect sql = new SQLConnect();
            Card card = Card.NoCard;
            if (isTerminalReady)
            {
                if (sql.IsSQLOnline())
                {
                    try
                    {
                        int error = reader.Purchase((int)(debitData.Amount * 100));
                        int temp = 0;
                        if (error == 0)
                        {
                            transactionResultDone = true;
                        }
                        else
                        {
                            OnAction(new ActionEventArgs($"Response Code: {error}", ActionType.DisplayCustomerMessage));
                        }
                    }
                    catch (Exception e)
                    {
                        log.Write("Debit Exception: " + e.Message);
                        _inTransaction = false;
                        OnTrace(TraceLevel.Error, "EPI Exception:" + e.Message);
                        lastTransaction = new TransactionFailedResult(TransactionType.Debit, DateTime.Now);
                    }
                    finally
                    {
                        log.Write("Finaly");
                        log.Write($"Transaction Done: {transactionResultDone}");
                        if (transactionResultDone)
                        {
                            if (debitData is ParkingTransactionData parkingTransactionData)
                            {
                                card = new CreditCard(Encoding.Default.GetString(reader.outStructure.pan), Encoding.Default.GetString(reader.outStructure.expiry), new CardIssuer(Encoding.Default.GetString(reader.outStructure.cardType)));
                                log.Write($"Finaly RefID:{parkingTransactionData.ReferenceId}, Ticket: {parkingTransactionData.TicketId}, Amount: {(int)parkingTransactionData.Amount}, CardNR(PAN): {card.Number} \nReceipt: \n{reader.GetReceipt()}");
                                sql.AddLinePurch(this.deviceID, parkingTransactionData.ReferenceId, parkingTransactionData.TicketId, reader.GetReceipt(), (int)parkingTransactionData.Amount, card.Number);
                                TransactionDoneResult doneResult = new TransactionDoneResult(TransactionType.Debit, DateTime.Now);
                                doneResult.ReceiptPrintoutMandatory = false;
                                doneResult.Receipts = new Receipts(new Receipt(reader.GetReceipt()), new Receipt(reader.GetReceipt()));
                                doneResult.Amount = (int)parkingTransactionData.Amount;
                                doneResult.Card = card;
                                doneResult.AuthorizationNumber = Encoding.Default.GetString(reader.outStructure.authCode);
                                doneResult.TransactionNumber = Encoding.Default.GetString(reader.outStructure.rrn);
                                doneResult.CustomerDisplayText = Encoding.Default.GetString(reader.outStructure.text_message); ;
                                lastTransaction = doneResult;
                                OnAction(new ActionEventArgs(lastTransaction.CustomerDisplayText, ActionType.DisplayCustomerMessage));
                            }
                            else
                            {
                                card = new CreditCard(Encoding.Default.GetString(reader.outStructure.pan), Encoding.Default.GetString(reader.outStructure.expiry), new CardIssuer(Encoding.Default.GetString(reader.outStructure.cardType)));
                                log.Write($"Finaly RefID:{debitData.ReferenceId}, Amount: {(int)debitData.Amount}, CardNR(PAN): {card.Number} \n\tReceipt: \n\t{reader.GetReceipt()}");
                                sql.AddLinePurch(this.deviceID, debitData.ReferenceId, "", reader.GetReceipt(), (int)debitData.Amount, card.Number);
                                TransactionDoneResult doneResult = new TransactionDoneResult(TransactionType.Debit, DateTime.Now);
                                doneResult.ReceiptPrintoutMandatory = false;
                                doneResult.Receipts = new Receipts(new Receipt(reader.GetReceipt()), new Receipt(reader.GetReceipt()));
                                doneResult.Amount = (int)debitData.Amount;
                                doneResult.Card = card;
                                doneResult.AuthorizationNumber = Encoding.Default.GetString(reader.outStructure.authCode);
                                doneResult.TransactionNumber = Encoding.Default.GetString(reader.outStructure.rrn);
                                doneResult.CustomerDisplayText = Encoding.Default.GetString(reader.outStructure.text_message);
                                lastTransaction = doneResult;
                                OnAction(new ActionEventArgs(lastTransaction.CustomerDisplayText, ActionType.DisplayCustomerMessage));
                            }
                            CountTransaction counter = new CountTransaction(this.deviceID);
                            int tr = counter.Get();
                            if (tr < 5)
                            {
                                ManualSettelment();
                            }
                            else
                            {
                                counter.Send(tr--);
                            }
                        }
                        else
                        {
                            log.Write($"Finaly RefID:{debitData.ReferenceId}, \nReceipt: \n{reader.GetReceipt()}, \nAmount: {(int)debitData.Amount} ");
                            lastTransaction = new TransactionFailedResult(TransactionType.Debit, DateTime.Now);
                            lastTransaction.CustomerDisplayText = String.IsNullOrEmpty(Encoding.Default.GetString(reader.outStructure.text_message)) ? Encoding.Default.GetString(reader.outStructure.text_message) : "Ошибка!";
                            lastTransaction.Receipts = new Receipts(new Receipt(reader.GetReceipt()));
                            OnAction(new ActionEventArgs(lastTransaction.CustomerDisplayText, ActionType.DisplayCustomerMessage));
                        }
                    }
                }
                else
                {
                    log.Write($"No connection to SQL. RefID:{debitData.ReferenceId}");
                    lastTransaction = new TransactionFailedResult(TransactionType.Debit, DateTime.Now);
                    OnTrace(TraceLevel.Error, $"No connection to SQL. RefID:{debitData.ReferenceId}");
                }
            }
            else
            {
                log.Write($"Terminal not ready. RefID:{debitData.ReferenceId}");
                lastTransaction = new TransactionFailedResult(TransactionType.Debit, DateTime.Now);
                OnTrace(TraceLevel.Error, $"Terminal not ready. RefID:{debitData.ReferenceId}");
            }
            return lastTransaction;
        }

        public TransactionResult Debit(TransactionData debitData, Card card)
        {
            return Debit(debitData);
        }

        public void ExecuteCommand(int commandId)
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write($"Execute Command({commandId.ToString()})");
            if (_activated == true)
            {
                switch (commandId)
                {
                    case 1001:
                        OnCardInserted();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                OnAction(new ActionEventArgs("Отмените операцию наличными! \n   (Нажмите отмена)", ActionType.DisplayCustomerMessage));
            }
        }

        public void ExecuteCommand(int commandId, object parameterValue)
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write($"ExecuteCommand() command {commandId.ToString()}, parameterValue: {parameterValue.ToString()}");
        }

        public CommandDefinitionCollection GetCommands()
        {
            CommandDefinitionCollection commandDefinitionCollection = new CommandDefinitionCollection();
            CommandDefinition cardBtn = new CommandDefinition(1001, "Карта");
            commandDefinitionCollection.Add(cardBtn);
            return commandDefinitionCollection;
        }

        public TransactionResult GetLastTransaction()
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write("GetLastTransaction()");
            if (lastTransaction == null)
                lastTransaction = new TransactionFailedResult(TransactionType.Debit, DateTime.Now);
            return lastTransaction;
        }

        public Card GetManualCard(Card card)
        {
            return card;
        }

        public Card GetManualCard(Card card, string paymentType)
        {
            return card;
        }

        
        public ValidationResult ValidateCard()
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write("ValidateCard()");
            return new ValidationResult();
        }

        public ValidationResult ValidateCard(Card card)
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write("ValidateCard()");
            return new ValidationResult();
        }

        public TransactionResult Void(TransactionDoneResult debitResultData)
        {
            return new TransactionFailedResult(debitResultData.TransactionType);
        }

        #endregion

        #region ICardHendling
        public void Activate(decimal amount)
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            //_transactionCancelled = false;
            _activated = true;
            log.Write($"Activate ({amount} Lei)");
        }

        public void Deactivate()
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write("Deactivate()");
            _activated = false;
        }

        public void ReleaseCard()
        {
            Logger log = new Logger(this.ShortName, this.deviceID);
            log.Write("ReleaseCard()");
            OnCardRemoved();
            _activated = false;
        }
        #endregion

        #region ISettlement
        SettlementSettings ISettlement.Settings => throw new NotImplementedException();
        public SettlementResult Settlement(SettlementInput settlementData)
        {
            bool settlementResult = false;
            SettlementResult settlResult = new SettlementResult();
            int error = reader.CloseBatch();
            int temp = 0;
            if (error == 0)
            {
                settlementResult = true;
            }
            else
            {
                Logger log = new Logger(this.ShortName, this.deviceID);
                log.Write($"Settlement Error: {error}");
            }
            
            return new SettlementResult();
        }
        #endregion

        #region Events
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
        public event EventHandler CancelPressed;
        public event EventHandler CardInserted;
        public event EventHandler CardRemoved;

        private void OnAction(ActionEventArgs args)
        {
            if (Action != null)
                Action(this, args);
        }

        private void OnChoice(ChoiceEventArgs args)
        {
            if (Choice != null)
                Choice(this, args);
        }

        private void OnConfirmation(ConfirmationEventArgs args)
        {
            if (Confirmation != null)
                Confirmation(this, args);
        }

        private void OnDeliveryCheck(DeliveryCheckEventArgs args)
        {
            if (DeliveryCheck != null)
                DeliveryCheck(this, args);
        }

        private void OnErrorOccurred(ErrorOccurredEventArgs args)
        {
            if (ErrorOccurred != null)
                ErrorOccurred(this, args);
        }

        private void OnErrorCleared(ErrorClearedEventArgs args)
        {
            if (ErrorCleared != null)
                ErrorCleared(this, args);
        }

        private void OnIrregularityDetected(IrregularityDetectedEventArgs args)
        {
            if (IrregularityDetected != null)
                IrregularityDetected(this, args);
        }

        private void OnTerminalReadyChanged()
        {
            if (TerminalReadyChanged != null)
                TerminalReadyChanged(this, new EventArgs());
        }

        private void OnJournalize(JournalizeEventArgs args)
        {
            if (Journalize != null)
                Journalize(this, args);
        }

        private void OnTrace(TraceLevel level, string format, params object[] args)
        {
            if (Trace != null)
                Trace(this, new TraceEventArgs(String.Format(CultureInfo.InvariantCulture, format, args), level));
        }

        private void OnCardInserted()
        {
            if (CardInserted != null)
                CardInserted(this, new EventArgs());
        }

        private void OnCardRemoved()
        {
            if (CardRemoved != null)
                CardRemoved(this, new EventArgs());
        }

        private void OnCancelPressed()
        {
            if (CancelPressed != null)
                CancelPressed(this, new EventArgs());
        }
        #endregion

        #region Custom methods
        bool CreateFolders()
        {
            if (!Directory.Exists(StringValue.WorkingDirectory))
            {
                Directory.CreateDirectory(StringValue.WorkingDirectory);
            }
            if (!Directory.Exists($"{StringValue.WorkingDirectory}Log"))
            {
                Directory.CreateDirectory($"{StringValue.WorkingDirectory}Log");
            }
            if (!Directory.Exists($"{StringValue.WorkingDirectory}EPI"))
            {
                Directory.CreateDirectory($"{StringValue.WorkingDirectory}EPI");
            }
            return true;
        }

        void ManualSettelment()
        {
            reader.CloseBatch();
        }

        
        #endregion
    }
}
