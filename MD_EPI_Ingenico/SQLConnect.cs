using System;
using System.Data;
using System.Data.SqlClient;

namespace MD_EPI_Ingenico
{
    class SQLConnect
    {
        string connectionString;
        SqlConnection conn;
        SqlCommand cmd;
        bool demo = true;
        public SQLConnect()
        {

            string compName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            string srvName = compName.Split('-')[0] + "-01";
            srvName = ".local";
            connectionString = string.Format(StringValue.SQLServerConnectionString, srvName);
        }

        public void AddLinePurch(string devID, string trID, string tckNR, string rb, int amnt, string crdNR)
        {
            if (!demo)
            {
                conn = new SqlConnection(connectionString);
                conn.Open();
                cmd = new SqlCommand("AddTransaction", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DeviceID", devID);
                cmd.Parameters.AddWithValue("@TransactionType", 1);
                cmd.Parameters.AddWithValue("@TransactionNR", trID);
                cmd.Parameters.AddWithValue("@ReceiptBody", rb);
                cmd.Parameters.AddWithValue("@Amount", amnt);
                cmd.Parameters.AddWithValue("@TicketNR", tckNR);
                cmd.Parameters.AddWithValue("@CardNR", crdNR);
                cmd.Parameters.AddWithValue("@IsPrinted", 0);
                try
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    reader.Close();
                    cmd.Dispose();
                }
                finally
                {
                    conn.Close();
                    cmd.Dispose();
                }
            }
        }

        public void AddLineSettlement(string devID, string trID, string rb)
        {
            if (!demo)
            {
                conn = new SqlConnection(connectionString);
                conn.Open();
                cmd = new SqlCommand("AddTransaction", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DeviceID", devID);
                cmd.Parameters.AddWithValue("@TransactionType", 2);
                cmd.Parameters.AddWithValue("@TransactionNR", trID);
                cmd.Parameters.AddWithValue("@ReceiptBody", rb);
                cmd.Parameters.AddWithValue("@Amount", 0);
                cmd.Parameters.AddWithValue("@TicketNR", " ");
                cmd.Parameters.AddWithValue("@CardNR", " ");
                cmd.Parameters.AddWithValue("@IsPrinted", 0);
                try
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    reader.Close();
                    cmd.Dispose();
                }
                finally
                {
                    conn.Close();
                    cmd.Dispose();
                }
            }
        }

        /// <summary>
        /// Check connection to SQL instance
        /// </summary>
        /// <returns>connection state</returns>
        public bool IsSQLOnline()
        {
            if (!demo)
            {
                bool isOnline = false;
                try
                {
                    conn = new SqlConnection(connectionString);
                    conn.Open();
                    isOnline = true;
                    //conn.Close();
                }
                catch (SqlException)
                {
                    isOnline = false;
                }
                return isOnline;
            }
            else
            {
                return true;
            }
        }
    }
}
