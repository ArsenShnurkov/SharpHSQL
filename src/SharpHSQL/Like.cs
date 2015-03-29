#region Usings
using System;
#endregion

#region License
/*
 * Like.cs
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
	/// Like class.
	/// </summary>
	sealed class Like 
	{
		private char[]  cLike;
		private int[]   iType;
		private int     iLen;
		private bool	bIgnoreCase;

		public Like(string s, char escape, bool ignorecase) 
		{
			if (ignorecase) 
			{
				s = s.ToUpper();
			}

			Normalize(s, false, escape);

			bIgnoreCase = ignorecase;
		}

		public string GetStartsWith() 
		{
			string s = "";
			int    i = 0;

			for (; i < iLen && iType[i] == 0; i++) 
			{
				s = s + cLike[i];
			}

			if (i == 0) 
			{
				return null;
			}

			return s;
		}

		public bool Compare(object obj) 
		{
			if (obj == null) 
			{
				return iLen == 0;
			}

			string s = obj.ToString();

			if (bIgnoreCase) 
			{
				s = s.ToUpper();
			}

			return CompareAt(s, 0, 0, s.Length);
		}

		private bool CompareAt(string s, int i, int j, int jLen) 
		{
			for (; i < iLen; i++) 
			{
				switch (iType[i]) 
				{

					case 0:    // general character
						if (j >= jLen || cLike[i] != Convert.ToChar(s.Substring(j++,1))) 
						{
							return false;
						}

						break;

					case 1:    // underscore: do not test this character
						if (j++ >= jLen) 
						{
							return false;
						}

						break;

					case 2:    // percent: none or any character(s)
						if (++i >= iLen) 
						{
							return true;
						}

						while (j < jLen) 
						{
							if (cLike[i] == Convert.ToChar(s.Substring(j,1)) && CompareAt(s, i, j, jLen)) 
							{
								return true;
							}

							j++;
						}

						return false;
				}
			}

			if (j != jLen) 
			{
				return false;
			}

			return true;
		}

		private void Normalize(string s, bool processEscape, char escapeChar ) 
		{
			iLen = 0;

			if (s == null) 
			{
				return;
			}

			int l = s.Length;

			cLike = new char[l];
			iType = new int[l];

			bool bEscaping = false, bPercent = false;

			for (int i = 0; i < l; i++) 
			{
				char c = Convert.ToChar(s.Substring(i,1));

				if (bEscaping == false) 
				{
					if (processEscape && c == escapeChar) 
					{
						bEscaping = true;

						continue;
					} 
					else if (c == '_') 
					{
						iType[iLen] = 1;
					} 
					else if (c == '%') 
					{
						if (bPercent) 
						{
							continue;
						}

						bPercent = true;
						iType[iLen] = 2;
					} 
					else 
					{
						bPercent = false;
					}
				} 
				else 
				{
					bPercent = false;
					bEscaping = false;
				}

				cLike[iLen++] = c;
			}

			for (int i = 0; i < iLen - 1; i++) 
			{
				if (iType[i] == 2 && iType[i + 1] == 1) 
				{
					iType[i] = 1;
					iType[i + 1] = 2;
				}
			}
		}
	}
}
