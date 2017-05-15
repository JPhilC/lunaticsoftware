using Lunatic.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TA.Ascom.ReactiveCommunications;
using TA.Ascom.ReactiveCommunications.Transactions;
using Lunatic.SyntaController.Transactions;
using ASCOM.DeviceInterface;
using Lunatic.Core.Geometry;

namespace Lunatic.SyntaController
{

   public sealed partial class MountController
   {
      public const double MAX_SLEW_SPEED = (800 * Constants.SIDEREAL_RATE_DEGREES);           //?
      public const double LOW_SPEED_MARGIN = (128.0 * Constants.SIDEREAL_RATE_DEGREES);
      /// <summary>
      /// Maximum error allowed when comparing Axis positions in radians (roughly 0.5 seconds)
      /// </summary>
      private const double AXIS_ERROR_TOLERANCE = 3.5E-5;

      #region Singleton code ...
      private static MountController _Instance = null;

      public static MountController Instance
      {
         get
         {
            if (_Instance == null) {
               _Instance = new MountController();
            }
            return _Instance;
         }
      }

      #endregion


      #region Settings related stuff ...
      private ISettingsProvider<ControllerSettings> _SettingsManager = null;

      public ISettingsProvider<ControllerSettings> SettingsManager
      {
         get
         {
            if (_SettingsManager == null) {
               _SettingsManager = new SettingsProvider();
            }
            return _SettingsManager;
         }
      }

      private ControllerSettings Settings
      {
         get
         {
            return SettingsManager.Settings;
         }
      }
      #endregion

      #region Properties ...
      // special charactor for communication.
      const char cStartChar_Out = ':';    // Leading charactor of a command 
      const char cStartChar_In = '=';        // Leading charactor of a NORMAL response.
      const char cErrChar = '!';              // Leading charactor of an ABNORMAL response.
      const char cEndChar = (char)13;         // Tailing charactor of command and response.

      private char dir = '0'; // direction
                              // Mount code: 0x00=EQ6, 0x01=HEQ5, 0x02=EQ5, 0x03=EQ3
                              //             0x80=GT,  0x81=MF,   0x82=114GT
                              //             0x90=DOB
      private int MountCode;
      private int[] StepTimerFreq = new int[2];        // Frequency of stepping timer (read from mount)
      private int[] PESteps = new int[2];              // PEC Period (read from mount)
      private int[] HighSpeedRatio = new int[2];       // High Speed Ratio (read from mount)
      //private int[] StepPosition = new int[2];       // Never Used
      private int[] BreakSteps = new int[2];           // Break steps from slewing to stop. (currently hard coded)
      private int[] LowSpeedGotoMargin = new int[2];     // if ( slewing steps exceeds this LowSpeedGotoMargin, 
                                                         // GOTO is in high speed slewing.
      private double[] LowSpeedSlewRate = new double[2];    // Low speed slew rate
      private double[] HighSpeedSlewRate = new double[2];   // High speed slew rate

      private int[] MountParameters = new int[2];
      private bool[] HasHalfCurrent = new bool[2];
      private bool[] HasEncoder = new bool[2];
      private bool[] HasPPEC = new bool[2];
      private bool[] HasSnap = new bool[2];

      private int[] FastTarget = new int[2];
      private int[] FinalTarget = new int[2];
      private int[] CurrentPosition = new int[2];
      private int[] GuideRateOffset = new int[2];

      private bool IsDCMotor;                // Ture: The motor controller is a DC motor controller. It uses TX/RX line is bus topology.
                                             // False: The motor controller is a stepper motor controller. TX/RX lines are seperated.
      private bool InstantStop;              // Use InstantStop command for MCAxisStop

      private bool HasPolarscopeLED;
      private bool HasHomeSensor;

      private object lockObject = new object();

      private int MCVersion = 0;   // Motor controller version number


      /// <summary>
      /// Connection string that is currently being used'
      /// </summary>
      private string ConnectionString;

      /// <summary>
      /// End point for connection to mount.
      /// </summary>
      private DeviceEndpoint EndPoint;

