#region Usings
using System;
using System.Collections;
#endregion

#region License
/*
 * Channel.cs
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
	/// Class that represents a user connection with a database.
	/// </summary>
	/// <remarks>version 1.0.0.1</remarks>
	public sealed class Channel : IDisposable
	{
		#region Private Vars

		private Database	_database;
		private User		_user;
		private ArrayList   _transaction;
		private bool		_autoCommit;
		private bool		_nestedTransaction;
		private bool		_nestedOldAutoCommit;
		private int			_nestedOldTransIndex;
		private bool		_readOnly;
		private int			_maxRows;
		private int			_lastIdentity;
		private bool		_closed;
		private int			_id;
		private bool		_disposed;
		private Hashtable	_variables;

		#endregion

		#region Constructors

		/// <summary>
		/// Private default constructor.
		/// </summary>
		private Channel()
		{
			_variables = Hashtable.Synchronized( new Hashtable() );
			_transaction = new ArrayList();
		}

		/// <summary>
		/// Builds a channel based on an existing one.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="id"></param>
		public Channel(Channel channel, int id) : this()
		{
			_id = id;
			_database = channel._database;
			_user = channel._user;
			_autoCommit = true;
			_readOnly = channel._readOnly;
		}

		/// <summary>
		/// Builds a new channel.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="user"></param>
		/// <param name="autoCommit"></param>
		/// <param name="readOnly"></param>
		/// <param name="id"></param>
		public Channel(Database db, User user, bool autoCommit, bool readOnly, int id) : this()
		{
			_id = id;
			_database = db;
			_user = user;
			_autoCommit = autoCommit;
			_readOnly = readOnly;
		}

		#endregion

		#region Synchronization
		/// <summary>
		/// Synchronization object.
		/// </summary>
		public object SyncRoot = new object();
		#endregion

		#region Public Properties

		/// <summary>
		/// Gets the channel identifier.
		/// </summary>
		public int Id
		{
			get
			{
				Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

				return _id;
			}
		}

		/// <summary>
		/// Return True if the channel is closed.
		/// </summary>
		public bool IsClosed
		{
			get
			{
				return _closed;
			}
		}

		/// <summary>
		/// Gets the current user name.
		/// </summary>
		public string UserName
		{
			get
			{
				Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

				return _user.Name;
			}
		}

		/// <summary>
		/// Returns True if the channel is in read only mode.
		/// </summary>
		/// <returns></returns>
		public bool IsReadOnly
		{
			get
			{
				Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

				return _readOnly;
			}
		}

		/// <summary>
		/// Gets or sets the maximum count of rows to return.
		/// </summary>
		public int MaxRows
		{
			get
			{
				Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

				return _maxRows;
			}
			set
			{
				Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

				_maxRows = value;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Executes a SQL query on the current channel.
		/// </summary>
		/// <param name="statement"></param>
		/// <returns></returns>
		public Result Execute(string statement) 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			return this._database.Execute( statement, this);
		}

		/// <summary>
		/// Disconnects the current channel from database.
		/// </summary>
		public void Disconnect() 
		{
			if (_closed) 
			{
				return;
			}

			Rollback();

			_user = null;
			_database = null;
			_transaction = null;
			_closed = true;
		}

		/// <summary>
		/// Sets the auto commit mode for this channel.
		/// </summary>
		/// <param name="autoCommit"></param>
		public void SetAutoCommit(bool autoCommit) 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			Commit();

			_autoCommit = autoCommit;
		}

		/// <summary>
		/// Commits all pending transactions.
		/// </summary>
		public void Commit() 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			_transaction.Clear();
		}

		/// <summary>
		/// Rollbacks all pending transactions.
		/// </summary>
		public void Rollback() 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			int i = _transaction.Count - 1;

			while (i >= 0) 
			{
				Transaction t = (Transaction) _transaction[i];

				t.Rollback();

				i--;
			}

			_transaction.Clear();
		}

		/// <summary>
		/// Sets the channel in read-only mode.
		/// </summary>
		/// <param name="readOnly"></param>
		public void SetReadOnly(bool readOnly) 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			_readOnly = readOnly;
		}

		#endregion

		#region Internal Methods & Properties

		/// <summary>
		/// Adds a declare object to the current channel.
		/// </summary>
		/// <param name="declare"></param>
		internal void AddDeclare( Declare declare )
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			_variables[declare.Name] = declare;
		}

		/// <summary>
		/// Sets a declare object value.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		internal void SetDeclareValue( string name, object value )
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			Declare declare = _variables[name] as Declare;

			if( declare != null )
			{
				declare.Value = value;
				declare.Expression.SetArg(value);
			}
			else
			{
				throw Trace.Error(Trace.VARIABLE_NOT_DECLARED);
			}
		}

		/// <summary>
		/// Gets a declare object.
		/// </summary>
		/// <param name="name"></param>
		internal Declare GetDeclare( string name )
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			return _variables[name] as Declare;
		}

		/// <summary>
		/// Gets a declare object value.
		/// </summary>
		/// <param name="name"></param>
		internal object GetDeclareValue( string name )
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			Declare declare = _variables[name] as Declare;

			if( declare != null )
			{
				return declare.Value;
			}
			else
			{
				throw Trace.Error(Trace.VARIABLE_NOT_DECLARED);
			}
		}

		/// <summary>
		/// Gets or Sets the last identity value for the current channel.
		/// </summary>
		internal int LastIdentity
		{
			get
			{
				Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

				return _lastIdentity;
			}
			set
			{
				Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

				_lastIdentity = value;
			}
		}

		/// <summary>
		/// Gets the current channel database.
		/// </summary>
		internal Database Database
		{
			get
			{
				Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

				return _database;
			}
		}

		/// <summary>
		/// Sets the current user for the channel.
		/// </summary>
		/// <param name="user"></param>
		internal void SetUser(User user) 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			_user = user;
		}

		/// <summary>
		/// Verifies if the current user is database admin.
		/// </summary>
		internal void CheckAdmin()
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			_user.CheckAdmin();
		}

		/// <summary>
		/// Check a desired right over an specific database object.
		/// </summary>
		/// <param name="databaseObject"></param>
		/// <param name="right"></param>
		internal void Check(string databaseObject, AccessType right)
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			_user.Check(databaseObject, right);
		}

		/// <summary>
		/// Verifies if the database is in read only state.
		/// </summary>
		internal void CheckReadWrite() 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			Trace.Check(!_readOnly, Trace.DATABASE_IS_READONLY);
		}

		/// <summary>
		/// Sets the current user password.
		/// </summary>
		/// <param name="password"></param>
		internal void SetPassword(string password) 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			_user.Password = password;
		}

		/// <summary>
		/// Add a delete transaction to the current transaction list.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="row"></param>
		internal void AddTransactionDelete(Table table, object[] row) 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			if (!_autoCommit) 
			{
				Transaction t = new Transaction(true, table, row);

				_transaction.Add(t);
			}
		}

		/// <summary>
		///  Add an insert transaction to the current transaction list.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="row"></param>
		internal void AddTransactionInsert(Table table, object[] row) 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			if (!_autoCommit) 
			{
				Transaction t = new Transaction(false, table, row);

				_transaction.Add(t);
			}
		}

		/// <summary>
		/// Begins a new nested transaction.
		/// </summary>
		internal void BeginNestedTransaction() 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			Trace.Assert(!_nestedTransaction, "beginNestedTransaction");

			_nestedOldAutoCommit = _autoCommit;

			// now all transactions are logged
			_autoCommit = false;
			_nestedOldTransIndex = _transaction.Count;
			_nestedTransaction = true;
		}

		/// <summary>
		/// Finalizes a nested transaction.
		/// </summary>
		/// <param name="rollback"></param>
		internal void EndNestedTransaction(bool rollback) 
		{
			Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

			Trace.Assert(_nestedTransaction, "EndNestedTransaction");

			int i = _transaction.Count - 1;

			if (rollback) 
			{
				while (i >= _nestedOldTransIndex) 
				{
					Transaction t = (Transaction) _transaction[i];

					t.Rollback();

					i--;
				}
			}

			_nestedTransaction = false;
			_autoCommit = _nestedOldAutoCommit;

			if (_autoCommit == true) 
			{
				_transaction.RemoveRange(_nestedOldTransIndex,(_transaction.Count - _nestedOldTransIndex));
			}
		}

		/// <summary>
		/// Returns True if a nested transaction is active.
		/// </summary>
		/// <returns></returns>
		internal bool IsNestedTransaction
		{
			get
			{
				Trace.Check(!_closed, Trace.CONNECTION_IS_CLOSED);

				return _nestedTransaction;
			}
		}
	
		#endregion

		#region IDisposable Members

		/// <summary>
		/// Clean up any used resources.
		/// </summary>
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		/// <summary>
		/// Get the disposed status.
		/// </summary>
		public bool Disposed
		{
			get
			{
				return _disposed;
			}
		}

		private void Dispose(bool disposing)
		{
			if (!_disposed && disposing)
			{
				_disposed = true;
				Disconnect();
			}
		}

		/// <summary>
		/// Class Destructor.
		/// </summary>
		~Channel()
		{
			Dispose(true);
		}

		#endregion
}
}
