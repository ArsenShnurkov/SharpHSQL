#region Usings
using System;
using System.Collections;
using System.Reflection;
using System.Threading;
#if !POCKETPC
using log4net;
#endif
#endregion

namespace SharpHsql
{
	/// <summary>
	/// Controller class for single point database access.
	/// </summary>
	public sealed class DatabaseController 
	{
		private static object SyncRoot = new object();
		private static Hashtable _dbs = Hashtable.Synchronized( new Hashtable() );
		private static Timer _checkPoint;

		private DatabaseController(){}

		static DatabaseController()
		{
			try
			{
				// try to shutdown gracefully when process exits
				#if !POCKETPC
				AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
				#endif

				// do a checkpoint every minute
				_checkPoint = new Timer( new TimerCallback(DoCheckPoint), null, 60000, 60000);
			}
			catch( Exception ex )
			{
				LogHelper.Publish( "Unexpected exception on [DatabaseController] constructor.", ex );
			}
		}

		/// <summary>
		/// Gets a <see cref="Database"/> instance by name.
		/// </summary>
		/// <remarks>
		/// This class mantains a cache of already created database
		/// objects to prevent opening the same database more than once, 
		/// because database files are open exclusively.
		/// </remarks>
		/// <param name="name">Database full path.</param>
		/// <returns>A reference of a new or existing database object.</returns>
		public static Database GetDatabase(string name) 
		{
			lock( SyncRoot )
			{
				try
				{
					Database db = null;
					if( _dbs.ContainsKey( name ) )
					{
						db = (Database)_dbs[name];
					}
					else
					{
						db = new Database(name);
						_dbs.Add(name, db);
					}
					return db;
				}
				catch( Exception ex )
				{
						LogHelper.Publish( String.Format("Unexpected exception on [GetDatabase] method for database {0}.", name ), ex );

					throw;
				}
			}
		}

		/// <summary>
		/// Shutdown an already open database.
		/// </summary>
		/// <param name="name">The name of the database.</param>
		public static void Shutdown( string name )
		{
			lock( SyncRoot )
			{
				try
				{
					Database db = _dbs[name] as Database;
					if( db != null )
					{
						lock( db )
						{
							db.Execute("SHUTDOWN", db.SysChannel );
						}
						_dbs.Remove( name );
					}
				}
				catch( Exception ex )
				{
					LogHelper.Publish( String.Format("Unexpected exception on [Shutdown] method for database {0}.", name ), ex );

					throw;
				}
			}			
		}

		/// <summary>
		/// Shut down all active database instances.
		/// </summary>
		public static void ShutdownAll()
		{
			lock( SyncRoot )
			{
				try
				{
					object[] keys = new object[_dbs.Keys.Count];
					_dbs.Keys.CopyTo( keys, 0 );
					foreach( string name in keys )
					{
						Database db = _dbs[name] as Database;
						if( db != null )
						{
							lock( db )
							{
								db.Execute("SHUTDOWN", db.SysChannel);
							}
							_dbs.Remove( name );
						}
					}
				}
				catch( Exception ex )
				{
					LogHelper.Publish( "Unexpected exception on [ShutdownAll] method.", ex );

					throw;
				}
			}
		}

		private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			try
			{
				// disable timer
				_checkPoint.Change(-1, -1);
				_checkPoint.Dispose();
				_checkPoint = null;

				ShutdownAll();
			}
			catch{}
		}

		private static void DoCheckPoint( object state )
		{
			lock( SyncRoot )
			{
				try
				{
					// disable timer
					_checkPoint.Change(-1, -1);

					// do a checkpoint an all open databases
					object[] keys = new object[_dbs.Keys.Count];
					_dbs.Keys.CopyTo( keys, 0 );
					foreach( string name in keys )
					{
						Database db = _dbs[name] as Database;
						if( db != null )
						{
							try
							{
								lock( db )
								{
									db.Execute("CHECKPOINT", db.SysChannel);
								}
							}
							catch( Exception e )
							{
								LogHelper.Publish( String.Format("Unexpected exception executing CHECKPOINT statement on database {0}.", db.Name ), e );

								db.Log.Write( db.SysChannel, "Error on CHECKPOINT: " + e.Message );
							}
						}
					}
				}
				catch( Exception ex )
				{
					LogHelper.Publish( "Unexpected exception on [DoCheckPoint] method.", ex );
				}
				finally
				{
					// re-enable checkpoint
					if( _checkPoint != null )
						_checkPoint.Change( 60000, 60000 );
				}
			}
		}
	}
}
