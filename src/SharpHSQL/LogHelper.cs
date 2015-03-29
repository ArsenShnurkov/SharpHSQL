#region using
using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.IO;

#if !POCKETPC
using log4net;
using log4net.Config;
using log4net.spi;
#endif
#endregion

namespace SharpHsql
{
	/// <summary>
	/// Provides static methods that supply helper utilities for logging data whith the ILog object. 
	/// This class cannot be inherited. 
	/// </summary>
	/// <author>Andrés G Vettori</author>
	sealed class LogHelper
	{
		#region Constants

		//log4net XML constants.
		
		/// <summary>
		/// TargetNamespace.
		/// </summary>
		public const string TargetNamespace = "http://log4net.sourceforge.net";

		/// <summary>
		/// DefaultPrefix.
		/// </summary>
		public const string DefaultPrefix = "log4net";

		/// <summary>
		/// log.
		/// </summary>
		public const string RootElement = "log";
		
		#if !POCKETPC
		private static readonly string newLine = Environment.NewLine;
		#else
		private const string newLine = "\n\r";
		#endif

		#endregion Constants

		#region Enums
		/// <summary>
		/// Specifies the event type of an log entry.
		/// </summary>
		/// <remarks>
		/// The type of an log entry is used to indicate the severity of a log entry.
		/// Each log must be of a single type, which the application indicates when it reports the log.
		/// </remarks>
		public enum LogEntryType
		{
			/// <summary>
			/// An audit log. This indicates a successful audit.
			/// </summary>
			Audit,
			/// <summary>
			/// A debug log. This is for testing and debugging operations.
			/// </summary>
			Debug,
			/// <summary>
			/// An information log. This indicates a significant, successful operation.
			/// </summary>
			Information,
			/// <summary>
			/// A warning log. 
			/// This indicates a problem that is not immediately significant, 
			/// but that may signify conditions that could cause future problems.
			/// </summary>
			Warning,
			/// <summary>
			/// An error log. 
			/// This indicates a significant problem the user should know about; 
			/// usually a loss of functionality or data.
			/// </summary>
			Error,
			/// <summary>
			/// An fatal log. 
			/// This indicates a fatal problem the user should know about; 
			/// allways a loss of functionality or data.
			/// </summary>
			Fatal
		}
		#endregion

		#region Private utility methods & constructors

		//Since this class provides only static methods, make the default constructor private to prevent 
		//instances from being created with "new LogHelper()".
		private LogHelper() {}

