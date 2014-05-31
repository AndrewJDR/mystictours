using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// This class encodes and decodes JSON strings.
/// Spec. details, see http://www.json.org/
/// 
/// JSON uses Arrays and Objects. These correspond here to the datatypes ArrayList and Hashtable.
/// All numbers are parsed to floats.
/// </summary>
public class JSON {
	public const int TOKEN_NONE = 0; 
	public const int TOKEN_CURLY_OPEN = 1;
	public const int TOKEN_CURLY_CLOSE = 2;
	public const int TOKEN_SQUARED_OPEN = 3;
	public const int TOKEN_SQUARED_CLOSE = 4;
	public const int TOKEN_COLON = 5;
	public const int TOKEN_COMMA = 6;
	public const int TOKEN_STRING = 7;
	public const int TOKEN_NUMBER = 8;
	public const int TOKEN_TRUE = 9;
	public const int TOKEN_FALSE = 10;
	public const int TOKEN_NULL = 11;
	public const int TOKEN_KEY_STRING = 12;

	private const int BUILDER_CAPACITY = 10;

	public static Hashtable JsonDecode(string json) {
		bool success = true;
		return JsonDecode(json, 0, ref success);
	}

	public static Hashtable JsonDecode(string json, int startindex, ref bool success) {
		success = true;
		if (json != null) {
			char[] charArray = json.ToCharArray();
            int index = startindex;
			Hashtable value = ParseValue(LookAhead(charArray, ref index), charArray, ref index, ref success) as Hashtable;
			return value;
        } else
            return null;
	}

	public static string JsonEncode(object json) {
		StringBuilder builder = new StringBuilder(BUILDER_CAPACITY);
		bool success = SerializeValue(json, builder);
		return (success ? builder.ToString() : null);
	}

	protected static Hashtable ParseObject(char[] json, ref int index, ref bool success) {
		Hashtable table = new Hashtable(6);
		int token;

		// {
		index++;

		while (true) {
			token = LookAhead(json, ref index);
			if (token == JSON.TOKEN_NONE) {
				success = false;
				return null;
			} else if (token == JSON.TOKEN_COMMA) {
				index++;
			} else if (token == JSON.TOKEN_CURLY_CLOSE) {
				index++;
				return table;
			} else {
				// name
				string name = token == JSON.TOKEN_KEY_STRING ? ParseKey(json, ref index, ref success) : ParseString(json, ref index, ref success);
				if (!success) {
					success = false;
					return null;
				}
				// :
				while (index < json.Length && json[index] <= ' ')
					index++;
				if (index == json.Length || json[index++] != ':') {
					success = false;
					return null;
				}
				// value
				object value = ParseValue(LookAhead(json, ref index), json, ref index, ref success);
				if (!success) {
					success = false;
					return null;
				}
				table[name] = value;
			}
		}
	}

	protected static ArrayList ParseArray(char[] json, ref int index, ref bool success) {
		ArrayList array = new ArrayList(16);

		// [
		index++;

		while (true) {
			int token = LookAhead(json, ref index);
			if (token == JSON.TOKEN_NONE) {
				success = false;
				return null;
			} else if (token == JSON.TOKEN_COMMA) {
				index++;
			} else if (token == JSON.TOKEN_SQUARED_CLOSE) {
				index++;
				break;
			} else {
				object value = ParseValue(token, json, ref index, ref success);
				if (!success)
					return null;
				array.Add(value);
			}
		}
		return array;
	}

	protected static object ParseValue(int token, char[] json, ref int index, ref bool success) {
		switch (token) {
			case JSON.TOKEN_STRING:
				return ParseString(json, ref index, ref success);
			case JSON.TOKEN_CURLY_OPEN:
				return ParseObject(json, ref index, ref success);
			case JSON.TOKEN_NUMBER:
				return ParseNumber(json, ref index, ref success);
			case JSON.TOKEN_SQUARED_OPEN:
				return ParseArray(json, ref index, ref success);
			case JSON.TOKEN_TRUE:
				index+= 4;
				return true;
			case JSON.TOKEN_FALSE:
				index+= 5;
				return false;
			case JSON.TOKEN_NULL:
				index+= 4;
				return null;
			case JSON.TOKEN_KEY_STRING:
				return ParseKey(json, ref index, ref success);
			//case JSON.TOKEN_NONE:
			//	break;
		}
		success = false;
		return null;
	}

