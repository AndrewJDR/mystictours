using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class Plist {
	public struct KeyValue {
		public string Key;
		public int Value;
	}

	public byte[] data;
	int[] offsets;
	string[] strings;
	int refSize, rootObjectOffset;

	/*
	 * encode
	 */
	int index = 0;
	int offset = 0;
	int stringCount = 0, stringsSize = 0;
	Dictionary<string, int> usedStrings;
	
	public static byte[] Encode(object o) {
		return new Plist(o).data;
	}
	
	public static object Decode(byte[] data) {
		return new Plist(data).Decode();
	}

	public Plist(object obj) {
		int objectCount = ObjectCount(obj);
		if (objectCount < 256) refSize = 0;
		else if (objectCount < 65536) refSize = 1;
		else refSize = 2;

		index = 0;
		offset = 0;
		data = new byte[64 + objectCount * 6 + stringsSize];
		offsets = new int[objectCount];
		usedStrings = new Dictionary<string, int>(stringCount);
		foreach(char c in "bplist00")
			data[index++] = (byte)c;

		rootObjectOffset = AddValue(obj);
		usedStrings = null;
		strings = new string[offset];
		
		int offsetTableOffset = index;
		int offsetByteSize = index <= 0xff ? 0 : (index <= 0xffff ? 1 : (index <= 0xffffff ? 2 : 3));

		if (data.Length < index + (offsetByteSize + 1) * offset + 32)
			System.Array.Resize<byte>(ref data, index + (offsetByteSize + 1) * offset + 32);

		if (offsetByteSize == 0) {
			for (int i=0; i<offset; i++)
				data[index++] = (byte)offsets[i];
		} else if (offsetByteSize == 1) {
			for (int i=0; i<offset; i++) {
				int v = offsets[i];
				data[index++] = (byte)(v >> 8);
				data[index++] = (byte)(v & 0xff);
			}
		} else if (offsetByteSize == 2) {
			for (int i=0; i<offset; i++) {
				int v = offsets[i];
				data[index++] = (byte)(v >> 16);
				data[index++] = (byte)((v >> 8) & 0xff);
				data[index++] = (byte)(v & 0xff);
			}
		} else {
			for (int i=0; i<offset; i++) {
				int v = offsets[i];
				data[index++] = (byte)(v >> 24);
				data[index++] = (byte)((v >> 16) & 0xff);
				data[index++] = (byte)((v >> 8) & 0xff);
				data[index++] = (byte)(v & 0xff);
			}
		}
		for (int i=0; i<32; i++)
			data[index++] = 0;
		
		if (data.Length > index)
			System.Array.Resize<byte>(ref data, index);

		int t = data.Length - 32;
		data[t + 6] = (byte)(offsetByteSize + 1);
		data[t + 7] = (byte)(refSize + 1);
		
		data[t + 15] = (byte)(offset & 0xff);
		data[t + 14] = (byte)((offset >> 8) & 0xff);
		data[t + 13] = (byte)((offset >> 16) & 0xff);
		data[t + 12] = (byte)((offset >> 24) & 0xff);

		data[t + 31] = (byte)(offsetTableOffset & 0xff);
		data[t + 30] = (byte)((offsetTableOffset >> 8) & 0xff);
		data[t + 29] = (byte)((offsetTableOffset >> 16) & 0xff);
		data[t + 28] = (byte)((offsetTableOffset >> 24) & 0xff);

		data[t + 23] = (byte)(rootObjectOffset & 0xff);
		data[t + 22] = (byte)((rootObjectOffset >> 8) & 0xff);
		data[t + 21] = (byte)((rootObjectOffset >> 16) & 0xff);
		data[t + 20] = (byte)((rootObjectOffset >> 24) & 0xff);
	}

	int ObjectCount(object o) {
		int cnt = 1;
		if (o is Hashtable) {
			Hashtable h = (Hashtable)o;
			cnt+= h.Count;
			stringsSize+= h.Count * 4;
			stringCount+= h.Count;
			foreach (object c in h.Values)
				cnt+= ObjectCount(c);
		} else if (o is ArrayList) {
			ArrayList a = (ArrayList)o;
			stringsSize+= a.Count * 2;
			for (int i=0; i<a.Count; i++)
				cnt+= ObjectCount(a[i]);
		} else if (o is string) {
			stringsSize+= ((string)o).Length;
			stringCount++;
		}
		return cnt;
	}

	int AddValue(object o) {
		if (o == null)
			return AddNull();
		else if (o is Hashtable)
			return AddDict((Hashtable)o);
		else if (o is ArrayList)
			return AddArray((ArrayList)o);
		else if (o is string)
			return AddString((string)o);
		else if (o is int)
			return AddInt((int)o);
		else if (o is double)
			return AddNumber((double)o);
		else if (o is float)
			return AddNumber((double)(float)o);
		else if (o is byte[])
			return AddData((byte[])o);
		else if (o is System.DateTime)
			return AddDate((System.DateTime)o);
		else if (o is bool)
			return AddBool((bool)o);
		throw new Exception("Not found type for: " + o);
	}

	int AddNull() {
		int o = GetOffset(1);
		data[index++] = 0;
		return o;
	}
	
	int AddBool(bool v) {
		int o = GetOffset(1);
		data[index++] = (byte)(v ? 9 : 8);
		return o;
	}

	int AddDict(Hashtable h) {
		int cnt = 0;
		int[] refs = new int[h.Count * 2];
		foreach (string Key in h.Keys) {
			if (Key == null)
				continue;
			object v = h[Key];
			if (v == null)
				continue;
			refs[cnt] = AddString(Key);
			refs[cnt + h.Count] = AddValue(v);
			cnt++;
		}
		int o = GetOffset(2 * cnt * (refSize + 1) + 5);
		if (h.Count < 15)
			data[index++] = (byte)(0xd0 + cnt);
		else {
			data[index++] = 0xdf;
			AddCount(cnt);
		}
		for (int i=0; i<cnt; i++)
			AddRef(refs[i]);
		for (int i=0; i<cnt; i++)
			AddRef(refs[i + h.Count]);
		return o;
	}

	int AddArray(ArrayList array) {
		int cnt = 0;
		int[] refs = new int[array.Count];
		for (int i=0; i<array.Count; i++)
			if (array[i] != null)
				refs[cnt++] = AddValue(array[i]);
		int o = GetOffset(cnt * (refSize + 1) + 5);
		if (cnt < 15)
			data[index++] = (byte)(0xa0 + cnt);
		else {
			data[index++] = 0xaf;
			AddCount(cnt);
		}
		for (int i=0; i<cnt; i++)
			AddRef(refs[i]);
		return o;
	}

	int AddString(string str) {
		int o;
		if (str.Length < 256 && usedStrings.TryGetValue(str, out o))
			return o;
		o = GetOffset(str.Length + 5);
		if (str.Length < 256)
			usedStrings[str] = o;
		int i1 = index;
		if (str.Length < 15)
			data[index++] = (byte)(0x50 + str.Length);
		else {
			data[index++] = 0x5f;
			AddCount(str.Length);
		}
		int i2 = index;
		bool unicode = false;
		for (int i=0; i<str.Length; i++) {
			char c = str[i];
			if (c > 255) {
				unicode = true;
				break;
			}
			data[index++] = (byte)c;
		}
		if (unicode) {
			data[i1] = (byte)(str.Length < 15 ? 0x60 + str.Length : 0x6f);
			index = i2;
			for (int i=0; i<str.Length; i++) {
				data[index++] = (byte)(str[i] >> 8);
				data[index++] = (byte)(str[i] & 0xff);
			}
		}
		return o;
	}

	int AddInt(int i) {
		int o = GetOffset(5);
		AddCount(i);
		return o;
	}

	int AddNumber(double number) {
		int o = GetOffset(9);
		byte[] d = BitConverter.GetBytes(number);
		data[index++] = (byte)(0x20 | (d.Length == 8 ? 3 : 2));
		for (int i=0; i<d.Length; i++)
			data[index++] = d[d.Length - 1 - i];
		return o;
	}

	int AddData(byte[] d) {
		int o = GetOffset(5 + d.Length);
		if (d.Length < 15)
			data[index++] = (byte)(0x40 + d.Length);
		else {
			data[index++] = 0x4f;
			AddCount(d.Length);
		}
		for (int i=0; i<d.Length; i++)
			data[index++] = d[i];
		return o;
	}
	
	int AddDate(System.DateTime dateTime) {
		int o = GetOffset(9);
        DateTime begin = new DateTime(2001, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = dateTime.ToUniversalTime() - begin;
        double t = Math.Floor(diff.TotalSeconds);
		data[index++] = 0x33;
		byte[] d = BitConverter.GetBytes(t);
		for (int i=0; i<d.Length; i++)
			data[index++] = d[d.Length - 1 - i];
		return o;
	}
	
	void AddRef(int r) {
		if (refSize > 1)
			data[index++] = (byte)(r >> 16);
		if (refSize > 0)
			data[index++] = (byte)((r >> 8) & 0xff);
		data[index++] = (byte)(r & 0xff);
	}

	int GetOffset(int length) {
		int o = offset++;
		offsets[o] = index;
		if (index + length >= data.Length)
			System.Array.Resize<byte>(ref data, data.Length * 2);
		return o;
	}

	void AddCount(int i) {
		if (i >= 0 && i <= 0xff) {
			data[index++] = 0x10;
			data[index++] = (byte)i;
		} else {
			if (i < 0 || i > 0xffff) {
				data[index++] = 0x12;
				data[index++] = (byte)((i >> 24) & 0xff);
				data[index++] = (byte)((i >> 16) & 0xff);
			} else
				data[index++] = 0x11;
			data[index++] = (byte)((i >> 8) & 0xff);
			data[index++] = (byte)(i & 0xff);
		}
	}
	
	/*
	 * Decode
	 */
	public Plist(byte[] data) {
		this.data = data;
		InitDecoder();
	}

	public object Decode() {
		return GetValue(rootObjectOffset);
	}

	object GetValue(int index) {
		byte b = data[offsets[index]];
		switch (b & 0xf0) {
		case 0:
			if (b == 0)
				return null;
			return b == 9;
		case 0x10:
			return GetInt(index);
		case 0x20:
			return GetNumber(index);
		case 0x30:
			return GetDate(index);
		case 0x40:
			return GetData(index);
		case 0x50:
		case 0x60:
			return GetString(index);
		case 0xA0:
			return GetArrayList(index);
		case 0xD0:
			return GetHashtable(index);
		}
		return null;
	}

	void InitDecoder() {
		int t = data.Length - 32;
		int offsetByteSize = data[t + 6] - 1;
		refSize = (data[t + 7]) - 1;

		int refCount = (data[t + 12] << 24) | (data[t + 13] << 16) | (data[t + 14] << 8) | data[t + 15];
		int offsetTableOffset = (data[t + 28] << 24) | (data[t + 29] << 16) | (data[t + 30] << 8) | data[t + 31];
		offsets = new int[refCount];
		strings = new string[refCount];
		int p = offsetTableOffset;
		for (int i=0; i<refCount; i++) {
			int o = data[p++];
			for (int k=0; k<offsetByteSize; k++)
				o = (o << 8) | data[p++];
			offsets[i] = o;
		}
		rootObjectOffset = (data[t + 20] << 24) | (data[t + 21] << 16) | (data[t + 22] << 8) | data[t + 23];
	}

	public int[] GetArray(int offset=-1) {
		int t = offsets[offset < 0 ? rootObjectOffset : offset];
		byte b = data[t++];
		if ((b & 0xf0) != 0xa0)
			return null;
		int count = b & 0xf;
		if (count == 15)
			count = GetCount(ref t);
		int[] res = new int[count];
		for (int i=0; i<count; i++) {
			int k = data[t++];
			for (int j=0; j<refSize; j++)
				k = (k << 8) | data[t++];
			res[i] = k;
		}
		return res;
	}

	ArrayList GetArrayList(int index) {
		int t = offsets[index];
		byte b = data[t++];
		if ((b & 0xf0) != 0xa0)
			return null;
		int count = b & 0xf;
		if (count == 15)
			count = GetCount(ref t);
		ArrayList res = new ArrayList(count);
		for (int i=0; i<count; i++) {
			int k = data[t++];
			for (int j=0; j<refSize; j++)
				k = (k << 8) | data[t++];
			res.Add(GetValue(k));
		}
		return res;
	}

	public KeyValue[] GetDict(int offset=-1) {
		int t = offsets[offset < 0 ? rootObjectOffset : offset];
		byte b = data[t++];
		if ((b & 0xf0) != 0xd0)
			return null;
		int count = b & 0xf;
		if (count == 15)
			count = GetCount(ref t);
		KeyValue[] res = new KeyValue[count];
		for (int i=0; i<count; i++) {
			int k = data[t++];
			for (int j=0; j<refSize; j++)
				k = (k << 8) | data[t++];
			res[i].Key = GetString(k);
		}
		for (int i=0; i<count; i++) {
			int v = data[t++];
			for (int j=0; j<refSize; j++)
				v = (v << 8) | data[t++];
			res[i].Value = v;
		}
		return res;
	}
	
	public Hashtable GetHashtable(int offset=-1) {
		int t = offsets[offset < 0 ? rootObjectOffset : offset];
		byte b = data[t++];
		if ((b & 0xf0) != 0xd0)
			return null;
		int count = b & 0xf;
		if (count == 15)
			count = GetCount(ref t);
		string[] keys = new string[count];
		Hashtable res = new Hashtable(count);
		for (int i=0; i<count; i++) {
			int k = data[t++];
			for (int j=0; j<refSize; j++)
				k = (k << 8) | data[t++];
			keys[i] = GetString(k);
		}
		for (int i=0; i<count; i++) {
			int v = data[t++];
			for (int j=0; j<refSize; j++)
				v = (v << 8) | data[t++];
			res[keys[i]] = GetValue(v);
		}
		return res;
	}

	public string GetString(int offset) {
		if (strings[offset] != null)
			return strings[offset];
		int t = offsets[offset];
		byte b = data[t++];
		if (b < 0x40 || b > 0x60)
			return null;
		int cnt = b & 0xf;
		if (cnt == 15)
			cnt = GetCount(ref t);
		string res;
		if ((b & 0xf0) == 0x50)
			res = Encoding.ASCII.GetString(data, t, cnt);
		else
			res = BitConverter.IsLittleEndian ? Encoding.BigEndianUnicode.GetString(data, t, cnt * 2) : Encoding.Unicode.GetString(data, t, cnt * 2);
		if (res.Length < 256)
			strings[offset] = res;
		return res;
	}

	public int GetInt(int offset) {
		int t = offsets[offset];
		byte b = data[t++];
		if ((b & 0xf0) == 0x20)
			return (int)GetNumber(offset - 1);
		else if ((b & 0xf0) != 0x10)
			return 0;
		int cnt = b & 0xf;
		int byteCount = (1 << cnt) - 1;
		int res = data[t++];
		for (int i=0; i<byteCount; i++)
			res = (res << 8) | data[t++];
		return res;
	}

	public double GetNumber(int offset) {
		int t = offsets[offset];
		byte b = data[t++];
		if ((b & 0xf0) == 0x10)
			return (double)GetInt(offset - 1);
		else if ((b & 0xf0) != 0x20)
			return 0;
		int cnt = b & 0xf;
		int byteCount = (1 << cnt) - 1;
		byte[] buffer = new byte[8];
		for (int i=0; i<byteCount && i<8; i++)
			buffer[7 - i] = data[t++];
		return System.BitConverter.ToDouble(buffer, 0);
	}
	
	public bool GetBool(int offset) {
		return data[offsets[offset]] == 9;
	}

	public System.DateTime GetDate(int offset) {
		int t = offsets[offset];
		byte b = data[t++];
		if (b != 0x33)
			return default(System.DateTime);
		byte[] buffer = new byte[8];
		for (int i=0; i<8; i++)
			buffer[7 - i] = data[t++];
		return new DateTime(2001, 1, 1, 0, 0, 0, 0).AddSeconds(BitConverter.ToDouble(buffer, 0)).ToLocalTime();
	}

	public byte[] GetData(int offset) {
		int t = offsets[offset];
		byte b = data[t++];
		if ((b & 0xf0) != 0x40)
			return null;
		int cnt = b & 0xf;
		if (cnt == 15)
			cnt = GetCount(ref t);
		byte[] res = new byte[cnt];
		for (int i=0; i<cnt; i++)
			res[i] = data[t++];
		return res;
	}
	
	/*
	 * Dict methods
	 */
	public KeyValue[] GetDict(string key, KeyValue[] dict) {
		foreach (KeyValue k in dict) {
			if (k.Key == key)
				return GetDict(k.Value);
		}
		return null;
	}
	
	public int[] GetArray(string key, KeyValue[] dict) {
		foreach (KeyValue k in dict)
			if (k.Key == key)
				return GetArray(k.Value);
		return null;
	}

	public string GetString(string key, KeyValue[] dict) {
		foreach (KeyValue k in dict)
			if (k.Key == key)
				return GetString(k.Value);
		return null;
	}

	public int GetInt(string key, KeyValue[] dict) {
		foreach (KeyValue k in dict)
			if (k.Key == key)
				return GetInt(k.Value);
		return 0;
	}
	
	public double GetNumber(string key, KeyValue[] dict) {
		foreach (KeyValue k in dict)
			if (k.Key == key)
				return GetNumber(k.Value);
		return 0;
	}

	public bool GetBool(string key, KeyValue[] dict) {
		foreach (KeyValue k in dict)
			if (k.Key == key)
				return GetBool(k.Value);
		return false;
	}

	public System.DateTime GetDate(string key, KeyValue[] dict) {
		foreach (KeyValue k in dict)
			if (k.Key == key)
				return GetDate(k.Value);
		return default(System.DateTime);
	}

	public byte[] GetData(string key, KeyValue[] dict) {
		foreach (KeyValue k in dict)
			if (k.Key == key)
				return GetData(k.Value);
		return null;
	}

	/*
	 * Private methods
	 */
	int GetCount(ref int t) {
		int cnt = ((data[t++] & 0xf) << 1) - 1;
		int res = data[t++];
		for (int i=0; i<cnt; i++)
			res = (res << 8) | data[t++];
		return res;
	}
}
