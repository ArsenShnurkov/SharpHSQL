using System;
using System.Collections;
using System.Data;
using System.Data.Hsql;

namespace Providertest
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			SharpHsqlConnection conn = new SharpHsqlConnection("Initial Catalog=mytest;User Id=sa;Pwd=;");

			byte[] data = new byte[]{255,255,255,255,255,255,255,255,255,255};
			string base64photo = Convert.ToBase64String(data, 0, data.Length);

			try
			{
				conn.Open();

				SharpHsqlCommand cmd = new SharpHsqlCommand("", conn);

				int res;

				Console.Write("Create table (y/n)?");
				string create = Console.ReadLine();
				if( create.ToLower() == "y" )
				{
					cmd.CommandText = "DROP TABLE IF EXIST \"data\";CREATE TABLE \"data\" (\"id\" int NOT NULL PRIMARY KEY, \"MyObject\" OBJECT);";
					res = cmd.ExecuteNonQuery();

					cmd.CommandText = "DROP TABLE IF EXIST \"clients\";CREATE TABLE \"clients\" (\"id\" int NOT NULL IDENTITY PRIMARY KEY, \"DoubleValue\" double, \"nombre\" char, \"photo\" varbinary, \"created\" date );";
					res = cmd.ExecuteNonQuery();

					SharpHsqlTransaction tran = conn.BeginTransaction();

					cmd = new SharpHsqlCommand("", conn);

					for(int i=0;i<10;i++)
					{
						cmd.CommandText = "INSERT INTO \"clients\" (\"DoubleValue\", \"nombre\", \"photo\", \"created\") VALUES (1.1, 'NOMBRE" + i.ToString() + "', '" + base64photo + "', NOW() );";
						res = cmd.ExecuteNonQuery();
						cmd.CommandText = "CALL IDENTITY();";
						int id = (int)cmd.ExecuteScalar();
						Console.WriteLine("Inserted id={0}", id );		
					}

					cmd.CommandText = "DROP TABLE IF EXIST \"books\";CREATE TABLE \"books\" (\"id\" INT NOT NULL PRIMARY KEY, \"name\" char, \"author\" char, \"qty\" int, \"value\" numeric);";
					res = cmd.ExecuteNonQuery();

					cmd.CommandText = "INSERT INTO \"books\" VALUES (1, 'Book000', 'Any', 1, 23.5);";
					res = cmd.ExecuteNonQuery();
					cmd.CommandText = "INSERT INTO \"books\" VALUES (2, 'Book001', 'Andy', 2, 43.9);";
					res = cmd.ExecuteNonQuery();
					cmd.CommandText = "INSERT INTO \"books\" VALUES (3, 'Book002', 'Andy', 3, 37.25);";
					res = cmd.ExecuteNonQuery();
					tran.Commit();
				}

				Console.WriteLine();


				
				Console.Write("Do Bulk INSERTS (y/n)?");
				string bulk = Console.ReadLine();
				if( bulk.ToLower() == "y" )
				{
					SharpHsqlTransaction tran = conn.BeginTransaction();

					cmd = new SharpHsqlCommand("", conn);

					for(int i=0;i<1000;i++)
					{
						cmd.CommandText = "INSERT INTO \"clients\" (\"DoubleValue\", \"nombre\", \"photo\", \"created\") VALUES (1.1, 'NOMBRE" + i.ToString() + "', '" + base64photo + "', NOW() );";
						res = cmd.ExecuteNonQuery();
					}

					tran.Commit();

					Console.WriteLine("Inserted 1000 new clients.");
					Console.WriteLine();
				}

				cmd = new SharpHsqlCommand("", conn);

				cmd.CommandText = "SELECT \"clients\".\"id\", \"clients\".\"DoubleValue\", \"clients\".\"nombre\",  \"clients\".\"photo\", \"clients\".\"created\" FROM \"clients\" ORDER BY \"clients\".\"id\" ";
				IDataReader reader = cmd.ExecuteReader();
				
				byte[] photo = null;

				while( reader.Read() )
				{
					long len = reader.GetBytes(3, 0, null, 0, 0);
					photo = new byte[len];
					reader.GetBytes(3, 0, photo, 0, (int)len);
					Console.WriteLine("id={0}, doubleValue={1}, nombre={2}, photo={3}, created={4}", reader.GetInt32(0), reader.GetDouble(1), reader.GetString(2), photo.Length, reader.GetDateTime(4).ToString("yyyy.MM.dd hh:mm:ss.fffffff") );
				}

				reader.Close();

				Console.WriteLine();

				cmd.CommandText = "SELECT * FROM \"books\"";
				reader = cmd.ExecuteReader();

				while( reader.Read() )
				{
					Console.WriteLine("id={0}book={1},\tauthor={2},\tqty={3},\tvalue={4}", reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetDecimal(4) );
				}

				Console.WriteLine();

				reader.Close();

				Console.WriteLine();

				cmd.CommandText = "SELECT * FROM \"books\" ORDER BY \"value\"";
				reader = cmd.ExecuteReader();

				while( reader.Read() )
				{
					Console.WriteLine("id={0}book={1},\tauthor={2},\tqty={3},\tvalue={4}", reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetDecimal(4) );
				}

				Console.WriteLine();

				reader.Close();

				Console.WriteLine();

				cmd.CommandText = "SELECT COUNT(*) as CNT, SUM(\"value\") FROM \"books\" WHERE \"author\" = 'Andy'";
				reader = cmd.ExecuteReader();

				while( reader.Read() )
				{
					Console.WriteLine("count={0},\tvalue={1}", reader.GetInt32(0), reader.GetDecimal(1) );
				}

				Console.WriteLine();

				reader.Close();

				cmd.CommandText = "SELECT \"name\", \"author\", SUM(\"value\") FROM \"books\" WHERE \"author\" = 'Andy' GROUP BY \"name\", \"author\";";
				reader = cmd.ExecuteReader();

				while( reader.Read() )
				{
					Console.WriteLine("name={0},\tauthor={1},\tvalue={2}", reader.GetString(0), reader.GetString(1), reader.GetDecimal(2) );
				}

				Console.WriteLine();

				reader.Close();

				cmd.CommandText = "SELECT \"name\", SUM(\"value\") FROM \"books\" WHERE \"author\" = 'Andy' GROUP BY \"name\";";
				reader = cmd.ExecuteReader();

				while( reader.Read() )
				{
					Console.WriteLine("name={0},\tvalue={1}", reader.GetString(0), reader.GetDecimal(1) );
				}

				Console.WriteLine();

				reader.Close();
				cmd.CommandText = "DELETE FROM \"clients\" WHERE \"clients\".\"id\" = 6;";
				res = cmd.ExecuteNonQuery();

				Console.WriteLine();

				cmd.CommandText = "SELECT MAX(\"clients\".\"id\") FROM \"clients\";";
				object result = cmd.ExecuteScalar();
				if( result != null )
				{
					res = (int)result;
					Console.WriteLine("MAX=" + res);
				}

				cmd.CommandText = "SELECT SUM(\"clients\".\"id\") FROM \"clients\";";
				result = cmd.ExecuteScalar();
				if( result != null )
				{
					res = (int)result;
					Console.WriteLine("SUM=" + res);
				}

				cmd.CommandText = "SELECT COUNT(\"clients\".\"id\") FROM \"clients\";";
				result = cmd.ExecuteScalar();
				if( result != null )
				{
					res = (int)result;
					Console.WriteLine("COUNT=" + res);
				}

				cmd.CommandText = "SELECT AVG(\"clients\".\"id\") FROM \"clients\";";
				result = cmd.ExecuteScalar();
				if( result != null )
				{
					res = (int)result;
					Console.WriteLine("AVG=" + res);
				}

				cmd.CommandText = "CALL ABS(-33.5632);";
				result = cmd.ExecuteScalar();
				if( result != null )
				{
					Double abs = (Double)result;
					Console.WriteLine("ABS=" + abs);
				}

				cmd.CommandText = "CREATE ALIAS CALCRATE FOR \"ExternalFunction,ExternalFunction.Simple.calcrate\";";
				res = cmd.ExecuteNonQuery();

				cmd.CommandText = "CREATE ALIAS EXTTAN FOR \"ExternalFunction,ExternalFunction.Simple.tan\";";
				res = cmd.ExecuteNonQuery();

				cmd.CommandText = "CALL CALCRATE(100, 21);";
				Decimal rate = (Decimal)cmd.ExecuteScalar();
				Console.WriteLine("CALCRATE=" + rate);

				cmd.CommandText = "CALL EXTTAN(23.456);";
				Double tan = (Double)cmd.ExecuteScalar();
				Console.WriteLine("EXTTAN=" + tan);

				cmd.CommandText = "CALL SQRT(3);";
				Double sqrt = (Double)cmd.ExecuteScalar();
				Console.WriteLine("SQRT=" + sqrt);
				
				cmd.CommandText = "CALL SUBSTRING('0123456', 3, 2);";
				string subs = (String)cmd.ExecuteScalar();
				Console.WriteLine("SUBSTRING=" + subs);
				
				cmd.CommandText = "CALL ASCII('A');";
				int ascii = (int)cmd.ExecuteScalar();
				Console.WriteLine("ASCII=" + ascii);

				cmd.CommandText = "CALL USER();";
				string user = (string)cmd.ExecuteScalar();
				Console.WriteLine("USER=" + user);

				cmd.CommandText = "SELECT \"clients\".\"photo\" FROM \"clients\" WHERE \"clients\".\"id\" = 5;";
				byte[] b = (byte[])cmd.ExecuteScalar();

				cmd.CommandText = "SELECT \"clients\".\"id\", \"clients\".\"DoubleValue\", \"clients\".\"nombre\" FROM \"clients\" WHERE \"clients\".\"id\" = 5;";

				SharpHsqlDataAdapter adapter = new SharpHsqlDataAdapter(cmd);
				DataSet ds = new DataSet();
				res = adapter.Fill( ds );
				adapter = null;

				Console.WriteLine();	
				Console.WriteLine("DataSet.Fill: " + ds.Tables[0].Rows.Count);
				
				cmd.CommandText = "DECLARE @MyVar CHAR;SET @MyVar = 'Andy';";
				cmd.ExecuteNonQuery();

				Console.WriteLine();
				cmd.CommandText = "SELECT @MyVar;";
				string var = (string)cmd.ExecuteScalar();
				Console.WriteLine("@MyVar=" + var);

				Console.WriteLine();

				cmd.CommandText = "SELECT \"name\", \"author\", SUM(\"value\") FROM \"books\" WHERE \"author\" = @MyVar GROUP BY \"name\", \"author\";";
				reader = cmd.ExecuteReader();

				while( reader.Read() )
				{
					Console.WriteLine("name={0},\tauthor={1},\tvalue={2}", reader.GetString(0), reader.GetString(1), reader.GetDecimal(2) );
				}

				Console.WriteLine();
				reader.Close();
				
				cmd.CommandText = "INSERT INTO \"clients\" (\"DoubleValue\", \"nombre\", \"photo\", \"created\") VALUES (1.1, @MyVar, '" + base64photo + "', NOW() );";
				res = cmd.ExecuteNonQuery();
				cmd.CommandText = "DECLARE @MyId INT;SET @MyId = IDENTITY();";
				cmd.ExecuteNonQuery();
				cmd.CommandText = "SELECT @MyId;";
				int myid = (int)cmd.ExecuteScalar();
				Console.WriteLine("Inserted id={0}", myid );	

				Console.WriteLine();

				cmd.CommandText = "SET @MyId = SELECT MAX(\"clients\".\"id\") + 1 FROM \"clients\";";
				cmd.ExecuteNonQuery();
				cmd.CommandText = "SELECT @MyId;";
				myid = (int)cmd.ExecuteScalar();
				Console.WriteLine("Next id={0}", myid );

				Console.WriteLine();
				reader.Close();

				DateTime dt = DateTime.Now;

				cmd.CommandText = "INSERT INTO \"clients\" (\"DoubleValue\", \"nombre\", \"photo\", \"created\") VALUES (@DoubleValue, @nombre, @photo, @date );SET @Id = IDENTITY();";
				cmd.Parameters.Add( new SharpHsqlParameter("@Id", DbType.Int32, 0, ParameterDirection.Output, false, 0, 0, null, DataRowVersion.Current, null) );
				cmd.Parameters.Add( new SharpHsqlParameter("@DoubleValue", DbType.Double, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 1.1) );
				cmd.Parameters.Add( new SharpHsqlParameter("@nombre", DbType.String, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "Andrés") );
				cmd.Parameters.Add( new SharpHsqlParameter("@photo", DbType.Binary, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, photo) );
				cmd.Parameters.Add( new SharpHsqlParameter("@date", DbType.DateTime, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, dt) );
				res = cmd.ExecuteNonQuery();
				SharpHsqlParameter p = (SharpHsqlParameter)cmd.Parameters["@Id"];
				myid = (int)p.Value;
				Console.WriteLine("Inserted id={0}", myid );
				Console.WriteLine();

				cmd.Parameters.Clear();
				cmd.CommandText = "SELECT \"clients\".\"created\" FROM \"clients\" WHERE \"clients\".\"id\" = " + myid + ";";
				reader = cmd.ExecuteReader();

				while( reader.Read() )
				{
					Console.WriteLine( String.Format("Dates are equal: {0}.", dt.Equals( reader.GetDateTime(0) ) ) );
				}

				Console.WriteLine();
				reader.Close();

				cmd.CommandText = "SHOW DATABASES;";
				reader = cmd.ExecuteReader();

				for( int i=0;i<reader.FieldCount;i++)
				{
					Console.Write( reader.GetName(i)  );
					Console.Write( "\t"  );
				}
				Console.Write( Environment.NewLine  );

				while( reader.Read() )
				{
					for( int i=0;i<reader.FieldCount;i++)
					{
						Console.Write( reader.GetValue(i).ToString()  );
						Console.Write( "\t"  );
						Console.Write( Environment.NewLine  );
					}
				}

				Console.WriteLine();
				reader.Close();

				// Dataset Fill for SHOW DATABASES
				adapter = new SharpHsqlDataAdapter(cmd);
				ds = new DataSet();
				res = adapter.Fill( ds );
				adapter = null;

				Console.WriteLine();	
				Console.WriteLine("DATABASES: " + ds.Tables[0].Rows.Count);

				Console.WriteLine();

				cmd.CommandText = "SHOW TABLES;";
				reader = cmd.ExecuteReader();

				for( int i=0;i<reader.FieldCount;i++)
				{
					Console.Write( reader.GetName(i)  );
					Console.Write( "\t"  );
				}
				Console.Write( Environment.NewLine  );

				while( reader.Read() )
				{
					for( int i=0;i<reader.FieldCount;i++)
					{
						Console.Write( reader.GetValue(i).ToString()  );
						Console.Write( "\t"  );
						Console.Write( Environment.NewLine  );
					}
				}

				Console.WriteLine();
				reader.Close();

				// Dataset Fill for SHOW TABLES
				adapter = new SharpHsqlDataAdapter(cmd);
				ds = new DataSet();
				res = adapter.Fill( ds );
				adapter = null;

				Console.WriteLine();	
				Console.WriteLine("TABLES: " + ds.Tables[0].Rows.Count);
			
				Hashtable myData = new Hashtable();
				myData.Add( "1", "ONE" );
				myData.Add( "2", "TWO" );
				myData.Add( "3", "TREE" );
				myData.Add( "4", "FOUR" );
				myData.Add( "5", "FIVE" );

				cmd.Parameters.Clear();
				cmd.CommandText = "DELETE FROM \"data\" WHERE \"id\" = 1;";
				res = cmd.ExecuteNonQuery();
				
				cmd.Parameters.Clear();
				cmd.CommandText = "INSERT INTO \"data\" (\"id\", \"MyObject\") VALUES( @id, @MyObject);";
				cmd.Parameters.Add( new SharpHsqlParameter("@id", DbType.Int32, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 1) );
				cmd.Parameters.Add( new SharpHsqlParameter("@MyObject", DbType.Object, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, myData) );
				res = cmd.ExecuteNonQuery();
				cmd.Parameters.Clear();


				cmd.CommandText = "SELECT \"data\".\"id\", \"data\".\"MyObject\" FROM \"data\";";
				reader = cmd.ExecuteReader();
				Console.Write( Environment.NewLine  );

				int myId = 0;
				Hashtable readData = null;
				while( reader.Read() )
				{
					myId = reader.GetInt32(0);
					readData = (Hashtable)reader.GetValue(1);
				}

				foreach( DictionaryEntry entry in readData )
				{
					Console.WriteLine( String.Format("Key: {0}, Value: {1}", entry.Key.ToString(), entry.Value.ToString() ) );
				}


				Console.WriteLine();
				reader.Close();			
	
				cmd.CommandText = "SHOW ALIAS;";
				reader = cmd.ExecuteReader();

				Console.Write( Environment.NewLine  );

				while( reader.Read() )
				{
					Console.WriteLine("ALIAS {0} FOR {1}", reader.GetString(0), reader.GetString(1)  );
				}

				Console.WriteLine();
				reader.Close();

				cmd.CommandText = "SHOW PARAMETERS CALCRATE;";
				reader = cmd.ExecuteReader();

				Console.Write( Environment.NewLine  );

				while( reader.Read() )
				{
					Console.WriteLine("ALIAS: {0}, PARAM: {1},\t TYPE {2},\t POSITION: {3}", reader.GetString(0), reader.GetString(1), reader.GetString(2),  reader.GetInt32(3)  );
				}

				Console.WriteLine();
				reader.Close();

				cmd.CommandText = "SHOW COLUMNS \"clients\";";
				reader = cmd.ExecuteReader();

				Console.Write( Environment.NewLine  );

				while( reader.Read() )
				{
					Console.WriteLine("TABLE: {0}, COLUMN: {1},\n\t NATIVE TYPE: {2},\t DB TYPE: {3},\n\t POSITION: {4},\t NULLABLE: {5},\t IDENTITY: {6}", reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetValue(3),  reader.GetInt32(4), reader.GetBoolean(5), reader.GetBoolean(6) );
				}

				Console.WriteLine();
				reader.Close();
			}
			catch( SharpHsqlException  ex )
			{
				Console.WriteLine(ex.Message);
			}
			catch( Exception e )
			{
				Console.WriteLine(e.Message);
			}
			finally
			{
				conn.Close();
				conn = null;
			}

			Console.WriteLine();
			Console.WriteLine("Press [ENTER] to exit.");
			Console.ReadLine();

		}
	}
}
