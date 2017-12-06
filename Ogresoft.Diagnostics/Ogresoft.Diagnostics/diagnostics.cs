using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Security.Principal;

namespace Ogresoft
{
    /// <summary>
    /// Class that contains assorted functionality for making an application debuggable. 
    /// </summary>

    public class Diagnostics
    {
        //private DiagnosticsSettings _settings = new DiagnosticsSettings();
        public Diagnostics()
        {
            //if (_settings.Trace)
            //{
                //_traceLogger = new TraceLogger();
            //}

            string sourceName = "Ogresoft"; 
            string logName = "Ogresoft";

            _eventLogWriter = new EventLogWriter(sourceName, logName); 
        }


        //private TraceLogger _traceLogger;
        private EventLogWriter _eventLogWriter;

        /// <summary>
        /// indicates that verbose tracing should be on. 
        /// </summary>

        //public bool Verbose

        //{
            //get
            //{
                //return _settings.TraceVerbose;
            //}
        //}

        //I can't make this part of the static constructor because the SetUnhandledExceptionMode needs to be called at a controlled point in the code. 
        //It will throw an error if a control is already initialized on the thread that calls this. 
        public void InitializeExceptionHandling()
        {
            //DlsTraceListener.Start();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            //System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            //System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.Automatic);
            _eventLogWriter.WriteInfo("Started");
        }

        public void LogEvent(string message)
        {
            _eventLogWriter.WriteInfo(message);
        }

        public void LogWarning(string message)
        {
            _eventLogWriter.WriteWarning(message);
        }

        void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            PublishException(e.Exception);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            PublishException(e.ExceptionObject as Exception);
        }

        /// <summary>
        /// Writes the exception to the event log.
        /// </summary>
        /// <param name="ex"></param>
        public void PublishException(Exception ex)
        {
            string info = GetVerboseExceptionInfo(ex);
            _eventLogWriter.WriteException(info);
            Trace(info);

            if (Debugger.IsAttached)
                Debugger.Break();
        }

        /// <summary>
        /// Writes the exception to the event log, but flags it as a warning.
        /// </summary>
        /// <param name="ex"></param>
        public void PublishWarning(Exception ex)
        {
            string info = GetVerboseExceptionInfo(ex);

            _eventLogWriter.WriteInfo(info);

            Trace(info);

        }

        /// <summary>
        /// Writes the exception to trace output, but not to the event log.
        /// </summary>
        /// <param name="ex"></param>
        public void TraceWarning(Exception ex)
        {
            string info = GetVerboseExceptionInfo(ex);
            Trace(info);
        }

        /// <summary>
        /// Conditionally enters a trace statement. 
        /// </summary>
        /// <param name="trace"></param>
        public void TraceIf(bool trace)
        {
            if (trace)
                Trace();
        }

        /// <summary>
        /// Logs the assembly, class, and method that called Trace() to the log file. 
        /// </summary>
        public void Trace()
        {
            Trace(string.Empty);
        }

        /// <summary>
        /// Conditionally traces a variable amount of objects. 
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void TraceIf(bool trace, params object[] args)
        {
            if (trace)
                Trace(args);
        }

