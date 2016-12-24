using Microsoft.Extensions.Logging;
using System;

namespace utilitiesBotDotCore.Utilities
{
    public class HashCalculatorInstantAnswer : IInstantAnswer
    {
       
        string msgLocal = "Proper format for /hash command is /hash sha256 test";
        ILogger _logger;
        public HashCalculatorInstantAnswer(ILogger logger)
        {
            _logger = logger;
        }

        public string GetInstantAnswer(string question)
        {
            try
            {
                string type = question.Split(' ')[0];
                question = question.Replace(type, "");
                if (string.IsNullOrEmpty(type))
                    return "Type for the hash was not set";
                string value = question.RemoveCommandPart().Trim();
                Type t = type.Contains("System.Security.Cryptography")
                    ? Type.GetType(type)
                    : Type.GetType("System.Security.Cryptography." + type.ToUpper());
                return value.Hash(t);
            }
            catch (IndexOutOfRangeException ior)
            {
                _logger.LogError(ior.ToString());
                msgLocal = "Proper format for /hash command is /hash sha256 test";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return msgLocal;
        }
    }
}
