using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilitiesBot.Utilities
{
    public class UnixTimeStamp
    {
        private NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public string ConvertCommandToUnixTime(string command) {
            string value = command.RemoveCommandPart().Trim();
            DateTime d = DateTime.Now;
            string res = "";
            try
            {
                if (string.Equals(value, "now", StringComparison.CurrentCultureIgnoreCase))
                    d = DateTime.Now;
                else
                    d = DateTime.ParseExact(value, "dd.MM.yyyy HH:mm:ss", null);
                res =d.ToUnixTimestamp().ToString();
            }
            catch (FormatException fe)
            {
                logger.Error(fe);
                res = "Format for the command must be: dd.MM.yyyy HH:mm:ss";
            }
            return res;
        }


    }
}
