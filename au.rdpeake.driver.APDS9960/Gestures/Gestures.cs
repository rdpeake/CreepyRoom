using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using au.rdpeake.driver.APDS9960.Gestures;

namespace au.rdpeake.driver.APDS9960
{
    public partial class Apds9960
    {
        private readonly GestureData gestureData;
        private int gestureUdDelta;
        private int gestureLrDelta;
        private int gestureUdCount;
        private int gestureLrCount;
        private int gestureNearCount;
        private int gestureFarCount;
        private States gestureState;

        public bool EnableGestureSensor(bool interrupts)
        {
            /* Enable gesture mode
               Set ENABLE to 0 (power off)
               Set WTIME to 0xFF
               Set AUX to LED_BOOST_100
               Enable PON, WEN, PEN, GEN in ENABLE 
            */
            ResetGestureParameters();
            BusDevice.WriteRegister(Register.WTIME, 0xFF);
            BusDevice.WriteRegister(Register.PPULSE, Defaults.DEFAULT_GESTURE_PPULSE);

            SetLEDBoost(LEDGain.X100);

            SetGestureIntEnable(interrupts);

            SetGestureMode(true);
            EnablePower(true);
            SetMode(OperatingMode.Wait, true);
            SetMode(OperatingMode.Proximity, true);
            SetMode(OperatingMode.Gesture, true);

            return true;
        }

        public void DisableGestureSensor()
        {
            ResetGestureParameters();
            SetGestureIntEnable(false);
            SetGestureMode(false);
            SetMode(OperatingMode.Gesture, false);
        }

        public bool IsGestureAvailable()
        {
            byte val = BusDevice.ReadRegister(Register.GSTATUS);

            /* Shift and mask out GVALID bit */
            val &= Constants.APDS9960_GVALID;

            /* Return true/false based on GVALID bit */
            return val == 1;
        }

        public Direction ReadGesture()
        {
            /* Make sure that power and gesture is on and data is valid */
            if (!IsGestureAvailable() || (GetMode() & 0b01000001) == 0x0)
            {
                return (int)Direction.None;
            }

            /* Keep looping as long as gesture data is valid */
            while (true)
            {
                byte fifo_level;
                byte bytes_read;

                /* Wait some time to collect next batch of FIFO data */
                Thread.Sleep(FIFO_PAUSE_TIME);

                var gstatus = ReadGestureStatusRegister();

                /* If we have valid data, read in FIFO */
                if ((gstatus & GestureStatus.Valid) == GestureStatus.Valid)
                {
                    fifo_level = BusDevice.ReadRegister(Register.GFLVL);

                    /* If there's stuff in the FIFO, read it into our data block */
                    if (fifo_level > 0)
                    {
                        byte len = (byte)(fifo_level * 4);
                        
                        BusDevice.ReadRegister(Register.GFIFO_U, readBuffer, 0, len);

                        bytes_read = len; //ToDo should we have a check> (byte)fifo_data.Length;

                        if (bytes_read < 1)
                        {
                            throw new Exception();
                        }

                        /* If at least 1 set of data, sort the data into U/D/L/R */
                        if (bytes_read >= 4)
                        {
                            for (int i = 0; i < bytes_read; i += 4)
                            {
                                gestureData.UData[gestureData.Index] = readBuffer[i + 0];
                                gestureData.DData[gestureData.Index] = readBuffer[i + 1];
                                gestureData.LData[gestureData.Index] = readBuffer[i + 2];
                                gestureData.RData[gestureData.Index] = readBuffer[i + 3];
                                gestureData.Index++;
                                gestureData.TotalGestures++;
                            }

                            /* Filter and process gesture data. Decode near/far state */
                            if (ProcessGestureData())
                            {
                                if (DecodeGesture().success)
                                {
                                    //***TODO: U-Turn Gestures
                                }
                            }

                            /* Reset data */
                            gestureData.Index = 0;
                            gestureData.TotalGestures = 0;
                        }
                    }
                }
                else
                {
                    /* Determine best guessed gesture and clean up */
                    Thread.Sleep(FIFO_PAUSE_TIME);
                    var result = DecodeGesture();

                    ResetGestureParameters();

                    if (result.success)
                    {
                        return result.direction;
                    }

                    return Direction.None;
                }
            }
        }