	protected static string ParseKey(char[] json, ref int index, ref bool success) {
		int si = 0;
		char c;
		while (index < json.Length) {
			c = json[index];
			if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
				return new string(json, 0, si);
			index++;
			json[si++] = c;
		}
		success = false;
		return null;
	}
	
	protected static string ParseString(char[] json, ref int index, ref bool success) {
		int si = 0;
		char c, startc = json[index++];
		// " or '
		while (index < json.Length) {
			c = json[index++];
			if (c == startc)
				return new string(json, 0, si);
			else if (c == '\\') {
				if (index == json.Length)
					break;
				c = json[index++];
				if (c == '"' || c == '\'' || c == '\\' || c == '/')
					json[si++] = c;
				else if (c == 'b')
					json[si++] = '\b';
				else if (c == 'f')
					json[si++] = '\f';
				else if (c == 'n')
					json[si++] = '\n';
				else if (c == 'r')
					json[si++] = '\r';
				else if (c == 't')
					json[si++] = '\t';
				else if (c == 'u') {
					int remainingLength = json.Length - index;
					if (remainingLength >= 4) {
						//char[] unicodeCharArray = new char[4];
						//Array.Copy(json, index, unicodeCharArray, 0, 4);
						//uint codePoint = UInt32.Parse(new string(json, index, 4), System.Globalization.NumberStyles.HexNumber);
						// convert the integer codepoint to a unicode char and add to string
						//s[si++] = Char.ConvertFromUtf32((int)codePoint)[0];
						// skip 4 chars
						index+= 4;
					} else
						break;
				}
			} else
				json[si++] = c;
		}
		success = false;
		return null;
	}

	protected static float ParseNumber(char[] json, ref int index, ref bool success) {
		int lastIndex;
		for (lastIndex = index; lastIndex < json.Length; lastIndex++)
			if (json[lastIndex] < '+' || json[lastIndex] > '9' || json[lastIndex] == '/' || json[lastIndex] == ',') //   "0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
				break;
		float number;
		//success = float.TryParse(new string(json, index, lastIndex - index), out number);
		success = TryParseFloatFastStream(json, index, lastIndex, out number);
		index = lastIndex;
		return number;
	}

	protected static int LookAhead(char[] json, ref int index) {
		char c;
		while (index < json.Length && (c = json[index]) <= ' ')
			index++;
		if (index == json.Length)
			return JSON.TOKEN_NONE;
		switch (c) {
			case '"':
				return JSON.TOKEN_STRING;
			case '\'':
				return JSON.TOKEN_STRING;
			case '{':
				return JSON.TOKEN_CURLY_OPEN;
			case '}':
				return JSON.TOKEN_CURLY_CLOSE;
			case '[':
				return JSON.TOKEN_SQUARED_OPEN;
			case ']':
				return JSON.TOKEN_SQUARED_CLOSE;
			case ',':
				return JSON.TOKEN_COMMA;
			case ':':
				return JSON.TOKEN_COLON;
		}
		if ((c >= '0' && c <= '9') || c == '-')
			return JSON.TOKEN_NUMBER;
		int remainingLength = json.Length - index;
		// false
		if (remainingLength >= 5 && c == 'f' && json[index + 1] == 'a' && json[index + 2] == 'l' && json[index + 3] == 's' && json[index + 4] == 'e')
			return JSON.TOKEN_FALSE;
		// true
		if (remainingLength >= 4 && c == 't' && json[index + 1] == 'r' && json[index + 2] == 'u' && json[index + 3] == 'e')
			return JSON.TOKEN_TRUE;
		// null
		if (remainingLength >= 4 && c == 'n' && json[index + 1] == 'u' && json[index + 2] == 'l' && json[index + 3] == 'l')
			return JSON.TOKEN_NULL;
		// object key
		if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_')
			return JSON.TOKEN_KEY_STRING;
		return JSON.TOKEN_NONE;
	}

