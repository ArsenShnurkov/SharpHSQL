using System.Diagnostics;

namespace System.Data.Hsql
{
	using System.Data.Common;
	
	/// <summary>
	/// Sharp hsql db provider factory.
	/// </summary>
	public class SharpHsqlDbProviderFactory : DbProviderFactory
	{
		/// <summary>
		/// Static instance member which returns an instanced SharpHsqlFactory class.
		/// </summary>
		public static readonly SharpHsqlDbProviderFactory Instance = new SharpHsqlDbProviderFactory();	

		public SharpHsqlDbProviderFactory()
		{
			Trace.WriteLine ("SharpHsqlDbProviderFactory.ctor()");
		}
		/// <summary>
		/// Returns a new SharpHsqlCommand object.
		/// </summary>
		/// <returns>A SharpHsqlCommand object.</returns>
		public override DbCommand CreateCommand()
		{
			return new SharpHsqlCommand();
		}
		/// <summary>
		/// Returns a new SharpHsqlCommandBuilder object.
		/// </summary>
		/// <returns>A SharpHsqlCommandBuilder object.</returns>
		public override DbCommandBuilder CreateCommandBuilder()
		{
			return new SharpHsqlCommandBuilder();
		}
		/// <summary>
		/// Creates a new SharpHsqlConnection.
		/// </summary>
		/// <returns>A SharpHsqlConnection object.</returns>
		public override DbConnection CreateConnection()
		{
			return new SharpHsqlConnection();
		}

		/// <summary>
		/// Creates a new SharpHsqlConnectionStringBuilder.
		/// </summary>
		/// <returns>A SharpHsqlConnectionStringBuilder object.</returns>
		public override DbConnectionStringBuilder CreateConnectionStringBuilder()
		{
			return new SharpHsqlConnectionStringBuilder(String.Empty);
		}

		/// <summary>
		/// Creates a new SharpHsqlDataAdapter.
		/// </summary>
		/// <returns>A SharpHsqlDataAdapter object.</returns>
		public override DbDataAdapter CreateDataAdapter()
		{
			return new SharpHsqlDataAdapter();
		}

		/// <summary>
		/// Creates a new SharpHsqlParameter.
		/// </summary>
		/// <returns>A SharpHsqlParameter object.</returns>
		public override DbParameter CreateParameter()
		{
			return new SharpHsqlParameter();
		}
	}
}
