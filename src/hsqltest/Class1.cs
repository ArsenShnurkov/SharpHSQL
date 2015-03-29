using System;
using SharpHsql;

namespace Sample
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
			// Create an in memory database by creating with the name "."
			// This has no logging or other disk access
			//Database db = new Database(".");
			Database db = new Database("mydb");
			// The "sa" user is created by default with no password, so we can connect
			// using this user
			Channel myChannel = db.Connect("sa","");
			//All queries return a Result object
			Result rs;
			// We need a string to enter our queries
			string query = "";

			// While the query is not the quit command
			while (!query.ToLower().Equals("quit"))
			{
				// Write a little prompt out to the console
				Console.Write("SQL> ");
				// Read a line of text
				query = Console.ReadLine();
				// Is it our quit command?
				if (!query.ToLower().Equals("quit"))
				{
					// No, execute it using our Channel object
					rs = db.Execute(query,myChannel);
					// If there was an error
					if (rs.Error != null)
					{
						// Print the error message out to the console
						Console.WriteLine(rs.Error);
					}
					else
					{
						// Write out some statistics
						Console.Write(rs.Size + " rows returned, " + rs.UpdateCount + " rows affected.\n\n");
						// If we had records returned
						if (rs.Root != null)
						{
							// Get the first one
							Record r = rs.Root;
							// Get the column count from the Result Object
							int column_count = rs.ColumnCount;
							for (int x = 0; x < column_count;x++)
							{
								// Print out the column names
								Console.Write(rs.Label[x]);
								Console.Write("\t");
							}
							Console.Write("\n");
							while (r != null)
							{
								for (int x = 0; x < column_count;x++)
								{
									// Write out the data values
									Console.Write(r.Data[x]);
									Console.Write("\t");
								}
								Console.Write("\n");
								// Get the next Record object
								r = r.Next;
							}
							Console.Write("\n");
						}
					}
				}
				/*else
				{
					//System.Diagnostics.Debugger.Launch();
					rs = db.execute("shutdown",myChannel);
					// If there was an error
					if (rs.Error != null)
					{
						// Print the error message out to the console
						Console.WriteLine(rs.Error);
						Console.WriteLine("Press [ENTER] to exit.");
						Console.ReadLine();
					}

				}*/
			}		
		}
	}
}
