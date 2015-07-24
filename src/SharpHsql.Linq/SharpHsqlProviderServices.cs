namespace System.Data.Hsql.Linq
{
	using System;
	using System.Data.Common;
	using System.Data.Entity.Core.Common;
	using System.Data.Entity.Core.Common.CommandTrees;
	using System.Data.Entity.Core.Metadata.Edm;
	using System.Diagnostics;
	using System.Collections.Generic;
	using System.Text;
	using System.Globalization;

	internal class SharpHsqlProviderServices : DbProviderServices, ISharpHsqlSchemaExtensions
	{
		internal static readonly SharpHsqlProviderServices Instance = new SharpHsqlProviderServices();

		protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest manifest, DbCommandTree commandTree)
		{
			DbCommand prototype = CreateCommand(manifest, commandTree);
			DbCommandDefinition result = this.CreateCommandDefinition(prototype);
			return result;
		}

		private DbCommand CreateCommand(DbProviderManifest manifest, DbCommandTree commandTree)
		{
			if (manifest == null)
				throw new ArgumentNullException("manifest");

			if (commandTree == null)
				throw new ArgumentNullException("commandTree");

			SharpHsqlCommand command = new SharpHsqlCommand();
			try
			{
				List<DbParameter> parameters;
				CommandType commandType;

				command.CommandText = SqlGenerator.GenerateSql((SharpHsqlProviderManifest)manifest, commandTree, out parameters, out commandType);
				command.CommandType = commandType;

				// Get the function (if any) implemented by the command tree since this influences our interpretation of parameters
				EdmFunction function = null;
				if (commandTree is DbFunctionCommandTree)
				{
					function = ((DbFunctionCommandTree)commandTree).EdmFunction;
				}

				// Now make sure we populate the command's parameters from the CQT's parameters:
				foreach (KeyValuePair<string, TypeUsage> queryParameter in commandTree.Parameters)
				{
					SharpHsqlParameter parameter;

					// Use the corresponding function parameter TypeUsage where available (currently, the SSDL facets and 
					// type trump user-defined facets and type in the EntityCommand).
					FunctionParameter functionParameter;
					if (null != function && function.Parameters.TryGetValue(queryParameter.Key, false, out functionParameter))
					{
						parameter = CreateSqlParameter(functionParameter.Name, functionParameter.TypeUsage, functionParameter.Mode, DBNull.Value);
					}
					else
					{
						parameter = CreateSqlParameter(queryParameter.Key, queryParameter.Value, ParameterMode.In, DBNull.Value);
					}

					command.Parameters.Add(parameter);
				}

				// Now add parameters added as part of SQL gen (note: this feature is only safe for DML SQL gen which
				// does not support user parameters, where there is no risk of name collision)
				if (null != parameters && 0 < parameters.Count)
				{
					if (!(commandTree is DbInsertCommandTree) &&
						!(commandTree is DbUpdateCommandTree) &&
						!(commandTree is DbDeleteCommandTree))
					{
						throw new InvalidOperationException("SqlGenParametersNotPermitted");
					}

					foreach (DbParameter parameter in parameters)
					{
						command.Parameters.Add(parameter);
					}
				}

				return command;
			}
			catch
			{
				command.Dispose();
				throw;
			}
		}

		protected override string GetDbProviderManifestToken(DbConnection connection)
		{
			if (String.IsNullOrEmpty(connection.ConnectionString))
				throw new ArgumentNullException("ConnectionString");

			bool parseViaFramework = false;

			if (connection is SharpHsqlConnection)
				parseViaFramework = ((SharpHsqlConnection)connection).ParseViaFramework;

			SortedList<string, string> opts = parseViaFramework ?
				SharpHsqlConnection.ParseConnectionStringViaFramework(connection.ConnectionString, false) :
				SharpHsqlConnection.ParseConnectionString(connection.ConnectionString);

			return SharpHsqlConnection.FindKey(opts, "DateTimeFormat", "ISO8601");
		}

		protected override DbProviderManifest GetDbProviderManifest(string versionHint)
		{
			return new SharpHsqlProviderManifest(versionHint);
		}

		/// <summary>
		/// Creates a SharpHsqlParameter given a name, type, and direction
		/// </summary>
		internal static SharpHsqlParameter CreateSqlParameter(string name, TypeUsage type, ParameterMode mode, object value)
		{
			int? size;

			SharpHsqlParameter result = new SharpHsqlParameter(name, value);

			// .Direction
			ParameterDirection direction = MetadataHelpers.ParameterModeToParameterDirection(mode);
			if (result.Direction != direction)
			{
				result.Direction = direction;
			}

			// .Size and .DbType
			// output parameters are handled differently (we need to ensure there is space for return
			// values where the user has not given a specific Size/MaxLength)
			bool isOutParam = mode != ParameterMode.In;
			DbType sqlDbType = GetSqlDbType(type, isOutParam, out size);
			if (result.DbType != sqlDbType)
			{
				result.DbType = sqlDbType;
			}

			// Note that we overwrite 'facet' parameters where either the value is different or
			// there is an output parameter.
			if (size.HasValue && (isOutParam || result.Size != size.Value))
			{
				result.Size = size.Value;
			}

			// .IsNullable
			bool isNullable = MetadataHelpers.IsNullable(type);
			if (isOutParam || isNullable != result.IsNullable)
			{
				result.IsNullable = isNullable;
			}

			return result;
		}


		/// <summary>
		/// Determines DbType for the given primitive type. Extracts facet
		/// information as well.
		/// </summary>
		private static DbType GetSqlDbType(TypeUsage type, bool isOutParam, out int? size)
		{
			// only supported for primitive type
			PrimitiveTypeKind primitiveTypeKind = MetadataHelpers.GetPrimitiveTypeKind(type);

			size = default(int?);

			switch (primitiveTypeKind)
			{
			case PrimitiveTypeKind.Binary:
				// for output parameters, ensure there is space...
				size = GetParameterSize(type, isOutParam);
				return GetBinaryDbType(type);

			case PrimitiveTypeKind.Boolean:
				return DbType.Boolean;

			case PrimitiveTypeKind.Byte:
				return DbType.Byte;

			case PrimitiveTypeKind.Time:
				return DbType.Time;

			case PrimitiveTypeKind.DateTimeOffset:
				return DbType.DateTimeOffset;

			case PrimitiveTypeKind.DateTime:
				return DbType.DateTime;

			case PrimitiveTypeKind.Decimal:
				return DbType.Decimal;

			case PrimitiveTypeKind.Double:
				return DbType.Double;

			case PrimitiveTypeKind.Guid:
				return DbType.Guid;

			case PrimitiveTypeKind.Int16:
				return DbType.Int16;

			case PrimitiveTypeKind.Int32:
				return DbType.Int32;

			case PrimitiveTypeKind.Int64:
				return DbType.Int64;

			case PrimitiveTypeKind.SByte:
				return DbType.SByte;

			case PrimitiveTypeKind.Single:
				return DbType.Single;

			case PrimitiveTypeKind.String:
				size = GetParameterSize(type, isOutParam);
				return GetStringDbType(type);

			default:
				Debug.Fail("unknown PrimitiveTypeKind " + primitiveTypeKind);
				return DbType.Object;
			}
		}

		/// <summary>
		/// Determines preferred value for SqlParameter.Size. Returns null
		/// where there is no preference.
		/// </summary>
		private static int? GetParameterSize(TypeUsage type, bool isOutParam)
		{
			int maxLength;
			if (MetadataHelpers.TryGetMaxLength(type, out maxLength))
			{
				// if the MaxLength facet has a specific value use it
				return maxLength;
			}
			else if (isOutParam)
			{
				// if the parameter is a return/out/inout parameter, ensure there 
				// is space for any value
				return int.MaxValue;
			}
			else
			{
				// no value
				return default(int?);
			}
		}

		/// <summary>
		/// Chooses the appropriate DbType for the given string type.
		/// </summary>
		private static DbType GetStringDbType(TypeUsage type)
		{
			Debug.Assert(type.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType &&
				PrimitiveTypeKind.String == ((PrimitiveType)type.EdmType).PrimitiveTypeKind, "only valid for string type");

			DbType dbType;

			// Specific type depends on whether the string is a unicode string and whether it is a fixed length string.
			// By default, assume widest type (unicode) and most common type (variable length)
			bool unicode;
			bool fixedLength;
			if (!MetadataHelpers.TryGetIsFixedLength(type, out fixedLength))
			{
				fixedLength = false;
			}

			if (!MetadataHelpers.TryGetIsUnicode(type, out unicode))
			{
				unicode = true;
			}

			if (fixedLength)
			{
				dbType = (unicode ? DbType.StringFixedLength : DbType.AnsiStringFixedLength);
			}
			else
			{
				dbType = (unicode ? DbType.String : DbType.AnsiString);
			}
			return dbType;
		}

		/// <summary>
		/// Chooses the appropriate DbType for the given binary type.
		/// </summary>
		private static DbType GetBinaryDbType(TypeUsage type)
		{
			Debug.Assert(type.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType &&
				PrimitiveTypeKind.Binary == ((PrimitiveType)type.EdmType).PrimitiveTypeKind, "only valid for binary type");

			// Specific type depends on whether the binary value is fixed length. By default, assume variable length.
			bool fixedLength;
			if (!MetadataHelpers.TryGetIsFixedLength(type, out fixedLength))
			{
				fixedLength = false;
			}

			return DbType.Binary;
			//            return fixedLength ? DbType.Binary : DbType.VarBinary;
		}

		#region ISharpHsqlSchemaExtensions Members

		/// <summary>
		/// Creates temporary tables on the connection so schema information can be queried
		/// </summary>
		/// <remarks>
		/// There's a lot of work involved in getting schema information out of SharpHsql, but LINQ expects to
		/// be able to query on schema tables.  Therefore we need to "fake" it by generating temporary tables
		/// filled with the schema of the current connection.  We get away with making this information static
		/// because schema information seems to always be queried on a new connection object, so the schema is
		/// always fresh.
		/// </remarks>
		/// <param name="cnn">The connection upon which to build the schema tables</param>
		void ISharpHsqlSchemaExtensions.BuildTempSchema(SharpHsqlConnection cnn)
		{
			string[] arr = new string[] { "TABLES", "COLUMNS", "VIEWS", "VIEWCOLUMNS", "INDEXES", "INDEXCOLUMNS", "FOREIGNKEYS", "CATALOGS" };

			using (DataTable table = cnn.GetSchema("Tables", new string[] { "temp", null, String.Format("SCHEMA{0}", arr[0]) }))
			{
				if (table.Rows.Count > 0) return;
			}

			for (int n = 0; n < arr.Length; n++)
			{
				using (DataTable table = cnn.GetSchema(arr[n]))
				{
					DataTableToTable(cnn, table, String.Format("SCHEMA{0}", arr[n]));
				}
			}

			using (SharpHsqlCommand cmd = cnn.CreateCommand())
			{
				cmd.CommandText = Properties.Resources.SQL_CONSTRAINTS;
				cmd.ExecuteNonQuery();

				cmd.CommandText = Properties.Resources.SQL_CONSTRAINTCOLUMNS;
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Turn a datatable into a table in the temporary database for the connection
		/// </summary>
		/// <param name="cnn">The connection to make the temporary table in</param>
		/// <param name="table">The table to write out</param>
		/// <param name="dest">The temporary table name to write to</param>
		private void DataTableToTable(SharpHsqlConnection cnn, DataTable table, string dest)
		{
			StringBuilder sql = new StringBuilder();
			SharpHsqlCommandBuilder builder = new SharpHsqlCommandBuilder();

			using (SharpHsqlCommand cmd = cnn.CreateCommand())
			using (DataTable source = new DataTable())
			{
				sql.AppendFormat(CultureInfo.InvariantCulture, "CREATE TEMP TABLE {0} (", builder.QuoteIdentifier(dest));
				string separator = String.Empty;
				foreach (DataColumn dc in table.Columns)
				{
					DbType dbtypeName = SharpHsqlConvert.TypeToDbType(dc.DataType);
					string typeName = SharpHsqlConvert.DbTypeToTypeName(dbtypeName);

					sql.AppendFormat(CultureInfo.InvariantCulture, "{2}{0} {1} COLLATE NOCASE", builder.QuoteIdentifier(dc.ColumnName), typeName, separator);
					separator = ", ";
				}
				sql.Append(")");

				cmd.CommandText = sql.ToString();
				cmd.ExecuteNonQuery();

				cmd.CommandText = String.Format("SELECT * FROM TEMP.{0} WHERE 1=2", builder.QuoteIdentifier(dest));
				using (SharpHsqlDataAdapter adp = new SharpHsqlDataAdapter(cmd))
				{
					builder.DataAdapter = adp;

					adp.Fill(source);

					foreach (DataRow row in table.Rows)
					{
						object[] arr = row.ItemArray;

						source.Rows.Add(arr);
					}
					adp.Update(source);
				}
			}
		}

		#endregion
	}
}

