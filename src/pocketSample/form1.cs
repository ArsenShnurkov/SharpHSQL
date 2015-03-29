using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.Data;
using System.Data.Hsql;

namespace pocketSample
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox txtResult;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MainMenu mainMenu1;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
		}
		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.txtResult = new System.Windows.Forms.TextBox();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.Add(this.menuItem1);
			// 
			// menuItem1
			// 
			this.menuItem1.MenuItems.Add(this.menuItem2);
			this.menuItem1.MenuItems.Add(this.menuItem3);
			this.menuItem1.Text = "Test";
			// 
			// txtResult
			// 
			this.txtResult.Multiline = true;
			this.txtResult.ReadOnly = true;
			this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtResult.Size = new System.Drawing.Size(240, 272);
			this.txtResult.Text = "";
			this.txtResult.WordWrap = false;
			// 
			// menuItem2
			// 
			this.menuItem2.Text = "Start";
			this.menuItem2.Click += new System.EventHandler(this.menuItemStart_Click);
			// 
			// menuItem3
			// 
			this.menuItem3.Text = "Exit";
			this.menuItem3.Click += new System.EventHandler(this.menuItemExit_Click);
			// 
			// Form1
			// 
			this.Controls.Add(this.txtResult);
			this.Menu = this.mainMenu1;
			this.Text = "PocketSharpHSQL";

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>

		static void Main() 
		{
			Application.Run(new Form1());
		}

		private void DoTest()
		{
			try
			{
				txtResult.Text = "Test started...";

				//System.Diagnostics.Debugger.Launch();

				SharpHsqlConnection conn = new SharpHsqlConnection("Initial Catalog=\\program files\\pocketSample\\mytest;User Id=sa;Pwd=;");
				conn.Open();

				SharpHsqlTransaction tran = conn.BeginTransaction();

				SharpHsqlCommand cmd = new SharpHsqlCommand("", conn);

				int res;

				if( MessageBox.Show( "Drop & create 'clients' table?", "Drop Table", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2 ) == DialogResult.Yes )
				{

					txtResult.Text += "\r\nDropping clients table...";

					cmd.CommandText = "DROP TABLE IF EXIST \"clients\";CREATE CACHED TABLE \"clients\" (\"id\" int NOT NULL IDENTITY PRIMARY KEY, \"DoubleValue\" double, \"nombre\" char);";
					res = cmd.ExecuteNonQuery();

					for(int i=0;i<10;i++)
					{
						cmd.CommandText = "INSERT INTO \"clients\" (\"DoubleValue\", \"nombre\") VALUES (1.1, 'NOMBRE" + i.ToString() + "');";
						res = cmd.ExecuteNonQuery();
						cmd.CommandText = "CALL IDENTITY();";
						int id = (int)cmd.ExecuteScalar();
						txtResult.Text += String.Format("\r\nInserted id={0}", id );		
					}
				}

				if( MessageBox.Show( "Bulk INSERT 'clients' table?", "Insert Table", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2 ) == DialogResult.Yes )
				{

					txtResult.Text += "\r\nBulk Insert clients table...";

					for(int i=0;i<1000;i++)
					{
						cmd.CommandText = "INSERT INTO \"clients\" (\"DoubleValue\", \"nombre\") VALUES (1.1, 'NOMBRE" + i.ToString() + "');";
						res = cmd.ExecuteNonQuery();
					}

					txtResult.Text += "\r\nInserted 1000 rows.";
				}

				txtResult.Text += "\r\nSelecting rows...";

				cmd.CommandText = "SELECT \"clients\".\"id\", \"clients\".\"DoubleValue\", \"clients\".\"nombre\" FROM \"clients\"";
				IDataReader reader = cmd.ExecuteReader();

				string row = "";
				int count = 0;

				while( reader.Read() )
				{
					count++;
					row = String.Format("id={0}, doubleValue={1}, nombre={2}", reader.GetInt32(0), reader.GetDouble(1), reader.GetString(2) );
				}

				txtResult.Text += String.Format("\r\nSelected {0} rows.", count);
				txtResult.Text += String.Format("\r\nLast row: \r\n{0}", row);

				reader.Close();

				tran.Commit();
				//tran.Rollback();

				cmd.CommandText = "DELETE FROM \"clients\" WHERE \"clients\".\"id\" = 5;";
				res = cmd.ExecuteNonQuery();

				cmd.CommandText = "SELECT MAX(\"clients\".\"id\") FROM \"clients\";";
				res = (int)cmd.ExecuteScalar();
				txtResult.Text += "\r\nMAX=" + res;

				cmd.CommandText = "SELECT SUM(\"clients\".\"id\") FROM \"clients\";";
				res = (int)cmd.ExecuteScalar();
				txtResult.Text += "\r\nSUM=" + res;

				cmd.CommandText = "SELECT COUNT(\"clients\".\"id\") FROM \"clients\";";
				res = (int)cmd.ExecuteScalar();
				txtResult.Text += "\r\nCOUNT=" + res;

				cmd.CommandText = "SELECT AVG(\"clients\".\"id\") FROM \"clients\";";
				res = (int)cmd.ExecuteScalar();
				txtResult.Text += "\r\nAVG=" + res;

				cmd.CommandText = "CALL ABS(-33.5632);";
				Double abs = (Double)cmd.ExecuteScalar();
				txtResult.Text += "\r\nABS=" + abs;

				cmd.CommandText = "CREATE ALIAS CALCRATE FOR \"ExternalFunction,ExternalFunction.Simple.calcrate\";";
				res = cmd.ExecuteNonQuery();

				cmd.CommandText = "CREATE ALIAS EXTTAN FOR \"ExternalFunction,ExternalFunction.Simple.tan\";";
				res = cmd.ExecuteNonQuery();

				cmd.CommandText = "CALL CALCRATE(100, 21);";
				Decimal rate = (Decimal)cmd.ExecuteScalar();
				txtResult.Text += "\r\nCALCRATE=" + rate;

				cmd.CommandText = "CALL EXTTAN(23.456);";
				Double tan = (Double)cmd.ExecuteScalar();
				txtResult.Text += "\r\nEXTTAN=" + tan;

				cmd.CommandText = "CALL SQRT(3);";
				Double sqrt = (Double)cmd.ExecuteScalar();
				txtResult.Text += "\r\nSQRT=" + sqrt;
				
				cmd.CommandText = "CALL SUBSTRING('0123456', 3, 2);";
				string subs = (String)cmd.ExecuteScalar();
				txtResult.Text += "\r\nSUBSTRING=" + subs;
				
				cmd.CommandText = "CALL ASCII('A');";
				int ascii = (int)cmd.ExecuteScalar();
				txtResult.Text += "\r\nASCII=" + ascii;

				cmd.CommandText = "CALL USER();";
				string user = (string)cmd.ExecuteScalar();
				txtResult.Text += "\r\nUSER=" + user;

				cmd.CommandText = "SELECT \"clients\".\"id\", \"clients\".\"DoubleValue\", \"clients\".\"nombre\" FROM \"clients\" WHERE \"clients\".\"id\" = 5;";

				SharpHsqlDataAdapter adapter = new SharpHsqlDataAdapter(cmd);
				DataSet ds = new DataSet();
				res = adapter.Fill( ds );
				adapter = null;

				txtResult.Text += "\r\nDataSet.Fill: " + ds.Tables[0].Rows.Count;

				conn.Close();
				conn = null;
			}
			catch( SharpHsqlException  ex )
			{
				txtResult.Text += "\r\nERROR: " + ex.Message;
			}
			catch( Exception ex )
			{
				txtResult.Text += "\r\nERROR: " + ex.Message;
			}
		}

		private void menuItemStart_Click(object sender, System.EventArgs e)
		{
			DoTest();
		}

		private void menuItemExit_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
	}
}
