using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

//
// Usage:
//  byte[] data = BinMsg.Encode(object, previousSize);
//  byte[] data = BinMsg.Encode(object);
//  object o = BinMsg.Decode(data);
//
// Supported Types:
// bool, byte, char, short, ushort, int, uint, long, ulong, float, double, string, unicodeString, DateTime
// ArrayList, Hashtable, Queue, Stack
// Array[], 2DArray[,], 3DArray[,,], 4DArray[,,,]
// List<>, Dictionary<,>, Queue<>, Stack<>, HashSet<>

// Format:
// 00 - simple types
// 20 - string
// 40 - unicode string
// 60 - Array [type]
// 80 - ArrayList
// a0 - List<> [type]
// c0 - Hashtable
// e0 - Dictionary<,> [type] [type]

// count:
// 00-0f - 0-15
// 10-17 - 0x7 ff
// 18-1b - 0x3 ff ff
// 1c-1f - 0x3 ff ff ff

// types:
// 00 - null or object (0)
// 01 - true or bool (0)
// 02 - false (0)
// 03 - byte (1)
// 04 - char (2)
// 05 - short (2)
// 06 - ushort (2)
// 07 - int (byte) (1)
// 08 - int (short) (2)
// 09 - int (4)
// 0a - uint (ushort) (2)
// 0b - uint (4)
// 0c - long (8)
// 0d - ulong (8)
// 0e - float (4)
// 0f - double (8)
// 10 - System.DateTime
// 11 - Vector2
// 12 - Vector3
// 13 - Vector4
// 14 - Color
// 15 - Color32
// 16 - Rect
// 17 - Other Unity3d Types
//      00 - Bounds
//		01 - Quaternion
// 18 - reference to object [int]
// 19 - object [type]
// 1a - 
// 1b - reference to string [short]
// 1c - reference to string [byte]
// 1d - reference to string [byte]
// 1e - reference to string [byte]
// 1f - reference to string [byte]

// 20 - arraytype: 1 dim array
// 21 - arraytype: 2 dim array
// 22 - arraytype: 3 dim array
// 23 - arraytype: 4 dim array
// 24 - arraytype: Queue<>
// 25 - arraytype: Stack<>
// 26 - arraytype: HashSet<>
// 27 - arraytype: Queue
// 28 - arraytype: Stack
//
// 30 - string
// 31 - Hashtable
// 32 - Dictionary<>
// 33 - List<>
// 34 - ArrayList
// 35 - Queue<>
// 36 - Stack<>
// 37 - HashSet<>
// 38 - 1 dim Array
// 39 - 2 dim Array
// 3a - 3 dim Array
// 3b - 4 dim Array
// 3c - Queue
// 3d - Stack
// 

//#define BINMSG_GENERICS

public class BinMsg {
	public static byte[] Encode(object o) {
		return new BinMsg().EncodeValue(o);
	}

	public static object Decode(byte[] data) {
		if (data == null || data.Length < 3)
			return null;
		return new BinMsg().DecodeValue(data);
	}
	
	public static byte[] Encode(object o, int estimateSize) {
		return new BinMsg().EncodeValue(o, estimateSize);
	}

	//
	// Private methods
	//
	int index = 0, stringCounter = 0, objectCounter = 0;
	byte[] data;
	string[] strings;
	object[] objects;
	Dictionary<string, int> usedStrings;
	Dictionary<object, int> usedObjects;
	Dictionary<string, Type> types;

	byte[] EncodeValue(object o) {
		return EncodeValue(o, 6144);
	}

	byte[] EncodeValue(object o, int estimateSize) {
		if (estimateSize < 256)
			estimateSize = 256;
		usedStrings = new Dictionary<string, int>(128, StringComparer.Ordinal);
		data = new byte[estimateSize + 256];
		index = 2;
		Add(o);
		System.Array.Resize<byte>(ref data, index);
		if (objectCounter > 0xffff)
			objectCounter = 0xffff;
		data[0] = (byte)(stringCounter >> 8);
		data[1] = (byte)(objectCounter >> 8);
		return data;
	}

