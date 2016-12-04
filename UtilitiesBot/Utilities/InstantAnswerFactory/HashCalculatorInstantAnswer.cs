using System;
using NLog;

namespace UtilitiesBot.Utilities
{
    public class HashCalculatorInstantAnswer : IInstantAnswer
    {
        private NLog.Logger logger = LogManager.GetCurrentClassLogger();
        string msgLocal = "Proper format for /hash command is /hash sha256 test";

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
                logger.Error(ior);
                msgLocal = "Proper format for /hash command is /hash sha256 test";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return msgLocal;
        }
    }
}
