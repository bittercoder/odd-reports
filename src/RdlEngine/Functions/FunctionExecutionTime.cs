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
using System.IO;

using fyiReporting.RDL;


namespace fyiReporting.RDL
{
	/// <summary>
	/// DateTime Report started; actually the time that data is retrieved
	/// </summary>
	[Serializable]
	internal class FunctionExecutionTime : IExpr
	{
		/// <summary>
		/// DateTime report started
		/// </summary>
		public FunctionExecutionTime() 
		{
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.DateTime;
		}

		public bool IsConstant()
		{
			return false;
		}

		public IExpr ConstantOptimization()
		{	// not a constant expression
			return this;
		}

		// Evaluate is for interpretation  
		public object Evaluate(Report rpt, Row row)
		{
			return EvaluateDateTime(rpt, row);
		}
		
		public double EvaluateDouble(Report rpt, Row row)
		{	
			DateTime result = EvaluateDateTime(rpt, row);
			return Convert.ToDouble(result);
		}
		
		public decimal EvaluateDecimal(Report rpt, Row row)
		{
			DateTime result = EvaluateDateTime(rpt, row);

			return Convert.ToDecimal(result);
		}

        public int EvaluateInt32(Report rpt, Row row)
        {
            DateTime result = EvaluateDateTime(rpt, row);

            return Convert.ToInt32(result);
        }

		public string EvaluateString(Report rpt, Row row)
		{
			DateTime result = EvaluateDateTime(rpt, row);
			return result.ToString();
		}

		public DateTime EvaluateDateTime(Report rpt, Row row)
		{
			return rpt.ExecutionTime;
		}
		
		public bool EvaluateBoolean(Report rpt, Row row)
		{
			return false;
		}
	}
}
