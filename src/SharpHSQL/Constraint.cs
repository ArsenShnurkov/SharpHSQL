#region Usings
using System;
using System.Collections;
#endregion

#region License
/*
 * Constraint.cs
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
	/// Class representing a database constraint.
	/// <seealso cref="Table"/>
	/// </summary>
	/// <remarks>version 1.0.0.1</remarks>
	sealed class Constraint 
	{
		#region Private Vars

		private ConstraintType      _type;
		private int					_len;

		// Main is the table that is referenced

		private Table     _mainTable;
		private int[]     _mainColumns;
		private Index     _mainIndex;
		private object[]  _mainData;

		// Ref is the table that has a reference to the main table

		private Table     _refTable;
		private int[]     _refColumns;
		private Index     _refIndex;
		private object[]  _refData;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor using only the Main Table.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="table"></param>
		/// <param name="col"></param>
		public Constraint(ConstraintType type, Table table, int[] col) 
		{
			_type = type;
			_mainTable = table;
			_mainColumns = col;
			_len = col.Length;
		}

		/// <summary>
		/// Constructor using Main and Reference Table.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="main"></param>
		/// <param name="child"></param>
		/// <param name="columnMain"></param>
		/// <param name="columnRef"></param>
		public Constraint(ConstraintType type, Table main, Table child, int[] columnMain, int[] columnRef) 
		{
			_type = type;
			_mainTable = main;
			_refTable = child;
			_mainColumns = columnMain;
			_refColumns = columnRef;
			_len = columnMain.Length;

			if (Trace.AssertEnabled) 
			{
				Trace.Assert(columnMain.Length == columnRef.Length);
			}

			_mainData = _mainTable.NewRow;
			_refData = _refTable.NewRow;
			_mainIndex = _mainTable.GetIndexForColumns(columnMain);
			_refIndex = _refTable.GetIndexForColumns(columnRef);
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets the constraint type.
		/// </summary>
		public ConstraintType ConstraintType
		{
			get
			{
				return _type;
			}
		}

		/// <summary>
		/// Gets the main table.
		/// </summary>
		public Table MainTable
		{
			get
			{
				return _mainTable;
			}
		}

		/// <summary>
		/// Gets the reference table.
		/// </summary>
		public Table RefTable
		{
			get
			{
				return _refTable;
			}
		}

		/// <summary>
		/// Gets the main table columns.
		/// </summary>
		public int[] MainTableColumns
		{
			get
			{
				return _mainColumns;
			}
		}

		/// <summary>
		/// Gets the referenced table columns.
		/// </summary>
		public int[] RefTableColumns
		{
			get
			{
				return _refColumns;
			}
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Replaces the main or reference table with a new one.
		/// </summary>
		/// <param name="oldTable"></param>
		/// <param name="newTable"></param>
		internal void ReplaceTable(Table oldTable, Table newTable) 
		{
			if (oldTable == _mainTable) 
			{
				_mainTable = newTable;
			} 
			else if (oldTable == _refTable) 
			{
				_refTable = newTable;
			} 
			else 
			{
				Trace.Assert(false, "could not replace");
			}
		}

		/// <summary>
		/// Verify if the insert operation can be performed.
		/// </summary>
		/// <param name="row"></param>
		internal void CheckInsert(object[] row) 
		{
			if (_type == ConstraintType.Main || _type == ConstraintType.Unique) 
			{

				// inserts in the main table are never a problem
				// unique constraints are checked by the unique index
				return;
			}

			// must be called synchronized because of _mainData
			for (int i = 0; i < _len; i++) 
			{
				object o = row[_refColumns[i]];

				if (o == null) 
				{

					// if one column is null then integrity is not checked
					return;
				}

				_mainData[_mainColumns[i]] = o;
			}

			// a record must exist in the main table
			Trace.Check(_mainIndex.Find(_mainData) != null,
				Trace.INTEGRITY_CONSTRAINT_VIOLATION);
		}

		/// <summary>
		/// Verify if the delete operation can be performed.
		/// </summary>
		/// <param name="row"></param>
		internal void CheckDelete(object[] row) 
		{
			if (_type == ConstraintType.ForeignKey || _type == ConstraintType.Unique) 
			{

				// deleting references are never a problem
				// unique constraints are checked by the unique index
				return;
			}

			// must be called synchronized because of _refData
			for (int i = 0; i < _len; i++) 
			{
				object o = row[_mainColumns[i]];

				if (o == null) 
				{

					// if one column is null then integrity is not checked
					return;
				}

				_refData[_refColumns[i]] = o;
			}

			// there must be no record in the 'slave' table
			Trace.Check(_refIndex.Find(_refData) == null,
				Trace.INTEGRITY_CONSTRAINT_VIOLATION);
		}

		/// <summary>
		/// Verify if the update operation can be performed.
		/// </summary>
		/// <param name="col"></param>
		/// <param name="deleted"></param>
		/// <param name="inserted"></param>
		internal void CheckUpdate(int[] col, Result deleted, Result inserted) 
		{
			if (_type == ConstraintType.Unique) 
			{

				// unique constraints are checked by the unique index
				return;
			}

			if (_type == ConstraintType.Main) 
			{
				if (!IsAffected(col, _mainColumns, _len)) 
				{
					return;
				}

				// check deleted records
				Record r = deleted.Root;

				while (r != null) 
				{

					// if a identical record exists we don't have to test
					if (_mainIndex.Find(r.Data) == null) 
					{
						CheckDelete(r.Data);
					}

					r = r.Next;
				}
			} 
			else if (_type == ConstraintType.ForeignKey) 
			{
				if (!IsAffected(col, _mainColumns, _len)) 
				{
					return;
				}

				// check inserted records
				Record r = inserted.Root;

				while (r != null) 
				{
					CheckInsert(r.Data);

					r = r.Next;
				}
			}
		}

		/// <summary>
		/// Verify if this constraint is affected by the database operation.
		/// </summary>
		/// <param name="col"></param>
		/// <param name="col2"></param>
		/// <param name="len"></param>
		/// <returns></returns>
		private bool IsAffected(int[] col, int[] col2, int len) 
		{
			if (_type == ConstraintType.Unique) 
			{

				// unique constraints are checked by the unique index
				return false;
			}

			for (int i = 0; i < col.Length; i++) 
			{
				int c = col[i];

				for (int j = 0; j < len; j++) 
				{
					if (c == col2[j]) 
					{
						return true;
					}
				}
			}

			return false;
		}

		#endregion
	}
}
