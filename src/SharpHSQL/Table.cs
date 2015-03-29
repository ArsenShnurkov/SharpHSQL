#region Usings
using System;
using System.Text;
using System.Collections;
#endregion

#region License
/*
 * Table.cs
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
	/// Table class.
	/// </summary>
	sealed class Table 
	{
		private string		sName;
		private ArrayList   vColumn;
		private ArrayList   vIndex;				// vIndex(0) is always the primary key index
		private int			iVisibleColumns;    // table may contain a hidden primary key
		private int			iColumnCount;		// inclusive the (maybe hidden) primary key
		private int			iPrimaryKey;
		private bool		bCached;
		private Database	dDatabase;
		private Log			lLog;
		private int			iIndexCount;
		private int			iIdentityColumn;    // -1 means no such row
		private int			iTimestampColumn;    // -1 means no such row
		private int			iIdentityId = 1;
		private ArrayList   vConstraint;
		private int			iConstraintCount;
		internal Cache	    cCache;

		public Table(Database db, bool log, string name, bool cached) 
		{
			dDatabase = db;
			lLog = log ? db.Log : null;

			if (cached) 
			{
				cCache = lLog.cCache;
				bCached = true;
			}

			sName = name;
			iPrimaryKey = -1;
			iIdentityColumn = -1;
			iTimestampColumn = -1;
			vColumn = new ArrayList();
			vIndex = new ArrayList();
			vConstraint = new ArrayList();
		}

		public void AddConstraint(Constraint c) 
		{
			vConstraint.Add(c);

			iConstraintCount++;
		}

		public ArrayList Constraints
		{
			get
			{
				return vConstraint;
			}
		}

		public void AddColumn(string name, ColumnType type) 
		{
			AddColumn(name, type, true, false);
		}

		public void AddColumn(Column c) 
		{
			AddColumn(c.Name, c.ColumnType, c.IsNullable, c.IsIdentity);
		}

		public void AddColumn(string name, ColumnType type, bool nullable, bool identity) 
		{
			if (identity) 
			{
				Trace.Check(type == ColumnType.Integer, Trace.WRONG_DATA_TYPE, name);
				Trace.Check(iIdentityColumn == -1, Trace.SECOND_PRIMARY_KEY,
					name);

				iIdentityColumn = iColumnCount;
			}

			if( type == ColumnType.Timestamp )
			{
				iTimestampColumn = iColumnCount;
			}

			Trace.Assert(iPrimaryKey == -1, "Table.addColumn");
			vColumn.Add(new Column(name, nullable, type, identity));

			iColumnCount++;
		}

		public void AddColumns(Result result) 
		{
			for (int i = 0; i < result.ColumnCount; i++) 
			{
				AddColumn(result.Label[i], result.Type[i], true, false);
			}
		}

		public string Name
		{
			get
			{
				return sName;
			}
		}

		public int InternalColumnCount
		{
			get
			{
				// todo: this is a temporary solution;
				// the the hidden column is not really required
				return iColumnCount;
			}
		}

		public Table MoveDefinition(string withoutIndex) 
		{
			Table tn = new Table(dDatabase, true, Name, IsCached);

			for (int i = 0; i < ColumnCount; i++) 
			{
				tn.AddColumn(GetColumn(i));
			}

			// todo: there should be nothing special with the primary key!
			if (iVisibleColumns < iColumnCount) 
			{
				tn.CreatePrimaryKey();
			} 
			else 
			{
				tn.CreatePrimaryKey(PrimaryIndex.Columns[0]);
			}

			Index idx = null;

			while (true) 
			{
				idx = GetNextIndex(idx);

				if (idx == null) 
				{
					break;
				}

				if (withoutIndex != null && idx.Name.Equals(withoutIndex)) 
				{
					continue;
				}

				if (idx == PrimaryIndex) 
				{
					continue;
				}

				tn.CreateIndex(idx);
			}

			for (int i = 0; i < iConstraintCount; i++) 
			{
				Constraint c = (Constraint) vConstraint[i];

				c.ReplaceTable(this, tn);
			}

			tn.vConstraint = vConstraint;

			return tn;
		}

		public int ColumnCount
		{
			get
			{
				return iVisibleColumns;
			}
		}

		public int IndexCount
		{
			get
			{
				return iIndexCount;
			}
		}

		public int IdentityColumn
		{
			get
			{
				return iIdentityColumn;
			}
		}

		public int TimestampColumn
		{
			get
			{
				return iTimestampColumn;
			}
		}

		public int GetColumnNumber(string c) 
		{
			int i = SearchColumn(c);

			if (i == -1) 
			{
				throw Trace.Error(Trace.COLUMN_NOT_FOUND, c);
			}

			return i;
		}

		public int SearchColumn(string c) 
		{
			for (int i = 0; i < iColumnCount; i++) 
			{
				if (c.ToLower().Equals(((Column) vColumn[i]).Name.ToLower())) 
				{
					return i;
				}
			}

			return -1;
		}

		public string GetColumnName(int i) 
		{
			return GetColumn(i).Name;
		}

		public ColumnType GetColumnType(int i) 
		{
			return GetColumn(i).ColumnType;
		}

		public bool GetColumnIsNullable(int i) 
		{
			return GetColumn(i).IsNullable;
		}

		public Index PrimaryIndex
		{
			get
			{
				if (iPrimaryKey == -1)
				{
					return null;
				}

				return GetIndex(0);
			}
		}

		public Index GetIndexForColumn(int column) 
		{
			for (int i = 0; i < iIndexCount; i++) 
			{
				Index h = GetIndex(i);

				if (h.Columns[0] == column) 
				{
					return h;
				}
			}

			return null;
		}

		public Index GetIndexForColumns(int[] col) 
		{
			for (int i = 0; i < iIndexCount; i++) 
			{
				Index h = GetIndex(i);
				int[]   icol = h.Columns;
				int   j = 0;

				for (; j < col.Length; j++) 
				{
					if (j >= icol.Length) 
					{
						break;
					}

					if (icol[j] != col[j]) 
					{
						break;
					}
				}

				if (j == col.Length) 
				{
					return h;
				}
			}

			return null;
		}

		public string IndexRoots
		{
			get
			{
				Trace.Assert(bCached, "Table.getIndexRootData");

				string s = "";

				for (int i = 0; i < iIndexCount; i++)
				{
					Node f = GetIndex(i).Root;

					if (f != null)
					{
						s = s + f.GetKey() + " ";
					}
					else
					{
						s = s + "-1 ";
					}
				}

				s += iIdentityId;

				return s;
			}
			set
			{
				// the user may try to set this; this is not only internal problem
				Trace.Check(bCached, Trace.TABLE_NOT_FOUND);

				int j = 0;

				for (int i = 0; i < iIndexCount; i++)
				{
					int n = value.IndexOf(' ', j);
					int p = int.Parse(value.Substring(j, (n - j)));

					if (p != -1)
					{
						Row r = cCache.GetRow(p, this);
						Node f = r.GetNode(i);

						GetIndex(i).Root = f;
					}

					j = n + 1;
				}

				iIdentityId = int.Parse(value.Substring(j));
			}
		}

		public Index GetNextIndex(Index index) 
		{
			int i = 0;

			if (index != null) 
			{
				for (; i < iIndexCount && GetIndex(i) != index; i++);

				i++;
			}

			if (i < iIndexCount) 
			{
				return GetIndex(i);
			}

			return null;    // no more indexes
		}

		public ColumnType GetType(int i) 
		{
			return GetColumn(i).ColumnType;
		}

		public void CreatePrimaryKey(int column) 
		{
			Trace.Assert(iPrimaryKey == -1, "Table.createPrimaryKey(column)");

			iVisibleColumns = iColumnCount;
			iPrimaryKey = column;

			int[] col = new int[1];
			col[0] = column;

			CreateIndex(col, "SYSTEM_PK", true);
		}

		public void CreatePrimaryKey() 
		{
			Trace.Assert(iPrimaryKey == -1, "Table.createPrimaryKey");
			AddColumn("SYSTEM_ID", ColumnType.Integer, true, true);
			CreatePrimaryKey(iColumnCount - 1);

			iVisibleColumns = iColumnCount - 1;
		}

		public void CreateIndex(Index index) 
		{
			CreateIndex(index.Columns, index.Name, index.IsUnique);
		}

		public void CreateIndex(int[] column, string name, bool unique) 
		{
			Trace.Assert(iPrimaryKey != -1, "createIndex");

			for (int i = 0; i < iIndexCount; i++) 
			{
				Index index = GetIndex(i);

				if (name.Equals(index.Name)) 
				{
					throw Trace.Error(Trace.INDEX_ALREADY_EXISTS);
				}
			}

			int s = column.Length;

			// The primary key field is added for non-unique indexes
			// making all indexes unique
			int[] col = new int[unique ? s : s + 1];
			ColumnType[] type = new ColumnType[unique ? s : s + 1];

			for (int j = 0; j < s; j++) 
			{
				col[j] = column[j];
				type[j] = GetColumn(col[j]).ColumnType;
			}

			if (!unique) 
			{
				col[s] = iPrimaryKey;
				type[s] = GetColumn(iPrimaryKey).ColumnType;
			}

			Index newindex = new Index(name, col, type, unique);

			if (iIndexCount != 0) 
			{
				Trace.Assert(IsEmpty, "createIndex");
			}

			vIndex.Add(newindex);

			iIndexCount++;
		}

		public void CheckDropIndex(string index) 
		{
			for (int i = 0; i < iIndexCount; i++) 
			{
				if (index.Equals(GetIndex(i).Name)) 
				{
					Trace.Check(i != 0, Trace.DROP_PRIMARY_KEY);

					return;
				}
			}

			throw Trace.Error(Trace.INDEX_NOT_FOUND, index);
		}

		public bool IsEmpty
		{
			get
			{
				return GetIndex(0).Root == null;
			}
		}

		public object[] NewRow
		{
			get
			{
				return new object[iColumnCount];
			}
		}

		public void MoveData(Table from) 
		{
			Index index = from.PrimaryIndex;
			Node  n = index.First();

			while (n != null) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				object[] o = n.GetData();

				InsertNoCheck(o, null);

				n = index.Next(n);
			}

			index = PrimaryIndex;
			n = index.First();

			while (n != null) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				object[] o = n.GetData();

				from.DeleteNoCheck(o, null);

				n = index.Next(n);
			}
		}

		public void CheckUpdate(int[] col, Result deleted, Result inserted) 
		{
			if (dDatabase.IsReferentialIntegrity) 
			{
				for (int i = 0; i < iConstraintCount; i++) 
				{
					Constraint v = (Constraint) vConstraint[i];

					v.CheckUpdate(col, deleted, inserted);
				}
			}
		}

		public void Insert(Result result, Channel c) 
		{
			// if violation of constraints can occur, insert must be rolled back
			// outside of this function!
			Record r = result.Root;
			int    len = result.ColumnCount;

			while (r != null) 
			{
				object[] row = NewRow;

				for (int i = 0; i < len; i++) 
				{
					row[i] = r.Data[i];
				}

				Insert(row, c);

				r = r.Next;
			}
		}

		public void Insert(object[] row, Channel channel) 
		{
			if (dDatabase.IsReferentialIntegrity) 
			{
				for (int i = 0; i < iConstraintCount; i++) 
				{
					((Constraint) vConstraint[i]).CheckInsert(row);
				}
			}

			InsertNoCheck(row, channel);
		}

		public void InsertNoCheck(object[] row, Channel channel) 
		{
			InsertNoCheck(row, channel, true);
		}

		public void InsertNoCheck(object[] row, Channel c, bool log) 
		{
			int i;

			if (iIdentityColumn != -1) 
			{
				if (row[iIdentityColumn] == null)
				{
					if (c != null) 
					{
						c.LastIdentity = iIdentityId;
					}

					row[iIdentityColumn] = iIdentityId++;
				} 
				else 
				{
					i = (int) row[iIdentityColumn];

					if (iIdentityId <= i) 
					{
						if (c != null) 
						{
							c.LastIdentity = i;
						}

						iIdentityId = i + 1;
					}
				}
			}

			if (iTimestampColumn != -1) 
			{
				if (row[iTimestampColumn] == null)
				{
					row[iTimestampColumn] = DateTime.Now;
				} 
				else 
				{
					DateTime timestamp = DateTime.Now;
					DateTime original = (DateTime) row[iTimestampColumn];

					// just in case to assure our timestamp is unique
					if ( timestamp == original ) 
					{
						row[iTimestampColumn] = timestamp.AddMilliseconds(1);
					}
					else
					{
						row[iTimestampColumn] = timestamp;
					}
				}
			}

			for (i = 0; i < iColumnCount; i++) 
			{
				if (row[i] == null &&!GetColumn(i).IsNullable) 
				{
					throw Trace.Error(Trace.TRY_TO_INSERT_NULL);
				}
			}

			try 
			{
				Row r = new Row(this, row);

				for (i = 0; i < iIndexCount; i++) 
				{
					Node n = r.GetNode(i);

					GetIndex(i).Insert(n);
				}
			} 
			catch (Exception e) 
			{    // rollback insert
				for (--i; i >= 0; i--) 
				{
					GetIndex(i).Delete(row, i == 0);
				}

				throw e;		      // and throw error again
			}

			if (c != null) 
			{
				c.AddTransactionInsert(this, row);
			}

			if (lLog != null) 
			{
				lLog.Write(c, GetInsertStatement(row));
			}
		}

		public void Delete(object[] row, Channel c) 
		{
			if (dDatabase.IsReferentialIntegrity) 
			{
				for (int i = 0; i < iConstraintCount; i++) 
				{
					((Constraint) vConstraint[i]).CheckDelete(row);
				}
			}

			DeleteNoCheck(row, c);

		}

		public void DeleteNoCheck(object[] row, Channel channel) 
		{
			DeleteNoCheck(row, channel, true);
		}

		public void DeleteNoCheck(object[] row, Channel channel, bool log) 
		{
			for (int i = 1; i < iIndexCount; i++) 
			{
				GetIndex(i).Delete(row, false);
			}

			// must delete data last
			GetIndex(0).Delete(row, true);

			if (channel != null) 
			{
				channel.AddTransactionDelete(this, row);
			}

			if (lLog != null) 
			{
				lLog.Write(channel, GetDeleteStatement(row));
			}
		}

		public string GetInsertStatement(object[] row) 
		{
			StringBuilder a = new StringBuilder();
			a.Append("INSERT INTO \"");

			a.Append(Name);
			a.Append("\" VALUES(");

			for (int i = 0; i < iVisibleColumns; i++) 
			{
				if( i>0 )
					a.Append(',');

				a.Append(Column.CreateString(row[i], GetColumn(i).ColumnType));
			}
			a.Append(')');

			return a.ToString();
		}

		public bool IsCached
		{
			get
			{
				return bCached;
			}
		}

		public Index GetIndex(string s) 
		{
			for (int i = 0; i < iIndexCount; i++) 
			{
				Index h = GetIndex(i);

				if (s.Equals(h.Name)) 
				{
					return h;
				}
			}

			// no such index
			return null;
		}

		public Column GetColumn(int i) 
		{
			return (Column) vColumn[i];
		}

		private Index GetIndex(int i) 
		{
			return (Index) vIndex[i];
		}

		private string GetDeleteStatement(object[] row) 
		{
			StringBuilder a = new StringBuilder();
			a.Append("DELETE FROM \"");

			a.Append(sName);
			a.Append("\" WHERE ");

			if (iVisibleColumns < iColumnCount) 
			{
				for (int i = 0; i < iVisibleColumns; i++) 
				{
					a.Append('"');
					a.Append(GetColumn(i).Name);
					a.Append('"');
					a.Append('=');
					a.Append(Column.CreateString(row[i], GetColumn(i).ColumnType));

					if (i < iVisibleColumns - 1) 
					{
						a.Append(" AND ");
					}
				}
			} 
			else 
			{
				a.Append('"');
				a.Append(GetColumn(iPrimaryKey).Name);
				a.Append('"');
				a.Append("=");
				a.Append(Column.CreateString(row[iPrimaryKey],
					GetColumn(iPrimaryKey).ColumnType));
			}

			return a.ToString();
		}
	}
}
