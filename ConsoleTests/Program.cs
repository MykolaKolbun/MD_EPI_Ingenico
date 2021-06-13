using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTests
{
    /* UseAuth structure declaration */
    [StructLayout(LayoutKind.Sequential)]
    internal struct UseAuth
    {
        public int handle;
        public int abg_id;
        public int operType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public byte[] track2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] pan;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] expiry;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] pay_acc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] additional_payment_data;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public byte[] amount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public byte[] original_amount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] currency;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] terminalID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public byte[] rrn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] authCode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] responseCode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] cardType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] date;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] time;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public byte[] payment_data;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public byte[] data_to_print;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public byte[] home_operator;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] received_text_message;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] text_message;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] AID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] ApplicationLabel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] TVR;
        public int system_res;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] enc_data;
    }

    /* Declaration of type for callback funcrion */
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CBFunction(string pan, IntPtr amount);
    class Program
    {
        [DllImport("C:\\Arcus2\\DLL\\ArcCom.dll", EntryPoint = "ProcessOw", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ProcessOw(ref UseAuth mtauth);

        private static CBFunction cbfunc;

        [DllImport("C:\\Arcus2\\DLL\\ArcCom.dll", EntryPoint = "SetAmountUpdateCB", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetAmountUpdateCB(CBFunction func);

        /* Callback function declaration
         * This function must communicate whith discount-module provide new amount to arccom.dll
         */
        static private int Handler(string pan, IntPtr amount)
        {
            Console.WriteLine("[TEST CALLBACK] Received PAN:  " + pan);
            Console.WriteLine("[TEST CALLBACK] Writed value:  1001.00");

            byte[] new_amount = Encoding.ASCII.GetBytes("100100");
            Marshal.Copy(new_amount, 0, amount, new_amount.Length);
            Marshal.WriteByte(amount, new_amount.Length, 0);

            return 1;
        }
        static void Main(string[] args)
        {
            /* UserAuth structure initialization */
            UseAuth test = new UseAuth();
            test.track2 = new byte[60];
            test.pan = new byte[20];
            test.expiry = new byte[5];
            test.pay_acc = new byte[20];
            test.additional_payment_data = new byte[80];
            test.amount = new byte[13];
            test.original_amount = new byte[13];
            test.currency = new byte[4];
            test.terminalID = new byte[9];
            test.rrn = new byte[13];
            test.authCode = new byte[9];
            test.responseCode = new byte[4];
            test.cardType = new byte[80];
            test.date = new byte[7];
            test.time = new byte[7];
            test.payment_data = new byte[50];
            test.data_to_print = new byte[50];
            test.home_operator = new byte[50];
            test.received_text_message = new byte[80];
            test.text_message = new byte[80];
            test.AID = new byte[80];
            test.ApplicationLabel = new byte[80];
            test.TVR = new byte[80];
            test.TVR.Initialize();
            test.enc_data = new byte[64];

            /* Initialization of new callback */
            CBFunction f = new CBFunction(Handler);
            /* Providing callback-function's pointer to arccom.dll */
            SetAmountUpdateCB(f);

            /* Writing operation's details to UseAuth struct
             * In this example we are trying to perfom operation "Purchase": amount 10.00; currency 643(RUB)
             */
            test.operType = 1;
            byte[] amt = Encoding.ASCII.GetBytes("1000");
            byte[] cur = Encoding.ASCII.GetBytes("643");
            Array.Copy(amt, test.amount, amt.Length);
            Array.Copy(cur, test.currency, cur.Length);

            /* Lounching operation */
            int retval = ProcessOw(ref test);

            /* Writing operation details to console */
            Console.WriteLine("ProcessOw returns[" + retval.ToString() + "]");
            Console.WriteLine("PAN:  " + Encoding.ASCII.GetString(test.pan));
            Console.WriteLine("RRN:  " + Encoding.ASCII.GetString(test.rrn));
            Console.WriteLine("TVR:  " + Encoding.ASCII.GetString(test.TVR));
            Console.WriteLine("AUTH: " + Encoding.ASCII.GetString(test.authCode));
            Console.WriteLine("RES:  " + Encoding.ASCII.GetString(test.responseCode));
            Console.WriteLine("CARD: " + Encoding.ASCII.GetString(test.cardType));
            Console.WriteLine("AID: " + Encoding.ASCII.GetString(test.AID));
            Console.WriteLine("APP Lable: " + Encoding.ASCII.GetString(test.ApplicationLabel));
            Console.WriteLine("HRS: " + Encoding.ASCII.GetString(test.enc_data));
            Console.WriteLine("Received text: " + Encoding.ASCII.GetString(test.received_text_message));
            Console.WriteLine("Text mess: " + Encoding.ASCII.GetString(test.text_message));
            //test.operType = 7;
            //retval = ProcessOw(ref test);
            Console.ReadKey();
        }
        public static string GetReceipt()
        {
            string path = @"c:\Arcus2\cheq.out";
            string outString = "";
            if (File.Exists(path))
            {
                outString = File.ReadAllText(path);
            }
            return outString;
        }
    }
}
