#region Usings
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
#endregion

#region License
/*
 * SharpHsqlEvents.cs
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
	#region Public Delegates

	/// <summary>
	/// InfoMessage event for Hsql ADO.NET data provider.
	/// </summary>
	public delegate void SharpHsqlInfoMessageEventHandler(object sender, SharpHsqlInfoMessageEventArgs e);
	/// <summary>
	/// Row Updated event for Hsql ADO.NET data provider.
	/// </summary>
	public delegate void SharpHsqlRowUpdatedEventHandler(object sender, SharpHsqlRowUpdatedEventArgs e);
	/// <summary>
	/// Row Updating event for Hsql ADO.NET data provider.
	/// </summary>
	public delegate void SharpHsqlRowUpdatingEventHandler(object sender, SharpHsqlRowUpdatingEventArgs e);
	
	#endregion

	#region SharpHsqlInfoMessageEventArgs

	/// <summary>
	/// InfoMessage argument class for Hsql ADO.NET data provider.
	/// </summary>
	public sealed class SharpHsqlInfoMessageEventArgs : EventArgs
	{
		/// <summary>
		/// Internal constructor.
		/// </summary>
		/// <param name="exception"></param>
		internal SharpHsqlInfoMessageEventArgs(SharpHsqlException exception)
		{
			this.exception = exception;
		}

		/// <summary>
		/// True if exists eny errors that should be serialized.
		/// </summary>
		/// <returns></returns>
		private bool ShouldSerializeErrors()
		{
			if (this.exception != null)
			{
				return (0 < this.exception.Errors.Count);
			}
			return false;
		}

		/// <summary>
		/// Returns a string representation of the object.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.Message;
		}

		/// <summary>
		/// Error collection
		/// </summary>
		public SharpHsqlErrorCollection Errors
		{
			get
			{
				return this.exception.Errors;
			}
		}

		/// <summary>
		/// Message
		/// </summary>
		public string Message
		{
			get
			{
				return this.exception.Message;
			}
		}

		#if !POCKETPC
		/// <summary>
		/// Exception Source.
		/// </summary>
		/// <remarks>Not supported on Compact Framwork 1.0.</remarks>
		public string Source
		{
			get
			{
				return this.exception.Source;
			}
		}
		#endif

		// Fields
		private SharpHsqlException exception;
	}

	#endregion

	#region SharpHsqlRowUpdatedEventArgs

	/// <summary>
	/// RowUpdated argument class for Hsql ADO.NET data provider.
	/// </summary>
	public sealed class SharpHsqlRowUpdatedEventArgs : RowUpdatedEventArgs
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="row"></param>
		/// <param name="command"></param>
		/// <param name="statementType"></param>
		/// <param name="tableMapping"></param>
		public SharpHsqlRowUpdatedEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping) : base(row, command, statementType, tableMapping)
		{
		}

		/// <summary>
		/// Command beign executed.
		/// </summary>
		public new SharpHsqlCommand Command
		{
			get
			{
				return (SharpHsqlCommand) base.Command;
			}
		}
	}

	#endregion

	#region SharpHsqlRowUpdatingEventArgs

	/// <summary>
	/// RowUpdating argument class for Hsql ADO.NET data provider.
	/// </summary>
	public sealed class SharpHsqlRowUpdatingEventArgs : RowUpdatingEventArgs
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="command"></param>
		/// <param name="statementType"></param>
		/// <param name="tableMapping"></param>
		public SharpHsqlRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping) : base(row, command, statementType, tableMapping)
		{
		}

		/// <summary>
		/// Command beign executed.
		/// </summary>
		public new SharpHsqlCommand Command
		{
			get
			{
				return (SharpHsqlCommand) base.Command;
			}
		}	
	}

	#endregion
}

