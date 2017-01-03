using NLog;
using System;

namespace UtilitiesBot.Utilities
{
    public class UnixTimeStampInstantAnswer : IInstantAnswer
    {
        private NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public string GetInstantAnswer(string question)
        {
            string value = question.RemoveCommandPart().Trim();
            DateTime d = DateTime.Now;
            string res = "";
            try
            {
                if (string.IsNullOrEmpty(value) || string.Equals(value, "now", StringComparison.CurrentCultureIgnoreCase))
                    d = DateTime.Now;
                else
                    d = DateTime.ParseExact(value, "dd.MM.yyyy HH:mm:ss", null);
                res = d.ToUnixTimestamp().ToString();
            }
            catch (FormatException fe)
            {
                logger.Error(fe);
                res = "Format for the command must be: dd.MM.yyyy HH:mm:ss";
            }
            return res;
        }
    } public class EpochConverterInstantAnswer : IInstantAnswer
    {
        private NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public string GetInstantAnswer(string question)
        {
            string value = question.RemoveCommandPart().Trim();
            
            string res = "";
            try
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var d = epoch.AddMilliseconds(int.Parse(value));
                res = d.ToString("dd.MM.yyyy HH:mm:ss");
            }
            catch (FormatException fe)
            {
                logger.Error(fe);
            }
            return res;
        }
    }
}
