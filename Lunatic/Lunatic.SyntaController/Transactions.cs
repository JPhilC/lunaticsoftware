﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TA.Ascom.ReactiveCommunications;
using System.Reactive.Linq;
using System.Diagnostics;
using TA.Ascom.ReactiveCommunications.Diagnostics;
using System.Runtime.InteropServices;

namespace Lunatic.SyntaController.Transactions
{
   /// <summary>
   /// Transaction for communicating with modified EQ (Synta) mounts
   /// </summary>
   [ComVisible(false)]
   public class EQTransaction : DeviceTransaction
   {

      readonly char responseInitiator;
      readonly char errorInitiator;
      readonly char terminator;

      /// <summary>
      ///     Initializes a new instance of the <see cref="DeviceTransaction" /> class.
      /// </summary>
      /// <param name="command">The command to be sent to the communications channel.</param>
      /// <param name="terminator">The terminator character. Optional; defaults to '/r'.</param>
      /// <param name="responseInitiator">The response initiator for good responses. Optional; defaults to '='. Not used, but is stripped from
      /// the start of the response (if present).</param>
      /// <param name="errorInitiator">The response initiator for errors. Optional; defaults to '!'. Not used, but is stripped from
      /// the start of the response (if present).</param>
      public EQTransaction(string command, char terminator = (char)13, char responseInitiator = '=', char errorInitiator = '!')
            : base(command)
            {
         this.responseInitiator = responseInitiator;
         this.errorInitiator = errorInitiator;
         this.terminator = terminator;
         Value = string.Empty;
      }

      /// <summary>
      ///     Gets the final response value.
      /// </summary>
      /// <value>The value as a string.</value>
      public string Value { get; private set; }
      public bool Error { get; private set; }

      /// <summary>
      ///     Observes the character sequence from the communications channel
      ///     until a satisfactory response has been received.
      /// </summary>
      /// <param name="source">The source sequence.</param>
      public override void ObserveResponse(IObservable<char> source)
      {
         source.TerminatedStrings(terminator)
             .Take(1)
             .Subscribe(OnNext, OnError, OnCompleted);
      }

      /// <summary>
      ///     Called when the response sequence completes. This indicates a successful transaction. If a valid
      ///     response was received, then delimiters are stripped off and the unterminated string is copied into the
      ///     <see cref="Value" /> property.
      /// </summary>
      protected override void OnCompleted()
      {
         if (Response.Any()) {
            var responseString = Response.Single();
            System.Diagnostics.Debug.WriteLine(string.Format("Raw response: {0}", responseString));
            Value = responseString.TrimStart(responseInitiator).TrimEnd(terminator);
            if (string.IsNullOrEmpty(Value)) {
               // Check if we have an error instead.
               string error = responseString.TrimStart(errorInitiator).TrimEnd(terminator);
               if (!string.IsNullOrEmpty(error)) {
                  ErrorMessage = new Maybe<string>(error);
                  Failed = true;
               }
            }
            else {
               Value = responseString;
            }
         }
         base.OnCompleted();
      }
   }

   [ComVisible(false)]
   public class EQContrlTransaction : DeviceTransaction
   {

      readonly char responseInitiator;
      readonly char errorInitiator;
      readonly char terminator;

      /// <summary>
      ///     Initializes a new instance of the <see cref="DeviceTransaction" /> class.
      /// </summary>
      /// <param name="command">The command to be sent to the communications channel.</param>
      /// <param name="terminator">The terminator character. Optional; defaults to '/r'.</param>
      /// <param name="responseInitiator">The response initiator for good responses. Optional; defaults to '='. Not used, but is stripped from
      /// the start of the response (if present).</param>
      /// <param name="errorInitiator">The response initiator for errors. Optional; defaults to '!'. Not used, but is stripped from
      /// the start of the response (if present).</param>
      public EQContrlTransaction(string command, char terminator = (char)13, char responseInitiator = '=', char errorInitiator = '!')
            : base(command)
      {
         this.responseInitiator = responseInitiator;
         this.errorInitiator = errorInitiator;
         this.terminator = terminator;
         Value = 0;
      }

      /// <summary>
      ///     Gets the final response value.
      /// </summary>
      /// <value>The value as a string.</value>
      public int Value { get; private set; }
      public bool Error { get; private set; }

      /// <summary>
      ///     Observes the character sequence from the communications channel
      ///     until a satisfactory response has been received.
      /// </summary>
      /// <param name="source">The source sequence.</param>
      public override void ObserveResponse(IObservable<char> source)
      {
         source.TerminatedStrings(terminator)
             .Take(1)
             .Subscribe(OnNext, OnError, OnCompleted);
      }

