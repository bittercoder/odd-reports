/* ====================================================================
   Copyright (C) 2004-2008  fyiReporting Software, LLC

   This file is part of the fyiReporting RDL project.
	
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.


   For additional information, email info@fyireporting.com or visit
   the website www.fyiReporting.com.
*/
using System;

namespace fyiReporting.RDL
{
	/// <summary>
	/// Token class that used by LangParser.
	/// </summary>
	internal class Token
	{
		internal string Value;
		internal int StartLine;
		internal int EndLine;
		internal int StartCol;
		internal int EndCol;
		internal TokenTypes Type;

		/// <summary>
		/// Initializes a new instance of the Token class.
		/// </summary>
		internal Token(string value, int startLine, int startCol, int endLine, int endCol, TokenTypes type)
		{
			Value = value;
			StartLine = startLine;
			EndLine = endLine;
			StartCol = startCol;
			EndCol = endCol;
			Type = type;
		}

		/// <summary>
		/// Initializes a new instance of the Token class.
		/// </summary>
		internal Token(string value, TokenTypes type)
			: this(value, 0, 0, 0, 0, type)
		{
			// use this
		}

		/// <summary>
		/// Initializes a new instance of the Token class.
		/// </summary>
		internal Token(TokenTypes type)
			: this(null, 0, 0, 0, 0, type)
		{
			// use this
		}

		/// <summary>
		/// Returns a string representation of the Token.
		/// </summary>
		public override string ToString()
		{
			return "<" + Type + "> " + Value;	
		}
	}
}
