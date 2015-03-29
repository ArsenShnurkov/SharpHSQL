#region Usings
using System;
using System.Collections;
#endregion

#region License
/*
 * Select.cs
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
	/// Class representing a SELECT operation.
	/// </summary>
	sealed class Select 
	{
		public bool				bDistinct;
		public TableFilter[]    tFilter;
		public Expression       eCondition;		// null means no condition
		public Expression[]     eColumn;		// 'result', 'group' and 'order' columns
		public int				iResultLen;		// number of columns that are 'result'
		public int				iGroupLen;		// number of columns that are 'group'
		public int				iOrderLen;		// number of columns that are 'order'
		public Select			sUnion;			// null means no union select
		public string			sIntoTable;		// null means not select..into
		public SelectType		UnionType;
		public bool				OnlyVars;		// true if the select has only variables

		// fredt@users.sourceforge.net begin changes from 1.50
		public int limitStart = 0;
		public int limitCount = -1;
		// fredt@users.sourceforge.net end changes from 1.50

		public void Resolve() 
		{
			int len = tFilter.Length;

			for (int i = 0; i < len; i++) 
			{
				Resolve(tFilter[i], true);
			}
		}

		public void Resolve(TableFilter filter, bool ownfilter) 
		{
			if (eCondition != null) 
			{

				// first set the table filter in the condition
				eCondition.Resolve(filter);

				if (filter != null && ownfilter) 
				{

					// the table filter tries to get as many conditions as possible
					// but only if the table filter belongs to this query
					filter.SetCondition(eCondition);
				}
			}

			int len = eColumn.Length;

			for (int i = 0; i < len; i++) 
			{
				eColumn[i].Resolve(filter);
			}
		}

		public void CheckResolved() 
		{
			if (eCondition != null) 
			{
				eCondition.CheckResolved();
			}

			int len = eColumn.Length;

			for (int i = 0; i < len; i++) 
			{
				eColumn[i].CheckResolved();
			}
		}

		public object GetValue(ColumnType type) 
		{
			Resolve();

			Result r = GetResult(2, null);    // 2 records are (already) too much
			int    size = r.Size;
			int    len = r.ColumnCount;

			Trace.Check(size == 1 && len == 1, Trace.SINGLE_VALUE_EXPECTED);

			object o = r.Root.Data[0];

			if (r.Type[0] == type) 
			{
				return o;
			}

			string s = Column.ConvertToString(o, type);

			return Column.ConvertString(s, type);
		}

		public Result GetResult(int maxrows, Channel cChannel) 
		{
			// fredt@users.sourceforge.net begin changes from 1.50
			return GetResult( 0, maxrows, cChannel );
		}
		// fredt@users.sourceforge.net end changes from 1.50
		// fredt@users.sourceforge.net begin changes from 1.50
		public Result GetResult(int start, int cnt, Channel cChannel) 
		{
			int maxrows=start+cnt;  //<-new, cut definitly
			// fredt@users.sourceforge.net begin changes from 1.50
			Resolve();
			CheckResolved();

			if (sUnion != null && sUnion.iResultLen != iResultLen) 
			{
				throw Trace.Error(Trace.COLUMN_COUNT_DOES_NOT_MATCH);
			}

			int     len = eColumn.Length;
			Result  r = new Result(len);
			bool aggregated = false;
			bool grouped = false;

			for (int i = 0; i < len; i++) 
			{
				Expression e = eColumn[i];

				r.Type[i] = e.ColumnType;

				if (e.IsAggregate) 
				{
					aggregated = true;
				}
			}

			object[] agg = null;

			if (aggregated) 
			{
				agg = new object[len];
			}

			if (iGroupLen > 0) 
			{    // has been set in Parser
				grouped = true;
			}

			bool simple_maxrows = false;

			if (maxrows != 0 && grouped == false && sUnion == null && iOrderLen == 0) 
			{
				simple_maxrows = true;
			} 

			int     count = 0;
			int     filter = tFilter.Length;
			bool[] first = new bool[filter];
			int     level = 0;

			while (level >= 0) 
			{
				bool     found = false;

				if( filter > 0 )
				{
					TableFilter t = tFilter[level];

					if (!first[level]) 
					{
						found = t.FindFirst();
						first[level] = found;
					} 
					else 
					{
						found = t.Next();
						first[level] = found;
					}

				}

				if (!found) 
				{
					level--;

					if( !OnlyVars )
						continue;
				}

				if (level < filter - 1) 
				{
					level++;

					continue;
				}

				if (eCondition == null || eCondition.Test()) 
				{
					object[] row = new object[len];

					for (int i = 0; i < len; i++) 
					{
						row[i] = eColumn[i].GetValue();

						if( cChannel != null && eColumn[i].IsVarAssign )
						{
							cChannel.SetDeclareValue( eColumn[i].Arg.ColumnName, row[i] );
						}
					}

					count++;

					if (aggregated && !grouped) 
					{
						UpdateAggregateRow(agg, row, len);
					} 
					else 
					{
						r.Add(row);

						if (simple_maxrows && count >= maxrows) 
						{
							break;
						}
					}
				}
			}

			if ( aggregated && !grouped )
			{
				AddAggregateRow(r, agg, len, count);
			} 
			else if ( grouped ) 
			{
				int[] order = new int[iGroupLen];
				int[] way = new int[iGroupLen];

				for (int i = iResultLen, j = 0; j < iGroupLen; i++, j++) 
				{
					order[j] = i;
					way[j] = 1;
				}

				r = SortResult(r, order, way);

				Record n = r.Root;
				Result x = new Result(len);

				for (int i = 0; i < len; i++) 
				{
					x.Type[i] = r.Type[i];
				}

				do 
				{
					object[] row = new object[len];

					count = 0;

					bool newgroup = false;

					while (n != null && newgroup == false) 
					{
						count++;

						for (int i = 0; i < iGroupLen; i++) 
						{
							if (n.Next == null) 
							{
								newgroup = true;
							} 
							else if (Column.Compare(n.Data[i], n.Next.Data[i], r.Type[i]) != 0) 
							{
								// can't use .Equals because 'null' is also one group
								newgroup = true;
							}
						}

						UpdateAggregateRow(row, n.Data, len);

						n = n.Next;
					}

					AddAggregateRow(x, row, len, count);

				} while (n != null);

				r = x;
			}

			if (iOrderLen != 0) 
			{
				int[] order = new int[iOrderLen];
				int[] way = new int[iOrderLen];

				for (int i = iResultLen, j = 0; j < iOrderLen; i++, j++) 
				{
					order[j] = i;
					way[j] = eColumn[i].IsDescending ? -1 : 1;
				}

				r = SortResult(r, order, way);
			}

			// the result maybe is bigger (due to group and order by)
			// but don't tell this anybody else
			r.SetColumnCount( iResultLen );

			if (bDistinct) 
			{
				r = RemoveDuplicates(r);
			}

			for (int i = 0; i < iResultLen; i++) 
			{
				Expression e = eColumn[i];

				r.Label[i] = e.Alias;
				r.Table[i] = e.TableName;
				r.Name[i] = e.ColumnName;
			}

			if (sUnion != null) 
			{
				Result x = sUnion.GetResult(0, cChannel);

				if (UnionType == SelectType.Union) 
				{
					r.Append(x);

					r = RemoveDuplicates(r);
				} 
				else if (UnionType == SelectType.UnionAll) 
				{
					r.Append(x);
				} 
				else if (UnionType == SelectType.Intersect) 
				{
					r = RemoveDuplicates(r);
					x = RemoveDuplicates(x);
					r = RemoveDifferent(r, x);
				} 
				else if (UnionType == SelectType.Except) 
				{
					r = RemoveDuplicates(r);
					x = RemoveDuplicates(x);
					r = RemoveSecond(r, x);
				}
			}

			if (maxrows > 0 &&!simple_maxrows) 
			{
				TrimResult(r, maxrows);
			}

			// fredt@users.sourceforge.net begin changes from 1.50
			if (start > 0) 
			{	//then cut the first 'start' elements
				TrimResultFront( r, start );
			}
			// fredt@users.sourceforge.net end changes from 1.50

			return r;
		}

		private void UpdateAggregateRow(object[] row, object[] n, int len) 
		{
			for (int i = 0; i < len; i++) 
			{
				ColumnType type = eColumn[i].ColumnType;

				switch (eColumn[i].Type) 
				{

					case ExpressionType.Average:
					case ExpressionType.Sum:
					case ExpressionType.Count:
						row[i] = Column.Sum(row[i], n[i], type);
						break;

					case ExpressionType.Minimum:
						row[i] = Column.Min(row[i], n[i], type);
						break;

					case ExpressionType.Maximum:
						row[i] = Column.Max(row[i], n[i], type);
						break;

					default:
						row[i] = n[i];
						break;
				}
			}
		}

		private void AddAggregateRow(Result result, object[] row, int len, int count) 
		{
			for (int i = 0; i < len; i++) 
			{
				ExpressionType t = eColumn[i].Type;

				if (t == ExpressionType.Average) 
				{
					row[i] = Column.Avg(row[i], eColumn[i].ColumnType, count);
				} 
				else if (t == ExpressionType.Count) 
				{
					// this fixes the problem with count(*) on a empty table
					if (row[i] == null) 
					{
						row[i] = 0;
					}
				}
			}

			result.Add(row);
		}

		private static Result RemoveDuplicates(Result result) 
		{
			int len = result.ColumnCount;
			int[] order = new int[len];
			int[] way = new int[len];

			for (int i = 0; i < len; i++) 
			{
				order[i] = i;
				way[i] = 1;
			}

			result = SortResult(result, order, way);

			Record n = result.Root;

			while (n != null) 
			{
				Record next = n.Next;

				if (next == null) 
				{
					break;
				}

				if (CompareRecord(n.Data, next.Data, result, len) == 0) 
				{
					n.Next = next.Next;
				} 
				else 
				{
					n = n.Next;
				}
			}

			return result;
		}

		private static Result RemoveSecond(Result result, Result minus) 
		{
			int     len = result.ColumnCount;
			Record  n = result.Root;
			Record  last = result.Root;
			bool rootr = true;    // checking rootrecord
			Record  n2 = minus.Root;
			int     i = 0;

			while (n != null && n2 != null) 
			{
				i = CompareRecord(n.Data, n2.Data, result, len);

				if (i == 0) 
				{
					if (rootr) 
					{
						result.SetRoot( n.Next );
					} 
					else 
					{
						last.Next = n.Next;
					}

					n = n.Next;
				} 
				else if (i > 0) 
				{    // r > minus
					n2 = n2.Next;
				} 
				else 
				{		   // r < minus
					last = n;
					rootr = false;
					n = n.Next;
				}
			}

			return result;
		}

		private static Result RemoveDifferent(Result r1, Result r2) 
		{
			int     len = r1.ColumnCount;
			Record  n = r1.Root;
			Record  last = r1.Root;
			bool rootr = true;    // checking rootrecord
			Record  n2 = r2.Root;
			int     i = 0;

			while (n != null && n2 != null) 
			{
				i = CompareRecord(n.Data, n2.Data, r1, len);

				if (i == 0) 
				{	      // same rows
					if (rootr) 
					{
						r1.SetRoot( n );      // make this the first record
					} 
					else 
					{
						last.Next = n;    // this is next record in resultset
					}

					rootr = false;
					last = n;	      // this is last record in resultset
					n = n.Next;
					n2 = n2.Next;
				} 
				else if (i > 0) 
				{       // r > r2
					n2 = n2.Next;
				} 
				else 
				{		      // r < r2
					n = n.Next;
				}
			}

			if (rootr) 
			{		 // if no lines in resultset
				r1.SetRoot( null );      // then return null
			} 
			else 
			{
				last.Next = null;    // else end resultset
			}

			return r1;
		}

		private static Result SortResult(Result r, int[] order, int[] way) 
		{
			if (r.Root == null || r.Root.Next == null) 
			{
				return r;
			}

			Record source0, source1;
			Record[] target = new Record[2];
			Record[] targetlast = new Record[2];
			int    dest = 0;
			Record n = r.Root;

			while (n != null) 
			{
				Record next = n.Next;

				n.Next = target[dest];
				target[dest] = n;
				n = next;
				dest ^= 1;
			}

			for (int blocksize = 1; target[1] != null; blocksize <<= 1) 
			{
				source0 = target[0];
				source1 = target[1];
				target[0] = target[1] = targetlast[0] = targetlast[1] = null;

				for (dest = 0; source0 != null; dest ^= 1) 
				{
					int n0 = blocksize, n1 = blocksize;

					while (true) 
					{
						if (n0 == 0 || source0 == null) 
						{
							if (n1 == 0 || source1 == null) 
							{
								break;
							}

							n = source1;
							source1 = source1.Next;
							n1--;
						} 
						else if (n1 == 0 || source1 == null) 
						{
							n = source0;
							source0 = source0.Next;
							n0--;
						} 
						else if (CompareRecord(source0.Data, source1.Data, r, order, way)
							> 0) 
						{
							n = source1;
							source1 = source1.Next;
							n1--;
						} 
						else 
						{
							n = source0;
							source0 = source0.Next;
							n0--;
						}

						if (target[dest] == null) 
						{
							target[dest] = n;
						} 
						else 
						{
							targetlast[dest].Next = n;
						}

						targetlast[dest] = n;
						n.Next = null;
					}
				}
			}

			r.SetRoot( target[0] );

			return r;
		}

		// fredt@users.sourceforge.net begin changes from 1.50
		private static void TrimResultFront( Result result, int start ) 
		{
			Record n=result.Root;
			if(n==null) 
			{
				return;
			}
			while(--start >= 0) 
			{
				n = n.Next;
				if(n == null) 
				{
					return;
				}
			}
			result.SetRoot( n );
		}

		// fredt@users.sourceforge.net end changes from 1.50
		private static void TrimResult(Result result, int maxrows) 
		{
			Record n = result.Root;

			if (n == null) 
			{
				return;
			}

			while (--maxrows > 0) 
			{
				n = n.Next;

				if (n == null) 
				{
					return;
				}
			}

			n.Next = null;
		}

		private static int CompareRecord(object[] a, object[] b, Result r, int[] order, int[] way) 
		{
			int i = Column.Compare(a[order[0]], b[order[0]], r.Type[order[0]]);

			if (i == 0) 
			{
				for (int j = 1; j < order.Length; j++) 
				{
					i = Column.Compare(a[order[j]], b[order[j]],
						r.Type[order[j]]);

					if (i != 0) 
					{
						return i * way[j];
					}
				}
			}

			return i * way[0];
		}

		private static int CompareRecord(object[] a, object[] b, Result r, int len) 
		{
			for (int j = 0; j < len; j++) 
			{
				int i = Column.Compare(a[j], b[j], r.Type[j]);

				if (i != 0) 
				{
					return i;
				}
			}

			return 0;
		}
	}
}
