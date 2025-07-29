using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace au.rdpeake.driver.APDS9960.Gestures
{
    internal class GestureData
    {
        public byte[] UData { get; set; } = new byte[32];
        public byte[] DData { get; set; } = new byte[32];
        public byte[] LData { get; set; } = new byte[32];
        public byte[] RData { get; set; } = new byte[32];
        public byte Index { get; set; }
        public byte TotalGestures { get; set; }
        public byte InThreshold { get; set; }
        public byte OutThreshold { get; set; }
    }
}
