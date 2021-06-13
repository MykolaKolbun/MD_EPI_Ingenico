using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD_EPI_Ingenico
{
    class Logger
    {
        string File { get; set; }
        string Path { get; set; }

        public Logger(string fileName, string machineID)
        {
            this.File = fileName;
            this.Path = $@"{StringValue.WorkingDirectory}Log\{File}Trace-{machineID} {DateTime.Now:yyyy-MM-dd}.txt";

        }

        public void Write(string mess)
        {
            try
            {
                StreamWriter sw = new StreamWriter(Path, true, Encoding.UTF8);
                string dateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                sw.WriteLine("{0}: {1}", dateTime, mess);
                sw.Close();
            }
            catch { }
        }
    }
}