        private void ResetGestureParameters()
        {
            gestureData.Index = 0;
            gestureData.TotalGestures = 0;

            gestureUdDelta = 0;
            gestureLrDelta = 0;

            gestureUdCount = 0;
            gestureLrCount = 0;

            gestureNearCount = 0;
            gestureFarCount = 0;

            gestureState = 0;
        }

        private bool ProcessGestureData()
        {
            byte u_first = 0;
            byte d_first = 0;
            byte l_first = 0;
            byte r_first = 0;
            byte u_last = 0;
            byte d_last = 0;
            byte l_last = 0;
            byte r_last = 0;
            int ud_ratio_first;
            int lr_ratio_first;
            int ud_ratio_last;
            int lr_ratio_last;
            int ud_delta;
            int lr_delta;
            int i;

            /* If we have less than 4 total gestures, that's not enough */
            if (gestureData.TotalGestures <= 4)
            {
                return false;
            }

            /* Check to make sure our data isn't out of bounds */
            if ((gestureData.TotalGestures <= 32) &&
                (gestureData.TotalGestures > 0))
            {
                /* Find the first value in U/D/L/R above the threshold */
                for (i = 0; i < gestureData.TotalGestures; i++)
                {
                    if ((gestureData.UData[i] > Constants.THRESHOLD_OUT) &&
                        (gestureData.DData[i] > Constants.THRESHOLD_OUT) &&
                        (gestureData.LData[i] > Constants.THRESHOLD_OUT) &&
                        (gestureData.RData[i] > Constants.THRESHOLD_OUT))
                    {

                        u_first = gestureData.UData[i];
                        d_first = gestureData.DData[i];
                        l_first = gestureData.LData[i];
                        r_first = gestureData.RData[i];
                        break;
                    }
                }

                /* If one of the _first values is 0, then there is no good data */
                if ((u_first == 0) || (d_first == 0) ||
                    (l_first == 0) || (r_first == 0))
                {
                    return false;
                }
                /* Find the last value in U/D/L/R above the threshold */
                for (i = gestureData.TotalGestures - 1; i >= 0; i--)
                {
                    if ((gestureData.UData[i] > Constants.THRESHOLD_OUT) &&
                        (gestureData.DData[i] > Constants.THRESHOLD_OUT) &&
                        (gestureData.LData[i] > Constants.THRESHOLD_OUT) &&
                        (gestureData.RData[i] > Constants.THRESHOLD_OUT))
                    {

                        u_last = gestureData.UData[i];
                        d_last = gestureData.DData[i];
                        l_last = gestureData.LData[i];
                        r_last = gestureData.RData[i];
                        break;
                    }
                }
            }

            /* Calculate the first vs. last ratio of up/down and left/right */
            ud_ratio_first = (u_first - d_first) * 100 / (u_first + d_first);
            lr_ratio_first = (l_first - r_first) * 100 / (l_first + r_first);
            ud_ratio_last = (u_last - d_last) * 100 / (u_last + d_last);
            lr_ratio_last = (l_last - r_last) * 100 / (l_last + r_last);

            /* Determine the difference between the first and last ratios */
            ud_delta = ud_ratio_last - ud_ratio_first;
            lr_delta = lr_ratio_last - lr_ratio_first;

            /* Accumulate the UD and LR delta values */
            gestureUdDelta += ud_delta;
            gestureLrDelta += lr_delta;

            /* Determine U/D gesture */
            if (gestureUdDelta >= Constants.SENSITIVITY_1)
            {
                gestureUdCount = 1;
            }
            else if (gestureUdDelta <= -Constants.SENSITIVITY_1)
            {
                gestureUdCount = -1;
            }
            else
            {
                gestureUdCount = 0;
            }

            /* Determine L/R gesture */
            if (gestureLrDelta >= Constants.SENSITIVITY_1)
            {
                gestureLrCount = 1;
            }
            else if (gestureLrDelta <= -Constants.SENSITIVITY_1)
            {
                gestureLrCount = -1;
            }
            else
            {
                gestureLrCount = 0;
            }

            /* Determine Near/Far gesture */
            if ((gestureUdCount == 0) && (gestureLrCount == 0))
            {
                if ((Math.Abs(ud_delta) < Constants.SENSITIVITY_2) &&
                    (Math.Abs(lr_delta) < Constants.SENSITIVITY_2))
                {
                    if (Math.Abs(ud_delta) <= 2 && Math.Abs(lr_delta) <= 2)
                    {
                        gestureNearCount++;
                    }
                    else
                    {
                        gestureFarCount++;
                    }

                    if (gestureNearCount >= 8)
                    {
                        gestureState = States.Near;
                        return true;
                    }
                    else if (gestureFarCount >= 2)
                    {
                        gestureState = States.Far;
                        return true;
                    }
                }
            }
            else
            {
                if ((Math.Abs(ud_delta) < Constants.SENSITIVITY_2) && (Math.Abs(lr_delta) < Constants.SENSITIVITY_2))
                {
                    if ((ud_delta == 0) && (lr_delta == 0))
                    {
                        gestureNearCount++;
                    }

                    if (gestureNearCount >= 10)
                    {
                        gestureUdCount = 0;
                        gestureLrCount = 0;
                        gestureUdDelta = 0;
                        gestureLrDelta = 0;
                    }
                }
            }

            return false;
        }


