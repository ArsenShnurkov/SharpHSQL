#region Usings
using System;
using System.Collections;
using System.Text;
using System.IO;
#endregion

#region License
/*
 * stringConverter.cs
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
	/// String helper class.
	/// </summary>
	sealed class StringConverter 
	{
		#if !POCKETPC
		/// <summary>
		/// Reference to the appropriate logger.
		/// </summary>
		static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(StringConverter));
		#endif

		private static char[]   HEXCHAR = 
		{
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
		};
		private static string HEXINDEX = "0123456789abcdef          ABCDEF";

		public static byte[] HexToByte(string source) 
		{
			int  l = source.Length / 2;
			byte[] data = new byte[l];
			int  j = 0;

			for (int i = 0; i < l; i++) 
			{
				char c = Convert.ToChar(source.Substring(j++,1));
				int  n, b;

				n = HEXINDEX.IndexOf(c);
				b = (n & 0xf) << 4;
				c = Convert.ToChar(source.Substring(j++,1));
				n = HEXINDEX.IndexOf(c);
				b += (n & 0xf);
				data[i] = (byte) b;
			}

			return data;
		}

		public static string ByteToHex(byte[] data) 
		{
			int	     len = data.Length;
			StringBuilder s = new StringBuilder();

			for (int i = 0; i < len; i++) 
			{
				int c = ((int) data[i]) & 0xff;

				s.Append(HEXCHAR[c >> 4 & 0xf]);
				s.Append(HEXCHAR[c & 0xf]);
			}

			return s.ToString();
		}

		static string UnicodeToHexstring(string source) 
		{
			MemoryStream stream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(stream);

			try 
			{
				writer.Write(source);
				writer.Close();
				stream.Close();
			} 
            #if !POCKETPC
            catch (Exception e)
            #else
			catch
            #endif
			{
				#if !POCKETPC
				if( Logger.IsErrorEnabled ) Logger.Error( "Unexpected error on unicodeToHexstring.", e );
				#endif
				return null;
			}

			return ByteToHex(stream.ToArray());
		}

		public static string HexstringToUnicode(string source) 
		{
			byte[]		     b = HexToByte(source);
			MemoryStream stream = new MemoryStream(b);
			BinaryReader writer = new BinaryReader(stream);

			try 
			{
				return writer.ReadString();
			}
#if !POCKETPC
            catch (Exception e)
#else
			catch
#endif
			{
				#if !POCKETPC
				if( Logger.IsErrorEnabled ) Logger.Error( "Unexpected error on hexstringToUnicode.", e );
				#endif
				return null;
			}
		}

		public static string UnicodeToAscii(string source) 
		{
			if (source == null || source.Equals("")) 
			{
				return source;
			}

			int	     len = source.Length;
			StringBuilder b = new StringBuilder();

			for (int i = 0; i < len; i++) 
			{
				char c = Convert.ToChar(source.Substring(i,1));

				if (c == '\\') 
				{
					if (i < len - 1 && Convert.ToChar(source.Substring(i + 1,1)) == 'u') 
					{
						b.Append(c);    // encode the \ as unicode, so 'u' is ignored
						b.Append("u005c");    // splited so the source code is not changed...
					} 
					else 
					{
						b.Append(c);
					}
				} 
				else if ((c >= 0x0020) && (c <= 0x007f)) 
				{
					b.Append(c);    // this is 99%
				} 
				else 
				{
					b.Append("\\u");
					b.Append(HEXCHAR[(c >> 12) & 0xf]);
					b.Append(HEXCHAR[(c >> 8) & 0xf]);
					b.Append(HEXCHAR[(c >> 4) & 0xf]);
					b.Append(HEXCHAR[c & 0xf]);
				}
			}

			return b.ToString();
		}

		public static string AsciiToUnicode(string source) 
		{
			if (source == null || source.IndexOf("\\u") == -1) 
			{
				return source;
			}

			int  len = source.Length;
			char[] b = new char[len];
			int  j = 0;

			for (int i = 0; i < len; i++) 
			{
				char c = Convert.ToChar(source.Substring(i,1));

				if (c != '\\' || i == len - 1) 
				{
					b[j++] = c;
				} 
				else 
				{
					c = Convert.ToChar(source.Substring(++i,1));

					if (c != 'u' || i == len - 1) 
					{
						b[j++] = '\\';
						b[j++] = c;
					} 
					else 
					{
						int k = (HEXINDEX.IndexOf(Convert.ToChar(source.Substring(++i,1))) & 0xf) << 12;

						k += (HEXINDEX.IndexOf(Convert.ToChar(source.Substring(++i,1))) & 0xf) << 8;
						k += (HEXINDEX.IndexOf(Convert.ToChar(source.Substring(++i,1))) & 0xf) << 4;
						k += (HEXINDEX.IndexOf(Convert.ToChar(source.Substring(++i,1))) & 0xf);
						b[j++] = (char) k;
					}
				}
			}

			return new string(b, 0, j);
		}

		public static string InputStreamTostring(BinaryReader reader)
		{
			StringWriter      write = new StringWriter();
			int		  blocksize = 8 * 1024;    // todo: is this a good value?
			char[]		  buffer = new char[blocksize];

			try 
			{
				while (true) 
				{
					int l = reader.Read(buffer, 0, blocksize);

					if (l == -1) 
					{
						break;
					}

					write.Write(buffer, 0, l);
				}

				write.Close();
				reader.Close();
			} 
			catch (IOException e) 
			{
				throw Trace.Error(Trace.INPUTSTREAM_ERROR, e.Message);
			}

			return write.ToString();
		}
	}
}
