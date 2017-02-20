using GalaSoft.MvvmLight;
using Lunatic.Core;
using Lunatic.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

/// Imported from https://github.com/skywatcher-pacific/skywatcher_open

/// Notes:
/// 1. Use exception instead of ErrCode because there is no dll import issue we need to handle and 
/// the exception code stlye will much easier to maintain.
/// 2. Need to confirm the mapping between SerialPort class in C# and DCB class in C++, such as CTS.
/// 3. Rename UpdateAxisPosition and UpdateAxesStatus to GetAxisPosition and GetAxesStatus to hide the details
/// 4. LastSlewingIsPositive has been merge with AxesStatus.SLEWING_FORWARD
///
/// 5. While bluetooth connection fail, user should apply Connect_COM and try to connect again
/// RTSEnable may not be accepcted 
/// http://blog.csdn.net/solond/archive/2008/03/04/2146446.aspx
/// 6. It looks like Skywatcher mounts response time is 1.5x longer than Celestron's mount

namespace ASCOM.Lunatic
{
   /// <summary>
   /// Define the abstract interface of a Mount, includes:
   /// 1) Connection via Serial Port    
   /// 2) Protocol 
   /// 3) Basic Mount control interface 
   /// LV0. 
   /// TalkWithAxis
   /// LV1. 
   /// DetectMount // Not implement yet
   /// MCOpenTelescopeConnection
   /// MCCloseTelescopeConnection
   /// MCAxisSlew
   /// MCAxisSlewTo
   /// MCAxisStop
   /// MCSetAxisPosition
   /// MCGetAxisPosition
   /// MCGetAxisStatus
   /// </summary>
   /// Checked 2/7/2011
   public abstract class SyntaMountBase : ReferenceCountedObjectBase
   {

      private SerialConnect_COM _Connection = null;
      /// The abstract Serial connection instance 
      /// it is static because all connection shared the same serial connection
      /// and connection should be lock between differnct thread
      protected SerialConnect_COM Connection
      {
         get
         {
            return _Connection;
         }
         set
         {
            if (value == _Connection) {
               return;
            }
            if (_Connection != null) {
               WatchCOMPorts(false);
            }
            _Connection = value;
            if (_Connection != null) {
               WatchCOMPorts(true);
            }
            RaisePropertyChanged();
         }
      }

      protected long MCVersion = 0;   // Motor controller version number

      public MountId MountID = 0;     // Mount Id
      public bool IsEQMount = false;      // the physical meaning of mount (Az or EQ)

      /// ************ Motion control related **********************
      /// They are variables represent the mount's status, but not grantee always updated.        
      /// 1) The Positions are updated with MCGetAxisPosition and MCSetAxisPosition
      /// 2) The TargetPositions are updated with MCAxisSlewTo        
      /// 3) The SlewingSpeed are updated with MCAxisSlew
      /// 4) The AxesStatus are updated updated with MCGetAxisStatus, MCAxisSlewTo, MCAxisSlew
      /// Notes:
      /// 1. Positions may not represent the mount's position while it is slewing, or user manually update by hand
      public double[] Positions = new double[2] { 0, 0 };          // The axis coordinate position of the carriage, in radians
      public double[] TargetPositions = new double[2] { 0, 0 };   // The target position, in radians
      public double[] SlewingSpeed = new double[2] { 0, 0 };      // The speed in radians per second                
      public AxisStatus[] AxesStatus = new AxisStatus[2];             // The two-axis status of the carriage should pass AxesStatus[AXIS1] and AxesStatus[AXIS2] by Reference


      public SyntaMountBase() : base()
      {

         Connection = null;
         MCVersion = 0;
         IsEQMount = false;

         Positions[0] = 0;
         Positions[1] = 0;
         TargetPositions[0] = 0;
         TargetPositions[1] = 0;
         SlewingSpeed[0] = 0;
         SlewingSpeed[1] = 0;
         AxesStatus[0] = new AxisStatus { FullStop = false, NotInitialized = true, HighSpeed = false, Slewing = false, SlewingForward = false, SlewingTo = false };
         AxesStatus[1] = new AxisStatus { FullStop = false, NotInitialized = true, HighSpeed = false, Slewing = false, SlewingForward = false, SlewingTo = false };
      }
      ~SyntaMountBase()
      {
         System.Diagnostics.Trace.WriteLine("ASCOM.Lunatic.SyntaMountBase destructor is called.");
         if (Connection != null) {
            Connection.Close();
         }
      }


      /// <summary>
      /// Build a connection to mount via COM
      /// </summary>
      /// <param name="TelescopePort">the COM port number for connection</param>
      /// Raise IOException
      public virtual void Connect_COM(string telescopePort)
      {
         if (Connection != null) {
            return;  // Already connected.
         }
         // May raise IOException 
         //var hCom = new SerialPort(string.Format("\\$device\\COM{0}", TelescopePort));
         var hCom = new SerialPort(telescopePort);

         // Set communication parameter
         hCom.BaudRate = (int)BaudRate.Baud9600;
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

         hCom.ReadTimeout = (int)TimeOutOption.TO1000;
         hCom.WriteTimeout = 60;

         hCom.Open();
         Connection = new SerialConnect_COM(hCom);
      }

