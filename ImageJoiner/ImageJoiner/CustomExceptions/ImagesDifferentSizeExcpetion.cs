using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageJoiner.CustomExceptions
{
    class ImagesDifferentSizeExcpetion : Exception
    {
        public ImagesDifferentSizeExcpetion()
        {
        }

        public ImagesDifferentSizeExcpetion(string message) : base(message)
        {
        }

        public ImagesDifferentSizeExcpetion(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
