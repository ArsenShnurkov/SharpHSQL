#region Usings
using System;
using System.Data;
using System.Data.Common;
using System.Collections;
#endregion

#region License
/*
 * SharpHsqlDataAdapter.cs
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
	/// Data adapter class for SharpHsql.
	/// <seealso cref="SharpHsqlConnection"/>
	/// <seealso cref="SharpHsqlReader"/>
	/// <seealso cref="SharpHsqlParameter"/>
	/// <seealso cref="SharpHsqlTransaction"/>
	/// <seealso cref="SharpHsqlCommand"/>
	/// </summary>
	public sealed class SharpHsqlDataAdapter : DbDataAdapter, IDbDataAdapter, ICloneable, IDataAdapter
	{
		#region Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		public SharpHsqlDataAdapter() : base()
		{
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Constructor using a <see cref="SharpHsqlCommand"/> object.
		/// </summary>
		/// <param name="selectCommand"></param>
		public SharpHsqlDataAdapter(SharpHsqlCommand selectCommand) : this()
		{
			_selectCommand = selectCommand;
		}

		/// <summary>
		/// Internal constructor used for cloning.
		/// </summary>
		/// <param name="adapter"></param>
		internal SharpHsqlDataAdapter(DbDataAdapter adapter) : base(adapter)
		{
		}
 
		/// <summary>
		/// Constructor using a command text string.
		/// </summary>
		/// <param name="selectCommandText"></param>
		public SharpHsqlDataAdapter(string selectCommandText) : this()
		{
			_selectCommand = new SharpHsqlCommand(selectCommandText, new SharpHsqlConnection());
		}

		/// <summary>
		/// Constructor using a command text string and a select connection string.
		/// </summary>
		/// <param name="selectCommandText"></param>
		/// <param name="selectConnectionString"></param>
		public SharpHsqlDataAdapter(string selectCommandText, string selectConnectionString)
		{
			_selectCommand = new SharpHsqlCommand(selectCommandText, new SharpHsqlConnection(selectConnectionString));
		}
 
		/// <summary>
		/// Constructor using a command text string and a select connection object.
		/// </summary>
		/// <param name="selectCommandText"></param>
		/// <param name="selectConnection"></param>
		public SharpHsqlDataAdapter(string selectCommandText, SharpHsqlConnection selectConnection) : this()
		{
			_selectCommand = new SharpHsqlCommand(selectCommandText, selectConnection);
		}

		#endregion

		#region DbDataAdapter Overrides

		/// <summary>
		/// Creates a new <see cref="RowUpdatedEventArgs"/> to fire the RowUpdated event.
		/// </summary>
		/// <param name="dataRow"></param>
		/// <param name="command"></param>
		/// <param name="statementType"></param>
		/// <param name="tableMapping"></param>
		/// <returns></returns>
		protected override RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
		{
			return new SharpHsqlRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
		}

		/// <summary>
		/// Creates a new <see cref="RowUpdatingEventArgs"/> to fire the RowUpdating event.
		/// </summary>
		/// <param name="dataRow"></param>
		/// <param name="command"></param>
		/// <param name="statementType"></param>
		/// <param name="tableMapping"></param>
		/// <returns></returns>
		protected override RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
		{
			return new SharpHsqlRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);
		}

		/// <summary>
		/// Fires the RowUpdated event.
		/// </summary>
		/// <param name="value"></param>
		protected override void OnRowUpdated(RowUpdatedEventArgs value)
		{
			#if !POCKETPC
			SharpHsqlRowUpdatedEventHandler handler = (SharpHsqlRowUpdatedEventHandler) base.Events[EventRowUpdated];
			if ((handler != null) && (value is SharpHsqlRowUpdatedEventArgs))
			{
				handler(this, (SharpHsqlRowUpdatedEventArgs) value);
			}
			#endif
		}

		/// <summary>
		/// Fires the RowUpdating event.
		/// </summary>
		/// <param name="value"></param>
		protected override void OnRowUpdating(RowUpdatingEventArgs value)
		{
			#if !POCKETPC
			SharpHsqlRowUpdatingEventHandler handler = (SharpHsqlRowUpdatingEventHandler) base.Events[EventRowUpdating];
			if ((handler != null) && (value is SharpHsqlRowUpdatingEventArgs))
			{
				handler(this, (SharpHsqlRowUpdatingEventArgs) value);
			}
			#endif
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Get or set the select command used.
		/// </summary>
		public SharpHsqlCommand SelectCmd
		{
			get { return _selectCommand;  }
			set { _selectCommand = value; }
		}

		/// <summary>
		/// Get or set the update command used.
		/// </summary>
		public SharpHsqlCommand UpdateCmd
		{
			get { return _updateCommand;  }
			set { _updateCommand = value; }
		}

		/// <summary>
		/// Get or set the insert command used.
		/// </summary>
		public SharpHsqlCommand InsertCmd
		{
			get { return _insertCommand;  }
			set { _insertCommand = value; }
		}

		/// <summary>
		/// Get or set the delete command used.
		/// </summary>
		public SharpHsqlCommand DeleteCmd
		{
			get { return _deleteCommand;  }
			set { _deleteCommand = value; }
		}

		#endregion

		#region Dispose Methods

		/// <summary>
		/// Clean up any used resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.tableMappings = null;
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Internal Methods

		internal int IndexOfDataSetTable(string dataSetTable)
		{
			if (this.tableMappings != null)
			{
				return ((DataTableMappingCollection)this.TableMappings).IndexOfDataSetTable(dataSetTable);
			}
			return -1;
		}

		internal DataTableMapping GetTableMappingBySchemaAction(string sourceTableName, string dataSetTableName, MissingMappingAction mappingAction)
		{
			return DataTableMappingCollection.GetTableMappingBySchemaAction(this.tableMappings, sourceTableName, dataSetTableName, mappingAction);
		}

		internal void ClearDataSet(DataSet dataSet)
		{
			dataSet.Reset();
		}

		#endregion

		#region Public Events

		#if !POCKETPC
		/// <summary>
		/// RowUpdated event.
		/// </summary>
		/// <remarks>Not supportes on Compact Framework 1.0</remarks>
		public event SharpHsqlRowUpdatedEventHandler RowUpdated
		{
			add
			{
				base.Events.AddHandler(EventRowUpdated, value);
			}
			remove
			{
				base.Events.RemoveHandler(EventRowUpdated, value);
			}
		}

		/// <summary>
		/// Row updating event.
		/// </summary>
		/// <remarks>Not supportes on Compact Framework 1.0</remarks>
		public event SharpHsqlRowUpdatingEventHandler RowUpdating
		{
			add
			{
				SharpHsqlRowUpdatingEventHandler handler = (SharpHsqlRowUpdatingEventHandler) base.Events[EventRowUpdating];
				if ((handler != null) && (value.Target is CommandBuilder))
				{
					SharpHsqlRowUpdatingEventHandler builder = (SharpHsqlRowUpdatingEventHandler) CommandBuilder.FindBuilder(handler);
					if (builder != null)
					{
						base.Events.RemoveHandler(EventRowUpdating, builder);
					}
				}
				base.Events.AddHandler(EventRowUpdating, value);
			}
			remove
			{
				base.Events.RemoveHandler(EventRowUpdating, value);
			}
		}
		#endif

		#endregion

		#region Private Fields

		internal static readonly object EventRowUpdated = null;
		internal static readonly object EventRowUpdating = null;

		private SharpHsqlCommand _selectCommand = null;
		private SharpHsqlCommand _deleteCommand = null;
		private SharpHsqlCommand _insertCommand = null;
		private SharpHsqlCommand _updateCommand = null;

		////////////////////
		// Private Data Members
		////////////////////
		//private bool acceptChangesDuringFill;
		//private bool continueUpdateOnError;
		//private MissingMappingAction missingMappingAction;
		//private MissingSchemaAction missingSchemaAction;
		private DataTableMappingCollection tableMappings;

		#endregion

		#region ICloneable Members

		/// <summary>
		/// Returns a clone of the current instance.
		/// </summary>
		/// <returns>A new <see cref="SharpHsqlDataAdapter"/> object clone of the current.</returns>
		public SharpHsqlDataAdapter Clone()
		{
			#if !POCKETPC
			return new SharpHsqlDataAdapter(this);
			#else
			return new SharpHsqlDataAdapter(this);
			#endif
		}

		/// <summary>
		/// Returns a clone of the current instance.
		/// </summary>
		/// <returns>A new <see cref="SharpHsqlDataAdapter"/> object clone of the current.</returns>
		object ICloneable.Clone()
		{	
			return Clone();
		}

		#endregion

		#region IDataAdapter Members

		/// <summary>
		/// Fills a <see cref="DataSet"/> object.
		/// </summary>
		/// <param name="dataSet"></param>
		/// <returns></returns>
		public override int Fill(DataSet dataSet)
		{
			return base.Fill( dataSet );
		}

		/// <summary>
		/// Get the fill parameters.
		/// </summary>
		/// <returns></returns>
		public override IDataParameter[] GetFillParameters()
		{
			return base.GetFillParameters();
		}

		/// <summary>
		/// Fills the schema.
		/// </summary>
		/// <param name="dataSet"></param>
		/// <param name="schemaType"></param>
		/// <returns>The schema <see cref="DataTable"/>.</returns>
		public override DataTable[] FillSchema(DataSet dataSet, System.Data.SchemaType schemaType)
		{
			return base.FillSchema( dataSet, schemaType );
		}

		/// <summary>
		/// Update the database using the passed <see cref="DataSet"/>.
		/// </summary>
		/// <param name="dataSet"></param>
		/// <returns></returns>
		public override int Update(DataSet dataSet)
		{
			return base.Update( dataSet );
		}

		#endregion
	}
}
