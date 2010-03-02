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
using System.Xml;

namespace fyiReporting.RDL
{
	///<summary>
	/// CategoryAxis definition and processing.
	///</summary>
	[Serializable]
	internal class CategoryAxis : ReportLink
	{
		Axis _Axis;		// Display properties for the category axis	
	
		internal CategoryAxis(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Axis = null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Axis":
						_Axis = new Axis(r, this, xNodeLoop);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown CategoryAxis element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}
		
		override internal void FinalPass()
		{
			if (_Axis != null)
				_Axis.FinalPass();
			return;
		}


		internal Axis Axis
		{
			get { return  _Axis; }
			set {  _Axis = value; }
		}
	}

}