	void Add(object o) {
		if (o == null) {
			if (data.Length <= index)
				IncreaseDataSize(1);
			data[index++] = 0;
			return;
		}
		Type t = o.GetType();
		if (t == typeof(string))
			Add ((string)o);
		else if (t == typeof(int))
			Add ((int)o);
		else if (t == typeof(bool)) {
			if (data.Length <= index)
				IncreaseDataSize(1);
			data[index++] = (byte)((bool)o ? 1 : 2);
		} else if (t == typeof(float))
			Add ((float)o);
		else if (t == typeof(byte))
			Add ((byte)o);
		else if (t == typeof(Hashtable))
			Add ((Hashtable)o);
		else if (t == typeof(ArrayList))
			Add ((ArrayList)o);
		else if (t.IsArray)
			AddArray (t, o);
		#if BINMSG_GENERICS
		else if (t.IsGenericType)
			AddGeneric (t, o);
		#endif
		else if (t == typeof(short))
			Add ((short)o);
		else if (t == typeof(ushort))
			Add ((ushort)o);
		else if (t == typeof(char))
			Add ((char)o);
		else if (t == typeof(long))
			Add ((long)o);
		else if (t == typeof(ulong))
			Add ((ulong)o);
		else if (t == typeof(uint))
			Add ((uint)o);
		else if (t == typeof(double))
			Add ((double)o);
		else if (t == typeof(Vector2))
			Add ((Vector2)o);
		else if (t == typeof(Vector3))
			Add ((Vector3)o);
		else if (t == typeof(Vector4))
			Add ((Vector4)o);
		else if (t == typeof(DateTime))
			Add ((DateTime)o);
		else if (t == typeof(Color))
			Add ((Color)o);
		else if (t == typeof(Color32))
			Add ((Color32)o);
		else if (t == typeof(Rect))
			Add ((Rect)o);
		else if (t == typeof(Quaternion))
			Add ((Quaternion)o);
		else if (t == typeof(Bounds))
			Add ((Bounds)o);
		else if (t == typeof(Stack))
			Add ((Stack)o);
		else if (t == typeof(Queue))
			Add ((Queue)o);
		else if (!t.IsValueType) {
			// add object
			if (data.Length <= index)
				IncreaseDataSize(1);
			if (usedObjects == null) {
				usedObjects = new Dictionary<object, int>(64);
				objectCounter = 0;
			} else {
				int idx;
				if (usedObjects.TryGetValue(o, out idx)) {
					data[index++] = 0x18;
					Add ((int)idx);
					return;
				}
			}
			data[index++] = 0x19;
			Add (t.Assembly.FullName);
			Add (t.FullName);
			System.Reflection.FieldInfo[] allFields = t.GetFields();
			var fields = new List<System.Reflection.FieldInfo>(allFields.Length);
			foreach (var field in allFields)
				if (!field.IsNotSerialized && field.IsPublic)
					fields.Add(field);
			Add ((int)fields.Count);
			for (int i=0; i<fields.Count; i++) {
				System.Reflection.FieldInfo field = fields[i];
				Add ((string)field.Name);
				Add (fields[i].GetValue(o));
			}
			usedObjects[o] = objectCounter++;
		} else // unsupported type
			Add ((object)null);
	}

	void Add(string v) {
		unchecked {
			if (v.Length > 0 && v.Length < 256) {
				int us;
				if (usedStrings.TryGetValue(v, out us)) {
					// reference to string
					if (data.Length < index + 3)
						IncreaseDataSize(3);
					if (us <= 0x3ff) {
						data[index++] = (byte)(0x1c | (us >> 8));
						data[index++] = (byte)(us & 0xff);
					} else {
						data[index++] = 0x1b;
						data[index++] = (byte)(us & 0xff);
						data[index++] = (byte)(us >> 8);
					}
					return;
				}
			}
			int idx = index;
			AddCount(0x20, 3, v.Length);
			bool unicode = false;
			foreach (char c in v) {
				if (c < 0x80)
					data[index++] = (byte)c;
				else {
					if (c < 0x800) {
						data[index++] = (byte)(0xc0 | (c >> 6 & 0x1f));
						data[index++] = (byte)(0x80 | (c & 0x3f));
					} else {
						data[index++] = (byte)(0xe0 | (c >> 12 & 0xf));
						data[index++] = (byte)(0x80 | (c >> 6 & 0x3f));
						data[index++] = (byte)(0x80 | (c & 0x3f));
					}
					if (!unicode)
						data[idx] = (byte)(data[idx] ^ 0x60);
					unicode = true;
				}
			}
			if (stringCounter <= 0xffff && v.Length > 0 && v.Length < 256)
				usedStrings[v] = stringCounter++;
		}
	}

	void Add(byte v) {
		unchecked {
			if (data.Length < index + 2)
				IncreaseDataSize(2);
			data[index++] = 3;
			data[index++] = v;
		}
	}
	
	void Add(char v) {
		unchecked {
			if (data.Length < index + 3)
				IncreaseDataSize(3);
			data[index++] = 4;
			data[index++] = (byte)((int)v & 0xff);
			data[index++] = (byte)((int)v >> 8);
		}
	}
	
	void Add(short v) {
		unchecked {
			if (data.Length < index + 3)
				IncreaseDataSize(3);
			data[index++] = 5;
			data[index++] = (byte)(v & 0xff);
			data[index++] = (byte)(v >> 8);
		}
	}
	
	void Add(ushort v) {
		unchecked {
			if (data.Length < index + 3)
				IncreaseDataSize(3);
			data[index++] = 6;
			data[index++] = (byte)(v & 0xff);
			data[index++] = (byte)(v >> 8);
		}
	}

	void Add(int v) {
		unchecked {
			if (data.Length < index + 5)
				IncreaseDataSize(5);
			if (v >= 0 && v < 256) {
				data[index++] = 7;
				data[index++] = (byte)v;
			} else if (v >= -32768 && v < 32768) {
				data[index++] = 8;
				data[index++] = (byte)(v & 0xff);
				data[index++] = (byte)(v >> 8 & 0xff);
			} else {
				data[index++] = 9;
				data[index++] = (byte)(v & 0xff);
				data[index++] = (byte)(v >> 8 & 0xff);
				data[index++] = (byte)(v >> 16 & 0xff);
				data[index++] = (byte)(v >> 24 & 0xff);
			}
		}
	}