		static LogHelper()
		{
			#if !POCKETPC
			//Configure log4net
			try
			{
				//If repository is already configured, skip this step.
				if (!LogManager.GetLoggerRepository(Assembly.GetExecutingAssembly()).Configured)
					//Look in our assembly same folder or Machine.config'
					DOMConfigurator.ConfigureAndWatch(new FileInfo(Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location) , "log4net.config")));
			} 
			catch(Exception e)
			{
				EventLogHelper.WriteLogEntry(e.Message, EventLogEntryType.Warning);
			}
			#endif
		}

		private static string InternalFormattedMessage(string message, Exception exception, Assembly assembly)
		{
			return InternalFormattedMessage(message, exception, assembly, true);
		}

		private static string InternalFormattedMessage(string message, Exception exception, Assembly assembly, bool showStack)
		{
			const string TEXT_SEPARATOR = "*********************************************";			

			// Create StringBuilder to maintain publishing information.
			StringBuilder strInfo = new StringBuilder(String.Concat(newLine, newLine, message, newLine, newLine));

			try
			{
				if (exception != null)
				{
					#region Loop through each exception class in the chain of exception objects
					// Loop through each exception class in the chain of exception objects.
				
					if(message == null) message = exception.Message;

					Exception currentException = exception; // Temp variable to hold BaseApplicationException object during the loop.

					int intExceptionCount = 1;// Count variable to track the number of exceptions in the chain.
					
					do
					{
						// Write title information for the exception object.
						#if !POCKETPC
						strInfo.AppendFormat("{1}) Exception Information{0}{2}", newLine, intExceptionCount.ToString(System.Globalization.CultureInfo.InvariantCulture), TEXT_SEPARATOR);
						strInfo.AppendFormat("{0}Exception Type: {1}", newLine, currentException.GetType().FullName);
						#else
						strInfo.AppendFormat(null, "{1}) Exception Information{0}{2}", newLine, intExceptionCount.ToString(), TEXT_SEPARATOR);
						strInfo.AppendFormat(null, "{0}Exception Type: {1}", newLine, currentException.GetType().FullName);
						#endif
				
						#region Loop through the public properties of the exception object and record their value
						// Loop through the public properties of the exception object and record their value.
						PropertyInfo[] aryPublicProperties = currentException.GetType().GetProperties();
						foreach (PropertyInfo p in aryPublicProperties)
						{
							// Do not log information for the InnerException or StackTrace. This information is 
							// captured later in the process.
							if (!p.Name.Equals("InnerException") && !p.Name.Equals("StackTrace") && !p.Name.Equals("BaseInnerException"))
							{
								object prop = null;
								try
								{
									prop = p.GetValue(currentException,null);
								}
								catch(TargetInvocationException) {}

								if (prop == null)
								{
									#if !POCKETPC
									strInfo.AppendFormat("{0}{1}: NULL", newLine, p.Name);
									#else
									strInfo.AppendFormat(null, "{0}{1}: NULL", newLine, p.Name);
									#endif
								}
								else
								{
									#if !POCKETPC
									strInfo.AppendFormat("{0}{1}: {2}", newLine, p.Name, p.GetValue(currentException,null));
									#else
									strInfo.AppendFormat(null, "{0}{1}: {2}", newLine, p.Name, p.GetValue(currentException,null));
									#endif
								}
							}
						}
						#endregion

						#region Record the Exception StackTrace

						#if !POCKETPC
						// Record the StackTrace with separate label.
						if (showStack && currentException.StackTrace != null)
						{
							strInfo.AppendFormat("{0}{0}StackTrace Information{0}{1}", newLine, TEXT_SEPARATOR);
							strInfo.AppendFormat("{0}{1}", newLine, currentException.StackTrace);
						}
						#endif
						#endregion

						#if !POCKETPC
						strInfo.AppendFormat("{0}{0}", newLine);
						#else
						strInfo.AppendFormat(null, "{0}{0}", newLine);
						#endif

						// Reset the temp exception object and iterate the counter.
						currentException = currentException.InnerException;
						intExceptionCount++;
					} while (currentException != null);
					#endregion
				}

				#if !POCKETPC
				strInfo.AppendFormat("{1}Assembly version: {0}{1}RuntimeVersion: {2}{1}Compilation: {3}{1}Assembly file version: {4}", 
					assembly.GetName().Version.ToString(),
					newLine,
					assembly.ImageRuntimeVersion,
					ReflexHelper.GetAssemblyConfiguration(assembly),
					ReflexHelper.GetAssemblyFileVersion(assembly));
				#else
				strInfo.AppendFormat(null, "{1}Assembly version: {0}", 
					assembly.GetName().Version.ToString(), newLine);
				#endif
			}
			catch (Exception ex)
			{
				#if !POCKETPC
				strInfo.AppendFormat("{0}{0}Exception in PublishException:{4}{0}{1}{0}Original message:{0}{2}{0}Original Exception:{0}{3}", newLine, ex.Message, message, exception.Message, TEXT_SEPARATOR);
				#else
				strInfo.AppendFormat(null, "{0}{0}Exception in PublishException:{4}{0}{1}{0}Original message:{0}{2}{0}Original Exception:{0}{3}", newLine, ex.Message, message, exception.Message, TEXT_SEPARATOR);
				#endif
			}
			return strInfo.ToString();
		}

		private static void PublishInternal(string message, Exception exception, LogEntryType exceptionTpe)
		{
			#if !POCKETPC
			//Using the StackTrace object may be tricky on release builds because of inlining optimizations.
			#if !POCKETPC
			StackTrace stackTrace = new StackTrace();
			MemberInfo prevMethodInfo = (MemberInfo)stackTrace.GetFrame(2).GetMethod();
			Type callertype = prevMethodInfo.ReflectedType;
			ILog Log = LogHelper.GetLogger(callertype.Assembly, callertype);
			#else
			Type callertype = typeof(LogHelper);
			ILog Log = LogHelper.GetLogger(Assembly.GetExecutingAssembly(), callertype);
			#endif

			switch(exceptionTpe)
			{
				case LogEntryType.Audit:
					if(Log.Logger.IsEnabledFor(Level.NOTICE))
						Log.Logger.Log(callertype.FullName, Level.NOTICE, 
							InternalFormattedMessage(message, exception, callertype.Assembly), null);
					else
						//fallback log
						#if !POCKETPC
						EventLogHelper.WriteLogEntry(InternalFormattedMessage(message, exception, callertype.Assembly), EventLogEntryType.SuccessAudit);
						#else
						System.Diagnostics.Debug.WriteLine( InternalFormattedMessage(message, exception, callertype.Assembly), "SuccessAudit");
						#endif
						
					break;					
				case LogEntryType.Error:
					if(Log.IsErrorEnabled)
						Log.Error(InternalFormattedMessage(message, exception, callertype.Assembly));
					else
						//fallback log
						#if !POCKETPC
						EventLogHelper.WriteLogEntry(InternalFormattedMessage(message, exception, callertype.Assembly), EventLogEntryType.Error);
						#else
						System.Diagnostics.Debug.WriteLine( InternalFormattedMessage(message, exception, callertype.Assembly), "Error");
						#endif
					break;
				case LogEntryType.Fatal:
					if(Log.IsFatalEnabled)
						Log.Fatal(InternalFormattedMessage(message, exception, callertype.Assembly));
					else
						//fallback log
						#if !POCKETPC
						EventLogHelper.WriteLogEntry(InternalFormattedMessage(message, exception, callertype.Assembly), EventLogEntryType.Error);
						#else
						System.Diagnostics.Debug.WriteLine( InternalFormattedMessage(message, exception, callertype.Assembly), "Error");
						#endif
					break;
				case LogEntryType.Warning:
					if(Log.IsWarnEnabled) Log.Warn(InternalFormattedMessage(message, exception, callertype.Assembly));
					break;
				case LogEntryType.Debug:
					if(Log.IsDebugEnabled) Log.Debug(InternalFormattedMessage(message, exception, callertype.Assembly));
					break;
				default:
					if(Log.IsInfoEnabled) Log.Info(InternalFormattedMessage(message, exception, callertype.Assembly));
					break;
			}
			#endif
		}

		#endregion

		#region Public members

		#region Publish
		/// <summary>
		/// Write Exception Info to the ILog interface.
		/// </summary>
		/// <remarks>
		/// For Debugging or Information uses, its faster to use ILog 
		/// interface directly, instead of this method. 
		/// </remarks>
		/// <param name="message">Additional exception info.</param>
		public static void Publish(string message)
		{
			PublishInternal(message, null, LogEntryType.Information);
		}

		/// <summary>
		/// Write Exception Info to the ILog interface.
		/// </summary>
		/// <param name="exception">Exception object.</param>
		public static void Publish(Exception exception)
		{
			PublishInternal(null, exception, LogEntryType.Error);
		}

		/// <summary>
		/// Write Exception Info to the ILog interface.
		/// </summary>
		/// <param name="exception">Exception object.</param>
		/// <param name="exceptionTpe">See <see cref="LogEntryType"/>.</param>
		public static void Publish(Exception exception, LogEntryType exceptionTpe)
		{
			PublishInternal(null, exception, exceptionTpe);
		}

		/// <summary>
		/// Write Exception Info to the ILog interface.
		/// </summary>
		/// <remarks>
		/// For Debugging or Information uses, its faster to use ILog 
		/// interface directly, instead of this method. 
		/// </remarks>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exceptionTpe">See <see cref="LogEntryType"/>.</param>
		public static void Publish(string message, LogEntryType exceptionTpe)
		{
			PublishInternal(message, null, exceptionTpe);
		}

		/// <summary>
		/// Write Exception Info to the ILog interface.
		/// </summary>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exception">Exception object.</param>
		public static void Publish(string message, Exception exception)
		{
			PublishInternal(message, exception, LogEntryType.Error);
		}

		/// <summary>
		/// Write Exception Info to the ILog interface.
		/// </summary>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exception">Exception object.</param>
		/// <param name="exceptionTpe">See <see cref="LogEntryType"/>.</param>
		public static void Publish(string message, Exception exception, LogEntryType exceptionTpe)
		{ 
			PublishInternal(message, exception, exceptionTpe);
		}
		
		#endregion

		#region Logger

		#if !POCKETPC

		/// <summary>
		/// Returns a ILog interface for logging services.
		/// The ILog interface is use by application to log messages into the log4net framework. 
		/// </summary>
		/// <param name="name">The name of the logger to retrieve.</param>
		/// <returns>The logger with the name specified.</returns>
		public static ILog GetLogger(string name)
		{
			return log4net.LogManager.GetLogger(name);
		}

		/// <summary>
		/// Returns a ILog interface for logging services.
		/// The ILog interface is use by application to log messages into the log4net framework. 
		/// </summary>
		/// <param name="domainAssembly">The assembly that determines the repository domain of the logger.</param>
		/// <param name="name">The name of the logger to retrieve.</param>
		/// <returns>The logger with the name specified.</returns>
		public static ILog GetLogger(Assembly domainAssembly, string name)
		{
			return log4net.LogManager.GetLogger(domainAssembly, name);
		}

		/// <summary>
		/// Returns a ILog interface for logging services.
		/// The ILog interface is use by application to log messages into the log4net framework. 
		/// </summary>
		/// <param name="type">The type that will be used as the name of the logger to retrieve.</param>
		/// <returns>The logger with the name specified.</returns>
		public static ILog GetLogger(Type type)
		{
			return log4net.LogManager.GetLogger(type);
		}

		/// <summary>
		/// Returns a ILog interface for logging services.
		/// The ILog interface is use by application to log messages into the log4net framework. 
		/// </summary>
		/// <param name="domainAssembly">The assembly that determines the repository domain of the logger.</param>
		/// <param name="type">The type that will be used as the name of the logger to retrieve.</param>
		/// <returns>The logger with the name specified.</returns>
		public static ILog GetLogger(Assembly domainAssembly, Type type)
		{
			return log4net.LogManager.GetLogger(domainAssembly, type);
		}
		#endif

		#endregion

		#region FormattedMessage
		/// <summary>
		/// Gets the Exception Info to be writen to the Log.
		/// </summary>
		/// <param name="exception">Exception object.</param>
		public static string FormattedMessage(Exception exception)
		{
			return InternalFormattedMessage(null, exception, Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Gets the Exception Info to be writen to the Log.
		/// </summary>
		/// <param name="exception">Exception object.</param>
		/// <param name="showStack">True, show all the stack trace inf.</param>
		public static string FormattedMessage(Exception exception, bool showStack)
		{
			return InternalFormattedMessage(null, exception, Assembly.GetCallingAssembly(), showStack);
		}

		/// <summary>
		/// Gets the Exception Info to be writen to the Log.
		/// </summary>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exception">Exception object.</param>
		public static string FormattedMessage(string message, Exception exception)
		{
			return InternalFormattedMessage(message, exception, Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Gets the Exception Info to be writen to the Log.
		/// </summary>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exception">Exception object.</param>
		/// <param name="showStack">True, show all the stack trace inf.</param>
		public static string FormattedMessage(string message, Exception exception, bool showStack)
		{
			return InternalFormattedMessage(message, exception, Assembly.GetCallingAssembly(), showStack);
		}

		#endregion

		#endregion
	}
}