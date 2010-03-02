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
using System.Collections;
using System.IO;
using System.Reflection;
using System.Globalization;

using fyiReporting.RDL;


namespace fyiReporting.RDL
{
	/// <summary>
	/// Global fields accessed dynamically. i.e. Globals(expr).
	/// </summary>
	[Serializable]
	internal class FunctionGlobalCollection : IExpr
	{
		private IDictionary _Globals;
		private IExpr _ArgExpr;

		/// <summary>
		/// obtain value of Field
		/// </summary>
		public FunctionGlobalCollection(IDictionary globals, IExpr arg) 
		{
			_Globals = globals;
			_ArgExpr = arg;
		}

		public virtual TypeCode GetTypeCode()
		{
			return TypeCode.Object;		// we don't know the typecode until we run the function
		}

		public virtual bool IsConstant()
		{
			return false;
		}

		public virtual IExpr ConstantOptimization()
		{	
			_ArgExpr = _ArgExpr.ConstantOptimization();

			if (_ArgExpr.IsConstant())
			{
				string o = _ArgExpr.EvaluateString(null, null);
				if (o == null)
					throw new Exception("Globals collection argument is null"); 
				switch (o.ToLower())
				{
					case "pagenumber":
						return new FunctionPageNumber();
					case "totalpages":
						return new FunctionTotalPages();
					case "executiontime":
						return new FunctionExecutionTime();
					case "reportfolder":
						return new FunctionReportFolder();
					case "reportname":
						return new FunctionReportName();
					default:
						throw new Exception(string.Format("Globals collection argument '{0}' is unknown.", o)); 
				}
			}

			return this;
		}

		// 
		public virtual object Evaluate(Report rpt, Row row)
		{
			if (rpt == null)
				return null;

			string g = _ArgExpr.EvaluateString(rpt, row);
			if (g == null)
				return null;

			switch (g.ToLower())
			{
				case "pagenumber":
					return rpt.PageNumber;
				case "totalpages":
					return rpt.TotalPages;
				case "executiontime":
					return rpt.ExecutionTime;
				case "reportfolder":
					return rpt.Folder;
				case "reportname":
					return rpt.Name;
				default:
					return null;
			}
		}
		
		public virtual double EvaluateDouble(Report rpt, Row row)
		{
			if (row == null)
				return Double.NaN;
			return Convert.ToDouble(Evaluate(rpt, row), NumberFormatInfo.InvariantInfo);
		}
		
		public virtual decimal EvaluateDecimal(Report rpt, Row row)
		{
			if (row == null)
				return decimal.MinValue;
			return Convert.ToDecimal(Evaluate(rpt, row), NumberFormatInfo.InvariantInfo);
		}

        public virtual int EvaluateInt32(Report rpt, Row row)
        {
            if (row == null)
                return int.MinValue;
            return Convert.ToInt32(Evaluate(rpt, row), NumberFormatInfo.InvariantInfo);
        }

        public virtual string EvaluateString(Report rpt, Row row)
		{
			if (row == null)
				return null;
			return Convert.ToString(Evaluate(rpt, row));
		}

		public virtual DateTime EvaluateDateTime(Report rpt, Row row)
		{
			if (row == null)
				return DateTime.MinValue;
			return Convert.ToDateTime(Evaluate(rpt, row));
		}

		public virtual bool EvaluateBoolean(Report rpt, Row row)
		{
			if (row == null)
				return false;
			return Convert.ToBoolean(Evaluate(rpt, row));
		}
	}
}
