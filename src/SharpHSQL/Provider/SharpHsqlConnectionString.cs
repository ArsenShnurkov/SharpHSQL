#region Usings
using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
#endregion

#region License
/*
 * SharpHsqlConnectionString.cs
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
	/// Helper static class for building SharpHsql connection strings.
	/// </summary>
	internal sealed class SharpHsqlConnectionString
	{
		#region Constructors

		/// <summary>
		/// Static constructor.
		/// </summary>
		static SharpHsqlConnectionString()
		{
			invariantComparer = CultureInfo.InvariantCulture.CompareInfo;
		}

		/// <summary>
		/// Creates a new <see cref="SharpHsqlConnectionString"/> object
		/// using a connection string.
		/// </summary>
		/// <param name="connstring"></param>
		internal SharpHsqlConnectionString( string connstring )
		{
			if( connstring == null || connstring.Length == 0 || connstring.Trim().Length == 0 )
				throw new ArgumentNullException("connstring");

			string[] pairs = connstring.Split(';');
			
			if( pairs.Length < 3 )
				throw new ArgumentException("The connection string is invalid.", "connstring");

			for( int i=0;i<pairs.Length;i++)
			{
				if( pairs[i].Trim() == String.Empty )
					continue;

				string[] pair = pairs[i].Split('=');
				
				if( pair.Length != 2 )
					throw new ArgumentException("The connection string has an invalid parameter.", "connstring");

				string key = pair[0].ToLower().Trim();
				string value = pair[1].ToLower().Trim();

				if( invariantComparer.Compare( key, Initial_Catalog) == 0 ||  
					invariantComparer.Compare( key, DB ) == 0 )
				{
					Database = value;
				}
				if( invariantComparer.Compare( key, User_ID) == 0 ||  
					invariantComparer.Compare( key, UID ) == 0 )
				{
					UserName = value;
				}
				if( invariantComparer.Compare( key, Password) == 0 ||  
					invariantComparer.Compare( key, Pwd ) == 0 )
				{
					UserPassword = value;
				}
			}

			if( Database == string.Empty )
				throw new ArgumentException("Database parameter is invalid in connection string.", "Database");

			if( UserName == string.Empty )
				throw new ArgumentException("UserName parameter is invalid in connection string.", "UserName");

			_connstring = connstring;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Returns the connection string built.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return _connstring;
		}

		#endregion

		#region Public Fields

		/// <summary>
		/// Database name.
		/// </summary>
		public string Database = String.Empty;
		/// <summary>
		/// User name.
		/// </summary>
		public string UserName = String.Empty;
		/// <summary>
		/// User password.
		/// </summary>
		public string UserPassword = String.Empty;

		#endregion

		#region Internal Vars

		/// <summary>
		/// Class used internally for comparisons.
		/// </summary>
		internal static CompareInfo invariantComparer;

		#endregion

		#region Internal String Constants

		internal const string Initial_Catalog = "initial catalog";
		internal const string DB = "database";
		internal const string User_ID = "user id";
		internal const string UID = "uid";
		internal const string Pwd = "pwd";
		internal const string Password = "password";

		#endregion

		#region Private Vars

		private string _connstring = String.Empty;

		#endregion
	}
}