      /// <summary>
      /// Timeout in seconds.
      /// </summary>
      private double TimeOut = 2;

      private int Retry = 1;

      public bool IsConnected
      {
         get
         {
            return (EndPoint != null);
         }
      }

      private int OpenConnections;

      #endregion
      /// <summary>
      /// Private constructor so that instances cannot be created outside this definition.
      /// </summary>
      private MountController()
      {
         ConnectionString = string.Empty;
         EndPoint = null;

         MCVersion = 0;
         Positions[0] = 0;
         Positions[1] = 0;
         TargetPositions[0] = 0;
         TargetPositions[1] = 0;
         SlewingSpeed[0] = 0;
         SlewingSpeed[1] = 0;
         AxesStatus[0] = new AxisStatus { FullStop = false, NotInitialized = true, HighSpeed = false, Slewing = false, SlewingForward = false, SlewingTo = false };
         AxesStatus[1] = new AxisStatus { FullStop = false, NotInitialized = true, HighSpeed = false, Slewing = false, SlewingForward = false, SlewingTo = false };
      }


      #region Skywatcher_Open code ...

      /// ************ Motion control related **********************
      /// They are variables represent the mount's status, but not grantee always updated.        
      /// 1) The Positions are updated with MCGetAxisPosition and MCSetAxisPosition
      /// 2) The TargetPositions are updated with MCAxisSlewTo        
      /// 3) The SlewingSpeed are updated with MCAxisSlew
      /// 4) The AxesStatus are updated updated with MCGetAxisStatus, MCAxisSlewTo, MCAxisSlew
      /// Notes:
      /// 1. Positions may not represent the mount's position while it is slewing, or user manually update by hand
      public double[] Positions = new double[2] { 0, 0 };            // The axis coordinate position of the carriage, in radians
      public double[] TargetPositions = new double[2] { 0, 0 };      // The target position, in radians
      public double[] SlewingSpeed = new double[2] { 0, 0 };         // The speed in radians per second                
      private AxisStatus[] AxesStatus = new AxisStatus[2];           // The two-axis status of the carriage should pass AxesStatus[AXIS1] and AxesStatus[AXIS2] by Reference

      private int[] GridPerRevolution = new int[2];                  // Number of steps for 360 degree

      // Converting an arc angle to a step
      private double[] FactorRadToStep = new double[] { 0, 0 };      // Multiply the radian value by the coefficient to get the motor position value (24-bit number can be discarded the highest byte)
      private int AngleToStep(AxisId Axis, double AngleInRad)
      {
         return (int)(AngleInRad * FactorRadToStep[(int)Axis]);
      }

      // Converts Step to Radian
      private double[] FactorStepToRad = new double[] { 0, 0 };                 // The value of the motor board position (need to deal with the problem after the symbol) multiplied by the coefficient can be a radian value
      private double StepToAngle(AxisId Axis, int Steps)
      {
         return Steps * FactorStepToRad[(int)Axis];
      }

      // Converts the speed in radians per second to an integer used to set the speed
      private double[] FactorRadRateToInt = new double[] { 0, 0 };           // Multiply the radians per second by this factor to obtain a 32-bit integer that sets the speed used by the motor board
      private int RadSpeedToInt(AxisId Axis, double RateInRad)
      {
         return (int)(RateInRad * FactorRadRateToInt[(int)Axis]);
      }
      /////// Conection-Initalization Functions /////

