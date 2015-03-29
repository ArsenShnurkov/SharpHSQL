#region Usings
using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using SharpHsql;
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
 * C# SharpHsql ADO.NET Provider by Andrés G Vettori.
 * http://workspaces.gotdotnet.com/sharphsql
 */
#endregion

namespace System.Data.Hsql
{
	/// <summary>
	/// Parameter Collection class for Hsql ADO.NET data provider.
	/// <seealso cref="SharpHsqlParameter"/>
	/// <seealso cref="SharpHsqlCommand"/>
	/// </summary>
	/// <remarks>Not serializable on Compact Framework 1.0</remarks>
	#if !POCKETPC
	[Serializable]
	#endif
	public sealed class SharpHsqlParameterCollection : CollectionBase, IDataParameterCollection
	{
		#region Constructors

		/// <summary>
		/// Internal Constructor.
		/// </summary>
		/// <param name="command"></param>
		internal SharpHsqlParameterCollection( SharpHsqlCommand command ) : base()
		{
			_command = command;
			_names = new Hashtable();
		}

		#endregion

		#region IDataParameterCollection Members

		/// <summary>
		/// Get or set a <see cref="SharpHsqlParameter"/> object by name.
		/// </summary>
		public object this[string parameterName]
		{
			get
			{
				int index = GetParameterIndex( parameterName );

				if( index > -1 )
					return base.InnerList[index];
				else
					return null;
			}
			set
			{
				int index = GetParameterIndex( parameterName );

				if( index > -1 )
					base.InnerList[index] = value;
				else
				{
					index = base.InnerList.Add(value);
					_names[parameterName] = index;
				}
			}
		}

		/// <summary>
		/// Remove the parameter from the collection.
		/// </summary>
		/// <param name="parameterName">The parameter name to remove.</param>
		public void RemoveAt(string parameterName)
		{
			int index = GetParameterIndex( parameterName );

			if( index > -1 )
				base.InnerList.RemoveAt(index);

			RebuildNames();
		}

		/// <summary>
		/// Look for a parameter in the collection.
		/// </summary>
		/// <param name="parameterName">The parameter name to remove.</param>
		/// <returns>True if the parameter is found.</returns>
		public bool Contains(string parameterName)
		{
			int index = GetParameterIndex( parameterName );

			if( index > -1 )
				return true;
			else
				return false;
		}

		/// <summary>
		/// Obtains the parameter index in the collection.
		/// </summary>
		/// <param name="parameterName">The parameter name to found.</param>
		/// <returns>The index of the parameter.</returns>
		public int IndexOf(string parameterName)
		{
			return GetParameterIndex( parameterName );
		}

		#endregion

		#region IList Members

		/// <summary>
		/// Get the updatability of the collection.
		/// </summary>
		public bool IsReadOnly
		{
			get
			{
				return base.InnerList.IsReadOnly;
			}
		}

		/// <summary>
		/// Get or set parameters by index.
		/// </summary>
		object System.Collections.IList.this[int index]
		{
			get
			{
				return base.InnerList[index];
			}
			set
			{
				base.InnerList[index] = value;
				_names[((SharpHsqlParameter)value).ParameterName] = index;
			}
		}

		/// <summary>
		///  Get or set parameters by index.
		/// </summary>
		public SharpHsqlParameter this[int index]
		{
			get
			{
				return (SharpHsqlParameter)base.InnerList[index];
			}
			set
			{
				base.InnerList[index] = value;
				_names[((SharpHsqlParameter)value).ParameterName] = index;
			}
		}

		/// <summary>
		/// Removes a parameter by index.
		/// </summary>
		/// <param name="index">The parameter index to remove.</param>
		void System.Collections.IList.RemoveAt(int index)
		{
			base.InnerList.RemoveAt(index);
			RebuildNames();
		}

		/// <summary>
		/// Inserts a new parameter at a specific location.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		public void Insert(int index, object value)
		{
			base.InnerList.Insert(index, value);
			RebuildNames();
		}

		/// <summary>
		/// Remove the passed parameter from the collection.
		/// </summary>
		/// <param name="value"></param>
		public void Remove(object value)
		{
			base.InnerList.Remove(value);
			_names.Remove(((SharpHsqlParameter)value).ParameterName);
		}

		/// <summary>
		/// Looks for a parameter in the collection.
		/// </summary>
		/// <param name="value">The paramerter object to find.</param>
		/// <returns>True if the parameter is found.</returns>
		bool System.Collections.IList.Contains(object value)
		{
			return base.InnerList.Contains(value);
		}

		/// <summary>
		/// Eliminates all parameters from the collection.
		/// </summary>
		void System.Collections.IList.Clear()
		{
			base.InnerList.Clear();
			_names.Clear();
		}

		/// <summary>
		/// Get the index of the passed parameter in the collection.
		/// </summary>
		/// <param name="value">The paramerter object to find.</param>
		/// <returns>The index of the parameter.</returns>
		int System.Collections.IList.IndexOf(object value)
		{
			return base.InnerList.IndexOf(value);
		}

		/// <summary>
		/// Adds a new parameter to the collection.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public int Add(object value)
		{
			int index = base.InnerList.Add(value);
			_names[((SharpHsqlParameter)value).ParameterName] = index;
			return index;
		}

		/// <summary>
		/// Returns the grow policy for this collection.
		/// </summary>
		public bool IsFixedSize
		{
			get
			{
				return base.InnerList.IsFixedSize;
			}
		}

		#endregion

		#region ICollection Members

		/// <summary>
		/// Returns the synchronization status of this collection.
		/// </summary>
		public bool IsSynchronized
		{
			get
			{
				return base.InnerList.IsSynchronized;
			}
		}

		/// <summary>
		/// Returns the parameter count for this collection.
		/// </summary>
		int ICollection.Count
		{
			get
			{
				return base.InnerList.Count;
			}
		}

		/// <summary>
		/// Copies the content of this collection to an array.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		public void CopyTo(Array array, int index)
		{
			base.InnerList.CopyTo(array, index);
		}

		/// <summary>
		/// Synchronization object.
		/// </summary>
		public object SyncRoot
		{
			get
			{
				return base.InnerList.SyncRoot;
			}
		}

		#endregion

		#region IEnumerable Members

		/// <summary>
		/// Gets the enumerator for this collection.
		/// </summary>
		/// <returns></returns>
		System.Collections.IEnumerator IEnumerable.GetEnumerator()
		{
			return base.GetEnumerator();
		}

		#endregion

		#region Private Methods

		private int GetParameterIndex( string name )
		{
			object index = _names[name];
			if( index == null )
				return -1;
			else
				return (int)index;
		}

		private void RebuildNames()
		{
			lock( this )
			{
				_names.Clear();

				for( int i=0;i<base.InnerList.Count;i++)
				{
					SharpHsqlParameter p = base.InnerList[i] as SharpHsqlParameter;
					if( p != null )
					{
						_names[p.ParameterName] = i;
					}
				}
			}
		}

		#endregion

		#region Private Vars

		private SharpHsqlCommand _command = null;
		private Hashtable _names = null;

		#endregion
	}
}