      /// <summary>
      ///     Called when the response sequence completes. This indicates a successful transaction. If a valid
      ///     response was received, then delimiters are stripped off and the unterminated string is copied into the
      ///     <see cref="Value" /> property.
      /// </summary>
      protected override void OnCompleted()
      {
         if (Response.Any()) {
            var responseString = Response.Single();
            System.Diagnostics.Debug.WriteLine(string.Format("    -> Raw response: {0}", responseString));
            if (responseString[0] == responseInitiator) {
               string response = responseString.TrimStart(responseInitiator).TrimEnd(terminator);
               char[] tmp = response.ToLower().ToCharArray();
               switch (responseString.Length) {
                  case 8:
                     // Three bytes (6 nibbles) returned
                     Value = (HexNibbleToInt(tmp[0]) << 4) + HexNibbleToInt(tmp[1]) +
                        (HexNibbleToInt(tmp[2]) << 12) + (HexNibbleToInt(tmp[3]) << 8) +
                        (HexNibbleToInt(tmp[4]) << 20) + (HexNibbleToInt(tmp[5]) << 16);
                     break;
                  case 6:
                     // Two bytes (4 nibbles) returned
                     Value = (HexNibbleToInt(tmp[0]) << 4) + HexNibbleToInt(tmp[1]) +
                        (HexNibbleToInt(tmp[2]) << 12) + (HexNibbleToInt(tmp[3]) << 8);
                     break;
                  case 5:
                     // Three nibbles returned
                     Value = (HexNibbleToInt(tmp[0]) << 4) + HexNibbleToInt(tmp[1]) +
                        (HexNibbleToInt(tmp[2]) << 8);
                     break;
                  case 4:
                     // One byte  (2 nibbles) returned
                     Value = (HexNibbleToInt(tmp[0]) << 4) + HexNibbleToInt(tmp[1]);
                     break;
                  case 3:
                     // One nibble returned
                     Value = HexNibbleToInt(tmp[0]);
                     break;
                  case 2:
                     // No return value
                     Value = Core.Constants.EQ_OK;
                     break;
                  default:
                     // Return 'Bad command' error
                     Value = Core.Constants.EQ_BADPACKET;
                     break;
               }
            }
            else if (responseString[0] == errorInitiator) {
               string error = responseString.TrimStart(errorInitiator).TrimEnd(terminator);
               if (!string.IsNullOrEmpty(error)) {
                  ErrorMessage = new Maybe<string>(error);
                  char[] inpstring = error.ToCharArray();
                  // Received '!' so next byte is error code
                  switch (inpstring[0] & 0x0f) {
                     case 1:
                        // missing parameter or to many parameters
                        Value = Core.Constants.EQ_BADPACKET;
                        break;
                     case 2:
                        // cannot eecute at this time
                        Value = Core.Constants.EQ_MOUNTBUSY;
                        break;
                     case 3:
                        // non hex character sent 
                        Value = Core.Constants.EQ_BADVALUE;
                        break;
                     case 4:
                        // motor coils inactive
                        Value = Core.Constants.EQ_NOMOUNT;
                        break;
                     case 8:
                        // PPEC table invalid / not set
                        Value = Core.Constants.EQ_PPECERROR;
                        break;
                     default:
                     case 0:
                        // general error
                        Value = Core.Constants.EQ_ERROR;
                        break;
                  }
                  Failed = true;
               }
            }
            else {
               // general error
               Value = Core.Constants.EQ_ERROR;
               Failed = true;
            }
         }
         base.OnCompleted();
      }

      /////////////////////////////////////////////////////////////////////////////////////
      /** \brief	Function name		: HexNibbleToInt
        * \brief  Original taken from : EQContrl.dll EQCom::EQ_Hv
        * \brief	Description			: Convert the hexadecimal nibble to binary
        * \param	BYTE				: nibble to convert
        * \return	BYTE				: binary value of the nibble
        *
        */

      int HexNibbleToInt(char ch)
      {
         if ((ch >= '0') && (ch <= '9')) {
            return (ch & 0x0f);
         }
         if ((ch >= 'a') && (ch <= 'f')) {
            return ((ch & 0x0f) + 9);
         }
         if ((ch >= 'A') && (ch <= 'F')) {
            return ((ch & 0x0f) + 9);
         }
         return 0;
         // ### AJ  no warning if crap data accidentally sent
      }

   }
}