	void Add(uint v) {
		unchecked {
			if (data.Length < index + 5)
				IncreaseDataSize(5);
			if (v < 65536) {
				data[index++] = 0xa;
				data[index++] = (byte)(v & 0xff);
				data[index++] = (byte)(v >> 8);
			} else {
				data[index++] = 0xb;
				data[index++] = (byte)(v & 0xff);
				data[index++] = (byte)(v >> 8 & 0xff);
				data[index++] = (byte)(v >> 16 & 0xff);
				data[index++] = (byte)(v >> 24 & 0xff);
			}
		}
	}
	
	void Add(long v) {
		unchecked {
			if (data.Length < index + 9)
				IncreaseDataSize(9);
			byte[] d = System.BitConverter.GetBytes(v);
			data[index++] = 0xc;
			for (int i=0; i<d.Length; i++)
				data[index++] = d[i];
		}
	}

	void Add(ulong v) {
		unchecked {
			if (data.Length < index + 9)
				IncreaseDataSize(9);
			byte[] d = System.BitConverter.GetBytes(v);
			data[index++] = 0xd;
			for (int i=0; i<d.Length; i++)
				data[index++] = d[i];
		}
	}
	
	void Add(float v) {
		unchecked {
			if (data.Length < index + 5)
				IncreaseDataSize(5);
			data[index++] = 0xe;
			byte[] d = System.BitConverter.GetBytes(v);
			for (int i=0; i<d.Length; i++)
				data[index++] = d[i];
		}
	}
	
	void Add(double v) {
		unchecked {
			if (data.Length < index + 9)
				IncreaseDataSize(9);
			data[index++] = 0xf;
			byte[] d = System.BitConverter.GetBytes(v);
			for (int i=0; i<d.Length; i++)
				data[index++] = d[i];
		}
	}
	
	void Add(System.DateTime dateTime) {
		unchecked {
			if (data.Length < index + 5)
				IncreaseDataSize(5);
			DateTime begin = new DateTime(1980, 1, 1, 0, 0, 0, 0);
			TimeSpan diff = dateTime.ToUniversalTime() - begin;
			uint t = (uint)(diff.TotalSeconds);
			data[index++] = 0x10;
			data[index++] = (byte)(t & 0xff);
			data[index++] = (byte)((t >> 8) & 0xff);
			data[index++] = (byte)((t >> 16) & 0xff);
			data[index++] = (byte)((t >> 24) & 0xff);
		}
	}
	
	void Add(Vector2 v) {
		unchecked {
			if (data.Length < index + 9)
				IncreaseDataSize(9);
			data[index++] = 0x11;
			AddValue(v);
		}
	}
	
	void Add(Vector3 v) {
		unchecked {
			if (data.Length < index + 13)
				IncreaseDataSize(13);
			data[index++] = 0x12;
			AddValue(v);
		}
	}
	
	void Add(Vector4 v) {
		unchecked {
			if (data.Length < index + 17)
				IncreaseDataSize(17);
			data[index++] = 0x13;
			AddValue(v);
		}
	}
	
	void Add(Color v) {
		unchecked {
			if (data.Length < index + 5)
				IncreaseDataSize(5);
			data[index++] = 0x14;
			data[index++] = (byte)(v.r * 255);
			data[index++] = (byte)(v.g * 255);
			data[index++] = (byte)(v.b * 255);
			data[index++] = (byte)(v.a * 255);
		}
	}
	
	void Add(Color32 v) {
		unchecked {
			if (data.Length < index + 5)
				IncreaseDataSize(5);
			data[index++] = 0x15;
			data[index++] = v.r;
			data[index++] = v.g;
			data[index++] = v.b;
			data[index++] = v.a;
		}
	}
	
	void Add(Rect v) {
		unchecked {
			if (data.Length < index + 17)
				IncreaseDataSize(17);
			data[index++] = 0x16;
			AddValue (v.xMin);
			AddValue (v.yMin);
			AddValue (v.width);
			AddValue (v.height);
		}
	}
	
	void Add(Bounds v) {
		unchecked {
			if (data.Length <= index + 26)
				IncreaseDataSize(26);
			data[index++] = 0x17;
			data[index++] = 0x00;
			AddValue (v.center);
			AddValue (v.size);
		}
	}
	
	void Add(Quaternion v) {
		unchecked {
			if (data.Length <= index + 18)
				IncreaseDataSize(18);
			data[index++] = 0x17;
			data[index++] = 0x01;
			AddValue (v.x);
			AddValue (v.y);
			AddValue (v.z);
			AddValue (v.w);
		}
	}

	void Add(ArrayList v) {
		unchecked {
			AddCount(0x80, 2, v.Count);
			for (int i=0; i<v.Count; i++)
				Add(v[i]);
		}
	}

	void Add(Queue v) {
		unchecked {
			AddCount(0x60, 2, v.Count);
			data[index++] = 0x27;
			foreach (object o in v)
				Add (o);
		}
	}
	
	void Add(Stack v) {
		unchecked {
			AddCount(0x60, 2, v.Count);
			data[index++] = 0x28;
			object[] array = new object[v.Count];
			v.CopyTo(array, 0);
			System.Array.Reverse(array);
			for (int i=0; i<array.Length; i++)
				Add (array[i]);
		}
	}

	void Add(Hashtable v) {
		unchecked {
			AddCount(0xc0, 6, v.Count);
			foreach (object key in v.Keys) {
				if (key is string)
					Add((string)key);
				else
					Add(key);
				Add(v[key]);
			}
		}
	}

