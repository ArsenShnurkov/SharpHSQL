#region Usings
using System;
using System.Data;
using System.Data.Common;
using SharpHsql;
#endregion

#region License
/*
 * SharpHsqlTransaction.cs
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
	/// Transaction class for Hsql ADO.NET data provider.
	/// <seealso cref="SharpHsqlConnection"/>
	/// <seealso cref="SharpHsqlReader"/>
	/// <seealso cref="SharpHsqlParameter"/>
	/// <seealso cref="SharpHsqlCommand"/>
	/// <seealso cref="SharpHsqlDataAdapter"/>
	/// </summary>
	public sealed class SharpHsqlTransaction : IDbTransaction
	{
		#region Constructors

		/// <summary>
		/// Transaction class constructor.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="isoLevel"></param>
		internal SharpHsqlTransaction(SharpHsqlConnection connection, IsolationLevel isoLevel)
		{
			this._isolationLevel = IsolationLevel.ReadCommitted;
			this._sqlConnection = connection;
			this._sqlConnection.LocalTransaction = this;
			this._isolationLevel = isoLevel;
		}

		#endregion

		#region IDbTransaction Members

		/// <summary>
		/// Aborts the current active transaction.
		/// </summary>
		public void Rollback()
		{
			if (this._sqlConnection == null)
			{
				throw new InvalidOperationException("Connection is not longer valid.");
			}
			//IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
			this._sqlConnection.Channel.Execute("ROLLBACK WORK");
			this._sqlConnection.Channel.Execute("SET AUTOCOMMIT TRUE");
			this._sqlConnection.LocalTransaction = null;
			this._sqlConnection = null;
		}

		/// <summary>
		/// Closes the current transaction applying all changes to the database.
		/// </summary>
		public void Commit()
		{
			if (this._sqlConnection.Channel == null)
			{
				throw new InvalidOperationException("Connection is not longer valid.");
			}
			this._sqlConnection.Channel.Execute("COMMIT WORK");
			this._sqlConnection.Channel.Execute("SET AUTOCOMMIT TRUE");
			this._sqlConnection.LocalTransaction = null;
			this._sqlConnection = null;
		}

		/// <summary>
		/// Gets the connection instance used in the transaction.
		/// </summary>
		public IDbConnection Connection
		{
			get
			{
				return _sqlConnection;
			}
		}

		/// <summary>
		/// Gets the transaction isolation level.
		/// </summary>
		public System.Data.IsolationLevel IsolationLevel
		{
			get
			{
				return _isolationLevel;
			}
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Dispose this transaction doing a rollback if needed.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing && (this._sqlConnection != null))
			{
				this.Rollback();
			}
		}

		#endregion

		#region Private & Internal Vars

		private IsolationLevel _isolationLevel = IsolationLevel.ReadCommitted;
		internal SharpHsqlConnection _sqlConnection = null;

		#endregion

	}
}
