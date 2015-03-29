#region Usings
using System;
using System.Collections;
using System.IO;
#endregion

#region License
/*
 * Trace.cs
 *
 * Copyright (c) 2001, The HSQL Development Group
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 *
 * Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 *
 * Neither the name of the HSQL Development Group nor the names of its
 * contributors may be used to endorse or promote products derived from this
 * software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE FOR
 * ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * This package is based on HypersonicSQL, originally developed by Thomas Mueller.
 *
 * C# port by Mark Tutt
 * C# SharpHsql by Andrés G Vettori.
 * http://workspaces.gotdotnet.com/sharphsql
 */
#endregion

namespace SharpHsql
{
	/// <summary>
	/// Internal Trace and Exceptions helper class.
	/// </summary>
	sealed class Trace
	{
		#region Consructor

		/// <summary>
		/// Default internal constructor.
		/// </summary>
		internal Trace() 
		{
		}

		#endregion

		#region Public Vars

		#if TRACE
			public static bool TraceEnabled = true;
		#else
			public static bool TraceEnabled = false;
		#endif

		public static bool		StopEnabled		= false;
		public static bool		AssertEnabled	= false;
		private static Trace	tTracer		= new Trace();
		private static int		iLine;
		private static string   sTrace;
		private static int		iStop = 0;
		
		#endregion 

		#region Public Constants

		public const int		DATABASE_ALREADY_IN_USE = 0,
			CONNECTION_IS_CLOSED = 1,
			CONNECTION_IS_BROKEN = 2,
			DATABASE_IS_SHUTDOWN = 3,
			COLUMN_COUNT_DOES_NOT_MATCH = 4,
			DIVISION_BY_ZERO = 5, InvalidEscape = 6,
			INTEGRITY_CONSTRAINT_VIOLATION = 7,
			VIOLATION_OF_UNIQUE_INDEX = 8,
			TRY_TO_INSERT_NULL = 9,
			UnexpectedToken = 10,
			UNEXPECTED_END_OF_COMMAND = 11,
			UNKNOWN_FUNCTION = 12, NEED_AGGREGATE = 13,
			SUM_OF_NON_NUMERIC = 14,
			WRONG_DATA_TYPE = 15,
			SINGLE_VALUE_EXPECTED = 16,
			SERIALIZATION_FAILURE = 17,
			TRANSFER_CORRUPTED = 18,
			FUNCTION_NOT_SUPPORTED = 19,
			TABLE_ALREADY_EXISTS = 20,
			TABLE_NOT_FOUND = 21,
			INDEX_ALREADY_EXISTS = 22,
			SECOND_PRIMARY_KEY = 23,
			DROP_PRIMARY_KEY = 24, INDEX_NOT_FOUND = 25,
			COLUMN_ALREADY_EXISTS = 26,
			COLUMN_NOT_FOUND = 27, FILE_IO_ERROR = 28,
			WRONG_DATABASE_FILE_VERSION = 29,
			DATABASE_IS_READONLY = 30,
			ACCESS_IS_DENIED = 31,
			INPUTSTREAM_ERROR = 32,
			NO_DATA_IS_AVAILABLE = 33,
			USER_ALREADY_EXISTS = 34,
			USER_NOT_FOUND = 35, ASSERT_FAILED = 36,
			EXTERNAL_STOP = 37, GENERAL_ERROR = 38,
			WRONG_OUT_PARAMETER = 39,
			ERROR_IN_FUNCTION = 40,
			TRIGGER_NOT_FOUND = 41,
			VARIABLE_NOT_DECLARED = 42;
		
		private static readonly string[]       sDescription = 
		{
			"08001 The database is already in use by another process",
			"08003 Connection is closed", "08003 Connection is broken",
			"08003 The database is shutdown",
			"21S01 Column count does not match", "22012 Division by zero",
			"22019 Invalid escape character",
			"23000 Integrity constraint violation",
			"23000 Violation of unique index",
			"23000 Try to insert null into a non-nullable column",
			"37000 Unexpected token", "37000 Unexpected end of command",
			"37000 Unknown function",
			"37000 Need aggregate function or group by",
			"37000 Sum on non-numeric data not allowed", "37000 Wrong data type",
			"37000 Single value expected", "40001 Serialization failure",
			"40001 Transfer corrupted", "IM001 This function is not supported",
			"S0001 Table already exists", "S0002 Table not found",
			"S0011 Index already exists",
			"S0011 Attempt to define a second primary key",
			"S0011 Attempt to drop the primary key", "S0012 Index not found",
			"S0021 Column already exists", "S0022 Column not found",
			"S1000 File input/output error", "S1000 Wrong database file version",
			"S1000 The database is in read only mode", "S1000 Access is denied",
			"S1000 InputStream error", "S1000 No data is available",
			"S1000 User already exists", "S1000 User not found",
			"S1000 Assert failed", "S1000 External stop request",
			"S1000 General error", "S1009 Wrong OUT parameter",
			"S1010 Error in function", "S0002 Trigger not found",
			"S1000 Variable name not found."
		};

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets an exception object from an error code and description.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="add"></param>
		/// <returns></returns>
		public static Exception GetError(int code, string add) 
		{
			string s = GetMessage(code);

			if (add != null) 
			{
				s += ": " + add;
			}

			return GetError(s);
		}

