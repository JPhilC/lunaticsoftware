using System;

namespace Lunatic.SyntaController
{
   [Serializable]
   public class MountControllerException : Exception
   {
      private ErrorCode ErrCode;
      private string ErrMessage;
      public MountControllerException(ErrorCode err)
      {
         ErrCode = err;
      }
      public MountControllerException(ErrorCode err, String message)
      {
         ErrCode = err;
         ErrMessage = message;
      }
   }
}
