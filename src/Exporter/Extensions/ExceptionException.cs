using System;

namespace CodeCave.Threejs.Revit.Exporter
{
    public static class ExceptionException
    {
        public static string GetAllMessages(this Exception exc)
        {
            if (exc is null)
                return string.Empty;

            var exception = exc;
            var message = string.Empty;
            while (exception != null)
            {
                message += exception.Message;
                exception = exception.InnerException;
            }

            return message;
        }

        public static string GetStackTrace(this Exception exc)
        {
            if (exc is null)
                return string.Empty;

            var exception = exc;
            var stackTrace = string.Empty;
            while (exception != null)
            {
                stackTrace += exception.StackTrace;
                exception = exception.InnerException;
            }

            return stackTrace;
        }

        public static string Dump(this Exception exc)
        {
            return string.Join(Environment.NewLine, exc.GetAllMessages(), exc.GetStackTrace());
        }
    }
}