	/*
	 * Serialize
	 */
	protected static bool SerializeValue(object value, StringBuilder builder) {
		if (value == null) {
			builder.Append("null");
			return true;
		}
		bool success = true;
		Type t = value.GetType();
		if (t == typeof(string))
			success = SerializeString((string)value, builder);
		else if (t == typeof(Hashtable))
			success = SerializeObject((Hashtable)value, builder);
		else if (t == typeof(ArrayList))
			success = SerializeArray((ArrayList)value, builder);
		else if (value is object[])
			success = SerializeArray((object[])value, builder);
		else if (t == typeof(int[]))
			success = SerializeArray((int[])value, builder);
		else if (t == typeof(int))
			success = SerializeNumber((int)value, builder);
		else if (t == typeof(byte))
			success = SerializeNumber((byte)value, builder);
		else if (t == typeof(float))
			success = SerializeNumber((float)value, builder);
		else if (t == typeof(bool))
			builder.Append(((Boolean)value == true) ? "true" : "false");
		else if (IsNumeric(value))
			success = SerializeNumber(Convert.ToDouble(value), builder);
		else
			success = false;
		return success;
	}
	
	protected static bool SerializeObject(Hashtable anObject, StringBuilder builder) {
		builder.Append("{");
		IDictionaryEnumerator e = anObject.GetEnumerator();
		bool first = true;
		while (e.MoveNext()) {
			string key = e.Key.ToString();
			object value = e.Value;
			if (!first)
				builder.Append(", ");
			SerializeString(key, builder);
			builder.Append(":");
			if (!SerializeValue(value, builder))
				return false;
			first = false;
		}
		builder.Append("}");
		return true;
	}

	protected static bool SerializeArray(ArrayList anArray, StringBuilder builder) {
		builder.Append("[");
		bool first = true;
		for (int i = 0; i < anArray.Count; i++) {
			object value = anArray[i];
			if (!first)
				builder.Append(", ");
			if (!SerializeValue(value, builder))
				return false;
			first = false;
		}
		builder.Append("]");
		return true;
	}

	protected static bool SerializeArray(object[] anArray, StringBuilder builder) {
		builder.Append("[");
		bool first = true;
		for (int i = 0; i < anArray.Length; i++) {
			object value = anArray[i];
			if (!first)
				builder.Append(", ");
			if (!SerializeValue(value, builder))
				return false;
			first = false;
		}
		builder.Append("]");
		return true;
	}

	protected static bool SerializeArray(int[] anArray, StringBuilder builder) {
		builder.Append("[");
		bool first = true;
		for (int i = 0; i < anArray.Length; i++) {
			int value = anArray[i];
			if (!first)
				builder.Append(", ");
			if (!SerializeValue(value, builder))
				return false;
			first = false;
		}
		builder.Append("]");
		return true;
	}

	protected static bool SerializeString(string aString, StringBuilder builder) {
		builder.Append("\"");
		char[] charArray = aString.ToCharArray();
		for (int i = 0; i < charArray.Length; i++) {
			char c = charArray[i];
			if (c == '"') {
				builder.Append("\\\"");
			} else if (c == '\\') {
				builder.Append("\\\\");
			} else if (c == '\b') {
				builder.Append("\\b");
			} else if (c == '\f') {
				builder.Append("\\f");
			} else if (c == '\n') {
				builder.Append("\\n");
			} else if (c == '\r') {
				builder.Append("\\r");
			} else if (c == '\t') {
				builder.Append("\\t");
			} else {
				int codepoint = Convert.ToInt32(c);
				if ((codepoint >= 32) && (codepoint <= 126)) {
					builder.Append(c);
				} else {
					builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
				}
			}
		}
		builder.Append("\"");
		return true;
	}

	protected static bool SerializeNumber(byte number, StringBuilder builder) {
		builder.Append(Convert.ToString(number));
		return true;
	}
	
	protected static bool SerializeNumber(int number, StringBuilder builder) {
		builder.Append(Convert.ToString(number));
		return true;
	}

	protected static bool SerializeNumber(float number, StringBuilder builder) {
		builder.Append(Convert.ToString(number));
		return true;
	}
	
