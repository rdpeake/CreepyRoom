using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace au.rdpeake.driver.APDS9960
{
    public enum Addresses : byte
    {
        Default = 0x39
    }

    public enum LedDriveLevels : byte
    {
        LED_DRIVE_100MA = 0,
        LED_DRIVE_50MA = 1,
        LED_DRIVE_25MA = 2,
        LED_DRIVE_12_5MA = 3
    }

    public enum PGain : byte
    {
        X1 = 0,
        X2 = 1,
        X4 = 2,
        X8 = 3
    }

    public enum GGain : byte
    {
        X1 = 0,
        X2 = 1,
        X4 = 2,
        X8 = 3
    }

    public enum AGain : byte
    {
        X1 = 0,
        X4 = 1,
        X16 = 2,
        X64 = 3
    }

    public enum LEDGain : byte
    {
        X100 = 0, X150 = 2, X200 = 3, X300 = 4
    }

    public enum GestureWaitTime : byte
    {
        None = 0,
        Wait_2_8MS = 1,
        Wait_5_6MS = 2,
        Wait_8_4MS = 3,
        Wait_14_0MS = 4,
        Wait_22_4MS = 5,
        Wait_30_8MS = 6,
        Wait_39_2MS = 7
    }

    public enum BitMask : byte
    {
        APDS9960_PON = 0b00000001,
        APDS9960_AEN = 0b00000010,
        APDS9960_PEN = 0b00000100,
        APDS9960_WEN = 0b00001000,
        APSD9960_AIEN = 0b00010000,
        APDS9960_PIEN = 0b00100000,
        APDS9960_GEN = 0b01000000
    }

    public enum DeviceIds : byte
    {
        ID_1 = 0xAB,
        ID_2 = 0x9C
    }
    [Flags]
    public enum PhotoMask
    {
        Right = 0b00000001,
        Left =  0b00000010,
        Down =  0b00000100,
        Up =    0b00001000
    }

    public enum Direction
    {
        None = 0,
        Left, Right, Up, Down, Near, Far, All
    }

    public enum OperatingMode : byte
    {
        Power,
        AmbientLight,
        Proximity,
        Wait,
        AmbientLightInt,
        ProximityInt,
        Gesture,
        All
    }

    enum States
    {
        None,
        Near,
        Far,
        All
    }

    public enum Register : byte
    {
        ENABLE = 0x80,
        ATIME = 0x81,
        WTIME = 0x83,
        AILTL = 0x84,
        AILTH = 0x85,
        AIHTL = 0x86,
        AIHTH = 0x87,
        PILT = 0x89,
        PIHT = 0x8B,
        PERS = 0x8C,
        CONFIG1 = 0x8D,
        PPULSE = 0x8E,
        CONTROL = 0x8F,
        CONFIG2 = 0x90,
        ID = 0x92,
        STATUS = 0x93,
        CDATAL = 0x94,
        CDATAH = 0x95,
        RDATAL = 0x96,
        RDATAH = 0x97,
        GDATAL = 0x98,
        GDATAH = 0x99,
        BDATAL = 0x9A,
        BDATAH = 0x9B,
        PDATA = 0x9C,
        POFFSET_UR = 0x9D,
        POFFSET_DL = 0x9E,
        CONFIG3 = 0x9F,
        GPENTH = 0xA0,
        GEXTH = 0xA1,
        GCONF1 = 0xA2,
        GCONF2 = 0xA3,
        GOFFSET_U = 0xA4,
        GOFFSET_D = 0xA5,
        GOFFSET_L = 0xA7,
        GOFFSET_R = 0xA9,
        GPULSE = 0xA6,
        GCONF3 = 0xAA,
        GCONF4 = 0xAB,
        GFLVL = 0xAE,
        GSTATUS = 0xAF,
        IFORCE = 0xE4,
        PICLEAR = 0xE5,
        CICLEAR = 0xE6,
        AICLEAR = 0xE7,
        GFIFO_U = 0xFC,
        GFIFO_D = 0xFD,
        GFIFO_L = 0xFE,
        GFIFO_R = 0xFF
    }

}