        private class GestureDecode
        {
            public bool success { get; internal set; }
            public Direction direction { get; internal set; }

            public GestureDecode(bool success, Direction direction)
            {
                this.success = success;
                this.direction = direction;
            }
        }

        private GestureDecode DecodeGesture()
        {
            // Check proximity gestures first
            if (gestureState == States.Near)
            {
                return new GestureDecode(true, Direction.Near);
            }

            if (gestureState == States.Far)
            {
                return new GestureDecode(true, Direction.Far);
            }

            // Handle pure directional gestures
            if (IsSimpleDirectionalGesture())
            {
                return DecodeSimpleDirection();
            }

            // Handle diagonal gestures
            if (IsDiagonalGesture())
            {
                return DecodeDiagonalDirection();
            }

            return new GestureDecode(false, Direction.None);
        }

        private bool IsSimpleDirectionalGesture()
        {
            return (Math.Abs(gestureUdCount) == 1 && gestureLrCount == 0) ||
                   (gestureUdCount == 0 && Math.Abs(gestureLrCount) == 1);
        }

        private GestureDecode DecodeSimpleDirection()
        {
            if (gestureUdCount == -1) return new GestureDecode(true, Direction.Up);
            if (gestureUdCount == 1) return new GestureDecode(true, Direction.Down);
            if (gestureLrCount == 1) return new GestureDecode(true, Direction.Right);
            if (gestureLrCount == -1) return new GestureDecode(true, Direction.Left);

            return new GestureDecode(false, Direction.None); // Should never reach here if IsSimpleDirectionalGesture() is true
        }

        private bool IsDiagonalGesture()
        {
            return Math.Abs(gestureUdCount) == 1 && Math.Abs(gestureLrCount) == 1;
        }

        private GestureDecode DecodeDiagonalDirection()
        {
            bool isVerticalDominant = Math.Abs(gestureUdDelta) > Math.Abs(gestureLrDelta);

            // Map diagonal movements to their dominant direction
            

            return new GestureDecode(true, gestureUdCount switch
            {
                1 => gestureLrCount switch
                {
                    1 => isVerticalDominant ? Direction.Down : Direction.Right,
                    -1 => isVerticalDominant ? Direction.Down : Direction.Left,
                    _ => Direction.None
                },
                -1 => gestureLrCount switch
                {
                    1 => isVerticalDominant ? Direction.Up : Direction.Right,
                    -1 => isVerticalDominant ? Direction.Up : Direction.Left,
                    _ => Direction.None
                },
                _ => Direction.None
            });
        }

