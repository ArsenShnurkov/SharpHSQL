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
#endregion

namespace SharpHsql
{
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

	class LogHelperBase
	{
		protected static void PublishInternal(string message, Exception exception, LogEntryType exceptionTpe)
		{
			Trace.WriteLine (message);
		}
	}

	internal class LogHelper : LogHelperBase
	{
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
	}
}