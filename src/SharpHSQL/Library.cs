#region Usings
using System;
using System.Collections;
using System.Text;
using System.Data.SqlTypes;
using System.Threading;
using System.Globalization;
#endregion

#region License
/*
 * Library.cs
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
	/// Internal SharpHsql functions library.
	/// </summary>
	/// <remarks>version 1.0.0.1</remarks>
	sealed class Library 
	{
		static String prefix       = "SharpHsql.Library.";
		static int    prefixLength = prefix.Length;

		static Hashtable _functions;

		#region Internal Functions Declaration

		static Library()
		{
			_functions = new Hashtable();

			_functions.Add("Abs", _abs);
			_functions.Add("Ascii", _ascii);
			_functions.Add("Bitand", _bitand);
			_functions.Add("Bitlength", _bitLength);
			_functions.Add("Bitor", _bitor);
			_functions.Add("Character", _character);
			_functions.Add("Concat", _concat);
			_functions.Add("Cot", _cot);
			_functions.Add("Curdate", _curdate);
			_functions.Add("Curtime", _curtime);
			_functions.Add("Database", _database);
			_functions.Add("Datediff", _datediff);
			_functions.Add("Dayname", _dayname);
			_functions.Add("Day", _day);
			_functions.Add("Dayofmonth", _dayofmonth);
			_functions.Add("Dayofweek", _dayofweek);
			_functions.Add("Dayofyear", _dayofyear);
			_functions.Add("Difference", _difference);
			_functions.Add("GetAutoCommit", _getAutoCommit);
			_functions.Add("GetDatabaseMajorVersion", _getDatabaseMajorVersion);
			_functions.Add("GetDatabaseMinorVersion", _getDatabaseMinorVersion);
			_functions.Add("GetDatabaseProductName", _getDatabaseProductName);
			_functions.Add("GetDatabaseProductVersion", _getDatabaseProductVersion);
			_functions.Add("HexToRaw", _hexToRaw);
			_functions.Add("Hour", _hour);
			_functions.Add("Identity", _identity);
			_functions.Add("Insert", _insert);
			_functions.Add("IsReadOnlyConnection", _isReadOnlyConnection);
			_functions.Add("IsReadOnlyDatabase", _isReadOnlyDatabase);
			_functions.Add("IsReadOnlyDatabaseFiles", _isReadOnlyDatabaseFiles);
			_functions.Add("Lcase", _lcase);
			_functions.Add("Left", _left);
			_functions.Add("Length", _length);
			_functions.Add("Locate", _locate);
			_functions.Add("Log10", _log10);
			_functions.Add("Ltrim", _ltrim);
			_functions.Add("Minute", _minute);
			_functions.Add("Mod", _mod);
			_functions.Add("Month", _month);
			_functions.Add("Monthname", _monthname);
			_functions.Add("Now", _now);
			_functions.Add("OctetLength", _octetLength);
			_functions.Add("PI", _pi);
			_functions.Add("Position", _position);
			_functions.Add("Quarter", _quarter);
			_functions.Add("Rand", _rand);
			_functions.Add("RawToHex", _rawToHex);
			_functions.Add("Repeat", _repeat);
			_functions.Add("Replace", _replace);
			_functions.Add("Right", _right);
			_functions.Add("Round", _round);
			_functions.Add("RoundMagic", _roundMagic);
			_functions.Add("Rtrim", _rtrim);
			_functions.Add("Second", _second);
			_functions.Add("Sign", _sign);
			_functions.Add("Soundex", _soundex);
			_functions.Add("Space", _space);
			_functions.Add("Substring", _substring);
			_functions.Add("Trim", _trim);
			_functions.Add("Truncate", _truncate);
			_functions.Add("Ucase", _ucase);
			_functions.Add("User", _user);
			_functions.Add("Week", _week);
			_functions.Add("Year", _year);
			_functions.Add("Acos", _acos);
			_functions.Add("Asin", _asin);
			_functions.Add("Atan", _atan);
			_functions.Add("Atan2", _atan2);
			_functions.Add("Ceiling", _ceiling);
			_functions.Add("Cos", _cos);
			_functions.Add("Degrees", _degrees);
			_functions.Add("Exp", _exp);
			_functions.Add("Floor", _floor);
			_functions.Add("Log", _log);
			_functions.Add("Pow", _pow);
			_functions.Add("Radians", _radians);
			_functions.Add("Sin", _sin);
			_functions.Add("Sqrt", _sqrt);
			_functions.Add("Tan", _tan);
			_functions.Add("Exists", _exists);

		}

		static string[] sNumeric = 
		{
			"ABS", "SharpHsql.Library.Abs", 
			"ACOS", "SharpHsql.Library.Acos", 
			"ASIN", "SharpHsql.Library.Asin", 
			"ATAN", "SharpHsql.Library.Atan", 
			"ATAN2", "SharpHsql.Library.Atan2", 
			"CEILING", "SharpHsql.Library.Ceiling", 
			"COS", "SharpHsql.Library.Cos", 
			"COT", "SharpHsql.Library.Cot", 
			"DEGREES", "SharpHsql.Library.Degrees", 
			"EXP", "SharpHsql.Library.Exp", 
			"FLOOR", "SharpHsql.Library.Floor", 
			"LOG", "SharpHsql.Library.Log", "LOG10",
			"SharpHsql.Library.Log10", 
			"MOD", "SharpHsql.Library.Mod", "PI",
			"SharpHsql.Library.PI", 
			"POWER", "SharpHsql.Library.Pow", 
			"RADIANS", "SharpHsql.Library.Radians", 
			"RAND", "SharpHsql.Library.Rand", 
			"ROUND", "SharpHsql.Library.Round", 
			"SIGN", "SharpHsql.Library.Sign", 
			"SIN", "SharpHsql.Library.Sin", 
			"SQRT", "SharpHsql.Library.Sqrt", 
			"TAN", "SharpHsql.Library.Tan", 
			"TRUNCATE", "SharpHsql.Library.Truncate",
			"BITAND", "SharpHsql.Library.Bitand", 
			"BITOR", "SharpHsql.Library.Bitor", 
			"ROUNDMAGIC", "SharpHsql.Library.RoundMagic",
		};
		static string[] sstring = 
		{
			"ASCII", "SharpHsql.Library.Ascii", "CHAR",
			"SharpHsql.Library.Character", "CONCAT", "SharpHsql.Library.Concat",
			"DIFFERENCE", "SharpHsql.Library.Difference", "INSERT",
			"SharpHsql.Library.Insert", "LCASE", "SharpHsql.Library.Lcase", "LEFT",
			"SharpHsql.Library.Left", "LENGTH", "SharpHsql.Library.Length",
			"LOCATE", "SharpHsql.Library.Locate", "LTRIM",
			"SharpHsql.Library.Ltrim", "REPEAT", "SharpHsql.Library.Repeat",
			"REPLACE", "SharpHsql.Library.Replace", "RIGHT",
			"SharpHsql.Library.Right", "RTRIM", "SharpHsql.Library.Rtrim",
			"SOUNDEX", "SharpHsql.Library.Soundex", "SPACE",
			"SharpHsql.Library.Space", "SUBSTRING", "SharpHsql.Library.Substring",
			"UCASE", "SharpHsql.Library.Ucase", "LOWER", "SharpHsql.Library.Lcase",
			"UPPER", "SharpHsql.Library.Ucase"
		};
		static string[] sTimeDate = 
		{
			"CURDATE", "SharpHsql.Library.Curdate", "CURTIME",
			"SharpHsql.Library.Curtime", "DAYNAME", "SharpHsql.Library.Dayname",
			"DAYOFMONTH", "SharpHsql.Library.Dayofmonth", "DAYOFWEEK",
			"SharpHsql.Library.Dayofweek", "DAYOFYEAR",
			"SharpHsql.Library.Dayofyear", "HOUR", "SharpHsql.Library.Hour",
			"MINUTE", "SharpHsql.Library.Minute", "MONTH",
			"SharpHsql.Library.Month", "MONTHNAME", "SharpHsql.Library.Monthname",
			"NOW", "SharpHsql.Library.Now", "QUARTER", "SharpHsql.Library.Quarter",
			"SECOND", "SharpHsql.Library.Second", "WEEK", "SharpHsql.Library.Week",
			"YEAR", "SharpHsql.Library.Year",
		};
		static string[] sSystem = 
		{
			"DATABASE", "SharpHsql.Library.Database", "USER",
			"SharpHsql.Library.User", "IDENTITY", "SharpHsql.Library.Identity",
			"EXISTS", "SharpHsql.Library.Exists"
		};

		const int _abs                       = 0;
		const int _ascii                     = 1;
		const int _bitand                    = 2;
		const int _bitLength                 = 3;
		const int _bitor                     = 4;
		const int _character                 = 5;
		const int _concat                    = 6;
		const int _cot                       = 7;
		const int _curdate                   = 8;
		const int _curtime                   = 9;
		const int _database                  = 10;
		const int _day                       = 11;
		const int _dayname                   = 12;
		const int _dayofmonth                = 13;
		const int _dayofweek                 = 14;
		const int _dayofyear                 = 15;
		const int _difference                = 16;
		const int _getAutoCommit             = 17;
		const int _getDatabaseMajorVersion   = 18;
		const int _getDatabaseMinorVersion   = 19;
		const int _getDatabaseProductName    = 20;
		const int _getDatabaseProductVersion = 21;
		const int _hexToRaw                  = 22;
		const int _hour                      = 23;
		const int _identity                  = 24;
		const int _insert                    = 25;
		const int _isReadOnlyConnection      = 26;
		const int _isReadOnlyDatabase        = 27;
		const int _isReadOnlyDatabaseFiles   = 28;
		const int _lcase                     = 29;
		const int _left                      = 30;
		const int _length                    = 31;
		const int _locate                    = 32;
		const int _log10                     = 33;
		const int _ltrim                     = 34;
		const int _minute                    = 35;
		const int _mod                       = 36;
		const int _month                     = 37;
		const int _monthname                 = 38;
		const int _now                       = 39;
		const int _octetLength               = 40;
		const int _pi                        = 41;
		const int _position                  = 42;
		const int _quarter                   = 43;
		const int _rand                      = 44;
		const int _rawToHex                  = 45;
		const int _repeat                    = 46;
		const int _replace                   = 47;
		const int _right                     = 48;
		const int _round                     = 49;
		const int _roundMagic                = 50;
		const int _rtrim                     = 51;
		const int _second                    = 52;
		const int _sign                      = 53;
		const int _soundex                   = 54;
		const int _space                     = 55;
		const int _substring                 = 56;
		const int _trim                      = 57;
		const int _truncate                  = 58;
		const int _ucase                     = 59;
		const int _user                      = 60;
		const int _week                      = 61;
		const int _year                      = 62;
		const int _datediff                  = 63;
		const int _acos						 = 64;
		const int _asin                      = 65;
		const int _atan                      = 66;
		const int _atan2                     = 67;
		const int _ceiling                   = 68;
		const int _cos                       = 69;
		const int _degrees                   = 70;
		const int _exp                       = 71;
		const int _floor                     = 72;
		const int _log                       = 73;
		const int _pow                       = 74;
		const int _radians                   = 75;
		const int _sin                       = 76;
		const int _sqrt                      = 77;
		const int _tan                       = 78;
		const int _exists                    = 79;

		#endregion

		private static Channel _channel;

		public static Object Invoke(int fID, Channel channel, Object[] parameters) 
		{
			_channel = channel;

			try {
				switch (fID) {

					case _abs : {
						return Abs(( Convert.ToDouble(parameters[0])));
					}
					case _ascii : {
						return Ascii( Convert.ToString( parameters[0] ));
					}
					case _bitand : {
						return Bitand(( Convert.ToInt32( parameters[0] )),
								( Convert.ToInt32( parameters[1] )));
					}
					case _bitor : {
						return Bitor(( Convert.ToInt32( parameters[0] )),
								( Convert.ToInt32( parameters[1] )));
					}
					case _character : {
						return Character((  Convert.ToInt32( parameters[0] )));
					}
					case _concat : {
						return Concat( Convert.ToString( parameters[0] ), 
							Convert.ToString( parameters[1] ));
					}
					case _cot : {
						return Cot(( Convert.ToDouble( parameters[0] )));
					}
					case _curdate : {
						return Curdate();
					}
					case _curtime : {
						return Curtime();
					}
					case _database : {
						return Database();
					}
					case _exists : 
					{
						return Exists( Convert.ToString(parameters[0]) );
					}
					case _datediff : {
						return Datediff( Convert.ToString( parameters[0] ),
										Convert.ToDateTime( parameters[1] ),
										Convert.ToDateTime( parameters[2] ));
					}
					case _dayname : {
						return Dayname( Convert.ToDateTime( parameters[0] ));
					}
					case _dayofmonth :
					case _day : {
						return Dayofmonth( Convert.ToDateTime( parameters[0] ));
					}
					case _dayofweek : {
						return Dayofweek( Convert.ToDateTime( parameters[0] ));
					}
					case _dayofyear : {
						return Dayofyear( Convert.ToDateTime( parameters[0] ));
					}
					case _difference : {
						return Difference( Convert.ToString( parameters[0] ),
											Convert.ToString( parameters[1] ));
					}
					case _getAutoCommit : {
						return null;
					}
					case _hour : {
						return Hour( Convert.ToDateTime( parameters[0] ));
					}
					case _identity : {
						return Identity() ;
					}
					case _insert : {
						return Insert( Convert.ToString( parameters[0] ),
									Convert.ToInt32( parameters[1] ),
									Convert.ToInt32( parameters[2] ),
									Convert.ToString( parameters[3]) );
					}
					case _isReadOnlyConnection : {
						return _channel.IsReadOnly;
					}
					case _isReadOnlyDatabase : {
						return null;
					}
					case _lcase : {
						return Lcase( Convert.ToString( parameters[0] ));
					}
					case _left : {
						return Left( Convert.ToString( parameters[0] ),
									( Convert.ToInt32( parameters[1] )));
					}
					case _length : {
						return Length( Convert.ToString( parameters[0] ));
					}
					case _locate : {
						return Locate( Convert.ToString( parameters[0] ),
										Convert.ToString( parameters[1] ),
										Convert.ToInt32( parameters[2]) );
					}
					case _log10 : {
						return Log10( Convert.ToDouble( parameters[0] ) );
					}
					case _ltrim : {
						return Ltrim(Convert.ToString( parameters[0] ));
					}
					case _minute : {
						return Minute( Convert.ToDateTime( parameters[0] ));
					}
					case _mod : {
						return Mod( Convert.ToInt32( parameters[0] ), Convert.ToInt32( parameters[1] ));
					}
					case _month : {
						return Month( Convert.ToDateTime( parameters[0] ));
					}
					case _monthname : {
						return Monthname( Convert.ToDateTime( parameters[0] ));
					}
					case _now : {
						return Now();
					}
					case _pi : {
						return PI();
					}
					case _quarter : {
						return Quarter(Convert.ToDateTime( parameters[0] ));
					}
					case _rand : {
						return Rand(Convert.ToInt32( parameters[0] ));
					}
					case _repeat : {
						return Repeat(Convert.ToString( parameters[0] ), Convert.ToInt32( parameters[1] ));
					}
					case _replace : {
						return Replace(Convert.ToString( parameters[0] ), Convert.ToString( parameters[1] ),
									Convert.ToString( parameters[2] ));
					}
					case _right : {
						return Right(Convert.ToString( parameters[0] ),
									(Convert.ToInt32( parameters[1] )));
					}
					case _round : {
						return Round((Convert.ToDouble( parameters[0] )),
								(Convert.ToInt32( parameters[1] )));
					}
					case _roundMagic : {
						return RoundMagic((Convert.ToDouble( parameters[0] )));
					}
					case _rtrim : {
						return Rtrim(Convert.ToString( parameters[0] ));
					}
					case _second : {
						return Second(Convert.ToDateTime( parameters[0] ));
					}
					case _sign : {
						return Sign((Convert.ToDouble( parameters[0] )));
					}
					case _soundex : {
						return Soundex(Convert.ToString( parameters[0] ));
					}
					case _space : {
						return Space((Convert.ToInt32( parameters[0] )));
					}
					case _substring : {
						return Substring(Convert.ToString( parameters[0] ),
										Convert.ToInt32( parameters[1] ),
										Convert.ToInt32( parameters[2] ));
					}
					case _trim : {
						return Trim(Convert.ToString( parameters[0] ), 
									Convert.ToString( parameters[1] ),
									Convert.ToBoolean( parameters[2] ),
									Convert.ToBoolean( parameters[3] ));
					}
					case _truncate : {
						return Truncate( Convert.ToDouble( parameters[0] ),
								Convert.ToInt32( parameters[1] ));
					}
					case _ucase : {
						return Ucase(Convert.ToString( parameters[0] ));
					}
					case _user : {
						return User();
					}
					case _week : {
						return Week( Convert.ToDateTime( parameters[0] ));
					}
					case _year : {
						return Year( Convert.ToDateTime( parameters[0] ));
					}
					case _isReadOnlyDatabaseFiles : {
						return null;
					}
					case _acos : 
					{
						return Acos(Convert.ToDouble( parameters[0] ));
					}
					case _asin : 
					{
						return Asin(Convert.ToDouble( parameters[0] ));
					}
					case _atan : 
					{
						return Atan(Convert.ToDouble( parameters[0] ));
					}
					case _atan2 : 
					{
						return Atan2(Convert.ToDouble( parameters[0] ), 
							Convert.ToDouble( parameters[1] ));
					}
					case _ceiling : 
					{
						return Ceiling(Convert.ToDouble( parameters[0] ));
					}
					case _cos : 
					{
						return Cos(Convert.ToDouble( parameters[0] ));
					}
					case _degrees : 
					{
						return Degrees(Convert.ToDouble( parameters[0] ));
					}
					case _exp : 
					{
						return Exp(Convert.ToDouble( parameters[0] ));
					}
					case _floor : 
					{
						return Floor(Convert.ToDouble( parameters[0] ));
					}
					case _log : 
					{
						return Log(Convert.ToDouble( parameters[0] ));
					}
					case _pow : 
					{
						return Pow(Convert.ToDouble( parameters[0] ),
							Convert.ToDouble( parameters[1] ));
					}
					case _radians : 
					{
						return Radians(Convert.ToDouble( parameters[0] ));
					}
					case _sin : 
					{
						return Sin(Convert.ToDouble( parameters[0] ));
					}
					case _sqrt : 
					{
						return Sqrt(Convert.ToDouble( parameters[0] ));
					}
					case _tan : 
					{
						return Tan(Convert.ToDouble( parameters[0] ));
					}

					default : 
					{
						return null;
					}
				}
			} 
			catch (Exception e) 
			{
				throw Trace.Error(Trace.FUNCTION_NOT_SUPPORTED, e.Message);
			}
		}

		public static int FunctionID(Hashtable h, String fname) 
		{
			return fname.StartsWith(prefix)
				? (int)_functions[fname.Substring(prefixLength)]
				: -1;
		}

		public static void Register(Hashtable h) 
		{
			Register(h, sNumeric);
			Register(h, sstring);
			Register(h, sTimeDate);
			Register(h, sSystem);
		}

		private static void Register(Hashtable h, string[] s) 
		{
			for (int i = 0; i < s.Length; i += 2) 
			{
				h.Add(s[i], s[i + 1]);
			}
		}

		static Random rRandom = new Random(DateTime.Now.Millisecond);

		// NUMERIC
		public static double Acos(double d) 
		{
			return Math.Acos(d);
		}

		public static double Asin(double d) 
		{
			return Math.Asin(d);
		}

		public static double Atan(double d) 
		{
			return Math.Atan(d);
		}

		public static double Atan2(double y, double x) 
		{
			return Math.Atan2(y,x);
		}

		public static double Ceiling(double d) 
		{
			return Math.Ceiling(d);
		}

		public static double Cos(double d) 
		{
			return Math.Cos(d);
		}

		public static double Degrees(double d) 
		{
			return (180 / Math.PI) * d;
		}

		public static double Exp(double d) 
		{
			return Math.Exp(d);
		}

		public static double Floor(double d) 
		{
			return Math.Floor(d);
		}

		public static double Log(double d) 
		{
			return Math.Log(d);
		}

		public static double Pow(double y, double x) 
		{
			return Math.Pow(x,y);
		}

		public static double Radians(double d) 
		{
			return (Math.PI / 180) * d; 
		}

		public static double Sin(double d) 
		{
			return Math.Sin(d);
		}

		public static double Sqrt(double d) 
		{
			return Math.Sqrt(d);
		}

		public static double Tan(double d) 
		{
			return Math.Tan(d);
		}

		/// <summary>
		/// Returns the absolute value of the given double value.
		/// </summary>
		/// <param name="value">The number for which to determine the absolute value.</param>
		/// <returns>The absolute value of d, as a double.</returns>
		public static double Abs(double value) 
		{
			return Math.Abs(value);
		}

		public static double Rand(int i) 
		{
			return rRandom.NextDouble();
		}

		// this magic number works for 100000000000000; but not for 0.1 and 0.01
		static double LOG10_FACTOR = 0.43429448190325183;

		public static double Log10(double x) 
		{
			return RoundMagic(Math.Log(x) * LOG10_FACTOR);
		}

		public static double RoundMagic(double d) 
		{

			// this function rounds numbers in a good way but slow:
			// - special handling for numbers around 0
			// - only numbers <= +/-1000000000000
			// - convert to a string
			// - check the last 4 characters:
			// '000x' becomes '0000'
			// '999x' becomes '999999' (this is rounded automatically)
			if (d < 0.0000000000001 && d > -0.0000000000001) 
			{
				return 0.0;
			}

			if ((d > 1000000000000) || (d < -1000000000000) )
			{
				return d;
			}

			string s = d.ToString();

			int len = s.Length;

			if (len < 16) 
			{
				return d;
			}

			char cx = Convert.ToChar(s.Substring(len - 1,1));
			char c1 = Convert.ToChar(s.Substring(len - 2,1));
			char c2 = Convert.ToChar(s.Substring(len - 3,1));
			char c3 = Convert.ToChar(s.Substring(len - 4,1));

			if (c1 == '0' && c2 == '0' && c3 == '0' && cx != '.') 
			{
				s.Remove(len - 1,1);
				s.Insert(len -1,"0");
			} 
			else if (c1 == '9' && c2 == '9' && c3 == '9' && cx != '.') 
			{
				s.Remove(len - 1,1);
				s.Insert(len -1,"9");
				s += "9";
				s += "9";
			}

			return Double.Parse(s);
		}

		public static double Cot(double d) 
		{
			return (1 / Math.Tan(d));
		}

		public static int Mod(int i1, int i2) 
		{
			return i1 % i2;
		}

		public static double PI() 
		{
			return Math.PI;
		}

		public static double Round(double d, int p) 
		{
			double f = Math.Pow(10, p);

			return Math.Round(d * f) / f;
		}

		public static int Sign(double d) 
		{
			return d < 0 ? -1 : (d > 0 ? 1 : 0);
		}

		public static double Truncate(double d, int p) 
		{
			double f = Math.Pow(10, p);
			double g = d * f;

			return ((d < 0) ? Math.Ceiling(g) : Math.Floor(g)) / f;
		}

		public static int Bitand(int i, int j) 
		{
			return i & j;
		}

		public static int Bitor(int i, int j) 
		{
			return i | j;
		}

		// STRING
		public static int Ascii(string s) 
		{
			if (s == null || s.Length == 0) 
			{
				return 0;
			}

			return (int)Convert.ToChar(s);
		}

		public static string Character(int code) 
		{
			return "" + (char) code;
		}

		public static string Concat(string s1, string s2) 
		{
			if (s1 == null) 
			{
				if (s2 == null) 
				{
					return null;
				}

				return s2;
			}

			if (s2 == null) 
			{
				return s1;
			}

			return String.Concat(s1, s2);
		}

		public static int Difference(string s1, string s2) 
		{

			// todo: check if this is the standard algorithm
			if (s1 == null || s2 == null) 
			{
				return 0;
			}

			s1 = Soundex(s1);
			s2 = Soundex(s2);

			int len1 = s1.Length, len2 = s2.Length;
			int e = 0;

			for (int i = 0; i < 4; i++) 
			{
				if (i >= len1 || i >= len2 || s1.Substring(i,1) != s2.Substring(i,1)) 
				{
					e++;
				}
			}

			return e;
		}

		public static string Insert(string s1, int start, int length, string s2) 
		{
			if (s1 == null) 
			{
				return s2;
			}

			if (s2 == null) 
			{
				return s1;
			}

			int len1 = s1.Length;
			int len2 = s2.Length;

			start--;

			if (start < 0 || length <= 0 || len2 == 0 || start > len1) 
			{
				return s1;
			}

			if (start + length > len1) 
			{
				length = len1 - start;
			}

			return s1.Substring(0, start) + s2 + s1.Substring(start + length);
		}

		/// <summary>
		/// Returns the character sequence s, with the leading,
		/// trailing or both the leading and trailing occurences of the first
		/// character of the character sequence trimstr removed.
		/// </summary>
		/// <param name="source">The string to trim.</param>
		/// <param name="trimstr">The character whose occurences will be removed.</param>
		/// <param name="leading">If true, remove leading occurences.</param>
		/// <param name="trailing">If true, remove trailing occurences.</param>
		/// <returns>Source, with the leading, trailing or both the leading and trailing
		/// occurences of the first character of trimstr removed.</returns>
		/// <remarks>
		/// This method is in support of the standard SQL String function TRIM.
		/// Ordinarily, the functionality of this method is accessed from SQL using
		/// the following syntax: 
		/// 
		/// <code>
		///      * &lt;trim function&gt; ::= TRIM &lt;left paren&gt; &lt;trim operands&gt; &lt;right paren&gt;
		///  &lt;trim operands&gt; ::= [ [ &lt;trim specification&gt; ] [ &lt;trim character&gt; ] FROM ] &lt;trim source&gt;
		///  &lt;trim source&gt; ::= &lt;character value expression&gt;
		///  &lt;trim specification&gt; ::= LEADING | TRAILING | BOTH
		///  &lt;trim character&gt; ::= &lt;character value expression&gt;
		///  
		///  </code>
		///  
		/// (since HSQLDB 1.7.2)
		/// </remarks>
		public static String Trim(String source, String trimstr, bool leading, bool trailing) 
		{

			if (source == null) 
			{
				return source;
			}

			if( leading )
				source = source.TrimStart();

			if( trailing )
				source = source.TrimEnd();

			return source;
		}

		public static string Lcase(string s) 
		{
			return s == null ? null : s.ToLower();
		}

		public static string Left(string s, int i) 
		{
			return s == null ? null
				: s.Substring(0,
				(i < 0 ? 0 : i < s.Length ? i : s.Length));
		}

		public static int Length(string s) 
		{
			return (s == null || s.Length < 1) ? 0 : s.Length;
		}

		public static int Locate(string search, string s, int start) 
		{
			if (s == null || search == null) 
			{
				return 0;
			}

			return s.IndexOf(search, start < 0 ? 0 : start) + 1;
		}

		public static string Ltrim(string s) 
		{
			return s.TrimStart(null);
		}

		public static string Repeat(string s, int i) 
		{
			return new string(Convert.ToChar(s), i);
		}

		public static string Replace(string s, string replace, string with) 
		{
			return s.Replace( replace, with);
		}

		public static string Right(string s, int i) 
		{
			if (s == null) 
			{
				return null;
			}

			i = s.Length - i;

			return s.Substring(i < 0 ? 0 : i < s.Length ? i : s.Length);
		}

		public static string Rtrim(string s) 
		{
			return s.TrimEnd(null);
		}

		public static string Soundex(string s) 
		{
			if (s == null) 
			{
				return s;
			}

			s = s.ToUpper();

			int  len = s.Length;
			char[] b = new char[4];
			
			b[0] = Convert.ToChar(s.Substring(0,1));

			int j = 1;

			for (int i = 1; i < len && j < 4; i++) 
			{
				char c = Convert.ToChar(s.Substring(i,1));

				if ("BFPV".IndexOf(c) != -1) 
				{
					b[j++] = '1';
				} 
				else if ("CGJKQSXZ".IndexOf(c) != -1) 
				{
					b[j++] = '2';
				} 
				else if (c == 'D' || c == 'T') 
				{
					b[j++] = '3';
				} 
				else if (c == 'L') 
				{
					b[j++] = '4';
				} 
				else if (c == 'M' || c == 'N') 
				{
					b[j++] = '5';
				} 
				else if (c == 'R') 
				{
					b[j++] = '6';
				}
			}

			return new string(b, 0, j);
		}

		public static string Space(int i) 
		{
			if (i < 0) 
			{
				return null;
			}

			char[] c = new char[i];

			while (i > 0) 
			{
				c[--i] = ' ';
			}

			return new string(c);
		}

		public static string Substring(string s, int start, int length) 
		{
			if (s == null) 
			{
				return null;
			}

			int len = s.Length;

			start--;
			start = start > len ? len : start;

			if (length == 0) 
			{
				return s.Substring(start);
			} 
			else 
			{
				int l = length;

				return s.Substring(start, start + l > len ? len : l);
			}
		}

		public static string Ucase(string s) 
		{
			return s.ToUpper();
		}

		// TIME AND DATE

		/// <summary>
		/// Returns the number of units elapsed between two dates.
		/// Contributed by Michael Landon
		/// </summary>
		/// <param name="datepart">Specifies the unit in which the interval is to be measured.</param>
		/// <param name="d1">The starting datetime value for the interval. This value is
		/// subtracted from d2 to return the number of date-parts between the two arguments.</param>
		/// <param name="d2">The ending datetime for the interval. d1 is subtracted
		/// from this value to return the number of date-parts between the two arguments.</param>
		/// <returns></returns>
		public static long Datediff(String datepart, DateTime d1, DateTime d2) 
		{
			datepart = datepart.Trim().ToLower();

			if ("yy" == datepart || "year" == datepart) 
			{
				return GetElapsed("year", d1, d2);
			} 
			else if ("mm" == datepart || "month" == datepart) 
			{
				return GetElapsed("month", d1, d2);
			} 
			else if ("dd" == datepart || "day" == datepart) 
			{
				return GetElapsed("day", d1, d2);
			} 
			else if ("hh" == datepart || "hour" == datepart) 
			{
				return GetElapsed("hour", d1, d2);
			} 
			else if ("mi" == datepart || "minute" == datepart) 
			{
				return GetElapsed("minute", d1, d2);
			} 
			else if ("ss" == datepart || "second" == datepart) 
			{
				return GetElapsed("second", d1, d2);
			} 
			else if ("ms" == datepart || "millisecond" == datepart) 
			{
				return GetElapsed("ms", d1, d2);
			} 
			else 
			{
				throw Trace.Error(Trace.ERROR_IN_FUNCTION);
			}
		}

		private static int GetElapsed(string part, DateTime d1, DateTime d2 ) 
		{
			TimeSpan elapsed = d1.Subtract( d2 );

			switch( part )
			{
				case "year":
					return new DateTime( elapsed.Ticks ).Year;
				case "month":
					return new DateTime( elapsed.Ticks ).Month;
				case "day":
					return new DateTime( elapsed.Ticks ).Day;
				case "hour":
					return new DateTime( elapsed.Ticks ).Hour;
				case "minute":
					return new DateTime( elapsed.Ticks ).Minute;
				case "second":
					return new DateTime( elapsed.Ticks ).Second;
				case "ms":
					return new DateTime( elapsed.Ticks ).Millisecond;
				default:
					throw Trace.Error(Trace.ERROR_IN_FUNCTION);
			}
		}

		public static DateTime Curdate() 
		{
			return DateTime.Now;
		}

		public static DateTime Curtime() 
		{
			return DateTime.Now;
		}

		public static string Dayname(SqlDateTime d) 
		{
			return d.ToString();
		}

		private static int GetDateTimePart(SqlDateTime d, string part) 
		{
			DateTime dt = d.Value;

			switch( part.Trim().ToLower() )
			{
				case "year":
				case "yy":
				case "yyyy":
					return dt.Year;
				case "quarter":
				case "q":
				case "qq":
					return (d.Value.Month / 3) + 1;
				case "month":
				case "m":
				case "mm":
					return dt.Month;
				case "dayofyear":
				case "dy":
				case "y":
					return dt.DayOfYear;
				case "day":
				case "dd":
				case "d":
					return dt.Day;
				case "week":
				case "wk":
				case "ww":
					// Gets the Calendar instance associated with a CultureInfo.
					#if !POCKETPC
					CultureInfo ci = Thread.CurrentThread.CurrentCulture;
					#else
					CultureInfo ci = CultureInfo.CurrentCulture;
					#endif
					Calendar cal = ci.Calendar;

					// Gets the DTFI properties required by GetWeekOfYear.
					CalendarWeekRule cwr = ci.DateTimeFormat.CalendarWeekRule;
					DayOfWeek dow = ci.DateTimeFormat.FirstDayOfWeek;

					return cal.GetWeekOfYear( dt, cwr, dow );

				case "weekday":
				case "dw":
					return (int)dt.DayOfWeek;
				case "hour":
				case "hh":
					return dt.Hour;
				case "minute":
				case "mi":
				case "n":
					return dt.Minute;
				case "second":
				case "ss":
				case "s":
					return dt.Second;
				case "millisecond":
				case "ms":
					return dt.Millisecond;
				default:
					return 0;
			}
		}

		public static int Dayofmonth(SqlDateTime d) 
		{
			return d.Value.Day;
		}

		public static int Dayofweek(SqlDateTime d) 
		{
			return (int)d.Value.DayOfWeek;
		}

		public static int Dayofyear(SqlDateTime d) 
		{
			return d.Value.DayOfYear;
		}

		public static int Hour(SqlDateTime t) 
		{
			return t.Value.Hour;
		}

		public static int Minute(SqlDateTime t) 
		{
			return t.Value.Minute;
		}

		public static int Month(SqlDateTime d) 
		{
			return d.Value.Month;
		}

		public static string Monthname(SqlDateTime d) 
		{
			return d.Value.ToString("MMMM",null);
		}

		public static DateTime Now() 
		{
			return DateTime.Now;
		}

		public static int Quarter(SqlDateTime d) 
		{
			return (d.Value.Month / 3) + 1;
		}

		public static int Second(SqlDateTime d) 
		{
			return d.Value.Second;
		}

		public static int Week(SqlDateTime d) 
		{
			return GetDateTimePart(d, "w");
		}

		public static int Year(SqlDateTime d) 
		{
			return d.Value.Year;
		}

		// SYSTEM
		public static string Database() 
		{
			return _channel.Database.Name;
		}

		public static bool Exists( string name ) 
		{
			Table t = _channel.Database.GetTable( name, _channel.Database.SysChannel );
			if( t == null )
				return false;
			else
				return true;
		}

		public static string User() 
		{
			//string    s = "SELECT Value FROM SYSTEM_CONNECTIONINFO WHERE KEY='USER'";
			//Result r = conn.execute(s);

			return _channel.UserName;
		}

		public static int Identity() 
		{
			return _channel.LastIdentity;
		}
	} 
}