		/// <summary>
		/// Gets an error message from a code.
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		public static string GetMessage(int code) 
		{
			return sDescription[code];
		}

		/// <summary>
		/// Gets an error message from an exception.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public static string GetMessage(Exception e) 
		{
			return e.Message;
		}

		/// <summary>
		/// Gets an exception from a string message.
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static Exception GetError(string msg) 
		{
			return new Exception(msg);
		}

		/// <summary>
		/// Gets an exception from a code.
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		public static Exception Error(int code) 
		{
			return GetError(code, null);
		}

		/// <summary>
		/// Gets an exception from a code and string message.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		public static Exception Error(int code, string s) 
		{
			return GetError(code, s);
		}

		/// <summary>
		/// Gets an exception from a code and numeric value.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public static Exception Error(int code, int i) 
		{
			return GetError(code, i.ToString() );
		}
		
		/// <summary>
		/// Asserts a condition.
		/// </summary>
		/// <param name="condition"></param>
		public static void Assert(bool condition) 
		{
			Assert(condition, null);
		}

		/// <summary>
		/// Asserts a condition with an error message.
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="error"></param>
		public static void Assert(bool condition, string error) 
		{
			if (!condition) 
			{
				PrintStack();

				throw GetError(ASSERT_FAILED, error);
			}
		}

		/// <summary>
		/// Checks a condition with an error code.
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="code"></param>
		public static void Check(bool condition, int code) 
		{
			Check(condition, code, null);
		}

		/// <summary>
		/// Checks a condition with an error code and error message.
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="code"></param>
		/// <param name="s"></param>
		public static void Check(bool condition, int code,	string s) 
		{
			if (!condition) 
			{
				throw GetError(code, s);
			}
		}

		/// <summary>
		/// Prints a text line.
		/// </summary>
		/// <param name="c"></param>
		public void PrintLine(char[] c) 
		{
			if (iLine++ == 2) 
			{
				string s = new string(c);
				int    i = s.IndexOf('.');

				if (i != -1) 
				{
					s = s.Substring(i + 1);
				}

				i = s.IndexOf('(');

				if (i != -1) 
				{
					s = s.Substring(0, i);
				}

				sTrace = s;
			}
		}

		/// <summary>
		/// Write a long value to the trace log.
		/// </summary>
		/// <param name="value"></param>
		public static void Write(long value) 
		{
			TraceCaller( value.ToString() );
		}

		/// <summary>
		/// Write a int value to the trace log.
		/// </summary>
		/// <param name="value"></param>
		public static void Write(int value) 
		{
			TraceCaller( value.ToString() );
		}

		/// <summary>
		/// Write a empty string to the trace log.
		/// </summary>
		public static void Write() 
		{
			TraceCaller("");
		}

		/// <summary>
		/// Write a string to the trace log.
		/// </summary>
		/// <param name="value"></param>
		public static void Write(string value) 
		{
			TraceCaller(value);
		}

		/// <summary>
		/// Stops the tracing.
		/// </summary>
		public static void Stop() 
		{
			Stop(null);
		}

		/// <summary>
		/// Stops the tracing with an error message.
		/// </summary>
		/// <param name="value"></param>
		public static void Stop(string value) 
		{
			if (iStop++ % 10000 != 0) 
			{
				return;
			}

			if (new FileInfo("trace.stop").Exists) 
			{
				PrintStack();

				throw GetError(EXTERNAL_STOP, value);
			}
		}

		/// <summary>
		/// Prints the stack trace.
		/// </summary>
		static private void PrintStack() 
		{
			Exception e = new Exception();

			#if !POCKETPC
			Console.WriteLine(e.StackTrace);
			#endif
		}

		/// <summary>
		/// Prints the stack trace.
		/// </summary>
		/// <param name="value"></param>
		static private void TraceCaller(string value) 
		{
			Exception e = new Exception();

			iLine = 0;

			#if !POCKETPC
				Console.WriteLine(e.StackTrace);
			#endif

			value = string.Concat( sTrace, "\t", value );

			// trace to System.Console is handy if only trace messages of hsql are required
			#if !DEBUG
				System.Console.WriteLine(value);
			#else
				System.Diagnostics.Debug.WriteLine(value);
			#endif
		}

		#endregion
	}
}
