using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace au.rdpeake.driver.APDS9960.Gestures
{
    [Flags]
    internal enum GestureStatus : byte
    {
        Valid = 1 << 0,
        Overflow = 1 << 1
    }
}
