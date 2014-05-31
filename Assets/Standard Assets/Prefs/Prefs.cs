using UnityEngine;
using System;
using System.Collections;

/*
 * Usage:
 *   set value:
 *     Prefs.Set("key", 123);
 * 
 *   get value of any type:
 *     object = Prefs.Get("key");
 * 
 *   get value:
 *     int = Prefs.Get<int>("key");
 * 
 *   check key existence:
 *     bool = Prefs.HasKey("key");
 * 
 *   delete key:
 *     Prefs.DeleteKey("key");
 *
 *   delete all keys:
 *     Prefs.DeleteAll();
 * 
 *   save changes:
 *     Prefs.Save();
 * 
 *   Hashtable h = Prefs.Load<Hashtable>("myhash");
 *   ...
 *   Prefs.Save("myhash", h);
 * 
 *   var myprefs = Prefs.Load("myprefs");
 *   myprefs["key"] = "any data";
 *   myprefs["key2"] = new int[] {2, 3, 4};
 *   string str = myprefs.Get<string>("key");
 *   myprefs.Get("key");
 *   myprefs.DeleteAll();
 *   myprefs.DeleteKey("key");
 *   myprefs.Save();
 * 
 */
public class Prefs {
	public class Data {
		public bool dirty = false;

		string id;
		Hashtable data;
		int initialSize;

		public Data(string id) {
			this.id = id;
			byte[] bytes = Read(id);
			initialSize = bytes != null ? bytes.Length : 256;
			data = BinMsg.Decode(bytes) as Hashtable;
			if (data == null)
				data = new Hashtable(16);
			dirty = false;
		}

		public void DeleteAll() {
			data.Clear();
			dirty = true;
		}

		public void DeleteKey(string key) {
			data.Remove(key);
		}

		public bool HasKey(string key) {
			return data.ContainsKey(key);
		}

		public object this[string key] {
			get {
				return data[key];
			}
			set {
				dirty = true;
				data[key] = value;
			}
		}

		public void Set(string key, object value) {
			data[key] = value;
			dirty = true;
		}

		public T Get<T>(string key) {
			object o = data[key];
			if (o == null)
				return default(T);
			return (T)o;
		}

		public void Save() {
			if (dirty) {
				Write(id, BinMsg.Encode(data, initialSize + 32));
				dirty = false;
			}
		}
	}

	static Data _defaultPrefs = null;
	static Data defaultPrefs {
		get {
			if (_defaultPrefs == null)
				_defaultPrefs = Load(null);
			return _defaultPrefs;
		}
	}

	public static void DeleteAll() {
		defaultPrefs.DeleteAll();
	}

	public static void DeleteKey(string key) {
		defaultPrefs.DeleteKey(key);
	}

	public static bool HasKey(string key) {
		return defaultPrefs.HasKey(key);
	}

	public static void Set(string key, object value) {
		defaultPrefs[key] = value;
	}

	public static object Get(string key) {
		return defaultPrefs[key];
	}

	public static T Get<T>(string key) {
		return defaultPrefs.Get<T>(key);
	}

	public static void Save(bool force=false) {
		defaultPrefs.dirty = defaultPrefs.dirty || force;
		defaultPrefs.Save();
	}

	public static Data Load(string id) {
		return new Data(id);
	}
	
	public static T Load<T>(string id) {
		object o = BinMsg.Decode(Read(id));
		if (o == null)
			return default(T);
		return (T)o;
	}

	public static void Save(string id, object o) {
		Write(id, BinMsg.Encode(o));
	}

	public static string GetPath(string id) {
		#if UNITY_WEBPLAYER
		return "_prefs" + (id != null ? "-" + id : "");
		#else
		return Application.persistentDataPath + "/prefs" + (id != null ? "-" + id : "") + ".txt";
		#endif
	}

	public static void Remove(string id) {
		if (!string.IsNullOrEmpty(id)) {
			string path = GetPath(id);
			#if UNITY_WEBPLAYER
			PlayerPrefs.DeleteKey(path);
			#else
			if (System.IO.File.Exists(path))
				System.IO.File.Delete(path);
			#endif
		}
	}

	static byte[] Read(string id) {
		byte[] bytes = null;
		if (!string.IsNullOrEmpty(id)) {
			string path = GetPath(id);
			#if UNITY_WEBPLAYER
			string text = PlayerPrefs.GetString(path);
			if (text.Length > 0)
				bytes = System.Convert.FromBase64String(text);
			#else
			if (System.IO.File.Exists(path))
				bytes = System.IO.File.ReadAllBytes(path);
			#endif
		}
		return bytes;
	}

	static void Write(string id, byte[] bytes) {
		if (!string.IsNullOrEmpty(id)) {
			string path = GetPath(id);
			#if UNITY_WEBPLAYER
			PlayerPrefs.SetString(path, System.Convert.ToBase64String(bytes));
			PlayerPrefs.Save();
			#else
			System.IO.File.WriteAllBytes(path, bytes);
			#endif
		}
	}
}