        /// <summary>
        /// Logs the assembly, class, and method that called Trace, along with the specified arguments to the log file.
        /// </summary>
        /// <param name="args"></param>
        public void Trace(params object[] args)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                sb.Append(args[i].ToString() + " ");
            }

            Trace(sb.ToString());
        }

        /// <summary>
        /// Conditionally traces an array of bytes.
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public void TraceIf(bool trace, byte[] bytes)
        {
            if (trace)
                Trace(bytes);
        }

        /// <summary>
        /// Writes the bytes to the trace file in a readable format. 
        /// </summary>
        /// <param name="bytes"></param>
        public void Trace(byte[] bytes)
        {
            if (bytes == null)
            {
                Trace("null bytes");
                return;
            }

            Trace(ToHexString(bytes));
        }

        public string ToHexString(byte[] bytes)
        {
            return ToHexString(bytes, '-', true);
        }

        public string ToDatabaseBinary(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("0x");

            for (int i = 0; i < bytes.Length; i++)
            {

                if (bytes[i] <= 0x0F)
                {
                    sb.Append("0");
                }

                sb.Append(bytes[i].ToString("X"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the specified bytes to a string displaying hexidecimal. 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToHexString(byte[] bytes, char delimeter, bool useSpaceOnFourth)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {

                if (bytes[i] <= 0x0F)
                {
                    sb.Append("0");
                }

                sb.Append(bytes[i].ToString("X"));

                if (i != bytes.Length - 1)
                    if (useSpaceOnFourth)
                        sb.Append((i % 4 == 3) ? " " : delimeter.ToString());
                    else
                        sb.Append(delimeter.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets a stack trace, trimming the bottom to not include methods contained certian types
        /// </summary>
        /// <param name="ignoredClasses">Types to filter out</param>
        /// <returns>A tack trace</returns>

        public static StackTrace GetStackTrace(params Type[] ignoredClasses)
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);

            for (int i = 0; i < st.FrameCount; i++)
            {
                MethodBase methodBase = st.GetFrame(i).GetMethod();

                if (methodBase.DeclaringType == typeof(Ogresoft.Diagnostics))
                {
                    continue;
                }

                foreach (Type type in ignoredClasses)
                {
                    if (Type.Equals(methodBase.DeclaringType, ignoredClasses))
                    {
                        continue;
                    }
                }

                return new System.Diagnostics.StackTrace(i, true);
            }

            return st;
        }

        /// <summary>
        /// Conditionally traces a message. 
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public void TraceIf(bool trace, string message)
        {
            if (trace)
                Trace(message);
        }

        /// <summary>
        /// Logs the message and the assembly, class, and method that called Trace to the log file. 
        /// </summary>
        public void Trace(string message)
        {
            //The stack trace is used to get the class and assembly name of the method that called this Trace method. 
            //I've tested it and in my tests the additional time of doing reflection was negligible. 

            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();

            string className;
            string methodName;
            string assemblyName;

            for (int i = 0; i < st.FrameCount; i++)
            {
                MethodBase methodBase = st.GetFrame(i).GetMethod();

                if (methodBase.DeclaringType == typeof(Ogresoft.Diagnostics))
                    continue;

                if (methodBase.DeclaringType.BaseType == typeof(Ogresoft.Diagnostics))
                    continue;

                className = methodBase.DeclaringType.FullName;
                methodName = methodBase.Name;
                assemblyName = methodBase.DeclaringType.Assembly.GetName().Name;

                //string traceMessage = assemblyName + "." + className + "." + methodName + " " + message;

                string traceMessage = className + "." + methodName + " " + message;

                System.Diagnostics.Trace.WriteLine(traceMessage);

                // if (_settings.Trace)
                //    _traceLogger.Write(traceMessage);

                return;
            }

            //unreachable. 
            Debug.Assert(false);
        }

        /// <summary>
        /// Conditionally traces an object. 
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="ob"></param>
        /// <returns></returns>
        public void TraceIf(bool trace, object ob)
        {
            if (trace)
                Trace(ob);
        }

        /// <summary>
        /// Writes the string representation of the object to the log file. 
        /// </summary>
        /// <param name="ob">The object that is being written to the log file.</param>
        public void Trace(object ob)
        {
            Trace(ob.ToString());
        }

        public static string GetVerboseExceptionInfo(Exception ex)
        {
            string separator = "----------"; 
            StringBuilder strInfo = new StringBuilder();

            System.Exception currentException = ex;

            int exceptionCount = 1;

            while (currentException != null)
            {
                strInfo.AppendLine(); 
                strInfo.AppendLine(separator);
                strInfo.AppendLine("Type: " + ex.GetType().ToString());

                exceptionCount++;

                PropertyInfo[] publicProperties = currentException.GetType().GetProperties();

                foreach (PropertyInfo p in publicProperties)
                {
                    // Do not log information for the InnerException, StackTrace or TargetSite. This information is 
                    // captured later in the process.

                    /*
                    TargetSite cannot be published because:
                    If a server method throws this exception, then this property contains information about
                    a method in a server side assembly. If this exception is serialized from server to a client
                    then a client will not be able to reflect on this field. (It cant find the assembly).
                    It dosen't matter much since the stack trace published later will explain what method
                    threw the exception.
                    */

                    if (p.Name == "InnerException" || p.Name == "StackTrace" || p.Name == "TargetSite")
                        continue;

                    System.Collections.IDictionary dictionary = p.GetValue(currentException, null) as System.Collections.IDictionary;

                    if (dictionary != null)
                    {
                        foreach (System.Collections.DictionaryEntry de in dictionary)
                        {
                            strInfo.AppendLine(string.Format("{0}\t{1}: {2}", "", de.Key.ToString(), de.Value.ToString()));
                        }

                        continue;
                    }

                    if (p.GetValue(currentException, null) == null)
                    {
                        strInfo.AppendLine(string.Format("{0}{1}: NULL", "", p.Name));
                        continue;
                    }

                    strInfo.AppendLine(string.Format("{0}{1}: {2}", "", p.Name, p.GetValue(currentException, null)));
                }

                // Record the StackTrace with separate label.
                if (currentException.StackTrace != null)
                {
                    strInfo.AppendLine("StackTrace: " + currentException.StackTrace); 
                }

                // Reset the temp exception object and iterate the counter.
                currentException = currentException.InnerException;
            }

            strInfo.AppendLine(separator);
            strInfo.AppendLine("Environment.UserName: " + Environment.UserName);
            strInfo.AppendLine("ApplicationBase: " + System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
            strInfo.AppendLine("Environment.CurrentDirectory: " + Environment.CurrentDirectory);
            strInfo.AppendLine("Environment.CommandLine: " + Environment.CommandLine);
            strInfo.AppendLine("Environment.MachineName: " + Environment.MachineName);
            strInfo.AppendLine("Environment.OSVersion: " + Environment.OSVersion);
            strInfo.AppendLine("Environment.ProcessorCount: " + Environment.ProcessorCount);
            strInfo.AppendLine("Time: " + DateTime.Now);
            strInfo.AppendLine("User Interactive: " + Environment.UserInteractive);
            strInfo.AppendLine("Windows Version: " + Environment.Version);
            strInfo.AppendLine("WorkingSet: " + Environment.WorkingSet);
            strInfo.AppendLine("AppDomain.CurrentDomain.FriendlyName: " + AppDomain.CurrentDomain.FriendlyName);
            strInfo.AppendLine("AppDomain.CurrentDomain.RelativeSearchPath: " + AppDomain.CurrentDomain.RelativeSearchPath);
            strInfo.AppendLine("Process.StartTime: " + Process.GetCurrentProcess().StartTime);
            strInfo.AppendLine("Process.Threads.Count: " + Process.GetCurrentProcess().Threads.Count);
            strInfo.AppendLine("Process.ProcessName: " + Process.GetCurrentProcess().ProcessName); 

            return strInfo.ToString();
        }
    }
}