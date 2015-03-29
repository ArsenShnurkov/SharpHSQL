#region Usings
using System;
using System.Collections;
using System.Text;
#endregion

#region License
/*
 * Access.cs
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
	/// Access Class.
	/// The collection (ArrayList) of User object instances within a specific
	/// database.  Methods are provided for creating, modifying and deleting users,
	/// as well as manipulating their access rights to the database objects.
	/// <seealso cref="User"/>
	/// </summary>
	/// <remarks>version 1.0.0.1</remarks>
	sealed class Access 
	{
		private ArrayList   uUser;
		private User		uPublic;

		/// <summary>
		/// Access Class constructor.
		/// Creates a new ArrayList to contain the User object instances, as well
		/// as creating an initial PUBLIC user, with no password.
		/// </summary>
		public Access() 
		{
			uUser = new ArrayList();
			uPublic = CreateUser("PUBLIC", null, false);
		}		
		
		/// <summary>
		/// GetRight method declaration.
		/// This GetRight method takes a string argument of the name of the access right.
		/// </summary>
		/// <param name="right">A string representation of the right.</param>
		/// <returns>A static int representing the string right passed in.</returns>
		public static AccessType GetRight(string right) 
		{
			if (right.Equals("ALL")) 
			{
				return AccessType.All;
			} 
			else if (right.Equals("SELECT")) 
			{
				return AccessType.Select;
			} 
			else if (right.Equals("UPDATE")) 
			{
				return AccessType.Update;
			} 
			else if (right.Equals("DELETE")) 
			{
				return AccessType.Delete;
			} 
			else if (right.Equals("INSERT")) 
			{
				return AccessType.Insert;
			}

			throw Trace.Error(Trace.UnexpectedToken, right);
		}

		/// <summary>
		/// GetRight method declaration.
		/// This GetRight method takes a int argument of the access right.
		/// </summary>
		/// <param name="right">A static int representing the right passed in</param>
		/// <returns>A string representation of the right or rights associated with the argument.</returns>
		public static string GetRight(AccessType right) 
		{

			if (right == AccessType.All) 
			{
				return "ALL";
			} 
			else if (right == AccessType.None) 
			{
				return null;
			}

			StringBuilder b = new StringBuilder();

			if ((right & AccessType.Select) != 0) 
			{
				b.Append("SELECT,");
			}

			if ((right & AccessType.Update) != 0) 
			{
				b.Append("UPDATE,");
			}

			if ((right & AccessType.Delete) != 0) 
			{
				b.Append("DELETE,");
			}

			if ((right & AccessType.Insert) != 0) 
			{
				b.Append("INSERT,");
			}

			string s = b.ToString();

			return s.Substring(0, s.Length - 1);
		}
		
		/// <summary>
		/// CreateUser method declaration.
		/// This method is used to create a new user.  The collection of users
		/// is first checked for a duplicate name, and an exception will be thrown
		/// if a user of the same name already exists.
		/// </summary>
		/// <param name="name">User login</param>
		/// <param name="password">Plaintext password</param>
		/// <param name="admin">Is this a database admin user?</param>
		/// <returns>An instance of the newly created User object</returns>
		public User CreateUser(string name, string password, bool admin) 
		{
			for (int i = 0; i < uUser.Count; i++) 
			{
				User u = (User)uUser[i];

				if (u != null && u.Name.Equals(name)) 
				{
					throw Trace.Error(Trace.USER_ALREADY_EXISTS, name);
				}
			}

			User unew = new User(name, password, admin, uPublic);

			uUser.Add(unew);

			return unew;
		}
		
		/// <summary>
		/// DropUser method declaration.
		/// This method is used to drop a user.  Since we are using a vector
		/// to hold the User objects, we must iterate through the ArrayList looking
		/// for the name.  The user object is currently set to null, and all access
		/// rights revoked.
		/// </summary>
		/// <remarks>
		/// An ACCESS_IS_DENIED exception will be thrown if an attempt
		/// is made to drop the PUBLIC user.
		/// </remarks>
		/// <param name="name">name of the user to be dropped</param>
		public void DropUser(string name) 
		{
			Trace.Check(!name.Equals("PUBLIC"), Trace.ACCESS_IS_DENIED);

			for (int i = 0; i < uUser.Count; i++) 
			{
				User u = (User) uUser[i];

				if (u != null && u.Name.Equals(name)) 
				{

					// todo: find a better way. Problem: removeElementAt would not
					// work correctly while others are connected
					uUser[i] = null;
					u.RevokeAll();    // in case the user is referenced in another way

					return;
				}
			}

			throw Trace.Error(Trace.USER_NOT_FOUND, name);
		}
		
		/// <summary>
		/// GetUser method declaration.
		/// This method is used to return an instance of a particular User object,
		/// given the user name and password.
		/// </summary>
		/// <remarks>
		/// An ACCESS_IS_DENIED exception will be thrown if an attempt
		/// is made to get the PUBLIC user.
		/// </remarks>
		/// <param name="name">user name</param>
		/// <param name="password">user password</param>
		/// <returns>The requested User object</returns>
		public User GetUser(string name, string password) 
		{
			Trace.Check(!name.Equals("PUBLIC"), Trace.ACCESS_IS_DENIED);

			if (name == null) 
			{
				name = "";
			}

			if (password == null) 
			{
				password = "";
			}

			User u = Get(name);

			u.CheckPassword(password);

			return u;
		}
		
		/// <summary>
		/// GetUsers method declaration.
		/// This method is used to access the entire ArrayList of User objects for this database.
		/// </summary>
		/// <returns>The ArrayList of our User objects</returns>
		public ArrayList GetUsers() 
		{
			return uUser;
		}
		
		/// <summary>
		/// Grant method declaration.
		/// This method is used to grant a user rights to database objects.
		/// </summary>
		/// <param name="name">name of the user</param>
		/// <param name="dbobject">object in the database</param>
		/// <param name="right">right to grant to the user</param>
		public void Grant(string name, string dbobject, AccessType right) 
		{
			Get(name).Grant(dbobject, right);
		}
		
		/// <summary>
		/// Revoke method declaration.
		/// This method is used to revoke a user's rights to database objects.
		/// </summary>
		/// <param name="name">name of the user</param>
		/// <param name="dbobject">object in the database</param>
		/// <param name="right">right to grant to the user</param>
		public void Revoke(string name, string dbobject, AccessType right) 
		{
			Get(name).Revoke(dbobject, right);
		}
		
		/// <summary>
		/// This private method is used to access the User objects in the collection
		/// and perform operations on them.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private User Get(string name) 
		{
			for (int i = 0; i < uUser.Count; i++) 
			{
				User u = (User) uUser[i];

				if (u != null && u.Name.Equals(name)) 
				{
					return u;
				}
			}

			throw Trace.Error(Trace.USER_NOT_FOUND, name);
		}
	}
}