      //
      // Function name    : Connection (Was EQ_Init() in EQMOD)
      // Description      : Connect to the Controller via Serial and initialize the stepper board
      // Return type      : DOUBLE
      //                      000 - Success (was NOT already initialised).
      //                      001 - Success (WAS already initialised).
      //                      002 - COM Port already Open
      //                      003 - COM Timeout Error
      //                      005 - Mount Initialized on using non-standard parameters
      //                      010 - Cannot execute command at the current stepper controller state
      //                      999 - Invalid parameter
      // Argument         : STRING COMPORT Name
      // Argument         : DOUBLE baud - Baud Rate
      // Argument         : DOUBLE timeout - COMPORT Timeout (1 - 50000) seconds
      // Argument         : DOUBLE retry - COMPORT Retry (0 - 100)
      //
      // Public Declare Function EQ_Init Lib "EQCONTRL" (ByVal COMPORT As String, ByVal baud As Long, ByVal timeout As Long, ByVal retry As Long) As Long
      public int Connect(string ComPort, int baud, int timeout, int retry)
      {
         if ((timeout == 0) || (timeout > 50000)) {
            return Constants.MOUNT_BADPARAM;
         }

         if (retry > 100) {
            return Constants.MOUNT_BADPARAM;
         }

         lock (lockObject) {
            int result = EQ_Init(ComPort, baud, timeout, retry);
            Interlocked.Increment(ref OpenConnections);
            return result;
         }
      }



      //
      // Function name    : EQ_End()
      // Description      : Close the COM Port and end EQ Connection
      // Return type      : DOUBLE
      //          00 - Success
      //          01 - COM Port Not Openavailable
      //
      // Public Declare Function EQ_End Lib "EQContrl.dll" () As Long
      public int Disconnect()
      {
         lock (lockObject) {
            int result = 0;
            Interlocked.Decrement(ref OpenConnections);
            if (OpenConnections <= 0) {
               EndPoint = null;
               ConnectionString = string.Empty;
               EQ_End();
            }
            SettingsManager.SaveSettings();
            return result;
         }
      }

      /// <summary>
      /// Initialize RA/DEC Motors and activate Motor Driver Coils
      /// </summary>
      /// <param name="raAxisValue">Initial RA axis position (Radians)</param>
      /// <param name="decAxisValue">Initial Dec axis position (Radians)</param>
      /// <returns>      
      ///   000 - Success
      ///   001 - COM PORT Not available
      ///   003 - COM Timeout Error
      ///   006 - RA Motor still running
      ///   007 - DEC Motor still running
      ///   008 - Error Initializing RA Motor
      ///   009 - Error Initilizing DEC Motor
      ///   010 - Cannot execute command at the current stepper controller state
      /// </returns>
      public int MCInitialiseAxes(AxisPosition axisPosition)
      {
         if (!MountActive) {
            return Constants.MOUNT_NOCOMPORT;
         }
         // Check if Both Motors are at rest
         AxisStatus axisStatus = MCGetAxisStatus(AxisId.Axis1_RA); // Get RA Motor Status
         if (!axisStatus.NotInitialized) {
            // ra motor is apprently moving -don't reinitialise
            return Constants.MOUNT_RARUNNING;
         }

         axisStatus = MCGetAxisStatus(AxisId.Axis2_DEC);          // Get DEC Motor Status
         if (!axisStatus.NotInitialized) {
            // dec motor is apprently moving - don't reiitialise
            return Constants.MOUNT_DECRUNNING;
         }

         // Set RA
         MCSetAxisPosition(axisPosition);

         // Confirm RA
         double confirmationPosition = MCGetAxisPosition(AxisId.Axis1_RA);
         if (Math.Abs(confirmationPosition - axisPosition[RA_AXIS]) > AXIS_ERROR_TOLERANCE) {
            return Constants.MOUNT_RAERROR;
         }

         // Confirm DEC
         confirmationPosition = MCGetAxisPosition(AxisId.Axis2_DEC);
         if (Math.Abs(confirmationPosition - axisPosition[DEC_AXIS]) > AXIS_ERROR_TOLERANCE) {
            return Constants.MOUNT_DECERROR;
         }

         // Activate RA  Motor
         TalkWithAxis(AxisId.Axis1_RA, 'F', null);

         // Activate DEC Motor
         TalkWithAxis(AxisId.Axis2_DEC, 'F', null);

         return Constants.MOUNT_SUCCESS;

      }

