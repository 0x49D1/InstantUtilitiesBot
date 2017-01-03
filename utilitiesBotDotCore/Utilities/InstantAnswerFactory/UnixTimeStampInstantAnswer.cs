using Microsoft.Extensions.Logging;
using System;

namespace utilitiesBotDotCore.Utilities
{
    public class UnixTimeStampInstantAnswer : IInstantAnswer
    {

        ILogger _logger;
        public UnixTimeStampInstantAnswer(ILogger logger)
        {
            _logger = logger;
        }

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
                _logger.LogError(fe.ToString());
                res = "Format for the command must be: dd.MM.yyyy HH:mm:ss";
            }
            return res;
        }
    }
}
