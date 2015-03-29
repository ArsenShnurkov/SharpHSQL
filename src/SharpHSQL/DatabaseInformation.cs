#region Usings
using System;
using System.Collections;
using System.Text;
#endregion

#region License
/*
 * DatabaseInformation.cs
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
	/// DatabaseInformation class.
	/// </summary>
	sealed class DatabaseInformation 
	{
		private Database	dDatabase;
		private Access		aAccess;
		private ArrayList   tTable;

		public DatabaseInformation(Database db, ArrayList tables, Access access) 
		{
			dDatabase = db;
			tTable = tables;
			aAccess = access;
		}

		// some drivers use the following titles:
		// static string META_SCHEM="OWNER";
		// static string META_CAT="QUALIFIER";
		// static string META_COLUMN_SIZE="PRECISION";
		// static string META_BUFFER_LENGTH="LENGTH";
		// static string META_DECIMAL_DIGITS="SCALE";
		// static string META_NUM_PREC_RADIX="RADIX";
		// static string META_FIXED_PREC_SCALE="MONEY";
		// static string META_ORDINAL_POSITON="SEQ_IN_INDEX";
		// static string META_ASC_OR_DESC="COLLATION";
		static string META_SCHEM = "SCHEM";
		static string META_CAT = "CAT";
		static string META_COLUMN_SIZE = "COLUMN_SIZE";
		static string META_BUFFER_LENGTH = "BUFFER_LENGTH";
		static string META_DECIMAL_DIGITS = "DECIMAL_DIGITS";
		static string META_NUM_PREC_RADIX = "NUM_PREC_RADIX";
		static string META_FIXED_PREC_SCALE = "FIXED_PREC_SCALE";
		static string META_ORDINAL_POSITON = "ORDINAL_POSITON";
		static string META_ASC_OR_DESC = "ASC_OR_DESC";

		public Table GetSystemTable(string name, Channel channel) 
		{
			if (name.Equals("SYSTEM_PROCEDURES")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("PROCEDURE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("PROCEDURE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("PROCEDURE_NAME", ColumnType.VarChar);
				t.AddColumn("NUM_INPUT_PARAMS", ColumnType.Integer);
				t.AddColumn("NUM_OUTPUT_PARAMS", ColumnType.Integer);
				t.AddColumn("NUM_RESULT_SETS", ColumnType.Integer);
				t.AddColumn("REMARKS", ColumnType.VarChar);
				t.AddColumn("PROCEDURE_TYPE", ColumnType.SmallInt);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_PROCEDURECOLUMNS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("PROCEDURE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("PROCEDURE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("PROCEDURE_NAME", ColumnType.VarChar);
				t.AddColumn("COLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("COLUMN_TYPE", ColumnType.SmallInt);
				t.AddColumn("DATA_TYPE", ColumnType.SmallInt);
				t.AddColumn("TYPE_NAME", ColumnType.VarChar);
				t.AddColumn("PRECISION", ColumnType.Integer);
				t.AddColumn("LENGTH", ColumnType.Integer);
				t.AddColumn("SCALE", ColumnType.SmallInt);
				t.AddColumn("RADIX", ColumnType.SmallInt);
				t.AddColumn("NULLABLE", ColumnType.SmallInt);
				t.AddColumn("REMARKS", ColumnType.VarChar);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_TABLES")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("TABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("TABLE_NAME", ColumnType.VarChar);
				t.AddColumn("TABLE_TYPE", ColumnType.VarChar);
				t.AddColumn("REMARKS", ColumnType.VarChar);
				t.CreatePrimaryKey();

				for (int i = 0; i < tTable.Count; i++) 
				{
					Table  table = (Table) tTable[i];
					object[] o = t.NewRow;

					o[2] = table.Name;
					o[3] = "TABLE";

					t.Insert(o, null);
				}

				return t;
			} 
			else if (name.Equals("SYSTEM_SCHEMAS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TABLE_" + META_SCHEM, ColumnType.VarChar);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_CATALOGS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TABLE_" + META_CAT, ColumnType.VarChar);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_TABLETYPES")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TABLE_TYPE", ColumnType.VarChar);
				t.CreatePrimaryKey();

				object[] o = t.NewRow;

				o[0] = "TABLE";

				t.Insert(o, null);

				return t;
			} 
			else if (name.Equals("SYSTEM_COLUMNS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("TABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("TABLE_NAME", ColumnType.VarChar);
				t.AddColumn("COLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("DATA_TYPE", ColumnType.SmallInt);
				t.AddColumn("TYPE_NAME", ColumnType.VarChar);
				t.AddColumn(META_COLUMN_SIZE, ColumnType.Integer);
				t.AddColumn(META_BUFFER_LENGTH, ColumnType.Integer);
				t.AddColumn(META_DECIMAL_DIGITS, ColumnType.Integer);
				t.AddColumn(META_NUM_PREC_RADIX, ColumnType.Integer);
				t.AddColumn("NULLABLE", ColumnType.Integer);
				t.AddColumn("REMARKS", ColumnType.VarChar);

				// Access and Intersolv do not return this fields
				t.AddColumn("COLUMN_DEF", ColumnType.VarChar);
				t.AddColumn("SQL_DATA_TYPE", ColumnType.VarChar);
				t.AddColumn("SQL_DATETIME_SUB", ColumnType.Integer);
				t.AddColumn("CHAR_OCTET_LENGTH", ColumnType.Integer);
				t.AddColumn("ORDINAL_POSITION", ColumnType.VarChar);
				t.AddColumn("IS_NULLABLE", ColumnType.VarChar);
				t.CreatePrimaryKey();

				for (int i = 0; i < tTable.Count; i++) 
				{
					Table table = (Table) tTable[i];
					int   columns = table.ColumnCount;

					for (int j = 0; j < columns; j++) 
					{
						object[] o = t.NewRow;

						o[2] = table.Name;
						o[3] = table.GetColumnName(j);
						o[4] = table.GetColumnType(j);
						o[5] = Column.GetColumnTypeString(table.GetColumnType(j));

						int nullable;

						if (table.GetColumnIsNullable(j)) 
						{
							nullable = Convert.ToInt32(true);
						} 
						else 
						{
							nullable = Convert.ToInt32(false);
						}

						o[10] = nullable;

						if (table.IdentityColumn == j) 
						{
							o[11] = "IDENTITY";
						}

						t.Insert(o, null);
					}
				}

				return t;
			} 
			else if (name.Equals("SYSTEM_COLUMNPRIVILEGES")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("TABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("TABLE_NAME", ColumnType.VarChar);
				t.AddColumn("COLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("GRANTOR", ColumnType.VarChar);
				t.AddColumn("GRANTEE", ColumnType.VarChar);
				t.AddColumn("PRIVILEGE", ColumnType.VarChar);
				t.AddColumn("IS_GRANTABLE", ColumnType.VarChar);
				t.CreatePrimaryKey();

				/*
				 * // todo: get correct info
				 * for(int i=0;i<tTable.size();i++) {
				 * Table table=(Table)tTable.elementAt(i);
				 * int columns=table.ColumnCount;
				 * for(int j=0;j<columns;j++) {
				 * object o[]=t.NewRow;
				 * o[2]=table.Name;
				 * o[3]=table.getColumnName(j);
				 * o[4]="sa";
				 * o[6]="FULL";
				 * o[7]="NO";
				 * t.insert(o,null);
				 * }
				 * }
				 */
				return t;
			} 
			else if (name.Equals("SYSTEM_TABLEPRIVILEGES")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("TABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("TABLE_NAME", ColumnType.VarChar);
				t.AddColumn("GRANTOR", ColumnType.VarChar);
				t.AddColumn("GRANTEE", ColumnType.VarChar);
				t.AddColumn("PRIVILEGE", ColumnType.VarChar);
				t.AddColumn("IS_GRANTABLE", ColumnType.VarChar);
				t.CreatePrimaryKey();

				for (int i = 0; i < tTable.Count; i++) 
				{
					Table  table = (Table) tTable[i];
					object[] o = t.NewRow;

					o[2] = table.Name;
					o[3] = "sa";
					o[5] = "FULL";

					t.Insert(o, null);
				}

				return t;
			} 
			else if (name.Equals("SYSTEM_BESTROWIDENTIFIER")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("SCOPE", ColumnType.SmallInt);
				t.AddColumn("COLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("DATA_TYPE", ColumnType.SmallInt);
				t.AddColumn("TYPE_NAME", ColumnType.VarChar);
				t.AddColumn(META_COLUMN_SIZE, ColumnType.Integer);
				t.AddColumn(META_BUFFER_LENGTH, ColumnType.Integer);
				t.AddColumn(META_DECIMAL_DIGITS, ColumnType.SmallInt);
				t.AddColumn("PSEUDO_COLUMN", ColumnType.SmallInt);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_VERSIONCOLUMNS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("SCOPE", ColumnType.Integer);
				t.AddColumn("COLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("DATA_TYPE", ColumnType.SmallInt);
				t.AddColumn("TYPE_NAME", ColumnType.VarChar);
				t.AddColumn(META_COLUMN_SIZE, ColumnType.SmallInt);
				t.AddColumn(META_BUFFER_LENGTH, ColumnType.Integer);
				t.AddColumn(META_DECIMAL_DIGITS, ColumnType.SmallInt);
				t.AddColumn("PSEUDO_COLUMN", ColumnType.SmallInt);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_PRIMARYKEYS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("TABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("TABLE_NAME", ColumnType.VarChar);
				t.AddColumn("COLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("KEY_SEQ", ColumnType.SmallInt);
				t.AddColumn("PK_NAME", ColumnType.VarChar);
				t.CreatePrimaryKey();

				for (int i = 0; i < tTable.Count; i++) 
				{
					Table table = (Table) tTable[i];
					Index index = table.GetIndex("SYSTEM_PK");
					int[]   cols = index.Columns;
					int   len = cols.Length;

					for (int j = 0; j < len; j++) 
					{
						object[] o = t.NewRow;

						o[2] = table.Name;
						o[3] = table.GetColumnName(cols[j]);
						o[4] = j + 1;
						o[5] = "SYSTEM_PK";

						t.Insert(o, null);
					}
				}

				return t;
			} 
			else if (name.Equals("SYSTEM_IMPORTEDKEYS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("PKTABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("PKTABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("PKTABLE_NAME", ColumnType.VarChar);
				t.AddColumn("PKCOLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("FKTABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("FKTABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("FKTABLE_NAME", ColumnType.VarChar);
				t.AddColumn("FKCOLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("KEY_SEQ", ColumnType.SmallInt);
				t.AddColumn("UPDATE_RULE", ColumnType.SmallInt);
				t.AddColumn("DELETE_RULE", ColumnType.SmallInt);
				t.AddColumn("FK_NAME", ColumnType.VarChar);
				t.AddColumn("PK_NAME", ColumnType.VarChar);
				t.AddColumn("DEFERRABILITY", ColumnType.SmallInt);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_EXPORTEDKEYS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("PKTABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("PKTABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("PKTABLE_NAME", ColumnType.VarChar);
				t.AddColumn("PKCOLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("FKTABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("FKTABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("FKTABLE_NAME", ColumnType.VarChar);
				t.AddColumn("FKCOLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("KEY_SEQ", ColumnType.SmallInt);
				t.AddColumn("UPDATE_RULE", ColumnType.SmallInt);
				t.AddColumn("DELETE_RULE", ColumnType.SmallInt);
				t.AddColumn("FK_NAME", ColumnType.VarChar);
				t.AddColumn("PK_NAME", ColumnType.VarChar);
				t.AddColumn("DEFERRABILITY", ColumnType.SmallInt);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_CROSSREFERENCE")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("PKTABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("PKTABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("PKTABLE_NAME", ColumnType.VarChar);
				t.AddColumn("PKCOLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("FKTABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("FKTABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("FKTABLE_NAME", ColumnType.VarChar);
				t.AddColumn("FKCOLUMN_NAME", ColumnType.VarChar);
				t.AddColumn("KEY_SEQ", ColumnType.Integer);
				t.AddColumn("UPDATE_RULE", ColumnType.SmallInt);
				t.AddColumn("DELETE_RULE", ColumnType.SmallInt);
				t.AddColumn("FK_NAME", ColumnType.VarChar);
				t.AddColumn("PK_NAME", ColumnType.VarChar);
				t.AddColumn("DEFERRABILITY", ColumnType.SmallInt);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_TYPEINFO")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TYPE_NAME", ColumnType.VarChar);
				t.AddColumn("DATA_TYPE", ColumnType.SmallInt);
				t.AddColumn("PRECISION", ColumnType.Integer);
				t.AddColumn("LITERAL_PREFIX", ColumnType.VarChar);
				t.AddColumn("LITERAL_SUFFIX", ColumnType.VarChar);
				t.AddColumn("CREATE_PARAMS", ColumnType.VarChar);
				t.AddColumn("NULLABLE", ColumnType.SmallInt);
				t.AddColumn("CASE_SENSITIVE", ColumnType.VarChar);
				t.AddColumn("SEARCHABLE", ColumnType.SmallInt);
				t.AddColumn("UNSIGNED_ATTRIBUTE", ColumnType.Bit);
				t.AddColumn(META_FIXED_PREC_SCALE, ColumnType.Bit);
				t.AddColumn("AUTO_INCREMENT", ColumnType.Bit);
				t.AddColumn("LOCAL_TYPE_NAME", ColumnType.VarChar);
				t.AddColumn("MINIMUM_SCALE", ColumnType.SmallInt);
				t.AddColumn("MAXIMUM_SCALE", ColumnType.SmallInt);

				// this columns are not supported by Access and Intersolv
				t.AddColumn("SQL_DATE_TYPE", ColumnType.Integer);
				t.AddColumn("SQL_DATETIME_SUB", ColumnType.Integer);
				t.AddColumn("NUM_PREC_RADIX", ColumnType.Integer);
				t.CreatePrimaryKey();

				for (int i = 0; i < Column.Types.Length; i++) 
				{
					object[] o = t.NewRow;
					ColumnType    type = Column.Types[i];

					o[0] = Column.GetColumnTypeString(type);
					o[1] = type;
					o[2] = 0;		 // precision
					o[6] = true; // need Column to track nullable for this
					o[7] = true;	 // case sensitive
					o[8] = true;;
					o[9] = false;       // unsigned
					o[10] = (type == ColumnType.Numeric	|| type == ColumnType.DbDecimal);
					o[11] = (type == ColumnType.Integer);
					o[12] = o[0];
					o[13] = 0;
					o[14] = 0;    // maximum scale
					o[15] = 0;
					o[16] = o[15];
					o[17] = 10;

					t.Insert(o, null);
				}

				return t;
			} 
			else if (name.Equals("SYSTEM_INDEXINFO")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TABLE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("TABLE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("TABLE_NAME", ColumnType.VarChar);
				t.AddColumn("NON_UNIQUE", ColumnType.Bit);
				t.AddColumn("INDEX_QUALIFIER", ColumnType.VarChar);
				t.AddColumn("INDEX_NAME", ColumnType.VarChar);
				t.AddColumn("TYPE", ColumnType.SmallInt);
				t.AddColumn(META_ORDINAL_POSITON, ColumnType.SmallInt);
				t.AddColumn("COLUMN_NAME", ColumnType.VarChar);
				t.AddColumn(META_ASC_OR_DESC, ColumnType.VarChar);
				t.AddColumn("CARDINALITY", ColumnType.Integer);
				t.AddColumn("PAGES", ColumnType.Integer);
				t.AddColumn("FILTER_CONDITION", ColumnType.VarChar);
				t.CreatePrimaryKey();

				for (int i = 0; i < tTable.Count; i++) 
				{
					Table table = (Table) tTable[i];
					Index index = null;

					while (true) 
					{
						index = table.GetNextIndex(index);

						if (index == null) 
						{
							break;
						}

						int[] cols = index.Columns;
						int len = cols.Length;

						// this removes the column that makes every index unique
						if (!index.IsUnique) 
						{
							len--;
						}

						for (int j = 0; j < len; j++) 
						{
							object[] o = t.NewRow;

							o[2] = table.Name;
							o[3] = !index.IsUnique;
							o[5] = index.Name;
							o[6] = 1;
							o[7] = (j + 1);
							o[8] = table.GetColumnName(cols[j]);
							o[9] = "A";

							t.Insert(o, null);
						}
					}
				}

				return t;
			} 
			else if (name.Equals("SYSTEM_UDTS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("TYPE_" + META_CAT, ColumnType.VarChar);
				t.AddColumn("TYPE_" + META_SCHEM, ColumnType.VarChar);
				t.AddColumn("TYPE_NAME", ColumnType.VarChar);
				t.AddColumn("CLASS_NAME", ColumnType.Bit);
				t.AddColumn("DATA_TYPE", ColumnType.VarChar);
				t.AddColumn("REMARKS", ColumnType.VarChar);
				t.CreatePrimaryKey();

				return t;
			} 
			else if (name.Equals("SYSTEM_CONNECTIONINFO")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("KEY", ColumnType.VarChar);
				t.AddColumn("VALUE", ColumnType.VarChar);
				t.CreatePrimaryKey();

				object[] o = t.NewRow;

				o[0] = "USER";
				o[1] = channel.UserName;

				t.Insert(o, null);

				o = t.NewRow;
				o[0] = "READONLY";
				o[1] = channel.IsReadOnly ? "TRUE" : "FALSE";

				t.Insert(o, null);

				o = t.NewRow;
				o[0] = "MAXROWS";
				o[1] = "" + channel.MaxRows;

				t.Insert(o, null);

				o = t.NewRow;
				o[0] = "DATABASE";
				o[1] = "" + channel.Database.Name;

				t.Insert(o, null);

				o = t.NewRow;
				o[0] = "IDENTITY";
				o[1] = "" + channel.LastIdentity;

				t.Insert(o, null);

				return t;
			} 
			else if (name.Equals("SYSTEM_USERS")) 
			{
				Table t = CreateTable(name);

				t.AddColumn("USER", ColumnType.VarChar);
				t.AddColumn("ADMIN", ColumnType.Bit);
				t.CreatePrimaryKey();

				ArrayList v = aAccess.GetUsers();

				for (int i = 0; i < v.Count; i++) 
				{
					User u = (User) v[i];

					// todo: this is not a nice implementation
					if (u == null) 
					{
						continue;
					}

					string user = u.Name;

					if (!user.Equals("PUBLIC")) 
					{
						object[] o = t.NewRow;

						o[0] = user;
						o[1] = u.IsAdmin;

						t.Insert(o, null);
					}
				}

				return t;
			}

			return null;
		}

		public Result GetScript(bool bDrop, bool bInsert, bool bCached, Channel channel) 
		{
			channel.CheckAdmin();

			Result r = new Result(1);

			r.Type[0] = ColumnType.VarChar;
			r.Table[0] = "SYSTEM_SCRIPT";
			r.Label[0] = "COMMAND";
			r.Name[0] = "COMMAND";

			StringBuilder a = new StringBuilder();

			for (int i = 0; i < tTable.Count; i++) 
			{
				Table t = (Table) tTable[i];

				if (bDrop) 
				{
					AddRow(r, "DROP TABLE \"" + t.Name + "\"");
				}

				a.Remove(0,a.Length);
				a.Append("CREATE ");

				if (t.IsCached) 
				{
					a.Append("CACHED ");
				}

				a.Append("TABLE ");
				a.Append('"');
				a.Append(t.Name);
				a.Append('"');
				a.Append("(");

				int   columns = t.ColumnCount;
				Index pki = t.GetIndex("SYSTEM_PK");
				int   pk = (pki == null) ? -1 : pki.Columns[0];

				for (int j = 0; j < columns; j++) 
				{
					a.Append('"');
					a.Append(t.GetColumnName(j));
					a.Append('"');
					a.Append(" ");
					a.Append(Column.GetColumnTypeString(t.GetType(j)));

					if (!t.GetColumnIsNullable(j)) 
					{
						a.Append(" NOT NULL");
					}

					if (j == t.IdentityColumn) 
					{
						a.Append(" IDENTITY");
					}

					if (j == pk) 
					{
						a.Append(" PRIMARY KEY");
					}

					if (j < columns - 1) 
					{
						a.Append(",");
					}
				}

				ArrayList v = t.Constraints;

				for (int j = 0; j < v.Count; j++) 
				{
					Constraint c = (Constraint) v[j];

					if (c.ConstraintType == ConstraintType.ForeignKey) 
					{
						a.Append(",FOREIGN KEY");

						int[] col = c.RefTableColumns;

						a.Append(GetColumnList(c.RefTable, col, col.Length));
						a.Append("REFERENCES ");
						a.Append(c.MainTable.Name);

						col = c.MainTableColumns;

						a.Append(GetColumnList(c.MainTable, col, col.Length));
					} 
					else if (c.ConstraintType == ConstraintType.Unique) 
					{
						a.Append(",UNIQUE");

						int[] col = c.MainTableColumns;

						a.Append(GetColumnList(c.MainTable, col, col.Length));
					}
				}

				a.Append(")");
				AddRow(r, a.ToString());

				Index index = null;

				while (true) 
				{
					index = t.GetNextIndex(index);

					if (index == null) 
					{
						break;
					}

					string indexname = index.Name;

					if (indexname.Equals("SYSTEM_PK")) 
					{
						continue;
					} 
					else if (indexname.StartsWith("SYSTEM_FOREIGN_KEY")) 
					{

						// foreign keys where created in the 'create table'
						continue;
					} 
					else if (indexname.StartsWith("SYSTEM_CONSTRAINT")) 
					{

						// constraints where created in the 'create table'
						continue;
					}

					a.Remove(0,a.Length);
					a.Append("CREATE ");

					if (index.IsUnique) 
					{
						a.Append("UNIQUE ");
					}

					a.Append("INDEX ");
					a.Append(indexname);
					a.Append(" ON ");
					a.Append(t.Name);

					int[] col = index.Columns;
					int len = col.Length;

					if (!index.IsUnique) 
					{
						len--;
					}

					a.Append(GetColumnList(t, col, len));
					AddRow(r, a.ToString());
				}

				if (bInsert) 
				{
					Index   primary = t.PrimaryIndex;
					Node    x = primary.First();
					bool integrity = true;

					if (x != null) 
					{
						integrity = false;

						AddRow(r, "SET REFERENTIAL_INTEGRITY FALSE");
					}

					while (x != null) 
					{
						AddRow(r, t.GetInsertStatement(x.GetData()));

						x = primary.Next(x);
					}

					if (!integrity) 
					{
						AddRow(r, "SET REFERENTIAL_INTEGRITY TRUE");
					}
				}

				if (bCached && t.IsCached) 
				{
					a.Remove(0,a.Length);
					a.Append("SET TABLE ");

					a.Append('"');
					a.Append(t.Name);
					a.Append('"');
					a.Append(" INDEX '");
					a.Append(t.IndexRoots);
					a.Append("'");
					AddRow(r, a.ToString());
				}

			}

			ArrayList uList = aAccess.GetUsers();

			for (int i = 0; i < uList.Count; i++) 
			{
				User u = (User) uList[i];

				// todo: this is not a nice implementation
				if (u == null) 
				{
					continue;
				}

				string name = u.Name;

				if (!name.Equals("PUBLIC")) 
				{
					a.Remove(0,a.Length);
					a.Append("CREATE USER ");

					a.Append(name);
					a.Append(" PASSWORD ");
					a.Append("\"" + u.Password + "\"");

					if (u.IsAdmin) 
					{
						a.Append(" ADMIN");
					}

					AddRow(r, a.ToString());
				}

				Hashtable rights = u.Rights;

				if (rights == null) 
				{
					continue;
				}

				foreach( string dbObject in rights.Keys)
				{
					AccessType    right = (AccessType) rights[dbObject];

					if (right == AccessType.None) 
					{
						continue;
					}

					a.Remove(0,a.Length);
					a.Append("GRANT ");

					a.Append(Access.GetRight(right));
					a.Append(" ON ");
					a.Append(dbObject);
					a.Append(" TO ");
					a.Append(u.Name);
					AddRow(r, a.ToString());
				}
			}

			if (dDatabase.IsIgnoreCase) 
			{
				AddRow(r, "SET IGNORECASE TRUE");
			}

			Hashtable   h = dDatabase.Alias;

			foreach(string alias in h.Keys)
			{
				string className = (string) h[alias];
				AddRow(r, "CREATE ALIAS " + alias + " FOR \"" + className + "\"");
			}

			return r;
		}

		private string GetColumnList(Table table, int[] col, int len) 
		{
			StringBuilder a = new StringBuilder();
			a.Append("(");

			for (int i = 0; i < len; i++) 
			{
				a.Append('"');
				a.Append(table.GetColumnName(col[i]));
				a.Append('"');

				if (i < len - 1) 
				{
					a.Append(",");
				}
			}

			return a.Append(")").ToString();
		}

		private void AddRow(Result result, string sql) 
		{
			string[] s = new string[1];

			s[0] = sql;

			result.Add(s);
		}

		private Table CreateTable(string name) 
		{
			return new Table(dDatabase, false, name, false);
		}
	}
}