      /// <summary>
      /// Slew about a given axis
      /// </summary>
      /// <param name = "Axis" ></ param >
      /// < param name="Speed">Slew speed in Radians per second.</param>
      public void MCAxisSlew(AxisId Axis, double Speed)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("MCAxisSlew: ({0}, {1})", Axis, Speed));
         // Limit maximum speed
         if (Speed > MAX_SLEW_SPEED) {                  // 3.4 degrees/sec, 800X sidereal rate, is the highest speed.
            Speed = MAX_SLEW_SPEED;
         }
         else if (Speed < -MAX_SLEW_SPEED) {
            Speed = -MAX_SLEW_SPEED;
         }

         double InternalSpeed = Speed;
         System.Diagnostics.Debug.WriteLine(string.Format("1. Internal speed: {0}", InternalSpeed));
         bool forward = false, highspeed = false;

         // InternalSpeed lower than 1/1000 of sidereal rate?
         if (Math.Abs(InternalSpeed) <= Constants.SIDEREAL_RATE_ARCSECS / 1000.0) {
            MCAxisStop(Axis);
            return;
         }

         // Stop motor and set motion mode if necessary.
         PrepareForSlewing(Axis, InternalSpeed);
         System.Diagnostics.Debug.WriteLine(string.Format("1. Internal speed: {0}", InternalSpeed));

         if (InternalSpeed > 0.0)
            forward = true;
         else {
            InternalSpeed = -InternalSpeed;
            forward = false;
         }

         // TODO: ask the details

         // Calculate and set step period. 
         //if (InternalSpeed > LOW_SPEED_MARGIN) {                  // High speed adjustment
         //   InternalSpeed = InternalSpeed / (double)HighSpeedRatio[(int)Axis];
         //   highspeed = true;
         //}
         InternalSpeed = 1 / InternalSpeed;                    // For using function RadSpeedToInt(), change to unit Senonds/Rad.
         int SpeedInt = RadSpeedToInt(Axis, InternalSpeed);
         if ((MCVersion == 0x010600) || (MCVersion == 0x010601))  // For special MC version.
            SpeedInt -= 3;
         if (SpeedInt < 6) SpeedInt = 6;
         SetStepPeriod(Axis, SpeedInt);

         // Start motion
         // if (AxesStatus[Axis] & AXIS_FULL_STOPPED)				// It must be remove for the latest DC motor board.
         StartMotion(Axis);

