#region Usings
using System;
#endregion

#region License
/*
 * CommandBuilderBehavior.cs
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
	#region CommandBuilderBehavior

	/// <summary>
	/// Enum describing the command builder behavior.
	/// <seealso cref="CommandBuilder"/>
	/// </summary>
	[FlagsAttribute()]
	enum CommandBuilderBehavior
	{
		/// <summary>
		/// Default behavior.
		/// </summary>
		Default = 0,
		/// <summary>
		/// When doing update use the original value if not changed.
		/// </summary>
		UpdateSetSameValue = 1,
		/// <summary>
		/// Use row version where doing updates.
		/// </summary>
		UseRowVersionInUpdateWhereClause = 2,
		/// <summary>
		/// Use row version where doing deletes.
		/// </summary>
		UseRowVersionInDeleteWhereClause = 4,
		/// <summary>
		/// Use row version in selects.
		/// </summary>
		UseRowVersionInWhereClause = 6,
		/// <summary>
		/// Compare matching row using only primary key and not all columns when updating.
		/// </summary>
		PrimaryKeyOnlyUpdateWhereClause = 16,
		/// <summary>
		/// Compare matching row using only primary key and not all columns when deleting.
		/// </summary>
		PrimaryKeyOnlyDeleteWhereClause = 32,
		/// <summary>
		/// Compare matching row using only primary key and not all columns.
		/// </summary>
		PrimaryKeyOnlyWhereClause = 48,
	}

	#endregion
}
