#region Usings
using System;
using System.Collections;
#endregion

#region License
/*
 * Parser.cs
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
	/// Query Parser class.
	/// </summary>
	sealed class Parser 
	{
		private Database  dDatabase;
		private Tokenizer tTokenizer;
		private Channel   cChannel;
		private string    sTable;
		private string    sToken;
		private object    oData;
		private ColumnType       iType;
		private ExpressionType   iToken;

		public Parser(Database db, Tokenizer tokenizer, Channel channel) 
		{
			dDatabase = db;
			tTokenizer = tokenizer;
			cChannel = channel;
		}

		public Result ProcessSelect() 
		{
			Select select = ParseSelect();

			if (select.sIntoTable == null) 
			{
				// fredt@users.sourceforge.net begin changes from 1.50
				//	   return select.getResult(cChannel.getMaxRows());
				return select.GetResult( select.limitStart, select.limitCount, cChannel );
				// fredt@users.sourceforge.net end changes from 1.50
			} 
			else 
			{
				Result r = select.GetResult(0, cChannel);
				Table  t = new Table(dDatabase, true, select.sIntoTable, false);

				t.AddColumns(r);
				t.CreatePrimaryKey();

				// SELECT .. INTO can't fail because of violation of primary key
				t.Insert(r, cChannel);
				dDatabase.LinkTable(t);

				int i = r.Size;

				r = new Result();
				r.SetUpdateCount( i );

				return r;
			}
		}

		public Result ProcessDeclare() 
		{
			Declare declare = ParseDeclare();
			
			// Adds the declare to the channel
			cChannel.AddDeclare( declare );

			return declare.GetResult();
		}

		public void ProcessSet( string varName ) 
		{
			tTokenizer.GetThis("=");

			Expression val = ParseExpression();

			val.Resolve(null);

			object value = val.GetValue( val.ColumnType );

			cChannel.SetDeclareValue( varName, value );
		}

		public Result ProcessCall() 
		{
			Expression e = ParseExpression();

			e.Resolve(null);

			ColumnType type = e.ColumnType;
			object o = e.GetValue();
			Result r = new Result(1);

			r.Table[0] = "";
			r.Type[0] = type;
			r.Label[0] = "";
			r.Name[0] = "";

			object[] row = new object[1];

			row[0] = o;

			r.Add(row);

			return r;
		}

		public Result ProcessUpdate() 
		{
			string token = tTokenizer.GetString();

			cChannel.CheckReadWrite();
			cChannel.Check(token, AccessType.Update);

			Table       table = dDatabase.GetTable(token, cChannel);
			TableFilter filter = new TableFilter(table, null, false);

			tTokenizer.GetThis("SET");

			ArrayList vColumn = new ArrayList();
			ArrayList eColumn = new ArrayList();
			int    len = 0;

			token = null;

			do 
			{
				len++;

				int i = table.GetColumnNumber(tTokenizer.GetString());

				vColumn.Add(i);
				tTokenizer.GetThis("=");

				Expression e = ParseExpression();

				e.Resolve(filter);
				eColumn.Add(e);

				token = tTokenizer.GetString();
			} while (token.Equals(","));

			Expression eCondition = null;

			if (token.Equals("WHERE")) 
			{
				eCondition = ParseExpression();

				eCondition.Resolve(filter);
				filter.SetCondition(eCondition);
			} 
			else 
			{
				tTokenizer.Back();
			}

			// do the update
			Expression[] exp = new Expression[len];

			eColumn.CopyTo(exp);

			int[] col = new int[len];
			ColumnType[] type = new ColumnType[len];

			for (int i = 0; i < len; i++) 
			{
				col[i] = ((int) vColumn[i]);
				type[i] = table.GetType(col[i]);
			}

			int count = 0;

			if (filter.FindFirst()) 
			{
				Result del = new Result();    // don't need column count and so on
				Result ins = new Result();
				int    size = table.ColumnCount;

				do 
				{
					if (eCondition == null || eCondition.Test()) 
					{
						object[] nd = filter.oCurrentData;

						del.Add(nd);

						object[] ni = table.NewRow;

						for (int i = 0; i < size; i++) 
						{
							ni[i] = nd[i];
						}

						for (int i = 0; i < len; i++) 
						{
							ni[col[i]] = exp[i].GetValue(type[i]);
						}

						ins.Add(ni);
					}
				} while (filter.Next());

				lock( cChannel.SyncRoot )
				{

					cChannel.BeginNestedTransaction();

					try 
					{
						Record nd = del.Root;

						while (nd != null) 
						{
							table.DeleteNoCheck(nd.Data, cChannel);

							nd = nd.Next;
						}

						Record ni = ins.Root;

						while (ni != null) 
						{
							table.InsertNoCheck(ni.Data, cChannel);

							ni = ni.Next;
							count++;
						}

						table.CheckUpdate(col, del, ins);

						ni = ins.Root;

						while (ni != null) 
						{
							ni = ni.Next;
						}

						cChannel.EndNestedTransaction(false);
					} 
					catch (Exception e) 
					{

						// update failed (violation of primary key / referential integrity)
						cChannel.EndNestedTransaction(true);

						throw e;
					}
				}
			}

			Result r = new Result();

			r.SetUpdateCount( count );

			return r;
		}

		public Result ProcessDelete() 
		{
			tTokenizer.GetThis("FROM");

			string token = tTokenizer.GetString();

			cChannel.CheckReadWrite();
			cChannel.Check(token, AccessType.Delete);

			Table       table = dDatabase.GetTable(token, cChannel);
			TableFilter filter = new TableFilter(table, null, false);

			token = tTokenizer.GetString();

			Expression eCondition = null;

			if (token.Equals("WHERE")) 
			{
				eCondition = ParseExpression();

				eCondition.Resolve(filter);
				filter.SetCondition(eCondition);
			} 
			else 
			{
				tTokenizer.Back();
			}

			int count = 0;

			if (filter.FindFirst()) 
			{
				Result del = new Result();    // don't need column count and so on

				do 
				{
					if (eCondition == null || eCondition.Test()) 
					{
						del.Add(filter.oCurrentData);
					}
				} while (filter.Next());

				Record n = del.Root;

				while (n != null) 
				{
					table.Delete(n.Data, cChannel);

					count++;
					n = n.Next;
				}
			}

			Result r = new Result();

			r.SetUpdateCount( count );

			return r;
		}

		public Result ProcessInsert() 
		{
			tTokenizer.GetThis("INTO");

			string token = tTokenizer.GetString();

			cChannel.CheckReadWrite();
			cChannel.Check(token, AccessType.Insert);

			Table t = dDatabase.GetTable(token, cChannel);

			token = tTokenizer.GetString();

			ArrayList vcolumns = null;

			if (token.Equals("(")) 
			{
				vcolumns = new ArrayList();

				int i = 0;

				while (true) 
				{
					vcolumns.Add(tTokenizer.GetString());

					i++;
					token = tTokenizer.GetString();

					if (token.Equals(")")) 
					{
						break;
					}

					if (!token.Equals(",")) 
					{
						throw Trace.Error(Trace.UnexpectedToken, token);
					}
				}

				token = tTokenizer.GetString();
			}

			int count = 0;
			int len;

			if (vcolumns == null) 
			{
				len = t.ColumnCount;
			} 
			else 
			{
				len = vcolumns.Count;
			}

			if (token.Equals("VALUES")) 
			{
				tTokenizer.GetThis("(");

				object[] row = t.NewRow;
				int    i = 0;

				while (true) 
				{
					int column;

					if (vcolumns == null) 
					{
						column = i;

						if (i > len) 
						{
							throw Trace.Error(Trace.COLUMN_COUNT_DOES_NOT_MATCH);
						}
					} 
					else 
					{
						if (i > len) 
						{
							throw Trace.Error(Trace.COLUMN_COUNT_DOES_NOT_MATCH);
						}

						column = t.GetColumnNumber((string) vcolumns[i]);
					}

					row[column] = GetValue(t.GetType(column));
					i++;
					token = tTokenizer.GetString();

					if (token.Equals(")")) 
					{
						break;
					}

					if (!token.Equals(",")) 
					{
						throw Trace.Error(Trace.UnexpectedToken, token);
					}
				}

				t.Insert(row, cChannel);

				count = 1;
			} 
			else if (token.Equals("SELECT")) 
			{
				Result result = ProcessSelect();
				Record r = result.Root;

				Trace.Check(len == result.ColumnCount, Trace.COLUMN_COUNT_DOES_NOT_MATCH);

				int[] col = new int[len];
				ColumnType[] type = new ColumnType[len];

				for (int i = 0; i < len; i++) 
				{
					int j;

					if (vcolumns == null) 
					{
						j = i;
					} 
					else 
					{
						j = t.GetColumnNumber((string) vcolumns[i]);
					}

					col[i] = j;
					type[i] = t.GetType(j);
				}

				lock( cChannel.SyncRoot )
				{
					cChannel.BeginNestedTransaction();

					try 
					{
						while (r != null) 
						{
							object[] row = t.NewRow;

							for (int i = 0; i < len; i++) 
							{
								row[col[i]] = Column.ConvertToObject(r.Data[i], type[i]);
							}

							t.Insert(row, cChannel);

							count++;
							r = r.Next;
						}

						cChannel.EndNestedTransaction(false);
					} 
					catch (Exception e) 
					{

						// insert failed (violation of primary key)
						cChannel.EndNestedTransaction(true);

						throw e;
					}
				}
			} 
			else 
			{
				throw Trace.Error(Trace.UnexpectedToken, token);
			}

			Result rs = new Result();

			rs.SetUpdateCount( count );

			return rs;
		}
		
		/// <summary>
		/// Process ALTER TABLE statements.
		/// 
		/// ALTER TABLE tableName ADD COLUMN columnName columnType;
		/// ALTER TABLE tableName DELETE COLUMN columnName;
		/// </summary>
		/// <remarks>
		/// The only change I've made to Sergio's original code was
		/// changing the insert's to call insertNoCheck to bypass the trigger
		/// mechanism that is a part of hsqldb 1.60 and beyond. - Mark Tutt
		/// </remarks>
		/// <returns></returns>
		public Result ProcessAlter() 
		{
			tTokenizer.GetThis("TABLE");

			string token = tTokenizer.GetString();

			cChannel.CheckReadWrite();

			// cChannel.check(token,Access.ALTER); --> Accessul nu-l inca controleaza...
			string tName = token;
			string swap = tName + "SWAP";

			// nimicirea swapului...
			dDatabase.Execute("DROP TABLE " + swap, cChannel);

			Table initialTable = dDatabase.GetTable(token, cChannel);
			int   count = 0;

			token = tTokenizer.GetString();

			if (token.Equals("ADD")) 
			{
				token = tTokenizer.GetString();

				if (token.Equals("COLUMN")) 
				{
					Table swapTable = new Table(dDatabase, true, swap,
						initialTable.IsCached);

					// copiem coloanele (fara date) din tabelul initial in swap
					for (int i = 0; i < initialTable.ColumnCount; i++) 
					{
						Column aColumn = initialTable.GetColumn(i);

						swapTable.AddColumn(aColumn);
					}

					// end Of copiem coloanele...
					// aflam daca are PrimaryKey & o cream...
					string  cName = tTokenizer.GetString();
					string  cType = tTokenizer.GetString();
					ColumnType iType = Column.GetColumnType(cType);
					string  sToken = cType;
					//					int     primarykeycolumn = -1;
					bool identity = false;
					int     column = initialTable.ColumnCount + 1;

					// !--
					// stolen from CREATE TABLE...
					string  sColumn = cName;

					if (iType == ColumnType.VarChar && dDatabase.IsIgnoreCase) 
					{
						iType = ColumnType.VarCharIgnoreCase;
					}

					sToken = tTokenizer.GetString();

					if (iType == ColumnType.DbDouble && sToken.Equals("PRECISION")) 
					{
						sToken = tTokenizer.GetString();
					}

					if (sToken.Equals("(")) 
					{

						// overread length
						do 
						{
							sToken = tTokenizer.GetString();
						} while (!sToken.Equals(")"));

						sToken = tTokenizer.GetString();
					}

					// !--
					bool nullable = true;

					if (sToken.Equals("NULL")) 
					{
						sToken = tTokenizer.GetString();
					} 
					else if (sToken.Equals("NOT")) 
					{
						tTokenizer.GetThis("NULL");

						nullable = false;
						sToken = tTokenizer.GetString();
					}

					/*
					 * if(sToken.Equals("IDENTITY")) {
					 * identity=true;
					 * Trace.check(primarykeycolumn==-1,Trace.SECOND_PRIMARY_KEY,sColumn);
					 * sToken=tTokenizer.getstring();
					 * primarykeycolumn=column;
					 * }
					 *
					 * if(sToken.Equals("PRIMARY")) {
					 * tTokenizer.getThis("KEY");
					 * Trace.check(identity || primarykeycolumn==-1,
					 * Trace.SECOND_PRIMARY_KEY,sColumn);
					 * primarykeycolumn=column;
					 * //sToken=tTokenizer.getstring();
					 * }
					 * //end of STOLEN...
					 */
					swapTable.AddColumn(cName, iType, nullable,
						identity);    // under construction...

					if (initialTable.ColumnCount
						< initialTable.InternalColumnCount) 
					{
						swapTable.CreatePrimaryKey();
					} 
					else 
					{
						swapTable.CreatePrimaryKey(initialTable.PrimaryIndex.Columns[0]);
					}

					// endof PrimaryKey...
					// sa ne farimam cu indicii... ;-((
					Index idx = null;

					while (true) 
					{
						idx = initialTable.GetNextIndex(idx);

						if (idx == null) 
						{
							break;
						}

						if (idx == initialTable.PrimaryIndex) 
						{
							continue;
						}

						swapTable.CreateIndex(idx);
					}

					// end of Index...
					cChannel.Commit();
					dDatabase.LinkTable(swapTable);

					Tokenizer tmpTokenizer = new Tokenizer("SELECT * FROM "
						+ tName);
					Parser    pp = new Parser(dDatabase, tmpTokenizer, cChannel);
					string    ff = tmpTokenizer.GetString();

					if (!initialTable.IsEmpty) 
					{
						Record n = ((Result) pp.ProcessSelect()).Root;

						do 
						{
							object[] row = swapTable.NewRow;
							object[] row1 = n.Data;

							for (int i = 0; i < initialTable.ColumnCount;
								i++) 
							{
								row[i] = row1[i];
							}

							swapTable.InsertNoCheck(row, cChannel);

							n = n.Next;
						} while (n != null);
					}

					dDatabase.Execute("DROP TABLE " + tName, cChannel);

					// cream tabelul vechi cu proprietatile celui nou...
					initialTable = new Table(dDatabase, true, tName,
						swapTable.IsCached);

					for (int i = 0; i < swapTable.ColumnCount; i++) 
					{
						Column aColumn = swapTable.GetColumn(i);

						initialTable.AddColumn(aColumn);
					}

					if (swapTable.ColumnCount
						< swapTable.InternalColumnCount) 
					{
						initialTable.CreatePrimaryKey();
					} 
					else 
					{
						initialTable.CreatePrimaryKey(swapTable.PrimaryIndex.Columns[0]);
					}

					// endof PrimaryKey...
					// sa ne farimam cu indicii... ;-((
					idx = null;

					while (true) 
					{
						idx = swapTable.GetNextIndex(idx);

						if (idx == null) 
						{
							break;
						}

						if (idx == swapTable.PrimaryIndex) 
						{
							continue;
						}

						initialTable.CreateIndex(idx);
					}

					// end of Index...
					cChannel.Commit();
					dDatabase.LinkTable(initialTable);

					// end of cream...
					// copiem datele din swap in tabel...
					tmpTokenizer = new Tokenizer("SELECT * FROM " + swap);
					pp = new Parser(dDatabase, tmpTokenizer, cChannel);
					ff = tmpTokenizer.GetString();

					if (!swapTable.IsEmpty) 
					{
						Record n = ((Result) pp.ProcessSelect()).Root;

						do 
						{
							object[] row = initialTable.NewRow;
							object[] row1 = n.Data;

							for (int i = 0; i < swapTable.ColumnCount; i++) 
							{
								row[i] = row1[i];
							}

							initialTable.InsertNoCheck(row, cChannel);

							n = n.Next;
						} while (n != null);

						// end of copiem...
					}

					dDatabase.Execute("DROP TABLE " + swap, cChannel);

					count = 4;
				} 
				else 
				{
					throw Trace.Error(Trace.UnexpectedToken, token);
				}
			} 
			else if (token.Equals("DELETE")) 
			{
				token = tTokenizer.GetString();

				if (token.Equals("COLUMN")) 
				{
					Table  swapTable = new Table(dDatabase, true, swap,
						initialTable.IsCached);
					string cName = tTokenizer.GetString();
					int    undesired = initialTable.GetColumnNumber(cName);

					for (int i = 0; i < initialTable.ColumnCount; i++) 
					{
						Column aColumn = initialTable.GetColumn(i);

						if (i != undesired) 
						{
							swapTable.AddColumn(aColumn);
						}
					}

					int pKey = -1;

					// !--
					if (initialTable.ColumnCount
						< initialTable.InternalColumnCount) 
					{
						swapTable.CreatePrimaryKey();
					} 
					else 
					{
						int[] cols = initialTable.PrimaryIndex.Columns;

						pKey = cols[0];

						if ((cols[0] > undesired)
							|| (cols[0] + cols.Length < undesired)) 
						{
							if (undesired
								< initialTable.PrimaryIndex.Columns[0]) 
							{

								// reindexarea...
								for (int i = 0; i < cols.Length; i++) 
								{
									cols[i]--;
								}

								// endOf reindexarea...
							}
							// MT: This initially wouldn't compile, missing the array index on cols[]
							swapTable.CreatePrimaryKey(cols[0]);
						} 
						else 
						{
							swapTable.CreatePrimaryKey();
						}
					}

					// endof PrimaryKey...
					// sa ne farimam cu indicii... ;-((
					Index idx = null;

					while (true) 
					{
						idx = initialTable.GetNextIndex(idx);

						if (idx == null) 
						{
							break;
						}

						if (idx == initialTable.PrimaryIndex) 
						{
							continue;
						}

						bool flag = true;
						int[]   cols = idx.Columns;

						for (int i = 0; i < cols.Length; i++) 
						{
							if (cols[i] == undesired) 
							{
								flag = false;
							}
						}

						if (flag) 
						{
							Index tIdx;

							for (int i = 0; i < cols.Length; i++) 
							{
								if (cols[i] > undesired) 
								{
									cols[i]--;
								}
							}

							tIdx = new Index(idx.Name, idx.Columns, idx.ColumnType, idx.IsUnique);

							swapTable.CreateIndex(tIdx);
						}
					}

					// !--
					cChannel.Commit();
					dDatabase.LinkTable(swapTable);

					Tokenizer tmpTokenizer = new Tokenizer("SELECT * FROM "
						+ tName);
					Parser    pp = new Parser(dDatabase, tmpTokenizer, cChannel);
					string    ff = tmpTokenizer.GetString();

					if (!initialTable.IsEmpty) 
					{
						Record n = ((Result) pp.ProcessSelect()).Root;

						do 
						{
							object[] row = swapTable.NewRow;
							object[] row1 = n.Data;
							int    j = 0;

							for (int i = 0; i < initialTable.ColumnCount;
								i++) 
							{
								if (i != undesired) 
								{
									row[j] = row1[i];
									j++;
								}
							}

							swapTable.InsertNoCheck(row, cChannel);

							n = n.Next;
						} while (n != null);
					}

					dDatabase.Execute("DROP TABLE " + tName, cChannel);

					// cream tabelul vechi cu proprietatile celui nou...
					initialTable = new Table(dDatabase, true, tName,
						swapTable.IsCached);

					for (int i = 0; i < swapTable.ColumnCount; i++) 
					{
						Column aColumn = swapTable.GetColumn(i);

						initialTable.AddColumn(aColumn);
					}

					// !--
					if (swapTable.ColumnCount
						< swapTable.InternalColumnCount) 
					{
						initialTable.CreatePrimaryKey();
					} 
					else 
					{
						initialTable.CreatePrimaryKey(swapTable.PrimaryIndex.Columns[0]);
					}

					// endof PrimaryKey...
					// sa ne farimam cu indicii... ;-((
					idx = null;

					while (true) 
					{
						idx = swapTable.GetNextIndex(idx);

						if (idx == null) 
						{
							break;
						}

						if (idx == swapTable.PrimaryIndex) 
						{
							continue;
						}

						initialTable.CreateIndex(idx);
					}

					// end of Index...
					// !--
					cChannel.Commit();
					dDatabase.LinkTable(initialTable);

					// end of cream...
					// copiem datele din swap in tabel...
					tmpTokenizer = new Tokenizer("SELECT * FROM " + swap);
					pp = new Parser(dDatabase, tmpTokenizer, cChannel);
					ff = tmpTokenizer.GetString();

					if (!swapTable.IsEmpty) 
					{
						Record n = ((Result) pp.ProcessSelect()).Root;

						do 
						{
							object[] row = initialTable.NewRow;
							object[] row1 = n.Data;

							for (int i = 0; i < swapTable.ColumnCount; i++) 
							{
								row[i] = row1[i];
							}

							initialTable.InsertNoCheck(row, cChannel);

							n = n.Next;
						} while (n != null);

						// end of copiem...
					}

					dDatabase.Execute("DROP TABLE " + swap, cChannel);

					count = 3;
				} 
				else 
				{
					throw Trace.Error(Trace.UnexpectedToken, token);
				}

				count = 3;
			}

			Result r = new Result();

			r.SetUpdateCount( count );   

			return r;
		}

		private Select ParseSelect() 
		{
			Select select = new Select();
			// fredt@users.sourceforge.net begin changes from 1.50
			select.limitStart = 0;
			select.limitCount = cChannel.MaxRows;
			// fredt@users.sourceforge.net end changes from 1.50
			string token = tTokenizer.GetString();

			if (token.Equals("DISTINCT")) 
			{
				select.bDistinct = true;
				// fredt@users.sourceforge.net begin changes from 1.50
			} 
			else if( token.Equals("LIMIT")) 
			{
				string limStart = tTokenizer.GetString();
				string limEnd = tTokenizer.GetString();
				//System.out.println( "LIMIT used from "+limStart+","+limEnd);
				select.limitStart = int.Parse(limStart);
				select.limitCount = int.Parse(limEnd);
				// fredt@users.sourceforge.net end changes from 1.50
			} 
			else 
			{
				tTokenizer.Back();
			}

			// parse column list
			ArrayList vcolumn = new ArrayList();
			select.OnlyVars = true;

			do 
			{
				Expression e = ParseExpression();

				token = tTokenizer.GetString();

				if (token.Equals("AS")) 
				{
					e.Alias = tTokenizer.GetName();

					token = tTokenizer.GetString();
				} 
				else if (tTokenizer.WasName) 
				{
					e.Alias = token;

					token = tTokenizer.GetString();
				}

				vcolumn.Add(e);

				select.OnlyVars = select.OnlyVars & (e.Type == ExpressionType.Variable);

			} while (token.Equals(","));

			if( !select.OnlyVars )
			{
				if (token.Equals("INTO")) 
				{
					select.sIntoTable = tTokenizer.GetString();
					token = tTokenizer.GetString();
				}

				if (!token.Equals("FROM")) 
				{
					throw Trace.Error(Trace.UnexpectedToken, token);
				}

				Expression condition = null;

				// parse table list
				ArrayList     vfilter = new ArrayList();

				vfilter.Add(ParseTableFilter(false));

				while (true) 
				{
					token = tTokenizer.GetString();

					if (token.Equals("LEFT")) 
					{
						token = tTokenizer.GetString();

						if (token.Equals("OUTER")) 
						{
							token = tTokenizer.GetString();
						}

						Trace.Check(token.Equals("JOIN"), Trace.UnexpectedToken,
							token);
						vfilter.Add(ParseTableFilter(true));
						tTokenizer.GetThis("ON");

						condition = AddCondition(condition, ParseExpression());
					} 
					else if (token.Equals("INNER")) 
					{
						tTokenizer.GetThis("JOIN");
						vfilter.Add(ParseTableFilter(false));
						tTokenizer.GetThis("ON");

						condition = AddCondition(condition, ParseExpression());
					} 
					else if (token.Equals(",")) 
					{
						vfilter.Add(ParseTableFilter(false));
					} 
					else 
					{
						break;
					}
				}

				tTokenizer.Back();

				int	    len = vfilter.Count;
				TableFilter[] filter = new TableFilter[len];

				vfilter.CopyTo(filter);

				select.tFilter = filter;

				// expand [table.]* columns
				len = vcolumn.Count;

				for (int i = 0; i < len; i++) 
				{
					Expression e = (Expression) (vcolumn[i]);

					if (e.Type == ExpressionType.Asterix) 
					{
						int    current = i;
						Table  table = null;
						string n = e.TableName;

						for (int t = 0; t < filter.Length; t++) 
						{
							TableFilter f = filter[t];

							e.Resolve(f);

							if (n != null &&!n.Equals(f.Name)) 
							{
								continue;
							}

							table = f.Table;

							int col = table.ColumnCount;

							for (int c = 0; c < col; c++) 
							{
								Expression ins =
									new Expression(f.Name,
									table.GetColumnName(c));

								vcolumn.Insert(current++,ins);

								// now there is one element more to parse
								len++;
							}
						}

						Trace.Check(table != null, Trace.TABLE_NOT_FOUND, n);

						// minus the asterix element
						len--;

						vcolumn.RemoveAt(current);
					}
					else if (e.Type == ExpressionType.DatabaseColumn)
					{
						if (e.TableName == null) 
						{
							for (int filterIndex=0; filterIndex < filter.Length; filterIndex++) 
							{
								e.Resolve(filter[filterIndex]);
							}
						}
					}
				}

				select.iResultLen = len;

				// where
				token = tTokenizer.GetString();

				if (token.Equals("WHERE")) 
				{
					condition = AddCondition(condition, ParseExpression());
					token = tTokenizer.GetString();
				}

				select.eCondition = condition;

				if (token.Equals("GROUP")) 
				{
					tTokenizer.GetThis("BY");

					len = 0;

					do 
					{
						vcolumn.Add(ParseExpression());

						token = tTokenizer.GetString();
						len++;
					} while (token.Equals(","));

					select.iGroupLen = len;
				}

				if (token.Equals("ORDER")) 
				{
					tTokenizer.GetThis("BY");

					len = 0;

					do 
					{
						Expression e = ParseExpression();

						if (e.Type == ExpressionType.Value) 
						{

							// order by 1,2,3
							if (e.ColumnType == ColumnType.Integer) 
							{
								int i = Convert.ToInt32(e.GetValue());

								e = (Expression) vcolumn[i - 1];
							}
						} 
						else if (e.Type == ExpressionType.DatabaseColumn
							&& e.TableName == null) 
						{

							// this could be an alias column
							string s = e.ColumnName;

							for (int i = 0; i < vcolumn.Count; i++) 
							{
								Expression ec = (Expression) vcolumn[i];

								if (s.Equals(ec.Alias)) 
								{
									e = ec;

									break;
								}
							}
						}

						token = tTokenizer.GetString();

						if (token.Equals("DESC")) 
						{
							e.IsDescending = true;

							token = tTokenizer.GetString();
						} 
						else if (token.Equals("ASC")) 
						{
							token = tTokenizer.GetString();
						}

						vcolumn.Add(e);

						len++;
					} while (token.Equals(","));

					select.iOrderLen = len;
				}

				len = vcolumn.Count;
				select.eColumn = new Expression[len];

				vcolumn.CopyTo(select.eColumn);

				if (token.Equals("UNION")) 
				{
					token = tTokenizer.GetString();

					if (token.Equals("ALL")) 
					{
						select.UnionType = SelectType.UnionAll;
					} 
					else 
					{
						select.UnionType = SelectType.Union;

						tTokenizer.Back();
					}

					tTokenizer.GetThis("SELECT");

					select.sUnion = ParseSelect();
				} 
				else if (token.Equals("INTERSECT")) 
				{
					tTokenizer.GetThis("SELECT");

					select.UnionType = SelectType.Intersect;
					select.sUnion = ParseSelect();
				} 
				else if (token.Equals("EXCEPT") || token.Equals("MINUS")) 
				{
					tTokenizer.GetThis("SELECT");

					select.UnionType = SelectType.Except;
					select.sUnion = ParseSelect();
				} 
				else 
				{
					tTokenizer.Back();
				}
			}
			else
			{
				select.tFilter = new TableFilter[]{};

				int len = vcolumn.Count;
				select.iResultLen = len;
				select.eColumn = new Expression[len];
				vcolumn.CopyTo(select.eColumn);
			}

			return select;
		}

		private Declare ParseDeclare() 
		{
			Declare declare = new Declare();

			Expression e = ParseExpression();

			declare.Name = e.ColumnName;
			declare.Expression = e;

			return declare;
		}

		private TableFilter ParseTableFilter(bool outerjoin)
		{
			string token = tTokenizer.GetString();
			Table  t = null;

			if (token.Equals("(")) 
			{
				tTokenizer.GetThis("SELECT");

				Select s = ParseSelect();
				Result r = s.GetResult(0, null);

				// it's not a problem that this table has not a unique name
				t = new Table(dDatabase, false, "SYSTEM_SUBQUERY", false);

				tTokenizer.GetThis(")");
				t.AddColumns(r);
				t.CreatePrimaryKey();

				// subquery creation can't fail because of violation of primary key
				t.Insert(r, cChannel);
			} 
			else 
			{
				cChannel.Check(token, AccessType.Select);

				t = dDatabase.GetTable(token, cChannel);
			}

			string sAlias = null;

			token = tTokenizer.GetString();

			if (token.Equals("AS")) 
			{
				sAlias = tTokenizer.GetName();
			} 
			else if (tTokenizer.WasName) 
			{
				sAlias = token;
			} 
			else 
			{
				tTokenizer.Back();
			}

			return new TableFilter(t, sAlias, outerjoin);
		}

		private Expression AddCondition(Expression e1, Expression e2) 
		{
			if (e1 == null) 
			{
				return e2;
			} 
			else if (e2 == null) 
			{
				return e1;
			} 
			else 
			{
				return new Expression(ExpressionType.And, e1, e2);
			}
		}

		private object GetValue(ColumnType type) 
		{
			Expression r = ParseExpression();

			r.Resolve(null);

			return r.GetValue(type);
		}

		private Expression ParseExpression() 
		{
			Read();

			// todo: really this should be in readTerm
			// but then grouping is much more complex
			if (iToken == ExpressionType.Minimum || iToken == ExpressionType.Maximum
				|| iToken == ExpressionType.Count || iToken == ExpressionType.Sum
				|| iToken == ExpressionType.Average) 
			{
				ExpressionType type = iToken;

				Read();

				Expression r = new Expression(type, ReadOr(), null);

				tTokenizer.Back();

				return r;
			}

			Expression rx = ReadOr();

			tTokenizer.Back();

			return rx;
		}

		private Expression ReadOr() 
		{
			Expression r = ReadAnd();

			while (iToken == ExpressionType.Or) 
			{
				ExpressionType type = iToken;
				Expression a = r;

				Read();

				r = new Expression(type, a, ReadAnd());
			}

			return r;
		}

		private Expression ReadAnd() 
		{
			Expression r = ReadCondition();

			while (iToken == ExpressionType.And) 
			{
				ExpressionType type = iToken;
				Expression a = r;

				Read();

				r = new Expression(type, a, ReadCondition());
			}

			return r;
		}

		private Expression ReadCondition() 
		{
			if (iToken == ExpressionType.Not) 
			{
				ExpressionType type = iToken;

				Read();

				return new Expression(type, ReadCondition(), null);
			} 
			else if (iToken == ExpressionType.Exists) 
			{
				ExpressionType type = iToken;

				Read();
				ReadThis(ExpressionType.Open);
				Trace.Check(iToken == ExpressionType.Select, Trace.UnexpectedToken);

				Expression s = new Expression(ParseSelect());

				Read();
				ReadThis(ExpressionType.Close);

				return new Expression(type, s, null);
			} 
			else 
			{
				Expression a = ReadConcat();
				bool    not = false;

				if (iToken == ExpressionType.Not) 
				{
					not = true;

					Read();
				}

				if (iToken == ExpressionType.Like) 
				{
					Read();

					Expression b = ReadConcat();
					char       escape = '0';

					if (sToken.Equals("ESCAPE")) 
					{
						Read();

						Expression c = ReadTerm();

						Trace.Check(c.Type == ExpressionType.Value,
							Trace.InvalidEscape);

						string s = (string) c.GetValue(ColumnType.VarChar);

						if (s == null || s.Length < 1) 
						{
							throw Trace.Error(Trace.InvalidEscape, s);
						}

						escape = Convert.ToChar(s.Substring(0,1));
					}

					a = new Expression(ExpressionType.Like, a, b);

					a.SetLikeEscape(escape);
				} 
				else if (iToken == ExpressionType.Between) 
				{
					Read();

					Expression l = new Expression(ExpressionType.BiggerEqual, a,
						ReadConcat());

					ReadThis(ExpressionType.And);

					Expression h = new Expression(ExpressionType.SmallerEqual, a,
						ReadConcat());

					a = new Expression(ExpressionType.And, l, h);
				} 
				else if (iToken == ExpressionType.In) 
				{
					ExpressionType type = iToken;

					Read();
					ReadThis(ExpressionType.Open);

					Expression b = null;

					if (iToken == ExpressionType.Select) 
					{
						b = new Expression(ParseSelect());

						Read();
					} 
					else 
					{
						tTokenizer.Back();

						ArrayList v = new ArrayList();

						while (true) 
						{
							v.Add(GetValue(ColumnType.VarChar));
							Read();

							if (iToken != ExpressionType.Comma) 
							{
								break;
							}
						}

						b = new Expression(v);
					}

					ReadThis(ExpressionType.Close);

					a = new Expression(type, a, b);
				} 
				else 
				{
					Trace.Check(!not, Trace.UnexpectedToken);

					if (Expression.IsCompare(iToken)) 
					{
						ExpressionType type = iToken;

						Read();

						return new Expression(type, a, ReadConcat());
					}

					return a;
				}

				if (not) 
				{
					a = new Expression(ExpressionType.Not, a, null);
				}

				return a;
			}
		}

		private void ReadThis(ExpressionType type) 
		{
			Trace.Check(iToken == type, Trace.UnexpectedToken);
			Read();
		}

		private Expression ReadConcat() 
		{
			Expression r = ReadSum();

			while (iToken == ExpressionType.StringConcat) 
			{
				ExpressionType type = ExpressionType.Concat;
				Expression a = r;

				Read();

				r = new Expression(type, a, ReadSum());
			}

			return r;
		}

		private Expression ReadSum() 
		{
			Expression r = ReadFactor();

			while (true) 
			{
				ExpressionType type;

				if (iToken == ExpressionType.Plus) 
				{
					type = ExpressionType.Add;
				} 
				else if (iToken == ExpressionType.Negate) 
				{
					type = ExpressionType.Subtract;
				} 
				else 
				{
					break;
				}

				Expression a = r;

				Read();

				r = new Expression(type, a, ReadFactor());
			}

			return r;
		}

		private Expression ReadFactor() 
		{
			Expression r = ReadTerm();

			while (iToken == ExpressionType.Multiply || iToken == ExpressionType.Divide) 
			{
				ExpressionType type = iToken;
				Expression a = r;

				Read();

				r = new Expression(type, a, ReadTerm());
			}

			return r;
		}

		private Expression ReadTerm() 
		{
			Expression r = null;

			if (iToken == ExpressionType.DatabaseColumn) 
			{
				string name = sToken;

				r = new Expression(sTable, sToken);

				Read();

				if (iToken == ExpressionType.Open) 
				{
					Function f = new Function(dDatabase.GetAlias(name), cChannel);
					int      len = f.GetArgCount();
					int      i = 0;

					Read();

					if (iToken != ExpressionType.Close) 
					{
						while (true) 
						{
							f.SetArgument(i++, ReadOr());

							if (iToken != ExpressionType.Comma) 
							{
								break;
							}

							Read();
						}
					}

					ReadThis(ExpressionType.Close);

					r = new Expression(f);
				} 
			}

			else if (iToken == ExpressionType.Negate) 
			{
				ExpressionType type = iToken;

				Read();

				r = new Expression(type, ReadTerm(), null);
			} 
			else if (iToken == ExpressionType.Plus) 
			{
				Read();

				r = ReadTerm();
			} 
			else if (iToken == ExpressionType.Open) 
			{
				Read();

				r = ReadOr();

				if (iToken != ExpressionType.Close) 
				{
					throw Trace.Error(Trace.UnexpectedToken, sToken);
				}

				Read();
			} 
			else if (iToken == ExpressionType.Value) 
			{
				r = new Expression(iType, oData);

				Read();
			} 
			else if (iToken == ExpressionType.Select) 
			{
				r = new Expression(ParseSelect());

				Read();
			} 
			else if (iToken == ExpressionType.Multiply) 
			{
				r = new Expression(sTable, null);

				Read();
			} 
			else if (iToken == ExpressionType.IfNull || iToken == ExpressionType.Concat) 
			{
				ExpressionType type = iToken;

				Read();
				ReadThis(ExpressionType.Open);

				r = ReadOr();

				ReadThis(ExpressionType.Comma);

				r = new Expression(type, r, ReadOr());

				ReadThis(ExpressionType.Close);
			} 
			else if (iToken == ExpressionType.CaseWhen) 
			{
				ExpressionType type = iToken;

				Read();
				ReadThis(ExpressionType.Open);

				r = ReadOr();

				ReadThis(ExpressionType.Comma);

				Expression thenelse = ReadOr();

				ReadThis(ExpressionType.Comma);

				// thenelse part is never evaluated; only init
				thenelse = new Expression(type, thenelse, ReadOr());
				r = new Expression(type, r, thenelse);

				ReadThis(ExpressionType.Close);
			} 
			else if (iToken == ExpressionType.Convert) 
			{
				ExpressionType type = iToken;

				Read();
				ReadThis(ExpressionType.Open);

				r = ReadOr();

				ReadThis(ExpressionType.Comma);

				ColumnType t = Column.GetColumnType(sToken);

				r = new Expression(type, r, null);

				r.SetDataType(t);
				Read();
				ReadThis(ExpressionType.Close);
			} 
			else if (iToken == ExpressionType.Cast) 
			{
				Read();
				ReadThis(ExpressionType.Open);

				r = ReadOr();

				Trace.Check(sToken.Equals("AS"), Trace.UnexpectedToken, sToken);
				Read();

				ColumnType t = Column.GetColumnType(sToken);

				r = new Expression(ExpressionType.Convert, r, null);

				r.SetDataType(t);
				Read();
				ReadThis(ExpressionType.Close);
			} 				
			else if (iToken == ExpressionType.Variable) 
			{
				ExpressionType type = iToken;

				r = new Expression(null, sToken);
				r.Type = type;

				string columnType = tTokenizer.GetString();

				if( Column.IsValidDataType( columnType ) )
				{
					ColumnType iType = Column.GetColumnType( columnType );

					if (iType == ColumnType.VarChar && dDatabase.IsIgnoreCase) 
					{
						iType = ColumnType.VarCharIgnoreCase;
					}

					r.SetDataType( iType );
					r.SetArg( null );
				}
				else
				{
					tTokenizer.Back();

					Declare declare = cChannel.GetDeclare( r.ColumnName );
					if( declare != null )
					{
						r = declare.Expression;
					}
					else
					{
						throw Trace.Error(Trace.VARIABLE_NOT_DECLARED, sToken);
					}
				}

				Read();
			}
			else 
			{
				throw Trace.Error(Trace.UnexpectedToken, sToken);
			}

			return r;
		}

		private void Read() 
		{
			sToken = tTokenizer.GetString();

			if (tTokenizer.WasValue) 
			{
				iToken = ExpressionType.Value;
				oData = tTokenizer.Value;
				iType = tTokenizer.ColumnType;
			}
			else if (tTokenizer.WasVariable) 
			{
				iToken = ExpressionType.Variable;
				sTable = null;
			} 
			else if (tTokenizer.WasName) 
			{
				iToken = ExpressionType.DatabaseColumn;
				sTable = null;
			} 
			else if (tTokenizer.WasLongName) 
			{
				sTable = tTokenizer.LongNameFirst;
				sToken = tTokenizer.LongNameLast;

				if (sToken.Equals("*")) 
				{
					iToken = ExpressionType.Multiply;
				} 
				else 
				{
					iToken = ExpressionType.DatabaseColumn;
				}
			} 
			else if (sToken.Equals("")) 
			{
				iToken = ExpressionType.End;
			} 
			else if (sToken.Equals("AND")) 
			{
				iToken = ExpressionType.And;
			} 
			else if (sToken.Equals("OR")) 
			{
				iToken = ExpressionType.Or;
			} 
			else if (sToken.Equals("NOT")) 
			{
				iToken = ExpressionType.Not;
			} 
			else if (sToken.Equals("IN")) 
			{
				iToken = ExpressionType.In;
			} 
			else if (sToken.Equals("EXISTS")) 
			{
				iToken = ExpressionType.Exists;
			} 
			else if (sToken.Equals("BETWEEN")) 
			{
				iToken = ExpressionType.Between;
			} 
			else if (sToken.Equals("+")) 
			{
				iToken = ExpressionType.Plus;
			} 
			else if (sToken.Equals("-")) 
			{
				iToken = ExpressionType.Negate;
			} 
			else if (sToken.Equals("*")) 
			{
				iToken = ExpressionType.Multiply;
				sTable = null;    // in case of ASTERIX
			} 
			else if (sToken.Equals("/")) 
			{
				iToken = ExpressionType.Divide;
			} 
			else if (sToken.Equals("||")) 
			{
				iToken = ExpressionType.StringConcat;
			} 
			else if (sToken.Equals("(")) 
			{
				iToken = ExpressionType.Open;
			} 
			else if (sToken.Equals(")")) 
			{
				iToken = ExpressionType.Close;
			} 
			else if (sToken.Equals("SELECT")) 
			{
				iToken = ExpressionType.Select;
			} 
			else if (sToken.Equals("<")) 
			{
				iToken = ExpressionType.Smaller;
			} 
			else if (sToken.Equals("<=")) 
			{
				iToken = ExpressionType.SmallerEqual;
			} 
			else if (sToken.Equals(">=")) 
			{
				iToken = ExpressionType.BiggerEqual;
			} 
			else if (sToken.Equals(">")) 
			{
				iToken = ExpressionType.Bigger;
			} 
			else if (sToken.Equals("=")) 
			{
				iToken = ExpressionType.Equal;
			} 
			else if (sToken.Equals("IS")) 
			{
				sToken = tTokenizer.GetString();

				if (sToken.Equals("NOT")) 
				{
					iToken = ExpressionType.NotEqual;
				} 
				else 
				{
					iToken = ExpressionType.Equal;

					tTokenizer.Back();
				}
			} 
			else if (sToken.Equals("<>") || sToken.Equals("!=")) 
			{
				iToken = ExpressionType.NotEqual;
			} 
			else if (sToken.Equals("LIKE")) 
			{
				iToken = ExpressionType.Like;
			} 
			else if (sToken.Equals("COUNT")) 
			{
				iToken = ExpressionType.Count;
			} 
			else if (sToken.Equals("SUM")) 
			{
				iToken = ExpressionType.Sum;
			} 
			else if (sToken.Equals("MIN")) 
			{
				iToken = ExpressionType.Minimum;
			} 
			else if (sToken.Equals("MAX")) 
			{
				iToken = ExpressionType.Maximum;
			} 
			else if (sToken.Equals("AVG")) 
			{
				iToken = ExpressionType.Average;
			} 
			else if (sToken.Equals("IFNULL")) 
			{
				iToken = ExpressionType.IfNull;
			} 
			else if (sToken.Equals("CONVERT")) 
			{
				iToken = ExpressionType.Convert;
			} 
			else if (sToken.Equals("CAST")) 
			{
				iToken = ExpressionType.Cast;
			} 
			else if (sToken.Equals("CASEWHEN")) 
			{
				iToken = ExpressionType.CaseWhen;
			} 
			else if (sToken.Equals(",")) 
			{
				iToken = ExpressionType.Comma;
			} 
			else 
			{
				iToken = ExpressionType.End;
			}
		}
	}
}
