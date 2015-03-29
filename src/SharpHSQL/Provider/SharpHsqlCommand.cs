#region Usings
using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Xml;
using System.Collections;
using SharpHsql;
#endregion

#region License
/*
 * SharpHsqlCommand.cs
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
	/// Command class for Hsql ADO.NET data provider.
	/// <seealso cref="SharpHsqlConnection"/>
	/// <seealso cref="SharpHsqlReader"/>
	/// <seealso cref="SharpHsqlParameter"/>
	/// <seealso cref="SharpHsqlTransaction"/>
	/// </summary>
	public sealed class SharpHsqlCommand : Component, IDbCommand, ICloneable, IDisposable
	{
		#region Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SharpHsqlCommand(){}

		/// <summary>
		/// Constructor using a command text.
		/// </summary>
		/// <param name="sCommand"></param>
		public SharpHsqlCommand(string sCommand)
		{
			_commandText = sCommand;
			_connection = null;
		}

		/// <summary>
		/// Constructor using a command text and connection.
		/// </summary>
		/// <param name="sCommand"></param>
		/// <param name="conn"></param>
		public SharpHsqlCommand(string sCommand, SharpHsqlConnection conn)
		{
			_commandText = sCommand;
			_connection = conn;

			if( _connection.LocalTransaction != null )
				_transaction = _connection.LocalTransaction;
		}

		#endregion

		#region Public Methods
		
		/// <summary>
		/// Command text to be executed.
		/// </summary>
		public string CommandText
		{
			get { return _commandText;  }
			set { _commandText = value; }
		}

		/// <summary>
		/// Get or set Execution timeout for the command.
		/// </summary>
		public int CommandTimeout
		{
			get { return _commandTimeout; }
			set { _commandTimeout = value; }
		}

		/// <summary>
		/// Get or set the Type of the current command.
		/// </summary>
		public CommandType CommandType
		{
			get { return _commandType; }
			set { _commandType = value; }
		}

		/// <summary>
		/// Get or set the Connection being used by the current command.
		/// </summary>
		public SharpHsqlConnection Connection
		{
			get { return _connection;  }
			set { _connection = value; }
		}

		/// <summary>
		/// Get or set the Connection being used by the current command.
		/// </summary>
		IDbConnection IDbCommand.Connection
		{
			get { return _connection;  }
			set { _connection = (SharpHsqlConnection) value; }
		}

		/// <summary>
		/// Command parameter collection.
		/// </summary>
		IDataParameterCollection IDbCommand.Parameters
		{
			get
			{
				if (this._parameters == null)
				{
					this._parameters = new SharpHsqlParameterCollection(this);
				}
				return this._parameters;
			}
		}

		/// <summary>
		/// Command parameter collection.
		/// </summary>
		public SharpHsqlParameterCollection Parameters
		{
			get
			{
				if (this._parameters == null)
				{
					this._parameters = new SharpHsqlParameterCollection(this);
				}
				return this._parameters;
			}
		}

		/// <summary>
		/// Get or set the Transaction object for use by this command.
		/// </summary>
		public SharpHsqlTransaction Transaction
		{
			get
			{
				if ((this._transaction != null) && (this._transaction.Connection == null))
				{
					this._transaction = null;
				}
				return this._transaction;
			}
			set
			{
				if ((this._connection != null) && (this._connection.Reader != null))
				{
					throw new InvalidOperationException("Comand is currently active.");
				}
				this._transaction = value;
			}

		}

		/// <summary>
		/// Get or set the Transaction object for use by this command.
		/// </summary>
		IDbTransaction IDbCommand.Transaction
		{
			get
			{
				if ((this._transaction != null) && (this._transaction.Connection == null))
				{
					this._transaction = null;
				}
				return this._transaction;
			}
			set
			{
				if ((this._connection != null) && (this._connection.Reader != null))
				{
					throw new InvalidOperationException("Comand is currently active.");
				}
				this._transaction = (SharpHsqlTransaction)value;
			}

		}

		/// <summary>
		/// Get or set the <see cref="UpdateRowSource"/> for the command.
		/// </summary>
		public UpdateRowSource UpdatedRowSource
		{
			get
			{
				return this._updatedRowSource;
			}
			set
			{
				if ((value < UpdateRowSource.None) || (value > UpdateRowSource.Both))
				{
					throw new InvalidOperationException("Invalid UpdateRowSource value");
				}
				this._updatedRowSource = value;
			}
		}
 
		/// <summary>
		/// Cancels the current operation.
		/// </summary>
		public void Cancel()
		{
			if( _connection == null || _connection.State != ConnectionState.Open )
				throw new InvalidOperationException("Can't execute if connection is not open.");

		}

		/// <summary>
		/// Creates and returns a new <see cref="SharpHsqlParameter"/> object.
		/// </summary>
		/// <returns></returns>
		public SharpHsqlParameter CreateParameter()
		{
			return new SharpHsqlParameter();
		}

		/// <summary>
		/// Creates and returns a new <see cref="SharpHsqlParameter"/> object.
		/// </summary>
		/// <returns></returns>
		IDbDataParameter IDbCommand.CreateParameter()
		{
			return new SharpHsqlParameter();
		}

		/// <summary>
		/// Executes a query with no results.
		/// </summary>
		/// <returns></returns>
		public int ExecuteNonQuery()
		{
			this.ValidateCommand("ExecuteNonQuery", true);
			
			ResolveParameters();

			Result res = _connection.Execute( _commandText );

			RetrieveOutputParameters( res );

			return res.UpdateCount;
		}

		/// <summary>
		/// Executes a query returning an <see cref="SharpHsqlReader"/> object.
		/// </summary>
		/// <returns></returns>
		IDataReader IDbCommand.ExecuteReader()
		{
			return this.ExecuteReader();
		}

		/// <summary>
		/// Executes a query returning an <see cref="SharpHsqlReader"/> object.
		/// </summary>
		/// <param name="behavior"></param>
		/// <returns></returns>
		IDataReader IDbCommand.ExecuteReader(CommandBehavior behavior)
		{
			return ExecuteReader(behavior);
		}

		/// <summary>
		/// Executes a query returning an <see cref="SharpHsqlReader"/> object.
		/// </summary>
		/// <returns></returns>
		public SharpHsqlReader ExecuteReader()
		{
			return ExecuteReader(CommandBehavior.Default);
		}

		/// <summary>
		/// Executes a query returning an <see cref="SharpHsqlReader"/> object.
		/// </summary>
		/// <param name="behavior"></param>
		/// <returns></returns>
		public SharpHsqlReader ExecuteReader(CommandBehavior behavior)
		{
			this.ValidateCommand("ExecuteReader", true);

			ResolveParameters();

			_result = _connection.Execute( _commandText );

			return new SharpHsqlReader( this );
		}

		/// <summary>
		/// Executes a query returning a single result.
		/// </summary>
		/// <returns></returns>
		public object ExecuteScalar()
		{
			this.ValidateCommand("ExecuteScalar", true);

			object ret = null;

			ResolveParameters();

			Result rs = _connection.Execute( _commandText );

			if( rs != null && rs.Root != null )
			{
				ret = rs.Root.Data[0];
			}

			return ret;
			
		}

		/// <summary>
		/// Executes a query that returns results as XML.
		/// </summary>
		/// <remarks>Not currently supported.</remarks>
		/// <returns></returns>
		public XmlReader ExecuteXmlReader()
		{
			throw new InvalidOperationException("SharpHSql Provider does not support this function");
			/*
			SharpHSQLReader reader = this.ExecuteReader(CommandBehavior.SequentialAccess, RunBehavior.ReturnImmediately, true);
			XmlReader xml = null;
			reader1.Close();
			*/
		}
 
		/// <summary>
		/// Prepare a stored procedure on the database.
		/// </summary>
		/// <remarks>Not currently supported.</remarks>
		public void Prepare()
		{
			throw new InvalidOperationException("SharpHSql Provider does not support this function");
		}

		/// <summary>
		/// Makes a clone of the current object.
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			SharpHsqlCommand cmd = new SharpHsqlCommand();
			cmd.CommandText = this.CommandText;
			cmd.CommandType = this.CommandType;
			cmd.UpdatedRowSource = this.UpdatedRowSource;
			IDataParameterCollection parameters = cmd.Parameters;
			foreach (ICloneable parameter in this.Parameters)
			{
				parameters.Add(parameter.Clone());
			}
			cmd.Connection = this.Connection;
			cmd.Transaction = this.Transaction;
			cmd.CommandTimeout = this.CommandTimeout;
			return cmd;
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Derive parameters from a stored procedure.
		/// </summary>
		internal void DeriveParameters()
		{
			CommandType type = this.CommandType;
			if (type == CommandType.Text)
			{
				throw new InvalidOperationException("Derive Parameters Not Supported");
			}
			if (type != CommandType.StoredProcedure)
			{
				if (type == CommandType.TableDirect)
				{
					throw new InvalidOperationException("Derive Parameters Not Supported");
				}
				throw new InvalidOperationException("Invalid CommandType");
			}
			this.ValidateCommand("DeriveParameters", false);
			SharpHsqlCommand command = new SharpHsqlCommand("sp_procedure_params_rowset", this._connection);
			command.CommandType = CommandType.StoredProcedure;
			command.Parameters.Add(new SharpHsqlParameter("@procedure_name", DbType.String, 0xff));
			command.Parameters[0].Value = this._commandText;
			ArrayList list = new ArrayList();
			try
			{
				try
				{
					SharpHsqlReader reader = command.ExecuteReader();
					try
					{
						SharpHsqlParameter parameter = null;
						while (reader.Read())
						{
							parameter = new SharpHsqlParameter();
							parameter.ParameterName = (string) reader["PARAMETER_NAME"];
							//parameter1.DbType = MetaType.GetSqlDbTypeFromOleDbType((short) reader1["DATA_TYPE"], (string) reader1["TYPE_NAME"]);
							object obj = reader["CHARACTER_MAXIMUM_LENGTH"];
							if (obj is int)
							{
								parameter.Size = (int) obj;
							}
							//parameter1.Direction = this.ParameterDirectionFromOleDbDirection((short) reader1["PARAMETER_TYPE"]);
							if (parameter.DbType == DbType.Decimal)
							{
								parameter.Scale = (byte) (((short) reader["NUMERIC_SCALE"]) & 0xff);
								parameter.Precision = (byte) (((short) reader["NUMERIC_PRECISION"]) & 0xff);
							}
							list.Add(parameter);
						}
					}
					finally
					{
						if (reader != null)
						{
							((IDisposable) reader).Dispose();
						}
					}
				}
				finally
				{
					command.Connection = null;
				}
			}
			catch
			{
				throw;
			}
			if (list.Count == 0)
			{
				throw new InvalidOperationException("No Stored Procedure Exists with that name");
			}
			this.Parameters.Clear();
			foreach (object p in list)
			{
				this._parameters.Add(p);
			}
		}

		/// <summary>
		/// Query results from SharpHsql database.
		/// </summary>
		internal Result Result
		{
			get
			{
				return _result;
			}
		}

		#endregion

		#region Private Methods

		private void RetrieveOutputParameters( Result result )
		{
			if( CommandText != null && CommandText != 
				string.Empty && GetParameterCount() > 0 )
			{
				int index = 0;

				for(int i=0;i<Parameters.Count;i++ )
				{
					SharpHsqlParameter p = Parameters[i];

					string name = p.ParameterName.ToUpper();

					if( name.IndexOf("@") > 0 )
						name = name.Substring(1, name.Length-1);

					if( ShouldSendParameter( p ) )
					{
						p.Value = result.Root.Data[index];
						index++;
					}
				}
			}
		}

		private void ResolveParameters()
		{
			if( CommandText != null && CommandText != 
				string.Empty && GetParameterCount() > 0 )
			{
				string command = CommandText;

				StringBuilder declares = new StringBuilder();
				ArrayList parms = new ArrayList();

				foreach( SharpHsqlParameter p in Parameters )
				{
					string name = p.ParameterName;

					if( ShouldSendParameter( p ) )
					{
						#if !POCKETPC
						declares.AppendFormat( "DECLARE {0} {1};", p.ParameterName, GetDataTypeName( p.DbType ) );
						if( p.Direction == ParameterDirection.Input || 
							p.Direction == ParameterDirection.InputOutput )
							declares.AppendFormat(null, "SET {0} = {1};", p.ParameterName, GetParameterValue( p.Value ) );
						#else
						declares.AppendFormat(null, "DECLARE {0} {1};", p.ParameterName, GetDataTypeName( p.DbType ) );
						if( p.Direction == ParameterDirection.Input || 
							p.Direction == ParameterDirection.InputOutput )
							declares.AppendFormat(null, "SET {0} = {1};", p.ParameterName, GetParameterValue( p.Value ) );
						#endif
						parms.Add( p.ParameterName );
					}
					else
					{
						if( command.IndexOf( name ) > -1 )
							command = command.Replace( name, GetParameterValue(p.Value) );
					}
				}
				CommandText = string.Concat( declares.ToString(), command, BuildParameterSelect( parms) )  ;
			}
		}

		private string BuildParameterSelect( ArrayList list )
		{
			if( list == null || list.Count == 0 )
				return string.Empty;

			StringBuilder select = new StringBuilder( "SELECT " );
			for(int i=0;i<list.Count;i++)
			{
				if( i > 0 )
					select.Append( ", " );

				select.Append( list[i].ToString() );
			}

			select.Append(";");

			return select.ToString();
		}

		private string GetParameterValue( object value )
		{
			if( value == DBNull.Value || value == null )
				return "NULL";

			if( value is Enum )
				return ((int)value).ToString();

			switch( value.GetType().Name )
			{
				case "Guid":
					return "'" + ((Guid)value).ToString("N") + "'";
				case "DateTime":
					return "'" + ((DateTime)value).ToString("yyyy.MM.dd HH:mm:ss.fffffff") + "'";
				case "String":
				case "Char":
					return "'" + value.ToString().Replace('\'', '´') + "'";
				case "Byte[]":
					return "'" + new ByteArray( (byte[])value ).ToString() + "'";
				default:
					if( value is ValueType )
						return value.ToString();
					else
						return "'" + ByteArray.SerializeTostring( value ) + "'";

			}
		}

		private int GetParameterCount()
		{
			if (this._parameters == null)
			{
				return 0;
			}
			return this._parameters.Count;
		}

		private bool ShouldSendParameter(SharpHsqlParameter p)
		{
			switch (p.Direction)
			{
				case ParameterDirection.Input:
				{
					return false;
				}
				case ParameterDirection.Output:
				case ParameterDirection.InputOutput:
				{
					return true;
				}
				case ParameterDirection.ReturnValue:
				{
					return false;
				}
			}
			return false;
		}

		private void ValidateCommand(string method, bool executing)
		{
			if (this._connection == null)
			{
				throw new InvalidOperationException("Connection Required for " + method);
			}
			if (ConnectionState.Open != this._connection.State)
			{
				throw new InvalidOperationException("Open Connection Required for " + method);
			}
			this._connection.CloseDeadReader();
			if ((this._connection.Reader != null))
			{
				throw new InvalidOperationException("An Open Reader Exists");
			}
			if (!executing)
			{
				return;
			}
			this._connection.RollbackDeadTransaction();
			if ((this._transaction != null) && (this._transaction._sqlConnection == null))
			{
				this._transaction = null;
			}
			if ((this._connection.LocalTransaction != null) && (this._transaction == null))
			{
				throw new InvalidOperationException("Transaction is Required");
			}
			if ((this._transaction != null) && (this._connection != this._transaction._sqlConnection))
			{
				throw new InvalidOperationException("Transaction Connection Mismatch");
			}
			if (this.CommandText == null || this.CommandText.Trim() == string.Empty )
			{
				throw new InvalidOperationException("CommandText is Required for " + method);
			}
		}

		private string GetDataTypeName( DbType type )
		{
			switch( type )
			{
				case DbType.AnsiString:
				case DbType.String:
					return "VARCHAR";				
				case DbType.AnsiStringFixedLength:
				case DbType.StringFixedLength:
					return "CHAR";
				case DbType.Boolean:
					return "BIT";
				case DbType.Binary:
					return "BINARY";
				case DbType.Byte:
					return "TINYINT";
				case DbType.Currency:
					return "DECIMAL";
				case DbType.Date:
					return "DATE";
				case DbType.DateTime:
					return "DATE";
				case DbType.Decimal:
					return "DECIMAL";
				case DbType.Double:
					return "DOUBLE";
				case DbType.Guid:
					return "UNIQUEIDENTIFIER";
				case DbType.Int16:
					return "SMALLINT";
				case DbType.Int32:
					return "INT";
				case DbType.Int64:
					return "BIGINT";
				case DbType.Object:
					return "OBJECT";
				case DbType.SByte:
					return "SMALLINT";
				case DbType.Single:
					return "REAL";
				case DbType.Time:
					return "TIME";
				case DbType.UInt16:
					return "INT";
				case DbType.UInt32:
					return "BIGINT";
				case DbType.UInt64:
					return "NUMERIC";
				case DbType.VarNumeric:
					return "NUMERIC";
				default:
					return "OTHER";
			}
		}	
		#endregion

		#region Internal Fields

		/// <summary>
		/// Connection object used internally.
		/// </summary>
		internal SharpHsqlConnection _connection = null;

		#endregion

		#region Private fields

		private string _commandText = String.Empty;
		private int _commandTimeout = 0;
		private CommandType _commandType = CommandType.Text;
		private SharpHsqlTransaction _transaction = null;
		private SharpHsqlParameterCollection _parameters = null;
		private Result _result = null;
		private UpdateRowSource _updatedRowSource;
 
		#endregion

		#region IDisposable Members

		/// <summary>
		/// Dispose the current command.
		/// </summary>
		void IDisposable.Dispose()
		{
			base.Dispose( true );
		}

		#endregion
	}
}