	void AddValue(float v) {
		unchecked {
			byte[] d = System.BitConverter.GetBytes(v);
			for (int i=0; i<d.Length; i++)
				data[index++] = d[i];
		}
	}

	void AddValue(Vector2 v) {
		AddValue(v.x);
		AddValue(v.y);
	}
	
	void AddValue(Vector3 v) {
		AddValue(v.x);
		AddValue(v.y);
		AddValue(v.z);
	}

	void AddValue(Vector4 v) {
		AddValue(v.x);
		AddValue(v.y);
		AddValue(v.z);
		AddValue(v.w);
	}

	void IncreaseDataSize(int requiredBytes) {
		unchecked {
			int newSize = data.Length * 2;
			if (newSize < data.Length + requiredBytes)
				newSize = data.Length + requiredBytes;
			System.Array.Resize<byte>(ref data, newSize);
		}
	}

	void AddCount(byte code, int osize, int count) {
		unchecked {
			if (data.Length < index + 5 + osize * count)
				IncreaseDataSize(5 + osize * count);
			if (count < 16) // 0..16
				data[index++] = (byte)(code | count);
			else {
				if (count < 0x7ff) // 0..7ff
					data[index++] = (byte)(code | 0x10 | (count >> 8));
				else {
					if (count < 0x3fffff) // 0..3fffff
						data[index++] = (byte)(code | 0x18 | (count >> 16));
					else { // 0..3fffffff
						data[index++] = (byte)(code | 0x1c | (count >> 24));
						data[index++] = (byte)((count >> 16) & 0xff);
					}
					data[index++] = (byte)((count >> 8) & 0xff);
				}
				data[index++] = (byte)(count & 0xff);
			}
		}
	}

	void AddType(Type t) {
		unchecked {
			if (data.Length < index + 2)
				IncreaseDataSize(2);

			if (t.IsArray) {
				data[index++] = (byte)(0x38 + t.GetArrayRank() - 1);
				AddType (t.GetElementType());
				return;
			#if BINMSG_GENERICS
			} else if (t.IsGenericType) {
				Type genericType = t.GetGenericTypeDefinition();
				if (genericType == typeof(List<>)) {
					data[index++] = 0x33;
					AddType (t.GetGenericArguments()[0]);
				} else if (genericType == typeof(Dictionary<,>)) {
					data[index++] = 0x32;
					AddType (t.GetGenericArguments()[0]);
					AddType (t.GetGenericArguments()[1]);
				} else if (genericType == typeof(Queue<>)) {
					data[index++] = 0x35;
					AddType (t.GetGenericArguments()[0]);
				} else if (genericType == typeof(Stack<>)) {
					data[index++] = 0x36;
					AddType (t.GetGenericArguments()[0]);
				} else if (genericType == typeof(HashSet<>)) {
					data[index++] = 0x37;
					AddType (t.GetGenericArguments()[0]);
				}
			#endif
			} else if (t == typeof(object))
				data[index++] = 0;
			else if (t == typeof(bool))
				data[index++] = 1;
			else if (t == typeof(byte))
				data[index++] = 3;
			else if (t == typeof(char))
				data[index++] = 4;
			else if (t == typeof(short))
				data[index++] = 5;
			else if (t == typeof(ushort))
				data[index++] = 6;
			else if (t == typeof(int))
				data[index++] = 9;
			else if (t == typeof(uint))
				data[index++] = 0xb;
			else if (t == typeof(long))
				data[index++] = 0xc;
			else if (t == typeof(ulong))
				data[index++] = 0xd;
			else if (t == typeof(float))
				data[index++] = 0xe;
			else if (t == typeof(double))
				data[index++] = 0xf;
			else if (t == typeof(DateTime))
				data[index++] = 0x10;
			else if (t == typeof(Vector2))
				data[index++] = 0x11;
			else if (t == typeof(Vector3))
				data[index++] = 0x12;
			else if (t == typeof(Vector4))
				data[index++] = 0x13;
			else if (t == typeof(Color))
				data[index++] = 0x14;
			else if (t == typeof(Color32))
				data[index++] = 0x15;
			else if (t == typeof(Rect))
				data[index++] = 0x16;
			else if (t == typeof(Bounds)) {
				data[index++] = 0x18;
				data[index++] = 0x00;
			} else if (t == typeof(Quaternion)) {
				data[index++] = 0x19;
				data[index++] = 0x01;
			} else if (t == typeof(string))
				data[index++] = 0x30;
			else if (t == typeof(ArrayList))
				data[index++] = 0x34;
			else if (t == typeof(Hashtable))
				data[index++] = 0x31;
			else if (t == typeof(Queue))
				data[index++] = 0x3c;
			else if (t == typeof(Stack))
				data[index++] = 0x3d;
			else {
				data[index++] = 0x1f;
				Add (t.Assembly.FullName);
				Add (t.FullName);
			}
		}
	}

