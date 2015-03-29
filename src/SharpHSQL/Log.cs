#region Usings
using System;
using System.Xml;
using System.Collections;
using System.IO;
using System.Threading;
using System.Reflection;
#endregion

#region License
/*
 * Log.cs
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
	/// This class is responsible for most file handling.
	/// </summary>
	/// <remarks>
	/// A HSQL database consists of a .properties file, a
	/// .script file (contains a SQL script), a
	/// .data file (contains data of cached tables) and a
	/// .backup file (contains the compressed .data file).
	/// 
	/// This is an example of the .properties file. The version and the
	/// modified properties are automatically created by the database and
	/// should not be changed manually.
	/// 
	/// The other properties are optional, this means they are not
	/// created automatically by the database, but they are interpreted
	/// if they exist in the .script file. They have to be created
	/// manually if required. If they don't exist the default is used.
	/// 
	/// This are the defaults for the database 'mytest':
	/// 
	/// <code>
	/// &lt;?xml version="1.0"?&gt;
	/// &lt;!--SharpHSQL Configuration--&gt;
	/// &lt;?Instruction Configuration Record?&gt;
	/// &lt;Properties 
	///		LogFile="mytest.log" 
	///		DataFile="mytest.data" 
	///		Backup="mytest.backup" 
	///		Version="1.0.0.0" 
	///		ReadOnly="false" 
	///		Modified="no" /&gt;
	/// </code>
	/// 
	/// </remarks>
	sealed class Log
	{
		private static int COPY_BLOCK_SIZE = 1 << 16;  // block size for copying data
		private string		       sName;
		private Database	       dDatabase;
		private Channel	           cSystem;
		private StreamWriter	   wScript;
		private FileStream		   _file;	
		private string	           sFileProperties;
		private string	           sFileScript;
		private string	           sFileCache;
		private string	           sFileBackup;
		private string			   sModified;
		private string			   sVersion;
		private bool	           bRestoring;
		private bool	           bReadOnly;
		private int		           iLogSize =	200;  // default: .script file is max 200 MB big
		private int		           iLogCount;
		private bool               bWriteDelay;
		private int		           mLastId;
		private object			   SyncLock = new object();
		internal Cache		       cCache;
		#if !POCKETPC
		private bool			   _running;
		private Thread	           tRunner;
		private bool               bNeedFlush;
		#endif

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="system"></param>
		/// <param name="name"></param>
		public Log(Database db, Channel system, string name) 
		{
			dDatabase = db;
			cSystem = system;
			sName = name;
			sFileProperties = sName + ".cfg";
			sFileScript = sName + ".log";
			sFileCache = sName + ".data";
			sFileBackup = sName + ".backup";
		}

		#if !POCKETPC
		/// <summary>
		/// Flush the transaction log.
		/// </summary>
		public void Run() 
		{
			_running = true;

			while (_running) 
			{
				try 
				{
					Thread.Sleep(1000);

					if (bNeedFlush && wScript != null) 
					{
						wScript.Flush();

						bNeedFlush = false;
					}

					// todo: try to do Cache.cleanUp() here, too
				} 
				catch( ThreadAbortException )
				{
					_running = false;
				}
				catch( ThreadInterruptedException )
				{
					_running = false;
				}
				catch (Exception e) 
				{
					// ignore exceptions
					LogHelper.Publish( "Unexpected error on Run.", e );
				}
			}
		}
		#endif

		/// <summary>
		/// Sets the write delay.
		/// </summary>
		/// <param name="delay"></param>
		public void SetWriteDelay(bool delay) 
		{
			bWriteDelay = delay;
		}

		/// <summary>
		/// Opens the database files.
		/// </summary>
		/// <returns>True if operation is sucessful.</returns>
		public bool Open() 
		{
			lock( SyncLock )
			{
				bool newdata = false;

				if (Trace.TraceEnabled) 
					Trace.Write();

				if (!(new FileInfo(sFileProperties)).Exists) 
				{
					Create();
					// this is a new database
					newdata = true;
				}

				// todo: some parts are not necessary for read-only access
				LoadProperties();

				if (bReadOnly == true)
				{
					dDatabase.SetReadOnly();

					cCache = new Cache(sFileCache);

					cCache.Open(true);
					RunScript();

					return false;
				}

				bool needbackup = false;

				if (sModified.Equals("yes-new-files")) 
				{
					RenameNewToCurrent(sFileScript);
					RenameNewToCurrent(sFileBackup);
				} 
				else if (sModified.Equals("yes")) 
				{
					if (IsAlreadyOpen()) 
					{
						throw Trace.Error(Trace.DATABASE_ALREADY_IN_USE);
					}

					// recovering after a crash (or forgot to close correctly)
					RestoreBackup();

					needbackup = true;
				}

				sModified = "yes";
				SaveProperties();

				cCache = new Cache(sFileCache);

				cCache.Open(false);
				RunScript();

				if (needbackup) 
				{
					Close(false);
					sModified = "yes";
					SaveProperties();
					cCache.Open(false);
				}

				OpenScript();

				// this is a existing database
				return newdata;
			}
		}

		/// <summary>
		/// Stops the log writer thread.
		/// </summary>
		public void Stop() 
		{
			#if !POCKETPC
			if( tRunner == null )
				return;

			_running = false;

			try
			{
				tRunner.Abort();
			}
			catch{}

			tRunner = null;
			#endif
		}

		/// <summary>
		/// Close the transaction log.
		/// </summary>
		/// <param name="compact"></param>
		public void Close(bool compact) 
		{	
			lock( SyncLock )
			{
				if (Trace.TraceEnabled) 
					Trace.Write();

				if (bReadOnly) 
					return;

				// no more scripting
				CloseScript();

				// create '.script.new' (for this the cache may be still required)
				WriteScript(compact);

				// flush the cache (important: after writing the script)
				cCache.Flush();

				// create '.backup.new' using the '.data'
				Backup();

				// we have the new files
				sModified = "yes-new-files";
				SaveProperties();

				// old files can be removed and new files renamed
				RenameNewToCurrent(sFileScript);
				RenameNewToCurrent(sFileBackup);

				// now its done completely
				sModified = "no";
				SaveProperties();
				CloseProperties();

				if (compact) 
				{
					// stop the runner thread of this process (just for security)
					Stop();

					// delete the .data so then a new file is created
					(new FileInfo(sFileCache)).Delete();
					(new FileInfo(sFileBackup)).Delete();

					// all files are closed now; simply open & close this database
					Database db = new Database(sName);

					db.Log.Close(false);
				}
			}
		}

		/// <summary>
		/// Performs a checkpoint operation on the database.
		/// </summary>
		public void Checkpoint() 
		{
			lock( SyncLock )
			{
				Close(false);
				sModified = "yes";
				SaveProperties();
				cCache.Open(false);
				OpenScript();
			}
		}

		/// <summary>
		/// Sets the maximum log size.
		/// </summary>
		/// <param name="mb"></param>
		public void SetLogSize(int mb) 
		{
			iLogSize = mb;
		}

		/// <summary>
		/// Writes a SQL statement to the transaction log.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="s"></param>
		public void Write(Channel channel, string s) 
		{
			if (bRestoring || s == null || s.Equals("")) 
				return;

			if (!bReadOnly) 
			{
				int id = 0;

				if (channel != null) 
				{
					id = channel.Id;
				}

				if (id != mLastId) 
				{
					s = "/*C" + id + "*/" + s;
					mLastId = id;
				}

				try 
				{
					lock( SyncLock )
					{
						writeLine(wScript, s);

						#if !POCKETPC
						if (bWriteDelay) 
							bNeedFlush = true;
						else 
						#endif	
							wScript.Flush();
					}
				} 
				catch (IOException e) 
				{
					Trace.Error(Trace.FILE_IO_ERROR, sFileScript);
					LogHelper.Publish( "Unexpected error on Write.", e );
				}
				catch( Exception e )
				{
					LogHelper.Publish( "Unexpected error on Write.", e );	
				}

				lock( SyncLock )
				{
					if (iLogSize > 0 && iLogCount++ > 100) 
					{
						iLogCount = 0;

						if ((new FileInfo(sFileScript)).Length > iLogSize * 1024 * 1024) 
						{
							Checkpoint();
						}
					}
				}
			}
		}

		/// <summary>
		/// Shutdown the transaction log.
		/// </summary>
		public void Shutdown() 
		{
			Stop();

			cCache.Shutdown();
			CloseScript();
			CloseProperties();
		}

		/// <summary>
		/// Script the database objects to a file.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="file"></param>
		/// <param name="full"></param>
		/// <param name="channel"></param>
		public static void ScriptToFile(Database db, string file, bool full, Channel channel) 
		{
			if ((new FileInfo(file)).Exists) 
			{
				// there must be no such file; overwriting not allowed for security
				throw Trace.Error(Trace.FILE_IO_ERROR, file);
			}

			try 
			{
				DateTime   time = DateTime.Now;

				// only ddl commands; needs not so much memory
				Result r;

				if (full) 
				{
					// no drop, no insert, and no positions for cached tables
					r = db.GetScript(false, false, false, channel);
				} 
				else 
				{
					// no drop, no insert, but positions for cached tables
					r = db.GetScript(false, false, true, channel);
				}

				Record     n = r.Root;
				StreamWriter w = new StreamWriter(file);	

				while (n != null) 
				{
					writeLine(w, (string) n.Data[0]);

					n = n.Next;
				}

				// inserts are done separetely to save memory
				ArrayList tables = db.Tables;

				for (int i = 0; i < tables.Count; i++) 
				{
					Table t = (Table) tables[i];

					// cached tables have the index roots set in the ddl script
					if (full ||!t.IsCached) 
					{
						Index primary = t.PrimaryIndex;
						Node  x = primary.First();

						while (x != null) 
						{
							writeLine(w, t.GetInsertStatement(x.GetData()));

							x = primary.Next(x);
						}
					}
				}

				w.Close();

				TimeSpan execution = DateTime.Now.Subtract(time);

				if (Trace.TraceEnabled) 
					Trace.Write((Int64)execution.TotalMilliseconds);
			} 
			catch (IOException e) 
			{
				Trace.Error(Trace.FILE_IO_ERROR, file + " " + e);
			}
		}

		/// <summary>
		/// Renames a .new files to be the current database files.
		/// </summary>
		/// <param name="file"></param>
		private void RenameNewToCurrent(string file) 
		{
			// even if it crashes here, recovering is no problem
			if ((new FileInfo(file + ".new")).Exists) 
			{
				// if we have a new file
				// delete the old (maybe already deleted)
				(new FileInfo(file)).Delete();

				// rename the new to the current
				new FileInfo(file + ".new").MoveTo(file);
			}
		}

		/// <summary>
		/// Close the database properties file.
		/// </summary>
		private void CloseProperties() 
		{
			try 
			{
				if (Trace.TraceEnabled) 
				{
					Trace.Write();
				}
			} 
			catch (Exception e) 
			{
				throw Trace.Error(Trace.FILE_IO_ERROR, sFileProperties + " " + e);
			}
		}

		/// <summary>
		/// Creates a new properties files.
		/// </summary>
		private void Create() 
		{
			if (Trace.TraceEnabled) 
				Trace.Write(sName);

			XmlTextWriter writer = new XmlTextWriter(sFileProperties, null);
		    writer.Formatting = Formatting.Indented;
			writer.Indentation=4;
     		writer.WriteStartDocument();
			writer.WriteComment("SharpHSQL Configuration");
			writer.WriteProcessingInstruction("Instruction","Configuration Record");
			writer.WriteStartElement("Properties","");
			writer.WriteStartAttribute("LogFile","");
			writer.WriteString(sFileScript);
			writer.WriteEndAttribute();
			writer.WriteStartAttribute("DataFile","");
			writer.WriteString(sFileCache);
			writer.WriteEndAttribute();
			writer.WriteStartAttribute("Backup","");
			writer.WriteString(sFileBackup);
			writer.WriteEndAttribute();
			writer.WriteStartAttribute("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString() );
			writer.WriteString("1.0");
			writer.WriteEndAttribute();
			writer.WriteStartAttribute("ReadOnly","");
			writer.WriteString("false");
			writer.WriteEndAttribute();
			writer.WriteStartAttribute("Modified","");
			writer.WriteString("no");
			writer.WriteEndElement();
			writer.WriteEndDocument();
			writer.Flush();
			writer.Close();

			SaveProperties();
		}


		/// <summary>
		/// Checks if the database is already open.
		/// </summary>
		/// <returns>True if the databse file is open.</returns>
		private bool IsAlreadyOpen() 
		{
			if (Trace.TraceEnabled) 
				Trace.Write();

			FileStream fs = null;

			try
			{
				// try to open the log file exclusively for writing
				fs = new FileStream(sFileScript, FileMode.Append, FileAccess.Write, FileShare.None);
			}
			catch( Exception )
			{
				return true;
			}
			finally 
			{
				if( fs != null )
				{
					fs.Close();
					fs = null;
				}
			}

			return false;
		}

		/// <summary>
		/// Load properties from file.
		/// </summary>
		private void LoadProperties() 
		{
			try 
			{
				XmlTextReader reader = new XmlTextReader(sFileProperties);
				//Read the tokens from the reader
				while ( reader.Read() )
				{
					if (XmlNodeType.Element == reader.NodeType)
					{
						sFileScript = reader.GetAttribute("LogFile");
						sFileCache = reader.GetAttribute("DataFile");
						sFileBackup = reader.GetAttribute("Backup");
						sModified = reader.GetAttribute("Modified");
		 		        sVersion = reader.GetAttribute("Version");
		 		        bReadOnly = reader.GetAttribute("ReadOnly").ToLower().Equals("true");
					}
				}
				reader.Close();
 			}
			catch (Exception e)
			{
				Console.WriteLine("Property File Exeception:", e.ToString());
			}

			if (Trace.TraceEnabled) 
				Trace.Write();
		}

		/// <summary>
		/// Save database properties.
		/// </summary>
		private void SaveProperties() 
		{
			lock( SyncLock )
			{
				//WATYF: 
				//Added check for ".new" file, delete if exists. 
				//Changed writer creation to include ".new" suffix on sFileProperties 
				FileInfo fi = new FileInfo(sFileProperties + ".new");
				if ( fi.Exists ){ fi.Delete();} 
				
				XmlTextWriter writer = new XmlTextWriter(sFileProperties + ".new", null); 
				writer.Formatting = Formatting.Indented;
				writer.Indentation=4;
				writer.WriteStartDocument();
				writer.WriteComment("SharpHSQL Configuration");
				writer.WriteProcessingInstruction("Instruction","Configuration Record");
				writer.WriteStartElement("Properties","");
				writer.WriteStartAttribute("LogFile","");
				writer.WriteString(sFileScript);
				writer.WriteEndAttribute();
				writer.WriteStartAttribute("DataFile","");
				writer.WriteString(sFileCache);
				writer.WriteEndAttribute();
				writer.WriteStartAttribute("Backup","");
				writer.WriteString(sFileBackup);
				writer.WriteEndAttribute();
				writer.WriteStartAttribute("Version","");
				writer.WriteString(sVersion);
				writer.WriteEndAttribute();
				writer.WriteStartAttribute("ReadOnly","");
				if (bReadOnly == true)
				{
					writer.WriteString("true");
				}
				else
				{
					writer.WriteString("false");
				}
				writer.WriteEndAttribute();
				writer.WriteStartAttribute("Modified","");
				writer.WriteString(sModified);
				writer.WriteEndElement();
				writer.WriteEndDocument();
				writer.Flush();
				writer.Close();

				//WATYF: Added RenameNewToCurrent 
				RenameNewToCurrent(sFileProperties); 

				CloseProperties();

				if (Trace.TraceEnabled) 
					Trace.Write();
			}
		}

		/// <summary>
		/// Performs a backup of the current database.
		/// </summary>
		private void Backup() 
		{
			if (Trace.TraceEnabled) 
				Trace.Write();

			// if there is no cache file then backup is not necessary
			if (!(new FileInfo(sFileCache)).Exists) 
				return;

			try 
			{
				DateTime time = DateTime.Now;

				// create a '.new' file; rename later
				BinaryWriter f = new BinaryWriter(new FileStream(sFileBackup + ".new",FileMode.OpenOrCreate,FileAccess.Write));
				byte[]		 b = new byte[COPY_BLOCK_SIZE];
				BinaryReader fin = new BinaryReader(new FileStream(sFileCache,FileMode.Open,FileAccess.Read));

				while (true) 
				{
					int l = fin.Read(b, 0, COPY_BLOCK_SIZE);

					if (l == 0) 
					{
						break;
					}

					f.Write(b, 0, l);
				}

				f.Close();
				fin.Close();

				TimeSpan execution = DateTime.Now.Subtract(time);

				if (Trace.TraceEnabled) 
					Trace.Write((Int64)execution.TotalMilliseconds);
			}
			catch (Exception e) 
			{
				LogHelper.Publish( "Unexpected error on Backup.", e );
				throw Trace.Error(Trace.FILE_IO_ERROR, sFileBackup);
			}
		}

		/// <summary>
		/// Restores a previous backup.
		/// </summary>
		private void RestoreBackup() 
		{
			if (Trace.TraceEnabled) 
				Trace.Write("Not closed last time!");

			if (!(new FileInfo(sFileBackup)).Exists) 
			{
				// the backup don't exists because it was never made or is empty
				// the cache file must be deleted in this case
				(new FileInfo(sFileCache)).Delete();

				return;
			}

			try 
			{
				DateTime		time = DateTime.Now;
				BinaryReader f = new BinaryReader(new FileStream(sFileBackup,FileMode.Open,FileAccess.Read));
				BinaryWriter cache = new BinaryWriter(new FileStream(sFileCache,FileMode.OpenOrCreate,FileAccess.Write));
				byte[]		b = new byte[COPY_BLOCK_SIZE];

				while (true) 
				{
					int l = f.Read(b, 0, COPY_BLOCK_SIZE);

					if (l == 0) 
					{
						break;
					}

					cache.Write(b, 0, l);
				}

				cache.Close();
				f.Close();

				TimeSpan execution = DateTime.Now.Subtract(time);

				if (Trace.TraceEnabled) 
				{
					Trace.Write((Int64)execution.TotalMilliseconds);
				}
			} 
			catch (Exception e) 
			{
				LogHelper.Publish( "Unexpected error on RestoreBackup.", e );
				throw Trace.Error(Trace.FILE_IO_ERROR, sFileBackup);
			}
		}

		/// <summary>
		/// Opens the transaction log for writing.
		/// </summary>
		private void OpenScript() 
		{
			if (Trace.TraceEnabled) 
				Trace.Write();

			try 
			{
				// opens the log file exclusively for writing
				_file = new FileStream(sFileScript, FileMode.Append, FileAccess.Write, FileShare.None);

				// todo: use a compressed stream
				wScript = new StreamWriter(_file, System.Text.Encoding.UTF8);

				#if !POCKETPC
				tRunner = new Thread( new ThreadStart( Run ) );
				tRunner.IsBackground = true;
				tRunner.Start();
				#endif
				
			} 
			catch (Exception e) 
			{
				LogHelper.Publish( "Unexpected error on OpenScript.", e );
				Trace.Error(Trace.FILE_IO_ERROR, sFileScript);
			}
		}

		/// <summary>
		/// Close the transaction log file.
		/// </summary>
		private void CloseScript() 
		{
			if (Trace.TraceEnabled) 
				Trace.Write();

			try 
			{
				if (wScript != null) 
				{
					Stop();

					wScript.Close();
					wScript = null;

					_file = null;
				}
			} 
			catch (Exception e) 
			{
				LogHelper.Publish( "Unexpected error on CloseScript.", e );
				Trace.Error(Trace.FILE_IO_ERROR, sFileScript);
			}
		}

		/// <summary>
		/// Opens the transaction log and runs it.
		/// </summary>
		private void RunScript() 
		{
			if (Trace.TraceEnabled) 
				Trace.Write();

			if (!(new FileInfo(sFileScript)).Exists) 
				return;

			bRestoring = true;

			dDatabase.IsReferentialIntegrity = false;

			ArrayList channel = new ArrayList();

			channel.Add(cSystem);

			Channel current = cSystem;
			int     size = 1;

			try 
			{
				DateTime	     time = DateTime.Now;
				StreamReader r = new StreamReader(sFileScript);

				while (true) 
				{
					string s = r.ReadLine();

					if (s == null) 
					{
						break;
					}

					if (s.StartsWith("/*C")) 
					{
						int id = Int32.Parse(s.Substring(3,(s.IndexOf('*', 4)-3)));

						if( id > (channel.Count-1) )
						{
							current = new Channel(cSystem, id);
							channel.Add(current);
							dDatabase.RegisterChannel(current);
						}
						else
						{
							current = (Channel)channel[id];
						}

						s = s.Substring(s.IndexOf('/', 1) + 1);
					}

					if (!s.Equals("")) 
					{
						dDatabase.Execute(s, current);
					}

					if (s.Equals("DISCONNECT")) 
					{
						int id = current.Id;

						current = new Channel(cSystem, id);

						channel.RemoveAt(id);
						channel.Insert(id, current);
					}
				}

				r.Close();

				for (int i = 0; i < size; i++) 
				{
					current = (Channel) channel[i];

					if (current != null) 
					{
						current.Rollback();
					}
				}

				TimeSpan execution = DateTime.Now.Subtract(time);

				if (Trace.TraceEnabled) 
					Trace.Write((Int64)execution.TotalMilliseconds);
			} 
			catch (IOException e) 
			{
				throw Trace.Error(Trace.FILE_IO_ERROR, sFileScript + " " + e);
			}

			dDatabase.IsReferentialIntegrity = true;

			bRestoring = false;
		}

		/// <summary>
		/// Script database to file.
		/// </summary>
		/// <param name="full"></param>
		private void WriteScript(bool full) 
		{
			if (Trace.TraceEnabled) 
				Trace.Write();

			// create script in '.new' file
			(new FileInfo(sFileScript + ".new")).Delete();

			// script; but only positions of cached tables, not full
			ScriptToFile(dDatabase, sFileScript + ".new", full, cSystem);
		}

		/// <summary>
		/// Writes a line to the log.
		/// </summary>
		/// <param name="w"></param>
		/// <param name="s"></param>
		private static void writeLine(StreamWriter w, string s) 
		{
			w.WriteLine(s);
		}

		/// <summary>
		/// Reads a line from the log.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		private static string ReadLine(TextReader reader) 
		{
			string s = reader.ReadLine();

			return StringConverter.AsciiToUnicode(s);
		}
	}
}
