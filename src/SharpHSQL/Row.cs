#region Usings
using System;
using System.Collections;
using System.IO;
#endregion

#region License
/*
 * Row.cs
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
	/// Row class.
	/// </summary>
	sealed class Row 
	{
		private object[] oData;
		private Table  tTable;    // null: memory row; otherwise: cached table
		private Node   nFirstIndex;

		// only required for cached table
		public static int     CurrentAccess = 0;

		// todo: use int iLastChecked;
		public int		   LastAccess;
		public Row		   Last, Next;
		public int		   Pos;
		public int		   Size;
		public bool        Changed;

		public Row(Table table, object[] o) 
		{
			tTable = table;

			int index = tTable.IndexCount;

			nFirstIndex = new Node(this, 0);

			Node n = nFirstIndex;

			for (int i = 1; i < index; i++) 
			{
				n.nNext = new Node(this, i);
				n = n.nNext;
			}

			oData = o;

			if (tTable != null && tTable.cCache != null) 
			{
				LastAccess = CurrentAccess++;

				// todo: 32 bytes overhead for each index + iSize, iPos
				Size = 8 + Column.GetSize(o, tTable)
					+ 16 * tTable.IndexCount;
				//((iSize + 7) / 8) * 8;    // align to 8 byte blocks

				tTable.cCache.Add(this);
			}

			Changed = true;
		}

		internal Row(Table t, BinaryReader din, int pos, Row before)  
		{
			tTable = t;

			int index = tTable.IndexCount;

			Pos = pos;
			nFirstIndex = new Node(this, din, 0);

			Node n = nFirstIndex;

			for (int i = 1; i < index; i++) 
			{
				n.nNext = new Node(this, din, i);
				n = n.nNext;
			}

			int l = tTable.InternalColumnCount;

			oData = Column.ReadData(din, l);

			int iCurrent = din.ReadInt32();

			LogHelper.Publish( String.Format("Row read with {0} columns. Row read position from file: {1}. Current position: {2}.", oData.Length, iCurrent, Pos ), LogHelper.LogEntryType.Debug );

			Trace.Check(iCurrent == Pos, Trace.INPUTSTREAM_ERROR);
			Insert(before);

			LastAccess = CurrentAccess++;
		}

		public void CleanUpCache() 
		{
			if (tTable != null && tTable.cCache != null) 
			{

				// so that this row is not cleaned
				LastAccess = CurrentAccess++;

				tTable.cCache.CleanUp();
			}
		}

		public void RowChanged() 
		{
			Changed = true;
			LastAccess = CurrentAccess++;
		}

		public Node GetNode(int pos, int index) 
		{
			Row r = tTable.cCache.GetRow(pos, tTable);

			r.LastAccess = CurrentAccess++;

			return r.GetNode(index);
		}

		private Row GetRow(int pos) 
		{
			return tTable.cCache.GetRow(pos, tTable);
		}

		public Node GetNode(int index) 
		{
			Node n = nFirstIndex;

			while (index-- > 0) 
			{
				n = n.nNext;
			}

			return n;
		}

		public object[] GetData() 
		{
			LastAccess = CurrentAccess++;

			return oData;
		}

		public void Insert(Row before) 
		{
			if (before == null) 
			{
				Next = this;
				Last = this;
			} 
			else 
			{
				Next = before;
				Last = before.Last;
				before.Last = this;
				Last.Next = this;
			}
		}

		public bool CanRemove() 
		{
			Node n = nFirstIndex;

			while (n != null) 
			{
				if (Trace.AssertEnabled) 
				{
					Trace.Assert(n.iBalance != -2);
				}

				if (Trace.StopEnabled) 
				{
					Trace.Stop();
				}

				if (n.iParent == 0 && n.nParent == null) 
				{
					return true;
				}

				n = n.nNext;
			}

			return false;
		}

		public byte[] Write() 
		{
			MemoryStream stream = new MemoryStream(Size);
			BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.Unicode);

			writer.Write(Size);
			nFirstIndex.Write(writer);
			Column.WriteData(writer, oData, tTable);
			writer.Write(Pos);

			Changed = false;
			return stream.ToArray();
		}

		public void Delete() 
		{
			if (tTable != null && tTable.cCache != null) 
			{
				Changed = false;

				tTable.cCache.Free(this, Pos, Size);
			}
		}

		public void Free() 
		{
			Last.Next = Next;
			Next.Last = Last;

			if (Next == this) 
			{
				Next = Last = null;
			}
		}
	}
}
