#region Usings
using System;
#endregion

#region License
/*
 * SharpHsqlError.cs
 *
 * Copyright (c) 2004, Andres G Vettori
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
 * C# SharpHsql ADO.NET Provider by Andrés G Vettori.
 * http://workspaces.gotdotnet.com/sharphsql
 */
#endregion

namespace System.Data.Hsql
{
	/// <summary>
	/// Error object used in <see cref="SharpHsqlException"/>.
	/// <seealso cref="SharpHsqlErrorCollection"/>
	/// <seealso cref="SharpHsqlException"/>
	/// </summary>
	public struct SharpHsqlError
	{
		#region Constructor

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="number"></param>
		/// <param name="procedure"></param>
		/// <param name="source"></param>
		public SharpHsqlError( string message, int number, string procedure, string source )
		{
			this.Message = message; 
			this.Number = number; 
			this.Procedure = procedure;
			this.Source = source;
		}

		#endregion

		#region Public Fields

		/// <summary>
		/// Textual description for this error.
		/// </summary>
		public string Message;
		/// <summary>
		/// Error code for this error.
		/// </summary>
		public int Number;
		/// <summary>
		/// Procedure where this error was waised.
		/// </summary>
		public string Procedure;
		/// <summary>
		/// Source module of this error.
		/// </summary>
		public string Source;

		#endregion
	}
}
