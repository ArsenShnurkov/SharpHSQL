#region Usings
using System;
using System.Collections;
#endregion

#region License
/*
 * User.cs
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
	/// Class representing a database user.
	/// </summary>
	public sealed class User 
	{
		#region Constructor

		/// <summary>
		/// Internal Class constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="password"></param>
		/// <param name="admin"></param>
		/// <param name="pub"></param>
		internal User(string name, string password, bool admin, User pub) 
		{
			_right = new Hashtable();
			_name = name;

			Password = password;

			_administrator = admin;
			_public = pub;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// True if the user is an administrator.
		/// </summary>
		public bool IsAdmin
		{
			get
			{
				return _administrator;
			}
		}

		/// <summary>
		/// Gets the user name.
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
		}

		#endregion 

		#region Internal Properties & Methods.

		/// <summary>
		/// Get or set the user password.
		/// </summary>
		internal string Password
		{
			get
			{
				// necessary to create the script
				return _password;
			}
			set
			{
				_password = value;
			}
		}

		/// <summary>
		/// Get the user rights.
		/// </summary>
		internal Hashtable Rights
		{
			get
			{
				// necessary to create the script
				return _right;
			}
		}

		/// <summary>
		/// Checks the user password.
		/// </summary>
		/// <param name="test">The password to test.</param>
		internal void CheckPassword(string test) 
		{
			Trace.Check(test.Equals(_password), Trace.ACCESS_IS_DENIED);
		}

		/// <summary>
		/// Grant a right for the user on the database object.
		/// </summary>
		/// <param name="dbobject"></param>
		/// <param name="right"></param>
		internal void Grant(string dbobject, AccessType right) 
		{
			AccessType n;

			if (!_right.ContainsKey(dbobject)) 
			{
				n = right;
			} 
			else 
			{
				n = (AccessType) _right[dbobject];
				n = (n | right);
			}

			_right.Add(dbobject, n);
		}

		/// <summary>
		/// Revoke the specified right from a database object.
		/// </summary>
		/// <param name="dbobject"></param>
		/// <param name="right"></param>
		internal void Revoke(string dbobject, AccessType right) 
		{
			AccessType n;

			if (!_right.ContainsKey(dbobject)) 
			{
				n = right;
			} 
			else 
			{
				n = (AccessType) _right[dbobject];
				n = (n & (AccessType.All ^ right));
			}

			_right.Add(dbobject, n);
		}

		/// <summary>
		/// Revoke all permissions.
		/// </summary>
		internal void RevokeAll() 
		{
			_right = null;
			_administrator = false;
		}

		/// <summary>
		/// Checks a database object for the specified right.
		/// </summary>
		/// <param name="dbobject">Database object checking.</param>
		/// <param name="right">Desired right.</param>
		internal void Check(string dbobject, AccessType right) 
		{
			if (_administrator) 
			{
				return;
			}

			AccessType n;

			n = (AccessType) _right[dbobject];

			if ((n & right) != 0) 
			{
				return;
			}

			if (_public != null) 
			{
				n = (AccessType) (_public._right)[dbobject];

				if ((n & right) != 0) 
				{
					return;
				}
			}

			throw Trace.Error(Trace.ACCESS_IS_DENIED);
		}

		/// <summary>
		/// Check if the user has administrative priviledges.
		/// </summary>
		internal void CheckAdmin() 
		{
			Trace.Check(IsAdmin, Trace.ACCESS_IS_DENIED);
		}

		#endregion

		#region Private Vars

		private bool      _administrator;
		private Hashtable _right;
		private string    _name;
		private string    _password;
		private User      _public;

		#endregion
	}
}
