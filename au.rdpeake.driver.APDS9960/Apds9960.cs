using System;
using System.Collections;
using System.Drawing;
using System.Text;
using System.Threading;
using au.rdpeake.driver.APDS9960.Gestures;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace au.rdpeake.driver.APDS9960
{
    public partial class Apds9960 : IDisposable
    {
        //public delegate void EventHandler<T>(T sender, EventArgs e);
        //public event EventHandler<Apds9960> AmbientLightChanged = default;
        //public event EventHandler<Apds9960> ColourChanged = default;
        private static readonly byte FIFO_PAUSE_TIME = 30;
        private byte[] readBuffer = new byte[8];
        private byte[] writeBuffer = new byte[8];

        private I2cDevice BusDevice;


        /// <summary>
        /// Is the object disposed
        /// </summary>
        public bool IsDisposed { get; private set; }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Apds9960(I2cController i2cBus, GpioPin interruptPin)
        {
            var settings = new I2cConnectionSettings((int)Addresses.Default);
            BusDevice = i2cBus.GetDevice(settings);

            if (interruptPin != null)
            {
                //createdPort = true;
                interruptPin.SetDriveMode(GpioPinDriveMode.Input);
                interruptPin.ValueChangedEdge = GpioPinEdge.RisingEdge;
                interruptPin.ValueChanged += InterruptPort_Changed;
            }

            gestureData = new GestureData();

            gestureUdDelta = 0;
            gestureLrDelta = 0;

            gestureUdCount = 0;
            gestureLrCount = 0;

            gestureNearCount = 0;
            gestureFarCount = 0;

            gestureState = 0;

            Initialize();
        }

        private void InterruptPort_Changed(object sender, GpioPinValueChangedEventArgs e)
        {
            //    throw new NotImplementedException();
        }

        public class SensorValue
        {
            public ushort Illuminance { get; internal set; }
            public Color colour { get; internal set; }

        }
        public SensorValue ReadSensor()
        {
            var ambient = ReadAmbientLight();

            var rgbDivisor = 65536 / 256; // come back as 16-bit values (ushorts). need to be byte.
            var r = ReadRedLight() / rgbDivisor;
            var g = ReadGreenLight() / rgbDivisor;
            var b = ReadBlueLight() / rgbDivisor;
            var a = ReadAmbientLight() / rgbDivisor;
            var colour = Color.FromArgb(a, r, g, b);

            return new SensorValue { colour = colour, Illuminance = ambient };
        }

        private void Initialize()
        {
            var id = BusDevice.ReadRegister(Register.ID);

            SetMode(OperatingMode.All, false);

            /* Set default values for ambient light and proximity Register */
            BusDevice.WriteRegister(Register.ATIME, Defaults.DEFAULT_ATIME);
            BusDevice.WriteRegister(Register.WTIME, Defaults.DEFAULT_WTIME);
            BusDevice.WriteRegister(Register.PPULSE, Defaults.DEFAULT_PROX_PPULSE);
            BusDevice.WriteRegister(Register.POFFSET_UR, Defaults.DEFAULT_POFFSET_UR);
            BusDevice.WriteRegister(Register.POFFSET_DL, Defaults.DEFAULT_POFFSET_DL);
            BusDevice.WriteRegister(Register.CONFIG1, Defaults.DEFAULT_CONFIG1);
            SetLEDDrive(Defaults.DEFAULT_LDRIVE);

            SetProximityGain(Defaults.DEFAULT_PGAIN);
            SetAmbientLightGain(Defaults.DEFAULT_AGAIN);
            SetProxIntLowThresh(Defaults.DEFAULT_PILT);
            SetProxIntHighThresh(Defaults.DEFAULT_PIHT);

            SetLightIntLowThreshold(Defaults.DEFAULT_AILT);

            SetLightIntHighThreshold(Defaults.DEFAULT_AIHT);

            BusDevice.WriteRegister(Register.PERS, Defaults.DEFAULT_PERS);

            BusDevice.WriteRegister(Register.CONFIG2, Defaults.DEFAULT_CONFIG2);

            BusDevice.WriteRegister(Register.CONFIG3, Defaults.DEFAULT_CONFIG3);

            SetGestureEnterThresh(Defaults.DEFAULT_GPENTH);

            SetGestureExitThresh(Defaults.DEFAULT_GEXTH);

            BusDevice.WriteRegister(Register.GCONF1, Defaults.DEFAULT_GCONF1);

            SetGestureGain(Defaults.DEFAULT_GGAIN);

            SetGestureLEDDrive(Defaults.DEFAULT_GLDRIVE);

            SetGestureWaitTime(Defaults.DEFAULT_GWTIME);

            BusDevice.WriteRegister(Register.GOFFSET_U, Defaults.DEFAULT_GOFFSET);
            BusDevice.WriteRegister(Register.GOFFSET_D, Defaults.DEFAULT_GOFFSET);
            BusDevice.WriteRegister(Register.GOFFSET_L, Defaults.DEFAULT_GOFFSET);
            BusDevice.WriteRegister(Register.GOFFSET_R, Defaults.DEFAULT_GOFFSET);
            BusDevice.WriteRegister(Register.GPULSE, Defaults.DEFAULT_GPULSE);
            BusDevice.WriteRegister(Register.GCONF3, Defaults.DEFAULT_GCONF3);
            SetGestureIntEnable(Defaults.DEFAULT_GIEN);
        }

        private byte GetMode()
        {
            return BusDevice.ReadRegister(Register.ENABLE);
        }

        private void SetMode(OperatingMode mode, bool enable)
        {
            byte reg_val;

            /* Read current ENABLE register */
            reg_val = GetMode();

            if (reg_val == 0xFF)
            {
                return; //ToDo exception
            }

            /* Change bit(s) in ENABLE register */
            if (mode >= 0 && (byte)mode <= 6)
            {
                if (enable)
                {
                    reg_val |= (byte)(1 << (byte)mode);
                }
                else
                {
                    reg_val &= (byte)~(1 << (byte)mode);
                }
            }
            else if (mode == OperatingMode.All)
            {
                if (enable)
                {
                    reg_val = 0x7F;
                }
                else
                {
                    reg_val = 0x00;
                }
            }

            /* Write value back to ENABLE register */
            BusDevice.WriteRegister(Register.ENABLE, reg_val);
        }

        public void EnableLightSensor(bool interrupts)
        {
            /* Set default gain, interrupts, enable power, and enable sensor */
            SetAmbientLightGain(Defaults.DEFAULT_AGAIN);

            SetAmbientLightIntEnable(interrupts);

            EnablePower(true);
            SetMode(OperatingMode.AmbientLight, true);
        }

        public void DisableLightSensor()
        {
            SetAmbientLightIntEnable(false);
            SetMode(OperatingMode.AmbientLight, false);
        }

        public void EnableProximitySensor(bool interrupts)
        {
            /* Set default gain, LED, interrupts, enable power, and enable sensor */
            SetProximityGain(Defaults.DEFAULT_PGAIN);
            SetLEDDrive(Defaults.DEFAULT_LDRIVE);

            if (interrupts)
            {
                SetProximityIntEnable(true);
            }
            else
            {
                SetProximityIntEnable(false);
            }
            EnablePower(true);
            SetMode(OperatingMode.Proximity, true);
        }

        public void DisableProximitySensor()
        {
            SetProximityIntEnable(false);
            SetMode(OperatingMode.Proximity, false);
        }

        public void EnablePower(bool enable)
        {
            SetMode(OperatingMode.Power, enable);
        }

        protected ushort ReadAmbientLight()
        {
            byte val = BusDevice.ReadRegister(Register.CDATAL);

            byte val_byte = BusDevice.ReadRegister(Register.CDATAH);

            return (ushort)(val + (val_byte << 8));
        }

        protected ushort ReadRedLight()
        {
            byte val = BusDevice.ReadRegister(Register.RDATAL);

            byte val_byte = BusDevice.ReadRegister(Register.RDATAH);

            return (ushort)(val + (val_byte << 8));
        }

        protected ushort ReadGreenLight()
        {
            byte val = BusDevice.ReadRegister(Register.GDATAL);

            byte val_byte = BusDevice.ReadRegister(Register.GDATAH);

            return (ushort)(val + (val_byte << 8));
        }

        protected ushort ReadBlueLight()
        {
            byte val = BusDevice.ReadRegister(Register.BDATAL);

            byte val_byte = BusDevice.ReadRegister(Register.BDATAH);

            return (ushort)(val + (val_byte << 8));
        }

        public byte ReadProximity()
        {
            return BusDevice.ReadRegister(Register.PDATA);
        }

        public byte GetProxIntLowThresh()
        {
            return BusDevice.ReadRegister(Register.PILT);
        }

        public void SetProxIntLowThresh(byte threshold)
        {
            BusDevice.WriteRegister(Register.PILT, threshold);
        }

        public byte GetProxIntHighThresh()
        {
            return BusDevice.ReadRegister(Register.PIHT);
        }

        public void SetProxIntHighThresh(byte threshold)
        {
            BusDevice.WriteRegister(Register.PIHT, threshold);
        }

        public LedDriveLevels GetLEDDrive()
        {
            byte val = BusDevice.ReadRegister(Register.CONTROL);

            /* Shift and mask out LED drive bits */
            val = (byte)((val >> 6) & 0b00000011);
            
            return (LedDriveLevels)val;
        }

        public bool SetLEDDrive(LedDriveLevels edrive)
        {
            byte val = BusDevice.ReadRegister(Register.CONTROL);

            /* Set bits in register to given value */
            byte drive = (byte)((byte)edrive & 0b00000011);
            drive = (byte)(drive << 6);
            val &= 0b00111111;
            val |= drive;

            /* Write register value back into CONTROL register */
            BusDevice.WriteRegister(Register.CONTROL, val);

            return true;
        }

        public PGain GetProximityGain()
        {
            byte val = BusDevice.ReadRegister(Register.CONTROL);

            /* Shift and mask out PDRIVE bits */
            val = (byte)((val >> 2) & 0b00000011);

            return (PGain)val;
        }

        public void SetProximityGain(PGain edrive)
        {
            byte val = BusDevice.ReadRegister(Register.CONTROL);

            /* Set bits in register to given value */
            byte drive = (byte)((byte)edrive & 0b00000011);
            drive = (byte)(drive << 2);
            val &= 0b11110011;
            val |= drive;

            /* Write register value back into CONTROL register */
            BusDevice.WriteRegister(Register.CONTROL, val);
        }

        private AGain GetAmbientLightGain()
        {
            byte val = BusDevice.ReadRegister(Register.CONTROL);

            /* Shift and mask out ADRIVE bits */
            val &= 0b00000011;

            return (AGain)val;
        }

        private void SetAmbientLightGain(AGain edrive)
        {
            byte val = BusDevice.ReadRegister(Register.CONTROL);

            /* Set bits in register to given value */
            byte drive = (byte)((byte)edrive & 0b00000011);
            val &= 0b11111100;
            val |= drive;

            BusDevice.WriteRegister(Register.CONTROL, val);
        }

        private LEDGain GetLEDBoost()
        {
            byte val = BusDevice.ReadRegister(Register.CONFIG2);

            /* Shift and mask out LED_BOOST bits */
            val = (byte)((val >> 4) & 0b00000011);

            return (LEDGain)val;
        }

        private void SetLEDBoost(LEDGain eboost)
        {
            byte val = BusDevice.ReadRegister(Register.CONFIG2);

            /* Set bits in register to given value */
            byte boost = (byte)((byte)eboost & 0b00000011);
            boost = (byte)(boost << 4);
            val &= 0b11001111;
            val |= boost;

            BusDevice.WriteRegister(Register.CONFIG2, val);
        }

        private bool GetProxGainCompEnable()
        {
            byte val = BusDevice.ReadRegister(Register.CONFIG3);

            /* Shift and mask out PCMP bits */
            val = (byte)((val >> 5) & 0b00000001);

            return val == 1;
        }

        private void SetProxGainCompEnable(bool benable)
        {
            byte val = BusDevice.ReadRegister(Register.CONFIG3);

            /* Set bits in register to given value */
            byte enable = (byte)(benable ? 0b00000001 : 0);
            enable = (byte)(enable << 5);
            val &= 0b11011111;
            val |= enable;

            /* Write register value back into CONFIG3 register */
            BusDevice.WriteRegister(Register.CONFIG3, val);
        }

        private PhotoMask GetProxPhotoMask()
        {
            byte val = BusDevice.ReadRegister(Register.CONFIG3);

            /* Mask out photodiode enable mask bits */
            val &= 0b00001111;

            return (PhotoMask)val;
        }

        private void SetProxPhotoMask(PhotoMask emask)
        {
            byte val = BusDevice.ReadRegister(Register.CONFIG3);

            /* Set bits in register to given value */
            byte mask = (byte)emask;
            val &= 0b11110000;
            val |= mask;

            BusDevice.WriteRegister(Register.CONFIG3, val);
        }

        private ushort GetLightIntLowThreshold()
        {
            var threshold = BusDevice.ReadRegister(Register.AILTL);

            var val_byte = BusDevice.ReadRegister(Register.AILTH);

            return (byte)(threshold + (val_byte << 8));
        }

        private void SetLightIntLowThreshold(ushort threshold)
        {
            byte val_low;
            byte val_high;

            /* Break 16-bit threshold into 2 8-bit values */
            val_low = (byte)(threshold & 0x00FF);
            val_high = (byte)((threshold & 0xFF00) >> 8);

            BusDevice.WriteRegister(Register.AILTL, val_low);
            BusDevice.WriteRegister(Register.AILTL, val_high);
        }

        private ushort GetLightIntHighThreshold()
        {
            var threshold = BusDevice.ReadRegister(Register.AIHTL);

            var val_byte = BusDevice.ReadRegister(Register.AIHTH);

            return (byte)(threshold + (val_byte << 8));
        }

        private void SetLightIntHighThreshold(ushort threshold)
        {
            /* Break 16-bit threshold into 2 8-bit values */
            byte val_low = (byte)(threshold & 0x00FF);
            byte val_high = (byte)((threshold & 0xFF00) >> 8);

            BusDevice.WriteRegister(Register.AIHTL, val_low);
            BusDevice.WriteRegister(Register.AIHTH, val_high);
        }

        private byte GetProximityIntLowThreshold()
        {
            return BusDevice.ReadRegister(Register.PILT);
        }

        private void SetProximityIntLowThreshold(byte threshold)
        {
            BusDevice.WriteRegister(Register.PILT, threshold);
        }

        private byte GetProximityIntHighThreshold()
        {
            return BusDevice.ReadRegister(Register.PIHT);
        }

        private void SetProximityIntHighThreshold(byte threshold)
        {
            BusDevice.WriteRegister(Register.PIHT, threshold);
        }

        private byte GetAmbientLightIntEnable()
        {
            byte val = BusDevice.ReadRegister(Register.ENABLE);

            /* Shift and mask out AIEN bit */
            val = (byte)((val >> 4) & 0b00000001);

            return val;
        }

        private void SetAmbientLightIntEnable(bool enable)
        {
            byte val = BusDevice.ReadRegister(Register.ENABLE);

            /* Set bits in register to given value */
            byte data = (byte)(enable ? 0x1 : 0x0);
            data &= 0b00000001;
            data = (byte)(data << 4);
            val &= 0b11101111;
            val |= data;

            BusDevice.WriteRegister(Register.ENABLE, val);
        }

        private byte GetProximityIntEnable()
        {
            byte val = BusDevice.ReadRegister(Register.ENABLE);

            /* Shift and mask out PIEN bit */
            val = (byte)((val >> 5) & 0b00000001);

            return val;
        }

        private void SetProximityIntEnable(bool data)
        {
            byte val = BusDevice.ReadRegister(Register.ENABLE);

            /* Set bits in register to given value */
            byte enable = (byte)(data ? 0x1 : 0x0);
            enable = (byte)(enable << 5);
            val &= 0b11011111;
            val |= enable;

            BusDevice.WriteRegister(Register.ENABLE, val);
        }

        private void ClearAmbientLightInt()
        {
            BusDevice.ReadRegister(Register.AICLEAR);
        }

        private void ClearProximityInt()
        {
            BusDevice.ReadRegister(Register.PICLEAR);
        }
    }
}
