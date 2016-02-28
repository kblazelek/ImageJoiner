using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageJoiner.CustomExceptions
{
    class WrongFileNameException : Exception
    {
        public WrongFileNameException()
        {
        }

        public WrongFileNameException(string message) : base(message)
        {
        }

        public WrongFileNameException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
