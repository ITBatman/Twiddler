using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ServiceBusListener.Utilities
{
  public static class ExceptionHelper
  {
    public static void DisplayException(this Exception pException)
    {
      StringBuilder exceptionstack = new StringBuilder();
      Exception exception = pException;
            
      while (exception != null)
      {
        exceptionstack.AppendLine(exception.Message);
        exceptionstack.AppendLine("--------------------------------------");
        exception = exception.InnerException;
      }

      exceptionstack.AppendLine("************************************");
      exceptionstack.AppendLine("CallStack follows:");

      exceptionstack.AppendLine(pException.StackTrace);
      
      MessageBox.Show(exceptionstack.ToString(), "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void WriteMessageToLog(string pMessage, EventLogEntryType EventLogEntryType = EventLogEntryType.Information)
    {
      string sSource;
      string sLog;

      sSource = "Twiddler";
      sLog = "Application";

      if (!EventLog.SourceExists(sSource))
        EventLog.CreateEventSource(sSource, sLog);

      EventLog.WriteEntry(sSource, pMessage, EventLogEntryType);
    }

    
  }
}
