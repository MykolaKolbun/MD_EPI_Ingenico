using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MD_EPI_Ingenico
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
    class IngenicoLib
    {
        [DllImport("C:\\Arcus2\\DLL\\ArcCom.dll", EntryPoint = "ProcessOw", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ProcessOw(ref UseAuth mtauth);

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

        public UseAuth outStructure;

        public int Init()
        {
            /* UserAuth structure initialization */
            outStructure = new UseAuth
            {
                track2 = new byte[60],
                pan = new byte[20],
                expiry = new byte[5],
                pay_acc = new byte[20],
                additional_payment_data = new byte[80],
                amount = new byte[13],
                original_amount = new byte[13],
                currency = new byte[4],
                terminalID = new byte[9],
                rrn = new byte[13],
                authCode = new byte[9],
                responseCode = new byte[4],
                cardType = new byte[80],
                date = new byte[7],
                time = new byte[7],
                payment_data = new byte[50],
                data_to_print = new byte[50],
                home_operator = new byte[50],
                received_text_message = new byte[80],
                text_message = new byte[80],
                AID = new byte[80],
                ApplicationLabel = new byte[80],
                TVR = new byte[80]
            };
            outStructure.TVR.Initialize();
            outStructure.enc_data = new byte[64];

            /* Initialization of new callback */
            CBFunction f = new CBFunction(Handler);
            /* Providing callback-function's pointer to arccom.dll */
            SetAmountUpdateCB(f);
            return 0;
        }

        public int Purchase(double amount)
        {
            outStructure.operType = 1;
            byte[] amt = Encoding.ASCII.GetBytes($"{amount}");
            byte[] cur = Encoding.ASCII.GetBytes("643");
            Array.Copy(amt, outStructure.amount, amt.Length);
            Array.Copy(cur, outStructure.currency, cur.Length);

            /* Lounching operation */
            return ProcessOw(ref outStructure);
        }

        public int CloseBatch()
        {
            outStructure.operType = 7;
            return ProcessOw(ref outStructure);
        }

        public string GetReceipt()
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
