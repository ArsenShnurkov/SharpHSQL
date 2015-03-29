#region Usings
using System;
using System.Collections;
using System.IO;
#endregion

#region License
/*
 * Node.cs
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
	/// Node class.
	/// </summary>
	sealed class Node 
	{
		internal int		iBalance;    // currently, -2 means 'deleted'
		internal int		iLeft, iRight, iParent;
		internal Node		nLeft, nRight, nParent;
		internal Node		nNext;    // node of next index (nNext==null || nNext.iId=iId+1)
		internal Row		rData;

		private int iId;	     // id of index this table

		public Node(Row r, BinaryReader din, int id) 
		{
			iId = id;
			rData = r;
			iBalance = din.ReadInt32();
			iLeft = din.ReadInt32();
			iRight = din.ReadInt32();
			iParent = din.ReadInt32();

			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}
		}

		public Node(Row r, int id) 
		{
			iId = id;
			rData = r;
		}

		public void Delete() 
		{
			iBalance = -2;
			nLeft = nRight = nParent = null;
			iLeft = iRight = iParent = 0;
		}

		public int GetKey() 
		{
			return rData.Pos;
		}

		public Node GetLeft() 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}

			if (iLeft == 0) 
			{
				return nLeft;
			}

			// rData.iLastAccess=Row.iCurrentAccess++;
			return rData.GetNode(iLeft, iId);
		}

		public void SetLeft(Node n) 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}

			rData.RowChanged();

			if (n == null) 
			{
				iLeft = 0;
				nLeft = null;
			} 
			else if (n.rData.Pos != 0) 
			{
				iLeft = n.rData.Pos;
			} 
			else 
			{
				nLeft = n;
			}
		}

		public Node GetRight() 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}

			if (iRight == 0) 
			{
				return nRight;
			}

			// rData.iLastAccess=Row.iCurrentAccess++;
			return rData.GetNode(iRight, iId);
		}

		public void SetRight(Node n) 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}

			rData.RowChanged();

			if (n == null) 
			{
				iRight = 0;
				nRight = null;
			} 
			else if (n.rData.Pos != 0) 
			{
				iRight = n.rData.Pos;
			} 
			else 
			{
				nRight = n;
			}
		}

		public Node GetParent() 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}

			if (iParent == 0) 
			{
				return nParent;
			}

			// rData.iLastAccess=Row.iCurrentAccess++;
			return rData.GetNode(iParent, iId);
		}

		public void SetParent(Node n) 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}

			rData.RowChanged();

			if (n == null) 
			{
				iParent = 0;
				nParent = null;
			} 
			else if (n.rData.Pos != 0) 
			{
				iParent = n.rData.Pos;
			} 
			else 
			{
				nParent = n;
			}
		}

		public int GetBalance() 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);

				// rData.iLastAccess=Row.iCurrentAccess++;
			}

			return iBalance;
		}

		public void SetBalance(int b) 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}

			if (iBalance != b) 
			{
				rData.RowChanged();

				iBalance = b;
			}
		}

		public object[] GetData() 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}

			return rData.GetData();
		}

		public bool Equals(Node node) 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);

				// rData.iLastAccess=Row.iCurrentAccess++;
			}

			if (Trace.AssertEnabled) 
			{
				if (node != this) 
				{
					Trace.Assert(rData.Pos == 0 || node == null
						|| node.rData.Pos != rData.Pos);
				} 
				else 
				{
					Trace.Assert(node.rData.Pos == rData.Pos);
				}
			}

			return node == this;
		}

		public void Write(BinaryWriter writer) 
		{
			if (Trace.AssertEnabled) 
			{
				Trace.Assert(iBalance != -2);
			}

			writer.Write(iBalance);
			writer.Write(iLeft);
			writer.Write(iRight);
			writer.Write(iParent);

			if (nNext != null) 
			{
				nNext.Write(writer);
			}
		}
	}
}
