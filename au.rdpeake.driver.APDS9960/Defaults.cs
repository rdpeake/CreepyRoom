﻿using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace au.rdpeake.driver.APDS9960
{
    internal static class Defaults
    {
        public const byte DEFAULT_ATIME = 219;     // 103ms
        public const byte DEFAULT_WTIME = 246;     // 27ms
        public const byte DEFAULT_PROX_PPULSE = 0x87;    // 16us, 8 pulses
        public const byte DEFAULT_GESTURE_PPULSE = 0x89;    // 16us, 10 pulses
        public const byte DEFAULT_POFFSET_UR = 0;       // 0 offset
        public const byte DEFAULT_POFFSET_DL = 0;       // 0 offset      
        public const byte DEFAULT_CONFIG1 = 0x60;    // No 12x wait (WTIME) factor
        public const LedDriveLevels DEFAULT_LDRIVE = LedDriveLevels.LED_DRIVE_100MA;
        public const PGain DEFAULT_PGAIN = PGain.X4;
        public const AGain DEFAULT_AGAIN = AGain.X4;
        public const byte DEFAULT_PILT = 0;       // Low proximity threshold
        public const byte DEFAULT_PIHT = 50;      // High proximity threshold
        public const ushort DEFAULT_AILT = 0xFFFF;  // Force interrupt for calibration
        public const byte DEFAULT_AIHT = 0;
        public const byte DEFAULT_PERS = 0x11;    // 2 consecutive prox or ALS for int.
        public const byte DEFAULT_CONFIG2 = 0x01;    // No saturation interrupts or LED boost  
        public const byte DEFAULT_CONFIG3 = 0;       // Enable all photodiodes, no SAI
        public const byte DEFAULT_GPENTH = 40;      // Threshold for entering gesture mode
        public const byte DEFAULT_GEXTH = 30;      // Threshold for exiting gesture mode    
        public const byte DEFAULT_GCONF1 = 0x40;    // 4 gesture events for int., 1 for exit
        public const GGain DEFAULT_GGAIN = GGain.X4;
        public const LedDriveLevels DEFAULT_GLDRIVE = LedDriveLevels.LED_DRIVE_100MA;
        public const GestureWaitTime DEFAULT_GWTIME = GestureWaitTime.Wait_2_8MS;
        public const byte DEFAULT_GOFFSET = 0;       // No offset scaling for gesture mode
        public const byte DEFAULT_GPULSE = 0xC9;    // 32us, 10 pulses
        public const byte DEFAULT_GCONF3 = 0;       // All photodiodes active during gesture
        public const bool DEFAULT_GIEN = false;       // Disable gesture interrupts
    }
}
