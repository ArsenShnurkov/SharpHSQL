#region Usings 
using System;
using System.Data;
using System.Collections;
using System.IO;
using System.Text;
#endregion

#region License
/*
 * Column.cs
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
	/// Class representing a database column.
	/// </summary>
	/// <remarks>version 1.0.0.1</remarks>
	sealed class Column 
	{
		#region Public Constants

		// NULL and VARCHAR_IGNORECASE is not part of TYPES
		/// <summary>
		/// Collection of all supported column types.
		/// </summary>
		internal static readonly ColumnType[] Types = 
		{
			ColumnType.Bit, ColumnType.TinyInt, ColumnType.BigInt, ColumnType.LongVarBinary, 
			ColumnType.VarBinary, ColumnType.Binary, ColumnType.LongVarChar,
			ColumnType.Char, ColumnType.Numeric, ColumnType.DbDecimal, ColumnType.Integer, 
			ColumnType.SmallInt, ColumnType.Float, ColumnType.Real, ColumnType.DbDouble,
			ColumnType.VarChar, ColumnType.Date, ColumnType.Time, ColumnType.Timestamp, 
			ColumnType.Other, ColumnType.UniqueIdentifier
		};
		#endregion

		#region Private Vars
		private static Hashtable _types;
		private string		     _name;
		private ColumnType	     _type;
		private bool			 _nullable;
		private bool			 _identity;
		#endregion

		#region Constructors
		/// <summary>
		/// Constructor declaration.
		/// </summary>
		/// <param name="name">Column name.</param>
		/// <param name="nullable">Flag indicating if the column supports nulls.</param>
		/// <param name="type">DataType of the column.</param>
		/// <param name="identity">Flag indicating if the column is identity.</param>
		public Column(string name, bool nullable, ColumnType type, bool identity) 
		{
			_name = name;
			_nullable = nullable;
			_type = type;
			_identity = identity;
		}
		#endregion

		#region Public Properties

		/// <summary>
		/// Name of the column.
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
		}

		/// <summary>
		/// Internal type of the column.
		/// </summary>
		public ColumnType ColumnType
		{
			get
			{
				return _type;
			}
		}

		/// <summary>
		/// Returns True id the column is identity.
		/// </summary>
		public bool IsIdentity
		{
			get
			{
				return _identity;
			}
		}

		/// <summary>
		/// Returns True if the column accepts null values.
		/// </summary>
		public bool IsNullable
		{
			get
			{
				return _nullable;
			}
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Gets the Type code of the string representation of a data type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static ColumnType GetColumnType(string type) 
		{
			FillTypes();

			if (_types.ContainsKey(type))
			{
				return (ColumnType)_types[type];
			}
			else
				throw Trace.Error(Trace.WRONG_DATA_TYPE, type);
		}


		/// <summary>
		/// Gets if the string passed is a valid data type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static bool IsValidDataType(string type) 
		{
			FillTypes();

			if (_types.ContainsKey(type))
				return true;
			else
				return false;
		}

		/// <summary>
		/// Gets the string representation of the passed Type code.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static string GetColumnTypeString(ColumnType type) 
		{
			switch (type) 
			{
				case ColumnType.Null:
					return "NULL";

				case ColumnType.Integer:
					return "INTEGER";

				case ColumnType.DbDouble:
					return "DOUBLE";

				case ColumnType.VarCharIgnoreCase:
					return "VARCHAR_IGNORECASE";

				case ColumnType.VarChar:
					return "VARCHAR";

				case ColumnType.Char:
					return "CHAR";

				case ColumnType.LongVarChar:
					return "LONGVARCHAR";

				case ColumnType.Date:
					return "DATE";

				case ColumnType.Time:
					return "TIME";

				case ColumnType.DbDecimal:
					return "DECIMAL";

				case ColumnType.Bit:
					return "BIT";

				case ColumnType.TinyInt:
					return "TINYINT";

				case ColumnType.SmallInt:
					return "SMALLINT";

				case ColumnType.BigInt:
					return "BIGINT";

				case ColumnType.Real:
					return "REAL";

				case ColumnType.Float:
					return "FLOAT";

				case ColumnType.Numeric:
					return "NUMERIC";

				case ColumnType.Timestamp:
					return "TIMESTAMP";

				case ColumnType.Binary:
					return "BINARY";

				case ColumnType.VarBinary:
					return "VARBINARY";

				case ColumnType.LongVarBinary:
					return "LONGVARBINARY";

				case ColumnType.Other:
					return "OBJECT";

				case ColumnType.UniqueIdentifier:
					return "UNIQUEIDENTIFIER";

				default:
					throw Trace.Error(Trace.WRONG_DATA_TYPE, (int)type);
			}
		}

		/// <summary>
		/// Gets the string representation of the passed Type code.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static DbType GetDbType(ColumnType type) 
		{
			switch (type) 
			{
				case ColumnType.Null:
					return DbType.String;

				case ColumnType.Integer:
					return DbType.Int32;

				case ColumnType.DbDouble:
					return DbType.Double;

				case ColumnType.VarCharIgnoreCase:
					return DbType.String;

				case ColumnType.VarChar:
					return DbType.String;

				case ColumnType.Char:
					return DbType.StringFixedLength;

				case ColumnType.LongVarChar:
					return DbType.String;

				case ColumnType.Date:
					return DbType.DateTime;

				case ColumnType.Time:
					return DbType.Time;

				case ColumnType.DbDecimal:
					return DbType.Decimal;

				case ColumnType.Bit:
					return DbType.Boolean;

				case ColumnType.TinyInt:
					return DbType.Byte;

				case ColumnType.SmallInt:
					return DbType.Int16;

				case ColumnType.BigInt:
					return DbType.Int64;

				case ColumnType.Real:
					return DbType.Single;

				case ColumnType.Float:
					return DbType.Single;

				case ColumnType.Numeric:
					return DbType.Decimal;

				case ColumnType.Timestamp:
					return DbType.DateTime;

				case ColumnType.Binary:
					return DbType.Object;

				case ColumnType.VarBinary:
					return DbType.Object;

				case ColumnType.LongVarBinary:
					return DbType.Object;

				case ColumnType.Other:
					return DbType.Object;

				case ColumnType.UniqueIdentifier:
					return DbType.Guid;

				default:
					throw Trace.Error(Trace.WRONG_DATA_TYPE, (int)type);
			}
		}

		/// <summary>
		/// Adds two values of the specified type.
		/// </summary>
		/// <param name="one">The first operand to Add.</param>
		/// <param name="two">The second operand to Add.</param>
		/// <param name="type">The data type of the two operands.</param>
		/// <returns>The result of the Add operation.</returns>
		internal static object Add(object one, object two, ColumnType type) 
		{
			if (one == null || two == null) 
			{
				return null;
			}

			switch (type) 
			{

				case ColumnType.Null:
					return null;

				case ColumnType.Integer:
					int ai = (int)one;
					int bi = (int)two;

					return (ai + bi);

				case ColumnType.Float:
				case ColumnType.Real:
					float ar = (float) one;
					float br = (float) two;

					return (ar + br);

				case ColumnType.DbDouble:
					double ad = (double) one;
					double bd = (double) two;

					return (ad + bd);

				case ColumnType.VarChar:
				case ColumnType.Char:
				case ColumnType.LongVarChar:
				case ColumnType.VarCharIgnoreCase:
					return (string) one + (string) two;

				case ColumnType.Numeric:
				case ColumnType.DbDecimal:
					decimal abd = (decimal) one;
					decimal bbd = (decimal) two;

					return (abd + bbd);

				case ColumnType.TinyInt:
					byte at = (byte) one;
					byte bt = (byte) two;

					return (at + bt);

				case ColumnType.SmallInt:
					short shorta = (short) one;
					short shortb = (short) two;

					return (shorta + shortb);

				case ColumnType.BigInt:
					long longa = (long) one;
					long longb = (long) two;

					return (longa + longb);

				default:
					throw Trace.Error(Trace.FUNCTION_NOT_SUPPORTED, (int)type);
			}
		}

		/// <summary>
		/// Cancatenates two objects.
		/// </summary>
		/// <param name="one"></param>
		/// <param name="two"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object Concat(object one, object two, ColumnType type ) 
		{
			if (one == null) 
			{
				return two;
			} 
			else if (two == null) 
			{
				return one;
			}

			return ConvertToString(one, type) + ConvertToString(two, type);
		}

		/// <summary>
		/// Negate the passed object.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object Negate(object obj, ColumnType type) 
		{
			if (obj == null) 
			{
				return null;
			}

			switch (type) 
			{

				case ColumnType.Null:
					return null;

				case ColumnType.Integer:
					return (-(int) obj);

				case ColumnType.Float:
				case ColumnType.Real:
					return (-(float) obj);

				case ColumnType.DbDouble:
					return (-(double) obj);

				case ColumnType.Numeric:
				case ColumnType.DbDecimal:
					return (-(decimal)obj);

				case ColumnType.TinyInt:
					return (-(byte)obj);

				case ColumnType.SmallInt:
					return (-(short)obj);

				case ColumnType.BigInt:
					return (-(long) obj);

				default:
					throw Trace.Error(Trace.FUNCTION_NOT_SUPPORTED, (int)type);
			}
		}

		/// <summary>
		/// Multiply two objects.
		/// </summary>
		/// <param name="one"></param>
		/// <param name="two"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object Multiply(object one, object two, ColumnType type) 
		{
			if (one == null || two == null) 
			{
				return null;
			}

			switch (type) 
			{

				case ColumnType.Null:
					return null;

				case ColumnType.Integer:
					int ai = (int) one;
					int bi = (int) two;

					return (ai * bi);

				case ColumnType.Float:
				case ColumnType.Real:
					float floata = (float) one;
					float floatb = (float) two;

					return (floata * floatb);

				case ColumnType.DbDouble:
					double ad = (double) one;
					double bd = (double) two;

					return (ad * bd);

				case ColumnType.Numeric:
				case ColumnType.DbDecimal:
					decimal abd = (decimal) one;
					decimal bbd = (decimal) two;

					return (abd * bbd);

				case ColumnType.TinyInt:
					byte ba = (byte) one;
					byte bb = (byte) two;

					return (ba * bb);

				case ColumnType.SmallInt:
					short shorta = (short) one;
					short shortb = (short) two;

					return (shorta * shortb);

				case ColumnType.BigInt:
					long longa = (long) one;
					long longb = (long) two;

					return (longa * longb);

				default:
					throw Trace.Error(Trace.FUNCTION_NOT_SUPPORTED, (int)type);
			}
		}

		/// <summary>
		/// Divide two objects.
		/// </summary>
		/// <param name="one"></param>
		/// <param name="two"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object Divide(object one, object two, ColumnType type) 
		{
			if (one == null || two == null) 
			{
				return null;
			}

			switch (type) 
			{

				case ColumnType.Null:
					return null;

				case ColumnType.Integer:
					if ((int)two == 0)
						return null;
					return ((int)one / (int)two);

				case ColumnType.Float:
				case ColumnType.Real:
					if ((float) two == 0)
						return null;
					return ((float)one / (float)two);


				case ColumnType.DbDouble:
					if ((double) two == 0)
						return null;
					return ((double)one / (double)two);

				case ColumnType.Numeric:
				case ColumnType.DbDecimal:
					if ((decimal) two == 0)
						return null;
					return ((decimal)one / (decimal)two);

				case ColumnType.TinyInt:
					if ((byte) two == 0)
						return null;
					return ((byte)one / (byte)two);

				case ColumnType.SmallInt:
					if ((short) two == 0)
						return null;
					return ((short)one / (short)two);

				case ColumnType.BigInt:
					if ((long) two == 0)
						return null;
					return ((long)one / (long)two);

				default:
					throw Trace.Error(Trace.FUNCTION_NOT_SUPPORTED, (int)type);
			}
		}

		/// <summary>
		/// Subtract two objects.
		/// </summary>
		/// <param name="one"></param>
		/// <param name="two"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object Subtract(object one, object two, ColumnType type) 
		{
			if (one == null || two == null) 
			{
				return null;
			}

			switch (type) 
			{

				case ColumnType.Null:
					return null;

				case ColumnType.Integer:
					return ((int)one - (int)two);

				case ColumnType.Float:
				case ColumnType.Real:
					return ((float)one - (float)two);

				case ColumnType.DbDouble:
					return ((double)one - (double)two);

				case ColumnType.Numeric:
				case ColumnType.DbDecimal:
					return ((decimal)one - (decimal)two);

				case ColumnType.TinyInt:
					return ((byte)one - (byte)two);

				case ColumnType.SmallInt:
					return ((short)one - (short)two);

				case ColumnType.BigInt:
					return ((long)one - (long)two);

				default:
					throw Trace.Error(Trace.FUNCTION_NOT_SUPPORTED, (int)type);
			}
		}

		/// <summary>
		/// Sum two objects.
		/// </summary>
		/// <param name="one"></param>
		/// <param name="two"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object Sum(object one, object two, ColumnType type) 
		{
			if (one == null) 
			{
				return two;
			}

			if (two == null) 
			{
				return one;
			}

			switch (type) 
			{

				case ColumnType.Null:
					return null;

				case ColumnType.Integer:
					return (((int) one) + ((int) two));

				case ColumnType.Float:
				case ColumnType.Real:
					return (((float) one)	+ ((float) two));

				case ColumnType.DbDouble:
					return (((double) one) + ((double) two));

				case ColumnType.Numeric:
				case ColumnType.DbDecimal:
					return (((decimal) one) + ((decimal) two));

				case ColumnType.TinyInt:
					return  (((byte) one) + ((byte) two));

				case ColumnType.SmallInt:
					return  (((short) one) + ((short) two));

				case ColumnType.BigInt:
					return (((long) one) + ((long) two));

				default:
					Trace.Error(Trace.SUM_OF_NON_NUMERIC);
					break;
			}

			return null;
		}

		/// <summary>
		/// Calculates the object average.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="type"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		internal static object Avg(object data, ColumnType type, int count) 
		{
			if (data == null || count == 0) 
			{
				return null;
			}

			switch (type) 
			{

				case ColumnType.Null:
					return null;

				case ColumnType.Integer:
					return ((int)data / count);

				case ColumnType.Float:
				case ColumnType.Real:
					return ((float) data / count);

				case ColumnType.DbDouble:
					return ((double) data / count);

				case ColumnType.Numeric:
				case ColumnType.DbDecimal:
					return ((decimal) data / (decimal)count);

				case ColumnType.TinyInt:
					return ((byte) data / count);

				case ColumnType.SmallInt:
					return ((short) data / count);

				case ColumnType.BigInt:
					return ((long) data / count);

				default:
					Trace.Error(Trace.SUM_OF_NON_NUMERIC);
					break;
			}

			return null;
		}

		/// <summary>
		/// Calculates the minimum value.
		/// </summary>
		/// <param name="one"></param>
		/// <param name="two"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object Min(object one, object two, ColumnType type) 
		{
			if (one == null) 
			{
				return two;
			}

			if (two == null) 
			{
				return one;
			}

			if (Compare(one, two, type) < 0) 
			{
				return one;
			}

			return two;
		}

		/// <summary>
		/// Calculates the maximum value.
		/// </summary>
		/// <param name="one"></param>
		/// <param name="two"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object Max(object one, object two, ColumnType type) 
		{
			if (one == null) 
			{
				return two;
			}

			if (two == null) 
			{
				return one;
			}

			if (Compare(one, two, type) > 0) 
			{
				return one;
			}

			return two;
		}

		/// <summary>
		/// Compare two objects.
		/// </summary>
		/// <param name="one"></param>
		/// <param name="two"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static int Compare(object one, object two, ColumnType type) 
		{
			int i = 0;

			// null handling: null==null and smaller any value
			// todo: implement standard SQL null handling
			// it is also used for grouping ('null' is one group)
			if (one == null) 
			{
				if (two == null) 
				{
					return 0;
				}

				return -1;
			}

			if (two == null) 
			{
				return 1;
			}

			switch (type) 
			{

				case ColumnType.Null:
					return 0;

				case ColumnType.Integer:
					return ((int)one > (int)two) ? 1 : ((int)two > (int)one ? -1 : 0);

				case ColumnType.Float:
				case ColumnType.Real:
					return ((float)one > (float)two) ? 1 : ((float)two > (float)one ? -1 : 0);

				case ColumnType.DbDouble:
					return ((double)one > (double)two) ? 1 : ((double)two > (double)one ? -1 : 0);

				case ColumnType.VarChar:

				case ColumnType.Char:

				case ColumnType.LongVarChar:
					i = ((string) one).CompareTo((string) two);
					break;

				case ColumnType.VarCharIgnoreCase:
					i = ((string) one).ToUpper().CompareTo(((string) two).ToUpper());

					break;

				case ColumnType.Date:
				case ColumnType.Time:
				case ColumnType.Timestamp:
					if ((DateTime)one > (DateTime)two)
					{
						return 1;
					} 
					if ((DateTime)two > (DateTime)one)
					{
						return -1;
					} 
					else 
					{
						return 0;
					}

				case ColumnType.Numeric:
				case ColumnType.DbDecimal:
					return ((decimal)one > (decimal)two) ? 1 : ((decimal)two > (decimal)one ? -1 : 0);

				case ColumnType.Bit:
					return (((bool)one == (bool)two) ? 0 : 1);

				case ColumnType.TinyInt:
					return ((byte)one > (byte)two) ? 1 : ((byte)two > (byte)one ? -1 : 0);

				case ColumnType.SmallInt:
					return ((short)one > (short)two) ? 1 : ((short)two > (short)one ? -1 : 0);

				case ColumnType.BigInt:
					return ((long)one > (long)two) ? 1 : ((long)two > (long)one ? -1 : 0);

				case ColumnType.Binary:
				case ColumnType.VarBinary:
				case ColumnType.LongVarBinary:
				case ColumnType.Other:
					i = (new ByteArray((byte[])one).CompareTo(new ByteArray((byte[])two)));
					break;
					
				case ColumnType.UniqueIdentifier:
					i = ((Guid)one).CompareTo((Guid)two);
					break;

				default:
					throw Trace.Error(Trace.FUNCTION_NOT_SUPPORTED,(int)type);
			}

			return (i > 0) ? 1 : (i < 0 ? -1 : 0);
		}

		/// <summary>
		/// Convert from string representation.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object ConvertString(string source, ColumnType type) 
		{
			if (source == null) 
			{
				return null;
			}

			switch (type) 
			{
				case ColumnType.Null:
					return null;

				case ColumnType.Integer:
					return int.Parse(source);

				case ColumnType.Float:
				case ColumnType.Real:
					return Single.Parse(source);

				case ColumnType.DbDouble:
					return Double.Parse(source);

				case ColumnType.VarCharIgnoreCase:
				case ColumnType.VarChar:
				case ColumnType.Char:
				case ColumnType.LongVarChar:
					return source;

				case ColumnType.Date:
				case ColumnType.Time:
				case ColumnType.Timestamp:
					return DateTime.Parse(source);

				case ColumnType.Numeric:
				case ColumnType.DbDecimal:
					return Decimal.Parse(source);

				case ColumnType.Bit:
					return Boolean.Parse(source);

				case ColumnType.TinyInt:
					return Byte.Parse(source);

				case ColumnType.SmallInt:
					return Int16.Parse(source);

				case ColumnType.BigInt:
					return Int64.Parse(source);

				case ColumnType.Binary:
				case ColumnType.VarBinary:
				case ColumnType.LongVarBinary:
					return new ByteArray(source).Value;

				case ColumnType.Other:
					return ByteArray.Deserialize(new ByteArray(source).Value);

				case ColumnType.UniqueIdentifier:
					return new Guid(source);

				default:
					throw Trace.Error(Trace.FUNCTION_NOT_SUPPORTED, (int)type);
			}
		}

		/// <summary>
		/// Convert to string from object.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static string ConvertToString(object obj, ColumnType type) 
		{
			if (obj == null) 
			{
				return null;
			}

			switch (type) 
			{
				case ColumnType.Date:
				case ColumnType.Time:
				case ColumnType.Timestamp:
					if( obj is DateTime )
						return ((DateTime)obj).ToString("yyyy.MM.dd HH:mm:ss.fffffff");
					else
						return obj.ToString();
				default:
					return obj.ToString();
			}
		}

		/// <summary>
		/// Convert to object making a string/object roundtrip.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static object ConvertToObject(object obj, ColumnType type) 
		{
			if (obj == null) 
			{
				return null;
			}
			return ConvertString(obj.ToString(), type);
		}

		/// <summary>
		/// Returns the string representation of the object.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static string CreateString(object obj, ColumnType type) 
		{
			if (obj == null) 
				return "NULL";

			switch (type) 
			{
				case ColumnType.Null:
					return "NULL";

				case ColumnType.Date:
				case ColumnType.Time:
				case ColumnType.Timestamp:
					return "'" + ((DateTime)obj).ToString("yyyy.MM.dd HH:mm:ss.fffffff") + "'";
				
				case ColumnType.Other:
					return "'" + ByteArray.SerializeTostring(obj) + "'";

				case ColumnType.Binary:
				case ColumnType.VarBinary:
				case ColumnType.LongVarBinary:
					return "'" + obj.ToString() + "'";

				case ColumnType.Real:
				case ColumnType.DbDouble:
				case ColumnType.DbDecimal:
				case ColumnType.Float:
				case ColumnType.Numeric:
					return "'" + obj.ToString() + "'";

				case ColumnType.VarCharIgnoreCase:
				case ColumnType.VarChar:
				case ColumnType.Char:
				case ColumnType.LongVarChar:
					return CreateString((string) obj);

				case ColumnType.UniqueIdentifier:
					return CreateString(((Guid)obj).ToString());

				default:
					return obj.ToString();
			}
		}

		/// <summary>
		/// Gets the string representation of the data.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		internal static string CreateString(string source) 
		{
			StringBuilder b = new StringBuilder();
			b.Append("\'");

			if (source != null) 
			{
				for (int i = 0, len = source.Length; i < len; i++) 
				{
					char c = Convert.ToChar(source.Substring(i,1));

					if (c == '\'') 
					{
						b.Append(c.ToString());
					}

					b.Append(c.ToString());
				}
			}

			return b.Append('\'').ToString();
		}

		/// <summary>
		/// Read column data from file.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		internal static object[] ReadData(BinaryReader reader, int length) 
		{
			object[] data = new object[length];

			for (int i = 0; i < length; i++) 
			{
				#if !POCKETPC
				ColumnType type = (ColumnType)Enum.Parse( typeof(ColumnType), reader.ReadInt32().ToString() );
				#else
				ColumnType type = (ColumnType)OpenNETCF.EnumEx.Parse( typeof(ColumnType), reader.ReadInt32().ToString() );
				#endif
				object o = null;
				switch (type) 
				{
					case ColumnType.Null:
						o = null;
						break;

					case ColumnType.Float:
					case ColumnType.Real:
						o = reader.ReadSingle();
						break;

					case ColumnType.DbDouble:
						o = reader.ReadDouble();
						break;

					case ColumnType.VarCharIgnoreCase:
					case ColumnType.VarChar:
					case ColumnType.Char:
					case ColumnType.LongVarChar:
						o = reader.ReadString();
						break;

					case ColumnType.Date:
					case ColumnType.Time:
					case ColumnType.Timestamp:
						o = new DateTime( reader.ReadInt64() );
						break;

					case ColumnType.Numeric:
					case ColumnType.DbDecimal:
						#if !POCKETPC
						o = reader.ReadDecimal();
						#else
						int l = reader.ReadInt32();
						byte[] bytes = reader.ReadBytes(l);
						int[] bits = new int[bytes.Length/4];
						for(int ix=0;ix<bits.Length;ix++)
						{
							bits[ix] = BitConverter.ToInt32(bytes, ix*4);
						}
						o = new Decimal( bits );
						#endif
						break;

					case ColumnType.Bit:
						o = reader.ReadBoolean();
						break;

					case ColumnType.TinyInt:
						o = reader.ReadByte();
						break;

					case ColumnType.SmallInt:
						o = reader.ReadInt16();
						break;

					case ColumnType.Integer:
						o = reader.ReadInt32();
						break;

					case ColumnType.BigInt:
						o = reader.ReadInt64();
						break;

					case ColumnType.Binary:
					case ColumnType.VarBinary:
					case ColumnType.LongVarBinary:
						int len = reader.ReadInt32();
						o = reader.ReadBytes(len);
						break;

					case ColumnType.Other:
						int other = reader.ReadInt32();
						o = ByteArray.Deserialize(reader.ReadBytes(other));
						break;

					case ColumnType.UniqueIdentifier:
						o = new Guid(reader.ReadString());
						break;

					default:
						throw Trace.Error(Trace.WRONG_DATA_TYPE, (int)type);
				}

				data[i] = o;
			}

			return data;
		}

		/// <summary>
		/// Write column data to file.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="data"></param>
		/// <param name="table"></param>
		internal static void WriteData(BinaryWriter writer, object[] data, Table table) 
		{
			int len = table.InternalColumnCount;
			ColumnType[] type = new ColumnType[len];

			for (int i = 0; i < len; i++) 
			{
				type[i] = table.GetType(i);
			}

			WriteData(writer, len, type, data);
		}

		/// <summary>
		/// Write column data to file.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="length"></param>
		/// <param name="type"></param>
		/// <param name="data"></param>
		internal static void WriteData(BinaryWriter writer, int length, ColumnType[] type, object[] data) 
		{
			for (int i = 0; i < length; i++) 
			{
				object o = data[i];

				if (o == null) 
				{
					writer.Write((int)ColumnType.Null);
				} 
				else 
				{
					ColumnType t = type[i];

					writer.Write((int)t);

					switch (t) 
					{
						case ColumnType.Null:
							o = null;
							break;

						case ColumnType.Float:
						case ColumnType.Real:
							writer.Write((float)o);
							break;

						case ColumnType.DbDouble:
							writer.Write((double)o);
							break;

						case ColumnType.Bit:
							writer.Write((bool)o);
							break;

						case ColumnType.TinyInt:
							writer.Write((byte)o);
							break;

						case ColumnType.SmallInt:
							writer.Write((short)o);
							break;

						case ColumnType.DbDecimal:
						case ColumnType.Numeric:
							#if !POCKETPC
							writer.Write((Decimal)o);
							#else
							int[] bits = Decimal.GetBits( (Decimal)o );
							byte[] bytes = new byte[bits.Length*4];
							for( int ix=0;ix<bits.Length;ix++)
							{
								byte[] r = BitConverter.GetBytes(bits[ix]);
								Array.Copy( r, 0, bytes, (ix*4), 4);
							}
							writer.Write( bytes.Length );
							writer.Write( bytes );
							#endif
							break;

						case ColumnType.Integer:
							writer.Write((int)o);
							break;

						case ColumnType.BigInt:
							writer.Write((long)o);
							break;

						case ColumnType.Date:
						case ColumnType.Time:
						case ColumnType.Timestamp:
							writer.Write( ((DateTime)o).Ticks );
							break;

						case ColumnType.Binary:
						case ColumnType.VarBinary:
						case ColumnType.LongVarBinary:
							byte[] b = (byte[])o;
							writer.Write(b.Length);
							writer.Write(b);
							break;

						case ColumnType.Other:
							byte[] other = ByteArray.Serialize(o);
							writer.Write(other.Length);
							writer.Write(other);
							break;

						case ColumnType.UniqueIdentifier:
							writer.Write(((Guid)o).ToString());
							break;

						default:
							writer.Write(o.ToString());
							break;
					}
				}
			}
		}

		/// <summary>
		/// Gets the size of the stored data.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="table"></param>
		/// <returns></returns>
		internal static int GetSize(object[] data, Table table) 
		{
			int l = data.Length;
			ColumnType[] type = new ColumnType[l];

			for (int i = 0; i < l; i++) 
			{
				type[i] = table.GetType(i);
			}

			return GetSize(data, l, type);
		}

		/// <summary>
		/// Gets the size of the stored data.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="l"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		unsafe private static int GetSize(object[] data, int l, ColumnType[] type) 
		{
			int s = 0;

			for (int i = 0; i < l; i++) 
			{
				object o = data[i];

				s += 4;    // type

				if (o != null) 
				{
					switch (type[i]) 
					{
						case ColumnType.Char:
						case ColumnType.LongVarChar:
						case ColumnType.VarChar:
						case ColumnType.VarCharIgnoreCase:
							s += System.Text.Encoding.Unicode.GetByteCount( o.ToString() );
							s += 1;
							break;

						case ColumnType.Float:
						case ColumnType.Real:
							s += sizeof(float);
							break;

						case ColumnType.DbDouble:
							s += sizeof(double);
							break;

						case ColumnType.Bit:
							s += sizeof(bool);
							break;

						case ColumnType.TinyInt:
							s += sizeof(byte);
							break;

						case ColumnType.SmallInt:
							s += sizeof(short);
							break;
	
						case ColumnType.Integer:
							s += sizeof(int);
							break;

						case ColumnType.DbDecimal:
						case ColumnType.Numeric:
							s += sizeof(Decimal);
							break;

						case ColumnType.BigInt:
							s += sizeof(long);
							break;

						case ColumnType.Date:
						case ColumnType.Time:
						case ColumnType.Timestamp:
							s += sizeof(long);
							break;

						case ColumnType.Binary:
						case ColumnType.VarBinary:
						case ColumnType.LongVarBinary:
							s += 4;
							s += ((byte[])o).Length;
							break;

						case ColumnType.Other:
							s += 4;
							s += ByteArray.Serialize(o).Length;
							break;

						case ColumnType.UniqueIdentifier:
							s += sizeof(Guid);
							break;

						default:
							s += System.Text.Encoding.Unicode.GetByteCount( o.ToString() );
							s += 1;
							break;
					}
				}
			}

			return s;
		}

		#endregion 

		#region Private methods

		private static void FillTypes()
		{
			if (_types == null)
			{
				_types = new Hashtable();

				AddTypes(ColumnType.Integer, "INTEGER", "int", "java.lang.int");
				AddType(ColumnType.Integer, "INT");
				AddTypes(ColumnType.DbDouble, "DOUBLE", "double", "java.lang.Double");
				AddType(ColumnType.Float, "FLOAT");		       // this is a Double
				AddTypes(ColumnType.VarChar, "VARCHAR", "java.lang.string", null);
				AddTypes(ColumnType.Char, "CHAR", "CHARACTER", null);
				AddType(ColumnType.LongVarChar, "LONGVARCHAR");

				// for ignorecase data types, the 'original' type name is lost
				AddType(ColumnType.VarCharIgnoreCase, "VARCHAR_IGNORECASE");
				AddTypes(ColumnType.Date, "DATE", "java.sql.Date", null);
				AddTypes(ColumnType.Time, "TIME", "java.sql.Time", null);

				// DATETIME is for compatibility with MS SQL 7
				AddTypes(ColumnType.Timestamp, "TIMESTAMP", "java.sql.Timestamp", "DATETIME");
				AddTypes(ColumnType.DbDecimal, "DECIMAL", "java.math.BigDecimal", null);
				AddType(ColumnType.Numeric, "NUMERIC");
				AddTypes(ColumnType.Bit, "BIT", "java.lang.Boolean", "bool");
				AddTypes(ColumnType.TinyInt, "TINYINT", "java.lang.Short", "short");
				AddType(ColumnType.SmallInt, "SMALLINT");
				AddTypes(ColumnType.BigInt, "BIGINT", "java.lang.Long", "long");
				AddTypes(ColumnType.Real, "REAL", "java.lang.Float", "float");
				AddTypes(ColumnType.Binary, "BINARY", "byte[]", null);    // maybe better "[B"
				AddType(ColumnType.VarBinary, "VARBINARY");
				AddType(ColumnType.LongVarBinary, "LONGVARBINARY");
				AddTypes(ColumnType.Other, "OTHER", "System.Object", "OBJECT");

				// --- other
				AddType(ColumnType.UniqueIdentifier, "UNIQUEIDENTIFIER");
			}
		}

		/// <summary>
		/// Creates the internal Types collection.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="n2"></param>
		/// <param name="n3"></param>
		private static void AddTypes(ColumnType type, string name, string n2, string n3) 
		{
			AddType(type, name);
			AddType(type, n2);
			AddType(type, n3);
		}

		/// <summary>
		/// Adds a specific type to the collection.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		private static void AddType(ColumnType type, string name) 
		{
			if (name != null) 
			{
				_types.Add(name, type);
			}
		}

		#endregion
	}
}
