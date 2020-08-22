using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Sid.ScriptableTemplate
{
	public class Template : IHasProps
	{
		// Template script
		const string FORMAT_Props = "props"; // Properties of the template
		const string FORMAT_CountProps = "countProps";
		const string FORMAT_CountVar = "countVar";
		const string FORMAT_RenderVar = "renderVar";
		const string FORMAT_EndIf = "endif";
		const string FORMAT_For = "for";
		const string FORMAT_ForEnd = "endFor";
		const string FORMAT_If = "if";
		const string FORMAT_Assign = "assign";
		const string FORMAT_Add = "add";
		const string FORMAT_Subtract = "subtract";
		const string FORMAT_Var = "var";
		const string FORMAT_LoopVar = "i";
		const string FORMAT_FilterInput = "input";
		const string FORMAT_Function = "func";
		const string FORMAT_FunctionEquals = "Equals";
		const string FORMAT_FunctionGreater = "Greater";
		const string FORMAT_FunctionIsNull = "IsNull";
		const string FORMAT_FunctionIsBlank = "IsBlank";
		const string FORMAT_FuncCommaSeperatedList = "CommaSeperatedList";
		const string FORMAT_FunctionIsTrue = "IsTrue";
		const string FORMAT_FunctionIsFalse = "IsFalse";
		//

		public string TemplateDirectory
		{
			get;
			set;
		}

		public static void ThrowTemplateException(string message,int pos=0)
		{
			//TODO template filename??
			string fullError = string.Format("There is an issue in the '{0}' template at pos {2}: {1}.",
			 "blah", //TemplateFilename,
			 message,
			 pos);
			throw new Exception(fullError);
		}

		public virtual int Count(string param)
		{
			ThrowTemplateException(string.Format("Unknown template count format: {0}",param));
			return 0;
		}

		public virtual object FormatProps(
		 string property,
		 int? arrayIndex,
		 bool throwIfNotExist = true,
		 Optional<bool> exists = null)
		{
			ThrowTemplateException(string.Format("Unknown template props format: {0}", property));
			return "";
		}

		public string FormatTemplate(
		 string templateName)
		{
			Dictionary<string, object> paramDictionary = new Dictionary<string, object>();
			return FormatTemplate(templateName, paramDictionary);
		}

		public string FormatTemplate(
		 string templateName,
		 Dictionary<string, object> paramDictionary)
		{
			// Open the filter template
			//TODO cache the template??
			StreamReader templateFile = File.OpenText(TemplateDirectory + "\\" + templateName);
			string templateBuffer = templateFile.ReadToEnd();

			// Format the template
			StringBuilder filterRender = new StringBuilder();
			FormatRecursive(filterRender, templateBuffer, 0, paramDictionary);

			string rv = filterRender.ToString();

			return rv;
		}

		private void FormatRecursive(
		 StringBuilder output,
		 string str,
		 int pos,
		 Dictionary<string, object> paramDictionary)
		{
			// Iterate the string character by character
			for (int i = pos; i < str.Length;)
			{
				char chr = str[i];
				if (chr == '{')
				{
					if (str[i + 1] == '{')
					{
						// Formatting is escaped
						output.Append("{");
						i += 2;
					}
					else
					{
						// Formating
						int begin = i + 1;
						i = str.IndexOf('}', begin);
						if (i == -1)
						{
							ThrowTemplateException("missing }}.");
						}

						string formatText = str.Substring(begin, i - begin);
						// Skip past the }
						++i;

						i = DoFormatting(output, str, formatText, i, paramDictionary);
					}
				}
				else
				{
					output.Append(chr);
					++i;
				}
			}
		}

		// Returns the position to continue from
		private int DoFormatting(
		 StringBuilder output,
		 string str,
		 string formatText,
		 int posAfterFormat,
		 Dictionary<string, object> paramDictionary)
		{
			int rv;

			// Is there a : ?
			int colonPos = formatText.IndexOf(':');
			if (colonPos != -1)
			{
				string beforeColon = formatText.Substring(0, colonPos);

				string afterColon = formatText.Substring(colonPos + 1, formatText.Length - colonPos - 1);

				rv = DoFormattingParam(output, str, beforeColon, afterColon, posAfterFormat, paramDictionary);
			}
			else
			{
				rv = DoFormattingNoParam(output, formatText, posAfterFormat);
			}

			return rv;
		}

		// Return the position to continue searching from
		private int DoFormattingParam(
		 StringBuilder output,
		 string str,
		 string format,
		 string param,
		 int posAfterFormat,
		 Dictionary<string, object> variableDictionary)
		{
			int rv;
			if (string.Equals(format, FORMAT_If, StringComparison.CurrentCultureIgnoreCase))
			{
				// If condition
				rv = DoFormattingIf(str, param, posAfterFormat, variableDictionary);
			}
			else if (string.Equals(format, FORMAT_For, StringComparison.CurrentCultureIgnoreCase))
			{
				// For
				rv = DoFor(output, str, param, posAfterFormat, variableDictionary);
			}
			else if (string.Equals(format, FORMAT_Assign, StringComparison.CurrentCultureIgnoreCase))
			{
				// Assign
				rv = DoAssignment(output, str, param, posAfterFormat, variableDictionary);
			}
			else if (string.Equals(format, FORMAT_Add, StringComparison.CurrentCultureIgnoreCase))
			{
				// Add
				rv = DoAdd(output, str, param, posAfterFormat, variableDictionary);
			}
			else if (string.Equals(format, FORMAT_Subtract, StringComparison.CurrentCultureIgnoreCase))
			{
				// Subtract
				rv = DoSubtractAssignment(output, str, param, posAfterFormat, variableDictionary);
			}
			else
			{
				output.Append(FormatTemplateValue(format, param, variableDictionary));
				rv = posAfterFormat;
			}

			return rv;
		}

		private int DoFormattingNoParam(
		 StringBuilder output,
		 string formatText,
		 int posAfterFormat)
		{
			if (string.Equals(formatText, FORMAT_EndIf, StringComparison.CurrentCultureIgnoreCase))
			{
				// Nothing to do
			}
			else
			{
				ThrowTemplateException(string.Format("Unexpected template file format: {0}", formatText));
			}

			// Continue after the format
			return posAfterFormat;
		}

		private bool FunctionIsTrue(
		 string functionString,
		 int parenOpenPos,
		 Dictionary<string, object> paramDictionary)
		{
			bool rv;

			string[] funcParams;
			string funcName = ParseFunctionParameters(functionString, parenOpenPos, out funcParams);

			if (string.Equals(funcName, FORMAT_FunctionEquals, StringComparison.CurrentCultureIgnoreCase))
			{
				// Equals function
				rv = FunctionEquals(funcParams, paramDictionary);
			}
			else if (string.Equals(funcName, FORMAT_FunctionIsTrue, StringComparison.CurrentCultureIgnoreCase))
			{
				rv = IsTrue(funcParams, paramDictionary);
			}
			else if (string.Equals(funcName, FORMAT_FunctionIsFalse, StringComparison.CurrentCultureIgnoreCase))
			{
				rv = !IsTrue(funcParams, paramDictionary);
			}
			else if (string.Equals(funcName, FORMAT_FunctionGreater, StringComparison.CurrentCultureIgnoreCase))
			{
				// Greater function
				rv = FunctionGreater(funcParams, paramDictionary);
			}
			else if (string.Equals(funcName, FORMAT_FunctionIsNull, StringComparison.CurrentCultureIgnoreCase))
			{
				// Is null
				rv = FunctionIsNull(funcParams, paramDictionary);
			}
			else if (string.Equals(funcName, FORMAT_FunctionIsBlank, StringComparison.CurrentCultureIgnoreCase))
			{
				// Is blank function
				rv = FunctionIsBlank(funcParams, paramDictionary);
			}
			else
			{
				ThrowTemplateException(string.Format("Unknown function {0}", funcName));
				rv = true;
			}
			return rv;
		}

		string CommaSeperatedList(
		 object[] paramArray,
		 string param)
		{
			if (paramArray.Length != 1)
			{
				ThrowTemplateException(string.Format("The comma seperated list function requires 1 parameter: {0}",
				 param));
			}
			string[] list = (string[])paramArray[0];
			string rv = string.Join(",", list);
			return rv;
		}

		object DoSubtractFunction(
		 object[] paramArray,
		 string param)
		{
			if (paramArray.Length != 2)
			{
				ThrowTemplateException(string.Format("The subtract function requires 2 parameters: {0}",
				 param));
			}
			int calcResult = Subtract(paramArray[0], paramArray[1]);
			return calcResult;
		}

		private object FormatFunction(
		 string functionString,
		 Dictionary<string, object> variableDictionary)
		{
#if DEBUG
			if(functionString== "CommaSeperatedList(func:SQLColumnsFromDisplayFormat(var:filter.displayFormat))")
			{
			}
#endif
			// Get the function name
			int parenOpenPos = functionString.IndexOf('(');
			if (parenOpenPos == -1)
			{
				ThrowTemplateException(string.Format("Invalid function string: {0}",
				 functionString));
			}

			// Get the raw function parameters
			string[] funcParamsStr;
			string funcName = ParseFunctionParameters(functionString, parenOpenPos, out funcParamsStr);

			// Format the function parameters
			object[] funcParams = new object[funcParamsStr.Length];
			for (int i = 0; i < funcParams.Length; ++i)
			{
				funcParams[i] = FormatParam(funcParamsStr[i], variableDictionary);
			}

			return FormatFunction(funcName, funcParams, functionString);
		}

		protected virtual object FormatFunction(
		 string funcName,
		 object[] param,
		 string functionString)
		{
			object rv;
			if (string.Equals(funcName, FORMAT_Subtract, StringComparison.CurrentCultureIgnoreCase))
			{
				rv = DoSubtractFunction(param, functionString);
			}
			else if (string.Equals(funcName, FORMAT_FuncCommaSeperatedList, StringComparison.CurrentCultureIgnoreCase))
			{
				rv = CommaSeperatedList(param, functionString);
			}
			else
			{
				ThrowTemplateException(
				 string.Format("Invalid function: {0}",
				 functionString));
				rv = null;
			}
			return rv;
		}


		// This now deals with nested if's!
		// Returns the position to continue searching from
		private int DoFormattingIf(
		 string str,
		 string conditionFormat,
		 int posAfterFormat,
		 Dictionary<string, object> paramDictionary)
		{
			//// Check if the condition is true or false
			bool conditionIsTrue;
			int parenOpenPos = conditionFormat.IndexOf('(');
			if (parenOpenPos != -1)
			{
				string left4Chars = conditionFormat.Substring(0, 4);
				const string NOT = "not ";
				if (string.Equals(left4Chars, NOT, StringComparison.CurrentCultureIgnoreCase))
				{
					// Not
					conditionIsTrue =
					 !FunctionIsTrue(conditionFormat.Substring(NOT.Length), parenOpenPos - NOT.Length, paramDictionary);
				}
				else
				{
					conditionIsTrue = FunctionIsTrue(conditionFormat, parenOpenPos, paramDictionary);
				}
			}
			else
			{
				ThrowTemplateException(string.Format("Unknown 'if' condition: {0}", conditionFormat));
				conditionIsTrue = false;
			}

			int rv;
			if (conditionIsTrue)
			{
				// Continue after the {if} (include this section)
				rv = posAfterFormat;
			}
			else
			{
				// Find the 'endIf' (taking in to account nested if's)
				int endIf = FindPosOfFormatNested(
				 str,
				 posAfterFormat,
				 FORMAT_EndIf,
				 FORMAT_If,
				 out rv);
				if (endIf == -1)
				{
					ThrowTemplateException("'if' without associated end if.", posAfterFormat);
				}
			}

			return rv;
		}

		private int Subtract(
		 object lhsObj,
		 object rhsObj)
		{
			int lhs = (int)lhsObj;
			int rhs = (int)rhsObj;

			return lhs - rhs;
		}

		// Returns the position after the subtract
		private int DoSubtractAssignment(
		 StringBuilder output,
		 string str,
		 string param,
		 int posAfterFormat,
		 Dictionary<string, object> paramDictionary)
		{
			// Split by space
			char[] splitBySpace = { ' ' };
			string[] assignTokens = param.Split(splitBySpace, StringSplitOptions.RemoveEmptyEntries);
			if (assignTokens.Length < 5)
			{
				ThrowTemplateException("Invalid 'subtract'.");
			}

			string varName = assignTokens[0];
			object lhs = FormatParam(assignTokens[2], paramDictionary);
			object rhs = FormatParam(assignTokens[4], paramDictionary);

			int calcResult = Subtract(lhs, rhs);

			SetParam(paramDictionary, varName, calcResult);

			return posAfterFormat;
		}

		// Returns the position after the add
		//{add:numHeadingPlusOne = var:numHeading + 1}
		private int DoAdd(
		 StringBuilder output,
		 string str,
		 string param,
		 int posAfterFormat,
		 Dictionary<string, object> paramDictionary)
		{
			// Split by space
			char[] splitBySpace = { ' ' };
			string[] assignTokens = param.Split(splitBySpace, StringSplitOptions.RemoveEmptyEntries);
			if (assignTokens.Length < 5)
			{
				ThrowTemplateException("Invalid 'add'.");
			}

			string varName = assignTokens[0];

			int firstParam = Convert.ToInt32(FormatParam(assignTokens[2], paramDictionary));
			int secondParam = Convert.ToInt32(FormatParam(assignTokens[4], paramDictionary));

			SetParam(paramDictionary, varName, firstParam + secondParam);

			return posAfterFormat;
		}

		// Returns the position after the assign
		//{assign:varName = var:something}
		private int DoAssignment(
		 StringBuilder output,
		 string str,
		 string param,
		 int posAfterFormat,
		 Dictionary<string, object> paramDictionary)
		{
			// Quote aware split by space
			TokenAndPosition[] assignTokens = Str.QuoteAwareSplit(param, char.IsWhiteSpace, true);
			if (assignTokens.Length < 3)
			{
				ThrowTemplateException("Invalid 'assignment'.");
			}

			// The variable to assign to
			string varName = assignTokens[0].Token;
#if DEBUG
			if (varName == "addedColumn")
			{
			}
#endif

			// The value param string (could e.g. contain a variable)
			string varValueParam = assignTokens[2].Token;
			// The actual value once formatted
			object varValue = FormatParam(varValueParam, paramDictionary);

			SetParam(paramDictionary, varName, varValue);

			return posAfterFormat;
		}

		// Returns the position after the for
		private int DoFor(
		 StringBuilder output,
		 string str,
		 string param,
		 int posAfterFormat,
		 Dictionary<string, object> paramDictionary)
		{
			// Find the for end (taking in to account nested for's)
			int rv;
			int forEnd = FindPosOfFormatNested(
			 str,
			 posAfterFormat,
			 FORMAT_ForEnd,
			 FORMAT_For,
			 out rv);
			if (forEnd == -1)
			{
				ThrowTemplateException("For without end for.");
			}

			// The entire for statement not including {for}/{endFor}
			string forFormat = str.Substring(posAfterFormat, forEnd - posAfterFormat);

			// Split by space
			char[] splitBySpace = { ' ' };
			string[] forTokens = param.Split(splitBySpace, StringSplitOptions.RemoveEmptyEntries);
			if (forTokens.Length < 5)
			{
				ThrowTemplateException("Invalid 'for'.");
			}

			string loopVarName = forTokens[0];
			int loopVarInitialValue = Convert.ToInt32(forTokens[2]);
			int loopLastValue = Convert.ToInt32(FormatTemplateValue(forTokens[4], paramDictionary));

			for (int i = loopVarInitialValue; i <= loopLastValue; ++i)
			{
				// Update the loop variable in the dictionary
				SetParam(paramDictionary, loopVarName, i);

				// Do the work inside the loop
				FormatRecursive(output, forFormat, 0, paramDictionary);
			}
			// Remove the loop variable from the dictionary
			RemoveParam(paramDictionary, loopVarName);

			return rv;
		}

		private void SetParam(
		 Dictionary<string, object> paramDictionary,
		 string paramName,
		 object value)
		{
#if DEBUG
			if (string.Equals(paramName, "hasSubHeading", StringComparison.CurrentCultureIgnoreCase))
			{
				int valueInt;
				if (Int32.TryParse(value.ToString(), out valueInt))
				{
					if (valueInt != 0)
					{
					}
				}
			}
			if (string.Equals(paramName, "curSubHeading", StringComparison.CurrentCultureIgnoreCase))
			{
			}
			if(paramName== "curSubHeading")
			{
			}
#endif
			paramDictionary[paramName] = value;
		}

		private void RemoveParam(
		 Dictionary<string, object> paramDictionary,
		 string paramName)
		{
#if DEBUG
			if (string.Equals(paramName, "numHeading", StringComparison.CurrentCultureIgnoreCase))
			{
			}
#endif
			paramDictionary.Remove(paramName);
		}

		protected string ParseFunctionParameters(
		 string functionString,
		 out string[] funcParams)
		{
			int parenOpenPos = functionString.IndexOf('(');

			return ParseFunctionParameters(
			 functionString,
			 parenOpenPos,
			 out funcParams);
		}

		// Returns the function name
		protected string ParseFunctionParameters(
		 string functionString,
		 int parenOpenPos,
		 out string[] funcParams)
		{
			int parenClosePos = Str.FindNestedOpenClose(functionString, parenOpenPos + 1, '(', ')');
			if (parenClosePos == -1)
			{
				ThrowTemplateException("Missing function close parenthesis.");
			}
			string funcParamString = functionString.Substring(parenOpenPos + 1, parenClosePos - parenOpenPos - 1);
			char[] seperator = { ',' };
			funcParams = funcParamString.Split(seperator);

			// Trim each of the parameters
			for (int i = 0; i < funcParams.Length; ++i)
			{
				funcParams[i].Trim();
			}

			// Function name
			string funcName = functionString.Substring(0, parenOpenPos);

			return funcName;
		}


		private object FormatParam(
		 string param,
		 Dictionary<string, object> variableDictionary)
		{
			object paramFormatted;
			if (param[0] == '\"')
			{
				// Just a simple string
				int closeQuotePos = Str.FindClosingQuote(1, param, true);
				if (closeQuotePos == -1)
				{
					ThrowTemplateException("String missing closing quote.");
				}
				paramFormatted = Str.Unescape(param.Substring(1, closeQuotePos - 1));
			}
			else if (string.Equals(param, "null", StringComparison.CurrentCultureIgnoreCase))
			{
				// Null object
				paramFormatted = null;
			}
			else
			{
				// Special type with parameters?
				int paramPos = param.IndexOf(':');
				if (paramPos != -1)
				{
					paramFormatted = FormatTemplateValue(param, variableDictionary);
				}
				else
				{
					if (string.Equals(param, "true", StringComparison.CurrentCultureIgnoreCase))
					{
						paramFormatted = true;
					}
					else if (string.Equals(param, "false", StringComparison.CurrentCultureIgnoreCase))
					{
						paramFormatted = false;
					}
					else
					{
						// Numeric
						int asInt;
						if (int.TryParse(param, out asInt))
						{
							paramFormatted = asInt;
						}
						else
						{
							paramFormatted = Convert.ToDouble(param);
						}
					}
				}
			}

			return paramFormatted;
		}

		// TODO: what's the point in this function now? Shouldn't we call 'FormatParam' instead? incase it's a number or
		// a string
		// The difference between this and 'FormatParam' is that this one doesn't deal with numbers or strings etc.
		private object FormatTemplateValue(
		 string str,
		 Dictionary<string, object> variableDictionary)
		{
			int paramPos = str.IndexOf(':');
			if (paramPos == -1)
			{
				ThrowTemplateException(
				 string.Format("Formatting option {0} does not have the parameter seperator (:) specified.", str));
			}

			string type = str.Substring(0, paramPos);
			string param = str.Substring(paramPos + 1, str.Length - paramPos - 1);
			return FormatTemplateValue(type, param, variableDictionary);
		}

		// I see this more as a 'returns a value' type function now
		// Formatting which returns a string.
		// For want of a better name
		private object FormatTemplateValue(
		 string type,
		 string param,
		 Dictionary<string, object> variableDictionary)
		{
			object rv;
			if (string.Equals(type, FORMAT_Props, StringComparison.CurrentCultureIgnoreCase))
			{
				// Properties
				rv = FormatProps(param, variableDictionary);
			}
			else if (string.Equals(type, FORMAT_Var, StringComparison.CurrentCultureIgnoreCase))
			{
				// Variable
				rv = FormatVariable(param, variableDictionary);
			}
			else if (string.Equals(type, FORMAT_Function, StringComparison.CurrentCultureIgnoreCase))
			{
				// Function
				rv = FormatFunction(param, variableDictionary);
			}
			else if (string.Equals(type, FORMAT_CountProps, StringComparison.CurrentCultureIgnoreCase))
			{
				rv = FormatCountProps(param, variableDictionary);
			}
			else if (string.Equals(type, FORMAT_CountVar, StringComparison.CurrentCultureIgnoreCase))
			{
				rv = FormatCountVar(param, variableDictionary);
			}
			else if (string.Equals(type, FORMAT_RenderVar, StringComparison.CurrentCultureIgnoreCase))
			{
				rv = RenderVariable(param, variableDictionary);
			}
			else
			{
				ThrowTemplateException(string.Format("Unexpected template file format: {0}", type));
				rv = "";
			}

			return rv;
		}

		private string RenderVariable(
		 string param,
		 Dictionary<string, object> variableDictionary)
		{
			string objectParam;
			int? objectArrayIndex;
			IHasTemplate obj = GetVarObject(param, variableDictionary, out objectParam, out objectArrayIndex) as IHasTemplate;
			if(obj==null)
			{
				obj = GetVariable(objectParam, variableDictionary, objectArrayIndex) as IHasTemplate;
			}

			if (string.IsNullOrEmpty(obj.TemplateName()))
			{
				ThrowTemplateException(
				 string.Format("Render called but the object does not have a template: {0}. {1}.",
				 param,
				 obj.ToString()));
			}

			IHasProps objProps = obj as IHasProps;


			variableDictionary["this"] = objProps;
			string rv = FormatTemplate(obj.TemplateName(), variableDictionary);
			variableDictionary.Remove("this");
			return rv;
		}

		private bool FunctionIsNull(
		 string[] funcParams,
		 Dictionary<string, object> variableDictionary)
		{
			if (funcParams.Length != 1)
			{
				ThrowTemplateException("The is null function requires 1 parameter.");
			}

			object param1 = FormatParam(funcParams[0], variableDictionary);
			bool rv = (param1 == null);
			return rv;
		}

		private bool FunctionIsBlank(
		 string[] funcParams,
		 Dictionary<string, object> variableDictionary)
		{
			if (funcParams.Length != 1)
			{
				ThrowTemplateException("The is not blank function requires 1 parameter.");
			}

			object param1 = FormatParam(funcParams[0], variableDictionary);
			bool rv;
			if(param1==null)
			{
				rv=true;
			}
			else
			{
				rv = string.IsNullOrEmpty((string)param1);
			}
			return rv;
		}

		private bool IsTrue(
		 string[] paramArray,
		 Dictionary<string, object> variableDictionary)
		{
			if (paramArray.Length != 1)
			{
				ThrowTemplateException(string.Format("The IsTrue function requires 1 parameter."));
			}

			return ((bool)FormatParam(paramArray[0], variableDictionary)) != false;
		}

		//TODO a lot of repetition in these comparison functions. What about 'binary function', 'unary function' etc?
		private bool FunctionEquals(string[] funcParams, Dictionary<string, object> variableDictionary)
		{
			if (funcParams.Length != 2)
			{
				ThrowTemplateException("The equals function requires 2 parameters.");
			}
			object param1 = FormatParam(funcParams[0], variableDictionary);
			object param2 = FormatParam(funcParams[1], variableDictionary);

#if DEBUG
			if (funcParams[0] == "var:hasSubHeading")
			{
			}
#endif

			bool rv;
			if(param1 == null || param2==null)
			{
				rv = (param1 == null && param2 == null);
			}
			else
			{
				// String comparison - this will do for now. Potentially later may need to do type checking
				rv = (param1.ToString() == param2.ToString());
			}
			return rv;
		}

		private bool FunctionGreater(string[] funcParams, Dictionary<string, object> variableDictionary)
		{
			if (funcParams.Length != 2)
			{
				ThrowTemplateException("The greater function requires 2 parameters.");
			}

			object param1 = FormatParam(funcParams[0], variableDictionary);
			object param2 = FormatParam(funcParams[1], variableDictionary);

			int param1Int = (int)param1;
			int param2Int = (int)param2;

			return (param1Int > param2Int);
		}

		private int FindPosOfFormatNested(
		 string str,
		 int beginPos,
		 string closingString,
		 string openingString,
		 out int afterFormat)
		{
			// Because this is looking for nested strings, do this within a loop

			// Where to start searching for the closing format (we start at the beginning of the string)
			int closeBeginPos = beginPos;

			// Where to start searching for the open format (we start at the beginning of the string)
			int openBeginPos = beginPos;
			for (; ; )
			{
				// Find the index of the next closing format
				int beginOfCloseIndex = FindPosOfFormat(
				 str,
				 closingString,
				 closeBeginPos,
				 str.Length,
				 out afterFormat);
				if (beginOfCloseIndex == -1)
				{
					// No closing format (format error)
					return -1;
				}
				// Update the closing begin position to after the closing format
				closeBeginPos = afterFormat;

				// In between the current start position, and the latest closing position, attempt to find the opening string
				int afterOpenIndex;
				int beginOfOpenIndex = FindPosOfFormatWithParam(
				 str,
				 openingString,
				 openBeginPos,
				 beginOfCloseIndex,
				 out afterOpenIndex);
				if (beginOfOpenIndex == -1)
				{
					// No more opening positions - success
					return beginOfCloseIndex;
				}

				// Update the beginning position to be after the latest opening string
				openBeginPos = afterOpenIndex;

				// Next
			}
		}

		// Returns the first character of the format.
		// 'afterFormat' returns the character directly after the format
		// E.g. for {endif}
		//				^ - return value points here
		//						 ^ - 'afterFormat' points here
		// 'endIndex' allows the client to specify how much of the input string to search into if they are only
		//  interested in a subset
		private int FindPosOfFormat(
		 string str,
		 string format,
		 int startIndex,
		 int endIndex,
		 out int afterFormat)
		{
			for (int i = startIndex; i < endIndex; ++i)
			{
				if (str[i] == '{')
				{
					// Matches the format?
					string subString = str.Substring(i + 1, format.Length + 1);
					if (string.Equals(subString, format + '}', StringComparison.CurrentCultureIgnoreCase))
					{
						// +2 to skip the { }
						afterFormat = i + format.Length + 2;
						return i;
					}
				}
			}
			afterFormat = -1;
			return -1;
		}

		// For finding formats that have parameters
		// 'afterFormat' returns the character directly after the format
		// E.g. for {forEach:blah}
		//				^ - return value points here
		//						        ^ - 'afterFormat' points here
		private int FindPosOfFormatWithParam(
		 string str,
		 string format,
		 int beginPos,
		 int endPos,
		 out int afterFormat)
		{
			string substring = str.Substring(beginPos, endPos - beginPos);
			string expression = string.Format(@"\{{{0}:.*?\}}", format);
			Match match = Regex.Match(substring, expression, RegexOptions.IgnoreCase);
			if (match.Success)
			{
				afterFormat = beginPos + match.Index + match.Length;
				return beginPos + match.Index;
			}
			afterFormat = -1;
			return -1;
		}

		public static void AssertPropertyRequiresIndex(
		 object caller,
		 string property,
		 int? arrayIndex)
		{
			if (!arrayIndex.HasValue)
			{
				ThrowTemplateException(string.Format("Property '{0}' on object '{1}' requires an array index.",
				 property,
				 caller.ToString()));
			}
		}

		private object GetVariable(
		 string name,
		 Dictionary<string, object> variableDictionary,
		 int? arrayIndex)
		{
			object obj = variableDictionary[name];
			if (obj == null)
			{
				if (!variableDictionary.ContainsKey(name))
				{
					ThrowTemplateException(string.Format("Variable '{0}' has not been assigned",
					 name));
				}
			}
			return obj;
		}

		// From:
		// {var:filter.input[i].Name}
		// Return:
		// objectParam = 'Name'
		// Rv = input[i]
		private IHasProps GetVarObject(
		 string param,
		 Dictionary<string, object> variableDictionary,
		 out string objectParam,
		 out int? objectArrayIndex)
		{
			// Tokenize by dot
			string[] dotTokens = param.Split('.');

			IHasProps currentObject = null;

			for (int i = 0; ; ++i)
			{
				// Get array index if specified
				objectParam = dotTokens[i];
				objectArrayIndex = ParseArrayNotation(variableDictionary, ref objectParam);

				bool isLastIteration = (i == dotTokens.Length - 1);

				// First token?
				if (i == 0)
				{
					if(isLastIteration)
					{
						break;
					}

					// Get from the dictionary
					object obj = GetVariable(objectParam, variableDictionary, objectArrayIndex);
					currentObject = obj as IHasProps;
				}
				else
				{
					if (isLastIteration)
					{
						break;
					}
					
					// get the next object
					currentObject = currentObject.FormatProps(objectParam, objectArrayIndex) as IHasProps;
				}
			}

			return currentObject;
		}

		private object FormatVariable(
		 string param,
		 Dictionary<string, object> paramDictionary)
		{
			string objectParam;
			int? objectArrayIndex;
			IHasProps obj = GetVarObject(param, paramDictionary, out objectParam, out objectArrayIndex);
			object rv;

			if(obj!=null)
			{
				rv = obj.FormatProps(objectParam, objectArrayIndex,true);
			}
			else
			{
				rv=GetVariable(objectParam, paramDictionary, objectArrayIndex);
			}

			return rv;
		}

		// From:
		// {props:filter.input[i].Name}
		// Return:
		// objectParam = 'Name'
		// Rv = input[i]
		private IHasProps GetPropsObject(
		 string param,
		 Dictionary<string, object> variableDictionary,
		 out string objectParam,
		 out int? objectArrayIndex)
		{
			// Tokenize by dot
			string[] dotTokens = param.Split('.');

			IHasProps currentObject = this;

			for (int i = 0; ; ++i)
			{
				// Get array index if specified
				objectParam = dotTokens[i];
				objectArrayIndex = ParseArrayNotation(variableDictionary, ref objectParam);

				bool lastItem= (i== dotTokens.Length - 1);
				if(lastItem)
				{
					break;
				}

				// Get the next object
				currentObject = currentObject.FormatProps(objectParam, objectArrayIndex) as IHasProps;
			}

			return currentObject;
		}

		//{for:i = 0 to countVar:filter.input}
		private int FormatCountVar(
		 string param,
		 Dictionary<string, object> variableDictionary)
		{
			string objectParam;
			int? objectArrayIndex;
			IHasProps obj = GetVarObject(param, variableDictionary, out objectParam, out objectArrayIndex);
			int rv;
			if (obj == null)
			{
				// This doesn't make sense. For exameple:
				//{for:i = 0 to countVar:i} <-- this is the error. why not just do below
				//{for:i = 0 to var:i}
				ThrowTemplateException(string.Format("Invalid count variable: {0}",param));
			}
			rv =obj.Count(objectParam);
			return rv;
		}

		//{for:i = 0 to countProps:filter}
		private int FormatCountProps(
		 string param,
		 Dictionary<string, object> variableDictionary)
		{
			string objectParam;
			int? objectArrayIndex;
			IHasProps obj = GetPropsObject(param, variableDictionary, out objectParam, out objectArrayIndex);
			return obj.Count(objectParam);
		}

		private object FormatProps(
		 string param,
		 Dictionary<string, object> variableDictionary)
		{
			string objectParam;
			int? objectArrayIndex;
			IHasProps obj = GetPropsObject(param, variableDictionary, out objectParam, out objectArrayIndex);
			return obj.FormatProps(objectParam,objectArrayIndex);
		}

		// Returns the array index if there is one
		private int? ParseArrayNotation(
		 Dictionary<string, object> variableDictionary,
		 ref string str)
		{
			string errorMsg;
			string arrayIndexStr;
			if (!Str.ParseArrayNotation(ref str, out arrayIndexStr, out errorMsg))
			{
				ThrowTemplateException(errorMsg);
			}

			int? arrayIndex = null;
			if (arrayIndexStr != null)
			{
				object formattedIndex = FormatParam(arrayIndexStr, variableDictionary);
				arrayIndex = (int)formattedIndex;
			}

			return arrayIndex;
		}
	}
}
