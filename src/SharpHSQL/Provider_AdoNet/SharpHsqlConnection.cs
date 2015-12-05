#region Usings
using System;
using System.ComponentModel;
using System.Data;
using System.Xml;
using SharpHsql;
#endregion

#region License
/*
 * SharpHsqlConnection.cs
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
	/// Class representing a database connection. 
	/// <seealso cref="SharpHsqlCommand"/>
	/// <seealso cref="SharpHsqlReader"/>
	/// <seealso cref="SharpHsqlParameter"/>
	/// <seealso cref="SharpHsqlTransaction"/>
	/// <seealso cref="SharpHsqlDataAdapter"/>
	/// </summary>
	public sealed class SharpHsqlConnection : Component, IDbConnection, ICloneable
	{
		#region Constructors

		/// <summary>
		/// Default Constructor.
		/// </summary>
		public SharpHsqlConnection()
		{
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Constructor using a connection string.
		/// </summary>
		/// <param name="connectionString"></param>
		public SharpHsqlConnection(string connectionString)
		{
			GC.SuppressFinalize(this);
			ConnectionString = connectionString;
		}

		/// <summary>
		/// Private constructor used internally.
		/// </summary>
		/// <param name="connection"></param>
		private SharpHsqlConnection(SharpHsqlConnection connection)
		{
			GC.SuppressFinalize(this);
			this._hidePasswordPwd = connection._hidePasswordPwd;
			this._constr = connection._constr;
		}

		#endregion

		#region Public Properties & Methods

		/// <summary>
		/// Get or set the connection string.
		/// </summary>
		public string ConnectionString
		{
			get { return _connString;  }
			set { 
				_connString = value; 

				_constr = new SharpHsqlConnectionString( _connString );
				_database = _constr.Database;
				_user = _constr.UserName;
				_pwd = _constr.UserPassword;
			}
		}
    
		/// <summary>
		/// Get the current connection timeout.
		/// </summary>
		public int ConnectionTimeout
		{
			get 
			{
				throw new InvalidOperationException("SharpHSql Provider does not support this function");
			}
		}

		/// <summary>
		/// Get the current database name.
		/// </summary>
		public string Database 
		{
			get 
			{
				return _database;
			}
		}

		/// <summary>
		/// Get the current connection state.
		/// </summary>
		public ConnectionState State 
		{
			get { return _connState; }
		}

		/// <summary>
		/// Starts a new transaction using the default isolation level (ReadCommitted).
		/// <seealso cref="IsolationLevel"/>
		/// </summary>
		/// <returns>The new <see cref="SharpHsqlTransaction"/> object.</returns>
		public SharpHsqlTransaction BeginTransaction()
		{
			return BeginTransaction(IsolationLevel.ReadCommitted);
		}

		/// <summary>
		/// Starts a new transaction using the default isolation level (ReadCommitted).
		/// <seealso cref="IsolationLevel"/>
		/// </summary>
		/// <returns>The new <see cref="SharpHsqlTransaction"/> object.</returns>
		IDbTransaction IDbConnection.BeginTransaction()
		{
			return this.BeginTransaction(IsolationLevel.ReadCommitted);
		}

		/// <summary>
		/// Starts a new transaction.
		/// <seealso cref="IsolationLevel"/>
		/// </summary>
		/// <param name="level"></param>
		/// <returns>The new <see cref="SharpHsqlTransaction"/> object.</returns>
		IDbTransaction IDbConnection.BeginTransaction(IsolationLevel level)
		{
			return this.BeginTransaction(level);
		}

		/// <summary>
		/// Starts a new transaction.
		/// <seealso cref="IsolationLevel"/>
		/// </summary>
		/// <param name="level"></param>
		/// <returns>The new <see cref="SharpHsqlTransaction"/> object.</returns>
		public SharpHsqlTransaction BeginTransaction(IsolationLevel level)
		{
			if (this._connState == ConnectionState.Closed)
			{
				throw new InvalidOperationException("Connection is closed.");
			}
			this.CloseDeadReader();
			this.RollbackDeadTransaction();
			if (this.LocalTransaction != null)
			{
				throw new InvalidOperationException("Parallel Transactions Not Supported");
			}
			this.Execute("SET AUTOCOMMIT FALSE");

			return new SharpHsqlTransaction( this, level );
		}

		/// <summary>
		/// Changes the current database for this connection.
		/// </summary>
		/// <remarks>Not currently supported.</remarks>
		/// <param name="databaseName"></param>
		public void ChangeDatabase(string databaseName)
		{
			throw new InvalidOperationException("SharpHSql Provider does not support this function");
		}

		/// <summary>
		/// Closes the current connection.
		/// </summary>
		public void Close()
		{
			switch (this._connState)
			{
				case ConnectionState.Closed:
					return;
				case ConnectionState.Open:
					if( _channel != null )
					{
						CloseReader();

						if (this._connState != ConnectionState.Open)
							return;

						if (this.LocalTransaction != null)
						{
							this.LocalTransaction.Rollback();
						}
						else
						{
							this.RollbackDeadTransaction();
						}
						_channel.Disconnect();
						_channel = null;
						_connState = ConnectionState.Closed;
						this.FireObjectState(ConnectionState.Open, ConnectionState.Closed);
					}
					break;
				default:
					return;
			}
		}

		/// <summary>
		/// Creates a new SharpHsqlCommand object.
		/// </summary>
		/// <returns>A new SharpHsqlCommand object.</returns>
		public SharpHsqlCommand CreateCommand()
		{
			return new SharpHsqlCommand(String.Empty, this);
		}

		/// <summary>
		/// Creates a new SharpHsqlCommand object.
		/// </summary>
		/// <returns>A new SharpHsqlCommand object.</returns>
		IDbCommand IDbConnection.CreateCommand()
		{
			return CreateCommand();
		}

		/// <summary>
		/// Open the current connection.
		/// </summary>
		public void Open()
		{
			switch (this._connState)
			{
				case ConnectionState.Closed:
					Database db = DatabaseController.GetDatabase( _database );
					_channel = db.Connect(_user,_pwd);
					_connState = ConnectionState.Open;
					this.FireObjectState(ConnectionState.Closed, ConnectionState.Open);
					break;
				case ConnectionState.Open:
					throw new InvalidOperationException("Connection Already Open");
			}
		}

		/// <summary>
		/// InfoMessage event.
		/// </summary>
		public event SharpHsqlInfoMessageEventHandler InfoMessage;
		/// <summary>
		/// StateChange event.
		/// </summary>
		public event StateChangeEventHandler StateChange;

		/// <summary>
		/// Get a clone of the current instance.
		/// </summary>
		/// <returns></returns>
		public SharpHsqlConnection Clone()
		{
			return new SharpHsqlConnection(this);
		}

		/// <summary>
		/// Get a clone of the current instance.
		/// </summary>
		/// <returns></returns>
		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion

		#region Dispose Methods

		/// <summary>
		/// Clean up used resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				switch (this._connState)
				{
					case ConnectionState.Open:
					{
						this.Close();
						break;
					}
				}
				this._constr = null;
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Rollbacks any dead transaction before doing something else.
		/// </summary>
		internal void RollbackDeadTransaction()
		{
			if ((this._localTransaction != null) && !this._localTransaction.IsAlive)
			{
				this.InternalRollback();
			}
		}

		/// <summary>
		/// Executes a rollback command on the database.
		/// </summary>
		internal void InternalRollback()
		{
			this.Execute("ROLLBACK TRANSACTION");
			this.LocalTransaction = null;
		}

		/// <summary>
		/// Closes any active reader before doing something else.
		/// </summary>
		internal void CloseDeadReader()
		{
			if ((this._reader == null) || this._reader.IsAlive)
			{
				return;
			}
			this._reader = null;
		}

		/// <summary>
		/// Connection state
		/// </summary>
		internal ConnectionState	_connState = ConnectionState.Closed;
		/// <summary>
		/// Connection string
		/// </summary>
		internal string				_connString = String.Empty;
		/// <summary>
		/// Database name
		/// </summary>
		internal string				_database =  String.Empty;

		/// <summary>
		/// Active reader using this connection.
		/// </summary>
		internal SharpHsqlReader Reader
		{
			get
			{
				if (this._reader != null)
				{
					SharpHsqlReader reader = (SharpHsqlReader) this._reader.Target;
					if ((reader != null) && this._reader.IsAlive)
					{
						return reader;
					}
				}
				return null;
			}
			set
			{
				this._reader = null;
				if (value != null)
				{
					this._reader = new WeakReference(value);
				}
			}
		}
 
		/// <summary>
		/// Database instance associated with this connection.
		/// </summary>
		internal Database InternalDatabase
		{
			get
			{
				if (_channel != null)
				{
					return _channel.Database;
				}
				else
					return null;
			}
		}

		/// <summary>
		/// Internal SharpHsql channel associated with this connection.
		/// </summary>
		internal Channel Channel
		{
			get
			{
				return _channel;
			}
		}

		/// <summary>
		/// Executes the sql query and return the results.
		/// </summary>
		/// <param name="sqlBatch"></param>
		/// <returns></returns>
		internal Result Execute(string sqlBatch)
		{
			if (this._connState == ConnectionState.Closed)
			{
				throw new InvalidOperationException("Connection is closed");
			}
			this.CloseDeadReader();
			this.RollbackDeadTransaction();

			Result _result = this._channel.Execute(sqlBatch);
			CheckForError( _result );
			return _result;
		}

		#endregion

		#region Private Methods

		private void CheckForError( Result _result )
		{
			if( _result != null && _result.Error != null && _result.Error != string.Empty )
			{
				throw new SharpHsqlException( _result.Error );
			}
		}

		private void CloseReader()
		{
			if (this._reader == null)
			{
				return;
			}
			SharpHsqlReader reader = (SharpHsqlReader) this._reader.Target;
			if ((reader != null) && this._reader.IsAlive)
			{
				if (!reader.IsClosed)
				{
					reader.Close();
				}
			}
			this._reader = null;
		}

		private void FireObjectState(ConnectionState original, ConnectionState current)
		{
			if( StateChange != null )
				StateChange(this, new StateChangeEventArgs(original, current));
		}

		private void FireInfoMessage( SharpHsqlException ex )
		{
			if( InfoMessage!= null )
				InfoMessage(this, new SharpHsqlInfoMessageEventArgs( ex ) );
		}

		#endregion

		#region Internal Vars

		/// <summary>
		/// Local transaction object used internally.
		/// </summary>
		internal SharpHsqlTransaction LocalTransaction = null;

		#endregion

		#region Private Vars

		private string				_user = String.Empty;
		private string				_pwd = String.Empty;
		private SharpHsqlConnectionString _constr;
		private bool _hidePasswordPwd;
		private WeakReference _localTransaction = null;
		private WeakReference _reader;
		private Channel _channel;

		#endregion
	}
}
