#region Usings
using System;
using System.Data;
using System.Data.Common;
using SharpHsql;
#endregion

#region License
/*
 * SharpHsqlParameter.cs
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
	/// Parameter class for Hsql ADO.NET data provider.
	/// <seealso cref="SharpHsqlCommand"/>
	/// </summary>
	public sealed class SharpHsqlParameter : IDbDataParameter, ICloneable
	{
		#region Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SharpHsqlParameter()
		{
			this._value = null;
			this._direction = ParameterDirection.Input;
			this._size = -1;
			this._version = DataRowVersion.Current;
			this._forceSize = false;
			this._offset = 0;
			this._suppress = false;
			this._inferType = true;
		}

		/// <summary>
		/// Constructor setting the parameter data type.
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="dbType"></param>
		public SharpHsqlParameter(string parameterName, DbType dbType)
		{
			this._value = null;
			this._direction = ParameterDirection.Input;
			this._size = -1;
			this._version = DataRowVersion.Current;
			this._forceSize = false;
			this._offset = 0;
			this._suppress = false;
			this._inferType = true;
			this.ParameterName = parameterName;
			this.DbType = dbType;
		}

		/// <summary>
		/// Constructor setting the parameter value and using data type automatic inference.
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="value"></param>
		public SharpHsqlParameter(string parameterName, object value)
		{
			this._value = null;
			this._direction = ParameterDirection.Input;
			this._size = -1;
			this._version = DataRowVersion.Current;
			this._forceSize = false;
			this._offset = 0;
			this._suppress = false;
			this._inferType = true;
			this.ParameterName = parameterName;
			this.Value = value;
		}

		/// <summary>
		/// Constructor setting the data type and size.
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="dbType"></param>
		/// <param name="size"></param>
		public SharpHsqlParameter(string parameterName, DbType dbType, int size)
		{
			this._value = null;
			this._direction = ParameterDirection.Input;
			this._size = -1;
			this._version = DataRowVersion.Current;
			this._forceSize = false;
			this._offset = 0;
			this._suppress = false;
			this._inferType = true;
			this.ParameterName = parameterName;
			this.DbType = dbType;
			this.Size = size;
		}

		/// <summary>
		/// Constructor setting the data type, size and source column.
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="dbType"></param>
		/// <param name="size"></param>
		/// <param name="sourceColumn"></param>
		public SharpHsqlParameter(string parameterName, DbType dbType, int size, string sourceColumn)
		{
			this._value = null;
			this._direction = ParameterDirection.Input;
			this._size = -1;
			this._version = DataRowVersion.Current;
			this._forceSize = false;
			this._offset = 0;
			this._suppress = false;
			this._inferType = true;
			this.ParameterName = parameterName;
			this.DbType = dbType;
			this.Size = size;
			this.SourceColumn = sourceColumn;
		}

		/// <summary>
		/// Constructor setting the data type, size, direction, nullability, precision, scale, source column, row version and value.
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="dbType"></param>
		/// <param name="size"></param>
		/// <param name="direction"></param>
		/// <param name="isNullable"></param>
		/// <param name="precision"></param>
		/// <param name="scale"></param>
		/// <param name="sourceColumn"></param>
		/// <param name="sourceVersion"></param>
		/// <param name="value"></param>
		public SharpHsqlParameter(string parameterName, DbType dbType, int size, ParameterDirection direction, bool isNullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value)
		{
			this._value = null;
			this._direction = ParameterDirection.Input;
			this._size = -1;
			this._version = DataRowVersion.Current;
			this._forceSize = false;
			this._offset = 0;
			this._suppress = false;
			this._inferType = true;
			this.ParameterName = parameterName;
			this.DbType = dbType;
			this.Size = size;
			this.Direction = direction;
			this.IsNullable = isNullable;
			this.Precision = precision;
			this.Scale = scale;
			this.SourceColumn = sourceColumn;
			this.SourceVersion = sourceVersion;
			this.Value = value;
		}

		#endregion

		#region IDbDataParameter Members

		/// <summary>
		/// Get or set the parameter precision.
		/// </summary>
		public byte Precision
		{
			get
			{
				return _precision;
			}
			set
			{
				_precision = value;
			}
		}

		/// <summary>
		/// Get or set the parameter scale.
		/// </summary>
		public byte Scale
		{
			get
			{
				return _scale;
			}
			set
			{
				_scale = value;
			}
		}

		/// <summary>
		/// Get or set the parameter size.
		/// </summary>
		public int Size
		{
			get
			{
				return _size;
			}
			set
			{
				_size = value;
			}
		}

		#endregion

		#region IDataParameter Members

		/// <summary>
		/// Get or set the parameter direction.
		/// <seealso cref="System.Data.ParameterDirection"/>
		/// </summary>
		public System.Data.ParameterDirection Direction
		{
			get
			{
				return _direction;
			}
			set
			{
				_direction = value;
			}
		}

		/// <summary>
		/// Get or set the parameter <see cref="DbType"/>.
		/// </summary>
		public DbType DbType
		{
			get
			{
				return _dbtype;
			}
			set
			{
				_dbtype = value;
			}
		}

		/// <summary>
		/// Get or set the parameter value.
		/// </summary>
		public object Value
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
			}
		}

		/// <summary>
		/// Get or set the parameter nullability.
		/// </summary>
		public bool IsNullable
		{
			get
			{
				return _isNullable;
			}
			set
			{
				this._isNullable = value;
			}
		}

		/// <summary>
		/// Get or set the parameter <see cref="DataRowVersion"/>.
		/// </summary>
		public DataRowVersion SourceVersion
		{
			get
			{
				return _version;
			}
			set
			{
				_version = value;
			}
		}

		/// <summary>
		/// Get or set the parameter name.
		/// </summary>
		public string ParameterName
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		/// <summary>
		/// Get or set the parameter source column name.
		/// </summary>
		public string SourceColumn
		{
			get
			{
				return _sourceColumn;
			}
			set
			{
				_sourceColumn = value;
			}
		}

		#endregion

		#region Internal Methods & Properties

		/// <summary>
		/// Flag that indicates if this parameter must be excluded.
		/// </summary>
		internal bool Suppress
		{
			get
			{
				return this._suppress;
			}
			set
			{
				this._suppress = value;
			}
		}

		/// <summary>
		/// Internal reference to the parent collection.
		/// </summary>
		internal SharpHsqlParameterCollection Parent
		{
			get
			{
				return this._parent;
			}
			set
			{
				this._parent = value;
			}
		}

		/// <summary>
		/// Sets the parameter properties.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="column"></param>
		/// <param name="version"></param>
		/// <param name="precision"></param>
		/// <param name="scale"></param>
		/// <param name="size"></param>
		/// <param name="forceSize"></param>
		/// <param name="offset"></param>
		/// <param name="direction"></param>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <param name="suppress"></param>
		/// <param name="inferType"></param>
		internal void SetProperties(string name, string column, DataRowVersion version, byte precision, byte scale, int size, bool forceSize, int offset, ParameterDirection direction, object value, DbType type, bool suppress, bool inferType)
		{
			this.ParameterName = name;
			this._sourceColumn = column;
			this.SourceVersion = version;
			this.Precision = precision;
			this._scale = scale;
			this._size = size;
			this._forceSize = forceSize;
			this._offset = offset;
			this.Direction = direction;
			if (value is ICloneable)
			{
				value = ((ICloneable) value).Clone();
			}
			this._value = value;
			this.Suppress = suppress;
			this._inferType = inferType;
		}
 
		#endregion

		#region Private Fields

		// Fields
		private ParameterDirection _direction;
		private bool _forceSize;
		private bool _inferType;
		private bool _isNullable;
		private string _name;
		private int _offset;
		private SharpHsqlParameterCollection _parent;
		private byte _precision;
		private byte _scale;
		private int _size;
		private string _sourceColumn;
		private bool _suppress;
		private object _value;
		private DataRowVersion _version;
		private DbType _dbtype;

		#endregion

		#region ICloneable Members

		/// <summary>
		/// Returns a new cloned instance of the current parameter.
		/// </summary>
		/// <returns>The cloned <see cref="SharpHsqlParameter"/> instance.</returns>
		public SharpHsqlParameter Clone()
		{
			SharpHsqlParameter p = new SharpHsqlParameter();
			p.SetProperties(this._name, this._sourceColumn, this._version, this._precision, this._scale, this._size, this._forceSize, this._offset, this._direction, this._value, this.DbType, this._suppress, this._inferType);
			return p;
		}

		/// <summary>
		/// Returns a new cloned instance of the current parameter.
		/// </summary>
		/// <returns>The cloned <see cref="SharpHsqlParameter"/> instance.</returns>
		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion
	}
}
