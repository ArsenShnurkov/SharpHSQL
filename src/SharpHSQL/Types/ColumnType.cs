#region Usings
using System;
#endregion

#region License
/*
 * ColumnType.cs
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
	#region ColumnType Enum

	/// <summary>
	/// Enum that defines all the data types supported.
	/// </summary>
	public enum ColumnType
	{
		/// <summary>
		/// Boolean data type.
		/// </summary>
		Bit = -7,
		/// <summary>
		/// Byte data type.
		/// </summary>
		TinyInt = -6,
		/// <summary>
		/// Long data type.
		/// </summary>
		BigInt = -5,
		/// <summary>
		/// Binary data type.
		/// </summary>
		LongVarBinary = -4,
		/// <summary>
		/// Binary data type.
		/// </summary>
		VarBinary = -3,
		/// <summary>
		/// Binary data type.
		/// </summary>
		Binary = -2,
		/// <summary>
		/// String data type.
		/// </summary>
		LongVarChar = -1,
		/// <summary>
		/// String data type.
		/// </summary>
		Char = 1,
		/// <summary>
		/// Numeric data type.
		/// </summary>
		Numeric = 2,
		/// <summary>
		/// Decimal data type.
		/// </summary>
		DbDecimal = 3,
		/// <summary>
		/// Int data type.
		/// </summary>
		Integer = 4,
		/// <summary>
		/// Short data type.
		/// </summary>
		SmallInt = 5,
		/// <summary>
		/// Float data type.
		/// </summary>
		Float = 6,
		/// <summary>
		/// Real data type.
		/// </summary>
		Real = 7,
		/// <summary>
		/// Double data type.
		/// </summary>
		DbDouble = 8,
		/// <summary>
		/// String data type.
		/// </summary>
		VarChar = 12,
		/// <summary>
		/// Date data type.
		/// </summary>
		Date = 91,
		/// <summary>
		/// Time data type.
		/// </summary>
		Time = 92,
		/// <summary>
		/// Timestamp data type.
		/// </summary>
		Timestamp = 93,
		/// <summary>
		/// Object data type.
		/// </summary>
		Other = 1111,
		/// <summary>
		/// Null data type.
		/// </summary>
		Null = 0,
		/// <summary>
		/// String data type.
		/// </summary>
		VarCharIgnoreCase = 100,	    // this is the only non-standard type
		/// <summary>
		/// Guid data type.
		/// </summary>
		UniqueIdentifier = 101 // Guid
	}

	#endregion
}
