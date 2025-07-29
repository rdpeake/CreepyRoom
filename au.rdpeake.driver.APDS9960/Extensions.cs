using GHIElectronics.TinyCLR.Devices.I2c;
using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace au.rdpeake.driver.APDS9960
{
    public static class Extensions
    {
        public static byte ReadRegister(this I2cDevice device, Register register)
        {
            var writeBuffer = new byte[] { (byte)register };
            var readBuffer = new byte[1];
            device.WriteRead(writeBuffer, readBuffer);
            return readBuffer[0];
        }

        public static void ReadRegister(this I2cDevice device, Register register, byte[] readBuffer, int startIndex, int length)
        {
            var writeBuffer = new byte[] { (byte)register };
            device.WriteRead(writeBuffer, 0, 1, readBuffer, startIndex, length);
        }

        public static void WriteRegister(this I2cDevice device, Register register, byte value)
        {
            var writeBuffer = new byte[] { (byte)register, value };
            device.Write(writeBuffer);
        }
    }
}
