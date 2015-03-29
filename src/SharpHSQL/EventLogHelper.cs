using System;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

//For Private Classes
using System.Timers;
using Microsoft.Win32;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using System.Security;
using System.Security.Principal;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace SharpHsql
{	
	/// <summary>
	/// Provides static methods that supply helper utilities for event log data access. This class cannot be inherited.
	/// </summary>
	/// <remarks>
	/// A more general logger class is <see cref="LogHelper"/> that uses the <see cref="log4net.ILog"/> interface 
	/// used by applications to log messages into the log4net framework. 
	/// </remarks>
	/// <author>Andrés G Vettori</author>
	sealed class EventLogHelper
	{
		#region Private utility methods & constructors

		//Private, Static Object Field
		//used purely for synchronization
		private static Object _objLock = new Object();

		private static readonly byte[] _rawData;

		//Since this class provides only static methods, make the default constructor private to prevent 
		//instances from being created with "new EventLogHelper()".
		private EventLogHelper() {}

		static EventLogHelper()
		{
			_rawData = Encoding.ASCII.GetBytes(typeof(EventLogHelper).AssemblyQualifiedName);
			_defaultLogSource = "SharpHsql";
		}

		private static void ExceptionLog(string message, Exception e)
		{
			try
			{
				string source = String.Concat("ASP.NET " , Environment.Version.ToString());
				message = String.Concat(Environment.NewLine , Environment.NewLine , "Exception catched in Retina.Utility.EventLogHelper." , Environment.NewLine , 
					e.Message , Environment.NewLine , Environment.NewLine ,
					"Original Message: " , message);					  
					      
				EventLog.WriteEntry(source, message, EventLogEntryType.Warning);
			} 
			catch(Exception ex)
			{
				System.Diagnostics.Trace.WriteLine(ex.Message);
			}
		}

		#endregion

		#region Public methods

		#region WriteLogEntry
		/// <summary>
		/// Writes an entry in the event log.
		/// </summary>
		/// <param name="message">The string to write to the event log.</param>
		static public void WriteLogEntry(string message)
		{
			WriteLogEntry(message, EventLogEntryType.Information, DefaultLogSource, DefaultLogType,  0);	
		}

		/// <summary>
		/// Writes an entry in the event log.
		/// </summary>
		/// <param name="message">The string to write to the event log.</param>
		/// <param name="eventType">One of the System.Diagnostics.EventLogEntryType values.</param>
		static public void WriteLogEntry(string message, EventLogEntryType eventType)
		{
			WriteLogEntry(message, eventType, DefaultLogSource, DefaultLogType, 0);	
		}

		/// <summary>
		/// Writes an entry in the event log.
		/// </summary>
		/// <param name="message">The string to write to the event log.</param>
		/// <param name="eventType">One of the System.Diagnostics.EventLogEntryType values.</param>
		/// <param name="logsource">The source by which the application is registered on the specified computer.</param>
		static public void WriteLogEntry(string message, EventLogEntryType eventType, string logsource)
		{
			WriteLogEntry(message, eventType, logsource, DefaultLogType, 0);				
		}

		/// <summary>
		/// Writes an entry in the event log.
		/// </summary>
		/// <param name="message">The string to write to the event log.</param>
		/// <param name="eventType">One of the System.Diagnostics.EventLogEntryType values.</param>
		/// <param name="logsource">The source by which the application is registered on the specified computer.</param>
		/// <param name="logtype">
		/// The name of the log the source's entries are written to. 
		/// Possible values include: Application, Security, System, or a custom event log. 
		/// If you do not specify a value, the logName defaults to Application. 
		/// </param>
		static public void WriteLogEntry(string message, EventLogEntryType eventType, string logsource,	string logtype)
		{
			WriteLogEntry(message, eventType, logsource, logtype,0);				
		}

		/// <summary>
		/// Writes an entry in the event log.
		/// </summary>
		/// <param name="message">The string to write to the event log.</param>
		/// <param name="eventType">One of the System.Diagnostics.EventLogEntryType values.</param>
		/// <param name="logsource">The source by which the application is registered on the specified computer.</param>
		/// <param name="logtype">
		/// The name of the log the source's entries are written to. 
		/// Possible values include: Application, Security, System, or a custom event log. 
		/// If you do not specify a value, the logtype defaults to Application. 
		/// </param>
		/// <param name="eventID">The application-specific identifier for the event.</param>
		static public void WriteLogEntry(string message, EventLogEntryType eventType, string logsource, string logtype, int eventID)
		{
			if (logsource == null || logsource.Length == 0)
				logsource = DefaultLogSource;

			try
			{
				//.NET EventLog Class
				if (!EventLog.SourceExists(logsource))
					EventLog.CreateEventSource(logsource, logtype);

				EventLog.WriteEntry(logsource, message, eventType, eventID, 0, _rawData);
			}
			catch (Exception e)
			{
				ExceptionLog(message,e);
			}
		}
		#endregion

		#region RemoveLogType
		/// <summary>
		/// Removes the LogType.
		/// </summary>
		static public void RemoveLogType()
		{
			RemoveLogType(DefaultLogType);				
		}

		/// <summary>
		/// Removes an event log from the local computer.
		/// </summary>
		/// <param name="logtype">
		/// The name of the log the source's entries are written to. 
		/// Possible values include: Application, Security, System, or a custom event log. 
		/// If you do not specify a value, the logName defaults to Application. 
		/// </param>
		static public void RemoveLogType(string logtype)
		{
			try
			{
				EventLog.Delete(logtype);				
			} 
			catch (Exception e)
			{
				ExceptionLog("Removing Log Type",e);			
			}
		}
		#endregion

		#region RemoveEventSource
		/// <summary>
		/// Removes an event log from the local computer.
		/// </summary>
		static public void RemoveEventSource()
		{
			RemoveEventSource(DefaultLogSource);				
		}

		/// <summary>
		/// Removes the event source registration from the event log of the local computer.
		/// </summary>
		/// <param name="logsource">The source by which the application is registered on the specified computer.</param>
		static public void RemoveEventSource(string logsource)
		{
			try
			{
				EventLog.DeleteEventSource(logsource);				
			} 
			catch (Exception e)
			{
				ExceptionLog("Removing Event Source",e);
			}
		}
		#endregion

		#endregion

		#region Public properties

		/// <summary>
		/// Default Log Type.
		/// </summary>
		public static string DefaultLogType
		{
			get	{return "Application";}
		}

		/// <summary>
		/// Default Log Source.
		/// </summary>
		public static string DefaultLogSource
		{
			get	{return _defaultLogSource;}
		} private static string _defaultLogSource;
	
		#endregion
	}
}