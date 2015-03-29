#region Usings
using System;
using System.Collections;
#endregion

#region License
/*
 * Expression.cs
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
	/// Class that represents an expression.
	/// </summary>
	/// <remarks>version 1.0.0.1</remarks>
	sealed class Expression 
	{
		#region Private Vars

		private ExpressionType _type;

		// nodes
		private Expression  eArg, eArg2;

		// VALUE, VALUELIST
		private object      _data;
		private Hashtable   _list;
		private ColumnType	_columnType;

		// QUERY (correlated subquery)
		private Select      sSelect;

		// FUNCTION
		private Function    fFunction;

		// LIKE
		private char	cLikeEscape;

		// COLUMN
		private string			sTable, sColumn;
		private TableFilter		tFilter;	// null if not yet resolved
		private int				iColumn;
		private string			sAlias;    // if it is a column of a select column list
		private bool			bDescending;    // if it is a column in a order by

		#endregion

		#region Constructors

		/// <summary>
		/// Expression constructor using a <see cref="Function"/>.
		/// </summary>
		/// <param name="f"></param>
		public Expression(Function f) 
		{
			_type = ExpressionType.Function;
			fFunction = f;
		}

		/// <summary>
		/// Expression constructor using an <see cref="Expression"/>.
		/// </summary>
		/// <param name="e"></param>
		public Expression(Expression e) 
		{
			_type = e.Type;
			_columnType = e.ColumnType;
			eArg = e.eArg;
			eArg2 = e.eArg2;
			cLikeEscape = e.cLikeEscape;
			sSelect = e.sSelect;
			fFunction = e.fFunction;
		}

		/// <summary>
		/// Expression constructor using a <see cref="Select"/>.
		/// </summary>
		/// <param name="s"></param>
		public Expression(Select s) 
		{
			_type = ExpressionType.Query;
			sSelect = s;
		}

		/// <summary>
		/// Expression constructor using a values list.
		/// </summary>
		/// <param name="v"></param>
		public Expression(ArrayList v) 
		{
			_type = ExpressionType.ValueList;
			_columnType = ColumnType.VarChar;

			int len = v.Count;

			_list = new Hashtable(len);

			for (int i = 0; i < len; i++) 
			{
				object o = v[i];

				if (o != null) 
				{
					_list.Add(o, this);    // todo: don't use such dummy objects
				}
			}
		}

		/// <summary>
		/// Expression constructor using two expressions as arguments.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="e"></param>
		/// <param name="e2"></param>
		public Expression(ExpressionType type, Expression e, Expression e2) 
		{
			_type = type;
			eArg = e;
			eArg2 = e2;
		}

		/// <summary>
		/// Expression constructor using a table select.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="column"></param>
		public Expression(string table, string column) 
		{
			sTable = table;

			if (column == null) 
			{
				_type = ExpressionType.Asterix;
			} 
			else 
			{
				_type = ExpressionType.DatabaseColumn;
				sColumn = column;
			}
		}

		/// <summary>
		/// Expression constructor using a value.
		/// </summary>
		/// <param name="datatype"></param>
		/// <param name="o"></param>
		public Expression(ColumnType datatype, object o) 
		{
			_type = ExpressionType.Value;
			_columnType = datatype;
			_data = o;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets or sets the expression type.
		/// </summary>
		public ExpressionType Type
		{
			get
			{
				return _type;
			}
			set
			{
				_type = value;
			}
		}

		/// <summary>
		/// Returns True if expression is Aggregate.
		/// </summary>
		public bool IsAggregate
		{
			get
			{
				if (_type == ExpressionType.Count || 
					_type == ExpressionType.Maximum || 
					_type == ExpressionType.Minimum || 
					_type == ExpressionType.Sum
					|| _type == ExpressionType.Average) 
				{
					return true;
				}

				// todo: recurse eArg and eArg2; maybe they are grouped.
				// grouping 'correctly' would be quite complex
				return false;
			}
		}

		/// <summary>
		/// Returns True if expression is a Variable assignation.
		/// </summary>
		public bool IsVarAssign
		{
			get
			{
				if (_type == ExpressionType.Equal &&
					eArg != null && eArg2 != null && 
					eArg.Type == ExpressionType.Variable) 
				{
					return true;
				}

				return false;
			}
		}

		/// <summary>
		/// Gets or sets if expression is ordered.
		/// </summary>
		public bool IsDescending
		{
			get
			{
				return bDescending;
			}
			set
			{
				bDescending = value;
			}
		}

		/// <summary>
		/// Gets or sets the expression alias.
		/// </summary>
		public string Alias
		{
			get
			{
				if (sAlias != null) 
				{
					return sAlias;
				}

				if (_type == ExpressionType.Value) 
				{
					return "";
				}

				if (_type == ExpressionType.DatabaseColumn) 
				{
					return sColumn;
				}

				if (_type == ExpressionType.Variable) 
				{
					return sColumn;
				}

				// todo
				return "";
			}
			set
			{
				sAlias = value;
			}
		}

		/// <summary>
		/// Gets the column index that this expression applies to.
		/// </summary>
		public int ColumnNumber
		{
			get
			{
				return iColumn;
			}
		}

		/// <summary>
		/// Gets the expression first argument.
		/// </summary>
		public Expression Arg
		{
			get
			{
				return eArg;
			}
		}

		/// <summary>
		/// Gets the expression second argument.
		/// </summary>
		public Expression Arg2
		{
			get
			{
				return eArg2;
			}
		}

		/// <summary>
		/// Gets the expression filter.
		/// </summary>
		public TableFilter Filter
		{
			get
			{
				return tFilter;
			}
		}

		/// <summary>
		/// Gets or sets the column type.
		/// </summary>
		public ColumnType ColumnType
		{
			get
			{
				return _columnType;
			}
			set
			{
				_columnType = value;
			}
		}

		/// <summary>
		/// Gets if the current expression is already resolved.
		/// </summary>
		/// <returns></returns>
		public bool IsResolved
		{
			get
			{
				if (_type == ExpressionType.Value) 
				{
					return true;
				}

				if (_type == ExpressionType.DatabaseColumn) 
				{
					return tFilter != null;
				}

				// todo: could recurse here, but never miss a 'false'!
				return false;
			}
		}

		/// <summary>
		/// Gets the table name for this expression.
		/// </summary>
		public string TableName
		{
			get
			{
				if (_type == ExpressionType.Asterix) 
				{
					return sTable;
				}

				if (_type == ExpressionType.DatabaseColumn) 
				{
					if (tFilter == null) 
					{
						return sTable;
					} 
					else 
					{
						return tFilter.Table.Name;
					}
				}

				// todo
				return "";
			}
		}

		/// <summary>
		/// Gets the column name for this expression.
		/// </summary>
		public string ColumnName
		{
			get
			{
				if (_type == ExpressionType.DatabaseColumn) 
				{
					if (tFilter == null) 
					{
						return sColumn;
					} 
					else 
					{
						return tFilter.Table.GetColumnName(iColumn);
					}
				}
				else if(_type == ExpressionType.Variable) 
				{
					return sColumn;
				}

				return Alias;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Sets the escape character.
		/// </summary>
		/// <param name="c"></param>
		public void SetLikeEscape(char c) 
		{
			cLikeEscape = c;
		}

		/// <summary>
		/// Sets the first argument value.
		/// </summary>
		/// <param name="value"></param>
		public void SetArg(object value) 
		{
			eArg = new Expression( _columnType, value );
		}

		/// <summary>
		/// Sets the second argument value.
		/// </summary>
		/// <param name="value"></param>
		public void SetArg2(object value) 
		{
			eArg2 = new Expression( _columnType, value );
		}

		/// <summary>
		/// Sets the column data type.
		/// </summary>
		/// <param name="type"></param>
		public void SetDataType(ColumnType type) 
		{
			_columnType = type;
		}

		/// <summary>
		/// Set the expression as True.
		/// </summary>
		public void SetTrue() 
		{
			_type = ExpressionType.True;
		}

		/// <summary>
		/// Check if the expression is resolved, recursively.
		/// </summary>
		public void CheckResolved() 
		{
			Trace.Check(_type != ExpressionType.DatabaseColumn || tFilter != null,
				Trace.COLUMN_NOT_FOUND, sColumn);

			if (eArg != null) 
			{
				eArg.CheckResolved();
			}

			if (eArg2 != null) 
			{
				eArg2.CheckResolved();
			}

			if (sSelect != null) 
			{
				sSelect.CheckResolved();
			}

			if (fFunction != null) 
			{
				fFunction.CheckResolved();
			}
		}

		/// <summary>
		/// Resolve the current expression.
		/// </summary>
		/// <param name="filter"></param>
		public void Resolve(TableFilter filter) 
		{
			if (filter != null && _type == ExpressionType.DatabaseColumn) 
			{
				if (sTable == null || filter.Name.Equals(sTable)) 
				{
					int i = filter.Table.SearchColumn(sColumn);

					if (i != -1) 
					{

						// todo: other error message: multiple tables are possible
						Trace.Check(tFilter == null || tFilter == filter,
							Trace.COLUMN_NOT_FOUND, sColumn);

						tFilter = filter;
						iColumn = i;
						sTable = filter.Name;
						_columnType = filter.Table.GetColumnType(i);
					}
				}
			}

			// currently sets only data type
			// todo: calculate fixed expressions if possible
			if (eArg != null) 
			{
				eArg.Resolve(filter);
			}

			if (eArg2 != null) 
			{
				eArg2.Resolve(filter);
			}

			if (sSelect != null) 
			{
				sSelect.Resolve(filter, false);
				sSelect.Resolve();
			}

			if (fFunction != null) 
			{
				fFunction.Resolve(filter);
			}

			if (_columnType != ColumnType.Null) 
			{
				return;
			}

			switch (_type) 
			{
				case ExpressionType.Function:
					_columnType = fFunction.GetReturnType();
					break;

				case ExpressionType.Query:
					_columnType = sSelect.eColumn[0].ColumnType;
					break;

				case ExpressionType.Negate:
					_columnType = eArg.ColumnType;
					break;

				case ExpressionType.Add:
				case ExpressionType.Subtract:
				case ExpressionType.Multiply:
				case ExpressionType.Divide:
					_columnType = eArg._columnType;
					break;

				case ExpressionType.Concat:
					_columnType = ColumnType.VarChar;
					break;

				case ExpressionType.Not:
				case ExpressionType.BiggerEqual:
				case ExpressionType.Bigger:
				case ExpressionType.Smaller:
				case ExpressionType.SmallerEqual:
				case ExpressionType.NotEqual:
				case ExpressionType.Like:
				case ExpressionType.And:
				case ExpressionType.Or:
				case ExpressionType.In:
				case ExpressionType.Exists:
					_columnType = ColumnType.Bit;
					break;

				case ExpressionType.Equal:
					if( this.IsVarAssign )
					{
						_columnType = eArg2.ColumnType;
					}
					else
					{
						_columnType = ColumnType.Bit;
					}
					break;

				case ExpressionType.Count:
					_columnType = ColumnType.Integer;
					break;

				case ExpressionType.Maximum:
				case ExpressionType.Minimum:
				case ExpressionType.Sum:
				case ExpressionType.Average:
					_columnType = eArg.ColumnType;
					break;

				case ExpressionType.Convert:
					// it is already set
					break;

				case ExpressionType.IfNull:
				case ExpressionType.CaseWhen:
					_columnType = eArg2.ColumnType;
					break;
				case ExpressionType.Variable:
					_columnType = eArg.ColumnType;
					break;
			}
		}
		
		/// <summary>
		/// Swaps the conditions for this expression.
		/// </summary>
		public void SwapCondition() 
		{
			ExpressionType i = ExpressionType.Equal;

			switch (_type) 
			{

				case ExpressionType.BiggerEqual:
					i = ExpressionType.SmallerEqual;

					break;

				case ExpressionType.SmallerEqual:
					i = ExpressionType.BiggerEqual;

					break;

				case ExpressionType.Smaller:
					i = ExpressionType.Bigger;

					break;

				case ExpressionType.Bigger:
					i = ExpressionType.Smaller;

					break;

				case ExpressionType.Equal:
					break;

				default:
					Trace.Assert(false, "Expression.swapCondition");
					break;
			}

			_type = i;

			Expression e = eArg;

			eArg = eArg2;
			eArg2 = e;
		}

		/// <summary>
		/// Gets the resulting expression value with a column type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public object GetValue(ColumnType type) 
		{
			object o = GetValue();

			if (o == null || _columnType == type) 
			{
				return o;
			}

			string s = Column.ConvertToString(o, type);

			return Column.ConvertString(s, type);
		}

		/// <summary>
		/// Gets if the expression is some type of comparison.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsCompare(ExpressionType type) 
		{
			switch (type) 
			{
				case ExpressionType.Equal:
				case ExpressionType.BiggerEqual:
				case ExpressionType.Bigger:
				case ExpressionType.Smaller:
				case ExpressionType.SmallerEqual:
				case ExpressionType.NotEqual:
					return true;
			}

			return false;
		}

		/// <summary>
		/// Gets the expression resulting value.
		/// </summary>
		/// <returns></returns>
		public object GetValue() 
		{
			switch (_type) 
			{

				case ExpressionType.Value:
					return _data;

				case ExpressionType.DatabaseColumn:
					try 
					{
						return tFilter.oCurrentData[iColumn];
					} 
					catch
					{
						throw Trace.Error(Trace.COLUMN_NOT_FOUND, sColumn);
					}

				case ExpressionType.Function:
					return fFunction.GetValue();

				case ExpressionType.Query:
					return sSelect.GetValue(_columnType);

				case ExpressionType.Negate:
					return Column.Negate(eArg.GetValue(_columnType), _columnType);

				case ExpressionType.Count:

					// count(*): sum(1); count(col): sum(col<>null)
					if (eArg.Type == ExpressionType.Asterix || eArg.GetValue() != null) 
					{
						return 1;
					}

					return 0;

				case ExpressionType.Maximum:
				case ExpressionType.Minimum:
				case ExpressionType.Sum:
				case ExpressionType.Average:
					return eArg.GetValue();

				case ExpressionType.Exists:
					return Test();

				case ExpressionType.Convert:
					return eArg.GetValue(_columnType);

				case ExpressionType.CaseWhen:
					if (eArg.Test()) 
					{
						return eArg2.eArg.GetValue();
					} 
					else 
					{
						return eArg2.eArg2.GetValue();
					}
				case ExpressionType.Variable:
					return eArg.GetValue();
			}

			// todo: simplify this
			object a = null, b = null;

			if (eArg != null) 
			{
				a = eArg.GetValue(_columnType);
			}

			if (eArg2 != null) 
			{
				b = eArg2.GetValue(_columnType);
			}

			switch (_type) 
			{

				case ExpressionType.Add:
					return Column.Add(a, b, _columnType);

				case ExpressionType.Subtract:
					return Column.Subtract(a, b, _columnType);

				case ExpressionType.Multiply:
					return Column.Multiply(a, b, _columnType);

				case ExpressionType.Divide:
					return Column.Divide(a, b, _columnType);

				case ExpressionType.Concat:
					return Column.Concat(a, b, _columnType);

				case ExpressionType.IfNull:
					return a == null ? b : a;

				case ExpressionType.Equal:
					if( eArg.Type == ExpressionType.Variable )
					{
						Trace.Check(eArg2 != null, Trace.GENERAL_ERROR);

						return eArg2.GetValue(eArg._columnType);
					}
					else
					{
						return Test();
					}

				default:

					// must be comparisation
					// todo: make sure it is
					return Test();
			}
		}

		/// <summary>
		/// Test the value list for True or False.
		/// </summary>
		/// <param name="o"></param>
		/// <param name="datatype"></param>
		/// <returns></returns>
		private bool TestValueList(object o, ColumnType datatype) 
		{
			if (_type == ExpressionType.ValueList) 
			{
				if (datatype != _columnType) 
				{
					o = Column.ConvertToObject(o, _columnType);
				}

				return _list.ContainsKey(o);
			} 
			else if (_type == ExpressionType.Query) 
			{
				// todo: convert to valuelist before if everything is resolvable
				Result r = sSelect.GetResult(0, null);
				Record n = r.Root;
				ColumnType type = r.Type[0];

				if (datatype != type) 
				{
					o = Column.ConvertToObject(o, type);
				}

				while (n != null) 
				{
					object o2 = n.Data[0];

					if (o2 != null && o2.Equals(o)) 
					{
						return true;
					}

					n = n.Next;
				}

				return false;
			}

			throw Trace.Error(Trace.WRONG_DATA_TYPE);
		}

		/// <summary>
		/// Test condition for True or False value.
		/// </summary>
		/// <returns></returns>
		public bool Test() 
		{
			switch (_type) 
			{

				case ExpressionType.True:
					return true;

				case ExpressionType.Not:
					Trace.Assert(eArg2 == null, "Expression.test");

					return !eArg.Test();

				case ExpressionType.And:
					return eArg.Test() && eArg2.Test();

				case ExpressionType.Or:
					return eArg.Test() || eArg2.Test();

				case ExpressionType.Like:

					// todo: now for all tests a new 'like' object required!
					string s = (string) eArg2.GetValue(ColumnType.VarChar);
					ColumnType  type = eArg._columnType;
					Like   l = new Like(s, cLikeEscape,	type == ColumnType.VarCharIgnoreCase);
					string c = (string) eArg.GetValue(ColumnType.VarChar);

					return l.Compare(c);

				case ExpressionType.In:
					return eArg2.TestValueList(eArg.GetValue(), eArg._columnType);

				case ExpressionType.Exists:
					Result r = eArg.sSelect.GetResult(1, null);    // 1 is already enough

					return r.Root != null;
			}

			Trace.Check(eArg != null, Trace.GENERAL_ERROR);

			object o = eArg.GetValue();
			ColumnType dtype = eArg._columnType;

			Trace.Check(eArg2 != null, Trace.GENERAL_ERROR);

			object o2 = eArg2.GetValue(dtype);
			int    result = Column.Compare(o, o2, dtype);

			switch (_type) 
			{

				case ExpressionType.Equal:
					return result == 0;

				case ExpressionType.Bigger:
					return result > 0;

				case ExpressionType.BiggerEqual:
					return result >= 0;

				case ExpressionType.SmallerEqual:
					return result <= 0;

				case ExpressionType.Smaller:
					return result < 0;

				case ExpressionType.NotEqual:
					return result != 0;
			}

			Trace.Assert(false, "Expression.test2");

			return false;
		}

		#endregion
	}
}
