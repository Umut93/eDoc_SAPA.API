using System;
using System.Diagnostics;
using System.Security.Principal;

namespace Fujitsu.eDoc.SAPA.WebService.Services
{
    public class LogService
    {
        public static void LogToEventLog(string message, System.Diagnostics.EventLogEntryType type)
        {
            try
            {
                using (WindowsImpersonationContext wic = WindowsIdentity.Impersonate(IntPtr.Zero))
                {
                    string source = "Fujitsu.eDoc.SAPA";
                    if (!EventLog.SourceExists(source))
                        EventLog.CreateEventSource(source, "Application");

                    EventLog.WriteEntry(source, message, type, 234, 1);
                }
            }
            catch
            {
                // Do nothing, we give up
            }
        }


        public static void LogToMsgDistributorEventLog(string message, System.Diagnostics.EventLogEntryType type)
        {
            try
            {
                using (WindowsImpersonationContext wic = WindowsIdentity.Impersonate(IntPtr.Zero))
                {
                    string source = "Fujitsu.eDoc.SAPA.WebService.Controllers";
                    if (!EventLog.SourceExists(source))
                        EventLog.CreateEventSource(source, "MsgDistributor");

                    EventLog.WriteEntry(source, message, type, 234, 1);
                }
            }
            catch
            {

            }
        }

    }
}