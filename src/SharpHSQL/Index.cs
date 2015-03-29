#region Usings
using System;
using System.Collections;
#endregion

#region License
/*
 * Index.cs
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
	/// Index class.
	/// </summary>
	sealed class Index 
	{
		private string			_name;
		private int				_fields;
		private int[]			_column;
		private ColumnType[]	_type;
		private bool			_unique;    // just for scripting; all indexes are made unique

		private Node			_root;
		private int				_column_0;
		private ColumnType		_type_0;
		private static int		_needCleanUp;

		public Index(string name, int[] column, ColumnType[] type, bool unique) 
		{
			_name = name;
			_fields = column.Length;
			_column = column;
			_type = type;
			_unique = unique;
			_column_0 = _column[0];
			_type_0 = _type[0];
		}

		public Node Root
		{
			get
			{
				return _root;
			}
			set
			{
				_root = value;
			}
		}

		public string Name
		{
			get
			{
				return _name;
			}
		}

		public ColumnType[] ColumnType
		{
			get
			{
				return _type;
			}
		}

		public bool IsUnique
		{
			get
			{
				return _unique;
			}
		}

		public int[] Columns
		{
			get
			{
				return _column;    // todo: this gives back also primary key field!
			}
		}

		public void Insert(Node i) 
		{
			object[] data = i.GetData();
			Node    n = _root, x = n;
			bool way = true;
			int     compare = -1;

			while (true) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				if (n == null) 
				{
					if (x == null) 
					{
						_root = i;

						return;
					}

					Set(x, way, i);

					break;
				}

				x = n;
				compare = CompareRow(data, x.GetData());

				Trace.Check(compare != 0, Trace.VIOLATION_OF_UNIQUE_INDEX);

				way = compare < 0;
				n = Child(x, way);
			}

			while (true) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				int sign = way ? 1 : -1;

				switch (x.GetBalance() * sign) 
				{

					case 1:
						x.SetBalance(0);

						return;

					case 0:
						x.SetBalance(-sign);

						break;

					case -1:
						Node l = Child(x, way);

						if (l.GetBalance() == -sign) 
						{
							Replace(x, l);
							Set(x, way, Child(l, !way));
							Set(l, !way, x);
							x.SetBalance(0);
							l.SetBalance(0);
						} 
						else 
						{
							Node r = Child(l, !way);

							Replace(x, r);
							Set(l, !way, Child(r, way));
							Set(r, way, l);
							Set(x, way, Child(r, !way));
							Set(r, !way, x);

							int rb = r.GetBalance();

							x.SetBalance((rb == -sign) ? sign : 0);
							l.SetBalance((rb == sign) ? -sign : 0);
							r.SetBalance(0);
						}

						return;
				}

				if (x.Equals(_root)) 
				{
					return;
				}

				way = From(x);
				x = x.GetParent();
			}
		}

		public void Delete(object[] row, bool datatoo) 
		{
			Node x = Search(row);

			if (x == null) 
			{
				return;
			}

			Node n;

			if (x.GetLeft() == null) 
			{
				n = x.GetRight();
			} 
			else if (x.GetRight() == null) 
			{
				n = x.GetLeft();
			} 
			else 
			{
				Node d = x;

				x = x.GetLeft();

				// todo: this can be improved
				while (x.GetRight() != null) 
				{
					if (Trace.StopEnabled) 
					{
						Trace.Stop();
					}

					x = x.GetRight();
				}

				// x will be replaced with n later
				n = x.GetLeft();

				// swap d and x
				int b = x.GetBalance();

				x.SetBalance(d.GetBalance());
				d.SetBalance(b);

				// set x.parent
				Node xp = x.GetParent();
				Node dp = d.GetParent();

				if (d == _root) 
				{
					_root = x;
				}

				x.SetParent(dp);

				if (dp != null) 
				{
					if (dp.GetRight().Equals(d)) 
					{
						dp.SetRight(x);
					} 
					else 
					{
						dp.SetLeft(x);
					}
				}

				// for in-memory tables we could use: d.rData=x.rData;
				// but not for cached tables
				// relink d.parent, x.left, x.right
				if (xp == d) 
				{
					d.SetParent(x);

					if (d.GetLeft().Equals(x)) 
					{
						x.SetLeft(d);
						x.SetRight(d.GetRight());
					} 
					else 
					{
						x.SetRight(d);
						x.SetLeft(d.GetLeft());
					}
				} 
				else 
				{
					d.SetParent(xp);
					xp.SetRight(d);
					x.SetRight(d.GetRight());
					x.SetLeft(d.GetLeft());
				}

				x.GetRight().SetParent(x);
				x.GetLeft().SetParent(x);

				// set d.left, d.right
				d.SetLeft(n);

				if (n != null) 
				{
					n.SetParent(d);
				}

				d.SetRight(null);

				x = d;
			}

			bool way = From(x);

			Replace(x, n);

			n = x.GetParent();

			x.Delete();

			if (datatoo) 
			{
				x.rData.Delete();
			}

			while (n != null) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				x = n;

				int sign = way ? 1 : -1;

				switch (x.GetBalance() * sign) 
				{

					case -1:
						x.SetBalance(0);

						break;

					case 0:
						x.SetBalance(sign);

						return;

					case 1:
						Node r = Child(x, !way);
						int  b = r.GetBalance();

						if (b * sign >= 0) 
						{
							Replace(x, r);
							Set(x, !way, Child(r, way));
							Set(r, way, x);

							if (b == 0) 
							{
								x.SetBalance(sign);
								r.SetBalance(-sign);

								return;
							}

							x.SetBalance(0);
							r.SetBalance(0);

							x = r;
						} 
						else 
						{
							Node l = Child(r, way);

							Replace(x, l);

							b = l.GetBalance();

							Set(r, way, Child(l, !way));
							Set(l, !way, r);
							Set(x, !way, Child(l, way));
							Set(l, way, x);
							x.SetBalance((b == sign) ? -sign : 0);
							r.SetBalance((b == -sign) ? sign : 0);
							l.SetBalance(0);

							x = l;
						}
						break;
				}

				way = From(x);
				n = x.GetParent();
			}
		}

		public Node Find(object[] data) 
		{
			Node x = _root, n;

			while (x != null) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				int i = CompareRowNonUnique(data, x.GetData());

				if (i == 0) 
				{
					return x;
				} 
				else if (i > 0) 
				{
					n = x.GetRight();
				} 
				else 
				{
					n = x.GetLeft();
				}

				if (n == null) 
				{
					return null;
				}

				x = n;
			}

			return null;
		}

		public Node FindFirst(object value, ExpressionType compare) 
		{
			Trace.Assert(compare == ExpressionType.Bigger
				|| compare == ExpressionType.Equal
				|| compare == ExpressionType.BiggerEqual,
				"Index.findFirst");

			Node x = _root;
			int  iTest = 1;

			if (compare == ExpressionType.Bigger) 
			{
				iTest = 0;
			}

			while (x != null) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				bool t = CompareValue(value, x.GetData()[_column_0]) >= iTest;

				if (t) 
				{
					Node r = x.GetRight();

					if (r == null) 
					{
						break;
					}

					x = r;
				} 
				else 
				{
					Node l = x.GetLeft();

					if (l == null) 
					{
						break;
					}

					x = l;
				}
			}

			while (x != null
				&& CompareValue(value, x.GetData()[_column_0]) >= iTest) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				x = Next(x);
			}

			return x;
		}

		public Node First() 
		{
			Node x = _root, l = x;

			while (l != null) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				x = l;
				l = x.GetLeft();
			}

			return x;
		}

		public Node Next(Node x) 
		{

			if ((++_needCleanUp & 127) == 0) 
			{
				x.rData.CleanUpCache();
			}

			Node r = x.GetRight();

			if (r != null) 
			{
				x = r;

				Node l = x.GetLeft();

				while (l != null) 
				{
					if (Trace.StopEnabled) 
					{
						Trace.Stop();
					}

					x = l;
					l = x.GetLeft();
				}

				return x;
			}

			Node ch = x;

			x = x.GetParent();

			while (x != null && ch.Equals(x.GetRight())) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				ch = x;
				x = x.GetParent();
			}

			return x;
		}

		private Node Child(Node x, bool w) 
		{
			return w ? x.GetLeft() : x.GetRight();
		}

		private void Replace(Node x, Node n) 
		{
			if (x.Equals(_root)) 
			{
				_root = n;

				if (n != null) 
				{
					n.SetParent(null);
				}
			} 
			else 
			{
				Set(x.GetParent(), From(x), n);
			}
		}

		private void Set(Node x, bool w, Node n) 
		{
			if (w) 
			{
				x.SetLeft(n);
			} 
			else 
			{
				x.SetRight(n);
			}

			if (n != null) 
			{
				n.SetParent(x);
			}
		}

		private bool From(Node x) 
		{
			if (x.Equals(_root)) 
			{
				return true;
			}

			if (Trace.AssertEnabled) 
			{
				Trace.Assert(x.GetParent() != null);
			}

			return x.Equals(x.GetParent().GetLeft());
		}

		private Node Search(object[] d) 
		{
			Node x = _root;

			while (x != null) 
			{
				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				int c = CompareRow(d, x.GetData());

				if (c == 0) 
				{
					return x;
				} 
				else if (c < 0) 
				{
					x = x.GetLeft();
				} 
				else 
				{
					x = x.GetRight();
				}
			}

			return null;
		}

		// todo: this is a hack
		private int CompareRowNonUnique(object[] a, object[] b) 
		{
			int i = Column.Compare(a[_column_0], b[_column_0], _type_0);

			if (i != 0) 
			{
				return i;
			}

			for (int j = 1; j < _fields - (_unique ? 0 : 1); j++) 
			{
				i = Column.Compare(a[_column[j]], b[_column[j]], _type[j]);

				if (i != 0) 
				{
					return i;
				}
			}

			return 0;
		}

		private int CompareRow(object[] a, object[] b) 
		{
			int i = Column.Compare(a[_column_0], b[_column_0], _type_0);

			if (i != 0) 
			{
				return i;
			}

			for (int j = 1; j < _fields; j++) 
			{
				i = Column.Compare(a[_column[j]], b[_column[j]], _type[j]);

				if (i != 0) 
				{
					return i;
				}
			}

			return 0;
		}

		private int CompareValue(object a, object b) 
		{
			return Column.Compare(a, b, _type_0);
		}
	}
}