        private byte GetGestureEnterThresh()
        {
            byte val = BusDevice.ReadRegister(Register.GPENTH);

            return val;
        }

        private void SetGestureEnterThresh(byte threshold)
        {
            BusDevice.WriteRegister(Register.GPENTH, threshold);
        }

        private byte GetGestureExitThresh()
        {
            byte val = BusDevice.ReadRegister(Register.GEXTH);

            return val;
        }

        private void SetGestureExitThresh(byte threshold)
        {
            BusDevice.WriteRegister(Register.GEXTH, threshold);
        }

        private GGain GetGestureGain()
        {
            byte val = BusDevice.ReadRegister(Register.GCONF2);

            /* Shift and mask out GGAIN bits */
            val = (byte)((val >> 5) & 0b00000011);
            
            return (GGain)val;
        }

        private void SetGestureGain(GGain egain)
        {
            byte val = BusDevice.ReadRegister(Register.GCONF2);

            /* Set bits in register to given value */
            byte gain = (byte)((byte)egain & 0b00000011);
            gain = (byte)(gain << 5);
            val &= 0b10011111;
            val |= gain;

            /* Write register value back into GCONF2 register */
            BusDevice.WriteRegister(Register.GCONF2, val);
        }

        private LedDriveLevels GetGestureLEDDrive()
        {
            /* Read value from GCONF2 register */
            byte val = BusDevice.ReadRegister(Register.GCONF2);

            /* Shift and mask out GLDRIVE bits */
            val = (byte)((val >> 3) & 0b00000011);
            
            return (LedDriveLevels)val;
        }

        private void SetGestureLEDDrive(LedDriveLevels edrive)
        {
            byte val = BusDevice.ReadRegister(Register.GCONF2);

            /* Set bits in register to given value */
            byte drive = (byte)((byte)edrive & 0b00000011);
            drive = (byte)(drive << 3);
            val &= 0b11100111;
            val |= drive;

            BusDevice.WriteRegister(Register.GCONF2, val);
        }

        private GestureWaitTime GetGestureWaitTime()
        {
            byte val = BusDevice.ReadRegister(Register.GCONF2);

            /* Mask out GWTIME bits */
            val &= 0b00000111;

            return (GestureWaitTime)val;
        }

        private void SetGestureWaitTime(GestureWaitTime etime)
        {
            byte val = BusDevice.ReadRegister(Register.GCONF2);

            /* Set bits in register to given value */
            byte time = (byte)((byte)etime & 0b00000111);
            val &= 0b11111000;
            val |= time;

            BusDevice.WriteRegister(Register.GCONF2, val);
        }

        private bool GetGestureIntEnable()
        {
            byte val = BusDevice.ReadRegister(Register.GCONF4);

            /* Shift and mask out GIEN bit */
            val = (byte)((val >> 1) & 0b00000001);

            return val == 1;
        }

        private void SetGestureIntEnable(bool benable)
        {
            byte val = BusDevice.ReadRegister(Register.GCONF4);

            /* Set bits in register to given value */
            byte enable = (byte)(benable ? 0x1 : 0);
            enable = (byte)(enable << 1);
            val &= 0b11111101;
            val |= enable;

            BusDevice.WriteRegister(Register.GCONF4, val);
        }

        private bool GetGestureMode()
        {
            byte val = BusDevice.ReadRegister(Register.GCONF4);

            /* Mask out GMODE bit */
            val &= 0b00000001;

            return val == 1;
        }

        private void SetGestureMode(bool bmode)
        {
            byte val = BusDevice.ReadRegister(Register.GCONF4);

            /* Set bits in register to given value */
            byte mode = (byte)(bmode ? 0x1 : 0);
            val &= 0b11111110;
            val |= mode;

            BusDevice.WriteRegister(Register.GCONF4, val);
        }

        internal GestureStatus ReadGestureStatusRegister()
        {
            return (GestureStatus)(BusDevice.ReadRegister(Register.GSTATUS) & 0x03);
        }
    }
}