	void AddArray(Type t, object o) {
		unchecked {
			Array a = (Array)o;
			AddCount(0x60, 2, a.GetLength(0));
			Type et = t.GetElementType();
			if (a.Rank > 1) {
				data[index++] = (byte)(0x20 + a.Rank - 1);
				for (int i=1; i<a.Rank; i++)
					Add ((int)a.GetLength(i));
				AddType(et);
				foreach (object obj in a)
					Add (obj);
			} else {
				if (et == typeof(bool))
					Add ((bool[])o);
				else if (et == typeof(byte))
					Add ((byte[])o);
				else if (et == typeof(int))
					Add ((int[])o);
				else if (et == typeof(string))
					Add ((string[])o);
				else if (et == typeof(float))
					Add ((float[])o);
				else if (et == typeof(Vector2))
					Add ((Vector2[])o);
				else if (et == typeof(Vector3))
					Add ((Vector3[])o);
				else if (et == typeof(Color))
					Add ((Color[])o);
				else if (et == typeof(Color32))
					Add ((Color32[])o);
				else if (et == typeof(object)) {
					data[index++] = 0;
					object[] v = (object[])o;
					for (int i=0; i<v.Length; i++)
						Add(v[i]);
				} else {
					AddType(et);
					for (int i=0; i<a.Length; i++)
						Add (a.GetValue(i));
				}
			}
		}
	}

	void Add(bool[] o) {
		unchecked {
			data[index++] = 1;
			for (int i=0; i<o.Length; i++)
				data[index++] = (byte)(o[i] ? 1 : 2);
		}
	}
	
	void Add(byte[] o) {
		unchecked {
			data[index++] = 3;
			System.Array.Copy(o, 0, data, index, o.Length);
			index+= o.Length;
		}
	}
	
	void Add(int[] o) {
		unchecked {
			data[index++] = 9;
			for (int i=0; i<o.Length; i++) {
				int v = o[i];
				if (v >= 0 && v < 256) {
					data[index++] = 7;
					data[index++] = (byte)v;
				} else if (v >= -32768 && v < 32768) {
					data[index++] = 8;
					data[index++] = (byte)(v & 0xff);
					data[index++] = (byte)(v >> 8 & 0xff);
				} else {
					data[index++] = 9;
					data[index++] = (byte)(v & 0xff);
					data[index++] = (byte)(v >> 8 & 0xff);
					data[index++] = (byte)(v >> 16 & 0xff);
					data[index++] = (byte)(v >> 24 & 0xff);
				}
			}
		}
	}
	
	void Add(string[] o) {
		unchecked {
			data[index++] = 0x30;
			for (int i=0; i<o.Length; i++)
				Add (o[i]);
		}
	}
	
	void Add(float[] o) {
		unchecked {
			data[index++] = 0xe;
			for (int i=-0; i<o.Length; i++)
				AddValue(o[i]);
		}
	}

	void Add(Vector2[] o) {
		unchecked {
			data[index++] = 0x11;
			for (int i=-0; i<o.Length; i++)
				AddValue(o[i]);
		}
	}
	
	void Add(Vector3[] o) {
		unchecked {
			data[index++] = 0x12;
			for (int i=-0; i<o.Length; i++)
				AddValue(o[i]);
		}
	}
	
	void Add(Color[] o) {
		unchecked {
			data[index++] = 0x14;
			foreach (Color v in o) {
				data[index++] = (byte)(v.r * 255);
				data[index++] = (byte)(v.g * 255);
				data[index++] = (byte)(v.b * 255);
				data[index++] = (byte)(v.a * 255);
			}
		}
	}
	
	void Add(Color32[] o) {
		unchecked {
			data[index++] = 0x15;
			foreach (Color32 v in o) {
				data[index++] = v.r;
				data[index++] = v.g;
				data[index++] = v.b;
				data[index++] = v.a;
			}
		}
	}

#if BINMSG_GENERICS
	void AddGeneric(Type t, object o) {
		unchecked {
			Type genericType = t.GetGenericTypeDefinition();
			if (genericType == typeof(List<>))
				// List<>
				Add (t.GetGenericArguments()[0], (IList)o);
			else if (genericType == typeof(Dictionary<,>))
				// Dictionary<,>
				Add (t.GetGenericArguments()[0], t.GetGenericArguments()[1], (IDictionary)o);
			else if (genericType == typeof(Queue<>) || genericType == typeof(Stack<>))
				// Queue<>, Stack<>
				Add (genericType, t.GetGenericArguments()[0], (ICollection)o);
			else if (genericType == typeof(HashSet<>))
				// HashSet<>
				Add (genericType, t.GetGenericArguments()[0], (IEnumerable)o);
			else
				// Unsupported type
				Add((object)null);
		}
	}
#endif
	
	void Add(Type t, IList o) {
		unchecked {
			AddCount(0xa0, 2, o.Count);
			AddType(t);
			for (int i=0; i<o.Count; i++)
				Add (o[i]);
		}
	}

	void Add(Type genericType, Type t, ICollection o) {
		unchecked {
			AddCount(0x60, 2, o.Count);
			if (genericType == typeof(Queue<>))
				data[index++] = 0x24;
			else if (genericType == typeof(Stack<>))
				data[index++] = 0x25;
			AddType(t);
			foreach (object obj in o)
				Add (obj);
		}
	}

	void Add(Type genericType, Type t, IEnumerable o) {
		unchecked {
			ArrayList array = new ArrayList();
			IEnumerator e = o.GetEnumerator();
			while (e.MoveNext())
				array.Add(e.Current);
			AddCount(0x60, 2, array.Count);
			if (genericType == typeof(HashSet<>))
				data[index++] = 0x26;
			AddType(t);
			foreach (object obj in array)
				Add (obj);
		}
	}

