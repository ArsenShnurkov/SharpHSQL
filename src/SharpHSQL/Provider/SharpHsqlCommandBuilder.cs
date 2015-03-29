#region Usings
using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
#endregion

#region License
/*
 * SharpHsqlCommandBuilder.cs
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
	/// CommandBuilder component for design time.
	/// <seealso cref="SharpHsqlConnection"/>
	/// <seealso cref="SharpHsqlReader"/>
	/// <seealso cref="SharpHsqlParameter"/>
	/// <seealso cref="SharpHsqlTransaction"/>
	/// <seealso cref="SharpHsqlDataAdapter"/>
	/// </summary>
	public class SharpHsqlCommandBuilder : Component
	{
		#region Constructors

		/// <summary>
		/// Component constructor.
		/// </summary>
		/// <param name="container"></param>
		public SharpHsqlCommandBuilder(System.ComponentModel.IContainer container)
		{
			//
			// Required for Windows.Forms Class Composition Designer support
			//
			container.Add((IComponent)this);
			InitializeComponent();
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SharpHsqlCommandBuilder()
		{
			//
			// Required for Windows.Forms Class Composition Designer support
			//
			InitializeComponent();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Constructor using an <see cref="SharpHsqlDataAdapter"/>.
		/// </summary>
		/// <param name="adapter"></param>
		public SharpHsqlCommandBuilder(SharpHsqlDataAdapter adapter)
		{
			GC.SuppressFinalize(this);
			this.DataAdapter = adapter;
		}

		#endregion

		#region Dispose Methods

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Public Methods & Properties

		/// <summary>
		/// Derive command parameters.
		/// </summary>
		/// <param name="command"></param>
		public static void DeriveParameters(SharpHsqlCommand command)
		{
			if (command == null)
			{
				throw new ArgumentNullException("command");
			}
			command.DeriveParameters();
		}

		/// <summary>
		/// Gets the delete command.
		/// </summary>
		/// <returns></returns>
		public SharpHsqlCommand GetDeleteCommand()
		{
			return (SharpHsqlCommand) this.GetBuilder().GetDeleteCommand();
		}
 
		/// <summary>
		/// Gets the insert command.
		/// </summary>
		/// <returns></returns>
		public SharpHsqlCommand GetInsertCommand()
		{
			return (SharpHsqlCommand) this.GetBuilder().GetInsertCommand();
		}

		/// <summary>
		/// Gets the update command.
		/// </summary>
		/// <returns></returns>
		public SharpHsqlCommand GetUpdateCommand()
		{
			return (SharpHsqlCommand) this.GetBuilder().GetUpdateCommand();
		}

		/// <summary>
		/// Refresh the database schema.
		/// </summary>
		public void RefreshSchema()
		{
			this.GetBuilder().RefreshSchema();
		}

		/// <summary>
		/// Get or set the <see cref="SharpHsqlDataAdapter"/> object used.
		/// </summary>
		public SharpHsqlDataAdapter DataAdapter
		{
			get
			{
				return (SharpHsqlDataAdapter) this.GetBuilder().DataAdapter;
			}
			set
			{
				this.GetBuilder().DataAdapter = value;
			}
		}
 
		/// <summary>
		/// Get or set the quote prefix.
		/// </summary>
		public string QuotePrefix
		{
			get
			{
				return this.GetBuilder().QuotePrefix;
			}
			set
			{
				this.GetBuilder().QuotePrefix = value;
			}
		}

		/// <summary>
		/// Get or set the quote suffix.
		/// </summary>
		public string QuoteSuffix
		{
			get
			{
				return this.GetBuilder().QuoteSuffix;
			}
			set
			{
				this.GetBuilder().QuoteSuffix = value;
			}
		}

		#endregion

		#region Private Methods

		private CommandBuilder GetBuilder()
		{
			if (this.cmdBuilder == null)
			{
				this.cmdBuilder = new CommandBuilder();
			}
			return this.cmdBuilder;
		}

		#endregion

		#region Private Vars

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private CommandBuilder cmdBuilder;

		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}
}