         AxesStatus[(int)Axis].SetSlewing(forward, highspeed);
         SlewingSpeed[(int)Axis] = Speed;
      }

      //public void MCAxisSlewTo(AxisId Axis, double TargetPosition)
      //{
      //   // Get current position of the axis.
      //   var CurPosition = MCGetAxisPosition(Axis);

      //   // Calculate slewing distance.
      //   // Note: For EQ mount, Positions[AXIS1] is offset( -PI/2 ) adjusted in UpdateAxisPosition().
      //   var MovingAngle = TargetPosition - CurPosition;

      //   // Convert distance in radian into steps.
      //   var MovingSteps = AngleToStep(Axis, MovingAngle);

      //   bool forward = false, highspeed = false;

      //   // if ( there is no increment, return directly.
      //   if (MovingSteps == 0) {
      //      return;
      //   }

      //   // Set moving direction
      //   if (MovingSteps > 0) {
      //      dir = '0';
      //      forward = true;
      //   }
      //   else {
      //      dir = '1';
      //      MovingSteps = -MovingSteps;
      //      forward = false;
      //   }

      //   // Might need to check whether motor has stopped.

      //   // Check if the distance is int enough to trigger a high speed GOTO.
      //   if (MovingSteps > LowSpeedGotoMargin[(int)Axis]) {
      //      SetMotionMode(Axis, '0', dir);      // high speed GOTO slewing 
      //      highspeed = true;
      //   }
      //   else {
      //      SetMotionMode(Axis, '2', dir);      // low speed GOTO slewing
      //      highspeed = false;
      //   }

      //   SetGotoTargetIncrement(Axis, MovingSteps);
      //   SetBreakPointIncrement(Axis, BreakSteps[(int)Axis]);
      //   StartMotion(Axis);

      //   TargetPositions[(int)Axis] = TargetPosition;
      //   AxesStatus[(int)Axis].SetSlewingTo(forward, highspeed);
      //}

      public void MCAxisStop(AxisId Axis)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("MCAxisStop: ({0})", Axis));
         if (InstantStop)
            TalkWithAxis(Axis, 'L', null);
         else
            TalkWithAxis(Axis, 'K', null);

         AxesStatus[(int)Axis].SetFullStop();
      }

      /// <summary>
      /// Set the Axis position using radians.
      /// </summary>
      /// <param name="Axis"></param>
      /// <param name="NewValue"></param>
      public void MCSetAxisPosition(AxisId Axis, double NewValue)
      {
         int NewStepIndex = AngleToStep(Axis, NewValue);
         NewStepIndex += 0x800000;

         string szCmd = intTo6BitHEX(NewStepIndex);
         TalkWithAxis(Axis, 'E', szCmd);

         Positions[(int)Axis] = NewValue;
      }


      public void MCSetAxisPosition(AxisPosition axisPosition)
      {
         MCSetAxisPosition(AxisId.Axis1_RA, axisPosition[RA_AXIS]);
         MCSetAxisPosition(AxisId.Axis2_DEC, axisPosition[DEC_AXIS]);
      }

      public double MCGetAxisPosition(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 'j', null);

         int iPosition = BCDstr2int(response);
         iPosition -= 0x00800000;
         Positions[(int)Axis] = StepToAngle(Axis, iPosition);

         return Positions[(int)Axis];
      }

      public AxisStatus MCGetAxisStatus(AxisId Axis)
      {

         var response = TalkWithAxis(Axis, 'f', null);

         if ((response[2] & 0x01) != 0) {
            // Axis is running
            if ((response[1] & 0x01) != 0)
               AxesStatus[(int)Axis].Slewing = true;     // Axis in slewing(AstroMisc speed) mode.
            else
               AxesStatus[(int)Axis].SlewingTo = true;      // Axis in SlewingTo mode.
         }
         else {
            AxesStatus[(int)Axis].FullStop = true; // FullStop = 1;	// Axis is fully stop.
         }

         if ((response[1] & 0x02) == 0)
            AxesStatus[(int)Axis].SlewingForward = true; // Angle increase = 1;
         else
            AxesStatus[(int)Axis].SlewingForward = false;

         if ((response[1] & 0x04) != 0)
            AxesStatus[(int)Axis].HighSpeed = true; // HighSpeed running mode = 1;
         else
            AxesStatus[(int)Axis].HighSpeed = false;

         if ((response[3] & 1) == 0)
            AxesStatus[(int)Axis].NotInitialized = true; // MC is not initialized.
         else
            AxesStatus[(int)Axis].NotInitialized = false;


         return AxesStatus[(int)Axis];
      }

      //public void MCSetSwitch(bool OnOff)
      //{
      //   if (OnOff)
      //      TalkWithAxis(AxisId.Axis1_RA, 'O', "1");
      //   else
      //      TalkWithAxis(AxisId.Axis1_RA, 'O', "0");
      //}

      /// <summary>
      /// One communication between mount and client
      /// </summary>
      /// <param name="Axis">The target of command</param>
      /// <param name="Command">The comamnd char set</param>
      /// <param name="cmdDataStr">The data need to send</param>
      /// <returns>The response string from mount</returns>
      private String TalkWithAxis(AxisId axis, char cmd, string cmdDataStr)
      {
         System.Diagnostics.Debug.Write(String.Format("TalkWithAxis({0}, {1}, {2})", axis, cmd, cmdDataStr));
         string response = string.Empty;

         const int BufferSize = 20;
         StringBuilder sb = new StringBuilder(BufferSize);
         sb.Append(cStartChar_Out);                  // 0: Leading char
         sb.Append(cmd);                         // 1: Length of command( Source, distination, command char, data )

         // Target Device
         sb.Append(((int)axis + 1).ToString());    // 2: Target Axis
                                                   // Copy command data to buffer
         sb.Append(cmdDataStr);

         sb.Append(cEndChar);    // CR Character            

         string cmdString = sb.ToString();
         //string.Format("{0}{1}{2}{3}{4}",
         //cStartChar_Out,
         //command,
         //(int)axis,
         //(cmdDataStr ?? "."),
         //cEndChar);

         var cmdTransaction = new EQTransaction(cmdString) { Timeout = TimeSpan.FromSeconds(TimeOut) };


         using (ICommunicationChannel channel = new SerialCommunicationChannel(EndPoint))
         using (var processor = new ReactiveTransactionProcessor()) {
            var transactionObserver = new TransactionObserver(channel);
            processor.SubscribeTransactionObserver(transactionObserver);
            try {
               channel.Open();

               // prepare to communicate
               for (int i = 0; i < Retry; i++) {

                  Task.Run(() => processor.CommitTransaction(cmdTransaction));
                  cmdTransaction.WaitForCompletionOrTimeout();
                  if (!cmdTransaction.Failed) {
                     response = cmdTransaction.Value;
                     break;
                  }
                  else {
                     Trace.TraceError(cmdTransaction.ErrorMessage.Single());
                  }
               }
            }
            catch (Exception ex) {
               Trace.TraceError("Connnection Lost");
               throw new MountControllerException(ErrorCode.ERR_NOT_CONNECTED, ex.Message);
            }
            finally {
               // To clean up, we just need to dispose the TransactionObserver and the channel is closed automatically.
               // Not strictly necessary, but good practice.
               transactionObserver.OnCompleted(); // There will be no more transactions.
               transactionObserver = null; // not necessary, but good practice.
            }

         }
         //if (string.IsNullOrWhiteSpace(response)) {
         //   if (axis == AxisId.Axis1)
         //      throw new MountControllerException(ErrorCode.ERR_NORESPONSE_AXIS1);
         //   else
         //      throw new MountControllerException(ErrorCode.ERR_NORESPONSE_AXIS2);
         //}
         System.Diagnostics.Debug.WriteLine(string.Format(" -> Response: {0} (0x{0:X})", response));
         return response;
      }


      #region Motor command set ...
      /************************ MOTOR COMMAND SET ***************************/
      // Inquire Motor Board Version ":e(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
      private void InquireMotorBoardVersion(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 'e', null);

         int tmpMCVersion = BCDstr2int(response);

         MCVersion = ((tmpMCVersion & 0xFF) << 16) | ((tmpMCVersion & 0xFF00)) | ((tmpMCVersion & 0xFF0000) >> 16);

      }

      // Inquire Grid Per Revolution ":a(*2)", where *2: '1'= CH1, '2// = CH2.
      private void InquireGridPerRevolution(AxisId Axis)
      {
         int axisId = (int)Axis;
         string response = TalkWithAxis(Axis, 'a', null);

         int gearRatio = BCDstr2int(response);
         // There is a bug in the earlier version firmware(Before 2.00) of motor controller MC001.
         // Overwrite the GearRatio reported by the MC for 80GT mount and 114GT mount.
         if ((MCVersion & 0x0000FF) == 0x80) {
            gearRatio = 0x162B97;      // for 80GT mount
         }
         if ((MCVersion & 0x0000FF) == 0x82) {
            gearRatio = 0x205318;      // for 114GT mount
         }
         // Final check taken from original EQCONTRL code
         if (gearRatio == 0) { gearRatio++; }   // Avoid DIV 0 Errors
         GridPerRevolution[axisId] = gearRatio;       // Save setting
         FactorRadToStep[axisId] = gearRatio / (2 * Math.PI);
         FactorStepToRad[axisId] = 2 * Math.PI / gearRatio;
      }


      // Inquire Timer Interrupt Freq ":b1".
      private void InquireTimerInterruptFreq(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 'b', null);

         int timeFreq = BCDstr2int(response);
         // Check taken from original EQCONTRL code to prevent DIV 0 errors
         if (timeFreq == 0) { timeFreq++; }
         StepTimerFreq[(int)Axis] = timeFreq;
         FactorRadRateToInt[(int)Axis] = (double)(StepTimerFreq[(int)Axis]) / FactorRadToStep[(int)Axis];
      }

      // Inquire high speed ratio ":g(*2)", where *2: '1'= CH1, '2// = CH2.
      private void InquireHighSpeedRatio(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 'g', null);

         int highSpeedRatio = BCDstr2int(response);
         HighSpeedRatio[(int)Axis] = highSpeedRatio;
      }

      // Inquire PEC Period ":s(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
      private void InquirePECPeriod(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 's', null);

         int PECPeriod = BCDstr2int(response);
         PESteps[(int)Axis] = PECPeriod;
      }

      // Inquire the mount parameters.  "q"
      private void InquireMountParameters()
      {
         int response = EQ_SendCommand(AxisId.Axis1_RA, 'q', 1, 6);
         if ((response & Core.Constants.EQ_ERROR) != Core.Constants.EQ_ERROR) {
            // its a later mount
            int axis = (int)AxisId.Axis1_RA;
            MountParameters[axis] = response;
            HasHalfCurrent[axis] = ((response & 0x00004000) == 0x00004000);
            HasEncoder[axis] = ((response & 0x00000001) == 0x00000001);
            HasPPEC[axis] = ((response & 0x00000002) == 0x00000002);
            HasPolarscopeLED = ((response & 0x00001000) == 0x00001000);
            HasHomeSensor = ((response & 0x00000004) == 0x00000004);
            // since the q: message is being supported read DEC axis as well
            response = EQ_SendCommand(AxisId.Axis2_DEC, 'q', 1, 6);
            if ((response & Core.Constants.EQ_ERROR) != Core.Constants.EQ_ERROR) {
               axis = (int)AxisId.Axis2_DEC;
               MountParameters[axis] = response;
               HasHalfCurrent[axis] = ((response & 0x00004000) == 0x00004000);
               HasEncoder[axis] = ((response & 0x00000001) == 0x00000001);
               HasPPEC[axis] = ((response & 0x00000002) == 0x00000002);
            }
         }
      }

      // Inquire the snap ports "O"
      private void InquireSnapPorts()
      {
         for (int i = 0; i < 2; i++) {
            int response = EQ_SendCommand(i, 'O', 0, 1);
            HasSnap[i] = ((response & Core.Constants.EQ_ERROR) != Core.Constants.EQ_ERROR);
         }
      }

      // Inquire Polar scope LED
      private void InquirePolarScopeLED()
      {
         try {
            string response = TalkWithAxis(AxisId.Axis2_DEC, 'V', null);
            // if ( no error so mount MAY have  polarscope LED
            HasPolarscopeLED = true;
         }
         catch { }
      }

      // Set initialization done ":F3", where '3'= Both CH1 and CH2.
      private void InitializeMC()
      {
         TalkWithAxis(AxisId.Axis1_RA, 'F', null);
         TalkWithAxis(AxisId.Axis2_DEC, 'F', null);
      }

      private void SetMotionMode(AxisId Axis, char func, char direction)
      {
         string szCmd = "" + func + direction;
         TalkWithAxis(Axis, 'G', szCmd);
      }

      //private void SetGotoTargetIncrement(AxisId Axis, int StepsCount)
      //{
      //   string cmd = intTo6BitHEX(StepsCount);

      //   TalkWithAxis(Axis, 'H', cmd);
      //}

      //private void SetBreakPointIncrement(AxisId Axis, int StepsCount)
      //{
      //   string szCmd = intTo6BitHEX(StepsCount);

      //   TalkWithAxis(Axis, 'M', szCmd);
      //}
      //private void SetBreakSteps(AxisId Axis, int NewBrakeSteps)
      //{
      //   string szCmd = intTo6BitHEX(NewBrakeSteps);
      //   TalkWithAxis(Axis, 'U', szCmd);
      //}

      private void SetStepPeriod(AxisId Axis, int StepsCount)
      {
         System.Diagnostics.Debug.WriteLine(String.Format("SetStepPeriod({0}, {1})", Axis, StepsCount));
         string szCmd = intTo6BitHEX(StepsCount);
         TalkWithAxis(Axis, 'I', szCmd);
      }
      private void StartMotion(AxisId Axis)
      {
         TalkWithAxis(Axis, 'J', null);
      }
      #endregion

      #region Skywatcher Helper functions ...
      private bool IsHEXChar(char tmpChar)
      {
         return ((tmpChar >= '0') && (tmpChar <= '9')) || ((tmpChar >= 'A') && (tmpChar <= 'F'));
      }
      private int HEX2Int(char HEX)
      {
         int tmp;
         tmp = HEX - 0x30;
         if (tmp > 9)
            tmp -= 7;
         return tmp;
      }
      private int BCDstr2int(string str)
      {
         // =020782 => 8521474
         try {
            int value = 0;
            for (int i = 1; i + 1 < str.Length; i += 2) {
               value += (int)(int.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.AllowHexSpecifier) * Math.Pow(16, i - 1));
            }

            // if(D)
            // Log.d(TAG,"BCDstr2int " + response + ","+value);
            return value;
         }
         catch (FormatException e) {
            throw new MountControllerException(ErrorCode.ERR_INVALID_DATA,
                            "Parse BCD Failed");
         }
         // return Integer.parseInt(response.substring(0, 2), 16)
         // + Integer.parseInt(response.substring(2, 4), 16) * 256
         // + Integer.parseInt(response.substring(4, 6), 16) * 256 * 256;
      }
      private string intTo6BitHEX(int number)
      {
         // 31 -> 0F0000
         String A = ((int)number & 0xFF).ToString("X").ToUpper();
         String B = (((int)number & 0xFF00) / 256).ToString("X").ToUpper();
         String C = (((int)number & 0xFF0000) / 256 / 256).ToString("X").ToUpper();

         if (A.Length == 1)
            A = "0" + A;
         if (B.Length == 1)
            B = "0" + B;
         if (C.Length == 1)
            C = "0" + C;

         // if (D)
         // Log.d(TAG, "intTo6BitHex " + number + "," + A + "," + B + "," + C);

         return A + B + C;
      }

      private void PrepareForSlewing(AxisId Axis, double speed)
      {
         char cDirection;

         var axesstatus = MCGetAxisStatus(Axis);
         if (!axesstatus.FullStop) {
            if ((axesstatus.SlewingTo) ||                               // GOTO in action
                 (axesstatus.HighSpeed) ||                              // Currently high speed slewing
                 (Math.Abs(speed) >= LOW_SPEED_MARGIN) ||               // Will be high speed slewing
                 ((axesstatus.SlewingForward) && (speed < 0)) ||        // Different direction
                 (!(axesstatus.SlewingForward) && (speed > 0))          // Different direction
                ) {
               // We need to stop the motor first to change Motion Mode, etc.
               MCAxisStop(Axis);
            }
            else
               // Other situatuion, there is no need to set motion mode.
               return;



            // Wait until the axis stop
            while (true) {
               // Update Mount status, the status of both axes are also updated because _GetMountStatus() includes such operations.
               axesstatus = MCGetAxisStatus(Axis);

               // Return if the axis has stopped.
               if (axesstatus.FullStop)
                  break;

               Thread.Sleep(100);

               // if ( the axis is asked to stop.
               // if ( (!AxesAskedToRun[Axis] && !(MountStatus & Constants.MOUNT_TRACKING_ON)) )		// if ( AXIS1 or AXIS2 is asked to stop or 
               //	return ERR_USER_INTERRUPT;

            }

         }
         if (speed > 0.0) {
            cDirection = '0';
         }
         else {
            cDirection = '1';
            speed = -speed;                     // Get absolute value of Speed.
         }

         if (speed > LOW_SPEED_MARGIN) {
            SetMotionMode(Axis, '3', cDirection);              // Set HIGH speed slewing mode.
         }
         else
            SetMotionMode(Axis, '1', cDirection);              // Set LOW speed slewing mode.

      }


      #endregion



      #endregion

   }
}
