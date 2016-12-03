using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace UtilitiesBot.Utilities
{
    public class HashCalculator
    {
        private NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public string CalculateHash(string command, string type)
        {
            string value = command.RemoveCommandPart().Trim();
            Type t = type.Contains("System.Security.Cryptography") ? Type.GetType(type) : Type.GetType("System.Security.Cryptography." + type.ToUpper());
            return value.Hash(t);
        }
    }
}