	protected static bool SerializeNumber(double number, StringBuilder builder) {
		builder.Append(Convert.ToString(number));
		return true;
	}

	/// <summary>
	/// Determines if a given object is numeric in any way
	/// (can be integer, float, null, etc). 
	/// 
	/// Thanks to mtighe for pointing out Float.TryParse to me.
	/// </summary>
	protected static bool IsNumeric(object o) {
		double result;
		return (o == null) ? false : Double.TryParse(o.ToString(), out result);
	}
	
	/*
	 * Misc
	 */
	public static bool TryParseFloatFastStream(char[] s, int begin, int end, out float result) {
	  result = 0;
	  char c = s[begin];
	  int sign = 0;
	  int start = begin;
	 
	  if (c == '-') {
	    sign = -1;
	    start = begin + 1;
	  } else if (c > 57 || c < 48) {
	    if (c < 33) {
	      do {
	        ++start;
	      } while (start < end && (c = s[start]) < 33);
	 
	      if (start >= end)
	        return false;
	 
	      if (c == '-') {
	        sign = -1;
	        ++start;
	      } else {
	        sign = 1;
	      }
	    } else {
	      result = 0;
	      return false;
	    }
	  } else {
	    start = begin + 1;
	    result = 10 * result + (c - 48);
	    sign = 1;
	  }
	 
	  int i = start;
	 
	  for (; i < end; ++i) {
	    c = s[i];
	    if (c > 57 || c < 48) {
	      if (c == '.') {
	        ++i;
	        goto DecimalPoint;
	      } else {
	        result = 0;
	        return false;
	      }
	    }
	    result = 10 * result + (c - 48);
	  }
	  result*= sign;
	  return true;

	DecimalPoint:
	  long temp = 0;
	  int length = i;
	  float exponent = 0;
	 
	  for (; i < end; ++i) {
	    c = s[i];
	    if (c > 57 || c < 48) {
	      if (c >= 33) {
	        if (c == 'e' || c == 'E') {
	          length = i - length;
	          goto ProcessExponent;
	        }
	        result = 0;
	        return false;
	      } else {
	        length = i - length;
	        goto ProcessFraction;
	      }
	    }
	    temp = 10 * temp + (c - 48);
	  }
	  length = i - length;

	ProcessFraction:
	  float fraction = (float)temp;
	  if (length < _powLookup.Length)
	    fraction = fraction / _powLookup[length];
	  else
	    fraction = fraction / _powLookup[_powLookup.Length - 1];

	  result+= fraction;
	  result*= sign;
	  if (exponent > 0)
	    result *= exponent;
	  else if(exponent < 0)
	    result /= -exponent;
	  return true;
	 
	ProcessExponent:
	 
	  int expSign = 1;
	  int exp = 0;
	  for (++i; i < end; ++i) {
	    c = s[i];
	    if (c > 57 || c < 48) {
	      if (c == '-') {
	        expSign = -1;
	        continue;
	      }
	    }
	    exp = 10 * exp + (c - 48);
	  }
	  exponent = _floatExpLookup[exp] * expSign;
	  goto ProcessFraction;
	}
	 
	private static readonly long[] _powLookup = new[] {
	  1, // 10^0
	  10, // 10^1
	  100, // 10^2
	  1000, // 10^3
	  10000, // 10^4
	  100000, // 10^5
	  1000000, // 10^6
	  10000000, // 10^7
	  100000000, // 10^8
	  1000000000, // 10^9,
	  10000000000, // 10^10,
	  100000000000, // 10^11,
	  1000000000000, // 10^12,
	  10000000000000, // 10^13,
	  100000000000000, // 10^14,
	  1000000000000000, // 10^15,
	  10000000000000000, // 10^16,
	  100000000000000000, // 10^17,
	};

	private static readonly float[] _floatExpLookup = GetFloatExponents();
	 
	private static float[] GetFloatExponents() {
	  var max = 309;
	  var exps = new float[max];
	  for (var i = 0; i < max; i++)
	    exps[i] = (float)Math.Pow(10, i);
	  return exps;
	}
}