	void Add(Type tk, Type tv, IDictionary o) {
		unchecked {
			AddCount(0xe0, 2, o.Count);
			AddType(tk);
			AddType(tv);
			foreach (DictionaryEntry e in o) {
				Add (e.Key);
				Add (e.Value);
			}
		}
	}

	/*
	 * Decode
	 */
	object DecodeValue(byte[] data) {
		this.data = data;
		int stringsCount = (data[0] + 1) << 8;
		int objectsCount = (data[1] + 1) << 8;
		strings = new string[stringsCount];
		objects = new object[objectsCount];
		index = 2;
		return Get();
	}

	object Get() {
		unchecked {
			byte b = data[index++];
			if (b < 0x1b) {
				// simple types
				switch (b) {
				case 0: return null;
				case 1: return true;
				case 2: return false;
				case 3: return (byte)data[index++];
				case 4: return (char)GetShort();
				case 5: return GetShort();
				case 6: return GetUShort();
				case 7: return (int)data[index++];
				case 8: return (int)GetShort();
				case 9: return GetInt32();
				case 0xa: return (uint)GetUShort();
				case 0xb: return GetUInt();
				case 0xc: return GetLong();
				case 0xd: return GetULong();
				case 0xe: return GetFloat();
				case 0xf: return GetDouble();
				case 0x10: return GetDate();
				case 0x11: return new Vector2(GetFloat(), GetFloat());
				case 0x12: return GetVector3();
				case 0x13: return new Vector4(GetFloat(), GetFloat(), GetFloat(), GetFloat());
				case 0x14: return new Color(data[index++] / 255.0f, data[index++] / 255.0f, data[index++] / 255.0f, data[index++] / 255.0f);
				case 0x15: return new Color32(data[index++], data[index++], data[index++], data[index++]);
				case 0x16: return new Rect(GetFloat(), GetFloat(), GetFloat(), GetFloat());
				case 0x17: return GetOtherObject();
				case 0x18: return objects[GetInt(data[index++])];
				case 0x19: return GetObject();
				case 0x1a: return null;
				}
			} else if (b < 0x20) {
				// string reference
				return strings[data[index++] | ((b == 0x1b ? data[index++] : (b & 3)) << 8)];
			} else {
				int cnt = b & 0x1f;
				if (cnt >= 0x10) {
					if (cnt < 0x18)
						cnt = ((b & 7) << 8) | data[index++];
					else if (cnt < 0x1c)
						cnt = ((b & 3) << 16) | (data[index++] << 8) | data[index++];
					else
						cnt = ((b & 3) << 24) | (data[index++] << 16) | (data[index++] << 8) | data[index++];
				}
				switch (b & 0xe0) {
				case 0x20:
					string r = System.Text.Encoding.ASCII.GetString(data, index, cnt);
					index+= cnt;
					if (stringCounter <= 0xffff && r.Length > 0 && r.Length < 256)
						strings[stringCounter++] = r;
					return r;
				case 0x40: return GetUnicode(cnt);
				case 0x60: return GetArray(cnt);
				case 0x80: return GetArrayList(cnt);
				case 0xc0: return GetHashtable(cnt);
				#if BINMSG_GENERICS
				case 0xa0: return GetList(cnt);
				case 0xe0: return GetDictionary(cnt);
				#endif
				}
			}
		}
		return null;
	}

	object GetOtherObject() {
		switch (data[index++]) {
		case 0x00: return new Bounds(GetVector3(), GetVector3());
		case 0x01: return new Quaternion(GetFloat(), GetFloat(), GetFloat(), GetFloat());
		}
		return null;
	}

	string GetUnicode(int count) {
		unchecked {
			char[] res = new char[count];
			for (int i=0; i<count; i++) {
				char c = (char)data[index++];
				if (c > 0x7f) {
					if (c < 0xe0)
						c = (char)(((c & 0x3f) << 6) | (data[index++] & 0x3f));
					else
						c = (char)(((c & 0x3f) << 12) | ((data[index++] & 0x3f) << 6) | (data[index++] & 0x3f));
				}
				res[i] = c;
			}
			string s = new string(res);
			if (stringCounter <= 0xffff && s.Length > 0 && s.Length < 256)
				strings[stringCounter++] = s;
			return s;
		}
	}

	int GetInt(byte b) {
		unchecked {
			if (b == 7)
				return data[index++];
			else if (b == 8)
				return data[index++] | (data[index++] << 8);
			else
				return data[index++] | (data[index++] << 8) | (data[index++] << 16) | (data[index++] << 24);
		}
	}

	short GetShort() {
		unchecked {
			return (short)(data[index++] | (data[index++] << 8));
		}
	}
	
	ushort GetUShort() {
		unchecked {
			return (ushort)((ushort)data[index++] | ((ushort)data[index++] << 8));
		}
	}
	
	int GetInt32() {
		unchecked {
			return data[index++] | (data[index++] << 8) | (data[index++] << 16) | (data[index++] << 24);
		}
	}
	
	uint GetUInt() {
		unchecked {
			return (uint)data[index++] | ((uint)data[index++] << 8) | ((uint)data[index++] << 16) | ((uint)data[index++] << 24);
		}
	}
	
