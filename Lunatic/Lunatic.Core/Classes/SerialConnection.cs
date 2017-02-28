using System;
using System.IO.Ports;
using System.Text;


/// Imported from https://github.com/skywatcher-pacific/skywatcher_open
/// Note:
/// When controller want to send data, it set RTS high
/// When device ready to receive data, it set CTS high, then data transmision begins
/// 
/// Refactor the CTS method in code:
/// 1: PrepareConversation
///     NexStar_GT, Skywatcher_EQ: SETRTS
///     Celestron,Skywatcher_AZ: Wait CTS on, Set SETRTS
/// 2: Clear Buffer
/// 3: Write
/// 4: PrepareToReceive
///     Celestron: Read Echo, CLRRTS
///     Others: Do nothing
/// 5: ReceiveResponse
///     Celestron, all others: Read, 
///     Skywatcher: CLRRTS
/// 6. EndConversation:
///     Celstron, Skywatcher_AZ: CLRRTS
/// Generally Speaking:
/// Wait CTS(depend), SETRTS, Send Data, Read Data, CLRRTS
/// 
/// Issues, the bluetooth serial port (COM) didn't support RTS operation. it will raise exception
/// HandShake also didn't support RTS operation
/// Right now, the bluetooth connection is built by windows SPP to com support


namespace Lunatic.Core
{
   /// <summary>
   /// The abstract class of serial connection.
   /// We use the same serial connection method with Bluetooth, COM, Wifi connection
   /// on PC, WinCE, iPhone, and Android
   /// </summary>
   public abstract class SerialConnection
   {
      //public int BytesSent;   // How many bytes have been sent in last Write
      public string StringSent;
      public abstract void Close();
      public StringBuilder mBuffer = new StringBuilder();

      public virtual void WaitIdle() { }
      public virtual void Lock() { }
      public virtual void Release() { }
      /// <summary>
      /// Write command to mount
      /// </summary>
      /// <param name="Command">command string</param>
      public abstract void Write(string Command);
      /// <summary>
      /// Read all string available from mount.
      /// </summary>
      /// <returns></returns>
      public abstract String Read();
      ///// <summary>
      ///// Read specify characters to buffer, start at offset
      ///// </summary>
      ///// <param name="buffer">read buffer</param>
      ///// <param name="offset">the starting bits</param>
      ///// <param name="count">the offset</param>
      ///// <returns></returns>
      ////public abstract int Read(char[] buffer, int offset, int count);

      /// <summary>
      /// Clean the read and write buffer
      /// </summary>
      public abstract void ClearBuffer();
   }

   /// <summary>
   /// 
   /// The mapping of DCB in C++ and SerialPort in C#:
   /// BaudRate = BaudRate
   /// BreakState =  Break
   /// BytesToRad = InBufferCount
   /// BytesToWrite= OutBufferCount
   /// CDHolding = MC_RLSD_ON
   /// CtsHolding = MS_CTS_ON
   /// DataBits = ByteSize
   /// DiscardNull = DiscardNulls
   /// DsrHolding = MS_DSR_ON
   /// DtrEnable 
   /// ref: http://www.java2s.com/Open-Source/CSharp/Development/SerialPort/OpenNETCF/IO/Ports/Streams/WinStream.cs.htm     
   /// </summary>
   public class SerialConnect_COM : SerialConnection, IEquatable<SerialConnect_COM>
   {
      public static class CBR
      {
         public const int CBR_110 = 110;
         public const int CBR_300 = 300;
         public const int CBR_600 = 600;
         public const int CBR_1200 = 1200;
         public const int CBR_2400 = 2400;
         public const int CBR_4800 = 4800;
         public const int CBR_9600 = 9600;
         public const int CBR_14400 = 14400;
         public const int CBR_19200 = 19200;
         public const int CBR_38400 = 38400;
         public const int CBR_56000 = 56000;
         public const int CBR_57600 = 57600;
         public const int CBR_115200 = 115200;
         public const int CBR_128000 = 128000;
         public const int CBR_256000 = 256000;
      }

      private SerialPort hCom = null;

      public SerialConnect_COM()
      {
      }

      ~SerialConnect_COM()
      {
         if (hCom != null) {
            hCom.Dispose();
            hCom = null;
         }
      }

      /// Detect the serial connection need to have HardwareFlowControl or notb
      private bool HasHardwareFlowControl()
      {
         bool result = true;
         const int SLEEP_TIME = 10;
         int MaxLoopCount = 400 / SLEEP_TIME;

         // Wait for other master to release CTS line.
         do {
            // Get CTS status. CTS is active low: it is ON when the level is low.
            if (hCom.CtsHolding) {
               if (MaxLoopCount == 0) {
                  // If overtime, it should be a bluetooth serial port.
                  return false;
               }
               // Calling thread sleep, reduce CPU load.
               System.Threading.Thread.Sleep(SLEEP_TIME);
               MaxLoopCount--;
            }
         } while (hCom.CtsHolding);// repeat if CTS is low level.

         // Pull CTS/RTS(Active Low) low. Enable TX driver and claim the TX/RX bus is busy.
         hCom.RtsEnable = true;

         // Now check CTS line, it should be pulled down on Celestron mount and Skywatcher's Az mount
         // if using a cable to connect iSky to the mount.
         // Get CTS status. CTS is active low: it is ON when the level is low.
         if (hCom.CtsHolding) result = true;
         else result = false;

         hCom.RtsEnable = false;

         return result;
      }

