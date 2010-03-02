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


using fyiReporting.RDL;


namespace fyiReporting.RDL
{
	/// <summary>
	/// And operator of form lhs &amp;&amp; rhs
	/// </summary>
	[Serializable]
	internal class FunctionAnd : FunctionBinary, IExpr
	{

		/// <summary>
		/// And two boolean expressions together of the form a &amp;&amp; b
		/// </summary>
		public FunctionAnd(IExpr lhs, IExpr rhs) 
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Boolean;
		}

		public IExpr ConstantOptimization()
		{
			_lhs = _lhs.ConstantOptimization();
			_rhs = _rhs.ConstantOptimization();
			bool bLeftConst = _lhs.IsConstant();
			bool bRightConst = _rhs.IsConstant();
			if (bLeftConst && bRightConst)
			{
				bool b = EvaluateBoolean(null, null);
				return new ConstantBoolean(b);
			}
			else if (bRightConst)
			{
				bool b = _rhs.EvaluateBoolean(null, null);
				if (b)
					return _lhs;
				else 
					return new ConstantBoolean(false);
			}
			else if (bLeftConst)
			{
				bool b = _lhs.EvaluateBoolean(null, null);
				if (b)
					return _rhs;
				else
					return new ConstantBoolean(false);
			}

			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public object Evaluate(Report rpt, Row row)
		{
			return EvaluateBoolean(rpt, row);
		}
		
		public double EvaluateDouble(Report rpt, Row row)
		{
			return Double.NaN;
		}
		
		public decimal EvaluateDecimal(Report rpt, Row row)
		{
			return decimal.MinValue;
		}

        public int EvaluateInt32(Report rpt, Row row)
        {
            return int.MinValue;
        }

		public string EvaluateString(Report rpt, Row row)
		{
			bool result = EvaluateBoolean(rpt, row);
			return result.ToString();
		}

		public DateTime EvaluateDateTime(Report rpt, Row row)
		{
			return DateTime.MinValue;
		}

		public bool EvaluateBoolean(Report rpt, Row row)
		{
			bool r = _lhs.EvaluateBoolean(rpt, row);
			if (!r)
				return false;
			return  _rhs.EvaluateBoolean(rpt, row);
		}
	}
}
