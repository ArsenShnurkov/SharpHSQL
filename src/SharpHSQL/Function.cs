#region Usings
using System;
using System.Collections;
using System.Reflection;
#endregion

#region License
/*
 * Function.cs
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
	/// Wrapper class for an external Function.
	/// </summary>
	sealed class Function
	{

		private String         sFunction;
		private MethodInfo     mMethod;
		private Type           cReturnType;
		private Type[]         aArgTypes;
		private ColumnType     iReturnType;
		//private int            iArgCount;
		private int            iSqlArgCount;
		//private int            iSqlArgStart;
		//private int[]          iArgType;
		//private bool[]         bArgNullable;
		private Expression[]   eArg;
		//private bool           bConnection;
		private static Hashtable methodCache = new Hashtable();
		private int            fID;
		private string         name;        // name used to call function
		//private bool           isSimple;    //CURRENT_TIME, NOW etc.
		private bool           hasAggregate;
		private Channel		   channel;
		private static Assembly thisAssembly = Assembly.GetExecutingAssembly();

		public Function( string fqn, Channel channel )
		{
			sFunction = fqn;
			this.channel = channel;

			fID = Library.FunctionID(channel.Database.Alias, fqn);

			if( fID > -1 ) // internal function
			{
				mMethod = methodCache[fqn] as MethodInfo;

				if( mMethod == null )
				{
					int i = fqn.LastIndexOf('.');

					Trace.Check(i != -1, Trace.UnexpectedToken, fqn);

					String classname = fqn.Substring(0, i);

					Type type = thisAssembly.GetType(classname, false);

					Trace.Check(type != null, Trace.ERROR_IN_FUNCTION, fqn);

					this.name = fqn.Substring(i+1);

					mMethod = type.GetMethod(name);

					Trace.Check(mMethod != null, Trace.UNKNOWN_FUNCTION, fqn);

					methodCache[fqn] = mMethod;
				}
			}
			else // external function
			{
				mMethod = methodCache[fqn] as MethodInfo;

				if( mMethod == null )
				{
					int x = fqn.IndexOf(',');

					Trace.Check(x != -1, Trace.UnexpectedToken, fqn);

					string assembly = fqn.Substring(0, x);
					string className = fqn.Substring(x+1);
					
					int i = className.LastIndexOf('.');

					Trace.Check(i != -1, Trace.UnexpectedToken, fqn);

					this.name = className.Substring(i+1);

					className = className.Substring(0, i);

					Assembly a = Assembly.Load(assembly);

					Type type = a.GetType(className, false);

					Trace.Check(type != null, Trace.ERROR_IN_FUNCTION, fqn);

					mMethod = type.GetMethod(name);

					Trace.Check(mMethod != null, Trace.UNKNOWN_FUNCTION, fqn);

					methodCache[fqn] = mMethod;
				}
			}

			ParameterInfo[] pi = mMethod.GetParameters();

			if( pi != null && pi.Length > 0 )
				eArg = new Expression[pi.Length];
			else
				eArg = new Expression[]{};

			aArgTypes = new Type[eArg.Length];
			for( int i=0;i<aArgTypes.Length;i++)
			{
				aArgTypes[i] = pi[i].ParameterType;
			}

			cReturnType = mMethod.ReturnType;

			iReturnType = GetDataType( cReturnType );
			iSqlArgCount = eArg.Length;

		}

		public void CheckResolved()
		{
			foreach( Expression ex in eArg )
				ex.CheckResolved();
		}

		public void Resolve( TableFilter filter )
		{
			foreach( Expression ex in eArg )
			{
				if( !ex.IsResolved )
					ex.Resolve(filter);
			}
		}

		public MethodInfo GetMethodInfo( string fqn )
		{
			return methodCache[fqn] as MethodInfo;
		}

		public ColumnType GetReturnType()
		{
			return iReturnType;
		}

		public int GetArgCount()
		{
			return iSqlArgCount;
		}

		public void SetArgument( int arg, Expression e )
		{
			eArg[arg] = e;

			hasAggregate = hasAggregate || (e != null && e.IsAggregate);
		}

		public object GetValue()
		{
			if( fID != -1 )
			{

				return Library.Invoke( fID, channel, GetParameters() );
			}
			else
			{
				if( mMethod.IsStatic )
				{
					return mMethod.Invoke( null, GetParameters() );
				}
				else
				{
					#if !POCKETPC
					object obj = Activator.CreateInstance( mMethod.DeclaringType, false );
					#else
					object obj = Activator.CreateInstance( mMethod.DeclaringType );
					#endif
					return mMethod.Invoke( obj, GetParameters() );
				}
			}
		}

		private object[] GetParameters()
		{
			object[] p = new object[eArg.Length];

			for( int i=0;i<eArg.Length;i++ )
				#if !POCKETPC
				p[i] = Convert.ChangeType( eArg[i].GetValue(), aArgTypes[i] );
				#else
				p[i] = Convert.ChangeType( eArg[i].GetValue(), aArgTypes[i], null );
				#endif

			return p;
		}

		internal static ColumnType GetDataType( Type type )
		{
			switch( type.Name )
			{
				case "Int64":
					return ColumnType.BigInt;
				case "Byte[]":
					return ColumnType.LongVarBinary;
				case "Boolean":
					return ColumnType.Bit;
				case "Char":
					return ColumnType.Char;
				case "DateTime":
					return ColumnType.Date;
				case "Decimal":
					return ColumnType.DbDecimal;
				case "Double":
					return ColumnType.DbDouble;
				case "Single":
					return ColumnType.Float;
				case "Int32":
					return ColumnType.Integer;
				case "DBNull":
					return ColumnType.Null;
				case "Int16":
					return ColumnType.SmallInt;
				case "Byte":
					return ColumnType.TinyInt;
				case "String":
					return ColumnType.VarChar;
				default:
					return ColumnType.Other;
			}
		}
	}
}