      public override void WaitIdle()
      {
         // Wait Idle is not c

         //const int SleepTime = 10;
         //const int MaxLoopCount = 20 / SleepTime;

         ///// 1. Wait CTS if need
         //if (!DoNotCheckCTS)
         //{
         //    for (int i = 0; i < MaxLoopCount; i++)
         //    {
         //        if (!hCom.CtsHolding)
         //            return;
         //        else
         //        {
         //            // If CTS is low level                                                                
         //            System.Threading.Thread.Sleep(SleepTime);
         //        }
         //    }
         //    throw new TimeoutException();
         //}
      }
      public override void Lock()
      {
         //hCom.RtsEnable = true;
      }

      public bool IsOpen
      {
         get
         {
            return (hCom != null && hCom.IsOpen);
         }
      }

      public bool Connect(string serialPort, int baudRate, int readTimeout )
      {
         if (hCom != null && hCom.IsOpen) {
            return true;  // Already connected.
         }
         try {
            if (hCom != null && !hCom.IsOpen) {
               hCom.Dispose();
               hCom = null;
            }
            // May raise IOException 
            //var hCom = new SerialPort(string.Format("\\$device\\COM{0}", TelescopePort));
            hCom = new SerialPort(serialPort);

            // Set communication parameter
            hCom.BaudRate = baudRate;
            // fOutxCTSFlow
            // fOutxDsrFlow
            hCom.DtrEnable = false;
            // fDsrSensitivity            
            hCom.Handshake = Handshake.RequestToSendXOnXOff;
            // fOutX
            // fInX
            // fErrorChar
            // fNull
            hCom.RtsEnable = false;
            // fAboveOnError
            hCom.Parity = Parity.None;
            hCom.DataBits = 8;
            hCom.StopBits = StopBits.One;
            hCom.Encoding = Encoding.ASCII;
            hCom.ReadTimeout = readTimeout;
            hCom.WriteTimeout = 60;

            hCom.Open();

            return true;
         }
         catch (Exception ex){
            throw new ASCOM.NotConnectedException("Unable to connect to COM ports.", ex);
         }
      }

      public override void Write(string Command)
      {
         // throw IOException
         //Console.WriteLine("Write :" + Command);
         hCom.Write(Command);
         StringSent = Command;

      }
      public override String Read()
      {
         // throw IOException
         var r = hCom.ReadExisting();
         //if(r.Length > 0)
         //    Console.WriteLine("Read :" + r);            
         mBuffer.Append(r);
         return r;
      }
      public override void Release()
      {
         //hCom.RtsEnable = false;
      }

      //public override int Read(char[] buffer, int offset, int count)
      //{
      //    return hCom.Read(buffer, offset, count);
      //}

      public override void ClearBuffer()
      {
         hCom.DiscardOutBuffer();
         hCom.DiscardInBuffer();
         mBuffer.Remove(0, mBuffer.Length);
      }

      public override void Close()
      {
         if (hCom != null && hCom.IsOpen) {
            hCom.Close();
         }
         if (hCom != null) {
            hCom.Dispose();
            hCom = null;
         }
      }

      #region  IEquatable ...
      public bool Equals(SerialConnect_COM other)
      {
         // If parameter is null return false:
         if ((object)other == null) {
            return false;
         }

         // Return true if the fields match:
         return (hCom.PortName == other.hCom.PortName);
      }

      public override bool Equals(object obj)
      {
         if (obj == null) {
            return false;
         }

         SerialConnect_COM other = obj as SerialConnect_COM;
         if ((object)other == null) {
            return false;
         }
         return (hCom.PortName == other.hCom.PortName);
      }
      public static bool operator ==(SerialConnect_COM connection1, SerialConnect_COM connection2)
      {
         if (object.ReferenceEquals(connection1, connection2)) {
            return true;
         }
         if (((object)connection1 == null) || ((object)connection2 == null)) {
            return false;
         }
         return (connection1.hCom.PortName == connection2.hCom.PortName);
      }


      public static bool operator !=(SerialConnect_COM connection1, SerialConnect_COM connection2)
      {
         return !(connection1 == connection2);
      }

      public override int GetHashCode()
      {
         int hash = 13;
         hash = (hash * 7) + hCom.PortName.GetHashCode();
         return hash;
      }
      #endregion
   }

}
