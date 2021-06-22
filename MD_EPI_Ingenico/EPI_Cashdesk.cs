using SkiData.ElectronicPayment;
using SkiData.Parking.ElectronicPayment.Extensions;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

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
        private string _userId = string.Empty;
        private string _userName = string.Empty;
        private string _shiftId = string.Empty;
        private bool isTerminalReady = true;
        private bool _activated = false;
        private bool _inTransaction = false;
        bool transactionCanceled = false;
        string terminalID = string.Empty;
        IngenicoLib reader;
        #endregion

        #region Constructor
        public EPI_Cashdesk()
        {
            _settings.AllowsCancel = false;
            _settings.AllowsCredit = true;
            _settings.AllowsRepeatReceipt = false;
            _settings.AllowsValidateCard = true;
            _settings.AllowsVoid = true;
            _settings.CanSetCardData = true;
            _settings.HasCardReader = true;
            _settings.IsContactless = false;
            _settings.MayPrintReceiptOnRejection = false;
            _settings.NeedsSkidataChipReader = false;
            _settings.RequireReceiptPrinter = false;
            _settings.PaymentCardMayDifferFromAccessCard = true;
        }
        #endregion

        #region IDisposable members

        private bool disposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">if set to <see langword="true"/> the managed resources will be disposed.</param>
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //if (_controlDialog != null)
                    //    _controlDialog.CloseForm();
                }
            }
            disposed = true;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Terminal"/> is reclaimed by garbage collection.
        /// </summary>
        ~EPI_Cashdesk()
        {
            Dispose(false);
        }
        #endregion


        #region ITerminal

        public string Name => "MD_Ingenico.Terminal";

        public string ShortName => "MD_Terminal";

        public Settings Settings
        {
            get { return TerminalSettings; }
        }

        private Settings TerminalSettings
        {
            get { return _settings; }
        }

        public bool BeginInstall(TerminalConfiguration configuration)
        {
            _termConfig = configuration;
            bool done = false;
            reader = new IngenicoLib();
            deviceType = configuration.DeviceType;
            deviceID = configuration.DeviceId;
            terminalID = configuration.CommunicationChannel;
            try
            {
                CreateFolders();
                Logger log = new Logger(ShortName, deviceID);
                log.Write("Begin Install");
                int error = reader.Init();
                log.Write($"Begin Install: Init result: {error}");
                if (error == 0)
                {
                    done = true;
                }
            }catch(Exception e)
            {
                Logger log = new Logger(ShortName, deviceID);
                log.Write($"Begin Install exception {e.Message}");
            }
            return done;
        }

        public void EndInstall()
        {
            Logger log = new Logger(ShortName, deviceID);
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
            Logger log = new Logger(ShortName, deviceID);
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
            Logger log = new Logger(ShortName, deviceID);
            log.Write($"Debit(Amount: {debitData.Amount}, RefID: {debitData.ReferenceId})");
            SQLConnect sql = new SQLConnect();
            Card card = Card.NoCard;
            if (isTerminalReady)
            {
                if (sql.IsSQLOnline())
                {
                    try
                    {
                        int error = reader.Purchase((int)(debitData.Amount));
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
                        byte[] cardType = new byte[20];
                        Array.Copy(reader.outStructure.cardType, cardType, cardType.Length);
                        if (transactionResultDone)
                        {
                            if (debitData is ParkingTransactionData parkingTransactionData)
                            {
                                card = new CreditCard(Encoding.Default.GetString(reader.outStructure.pan), Encoding.Default.GetString(reader.outStructure.expiry), new CardIssuer(Encoding.Default.GetString(cardType)));
                                log.Write($"Finaly RefID:{parkingTransactionData.ReferenceId}, Ticket: {parkingTransactionData.TicketId}, Amount: {(int)parkingTransactionData.Amount}, CardNR(PAN): {card.Number} \nReceipt: \n{reader.GetReceipt()}");
                                sql.AddLinePurch(deviceID, parkingTransactionData.ReferenceId, parkingTransactionData.TicketId, reader.GetReceipt(), (int)parkingTransactionData.Amount, card.Number);
                                TransactionDoneResult doneResult = new TransactionDoneResult(TransactionType.Debit, DateTime.Now)
                                {
                                    ReceiptPrintoutMandatory = false,
                                    Receipts = new Receipts(new Receipt(reader.GetReceipt()), new Receipt(reader.GetReceipt())),
                                    Amount = (int)parkingTransactionData.Amount,
                                    Card = card,
                                    AuthorizationNumber = Encoding.Default.GetString(reader.outStructure.authCode),
                                    TransactionNumber = Encoding.Default.GetString(reader.outStructure.rrn),
                                    CustomerDisplayText = Encoding.Default.GetString(reader.outStructure.text_message)
                                };
                                ;
                                lastTransaction = doneResult;
                                OnAction(new ActionEventArgs(lastTransaction.CustomerDisplayText, ActionType.DisplayCustomerMessage));
                            }
                            else
                            {
                                card = new CreditCard(Encoding.Default.GetString(reader.outStructure.pan), Encoding.Default.GetString(reader.outStructure.expiry), new CardIssuer(Encoding.Default.GetString(cardType)));
                                log.Write($"Finaly RefID:{debitData.ReferenceId}, Amount: {(int)debitData.Amount}, CardNR(PAN): {card.Number} \n\tReceipt: \n\t{reader.GetReceipt()}");
                                sql.AddLinePurch(deviceID, debitData.ReferenceId, "", reader.GetReceipt(), (int)debitData.Amount, card.Number);
                                TransactionDoneResult doneResult = new TransactionDoneResult(TransactionType.Debit, DateTime.Now)
                                {
                                    ReceiptPrintoutMandatory = false,
                                    Receipts = new Receipts(new Receipt(reader.GetReceipt()), new Receipt(reader.GetReceipt())),
                                    Amount = (int)debitData.Amount,
                                    Card = card,
                                    AuthorizationNumber = Encoding.Default.GetString(reader.outStructure.authCode),
                                    TransactionNumber = Encoding.Default.GetString(reader.outStructure.rrn),
                                    CustomerDisplayText = Encoding.Default.GetString(reader.outStructure.text_message)
                                };
                                lastTransaction = doneResult;
                                OnAction(new ActionEventArgs(lastTransaction.CustomerDisplayText, ActionType.DisplayCustomerMessage));
                            }
                            CountTransaction counter = new CountTransaction(deviceID);
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
                            lastTransaction = new TransactionFailedResult(TransactionType.Debit, DateTime.Now)
                            {
                                CustomerDisplayText = string.IsNullOrEmpty(Encoding.Default.GetString(reader.outStructure.text_message)) ? Encoding.Default.GetString(reader.outStructure.text_message) : "Ошибка!",
                                Receipts = new Receipts(new Receipt(reader.GetReceipt()))
                            };
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
            Logger log = new Logger(ShortName, deviceID);
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
            Logger log = new Logger(ShortName, deviceID);
            log.Write($"ExecuteCommand() command {commandId.ToString()}, parameterValue: {parameterValue.ToString()}");
        }

        public CommandDefinitionCollection GetCommands()
        {
            CommandDefinitionCollection commandDefinitionCollection = new CommandDefinitionCollection();
            CommandDefinition cardBtn = new CommandDefinition(1001, "Card");
            commandDefinitionCollection.Add(cardBtn);
            return commandDefinitionCollection;
        }

        public TransactionResult GetLastTransaction()
        {
            Logger log = new Logger(ShortName, deviceID);
            log.Write("GetLastTransaction()");
            if (lastTransaction == null)
            {
                lastTransaction = new TransactionFailedResult(TransactionType.Debit, DateTime.Now);
            }

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
            Logger log = new Logger(ShortName, deviceID);
            log.Write("ValidateCard()");
            return new ValidationResult();
        }

        public ValidationResult ValidateCard(Card card)
        {
            Logger log = new Logger(ShortName, deviceID);
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
            Logger log = new Logger(ShortName, deviceID);
            _activated = true;
            log.Write(string.Format("Activate({0})", amount));
            if (this.deviceType == DeviceType.PaymentCheckpoint)
                OnCardInserted();
        }

        public void Deactivate()
        {
            Logger log = new Logger(ShortName, deviceID);
            log.Write("Deactivate()");
            _activated = false;
        }

        public void ReleaseCard()
        {
            Logger log = new Logger(ShortName, deviceID);
            log.Write("ReleaseCard()");
            OnCardRemoved();
            _activated = false;
        }
        #endregion

        #region ISettlement
        SettlementSettings ISettlement.Settings => throw new NotImplementedException();
        public SettlementResult Settlement(SettlementInput settlementData)
        {
            SettlementResult settlResult = new SettlementResult();
            try
            {
                int error = reader.CloseBatch();
                if (error == 0)
                {
                }
                else
                {
                    Logger log = new Logger(ShortName, deviceID);
                    log.Write($"Settlement Error: {error}");
                }
            }
            catch (Exception e)
            {
                Logger log = new Logger(ShortName, deviceID);
                log.Write($"Settlement Exception: {e.Message}");
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
            {
                Action(this, args);
            }
        }

        private void OnChoice(ChoiceEventArgs args)
        {
            if (Choice != null)
            {
                Choice(this, args);
            }
        }

        private void OnConfirmation(ConfirmationEventArgs args)
        {
            if (Confirmation != null)
            {
                Confirmation(this, args);
            }
        }

        private void OnDeliveryCheck(DeliveryCheckEventArgs args)
        {
            if (DeliveryCheck != null)
            {
                DeliveryCheck(this, args);
            }
        }

        private void OnErrorOccurred(ErrorOccurredEventArgs args)
        {
            if (ErrorOccurred != null)
            {
                ErrorOccurred(this, args);
            }
        }

        private void OnErrorCleared(ErrorClearedEventArgs args)
        {
            if (ErrorCleared != null)
            {
                ErrorCleared(this, args);
            }
        }

        private void OnIrregularityDetected(IrregularityDetectedEventArgs args)
        {
            if (IrregularityDetected != null)
            {
                IrregularityDetected(this, args);
            }
        }

        private void OnTerminalReadyChanged()
        {
            if (TerminalReadyChanged != null)
            {
                TerminalReadyChanged(this, new EventArgs());
            }
        }

        private void OnJournalize(JournalizeEventArgs args)
        {
            if (Journalize != null)
            {
                Journalize(this, args);
            }
        }

        private void OnTrace(TraceLevel level, string format, params object[] args)
        {
            if (Trace != null)
            {
                Trace(this, new TraceEventArgs(string.Format(CultureInfo.InvariantCulture, format, args), level));
            }
        }

        private void OnCardInserted()
        {
            if (CardInserted != null)
            {
                CardInserted(this, new EventArgs());
            }
        }

        private void OnCardRemoved()
        {
            if (CardRemoved != null)
            {
                CardRemoved(this, new EventArgs());
            }
        }

        private void OnCancelPressed()
        {
            if (CancelPressed != null)
            {
                CancelPressed(this, new EventArgs());
            }
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