	float GetFloat() {
		unchecked {
			float res = System.BitConverter.ToSingle(data, index);
			index+= 4;
			return res;
		}
	}
	
	double GetDouble() {
		unchecked {
			double res = System.BitConverter.ToDouble(data, index);
			index+= 8;
			return res;
		}
	}
	
	long GetLong() {
		unchecked {
			long res = System.BitConverter.ToInt64(data, index);
			index+= 8;
			return res;
		}
	}
	
	ulong GetULong() {
		unchecked {
			ulong res = System.BitConverter.ToUInt64(data, index);
			index+= 8;
			return res;
		}
	}
	
	System.DateTime GetDate() {
		unchecked {
			uint t = (uint)data[index++] | ((uint)data[index++] << 8) | ((uint)data[index++] << 16) | ((uint)data[index++] << 24);
			System.DateTime dateTime = new DateTime(1980, 1, 1, 0, 0, 0, 0).AddSeconds(t).ToLocalTime();
			return dateTime;
		}
	}

	Vector3 GetVector3() {
		return new Vector3(GetFloat(), GetFloat(), GetFloat());
	}

	ArrayList GetArrayList(int count) {
		ArrayList arrayList = new ArrayList(count);
		for (int i=0; i<count; i++)
			arrayList.Add(Get ());
		return arrayList;
	}

	Hashtable GetHashtable(int count) {
		unchecked {
			Hashtable h = new Hashtable(count);
			for (int i=0; i<count; i++) {
				object key = Get();
				h[key] = Get();
			}
			return h;
		}
	}

