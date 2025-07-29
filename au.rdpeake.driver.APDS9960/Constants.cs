using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace au.rdpeake.driver.APDS9960
{
    internal static class Constants
    {
        /* bit mask */
        public const byte APDS9960_GVALID = 0b00000001;

        /* Gesture parameters */
        public const byte THRESHOLD_OUT = 10;
        public const byte SENSITIVITY_1 = 50;
        public const byte SENSITIVITY_2 = 20;

    }
}
