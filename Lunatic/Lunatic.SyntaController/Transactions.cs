using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TA.Ascom.ReactiveCommunications;
using System.Reactive.Linq;
using System.Diagnostics;
using TA.Ascom.ReactiveCommunications.Diagnostics;

namespace Lunatic.SyntaController.Transactions
{
   /// <summary>
   /// Transaction for communicating with modified EQ (Synta) mounts
   /// </summary>
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
            Value = responseString.TrimStart(responseInitiator).TrimEnd(terminator);
            if (string.IsNullOrEmpty(Value)) {
               // Check if we have an error instead.
               string error = responseString.TrimStart(errorInitiator).TrimEnd(terminator);
               if (!string.IsNullOrEmpty(error)) {
                  ErrorMessage = new Maybe<string>(error);
                  Failed = true;
               }
            }
         }
         base.OnCompleted();
      }
   }
}

