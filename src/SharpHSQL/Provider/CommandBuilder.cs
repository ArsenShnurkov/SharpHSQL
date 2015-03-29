#region Usings
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Text;
using System.Globalization;
#endregion

#region License
/*
 * CommandBuilder.cs
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
	/// Command builder class.
	/// <seealso cref="SharpHsqlCommand"/>
	/// <seealso cref="SharpHsqlDataAdapter"/>
	/// </summary>
	internal sealed class CommandBuilder: IDisposable
	{
		#region Constructor

		/// <summary>
		/// Default constructor.
		/// </summary>
		internal CommandBuilder()
		{
		}

		#endregion

		#region Internal Properties

		/// <summary>
		/// Get or set the <see cref="IDbDataAdapter"/> instance.
		/// </summary>
		internal IDbDataAdapter DataAdapter
		{
			get
			{
				return adapter;
			}

			set
			{
				if (adapter != value)
				{
					Dispose(true);
					adapter = value;
					if (adapter != null)
					{
						if (adapter is SharpHsqlDataAdapter)
						{
							sqlhandler = new SharpHsqlRowUpdatingEventHandler(this.SqlRowUpdating);
							#if !POCKETPC
							((SharpHsqlDataAdapter)adapter).RowUpdating += sqlhandler;
							#endif
							namedParameters = true;
							return;
						}
					}
				}
			}
		}

		/// <summary>
		/// Get or set the quote prefix to use.
		/// </summary>
		internal string QuotePrefix
		{
			get
			{
				if (quotePrefix == null)
				{
					return String.Empty;
				}
				else
				{
					return quotePrefix;
				}
			}

			set
			{
				if (dbSchemaTable != null)
				{
					throw new InvalidOperationException("No Quote Change Allowed with an existing Schema.");
				}
				quotePrefix = value;
			}
		}

		/// <summary>
		/// Get or set the quote suffix to use.
		/// </summary>
		internal string QuoteSuffix
		{
			get
			{
				if (quoteSuffix == null)
				{
					return String.Empty;
				}
				else
				{
					return quoteSuffix;
				}
			}

			set
			{
				if (dbSchemaTable != null)
				{
					throw new InvalidOperationException("No Quote Change Allowed with an existing Schema.");
				}
				quoteSuffix = value;
			}
		}

		/// <summary>
		/// Get the source command.
		/// </summary>
		internal IDbCommand SourceCommand
		{
			get
			{
				if (adapter != null)
				{
					return adapter.SelectCommand;
				}
				else
				{
					return null;
				}
			}
		}

		#endregion

		#region Private Properties & Methods

		private string QuotedBaseTableName
		{
			get
			{
				return quotedBaseTableName;
			}
		}

		private IDbCommand GetXxxCommand(IDbCommand cmd)
		{
			return cmd;
		}

		private bool IsBehavior(CommandBuilderBehavior behavior)
		{
			return behavior == (options & behavior);
		}

		private bool IsNotBehavior(CommandBuilderBehavior behavior)
		{
			return behavior == (options & behavior) == false;
		}

		private string QuotedColumn(string column)
		{
			return String.Concat(QuotePrefix, column, QuoteSuffix);
		}

		private void ClearHandlers()
		{
			#if !POCKETPC
			if (sqlhandler != null)
			{
				((SharpHsqlDataAdapter)adapter).RowUpdating -= sqlhandler;
				sqlhandler = null;
				return;
			}
			#endif
		}

		private void ClearState()
		{
			dbSchemaTable = null;
			dbSchemaRows = null;
			sourceColumnNames = null;
			quotedBaseTableName = null;
		}

		private void SqlRowUpdating(object sender, SharpHsqlRowUpdatingEventArgs ruevent)
		{
			RowUpdating(sender, ruevent);
		}

		private void RowUpdating(object sender, RowUpdatingEventArgs ruevent)
		{
			if (ruevent == null)
			{
				return;
			}
			if (ruevent.Command != null)
			{
				StatementType statementType = ruevent.StatementType;
				switch (statementType)
				{
					case StatementType.Insert:
						if (insertCommand != ruevent.Command)
						{
							return;
						}
						break;

					case StatementType.Update:
						if (updateCommand != ruevent.Command)
						{
							return;
						}
						break;

					case StatementType.Delete:
						if (deleteCommand != ruevent.Command)
						{
							return;
						}
						break;

					default:
						return;
				}
			}
			try
			{
				BuildCache(false);
				StatementType statementType = ruevent.StatementType;
				switch (statementType)
				{
					case StatementType.Insert:
						ruevent.Command = BuildInsertCommand(ruevent.TableMapping, ruevent.Row);
						break;

					case StatementType.Update:
						ruevent.Command = BuildUpdateCommand(ruevent.TableMapping, ruevent.Row);
						break;

					case StatementType.Delete:
						ruevent.Command = BuildDeleteCommand(ruevent.TableMapping, ruevent.Row);
						break;
				}
				if (ruevent.Command == null)
				{
					if (ruevent.Row != null)
					{
						ruevent.Row.AcceptChanges();
					}
					ruevent.Status = UpdateStatus.SkipCurrentRow;
				}
			}
			catch (Exception e)
			{
				ruevent.Errors = e;
				ruevent.Status = UpdateStatus.ErrorsOccurred;
			}
		}

		private void BuildCache(bool closeConnection)
		{
			IDbCommand iDbCommand = SourceCommand;
			if (iDbCommand == null)
			{
				throw new InvalidOperationException("Missing Source Command");
			}
			IDbConnection iDbConnection = iDbCommand.Connection;
			if (iDbConnection == null)
			{
				throw new InvalidOperationException("Missing Source Command Connection");
			}
			if (DataAdapter != null)
			{
				missingMapping = DataAdapter.MissingMappingAction;
				if (MissingMappingAction.Passthrough != missingMapping)
				{
					missingMapping = MissingMappingAction.Error;
				}
			}
			if (dbSchemaTable == null)
			{
				if ((ConnectionState.Open & iDbConnection.State) == 0)
				{
					iDbConnection.Open();
				}
				else
				{
					closeConnection = false;
				}
				try
				{
					DataTable dataTable = null;
					IDataReader iDataReader = iDbCommand.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly);
					try
					{
						dataTable = iDataReader.GetSchemaTable();
					}
					finally
					{
						if (iDataReader != null)
						{
							iDataReader.Dispose();
						}
					}
					if (dataTable == null)
					{
						throw new InvalidOperationException("Dynamic SQL doesn't have Table Information.");
					}
					BuildInformation(dataTable);
					dbSchemaTable = dataTable;
					int i = (int)dbSchemaRows.Length;
					sourceColumnNames = new string[(uint)i];
					for (int j = 0; j < i; j++)
					{
						if (dbSchemaRows[j] != null)
						{
							sourceColumnNames[j] = dbSchemaRows[j]["ColumnName"].ToString();
						}
					}
					BuildSchemaTableInfoTableNames(sourceColumnNames);
				}
				catch
				{
					throw;
				}
				finally
				{
					if (closeConnection)
					{
						iDbConnection.Close();
					}
				}
			}
		}

		private static int GenerateUniqueName(Hashtable hash, ref string columnName, int index, int uniqueIndex)
		{
			while (true)
			{
				string cname = columnName + uniqueIndex.ToString();
				string name = cname.ToLower(CultureInfo.InvariantCulture);
				if (!hash.Contains(name))
				{
					columnName = cname;
					hash.Add(name, index);
					return uniqueIndex;
				}
				uniqueIndex++;
			}
		}


		private void BuildInformation(DataTable schemaTable)
		{
			DataRow[] dBSchemaRows = GetSortedSchemaRows(schemaTable);
			if (dBSchemaRows == null || (int)dBSchemaRows.Length == 0)
			{
				throw new InvalidOperationException("Dynamic SQL No Table Info");
			}
			string svr = string.Empty;
			string cat = string.Empty;
			string sch = string.Empty;
			string tbl = null;
			for (int i = 0; i < (int)dBSchemaRows.Length; i++)
			{
				DataRow dBSchemaRow = dBSchemaRows[i];
				string table = dBSchemaRow["BaseTableName"].ToString();
				if (table == null || table.Length == 0)
				{
					dBSchemaRows[i] = null;
				}
				else
				{
					string server = dBSchemaRow["BaseServerName"].ToString();
					string catalog = dBSchemaRow["BaseCatalogName"].ToString();
					string schema = dBSchemaRow["BaseSchemaName"].ToString();
					if (server == null)
						server = string.Empty;
					if (catalog == null)
						catalog = string.Empty;
					if (schema == null)
						schema = string.Empty;
					if (tbl == null)
					{
						svr = server;
						cat = catalog;
						sch = schema;
						tbl = table;
					}
					else if (SrcCompare(tbl, table) != 0 || SrcCompare(sch, schema) != 0 || SrcCompare(cat, catalog) != 0 || SrcCompare(svr, server) != 0)
					{
						throw new InvalidOperationException("Dynamic SQL Join Unsupported");
					}
				}
			}
			if (svr.Length == 0)
				svr = null;
			if (cat.Length == 0)
			{
				svr = null;
				cat = null;
			}
			if (sch.Length == 0)
			{
				svr = null;
				cat = null;
				sch = null;
			}
			if (tbl == null || tbl.Length == 0)
				throw new InvalidOperationException("Dynamic SQL No Table Info");

			if (!IsEmpty(quotePrefix) && -1 != tbl.IndexOf(quotePrefix))
				throw new InvalidOperationException("Dynamic SQL Nested Quote");

			if (!IsEmpty(quoteSuffix) && -1 != tbl.IndexOf(quoteSuffix))
				throw new InvalidOperationException("Dynamic SQL Nested Quote");

			StringBuilder stringBuilder = new StringBuilder();
			if (svr != null)
			{
				stringBuilder.Append(QuotePrefix);
				stringBuilder.Append(svr);
				stringBuilder.Append(QuoteSuffix);
				stringBuilder.Append(".");
			}
			if (cat != null)
			{
				stringBuilder.Append(QuotePrefix);
				stringBuilder.Append(cat);
				stringBuilder.Append(QuoteSuffix);
				stringBuilder.Append(".");
			}
			if (sch != null)
			{
				stringBuilder.Append(QuotePrefix);
				stringBuilder.Append(sch);
				stringBuilder.Append(QuoteSuffix);
				stringBuilder.Append(".");
			}
			stringBuilder.Append(QuotePrefix);
			stringBuilder.Append(tbl);
			stringBuilder.Append(QuoteSuffix);
			quotedBaseTableName = stringBuilder.ToString();
			dbSchemaRows = dBSchemaRows;
		}

		private IDbCommand BuildNewCommand(IDbCommand cmd)
		{
			IDbCommand iDbCommand = SourceCommand;
			if (cmd == null)
			{
				cmd = iDbCommand.Connection.CreateCommand();
				cmd.CommandTimeout = iDbCommand.CommandTimeout;
				cmd.Transaction = iDbCommand.Transaction;
			}
			cmd.CommandType = CommandType.Text;
			cmd.UpdatedRowSource = UpdateRowSource.None;
			return cmd;
		}

		private void ApplyParameterInfo(SharpHsqlParameter parameter, int pcount, DataRow row)
		{
#if POCKETPC
				parameter.DbType = (DbType)OpenNETCF.EnumEx.Parse( typeof(DbType), row["ProviderType"].ToString() );
#else
			parameter.DbType = (DbType)Enum.Parse(typeof(DbType), row["ProviderType"].ToString() );
#endif

			parameter.IsNullable = (bool)row["AllowDBNull"];
			if ((byte)row["Precision"] != byte.MaxValue)
			{
				parameter.Precision = (byte)row["Precision"];
			}
			if ((byte)row["Scale"] != byte.MaxValue)
			{
				parameter.Scale = (byte)row["Scale"];
			}
			parameter.Size = 0;
		}

		private IDbCommand BuildInsertCommand(DataTableMapping mappings, DataRow dataRow)
		{
			if (IsEmpty(this.quotedBaseTableName))
			{
				return null;
			}
			IDbCommand command = this.BuildNewCommand(this.insertCommand);
			int pcount = 0;
			int idx = 1;
			StringBuilder sb = new StringBuilder();
			sb.Append("INSERT INTO ");
			sb.Append(this.QuotedBaseTableName);
			int rows = this.dbSchemaRows.Length;
			for (int i = 0; i < rows; i++)
			{
				DataRow row = this.dbSchemaRows[i];
				if ((((row == null) || (row["BaseColumnName"].ToString().Length == 0)) || ((bool)row["IsAutoIncrement"] || (bool)row["IsHidden"])) || ((bool)row["IsExpression"] || (bool)row["IsRowVersion"]))
				{
					continue;
				}
				object value = null;
				string name = this.sourceColumnNames[i];
				if ((mappings != null) && (dataRow != null))
				{
					value = this.GetParameterInsertValue(name, mappings, dataRow, (bool)row["IsReadOnly"]);
					if (value == null)
					{
						if (!(bool)row["IsReadOnly"] && !(command is SharpHsqlCommand))
						{
							goto Close;
						}
						continue;
					}
					if (Convert.IsDBNull(value) && !(bool)row["AllowDBNull"])
					{
						continue;
					}
				}
			Close:
				if (pcount == 0)
				{
					sb.Append("( ");
				}
				else
				{
					sb.Append(" , ");
				}
				sb.Append(this.QuotedColumn(row["BaseColumnName"].ToString()));
				IDataParameter parameter = CommandBuilder.GetNextParameter(command, pcount);
				parameter.ParameterName = "@p" + idx.ToString();
				parameter.Direction = ParameterDirection.Input;
				parameter.SourceColumn = name;
				parameter.SourceVersion = DataRowVersion.Current;
				parameter.Value = value;
				if (parameter is SharpHsqlParameter)
				{
					this.ApplyParameterInfo((SharpHsqlParameter) parameter, idx, row);
				}
				if (!command.Parameters.Contains(parameter))
				{
					command.Parameters.Add(parameter);
				}
				pcount++;
				idx++;
			}
			if (pcount == 0)
			{
				sb.Append(" DEFAULT VALUES");
			}
			else if (this.namedParameters)
			{
				sb.Append(" ) VALUES ( @p1");
				for (int num5 = 2; num5 <= pcount; num5++)
				{
					sb.Append(" , @p");
					sb.Append(num5.ToString());
				}
				sb.Append(" )");
			}
			else
			{
				sb.Append(" ) VALUES ( ?");
				for (int num6 = 2; num6 <= pcount; num6++)
				{
					sb.Append(" , ?");
				}
				sb.Append(" )");
			}
			command.CommandText = sb.ToString();
			CommandBuilder.RemoveExtraParameters(command, pcount);
			this.insertCommand = command;
			return command;
		}
 

		private IDbCommand BuildUpdateCommand(DataTableMapping mappings, DataRow dataRow)
		{
			if (IsEmpty(this.quotedBaseTableName))
			{
				return null;
			}
			IDbCommand command = this.BuildNewCommand(this.updateCommand);
			int pcount = 1;
			int toupdate = 0;
			int index = 0;
			int filter = 0;
			StringBuilder sb = new StringBuilder();
			sb.Append("UPDATE ");
			sb.Append(this.QuotedBaseTableName);
			sb.Append(" SET ");
			int rows = this.dbSchemaRows.Length;
			for (int i = 0; i < rows; i++)
			{
				DataRow row = this.dbSchemaRows[i];
				if (((row != null) && (row["BaseColumnName"].ToString().Length != 0)) && !this.ExcludeFromUpdateSet(row))
				{
					toupdate++;
					object value = null;
					string name = this.sourceColumnNames[i];
					if ((mappings != null) && (dataRow != null))
					{
						value = this.GetParameterUpdateValue(name, mappings, dataRow, (bool)row["IsReadOnly"]);
						if (value == null)
						{
							if ((bool)row["IsReadOnly"])
								toupdate--;

							continue;
						}
					}
					if (0 < index)
					{
						sb.Append(" , ");
					}
					sb.Append(this.QuotedColumn(row["BaseColumnName"].ToString()));
					this.AppendParameterText(sb, pcount);
					IDataParameter parameter = CommandBuilder.GetNextParameter(command, index);
					parameter.ParameterName = "@p" + pcount.ToString();
					parameter.Direction = ParameterDirection.Input;
					parameter.SourceColumn = name;
					parameter.SourceVersion = DataRowVersion.Current;
					parameter.Value = value;
					if (parameter is SharpHsqlParameter)
					{
						this.ApplyParameterInfo((SharpHsqlParameter) parameter, pcount, row);
					}
					if (!command.Parameters.Contains(parameter))
					{
						command.Parameters.Add(parameter);
					}
					pcount++;
					index++;
				}
			}
			filter = index;
			sb.Append(" WHERE ( ");
			string sql = "";
			int where = 0;
			string tname = null;
			string colname = null;
			for (int i = 0; i < rows; i++)
			{
				DataRow row = this.dbSchemaRows[i];
				if (((row != null) && (row["BaseColumnName"].ToString().Length != 0)) && this.IncludeForUpdateWhereClause(row))
				{
					sb.Append(sql);
					sql = " AND ";
					object value = null;
					string name = this.sourceColumnNames[i];
					if ((mappings != null) && (dataRow != null))
					{
						value = this.GetParameterValue(name, mappings, dataRow, DataRowVersion.Original);
					}
					bool ispk = this.IsPKey(row);
					string basecol = this.QuotedColumn(row["BaseColumnName"].ToString());
					if (ispk)
					{
						if (Convert.IsDBNull(value))
						{
							sb.Append(string.Format("({0} IS NULL)", basecol));
						}
						else if (this.namedParameters)
						{
							sb.Append(string.Format("({0} = @p{1})", basecol, pcount));
						}
						else
						{
							sb.Append(string.Format("({0} = ?)", basecol));
						}
					}
					else if (this.namedParameters)
					{
						sb.Append(string.Format("((@p{1} = 1 AND {0} IS NULL) OR ({0} = @p{2}))", basecol, pcount, 1 + pcount));
					}
					else
					{
						sb.Append(string.Format("((? = 1 AND {0} IS NULL) OR ({0} = ?))", basecol));
					}
					if (!ispk || !Convert.IsDBNull(value))
					{
						IDataParameter parameter = CommandBuilder.GetNextParameter(command, index);
						parameter.ParameterName = "@p" + pcount.ToString();
						parameter.Direction = ParameterDirection.Input;
						if (ispk)
						{
							parameter.SourceColumn = name;
							parameter.SourceVersion = DataRowVersion.Original;
							parameter.Value = value;
						}
						else
						{
							parameter.SourceColumn = null;
							parameter.Value = IsNull(value) ? 1 : 0;
						}
						pcount++;
						index++;
						if (parameter is SharpHsqlParameter)
						{
							this.ApplyParameterInfo((SharpHsqlParameter) parameter, pcount, row);
						}
						if (!ispk)
						{
							parameter.DbType = DbType.Int32;
						}
						if (!command.Parameters.Contains(parameter))
						{
							command.Parameters.Add(parameter);
						}
					}
					if (!ispk)
					{
						IDataParameter parameter = CommandBuilder.GetNextParameter(command, index);
						parameter.ParameterName = "@p" + pcount.ToString();
						parameter.Direction = ParameterDirection.Input;
						parameter.SourceColumn = name;
						parameter.SourceVersion = DataRowVersion.Original;
						parameter.Value = value;
						pcount++;
						index++;
						if (parameter is SharpHsqlParameter)
						{
							this.ApplyParameterInfo((SharpHsqlParameter) parameter, pcount, row);
						}
						if (!command.Parameters.Contains(parameter))
						{
							command.Parameters.Add(parameter);
						}
					}
					if (this.IncrementUpdateWhereCount(row))
					{
						where++;
					}
				}
			}
			sb.Append(" )");
			command.CommandText = sb.ToString();
			CommandBuilder.RemoveExtraParameters(command, index);
			this.updateCommand = command;
			if (toupdate == 0)
			{
				throw new InvalidOperationException("Dynamic SQL is read only.");
			}
			if (filter == 0)
			{
				command = null;
			}
			if (where == 0)
			{
				throw new InvalidOperationException("Dynamic SQL has no primary key information to perform an update.");
			}
			if (tname != null)
			{
				DataColumn col = this.GetParameterDataColumn(colname, mappings, dataRow);
				throw new InvalidOperationException("Where Clause Unspecified Value");
			}
			return command;
		}

		private IDbCommand BuildDeleteCommand(DataTableMapping mappings, DataRow dataRow)
		{
			if (IsEmpty(quotedBaseTableName))
			{
				return null;
			}
			IDbCommand command = BuildNewCommand(deleteCommand);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("DELETE FROM  ");
			stringBuilder.Append(QuotedBaseTableName);
			stringBuilder.Append(" WHERE ( ");
			int pcount = 1;
			int pindex = 0;
			int index = 0;
			string sql = "";
			string tname = null;
			string cname = null;
			int count = (int)dbSchemaRows.Length;
			for (int i = 0; i < count; i++)
			{
				DataRow dBSchemaRow = dbSchemaRows[i];
				if (dBSchemaRow != null && dBSchemaRow["BaseColumnName"].ToString().Length != 0 && IncludeForDeleteWhereClause(dBSchemaRow))
				{
					stringBuilder.Append(sql);
					sql = " AND ";
					object value = null;
					string name = sourceColumnNames[i];
					if (mappings != null && dataRow != null)
					{
						value = GetParameterValue(name, mappings, dataRow, DataRowVersion.Original);
					}
					bool ispk = IsPKey(dBSchemaRow);
					string qcolumn = QuotedColumn(dBSchemaRow["BaseColumnName"].ToString());
					if (ispk)
					{
						if (Convert.IsDBNull(value))
						{
							stringBuilder.Append(String.Format("({0} IS NULL)", qcolumn));
						}
						else if (namedParameters)
						{
							stringBuilder.Append(String.Format("({0} = @p{1})", qcolumn, pcount));
						}
						else
						{
							stringBuilder.Append(String.Format("({0} = ?)", qcolumn));
						}
					}
					else if (namedParameters)
					{
						stringBuilder.Append(String.Format("((@p{1} = 1 AND {0} IS NULL) OR ({0} = @p{2}))", qcolumn, pcount, (1 + pcount)));
					}
					else
					{
						stringBuilder.Append(String.Format("((? = 1 AND {0} IS NULL) OR ({0} = ?))", qcolumn));
					}
					if (!ispk || !Convert.IsDBNull(value))
					{
						IDataParameter parameter = GetNextParameter(command, pindex);
						parameter.ParameterName = String.Concat("@p", pcount.ToString());
						parameter.Direction = ParameterDirection.Input;
						if (ispk)
						{
							parameter.SourceColumn = name;
							parameter.SourceVersion = DataRowVersion.Original;
							parameter.Value = value;
						}
						else
						{
							parameter.SourceColumn = null;
							parameter.Value = (!IsNull(value) ? 0 : 1);
						}
						pcount++;
						pindex++;
						if (parameter is SharpHsqlParameter)
						{
							ApplyParameterInfo((SharpHsqlParameter)parameter, pcount, dBSchemaRow);
						}
						if (!ispk)
						{
							parameter.DbType = DbType.Int32;
						}
						if (!command.Parameters.Contains(parameter))
						{
							command.Parameters.Add(parameter);
						}
					}
					if (!ispk)
					{
						IDataParameter parameter = GetNextParameter(command, pindex);
						parameter.ParameterName = String.Concat("@p", pcount.ToString());
						parameter.Direction = ParameterDirection.Input;
						parameter.SourceColumn = name;
						parameter.SourceVersion = DataRowVersion.Original;
						parameter.Value = value;
						pcount++;
						pindex++;
						if (parameter is SharpHsqlParameter)
						{
							ApplyParameterInfo((SharpHsqlParameter)parameter, pcount, dBSchemaRow);
						}
						if (!command.Parameters.Contains(parameter))
						{
							command.Parameters.Add(parameter);
						}
					}
					if (IncrementDeleteWhereCount(dBSchemaRow))
					{
						index++;
					}
				}
			}
			stringBuilder.Append(" )");
			command.CommandText = stringBuilder.ToString();
			RemoveExtraParameters(command, pindex);
			deleteCommand = command;
			if (index == 0)
			{
				throw new InvalidOperationException("Dynamic SQL has not indexey information to Delete.");
			}
			if (tname == null)
			{
				return command;
			}
			DataColumn dataColumn = GetParameterDataColumn(cname, mappings, dataRow);
			throw new InvalidOperationException("Where Clause Unspecified Value");
		}

		private static IDataParameter GetNextParameter(IDbCommand cmd, int pcount)
		{
			if (pcount < cmd.Parameters.Count)
			{
				return (IDataParameter)cmd.Parameters[pcount];
			}
			else
			{
				return cmd.CreateParameter();
			}
		}

		private static void RemoveExtraParameters(IDbCommand cmd, int pcount)
		{
			while (pcount < cmd.Parameters.Count)
			{
				cmd.Parameters.RemoveAt(cmd.Parameters.Count - 1);
			}
		}

		private bool ExcludeFromUpdateSet(DataRow row)
		{
			if (!(bool)row["IsAutoIncrement"] && !(bool)row["IsRowVersion"])
			{
				return (bool)row["IsHidden"];
			}
			else
			{
				return true;
			}
		}

		private bool IncludeForUpdateWhereClause(DataRow row)
		{
			if (IsBehavior(CommandBuilderBehavior.UseRowVersionInUpdateWhereClause))
			{
				if (((bool)row["IsRowVersion"] || (bool)row["IsKey"] || (bool)row["IsUnique"]) && !(bool)row["IsLong"])
				{
					return (bool)row["IsHidden"] == false;
				}
				else
				{
					return false;
				}
			}
			if ((IsNotBehavior(CommandBuilderBehavior.PrimaryKeyOnlyUpdateWhereClause) || (bool)row["IsKey"] || (bool)row["IsUnique"]) && !(bool)row["IsLong"] && ((bool)row["IsKey"] || !(bool)row["IsRowVersion"]))
			{
				return (bool)row["IsHidden"] == false;
			}
			else
			{
				return false;
			}
		}

		private bool IncludeForDeleteWhereClause(DataRow row)
		{
			if (IsBehavior(CommandBuilderBehavior.UseRowVersionInDeleteWhereClause))
			{
				if (((bool)row["IsRowVersion"] || (bool)row["IsKey"] || (bool)row["IsUnique"]) && !(bool)row["IsLong"])
				{
					return (bool)row["IsHidden"] == false;
				}
				else
				{
					return false;
				}
			}
			if ((IsNotBehavior(CommandBuilderBehavior.PrimaryKeyOnlyDeleteWhereClause) || (bool)row["IsKey"] || (bool)row["IsUnique"]) && !(bool)row["IsLong"] && ((bool)row["IsKey"] || !(bool)row["IsRowVersion"]))
			{
				return (bool)row["IsHidden"] == false;
			}
			else
			{
				return false;
			}
		}

		private bool IsPKey(DataRow row)
		{
			return (bool)row["IsKey"];
		}

		private bool IncrementUpdateWhereCount(DataRow row)
		{
			if (!(bool)row["IsKey"])
			{
				return (bool)row["IsUnique"];
			}
			else
			{
				return true;
			}
		}

		private bool IncrementDeleteWhereCount(DataRow row)
		{
			if (!(bool)row["IsKey"])
			{
				return (bool)row["IsUnique"];
			}
			else
			{
				return true;
			}
		}

		private DataColumn GetParameterDataColumn(string columnName, DataTableMapping mappings, DataRow row)
		{
			if (!IsEmpty(columnName))
			{
				DataColumnMapping dataColumnMapping = mappings.GetColumnMappingBySchemaAction(columnName, missingMapping);
				if (dataColumnMapping != null)
				{
					return dataColumnMapping.GetDataColumnBySchemaAction(row.Table, null, MissingSchemaAction.Error);
				}
			}
			return null;
		}

		private object GetParameterValue(string columnName, DataTableMapping mappings, DataRow row, DataRowVersion version)
		{
			DataColumn dataColumn = GetParameterDataColumn(columnName, mappings, row);
			if (dataColumn != null)
			{
				return row[dataColumn, version];
			}
			else
			{
				return null;
			}
		}

		private object GetParameterInsertValue(string columnName, DataTableMapping mappings, DataRow row, bool readOnly)
		{
			DataColumn dataColumn = GetParameterDataColumn(columnName, mappings, row);
			if (dataColumn == null)
			{
				return null;
			}
			if (readOnly && dataColumn.ReadOnly)
			{
				return null;
			}
			else
			{
				return row[dataColumn, DataRowVersion.Current];
			}
		}

		private object GetParameterUpdateValue(string columnName, DataTableMapping mappings, DataRow row, bool readOnly)
		{
			DataColumn dataColumn = GetParameterDataColumn(columnName, mappings, row);
			if (dataColumn != null)
			{
				if (readOnly && dataColumn.ReadOnly)
				{
					return null;
				}
				object local1 = row[dataColumn, DataRowVersion.Current];
				if (!IsNotBehavior(CommandBuilderBehavior.UpdateSetSameValue))
				{
					return local1;
				}
				object local2 = row[dataColumn, DataRowVersion.Original];
				if (local2 != local1 && (local2 == null || !local2.Equals(local1)))
				{
					return local1;
				}
			}
			return null;
		}

		private void AppendParameterText(StringBuilder builder, int pcount)
		{
			if (namedParameters)
			{
				builder.Append(" = @p");
				builder.Append(pcount.ToString());
				return;
			}
			builder.Append(" = ?");
		}

		#endregion

		#region Dispose Methods

		/// <summary>
		/// Dispose method.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				ClearHandlers();
				RefreshSchema();
				adapter = null;
			}
		}

		#endregion

		#region Internal Methods
		
		/// <summary>
		/// Get a command for Insert.
		/// </summary>
		/// <returns></returns>
		internal IDbCommand GetInsertCommand()
		{
			BuildCache(true);
			return GetXxxCommand(BuildInsertCommand(null, null));
		}

		/// <summary>
		/// Get a command for Update.
		/// </summary>
		/// <returns></returns>
		internal IDbCommand GetUpdateCommand()
		{
			BuildCache(true);
			return GetXxxCommand(BuildUpdateCommand(null, null));
		}

		/// <summary>
		/// Get a command for Delete.
		/// </summary>
		/// <returns></returns>
		internal IDbCommand GetDeleteCommand()
		{
			BuildCache(true);
			return GetXxxCommand(BuildDeleteCommand(null, null));
		}

		/// <summary>
		/// Refresh database schema 
		/// </summary>
		internal void RefreshSchema()
		{
			ClearState();
			if (adapter != null)
			{
				if (insertCommand == adapter.InsertCommand)
				{
					adapter.InsertCommand = null;
				}
				if (updateCommand == adapter.UpdateCommand)
				{
					adapter.UpdateCommand = null;
				}
				if (deleteCommand == adapter.DeleteCommand)
				{
					adapter.DeleteCommand = null;
				}
			}
			if (insertCommand != null)
			{
				insertCommand.Dispose();
			}
			if (updateCommand != null)
			{
				updateCommand.Dispose();
			}
			if (deleteCommand != null)
			{
				deleteCommand.Dispose();
			}
			insertCommand = null;
			updateCommand = null;
			deleteCommand = null;
		}

		/// <summary>
		/// Build the schema information table.
		/// </summary>
		/// <param name="columnNameArray"></param>
		internal static void BuildSchemaTableInfoTableNames(string[] columnNameArray)
		{
			int count = columnNameArray.Length;
			Hashtable columns = new Hashtable(count);
			int min = count;
			for (int i = count - 1; 0 <= i; i--)
			{
				string name = columnNameArray[i];
				if ((name != null) && (0 < name.Length))
				{
					name = name.ToLower(CultureInfo.InvariantCulture);
					if (columns.Contains(name))
					{
						min = Math.Min(min, (int) columns[name]);
					}
					columns[name] = i;
				}
				else
				{
					columnNameArray[i] = "";
					min = i;
				}
			}
			int index = 1;
			for (int i = min; i < count; i++)
			{
				string name = columnNameArray[i];
				if (name.Length == 0)
				{
					columnNameArray[i] = "Column";
					index = GenerateUniqueName(columns, ref columnNameArray[i], i, index);
				}
				else
				{
					name = name.ToLower(CultureInfo.InvariantCulture);
					if (i != ((int) columns[name]))
					{
						GenerateUniqueName(columns, ref columnNameArray[i], i, 1);
					}
				}
			}
		}
 
		/// <summary>
		/// Get the schema row.
		/// </summary>
		/// <param name="dataTable"></param>
		/// <returns></returns>
		internal static DataRow[] GetSortedSchemaRows(DataTable dataTable)
		{
			DataColumn column = new DataColumn("SchemaMapping Unsorted Index", typeof(int));
			dataTable.Columns.Add(column);
			int count = dataTable.Rows.Count;
			for (int i = 0; i < count; i++)
			{
				dataTable.Rows[i][column] = i;
			}
			return dataTable.Select(null, "ColumnOrdinal ASC", DataViewRowState.CurrentRows);
		}

		/// <summary>
		/// Helper routine to compare two strings case sensitive and using the current culture.
		/// </summary>
		/// <param name="strA"></param>
		/// <param name="strB"></param>
		/// <returns></returns>
		internal static int SrcCompare(string strA, string strB)
		{
			return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.None);
		}

		/// <summary>
		/// Helper routine to compare two strings case insensitive and using the current culture
		/// </summary>
		/// <param name="strA"></param>
		/// <param name="strB"></param>
		/// <returns></returns>
		internal static int DstCompare(string strA, string strB)
		{
			return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreCase );
		}

		/// <summary>
		/// Helper routine used for empty strings comparison.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		internal static bool IsEmpty(string str)
		{
			if (str != null)
			{
				return 0 == str.Length;
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// Helper routine used for empty string arrays comparison.
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		internal static bool IsEmpty(string[] array)
		{
			if (array != null)
			{
				return 0 == (int)array.Length;
			}
			else
			{
				return true;
			}
		}

		#if !POCKETPC
		/// <summary>
		/// Internal delegate.
		/// </summary>
		/// <param name="mcd"></param>
		/// <returns></returns>
		internal static Delegate FindBuilder(MulticastDelegate mcd)
		{
			if (null != mcd)
			{
				Delegate[] delegates = mcd.GetInvocationList();
				for (int i = 0; i < (int)delegates.Length; i++)
				{
					if (delegates[i].Target is CommandBuilder)
					{
						return delegates[i];
					}
				}
			}
			return null;
		}
		#endif

		/// <summary>
		/// Helper routine for nullable objects comparison.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		internal static bool IsNull(object value)
		{
			if (value == null || Convert.IsDBNull(value))
			{
				return true;
			}
			if (value is INullable)
			{
				return ((INullable)value).IsNull;
			}
			else
			{
				return false;
			}
		}

		#endregion 

		#region Private Vars

		private const string WhereClause1 = "((@p{1} = 1 AND {0} IS NULL) OR ({0} = @p{2}))";
		private const string WhereClause2 = "((? = 1 AND {0} IS NULL) OR ({0} = ?))";
		private const string WhereClause1p = "({0} = @p{1})";
		private const string WhereClause2p = "({0} = ?)";
		private const string WhereClausepn = "({0} IS NULL)";
		private const string AndClause = " AND ";
		private const MissingSchemaAction missingSchema = MissingSchemaAction.Error;
		private IDbDataAdapter adapter;
		private MissingMappingAction missingMapping;
		private CommandBuilderBehavior options = CommandBuilderBehavior.Default;
		private SharpHsqlRowUpdatingEventHandler sqlhandler;
		private DataTable dbSchemaTable;
		private DataRow[] dbSchemaRows;
		private string[] sourceColumnNames;
		private string quotePrefix;
		private string quoteSuffix;
		private bool namedParameters;
		private string quotedBaseTableName;
		private IDbCommand insertCommand;
		private IDbCommand updateCommand;
		private IDbCommand deleteCommand;

		#endregion
	}
}
