using System;

namespace System.Data.Hsql.Linq
{
	internal interface ISharpHsqlSchemaExtensions
	{
		void BuildTempSchema(SharpHsqlConnection cnn);
	}
}

