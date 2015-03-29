#region Usings
using System;
using System.Collections;
using System.IO;
using System.Text;
#endregion

#region License
/*
 * ByteArray.cs
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
	/// ByteArray class declaration.
	/// This class allows HSQL to store binary data as an array of bytes.
	/// It contains methods to create and access the data, perform comparisons, etc.
	/// </summary>
	/// <remarks>version 1.0.0.1</remarks>
	sealed class ByteArray 
	{
		private byte[] _data;
		
		/// <summary>
		/// ByteArray Constructor declaration.
		/// Converts a string parameter to the array of bytes the ByteArray object
		/// will contain.
		/// </summary>
		/// <param name="source">The byte array Base64 string representation.</param>
		public ByteArray(string source) 
		{
			_data = Convert.FromBase64String(source);
		}
		
		/// <summary>
		/// ByteArray Constructor declaration.
		/// Creates a ByteArray object from an array of bytes.
		/// </summary>
		/// <param name="array"></param>
		public ByteArray(byte []array) 
		{
			_data = array;
		}
		
		/// <summary>
		/// Value method declaration.
		/// Give access to the object's data.
		/// </summary>
		/// <remarks>The array of bytes representing this objects data.</remarks>
		public byte[] Value
		{
			get
			{
				return _data;
			}
		}
		
		/// <summary>
		/// CompareTo method declaration.
		/// This method compares the object to another ByteArray object.
		/// </summary>
		/// <param name="compareTo">ByteArray object we are comparing against.</param>
		/// <returns>Zero if objects are the same, non-zero otherwise.</returns>
		public int CompareTo(ByteArray compareTo) 
		{
			int len = _data.Length;
			int lenb = compareTo.Value.Length;

			for (int i = 0; ; i++) 
			{
				int a = 0, b = 0;

				if (i < len) 
				{
					a = ((int) _data[i]) & 0xff;
				} 
				else if (i >= lenb) 
				{
					return 0;
				}

				if (i < lenb) 
				{
					b = ((int) compareTo.Value[i]) & 0xff;
				}

				if (a > b) 
				{
					return 1;
				}

				if (b > a) 
				{
					return -1;
				}
			}
		}

		/// <summary>
		/// Serialize method declaration.
		/// This method serializes an object into an array of bytes.
		/// </summary>
		/// <param name="obj">The object to serialize</param>
		/// <returns>A static byte array representing the passed object</returns>
		public static byte[] Serialize(object obj) 
		{
			try 
			{
				#if !POCKETPC
				MemoryStream ms = new MemoryStream();
				BinaryFormatter b = new BinaryFormatter();
				b.Serialize(ms,obj);

				return ms.ToArray();
				#else
					throw new NotSupportedException();
				#endif
			} 
			catch (Exception e) 
			{
				throw Trace.Error(Trace.SERIALIZATION_FAILURE, e.Message);
			}
		}

		/// <summary>
		/// SerializeTostring method declaration.
		/// This method serializes an object into a string.
		/// </summary>
		/// <param name="obj">The object to serialize</param>
		/// <returns>A string representing the passed object</returns>
		public static string SerializeTostring(object obj) 
		{
			return CreateString(Serialize(obj));
		}

		/// <summary>
		/// Deserialize method declaration.
		/// This method returns the array of bytes stored in the instance of
		/// ByteArray class as an object instance.
		/// </summary>
		/// <returns>The deserialized object</returns>
		public object Deserialize() 
		{
			try 
			{
				#if !POCKETPC
				MemoryStream ms = new MemoryStream(_data);
				BinaryFormatter b = new BinaryFormatter();

				return b.Deserialize(ms);
				#else
					throw new NotSupportedException();
				#endif
			} 
			catch (Exception e) 
			{
				throw Trace.Error(Trace.SERIALIZATION_FAILURE, e.Message);
			}
		}
	
		/// <summary>
		/// Deserialize method declaration.
		/// This method returns the array of bytes stored in the instance of
		/// ByteArray class as an object instance.
		/// </summary>
		/// <returns>The deserialized object</returns>
		public static object Deserialize( byte[] data ) 
		{
			try 
			{
				#if !POCKETPC
				MemoryStream ms = new MemoryStream(data);
				BinaryFormatter b = new BinaryFormatter();

				return b.Deserialize(ms);
				#else
				throw new NotSupportedException();
				#endif
			} 
			catch (Exception e) 
			{
				throw Trace.Error(Trace.SERIALIZATION_FAILURE, e.Message);
			}
		}

		/// <summary>
		/// Createstring method declaration.
		/// This method creates a string from the passed array of bytes.
		/// </summary>
		/// <param name="bytes">The byte array to convert.</param>
		/// <returns>A Base64 string representation of the byte array.</returns>
		private static string CreateString(byte[] bytes) 
		{
			return Convert.ToBase64String( bytes, 0, bytes.Length );
		}

		/// <summary>
		/// ToString method declaration.
		/// This method creates a string from the passed array of bytes stored in
		/// this instance of the ByteArray class.
		/// </summary>
		/// <returns>The string representation of the ByteArray.</returns>
		public override string ToString() 
		{
			return CreateString(_data);
		}

		/// <summary>
		/// GetHashCode method declaration.
		/// This method returns the hashcode for the data stored in this instance of
		/// the ByteArray class.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() 
		{
			return _data.GetHashCode();
		}
	}
}
