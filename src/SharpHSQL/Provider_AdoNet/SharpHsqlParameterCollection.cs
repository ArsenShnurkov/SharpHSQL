#region Usings
using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using SharpHsql;
using System.Collections.Generic;
using System.Linq;


#endregion

#region License
/*
 * SharpHsqlParameterCollection.cs
 *
 * Copyright (c) 2004, Andres G Vettori
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
 * C# SharpHsql ADO.NET Provider by Andr¨¦s G Vettori.
 * http://workspaces.gotdotnet.com/sharphsql
 */
#endregion

namespace System.Data.Hsql
{
	public class DbParameterCollection<TParameter> : DbParameterCollection
		where TParameter : DbParameter
	{
		List<TParameter> parameters = new List<TParameter> ();

		public DbParameterCollection ()
		{
		}

		public override int Count {get {return parameters.Count;}}
		public override bool IsFixedSize {get {return false;}}
		public override bool IsReadOnly {get {return false;}}
		public override bool IsSynchronized {get {return false;}}
		public override object SyncRoot {get {return parameters;}}

		public override int Add (object value)
		{
			if (!(value is TParameter))
				throw new ArgumentException ("wrong type", "value");
			parameters.Add ((TParameter) value);
			return parameters.Count-1;
		}

		public override void AddRange (Array values)
		{
			foreach (TParameter p in values)
				Add (p);
		}

		public override void Clear ()
		{
			parameters.Clear ();
		}

		public override bool Contains (object value)
		{
			return parameters.Contains ((TParameter) value);
		}

		public override bool Contains (string value)
		{
			return parameters.Any (p => p.ParameterName == value);
		}

		public override void CopyTo (Array array, int index)
		{
			((ICollection) parameters).CopyTo (array, index);
		}

		public override IEnumerator GetEnumerator ()
		{
			return parameters.GetEnumerator ();
		}

		public override int IndexOf (object value)
		{
			return parameters.IndexOf ((TParameter) value);
		}

		public override int IndexOf (string value)
		{
			for (int i = 0; i < parameters.Count; ++i)
				if (parameters [i].ParameterName == value)
					return i;
			return -1;
		}

		public override void Insert (int index, object value)
		{
			parameters.Insert (index, (TParameter) value);
		}

		public override void Remove (object value)
		{
			parameters.Remove ((TParameter) value);
		}

		public override void RemoveAt (int index)
		{
			parameters.RemoveAt (index);
		}

		public override void RemoveAt (string value)
		{
			int idx = IndexOf (value);
			if (idx >= 0)
				parameters.RemoveAt (idx);
		}

		protected override DbParameter GetParameter (int index)
		{
			return parameters [index];
		}

		protected override DbParameter GetParameter (string value)
		{
			return parameters.Where (p => p.ParameterName == value)
				.FirstOrDefault ();
		}

		protected override void SetParameter (int index, DbParameter value)
		{
			parameters [index] = (TParameter) value;
		}

		protected override void SetParameter (string index, DbParameter value)
		{
			parameters [IndexOf (value)] = (TParameter) value;
		}
	}

	/// <summary>
	/// Parameter Collection class for Hsql ADO.NET data provider.
	/// <seealso cref="SharpHsqlParameter"/>
	/// <seealso cref="SharpHsqlCommand"/>
	/// </summary>
	/// <remarks>Not serializable on Compact Framework 1.0</remarks>
	public sealed class SharpHsqlParameterCollection : DbParameterCollection<SharpHsqlParameter>
	{
		SharpHsqlCommand _cmd;
		public SharpHsqlParameterCollection(SharpHsqlCommand cmd)
		{
			_cmd = cmd;
		}
		/// <summary>
		///  Get or set parameters by index.
		/// </summary>
		public SharpHsqlParameter this[int index]
		{
			get
			{
				return (SharpHsqlParameter)base[index];
			}
			set
			{
				base[index] = value;
				//_names[((SharpHsqlParameter)value).ParameterName] = index;
			}
		}
	}
}