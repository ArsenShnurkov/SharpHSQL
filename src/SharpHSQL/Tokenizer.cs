#region Usings
using System;
using System.Collections;
using System.Text;
using System.Globalization;
#endregion

#region License
/*
 * Tokenizer.cs
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
	/// Tokenizer class.
	/// </summary>
	sealed class Tokenizer 
	{
		private string	     sCommand;
		private char[]	     cCommand;
		private int		     iLength;
		private int		     iIndex;
		private TokenType	 iType;
		private string	     sToken, sLongNameFirst, sLongNameLast;
		private bool	     bWait;
		private static Hashtable hKeyword;

		string[] keyword = 
		{
			"AND", "ALL", "AVG", "BY", "BETWEEN", "COUNT", "CASEWHEN",
			"DISTINCT", "EXISTS", "EXCEPT", "FALSE", "FROM",
			"GROUP", "IF", "INTO", "IFNULL", "IS", "IN", "INTERSECT", "INNER",
			"LEFT", "LIKE", "MAX", "MIN", "NULL", "NOT", "ON", "ORDER", "OR",
			"OUTER", "PRIMARY", "SELECT", "SET", "SUM", "TO", "TRUE",
			"UNIQUE", "UNION", "VALUES", "WHERE", "CONVERT", "CAST",
			"CONCAT", "MINUS", "CALL"
		};

	
		public Tokenizer(string s) 
		{
			if (hKeyword == null)
			{
				hKeyword = new Hashtable();
				for (int i = 0; i < keyword.Length; i++) 
				{
					hKeyword.Add(keyword[i], i);
				}	
			}

			sCommand = s;
			cCommand = s.ToCharArray();
			iLength = cCommand.Length;
			iIndex = 0;
		}

		public void Back() 
		{
			Trace.Assert(!bWait, "back");

			bWait = true;
		}

		public void GetThis(string match) 
		{
			GetToken();

			if (!sToken.Equals(match)) 
			{
				throw Trace.Error(Trace.UnexpectedToken, sToken);
			}
		}

		public string GetStringToken() 
		{
			GetToken();

			// todo: this is just compatibility for old style USER 'sa'
			if (iType == TokenType.STRING) 
			{
				return sToken.Substring(1).ToUpper();
			} 
			else if (iType == TokenType.NAME) 
			{
				return sToken;
			} 
			else if (iType == TokenType.QUOTED_IDENTIFIER) 
			{
				return sToken.ToUpper();
			}

			throw Trace.Error(Trace.UnexpectedToken, sToken);
		}

		public bool WasValue
		{
			get
			{
				if (iType == TokenType.STRING || 
					iType == TokenType.NUMBER || 
					iType == TokenType.FLOAT) 
				{
					return true;
				}

				if (sToken.Equals("NULL")) 
				{
					return true;
				}

				if (sToken.Equals("TRUE") || sToken.Equals("FALSE")) 
				{
					return true;
				}

				return false;
			}
		}

		public bool WasLongName
		{
			get
			{
				return iType == TokenType.LONG_NAME;
			}
		}

		public bool WasVariable
		{
			get
			{
				return iType == TokenType.VARIABLE;
			}
		}

		public bool WasName
		{
			get
			{
				if (iType == TokenType.QUOTED_IDENTIFIER) 
				{
					return true;
				}

				if (iType != TokenType.NAME) 
				{
					return false;
				}

				return !hKeyword.ContainsKey(sToken);
			}
		}

		public string LongNameFirst
		{
			get
			{
				return sLongNameFirst;
			}
		}

		public string LongNameLast
		{
			get
			{
				return sLongNameLast;
			}
		}

		public string GetName() 
		{
			GetToken();

			if (!WasName) 
			{
				throw Trace.Error(Trace.UnexpectedToken, sToken);
			}

			return sToken;
		}

		public string GetString() 
		{
			GetToken();

			return sToken;
		}

		public TokenType TokenType 
		{
			get
			{
				return iType;
			}
		}

		public ColumnType ColumnType
		{
			get
			{
				// todo: make sure it's used only for Values!
				// todo: synchronize iType with hColumn
				switch (iType) 
				{
					case TokenType.STRING:
						return ColumnType.VarChar;

					case TokenType.NUMBER:
						return ColumnType.Integer;

					case TokenType.FLOAT:
						return ColumnType.DbDouble;

					case TokenType.LONG:
						return ColumnType.BigInt;
				}

				return ColumnType.Null;
			}
		}

		public object Value
		{
			get
			{
				if (!WasValue) 
				{
					throw Trace.Error(Trace.UnexpectedToken, sToken);
				}

				if (iType == TokenType.STRING) 
				{
					return sToken.Substring(1);    // todo: this is a bad solution: remove '
				}

				// convert NULL to null string if not a string
				// todo: make this more straightforward
				if (sToken.Equals("NULL")) 
				{
					return null;
				}

				if (iType == TokenType.NUMBER) 
				{
					if (sToken.Length > 9) 
					{

						// 2147483647 is the biggest int value, so more than
						// 9 digits are better returend as a long
						iType = TokenType.LONG;

						return long.Parse(sToken);
					}

					return int.Parse(sToken);
				} 
				else if (iType == TokenType.FLOAT) 
				{
					return Double.Parse(sToken);
				}

				return sToken;
			}
		}

		public int Position
		{
			get
			{
				return iIndex;
			}
		}

		public string GetPart(int begin, int end) 
		{
			return sCommand.Substring(begin, (end-begin));
		}

		private void GetToken() 
		{
			if (bWait) 
			{
				bWait = false;

				return;
			}

			while (iIndex < iLength && Char.IsWhiteSpace(cCommand[iIndex])) 
			{
				iIndex++;
			}

			sToken = "";

			if (iIndex >= iLength) 
			{
				iType = 0;

				return;
			}

			bool      point = false, digit = false, exp = false, afterexp = false;
			bool      end = false;
			char	  c = cCommand[iIndex];
			char	  cfirst = '0';
			StringBuilder name = new StringBuilder();

            if (Char.IsLetter(c)) {
	            iType = TokenType.NAME;
			}
	        else if ("(),*=;+%".IndexOf(c) >= 0) 
			{
				iType = TokenType.SPECIAL;
				iIndex++;
				sToken = "" + c;

				return;
			} 
			else if (Char.IsDigit(c)) 
			{
				iType = TokenType.NUMBER;
				digit = true;
			}
			else if ("@".IndexOf(c) >= 0) 
			{
				cfirst = c;
				iType = TokenType.VARIABLE;
			} 
			else if ("!<>|/-".IndexOf(c) >= 0) 
			{
				cfirst = c;
				iType = TokenType.SPECIAL;
			} 
			else if (c == '\"') 
			{
				iType = TokenType.QUOTED_IDENTIFIER;
			} 
			else if (c == '\'') 
			{
				iType = TokenType.STRING;

				name.Append('\'');
			} 
			else if (c == '.') 
			{
				iType = TokenType.FLOAT;
				point = true;
			} 
			else 
			{
				throw Trace.Error(Trace.UnexpectedToken, "" + c);
			}

			int start = iIndex++;

			while (true) 
			{
				if (iIndex >= iLength) 
				{
					c = ' ';
					end = true;

					Trace.Check(iType != TokenType.STRING && iType != TokenType.QUOTED_IDENTIFIER,
						Trace.UNEXPECTED_END_OF_COMMAND);
				} 
				else 
				{
					c = cCommand[iIndex];
				}

				switch (iType) 
				{
					case TokenType.NAME:
                        if (Char.IsLetter(c) || Char.IsDigit(c) || c.Equals('_')) {
	                        break;
			            }

						sToken = sCommand.Substring(start, (iIndex - start)).ToUpper();

						if (c == '.') 
						{
							sLongNameFirst = sToken;
							iIndex++;

							GetToken();	       // todo: eliminate recursion

							sLongNameLast = sToken;
							iType = TokenType.LONG_NAME;
							sToken = sLongNameFirst + "." + sLongNameLast;
						}

						return;

					case TokenType.QUOTED_IDENTIFIER:
						if (c == '\"') 
						{
							iIndex++;

							if (iIndex >= iLength) 
							{
								sToken = name.ToString();

								return;
							}

							c = cCommand[iIndex];

							if (c == '.') 
							{
								sLongNameFirst = name.ToString();
								iIndex++;

								GetToken();    // todo: eliminate recursion

								sLongNameLast = sToken;
								iType = TokenType.LONG_NAME;
								sToken = sLongNameFirst + "." + sLongNameLast;

								return;
							}

							if (c != '\"') 
							{
								sToken = name.ToString();

								return;
							}
						}

						name.Append(c);

						break;

					case TokenType.VARIABLE:
						if ( char.IsWhiteSpace(c) || 
							char.IsPunctuation(c) || 
							#if !POCKETPC
							char.IsSeparator(c) ||
							#endif
							iIndex >= iLength ) 
						{
							sToken = name.ToString();

							return;
						}
						name.Append(c);

						break;
					case TokenType.STRING:
						if (c == '\'') 
						{
							iIndex++;

							if (iIndex >= iLength || cCommand[iIndex] != '\'') 
							{
								sToken = name.ToString();

								return;
							}
						}

						name.Append(c);

						break;

					case TokenType.REMARK:
						if (end) 
						{

							// unfinished remark
							// maybe print error here
							iType = 0;

							return;
						} 
						else if (c == '*') 
						{
							iIndex++;

							if (iIndex < iLength && cCommand[iIndex] == '/') 
							{

								// using recursion here
								iIndex++;

								GetToken();

								return;
							}
						}

						break;

					case TokenType.REMARK_LINE:
						if (end) 
						{
							iType = 0;

							return;
						} 
						else if (c == '\r' || c == '\n') 
						{

							// using recursion here
							GetToken();

							return;
						}

						break;

					case TokenType.SPECIAL:
						if (c == '/' && cfirst == '/') 
						{
							iType = TokenType.REMARK_LINE;

							break;
						} 
						else if (c == '-' && cfirst == '-') 
						{
							iType = TokenType.REMARK_LINE;

							break;
						} 
						else if (c == '*' && cfirst == '/') 
						{
							iType = TokenType.REMARK;

							break;
						} 
						else if (">=|".IndexOf(c) >= 0) 
						{
							break;
						}

						sToken = sCommand.Substring(start, (iIndex - start));

						return;

					case TokenType.FLOAT:
					case TokenType.NUMBER:
						if (Char.IsDigit(c)) 
						{
							digit = true;
						} 
						else if (c == '.') 
						{
							iType = TokenType.FLOAT;

							if (point) 
							{
								throw Trace.Error(Trace.UnexpectedToken, ".");
							}

							point = true;
						} 
						else if (c == 'E' || c == 'e') 
						{
							if (exp) 
							{
								throw Trace.Error(Trace.UnexpectedToken, "E");
							}

							afterexp = true;    // first character after exp may be + or -
							point = true;
							exp = true;
						} 
						else if (c == '-' && afterexp) 
						{
							afterexp = false;
						} 
						else if (c == '+' && afterexp) 
						{
							afterexp = false;
						} 
						else 
						{
							afterexp = false;

							if (!digit) 
							{
								if (point && start == iIndex - 1) 
								{
									sToken = ".";
									iType = TokenType.SPECIAL;

									return;
								}

								throw Trace.Error(Trace.UnexpectedToken, "" + c);
							}

							sToken = sCommand.Substring(start, (iIndex - start));

							return;
						}
						break;
				}

				iIndex++;
			}
		}
	}
}
