#region Usings
using System;
using System.Collections;
using System.Text;
#endregion

#region License
/*
 * SharpHsqlException.cs
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
	/// Exception class for Hsql ADO.NET data provider.
	/// <seealso cref="SharpHsqlConnection"/>
	/// <seealso cref="SharpHsqlReader"/>
	/// <seealso cref="SharpHsqlParameter"/>
	/// <seealso cref="SharpHsqlTransaction"/>
	/// <seealso cref="SharpHsqlCommand"/>
	/// <seealso cref="SharpHsqlDataAdapter"/>
	/// </summary>
	/// <remarks>Not serializable for Compact Framework 1.0</remarks>
	#if !POCKETPC
	[Serializable]
	#endif
	public sealed class SharpHsqlException : SystemException
	{
		#region Constructors

		/// <summary>
		/// Internal default constructor.
		/// </summary>
		internal SharpHsqlException() : base()
		{
			#if !POCKETPC
			base.HResult = -2146232060;
			#endif
		}

		/// <summary>
		/// Constructor using an error string.
		/// </summary>
		/// <param name="error"></param>
		internal SharpHsqlException( string error ) : this()
		{
			if( error == null )
				throw new ArgumentNullException("error");

			int number = 0;

			try
			{
				#if !POCKETPC
				if( Char.IsDigit( error, 0 ) )
				#else
				if( Char.IsDigit( error.ToCharArray()[0] ) )
				#endif
					number = int.Parse(error.Substring(0, 5));
				else
					number = int.Parse(error.Substring(1, 4));
			}
			catch{}

			string message = error;

			SharpHsqlError e = new SharpHsqlError( message, number, String.Empty, String.Empty);

			this.Errors.Add( e );
		}

		#endregion 

		#region Serialization methods

		#if !POCKETPC
		/// <summary>
		/// Deserialization constructor.
		/// </summary>
		/// <remarks>Not supported on Compact Framework 1.0</remarks>
		/// <param name="si"></param>
		/// <param name="sc"></param>
		private SharpHsqlException(SerializationInfo si, StreamingContext sc) : this()
		{
			this._errors = (SharpHsqlErrorCollection) si.GetValue("Errors", typeof(SharpHsqlErrorCollection));
		}
		#endif

		#if !POCKETPC
		/// <summary>
		/// Serialization method.
		/// </summary>
		/// <remarks>Not supported on Compact Framework 1.0</remarks>
		/// <param name="si"></param>
		/// <param name="context"></param>
		public override void GetObjectData(SerializationInfo si, StreamingContext context)
		{
			if (si == null)
			{
				throw new ArgumentNullException("si");
			}
			si.AddValue("Errors", this._errors, typeof(SharpHsqlErrorCollection));
			base.GetObjectData(si, context);
		}
		#endif

		#endregion

		#region Public Properties

		/// <summary>
		/// Error collection.
		/// </summary>
		public SharpHsqlErrorCollection Errors
		{
			get
			{
				if (this._errors == null)
				{
					this._errors = new SharpHsqlErrorCollection();
				}
				return this._errors;
			}
		}

		/// <summary>
		/// Exception message.
		/// </summary>
		public override string Message
		{
			get
			{
				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < this.Errors.Count; i++)
				{
					if (i > 0)
					{
						builder.Append("\r\n");
					}
					builder.Append(((SharpHsqlError)this.Errors[i]).Message);
				}
				return builder.ToString();
			}
		}

		/// <summary>
		/// Exception error number.
		/// </summary>
		public int Number
		{
			get
			{
				return this.Errors[0].Number;
			}
		}

		/// <summary>
		/// Procedure where the exception was generated.
		/// </summary>
		public string Procedure
		{
			get
			{
				return this.Errors[0].Procedure;
			}
		}

		#if !POCKETPC
		/// <summary>
		/// Source of the error.
		/// </summary>
		/// <remarks>Not supported on Compact Framework 1.0</remarks>
		public override string Source
		{
			get
			{
				return this.Errors[0].Source;
			}
		}
		#endif

		#endregion

		#region Private Vars

		// Fields
		private SharpHsqlErrorCollection _errors;

		#endregion
	}
 

}
