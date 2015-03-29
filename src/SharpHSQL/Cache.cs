#region Usings
using System;
using System.Collections;
using System.IO;
#endregion

#region License
/*
 * Cache.cs
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
	/// Cache class declaration.
	/// The cache class implements the handling of cached tables.
	/// <seealso cref="Row"/>
	/// <seealso cref="CacheFree"/>
	/// </summary>
	/// <remarks>version 1.0.0.1</remarks>
	sealed class Cache 
	{
		#region Private Constants
		private const int LENGTH = 1 << 14;
		private const int MAX_CACHE_SIZE = LENGTH * 3 / 4;
		private const int MASK = (LENGTH) - 1;
		private const int INITIAL_FREE_POS = 4;
		private const int MAX_FREE_COUNT = 1024;
		private const int FREE_POS_POS = 0;    // where _freePos is saved
		#endregion

		#region Private Vars

		private FileStream _fileStream;
		private Row[]      _rowData;
		private Row[]	   _rowWriter;
		private Row	       _rowFirst;		// must point to one of _rowData[]
		private Row		   _rowLastChecked;	// can be any row
		private string	   _name;	
		private int		   _freePos;		
		private CacheFree  _cacheRoot;
		private int	       _freeCount;
		private int	       _cacheSize;
		#endregion

		#region Constructors

		/// <summary>
		/// Cache constructor declaration.
		/// The cache constructor sets up the initial parameters of the cache
		/// object, setting the name used for the file, etc.
		/// </summary>
		/// <param name="name">The name of database file</param>
		public Cache(string name) 
		{
			_name = name;
			_rowData = new Row[LENGTH];
			_rowWriter = new Row[LENGTH];
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Open method declaration.
		/// The open method creates or opens a database file.
		/// </summary>
		/// <param name="readOnly">Flag that indicates if this cache is readonly</param>
		public void Open(bool readOnly) 
		{
			try 
			{
				bool exists = false;
				FileInfo    f = new FileInfo(_name);

				if (f.Exists && f.Length > FREE_POS_POS) 
				{
					exists = true;
				}

				if (readOnly)
				{
					_fileStream = new FileStream(_name, System.IO.FileMode.OpenOrCreate,FileAccess.Read);	
				}
				else
				{
					_fileStream = new FileStream(_name, System.IO.FileMode.OpenOrCreate,FileAccess.ReadWrite);	
				}
				
				if (exists) 
				{
					_fileStream.Seek(FREE_POS_POS,SeekOrigin.Begin);

					BinaryReader b = new BinaryReader(_fileStream);

					_freePos = b.ReadInt32();
				} 
				else 
				{
					_freePos = INITIAL_FREE_POS;
				}
			} 
			catch (Exception e) 
			{
				throw Trace.Error(Trace.FILE_IO_ERROR, "error " + e + " opening " + _name);
			}
		}

		/// <summary>
		/// Flush method declaration.
		/// The flush method saves all cached data to the file, saves the free position
		/// and closes the file.
		/// </summary>
		public void Flush() 
		{
			try 
			{
				_fileStream.Seek(FREE_POS_POS,SeekOrigin.Begin);
				BinaryWriter b = new BinaryWriter(_fileStream);
				b.Write(_freePos);
				SaveAll();
				_fileStream.Close();
			} 
			catch (Exception e) 
			{
				throw Trace.Error(Trace.FILE_IO_ERROR, "error " + e + " closing " + _name);
			}
		}

		/// <summary>
		/// Shutdown method declaration.
		/// The shutdown method closes the cache file.
		/// It does not flush pending writes.
		/// </summary>
		public void Shutdown() 
		{
			try 
			{
				_fileStream.Close();
			} 
			catch (Exception e) 
			{
				throw Trace.Error(Trace.FILE_IO_ERROR, "error " + e + " in shutdown " + _name);
			}
		}

		/// <summary>
		/// Free method declaration.
		/// This method marks space in the database file as free.
		/// </summary>
		/// <remarks>
		/// If more than MAX_FREE_COUNT free positios then
		/// they are probably all are too small anyway; so we start a new list.
		/// TODO: This is wrong when deleting lots of records.
		/// </remarks>
		/// 
		/// <param name="row">Row object to be marked free</param>
		/// <param name="pos">Offset in the file this Row was stored at</param>
		/// <param name="length">Size of the Row object to free</param>
		public void Free(Row row, int pos, int length) 
		{
			_freeCount++;

			CacheFree n = new CacheFree();

			n.Pos = pos;
			n.Length = length;

			if (_freeCount > MAX_FREE_COUNT) 
				_freeCount = 0;
			else 
				n.Next = _cacheRoot;

			_cacheRoot = n;

			// it's possible to remove roots to
			Remove(row);
		}

		/// <summary>
		/// Add method declaration.
		/// This method adds a Row to the Cache.  It walks the
		/// list of CacheFree objects to see if there is available space
		/// to store the new Row, reusing space if it exists, otherwise
		/// we grow the file.
		/// </summary>
		/// <param name="row">Row to be added to Cache</param>
		public void Add(Row row) 
		{
			int       size = row.Size;
			CacheFree f = _cacheRoot;
			CacheFree last = null;
			int       i = _freePos;

			while (f != null) 
			{
				if (Trace.TraceEnabled) 
				{
					Trace.Stop();
				}
				// first that is long enough
				if (f.Length >= size) 
				{
					i = f.Pos;
					size = f.Length - size;

					if (size < 8) 
					{
						// remove almost empty blocks
						if (last == null) 
						{
							_cacheRoot = f.Next;
						} 
						else 
						{
							last.Next = f.Next;
						}

						_freeCount--;
					} 
					else 
					{
						f.Length = size;
						f.Pos += row.Size;
					}

					break;
				}

				last = f;
				f = f.Next;
			}

			row.Pos = i;

			if (i == _freePos) 
			{
				_freePos += size;
			}

			int k = i & MASK;
			Row before = _rowData[k];

			if (before == null) 
			{
				before = _rowFirst;
			}

			row.Insert(before);

			_cacheSize++;
			_rowData[k] = row;
			_rowFirst = row;
		}

		/// <summary>
		/// GetRow method declaration.
		/// This method reads a Row object from the cache.
		/// </summary>
		/// <param name="pos">Offset of the requested Row in the cache</param>
		/// <param name="table">Table this Row belongs to</param>
		/// <returns>The Row object as read from the cache.</returns>
		public Row GetRow(int pos, Table table) 
		{
			int k = pos & MASK;
			Row r = _rowData[k];
			Row start = r;

			while (r != null) 
			{
				if (Trace.StopEnabled) 
					Trace.Stop();

				int p = r.Pos;

				if (p == pos) 
					return r;
				else if ((p & MASK) != k) 
					break;

				r = r.Next;

				if (r == start) 
					break;
			}

			Row before = _rowData[k];

			if (before == null) 
			{
				before = _rowFirst;
			}

			try 
			{
				LogHelper.Publish( String.Format("Retrieving row at position: {0}.", pos ), LogHelper.LogEntryType.Debug );

				_fileStream.Seek(pos, SeekOrigin.Begin);

				BinaryReader b = new BinaryReader(_fileStream);

				int  size = b.ReadInt32();
				byte[] buffer = new byte[size];

				buffer = b.ReadBytes(size);

				LogHelper.Publish( String.Format("Row Size: {0}. Retrieved {1} bytes.", size, buffer.Length ), LogHelper.LogEntryType.Debug );

				MemoryStream bin = new MemoryStream(buffer);
				BinaryReader din = new BinaryReader(bin, System.Text.Encoding.Unicode);

				r = new Row(table, din, pos, before);
				r.Size = size;
			} 
			catch (IOException e) 
			{
				#if !POCKETPC
				Console.WriteLine(e.StackTrace);
				#endif

				throw Trace.Error(Trace.FILE_IO_ERROR, "reading: " + e);
			}

			_cacheSize++;
			_rowData[k] = r;
			_rowFirst = r;

			return r;
		}
		
		/// <summary>
		/// CleanUp method declaration.
		/// This method cleans up the cache when it grows too large. It works by
		/// checking Rows in held in the Cache's iLastAccess member and removing
		/// Rows that haven't been accessed in the longest time.
		/// </summary>
		public void CleanUp() 
		{
			if (_cacheSize < MAX_CACHE_SIZE) 
				return;

			int count = 0, j = 0;

			while (j++ < LENGTH && _cacheSize + LENGTH > MAX_CACHE_SIZE && 
				(count * 16) < LENGTH) 
			{
				if (Trace.StopEnabled) 
					Trace.Stop();

				Row r = GetWorst();

				if (r == null) 
					return;

				if (r.Changed) 
				{
					_rowWriter[count++] = r;
				} 
				else 
				{

					// here we can't remove roots
					if (!r.CanRemove()) 
					{
						Remove(r);
					}
				}
			}

			if (count != 0) 
				SaveSorted(count);

			for (int i = 0; i < count; i++) 
			{
				// here we can't remove roots
				Row r = _rowWriter[i];

				if (!r.CanRemove()) 
					Remove(r);

				_rowWriter[i] = null;
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Remove method declaration.
		/// This method is used to remove Rows from the Cache. It is called
		/// by the CleanUp method.
		/// </summary>
		/// <param name="row">Row to be removed</param>
		private void Remove(Row row) 
		{
			if (Trace.AssertEnabled) 
				Trace.Assert(!row.Changed);

			// make sure _rowLastChecked does not point to r
			if (row == _rowLastChecked) 
			{
				_rowLastChecked = _rowLastChecked.Next;

				if (_rowLastChecked == row) 
					_rowLastChecked = null;
			}

			// make sure _rowData[k] does not point here
			int k = row.Pos & MASK;

			if (_rowData[k] == row) 
			{
				Row n = row.Next;

				_rowFirst = n;

				if (n == row || (n.Pos & MASK) != k) 
					n = null;

				_rowData[k] = n;
			}

			// make sure _rowFirst does not point here
			if (row == _rowFirst) 
			{
				_rowFirst = _rowFirst.Next;

				if (row == _rowFirst) 
					_rowFirst = null;
			}

			row.Free();

			_cacheSize--;
		}

		/// <summary>
		/// GetWorst method declaration.
		/// This method finds the Row with the smallest (oldest) iLastAccess member.
		/// Called by the Cleanup method.
		/// </summary>
		/// <returns>The selected Row object</returns>
		private Row GetWorst() 
		{
			if (_rowLastChecked == null) 
				_rowLastChecked = _rowFirst;

			Row r = _rowLastChecked;

			if (r == null) 
				return null;

			Row candidate = r;
			int worst = Row.CurrentAccess;

			// algorithm: check the next rows and take the worst
			for (int i = 0; i < 6; i++) 
			{
				int w = r.LastAccess;

				if (w < worst) 
				{
					candidate = r;
					worst = w;
				}

				r = r.Next;
			}

			_rowLastChecked = r.Next;

			return candidate;
		}

		/// <summary>
		/// </summary>
		private void SaveAll() 
		{
			if (_rowFirst == null) 
				return;

			Row r = _rowFirst;

			while (true) 
			{
				int count = 0;
				Row begin = r;

				do 
				{
					if (Trace.StopEnabled) 
						Trace.Stop();

					if (r.Changed) 
						_rowWriter[count++] = r;

					r = r.Next;
				} while (r != begin && count < LENGTH);

				if (count == 0) 
					return;

				SaveSorted(count);

				for (int i = 0; i < count; i++) 
				{
					_rowWriter[i] = null;
				}
			}
		}


		/// <summary>
		/// </summary>
		private void SaveSorted(int count) 
		{
			if (count < 1)
				return;

			Sort(_rowWriter, 0, count - 1);

			try 
			{
				BinaryWriter b = new BinaryWriter(_fileStream);

				LogHelper.Publish( "Saving rows to cache.", LogHelper.LogEntryType.Debug );

				for (int i = 0; i < count; i++) 
				{
					LogHelper.Publish( String.Format("Writing row number {0}. File at position: {1}.", i, _fileStream.Position ), LogHelper.LogEntryType.Debug );

					if( _fileStream.Position < _rowWriter[i].Pos )
						_fileStream.Seek(_rowWriter[i].Pos,SeekOrigin.Begin);

					if( _fileStream.Position > _rowWriter[i].Pos )
						Trace.Error( Trace.INPUTSTREAM_ERROR );

					byte[] row = _rowWriter[i].Write();

					Trace.Check( (row.Length == _rowWriter[i].Size), Trace.SERIALIZATION_FAILURE );

					LogHelper.Publish( String.Format("Byte array size: {0}. Row Pos: {1}. Row Size: {2}", row.Length, _rowWriter[i].Pos, _rowWriter[i].Size ), LogHelper.LogEntryType.Debug );

					b.Write(row);
				}
			} 
			catch (Exception e) 
			{
				throw Trace.Error(Trace.FILE_IO_ERROR, "saveSorted " + e);
			}
		}


		/// <summary>
		/// </summary>
		private static void Sort(Row[] w, int l, int r) 
		{
			int i, j, p;

			while (r - l > 10) 
			{
				i = (r + l) >> 1;

				if (w[l].Pos > w[r].Pos) 
				{
					Swap(w, l, r);
				}

				if (w[i].Pos < w[l].Pos) 
				{
					Swap(w, l, i);
				} 
				else if (w[i].Pos > w[r].Pos) 
				{
					Swap(w, i, r);
				}

				j = r - 1;

				Swap(w, i, j);

				p = w[j].Pos;
				i = l;

				while (true) 
				{
					if (Trace.StopEnabled) 
						Trace.Stop();

					while (w[++i].Pos < p);

					while (w[--j].Pos > p);

					if (i >= j) 
						break;

					Swap(w, i, j);
				}

				Swap(w, i, r - 1);
				Sort(w, l, i - 1);

				l = i + 1;
			}

			for (i = l + 1; i <= r; i++) 
			{
				if (Trace.StopEnabled) 
					Trace.Stop();

				Row t = w[i];

				for (j = i - 1; j >= l && w[j].Pos > t.Pos; j--) 
				{
					w[j + 1] = w[j];
				}

				w[j + 1] = t;
			}
		}

		/// <summary>
		/// </summary>
		private static void Swap(Row[] w, int a, int b) 
		{
			Row t = w[a];

			w[a] = w[b];
			w[b] = t;
		}

		#endregion
	}
}
