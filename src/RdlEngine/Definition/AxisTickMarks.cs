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
	///<summary>
	/// AxisTickMarks definition and processing.
	///</summary>
	public enum AxisTickMarksEnum
	{
		None,
		Inside,
		Outside,
		Cross
	}

	public class AxisTickMarks
	{
        static public AxisTickMarksEnum GetStyle(string s)
        {
            return AxisTickMarks.GetStyle(s, null);
        }

		static internal AxisTickMarksEnum GetStyle(string s, ReportLog rl)
		{
			AxisTickMarksEnum rs;

			switch (s)
			{		
				case "None":
					rs = AxisTickMarksEnum.None;
					break;
				case "Inside":
					rs = AxisTickMarksEnum.Inside;
					break;
				case "Outside":
					rs = AxisTickMarksEnum.Outside;
					break;
				case "Cross":
					rs = AxisTickMarksEnum.Cross;
					break;
				default:		
                    if (rl != null)
					    rl.LogError(4, "Unknown Axis Tick Mark '" + s + "'.  None assumed.");
					rs = AxisTickMarksEnum.None;
					break;
			}
			return rs;
		}
	}

}
