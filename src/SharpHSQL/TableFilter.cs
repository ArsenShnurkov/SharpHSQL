#region Usings
using System;
using System.Collections;
#endregion

#region License
/*
 * TableFilter.cs
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
	/// Table filter class.
	/// </summary>
	sealed class TableFilter 
	{
		private Table      tTable;
		private string     sAlias;
		private Index      iIndex;
		private Node       nCurrent;
		private object[]     oEmptyData;
		private Expression eStart, eEnd;
		private Expression eAnd;
		private bool    bOuterJoin;

		// this is internal to improve performance
		internal object[]	       oCurrentData;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="alias"></param>
		/// <param name="outerjoin"></param>
		public TableFilter(Table t, string alias, bool outerjoin) 
		{
			tTable = t;
			iIndex = null;
			sAlias = alias != null ? alias : t.Name;
			bOuterJoin = outerjoin;
			oEmptyData = tTable.NewRow;
		}

		public string Name
		{
			get
			{
				return sAlias;
			}
		}


		public Table Table
		{
			get
			{
				return tTable;
			}
		}

		public void SetCondition(Expression e) 
		{
			ExpressionType type = e.Type;
			Expression e1 = e.Arg;
			Expression e2 = e.Arg2;

			if (type == ExpressionType.And) 
			{
				SetCondition(e1);
				SetCondition(e2);

				return;
			}

			int candidate;

			switch (type) 
			{

				case ExpressionType.NotEqual:

				case ExpressionType.Like:    // todo: maybe use index

				case ExpressionType.In:
					candidate = 0;

					break;

				case ExpressionType.Equal:
					candidate = 1;

					break;

				case ExpressionType.Bigger:

				case ExpressionType.BiggerEqual:
					candidate = 2;

					break;

				case ExpressionType.Smaller:

				case ExpressionType.SmallerEqual:
					candidate = 3;

					break;

				default:

					// not a condition so forget it
					return;
			}

			if (e1.Filter == this) 
			{

				// ok include this
			} 
			else if (e2.Filter == this && candidate != 0) 
			{

				// swap and try again to allow index usage
				e.SwapCondition();
				SetCondition(e);

				return;
			} 
			else 
			{

				// unrelated: don't include
				return;
			}

			Trace.Assert(e1.Filter == this, "setCondition");

			if (!e2.IsResolved) 
			{
				return;
			}

			if (candidate == 0) 
			{
				AddAndCondition(e);

				return;
			}

			int   i = e1.ColumnNumber;
			Index index = tTable.GetIndexForColumn(i);

			if (index == null || (iIndex != index && iIndex != null)) 
			{

				// no index or already another index is used
				AddAndCondition(e);

				return;
			}

			iIndex = index;

			if (candidate == 1) 
			{

				// candidate for both start & end
				if (eStart != null || eEnd != null) 
				{
					AddAndCondition(e);

					return;
				}

				eStart = new Expression(e);
				eEnd = eStart;
			} 
			else if (candidate == 2) 
			{

				// candidate for start
				if (eStart != null) 
				{
					AddAndCondition(e);

					return;
				}

				eStart = new Expression(e);
			} 
			else if (candidate == 3) 
			{

				// candidate for end
				if (eEnd != null) 
				{
					AddAndCondition(e);

					return;
				}

				eEnd = new Expression(e);
			}

			e.SetTrue();
		}

		public bool FindFirst() 
		{
			if (iIndex == null) 
			{
				iIndex = tTable.PrimaryIndex;
			}

			if (eStart == null) 
			{
				nCurrent = iIndex.First();
			} 
			else 
			{
				ColumnType type = eStart.Arg.ColumnType;
				object o = eStart.Arg2.GetValue(type);

				nCurrent = iIndex.FindFirst(o, eStart.Type);
			}

			while (nCurrent != null) 
			{
				oCurrentData = nCurrent.GetData();

				if (!Test(eEnd)) 
				{
					break;
				}

				if (Test(eAnd)) 
				{
					return true;
				}

				nCurrent = iIndex.Next(nCurrent);
			}

			oCurrentData = oEmptyData;

			if (bOuterJoin) 
			{
				return true;
			}

			return false;
		}

		public bool Next() 
		{
			if (bOuterJoin && nCurrent == null) 
			{
				return false;
			}

			nCurrent = iIndex.Next(nCurrent);

			while (nCurrent != null) 
			{
				oCurrentData = nCurrent.GetData();

				if (!Test(eEnd)) 
				{
					break;
				}

				if (Test(eAnd)) 
				{
					return true;
				}

				nCurrent = iIndex.Next(nCurrent);
			}

			oCurrentData = oEmptyData;

			return false;
		}

		private void AddAndCondition(Expression e) 
		{
			Expression e2 = new Expression(e);

			if (eAnd == null) 
			{
				eAnd = e2;
			} 
			else 
			{
				Expression and = new Expression(ExpressionType.And, eAnd, e2);

				eAnd = and;
			}

			e.SetTrue();
		}

		private bool Test(Expression e) 
		{
			if (e == null) 
			{
				return true;
			}

			return e.Test();
		}
	}
}
