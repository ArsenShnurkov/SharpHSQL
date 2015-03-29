#region Usings
using System;
using System.Collections;
#endregion

#region License
/*
 * Database.cs
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
	/// Database class declaration.
	/// Database is the root class for HSQL Database Engine database.
	/// This class should not be used directly by the application,
	/// instead the ADO.NET provider classes should be used.
	/// <seealso cref="System.Data.Hsql.SharpHsqlConnection"/>
	/// <seealso cref="System.Data.Hsql.SharpHsqlCommand"/>
	/// <seealso cref="System.Data.Hsql.SharpHsqlReader"/>
	/// <seealso cref="System.Data.Hsql.SharpHsqlParameter"/>
	/// <seealso cref="System.Data.Hsql.SharpHsqlTransaction"/>
	/// <seealso cref="System.Data.Hsql.SharpHsqlDataAdapter"/>
	/// </summary>
	public sealed class Database : IDisposable
	{
		#region Constructor

		/// <summary>
		/// Database class constructor.
		/// </summary>
		/// <param name="name">The database name to open or create.</param>
		public Database(string name) 
		{
			if (Trace.TraceEnabled) 
			{
				Trace.Write();
			}

			_name = name;
			_table = new ArrayList();
			_access = new Access();
			_channel = new Hashtable();
			_alias = new Hashtable();
			_referentialIntegrity = true;

			Library.Register(_alias);

			_databaseInfo = new DatabaseInformation(this, _table, _access);

			bool newdatabase = false;
			Channel sys = new Channel(this, new User(null, null, true, null),
				true, false, 0);

			RegisterChannel(sys);

			if (name.Equals(".")) 
			{
				newdatabase = true;
			} 
			else 
			{
				_log = new Log(this, sys, name);
				newdatabase = _log.Open();
			}

			if (newdatabase) 
			{
				Execute("CREATE USER SA PASSWORD \"\" ADMIN", sys);
			}

			//_access.grant("PUBLIC", "CLASS \"SharpHSQL.Library\"", Access.ALL);
		}

		#endregion

		#region Public Properties & Methods

		/// <summary>
		/// Returns the database name.
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
		}

		/// <summary>
		/// Returns True if the database is shutdown.
		/// </summary>
		public bool IsShutdown
		{
			get
			{
				return _shutDown;
			}
		}

		/// <summary>
		/// Connects to the database using an user and password.
		/// </summary>
		/// <param name="userName">The user name to connect.</param>
		/// <param name="password">The password to use.</param>
		/// <returns>The created channel object.</returns>
		public Channel Connect(string userName, string password) 
		{
			User user = _access.GetUser(userName.ToUpper(), password.ToUpper());
			int  size = _channel.Count, id = size;

			for (int i = 0; i < size; i++) 
			{
				if (_channel[i] == null) 
				{
					id = i;
					break;
				}
			}

			Channel c = new Channel(this, user, true, _readOnly, id);

			if (_log != null) 
				_log.Write(c, String.Concat("CONNECT USER ", userName, " PASSWORD \"", password, "\"") );

			RegisterChannel(c);

			return c;
		}

		/// <summary>
		/// Executes an SQL statement and return the results.
		/// </summary>
		/// <param name="user">The user name to use.</param>
		/// <param name="password">The password to use.</param>
		/// <param name="statement">The SQL statement to execute.</param>
		/// <returns>The Result object.</returns>
		public byte[] Execute(string user, string password, string statement) 
		{
			Result r = null;

			try 
			{
				Channel channel = Connect(user, password);

				r = Execute(statement, channel);

				Execute("DISCONNECT", channel);
			} 
			catch (Exception e) 
			{
				r = new Result(e.Message);
			}

			try 
			{
				return r.GetBytes();
			} 
			catch (Exception e) 
			{
				LogHelper.Publish( "Unexpected error on execute.", e );
				return new byte[0];
			}
		}

		/// <summary>
		/// Executes an SQL statement and return the results.
		/// </summary>
		/// <param name="statement">The SQL statement to execute.</param>
		/// <param name="channel">The channel to use.</param>
		/// <returns>The Result object.</returns>
		public Result Execute(string statement, Channel channel) 
		{
			if (Trace.TraceEnabled) 
			{
				Trace.Write(statement);
			}

			Tokenizer c = new Tokenizer(statement);
			Parser    p = new Parser(this, c, channel);
			Result    rResult = new Result();
			string    newStatement = string.Empty;
			int updateCount = 0;

			try 
			{
				if (_log != null && _log.cCache != null) 
				{
					_log.cCache.CleanUp();
				}

				if (Trace.AssertEnabled) 
				{
					Trace.Assert(!channel.IsNestedTransaction);
				}

				Trace.Check(channel != null, Trace.ACCESS_IS_DENIED);
				Trace.Check(!_shutDown, Trace.DATABASE_IS_SHUTDOWN);

				while (true) 
				{
					int     begin = c.Position;
					bool script = false;
					string  sToken = c.GetString();

					if( sToken.Equals("") )
						break;

					switch(sToken)
					{
						case "SELECT":
							rResult = p.ProcessSelect();
							break;
						case "INSERT":
							rResult = p.ProcessInsert();
							break;
						case "UPDATE":
							rResult = p.ProcessUpdate();
							break;
						case "DELETE":
							rResult = p.ProcessDelete();
							break;
						case "ALTER":
							rResult=p.ProcessAlter();
							break;
						case "CREATE":
							rResult = ProcessCreate(c, channel);
							script = true;
							break;
						case "DROP":
							rResult = ProcessDrop(c, channel);
							script = true;
							break;
						case "GRANT":
							rResult = ProcessGrantOrRevoke(c, channel, true);
							script = true;
							break;
						case "REVOKE":
							rResult = ProcessGrantOrRevoke(c, channel, false);
							script = true;
							break;
						case "CONNECT":
							rResult = ProcessConnect(c, channel);
							break;
						case "DISCONNECT":
							rResult = ProcessDisconnect(c, channel);
							break;
						case "SET":
							rResult = ProcessSet(c, channel);
							script = true;
							break;
						case "SCRIPT":
							rResult = ProcessScript(c, channel);
							break;
						case "COMMIT":
							rResult = ProcessCommit(c, channel);
							script = true;
							break;
						case "ROLLBACK":
							rResult = ProcessRollback(c, channel);
							script = true;
							break;
						case "SHUTDOWN":
							rResult = ProcessShutdown(c, channel);
							break;
						case "CHECKPOINT":
							rResult = ProcessCheckpoint(channel);
							break;
						case "CALL":
							rResult = p.ProcessCall();
							break;
						case "SHOW":
							rResult = ProcessShow(c,channel);
							break;
						case "DECLARE":
							rResult = p.ProcessDeclare();
							script = true;
							break;
						case ";":
							continue;
						default:
							throw Trace.Error(Trace.UnexpectedToken, sToken);
					}

					if( rResult != null && rResult.UpdateCount > updateCount )
						updateCount = rResult.UpdateCount;

					if (script && _log != null) 
					{
						int end = c.Position;

						_log.Write(channel, c.GetPart(begin, end));
					}
				}
			} 
			catch (Exception e) 
			{
				rResult = new Result(Trace.GetMessage(e) + " in statement [" + statement + "]");
			} 

			if( rResult != null && rResult.UpdateCount < updateCount )
				rResult.SetUpdateCount( updateCount );

			return rResult;
		}

		/// <summary>
		/// Sets the database readonly mode.
		/// </summary>
		public void SetReadOnly() 
		{
			_readOnly = true;
		}

		/// <summary>
		/// True if case must be ignored.
		/// </summary>
		public bool IsIgnoreCase
		{
			get
			{
				return _ignoreCase;
			}
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Gets the tables collection.
		/// </summary>
		internal ArrayList Tables
		{
			get
			{
				return _table;
			}
		}

		/// <summary>
		/// Register an existing channel in this database.
		/// </summary>
		/// <param name="channel"></param>
		internal void RegisterChannel(Channel channel) 
		{
			int id = channel.Id;
			_channel[id] = channel;
		}

		/// <summary>
		/// Gets or sets the referential integrity check.
		/// </summary>
		internal bool IsReferentialIntegrity
		{
			get
			{
				return _referentialIntegrity;
			}
			set
			{
				_referentialIntegrity = value;
			}
		}

		/// <summary>
		/// Gets the database alias objects.
		/// </summary>
		internal Hashtable Alias
		{
			get
			{
				return _alias;
			}
		}

		internal string GetAlias(string source) 
		{
			object o = _alias[source];

			if (o == null) 
			{
				return source;
			}

			return (string) o;
		}

		internal Log Log
		{
			get
			{
				return _log;
			}
		}

		internal Table GetTable(string name, Channel channel) 
		{
			Table t = null;

			for (int i = 0; i < _table.Count; i++) 
			{
				t = (Table) _table[i];

				if (t.Name.ToLower().Equals(name.ToLower())) 
				{
					return t;
				}
			}

			t = _databaseInfo.GetSystemTable(name, channel);

			if (t == null) 
			{
				throw Trace.Error(Trace.TABLE_NOT_FOUND, name);
			}

			return t;
		}

		internal Result GetScript(bool drop, bool insert, bool cached, Channel channel) 
		{
			return _databaseInfo.GetScript(drop, insert, cached, channel);
		}

		internal void LinkTable(Table table) 
		{
			string name = table.Name;

			for (int i = 0; i < _table.Count; i++) 
			{
				Table o = (Table)_table[i];

				if (o.Name.Equals(name)) 
				{
					throw Trace.Error(Trace.TABLE_ALREADY_EXISTS, name);
				}
			}

			_table.Add(table);
		}

		internal Channel SysChannel
		{
			get
			{
				return (Channel)_channel[0];
			}
		}

		#endregion

		#region Private Methods

		private Result ProcessScript(Tokenizer tokenizer, Channel channel) 
		{
			string sToken = tokenizer.GetString();

			if (tokenizer.WasValue) 
			{
				sToken = (string) tokenizer.Value;

				Log.ScriptToFile(this, sToken, true, channel);

				return new Result();
			} 
			else 
			{
				tokenizer.Back();

				// try to script all: drop, insert; but no positions for cached tables
				return GetScript(true, true, false, channel);
			}
		}

		private Result ProcessCreate(Tokenizer tokenizer, Channel channel) 
		{
			channel.CheckReadWrite();
			channel.CheckAdmin();

			string sToken = tokenizer.GetString();

			switch(sToken)
			{
				case "TABLE":
					ProcessCreateTable(tokenizer, channel, true);
					break;
				case "MEMORY":
					tokenizer.GetThis("TABLE");
					ProcessCreateTable(tokenizer, channel, false);
					break;
				case "CACHED":
					tokenizer.GetThis("TABLE");
					ProcessCreateTable(tokenizer, channel, true);
					break;
				case "USER":
				{
					string u = tokenizer.GetStringToken();

					tokenizer.GetThis("PASSWORD");

					string  p = tokenizer.GetStringToken();
					bool admin;

					if (tokenizer.GetString().Equals("ADMIN")) 
					{
						admin = true;
					} 
					else 
					{
						admin = false;
					}

					_access.CreateUser(u, p, admin);
				}
					break;
				case "ALIAS":
				{
					string name = tokenizer.GetString();

					sToken = tokenizer.GetString();

					Trace.Check(sToken.Equals("FOR"), Trace.UnexpectedToken, sToken);

					sToken = tokenizer.GetString();

					_alias[name] = sToken;
				} 
					break;
				default:
				{
					bool unique = false;

					if (sToken.Equals("UNIQUE")) 
					{
						unique = true;
						sToken = tokenizer.GetString();
					}

					if (!sToken.Equals("INDEX")) 
					{
						throw Trace.Error(Trace.UnexpectedToken, sToken);
					}

					string name = tokenizer.GetName();

					tokenizer.GetThis("ON");

					Table t = GetTable(tokenizer.GetString(), channel);

					AddIndexOn(tokenizer, channel, name, t, unique);
				}
					break;
			}

			return new Result();
		}

		private int[] ProcessColumnList(Tokenizer tokenizer, Table table) 
		{
			ArrayList v = new ArrayList();

			tokenizer.GetThis("(");

			while (true) 
			{
				v.Add(tokenizer.GetString());

				string sToken = tokenizer.GetString();

				if (sToken.Equals(")")) 
				{
					break;
				}

				if (!sToken.Equals(",")) 
				{
					throw Trace.Error(Trace.UnexpectedToken, sToken);
				}
			}

			int s = v.Count;
			int[] col = new int[s];

			for (int i = 0; i < s; i++) 
			{
				col[i] = table.GetColumnNumber((string) v[i]);
			}

			return col;
		}

		private void CreateIndex(Channel channel, Table t, int[] col, string name, bool unique) 
		{
			channel.Commit();

			if (t.IsEmpty) 
			{
				t.CreateIndex(col, name, unique);
			} 
			else 
			{
				Table tn = t.MoveDefinition(null);

				tn.CreateIndex(col, name, unique);
				tn.MoveData(t);
				DropTable(t.Name);
				LinkTable(tn);
			}
		}

		private void AddForeignKeyOn(Tokenizer tokenizer, Channel channel, string name, Table table) 
		{
			int[] col = ProcessColumnList(tokenizer, table);

			tokenizer.GetThis("REFERENCES");

			Table t2 = GetTable(tokenizer.GetString(), channel);
			int[]   col2 = ProcessColumnList(tokenizer, t2);

			if (table.GetIndexForColumns(col) == null) 
			{
				CreateIndex(channel, table, col, "SYSTEM_FOREIGN_KEY_" + name, false);
			}

			if (t2.GetIndexForColumns(col2) == null) 
			{
				CreateIndex(channel, t2, col2, "SYSTEM_REFERENCE_" + name, false);
			}

			table.AddConstraint(new Constraint(ConstraintType.ForeignKey, t2, table, col2,	col));
			t2.AddConstraint(new Constraint(ConstraintType.Main, t2, table, col2, col));
		}

		private void AddUniqueConstraintOn(Tokenizer tokenizer, Channel channel, string name, Table table) 
		{
			int[] col = ProcessColumnList(tokenizer, table);

			CreateIndex(channel, table, col, name, true);
			table.AddConstraint(new Constraint(ConstraintType.Unique, table, col));
		}

		private void AddIndexOn(Tokenizer tokenizer, Channel channel, string name, Table table, bool unique) 
		{
			int[] col = ProcessColumnList(tokenizer, table);

			CreateIndex(channel, table, col, name, unique);
		}

		private void ProcessCreateTable(Tokenizer tokenizer, Channel channel, bool cached) 
		{
			Table  t;
			string sToken = tokenizer.GetName();

			if (cached && _log != null) 
			{
				t = new Table(this, true, sToken, true);
			} 
			else 
			{
				t = new Table(this, true, sToken, false);
			}

			tokenizer.GetThis("(");

			int     primarykeycolumn = -1;
			int     column = 0;
			bool constraint = false;

			while (true) 
			{
				bool identity = false;

				sToken = tokenizer.GetString();

				if (sToken.Equals("CONSTRAINT") || sToken.Equals("PRIMARY")
					|| sToken.Equals("FOREIGN") || sToken.Equals("UNIQUE")) 
				{
					tokenizer.Back();

					constraint = true;

					break;
				}

				string sColumn = sToken;
				ColumnType iType = Column.GetColumnType(tokenizer.GetString());

				if (iType == ColumnType.VarChar && _ignoreCase) 
				{
					iType = ColumnType.VarCharIgnoreCase;
				}

				sToken = tokenizer.GetString();

				if (iType == ColumnType.DbDouble && sToken.Equals("PRECISION")) 
				{
					sToken = tokenizer.GetString();
				}

				if (sToken.Equals("(")) 
				{

					// overread length
					do 
					{
						sToken = tokenizer.GetString();
					} while (!sToken.Equals(")"));

					sToken = tokenizer.GetString();
				}

				bool nullable = true;

				if (sToken.Equals("NULL")) 
				{
					sToken = tokenizer.GetString();
				} 
				else if (sToken.Equals("NOT")) 
				{
					tokenizer.GetThis("NULL");

					nullable = false;
					sToken = tokenizer.GetString();
				}

				if (sToken.Equals("IDENTITY")) 
				{
					identity = true;

					Trace.Check(primarykeycolumn == -1, Trace.SECOND_PRIMARY_KEY,
						sColumn);

					sToken = tokenizer.GetString();
					primarykeycolumn = column;
				}

				if (sToken.Equals("PRIMARY")) 
				{
					tokenizer.GetThis("KEY");
					Trace.Check(identity || primarykeycolumn == -1,
						Trace.SECOND_PRIMARY_KEY, sColumn);

					primarykeycolumn = column;
					sToken = tokenizer.GetString();
				}

				t.AddColumn(sColumn, iType, nullable, identity);

				if (sToken.Equals(")")) 
				{
					break;
				}

				if (!sToken.Equals(",")) 
				{
					throw Trace.Error(Trace.UnexpectedToken, sToken);
				}

				column++;
			}

			if (primarykeycolumn != -1) 
			{
				t.CreatePrimaryKey(primarykeycolumn);
			} 
			else 
			{
				t.CreatePrimaryKey();
			}

			if (constraint) 
			{
				int i = 0;

				while (true) 
				{
					sToken = tokenizer.GetString();

					string name = "SYSTEM_CONSTRAINT" + i;

					i++;

					if (sToken.Equals("CONSTRAINT")) 
					{
						name = tokenizer.GetString();
						sToken = tokenizer.GetString();
					}

					if (sToken.Equals("PRIMARY")) 
					{
						tokenizer.GetThis("KEY");
						AddUniqueConstraintOn(tokenizer, channel, name, t);
					} 
					else if (sToken.Equals("UNIQUE")) 
					{
						AddUniqueConstraintOn(tokenizer, channel, name, t);
					} 
					else if (sToken.Equals("FOREIGN")) 
					{
						tokenizer.GetThis("KEY");
						AddForeignKeyOn(tokenizer, channel, name, t);
					}

					sToken = tokenizer.GetString();

					if (sToken.Equals(")")) 
					{
						break;
					}

					if (!sToken.Equals(",")) 
					{
						throw Trace.Error(Trace.UnexpectedToken, sToken);
					}
				}
			}

			channel.Commit();
			LinkTable(t);
		}

		private Result ProcessDrop(Tokenizer tokenizer, Channel channel) 
		{
			channel.CheckReadWrite();
			channel.CheckAdmin();

			string sToken = tokenizer.GetString();

			if (sToken.Equals("TABLE")) 
			{
				sToken = tokenizer.GetString();

				if (sToken.Equals("IF")) 
				{
					sToken = tokenizer.GetString();    // EXISTS
					sToken = tokenizer.GetString();    // <table>

					DropTable(sToken, true);
				} 
				else 
				{
					DropTable(sToken, false);
				}
				channel.Commit();
			} 
			else if (sToken.Equals("USER")) 
			{
				_access.DropUser(tokenizer.GetStringToken());
			} 
			else if (sToken.Equals("INDEX")) 
			{
				sToken = tokenizer.GetString();

				if (!tokenizer.WasLongName) 
				{
					throw Trace.Error(Trace.UnexpectedToken, sToken);
				}

				string table = tokenizer.LongNameFirst;
				string index = tokenizer.LongNameLast;
				Table  t = GetTable(table, channel);

				t.CheckDropIndex(index);

				Table tn = t.MoveDefinition(index);

				tn.MoveData(t);
				DropTable(table);
				LinkTable(tn);
				channel.Commit();
			} 
			else 
			{
				throw Trace.Error(Trace.UnexpectedToken, sToken);
			}

			return new Result();
		}

		private Result ProcessShow(Tokenizer tokenizer, Channel channel)
		{
			Result r = new Result(1);

			string sToken = tokenizer.GetString();

			if (sToken.Equals("TABLES"))
			{
				System.Collections.ArrayList al = channel.Database.Tables;
				r.Label[0]="TABLE";
				r.Type[0] = ColumnType.VarChar;
				for(int x=0;x<al.Count;x++)
				{
					Table table = (Table)al[x];
					string[] tablename = new string [1];
					tablename[0]=table.Name;
					r.Add(tablename);
				}
				channel.Commit();
			}
			else if (sToken.Equals("DATABASES"))
			{
				r.Label[0]="DATABASE";
				r.Type[0] = ColumnType.VarChar;

				System.IO.DirectoryInfo di = new 
					System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory());
				System.IO.FileInfo[] rgFiles = di.GetFiles("*.data");
				foreach(System.IO.FileInfo fi in rgFiles)
				{

					string[] databaseName = new string [1];
					databaseName[0]=fi.Name.ToUpper().Replace(".DATA","");
					r.Add(databaseName);
				}

				channel.Commit();
			}
			else if (sToken.Equals("ALIAS"))
			{
				r = new Result(2);
				r.Label[0]="NAME";
				r.Type[0] = ColumnType.VarChar;
				r.Label[1]="LIBRARY";
				r.Type[1] = ColumnType.VarChar;

				foreach( DictionaryEntry entry in _alias )
				{
					string[] alias = new string [2];
					alias[0] = entry.Key.ToString();
					alias[1] = entry.Value.ToString();
					r.Add(alias);
				}

				channel.Commit();
			}
			else if (sToken.Equals("PARAMETERS"))
			{
				string alias = tokenizer.GetString().ToUpper();

				if( !_alias.ContainsKey( alias ) )
					throw Trace.Error(Trace.UNKNOWN_FUNCTION, alias);

				string fqn = _alias[alias].ToString();

				Function f = new Function( fqn, channel );

				System.Reflection.MethodInfo mi = f.GetMethodInfo( fqn );

				r = new Result(4);
				r.Label[0]="ALIAS";
				r.Type[0] = ColumnType.VarChar;
				r.Label[1]="PARAMETER";
				r.Type[1] = ColumnType.VarChar;
				r.Label[2]="TYPE";
				r.Type[2] = ColumnType.VarChar;
				r.Label[3]="POSITION";
				r.Type[3] = ColumnType.Integer;

				System.Reflection.ParameterInfo[] parms = mi.GetParameters();

				int rt = 0;

				if( mi.ReturnType != null )
				{
					object[] p = new object[4];
					p[0] = alias;
					p[1] = "RETURN_VALUE";
					p[2] = Column.GetColumnTypeString( Function.GetDataType( mi.ReturnType ) );
					p[3] = 0;
					r.Add(p);
					rt = 1;
				}

				foreach( System.Reflection.ParameterInfo pi in parms )
				{
					object[] p = new object[4];
					p[0] = alias;
					p[1] = pi.Name;
					p[2] = Column.GetColumnTypeString( Function.GetDataType( pi.ParameterType ) );
					p[3] = (pi.Position + rt);
					r.Add(p);
				}

				channel.Commit();
			}
			else if (sToken.Equals("COLUMNS"))
			{
				string t = tokenizer.GetString().ToUpper();
				Table theTable = null;

				foreach( Table table in channel.Database.Tables )
				{
					if( table.Name.ToUpper() == t )
					{
						theTable = table;
						break;
					}
				}

				if( theTable == null )
					throw Trace.Error(Trace.TABLE_NOT_FOUND, t);

				r = new Result(7);
				r.Label[0]="TABLE";
				r.Type[0] = ColumnType.VarChar;
				r.Label[1]="COLUMN";
				r.Type[1] = ColumnType.VarChar;
				r.Label[2]="NATIVETYPE";
				r.Type[2] = ColumnType.VarChar;
				r.Label[3]="DBTYPE";
				r.Type[3] = ColumnType.Integer;
				r.Label[4]="POSITION";
				r.Type[4] = ColumnType.Integer;
				r.Label[5]="NULLABLE";
				r.Type[5] = ColumnType.Bit;
				r.Label[6]="IDENTITY";
				r.Type[6] = ColumnType.Bit;

				for(int ix=0;ix<theTable.ColumnCount;ix++)
				{
					Column col = theTable.GetColumn(ix);
					object[] coldata = new object[7];
					coldata[0] = theTable.Name;
					coldata[1] = col.Name;
					coldata[2] = Column.GetColumnTypeString( col.ColumnType );
					coldata[3] = Column.GetDbType( col.ColumnType );
					coldata[4] = ix;
					coldata[5] = col.IsNullable;
					coldata[6] = col.IsIdentity;
					r.Add(coldata);
				}
				channel.Commit();
			}
			else
			{
				throw Trace.Error(Trace.UnexpectedToken, sToken);
			}

			return r;
		}

		private Result ProcessGrantOrRevoke(Tokenizer c, Channel channel, bool grant) 
		{
			channel.CheckReadWrite();
			channel.CheckAdmin();

			AccessType    right = AccessType.None;
			string sToken;

			do 
			{
				string sRight = c.GetString();

				right |= Access.GetRight(sRight);
				sToken = c.GetString();
			} while (sToken.Equals(","));

			if (!sToken.Equals("ON")) 
			{
				throw Trace.Error(Trace.UnexpectedToken, sToken);
			}

			string table = c.GetString();

			if (table.Equals("CLASS")) 
			{
				// object is saved as 'CLASS "java.lang.Math"'
				// tables like 'CLASS "xy"' should not be created
				table += " \"" + c.GetString() + "\"";
			} 
			else 
			{
				GetTable(table, channel);    // to make sure the table exists
			}

			c.GetThis("TO");

			string user = c.GetStringToken();
			//			string command;

			if (grant) 
			{
				_access.Grant(user, table, right);

				//				command = "GRANT";
			} 
			else 
			{
				_access.Revoke(user, table, right);

				//				command = "REVOKE";
			}

			return new Result();
		}

		private Result ProcessConnect(Tokenizer c,
			Channel channel) 
		{
			c.GetThis("USER");

			string username = c.GetStringToken();

			c.GetThis("PASSWORD");

			string password = c.GetStringToken();
			User   user = _access.GetUser(username, password);

			channel.Commit();
			channel.SetUser(user);

			return new Result();
		}

		private Result ProcessDisconnect(Tokenizer tokenizer, Channel channel) 
		{
			if (!channel.IsClosed) 
			{
				channel.Disconnect();
				_channel.Remove(channel.Id);
			}

			return new Result();
		}

		private Result ProcessSet(Tokenizer tokenizer, Channel channel) 
		{
			string sToken = tokenizer.GetString();

			switch(sToken)
			{
				case "PASSWORD":
					channel.CheckReadWrite();
					channel.SetPassword(tokenizer.GetStringToken());
					break;
				case "READONLY":
					channel.Commit();
					channel.SetReadOnly(ProcessTrueOrFalse(tokenizer));
					break;
				case "LOGSIZE":
				{
					channel.CheckAdmin();

					int i = Int32.Parse(tokenizer.GetString());

					if (_log != null) 
					{
						_log.SetLogSize(i);
					}
				}
					break;
				case "IGNORECASE":
					channel.CheckAdmin();
					_ignoreCase = ProcessTrueOrFalse(tokenizer);
					break;
				case "MAXROWS":
				{
					int i = Int32.Parse(tokenizer.GetString());
					channel.MaxRows = i;
					break;
				}
				case "AUTOCOMMIT":
					channel.SetAutoCommit(ProcessTrueOrFalse(tokenizer));
					break;
				case "TABLE":
				{
					channel.CheckReadWrite();
					channel.CheckAdmin();

					Table t = GetTable(tokenizer.GetString(), channel);

					tokenizer.GetThis("INDEX");
					tokenizer.GetString();
					t.IndexRoots = (string)tokenizer.Value;
				}
					break;
				case "REFERENCIAL_INTEGRITY":
				case "REFERENTIAL_INTEGRITY":
					channel.CheckAdmin();
					_referentialIntegrity = ProcessTrueOrFalse(tokenizer);
					break;
				case "WRITE_DELAY":
				{
					channel.CheckAdmin();

					bool delay = ProcessTrueOrFalse(tokenizer);

					if (_log != null) 
					{
						_log.SetWriteDelay(delay);
					}
				}
					break;
				default:
					if( tokenizer.TokenType == TokenType.VARIABLE )
					{
						Parser p = new Parser(this, tokenizer, channel);
						p.ProcessSet( sToken );
						break;
					}

					throw Trace.Error(Trace.UnexpectedToken, sToken);
			}

			return new Result();
		}

		private bool ProcessTrueOrFalse(Tokenizer tokenizer) 
		{
			string sToken = tokenizer.GetString();

			switch(sToken)
			{
				case "TRUE":
					return true;
				case "FALSE":
					return false;
				default:
					throw Trace.Error(Trace.UnexpectedToken, sToken);
			}
		}

		private Result ProcessCommit(Tokenizer c,
			Channel channel) 
		{
			string sToken = c.GetString();

			if (!sToken.Equals("WORK")) 
			{
				c.Back();
			}

			channel.Commit();

			return new Result();
		}

		private Result ProcessRollback(Tokenizer tokenizer,	Channel channel) 
		{
			string sToken = tokenizer.GetString();

			if (!sToken.Equals("WORK")) 
			{
				tokenizer.Back();
			}

			channel.Rollback();

			return new Result();
		}

		private void Close(int type) 
		{
			if (_log == null) 
			{
				return;
			}

			_closed = true;

			_log.Stop();

			if (type == -1) 
			{
				_log.Shutdown();
			} 
			else if (type == 0) 
			{
				_log.Close(false);
			} 
			else if (type == 1) 
			{
				_log.Close(true);
			}

			_log = null;
			_shutDown = true;
		}

		private Result ProcessShutdown(Tokenizer tokenizer, Channel channel) 
		{
			channel.CheckAdmin();

			// don't disconnect system user; need it to save database
			for (int i = 1; i < _channel.Count; i++) 
			{
				Channel d = (Channel) _channel[i];

				if (d != null) 
				{
					d.Disconnect();
				}
			}

			_channel.Clear();

			string token = tokenizer.GetString();

			switch(token)
			{
				case "IMMEDIATELY":
					Close(-1);
					break;
				case "COMPACT":
					Close(1);
					break;
				default:
					tokenizer.Back();
					Close(0);
					break;
			}

			ProcessDisconnect(tokenizer, channel);

			return new Result();
		}

		private Result ProcessCheckpoint(Channel channel) 
		{
			channel.CheckAdmin();

			if (_log != null) 
			{
				_log.Checkpoint();
			}

			return new Result();
		}

		private void DropTable(string name) 
		{
			DropTable(name, false);
		}

		private void DropTable(string name, bool bExists) 
		{
			for (int i = 0; i < _table.Count; i++) 
			{
				Table o = (Table) _table[i];

				if (o.Name.Equals(name)) 
				{
					_table.RemoveAt(i);

					return;
				}
			}

			if (!bExists) 
			{
				throw Trace.Error(Trace.TABLE_NOT_FOUND, name);
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

		private void Dispose(bool disposing)
		{
			if (!_closed && disposing)
			{
				_closed = true;

				try
				{
					Close(-1);
				}
				catch (Exception e)
				{
					// it's too late now
					LogHelper.Publish("Unexpected error on Dispose.", e);
				}
			}
		}

		#endregion

		#region Class Destructor

		/// <summary>
		/// Class Destructor.
		/// </summary>
 		~Database()
		{
			Dispose(true);
		}

		#endregion

		#region Private Vars

		private string				_name;
		private Access				_access;
		private ArrayList			_table;
		private DatabaseInformation _databaseInfo;
		private Log					_log;
		private bool				_readOnly;
		private bool	            _closed;
		private bool				_shutDown;
		private Hashtable			_alias;
		private bool				_ignoreCase;
		private bool				_referentialIntegrity;
		private Hashtable			_channel;

		#endregion
	}
}
