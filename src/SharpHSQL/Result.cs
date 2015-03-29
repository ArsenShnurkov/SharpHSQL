#region Usings
using System;
using System.Collections;
using System.IO;
#endregion

#region License
/*
 * Result.cs
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
	/// Class used for results transport resulting of a query execution.
	/// </summary>
	public sealed class Result 
	{
		#region Private Vars

		private Record       _tailRecord;
		private int          _size;
		private int          _columnCount;
		private ResultType   _mode;
		private string	     _error;
		private int		     _updateCount;
		private Record	     _root;
		private string[]	 _label;
		private string[]	 _table;
		private string[]	 _name;
		private ColumnType[] _type;

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		internal Result() 
		{
			_mode = ResultType.UpdateCount;
			_updateCount = 0;
		}

		/// <summary>
		/// Result constructor with error.
		/// </summary>
		/// <param name="error"></param>
		internal Result(string error) 
		{
			_mode = ResultType.Error;
			_error = error;
		}

		/// <summary>
		/// Result constructor with columns.
		/// </summary>
		/// <param name="columns"></param>
		internal Result(int columns) 
		{
			PrepareData(columns);

			_columnCount = columns;
		}

		/// <summary>
		/// Constructor for a result based on a byte stream.
		/// </summary>
		/// <param name="b"></param>
		internal Result(byte[] b) 
		{
			MemoryStream bin = new MemoryStream(b);
			BinaryReader din = new BinaryReader(bin);

			try 
			{
				#if !POCKETPC
				_mode = (ResultType)Enum.Parse( typeof(ResultType), din.ReadInt32().ToString() );
				#else
				_mode = (ResultType)OpenNETCF.EnumEx.Parse( typeof(ResultType), din.ReadInt32().ToString() );
				#endif

				if (_mode == ResultType.Error) 
				{
					throw Trace.GetError(din.ReadString());
				} 
				else if (_mode == ResultType.UpdateCount) 
				{
					_updateCount = din.ReadInt32();
				} 
				else if (_mode == ResultType.Data) 
				{
					int l = din.ReadInt32();

					PrepareData(l);

					_columnCount = l;

					for (int i = 0; i < l; i++) 
					{
						#if !POCKETPC
						Type[i] = (ColumnType)Enum.Parse(typeof(ColumnType), din.ReadInt32().ToString() );
						#else
						Type[i] = (ColumnType)OpenNETCF.EnumEx.Parse(typeof(ColumnType), din.ReadInt32().ToString() );
						#endif

						Label[i] = din.ReadString();
						Table[i] = din.ReadString();
						Name[i] = din.ReadString();
					}

					while (din.PeekChar() != -1) 
					{
						Add(Column.ReadData(din, l));
					}
				}
			} 
			catch (Exception e) 
			{
				LogHelper.Publish( "Unexpected error on Result.", e );
				Trace.Error(Trace.TRANSFER_CORRUPTED);
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Get the column names in the result.
		/// </summary>
		public string[] Name
		{
			get
			{
				return _name;
			}
		}

		/// <summary>
		/// Array containing the table name for each column.
		/// </summary>
		public string[] Table
		{
			get
			{
				return _table;
			}
		}

		/// <summary>
		/// Array containing the column labels.
		/// </summary>
		public string[] Label
		{
			get
			{
				return _label;
			}
		}

		/// <summary>
		/// Array containing the data type for each column.
		/// </summary>
		public ColumnType[] Type
		{
			get
			{
				return _type;
			}
		}

		/// <summary>
		/// Get the root record for this result.
		/// </summary>
		public Record Root
		{
			get
			{
				return _root;
			}
		}

		/// <summary>
		/// Get the updated rows count.
		/// </summary>
		public int UpdateCount
		{
			get
			{
				return _updateCount;
			}
		}

		/// <summary>
		/// Get the result type.
		/// </summary>
		public ResultType Mode
		{
			get
			{
				return _mode;
			}
		}

		/// <summary>
		/// Get or set the error description.
		/// </summary>
		public string Error
		{
			get
			{
				return _error;
			}
		}

		/// <summary>
		/// Get the result size.
		/// </summary>
		public int Size
		{
			get
			{
				return _size;
			}
		}

		/// <summary>
		/// Get the column count in the result.
		/// </summary>
		public int ColumnCount 
		{
			get
			{
				return _columnCount;
			}
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Sets the result root record.
		/// </summary>
		/// <param name="root"></param>
		internal void SetRoot( Record root )
		{
			_root = root;
		}

		/// <summary>
		/// Sets the update count.
		/// </summary>
		/// <param name="count"></param>
		internal void SetUpdateCount( int count )
		{
			_updateCount = count;
		}

		/// <summary>
		/// Sets the column count.
		/// </summary>
		/// <param name="count"></param>
		internal void SetColumnCount( int count )
		{
			_columnCount = count;
		}

		/// <summary>
		/// Append a result to this results.
		/// </summary>
		/// <param name="result"></param>
		internal void Append(Result result) 
		{
			if (_root == null) 
			{
				_root = result.Root;
			} 
			else 
			{
				_tailRecord.Next = result.Root;
			}

			_tailRecord = result._tailRecord;
			_size += result._size;
		}

		/// <summary>
		/// Add a row to this results.
		/// </summary>
		/// <param name="d"></param>
		internal void Add(object[] d) 
		{
			Record r = new Record();

			r.Data = d;

			if (_root == null) 
			{
				_root = r;
			} 
			else 
			{
				_tailRecord.Next = r;
			}

			_tailRecord = r;
			_size++;
		}

		/// <summary>
		/// Serializes the result into a byte array.
		/// </summary>
		/// <returns></returns>
		internal byte[] GetBytes() 
		{
			MemoryStream stream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(stream);

			try 
			{
				writer.Write((int)_mode);

				if (_mode == ResultType.UpdateCount) 
				{
					writer.Write(_updateCount);
				} 
				else if (_mode == ResultType.Error) 
				{
					writer.Write(_error);
				} 
				else 
				{
					int l = _columnCount;

					writer.Write(l);

					Record n = _root;

					for (int i = 0; i < l; i++) 
					{
						writer.Write((int)Type[i]);
						writer.Write(Label[i]);
						writer.Write(Table[i]);
						writer.Write(Name[i]);
					}

					while (n != null) 
					{
						Column.WriteData(writer, l, Type, n.Data);

						n = n.Next;
					}
				}

				return stream.ToArray();
			} 
			catch (Exception e) 
			{
				LogHelper.Publish( "Unexpected error on getBytes.", e );
				throw Trace.Error(Trace.TRANSFER_CORRUPTED);
			}
		}

		#endregion

		#region Private Methods

		private void PrepareData(int columns) 
		{
			_mode = ResultType.Data;
			_label = new string[columns];
			_table = new string[columns];
			_name = new string[columns];
			_type = new ColumnType[columns];
		}

		#endregion

	}
}