	#if BINMSG_GENERICS
	IList GetList(int count) {
		unchecked {
			Type t = GetType(data[index++]);
			IList list = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(t), count);
			for (int i=0; i<count; i++)
				list.Add(Get());
			return list;
		}
	}
	
	IDictionary GetDictionary(int count) {
		unchecked {
			Type kt = GetType(data[index++]);
			Type vt = GetType(data[index++]);
			IDictionary dict = (IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(kt, vt), count);
			for (int i=0; i<count; i++)
				dict.Add(Get(), Get());
			return dict;
		}
	}
	#endif

	object GetObject() {
		if (types == null)
			types = new Dictionary<string, Type>();
		string assemblyName = (string)Get();
		string typeName = (string)Get();
		int cnt = GetInt(data[index++]);
		
		Type type;
		if (!types.TryGetValue(typeName, out type)) {
			System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(assemblyName);
			type = assembly.GetType(typeName);
			types[typeName] = type;
		}
		object o = Activator.CreateInstance(type);
		for (int i=0; i<cnt; i++) {
			System.Reflection.FieldInfo t = type.GetField((string)Get());
			t.SetValue(o, Get());
		}
		if (objects.Length <= objectCounter)
			System.Array.Resize<object>(ref objects, objects.Length * 2);
		objects[objectCounter++] = o;
		return o;
	}

	Type GetType(byte t) {
		unchecked {
			switch (t) {
			case 0: return typeof(object);
			case 1: return typeof(bool);
			case 3: return typeof(byte);
			case 4: return typeof(char);
			case 5: return typeof(short);
			case 6: return typeof(ushort);
			case 7: return typeof(int);
			case 8: return typeof(int);
			case 9: return typeof(int);
			case 0xa: return typeof(uint);
			case 0xb: return typeof(uint);
			case 0xc: return typeof(long);
			case 0xd: return typeof(ulong);
			case 0xe: return typeof(float);
			case 0xf: return typeof(double);
			case 0x10: return typeof(System.DateTime);
			case 0x11: return typeof(Vector2);
			case 0x12: return typeof(Vector3);
			case 0x13: return typeof(Vector4);
			case 0x14: return typeof(Color);
			case 0x15: return typeof(Color32);
			case 0x16: return typeof(Rect);
			case 0x17:
				switch (data[index++]) {
				case 0: return typeof(Bounds);
				case 1: return typeof(Quaternion);
				}
				return null;
			case 0x1f:
			case 0x20:
				if (types == null) {
					types = new Dictionary<string, Type>();
					objects = new object[1024];
				}
				string assemblyName = (string)Get();
				string typeName = (string)Get();
				Type type = null;
				if (!types.TryGetValue(typeName, out type)) {
					System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(assemblyName);
					type = assembly.GetType(typeName);
					types[typeName] = type;
				}
				return type;
			case 0x30: return typeof(string);
			case 0x31: return typeof(Hashtable);
			#if BINMSG_GENERICS
			case 0x32: return typeof(Dictionary<,>).MakeGenericType(GetType(data[index++]), GetType(data[index++]));
			case 0x33: return typeof(List<>).MakeGenericType(GetType(data[index++]));
			case 0x35: return typeof(Queue<>).MakeGenericType(GetType(data[index++]));
			case 0x36: return typeof(Stack<>).MakeGenericType(GetType(data[index++]));
			case 0x37: return typeof(HashSet<>).MakeGenericType(GetType(data[index++]));
			#endif
			case 0x34: return typeof(ArrayList);
			case 0x38: return GetType(data[index++]).MakeArrayType();
			case 0x39: return GetType(data[index++]).MakeArrayType(2);
			case 0x3a: return GetType(data[index++]).MakeArrayType(3);
			case 0x3b: return GetType(data[index++]).MakeArrayType(4);
			case 0x3c: return typeof(Queue);
			case 0x3d: return typeof(Stack);
			}
			return null;
		}
	}

	object GetArray(int count) {
		byte t = data[index++];
		if (t >= 0x20 && t <= 0x23) {
			// multidim array
			int rank = t - 0x20 + 1;
			Array array;
			int cnt2 = GetInt(data[index++]);
			if (rank == 2) {
				array = Array.CreateInstance(GetType(data[index++]), count, cnt2);
				for (int i0=0; i0<count; i0++)
					for (int i1=0; i1<cnt2; i1++)
						array.SetValue(Get (), i0, i1);
			} else if (rank == 3) {
				int cnt3 = GetInt(data[index++]);
				array = Array.CreateInstance(GetType(data[index++]), count, cnt2, cnt3);
				for (int i0=0; i0<count; i0++)
					for (int i1=0; i1<cnt2; i1++)
						for (int i2=0; i2<cnt2; i2++)
							array.SetValue(Get (), i0, i1, i2);
			} else {
				int cnt3 = GetInt(data[index++]);
				int cnt4 = GetInt(data[index++]);
				array = Array.CreateInstance(GetType(data[index++]), count, cnt2, cnt3, cnt4);
				for (int i0=0; i0<count; i0++)
					for (int i1=0; i1<cnt2; i1++)
						for (int i2=0; i2<cnt2; i2++)
							for (int i3=0; i3<cnt2; i3++)
								array.SetValue(Get (), i0, i1, i2, i3);
			}
			return array;
		} else if (t >= 0x24 && t <= 0x2f) {
			// 24 - arraytype: Queue<>
			// 25 - arraytype: Stack<>
			// 26 - arraytype: HashSet<>
			// 27 - arraytype: Queue
			// 28 - arraytype: Stack
			if (t >= 0x24 && t <= 0x26) {
				#if BINMSG_GENERICS
				Type subType = GetType(data[index++]);
				IList list = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(subType), count);
				for (int i=0; i<count; i++)
					list.Add(Get());
				switch (t) {
				case 0x24:
					return (object)Activator.CreateInstance(typeof(System.Collections.Generic.Queue<>).MakeGenericType(subType), list);
				case 0x25:
					// reverse
					for (int i=0; i<count/2; i++) {
						object so = list[i];
						list[i] = list[count - 1 - i];
						list[count - 1 - i] = so;
					}
					return (object)Activator.CreateInstance(typeof(System.Collections.Generic.Stack<>).MakeGenericType(subType), list);
				case 0x26:
					return (object)Activator.CreateInstance(typeof(System.Collections.Generic.HashSet<>).MakeGenericType(subType), list);
				}
				#endif
			} else if (t == 0x27) {
				Queue queue = new Queue(count);
				for (int i=0; i<count; i++)
					queue.Enqueue(Get());
				return queue;
			} else if (t == 0x28) {
				Stack stack = new Stack(count);
				for (int i=0; i<count; i++)
					stack.Push(Get());
				return stack;
			}
			return null;
		}

		switch (t) {
		case 0: // object[]
			object[] objs = new object[count];
			for (int i=0; i<count; i++)
				objs[i] = Get();
			return objs;
		case 1: // bool[]
			bool[] bools = new bool[count];
			for (int i=0; i<count; i++)
				bools[i] = data[index++] == 1;
			return bools;
		case 3: // byte[]
			byte[] bytes = new byte[count];
			System.Array.Copy(data, index, bytes, 0, count);
			return bytes;
		case 9: // int[]
			int[] ints = new int[count];
			for (int i=0; i<count; i++) {
				switch (data[index++]) {
				case 7: ints[i] = data[index++]; break;
				case 8: ints[i] = data[index++] | (data[index++] << 8); break;
				case 9: ints[i] = data[index++] | (data[index++] << 8) | (data[index++] << 16) | (data[index++] << 24); break;
				}
			}
			return ints;
		case 0xf: // float[]
			float[] floats = new float[count];
			for (int i=0; i<count; i++)
				floats[i] = GetFloat();
			return floats;
		case 0x12: // Vector2[]
			Vector2[] v2s = new Vector2[count];
			for (int i=0; i<count; i++)
				v2s[i] = new Vector2(GetFloat(), GetFloat());
			return v2s;
		case 0x13: // Vector3[]
			Vector3[] v3s = new Vector3[count];
			for (int i=0; i<count; i++)
				v3s[i] = new Vector3(GetFloat(), GetFloat(), GetFloat());
			return v3s;
		case 0x15: // Color[]
			Color[] cs = new Color[count];
			for (int i=0; i<count; i++)
				cs[i] = new Color(data[index++] / 255.0f, data[index++] / 255.0f, data[index++] / 255.0f, data[index++] / 255.0f);
			return cs;
		case 0x16: // Color32[]
			Color32[] cs32 = new Color32[count];
			for (int i=0; i<count; i++)
				cs32[i] = new Color32(data[index++], data[index++], data[index++], data[index++]);
			return cs32;
		case 0x30: // string[]
			string[] strs = new string[count];
			for (int i=0; i<count; i++)
				strs[i] = (string)Get();
			return strs;
		default:
			Array array = Array.CreateInstance(GetType(t), count);
			for (int i=0; i<count; i++)
				array.SetValue(Get (), i);
			return array;
		}
	}
}
