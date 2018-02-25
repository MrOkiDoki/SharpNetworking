using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNet
{
    class LockableBool
    {
        public bool Value;

        public static implicit operator bool(LockableBool b)
        {
            return b.Value;
        }

    }
}