      private void WatchCOMPorts(bool AddWatch)
      {
         if (AddWatch) {
            COMPortService.AddListener();
            COMPortService.PortRemoved += COMPortService_PortRemoved;
         }
         else {
            COMPortService.PortRemoved -= COMPortService_PortRemoved;
            COMPortService.RemoveListener();
         }
      }

      private void COMPortService_PortRemoved(object sender, EventArgs e)
      {
         // Check that the current com port is still in the list of available ports
         if (Connection != null) {
            if (!((SerialConnect_COM)Connection).hCom.IsOpen) {
               Connection = null;
            }
         }
      }

      public virtual void Disconnect_COM()
      {
         if (Connection != null) {
            Connection.Close();
            Connection = null;
         }
      }
      /// <summary>
      /// One communication between mount and client
      /// </summary>
      /// <param name="Axis">The target of command</param>
      /// <param name="Command">The comamnd char set</param>
      /// <param name="cmdDataStr">The data need to send</param>
      /// <returns>The response string from mount</returns>
      protected virtual String TalkWithAxis(AxisId Axis, char Command, string cmdDataStr)
      {
         /// Lock the serial connection
         /// It grantee there is only one thread entering this function in one time
         /// ref: http://msdn.microsoft.com/en-us/library/ms173179.aspx
         /// TODO: handle exception
         lock (Connection) {
            for (int i = 0; i < 2; i++) {
               /// The General Process for SerialPort COM
               /// 1. Prepare Command Str by protocol
               /// 2. Wait CTS if need
               /// 3. Set RTS
               /// 4. Send Command
               /// 5. Receive Response
               /// 6. Clear RTS

               // prepare to communicate
               try {
                  Connection.ClearBuffer();
                  Connection.WaitIdle();
                  Connection.Lock();

                  // send the request
                  SendRequest(Axis, Command, cmdDataStr);

                  // Release the line, so the mount can send response
                  Connection.Release();

                  //Trace.TraceInformation("Send command successful");
                  // receive the response
                  return ReceiveResponse();
               }
               catch (TimeoutException e) {
                  Trace.TraceError("Timeout, need Resend the Command");
               }
               catch (IOException e) {
                  Trace.TraceError("Connnection Lost");
                  throw new MountControlException(ErrorCode.ERR_NOT_CONNECTED, e.Message);
               }
            }
            //Trace.TraceError("Timeout, stop send");
            if (Axis == AxisId.Axis1)
               throw new MountControlException(ErrorCode.ERR_NORESPONSE_AXIS1);
            else
               throw new MountControlException(ErrorCode.ERR_NORESPONSE_AXIS2);
         }

      }

      /// <summary>
      /// 
      /// </summary>
      /// <exception cref="IOException">Throw when</exception>
      /// <param name="Axis"></param>
      /// <param name="Command"></param>
      /// <param name="cmdDataStr"></param>
      protected abstract void SendRequest(AxisId Axis, char Command, string cmdDataStr);
      /// <summary>
      /// Receive the response
      /// </summary>
      /// <exception cref="IOException"></exception>
      /// <exception cref="TimeOutException"></exception>
      /// <returns></returns>
      protected abstract string ReceiveResponse();

      /// ************** The Mount Control Interface *****************
      public abstract void MCInit();

      // Mount dependent motion control functions 
      public abstract void MCAxisSlew(AxisId Axis, double rad);
      public abstract void MCAxisSlewTo(AxisId Axis, double pos);
      public abstract void MCAxisStop(AxisId Axis);
      // Unit: radian
      public abstract void MCSetAxisPosition(AxisId Axis, double pos);
      public abstract double MCGetAxisPosition(AxisId Axis);
      // Support Mount Status;        
      public abstract AxisStatus MCGetAxisStatus(AxisId Axis);
      public abstract void MCSetSwitch(bool OnOff);

      // Converting an arc angle to a step
      protected double[] FactorRadToStep = new double[] { 0, 0 };             // Multiply the radian value by the coefficient to get the motor position value (24-bit number can be discarded the highest byte)
      protected long AngleToStep(AxisId Axis, double AngleInRad)
      {
         return (long)(AngleInRad * FactorRadToStep[(int)Axis]);
      }

      // Converts Step to Radian
      protected double[] FactorStepToRad = new double[] { 0, 0 };                 // The value of the motor board position (need to deal with the problem after the symbol) multiplied by the coefficient can be a radian value
      protected double StepToAngle(AxisId Axis, long Steps)
      {
         return Steps * FactorStepToRad[(int)Axis];
      }

      // Converts the speed in radians per second to an integer used to set the speed
      protected double[] FactorRadRateToInt = new double[] { 0, 0 };           // Multiply the radians per second by this factor to obtain a 32-bit integer that sets the speed used by the motor board
      protected long RadSpeedToInt(AxisId Axis, double RateInRad)
      {
         return (long)(RateInRad * FactorRadRateToInt[(int)Axis]);
      }

   }
}
