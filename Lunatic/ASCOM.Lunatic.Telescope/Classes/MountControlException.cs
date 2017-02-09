using System;

namespace ASCOM.Lunatic
{
   public class MountControlException : Exception
   {
      private ErrorCode ErrCode;
      private string ErrMessage;
      public MountControlException(ErrorCode err)
      {
         ErrCode = err;
      }
      public MountControlException(ErrorCode err, String message)
      {
         ErrCode = err;
         ErrMessage = message;
      }
   }
}
