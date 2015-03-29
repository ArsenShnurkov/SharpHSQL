#region Usings
using System;
#endregion

#region License
/*
 * ExpressionType.cs
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
	#region ExpressionType Enum

	/// <summary>
	/// Enum that defines all types of expressions.
	/// </summary>
	public enum ExpressionType
	{
		// leaf types
		/// <summary>
		/// Simple value
		/// </summary>
		Value = 1, 
		/// <summary>
		/// Database column
		/// </summary>
		DatabaseColumn = 2, 
		/// <summary>
		/// Database query
		/// </summary>
		Query = 3, 
		/// <summary>
		/// Boolean True
		/// </summary>
		True = 4,
		/// <summary>
		/// Value list
		/// </summary>
		ValueList = 5, 
		/// <summary>
		/// Asterix
		/// </summary>
		Asterix = 6, 
		/// <summary>
		/// Function
		/// </summary>
		Function = 7, 
		/// <summary>
		/// Variable
		/// </summary>
		Variable = 8,

		// operations
		/// <summary>
		/// Negate
		/// </summary>
		Negate = 9, 
		/// <summary>
		/// Add
		/// </summary>
		Add = 10, 
		/// <summary>
		/// Subtract
		/// </summary>
		Subtract = 11, 
		/// <summary>
		/// Multiply
		/// </summary>
		Multiply = 12,
		/// <summary>
		/// Divide
		/// </summary>
		Divide = 14, 
		/// <summary>
		/// Concat
		/// </summary>
		Concat = 15,

		// logical operations
		/// <summary>
		/// Not
		/// </summary>
		Not = 20, 
		/// <summary>
		/// Equal
		/// </summary>
		Equal = 21, 
		/// <summary>
		/// BiggerEqual
		/// </summary>
		BiggerEqual = 22, 
		/// <summary>
		/// Bigger
		/// </summary>
		Bigger = 23,
		/// <summary>
		/// Smaller
		/// </summary>
		Smaller = 24, 
		/// <summary>
		/// SmallerEqual
		/// </summary>
		SmallerEqual = 25, 
		/// <summary>
		/// NotEqual
		/// </summary>
		NotEqual = 26,
		/// <summary>
		/// Like
		/// </summary>
		Like = 27, 
		/// <summary>
		/// And
		/// </summary>
		And = 28, 
		/// <summary>
		/// Or
		/// </summary>
		Or = 29,
		/// <summary>
		/// In
		/// </summary>
		In = 30, 
		/// <summary>
		/// Exist
		/// </summary>
		Exists = 31,

		// aggregate functions
		/// <summary>
		/// Count
		/// </summary>
		Count = 40, 
		/// <summary>
		/// Sum
		/// </summary>
		Sum = 41, 
		/// <summary>
		/// Minimum
		/// </summary>
		Minimum = 42, 
		/// <summary>
		/// Maximum
		/// </summary>
		Maximum = 43, 
		/// <summary>
		/// Average
		/// </summary>
		Average = 44,

		// system functions
		/// <summary>
		/// IfNull
		/// </summary>
		IfNull = 60, 
		/// <summary>
		/// Convert
		/// </summary>
		Convert = 61, 
		/// <summary>
		/// CaseWhen
		/// </summary>
		CaseWhen = 62,

		// temporary used during parsing
		/// <summary>
		/// Plus
		/// </summary>
		Plus = 100, 
		/// <summary>
		/// Open
		/// </summary>
		Open = 101, 
		/// <summary>
		/// Close
		/// </summary>
		Close = 102, 
		/// <summary>
		/// Select
		/// </summary>
		Select = 103,
		/// <summary>
		/// Comma
		/// </summary>
		Comma = 104, 
		/// <summary>
		/// StringConcat
		/// </summary>
		StringConcat = 105, 
		/// <summary>
		/// Between
		/// </summary>
		Between = 106,
		/// <summary>
		/// Cast
		/// </summary>
		Cast = 107, 
		/// <summary>
		/// End
		/// </summary>
		End = 108
	}

	#endregion
}
