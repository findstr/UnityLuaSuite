using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public class ABT : UnityEditor.EditorWindow {
	static bool FORCEREBUILD = false;
	static string ENTRY = "MAIN.bundle";
	static string OUTPUT_PATH = Application.streamingAssetsPath;
	static string PROJECT_PATH = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));
	class AssetBundleInfo {
		public AssetBundleInfo() {
			Entry = ENTRY;
			AssetBundles = new Dictionary<string, string[]>();
		}
		public string Entry {get; set;}
		public Dictionary<string, string[]> AssetBundles {get; set; }
	};
	static string getconfpath() {
		var path = "ProjectSettings/ABT.yaml";
		Debug.Log("ABT getconfpath:" + path);
		return path;
	}
	static void check_modified(string output_root) {
		var path = getconfpath();
		if (File.Exists(path) == false) {
			try {Directory.Delete(output_root, true);}catch(System.Exception) { }
			return ;
		}
		int prefix = output_root.Length;
		int c = output_root[output_root.Length - 1];
		if (c != '\\' && c != '/')
			prefix += 1;
		var str = File.ReadAllText(getconfpath());
		var reader = new YamlDotNet.Serialization.Deserializer();
		var abm = reader.Deserialize<AssetBundleInfo>(str);
		var bundle_list = Directory.GetFiles(output_root, "*");
		foreach (var bundle_path in bundle_list) {
			if (bundle_path.EndsWith(".manifest"))
				continue;
			string[] assets_depend = null;
			var bundle_name= bundle_path.Substring(prefix);
			var bundle_time = File.GetLastWriteTime(bundle_path);
			if (!abm.AssetBundles.TryGetValue(bundle_name, out assets_depend)) {
				Debug.Log("ABT check_modified asset bundle:" + bundle_name + " useless");
				File.Delete(bundle_path);
				File.Delete(bundle_path + ".manifest");
			} else {
				foreach (var asset_name in assets_depend) {
					var asset_time = File.GetLastWriteTime(Path.Combine(PROJECT_PATH, asset_name));
					if (asset_time > bundle_time) {
						Debug.Log("ABT check_modified asset bundle:" + bundle_name + " is invalid");
						File.Delete(bundle_path);
						File.Delete(bundle_path + ".manifest");
						break;
					}
				}
			}
		}
	}
	static void build_target(BuildTarget target) {
		string output = Path.Combine(OUTPUT_PATH, ENTRY);
		BuildAssetBundleOptions option = BuildAssetBundleOptions.DeterministicAssetBundle;
		option |= BuildAssetBundleOptions.StrictMode;
		option |= BuildAssetBundleOptions.ChunkBasedCompression;
		option |= BuildAssetBundleOptions.DisableLoadAssetByFileName;
		option |= BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
		if (FORCEREBUILD == true) {
			option |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
			try {Directory.Delete(output, true);}catch(System.Exception) { }
		} else {
			check_modified(output);
		}
		Directory.CreateDirectory(output);
		BuildPipeline.BuildAssetBundles(output, option, target);
		AssetDatabase.Refresh();
		Debug.Log("ABT build finish:" + output);
	}
	[MenuItem("ABT/Build AssetBundle(Win)")]
	static void build_win() {
		build_target(BuildTarget.StandaloneWindows);
	}
	[MenuItem("ABT/Build AssetBundle(IOS)")]
	static void build_ios() {
		build_target(BuildTarget.iOS);
	}
	[MenuItem("ABT/Build AssetBundle(Android)")]
	static void build_android() {
		build_target(BuildTarget.Android);
	}	
	static void set_bundle_name(string path, string bundlename) {
		var ai = AssetImporter.GetAtPath(path);
		Debug.Log(path + "=>" + bundlename);
		if (ai.assetBundleName.CompareTo(bundlename) != 0) {
			ai.SetAssetBundleNameAndVariant(bundlename, "");
			ai.SaveAndReimport();
		}
	}
	[MenuItem("ABT/Import AssetBundle Setting")]
	static void import() {
		var str = File.ReadAllText(getconfpath());
		var reader = new YamlDotNet.Serialization.Deserializer();
		var abm = reader.Deserialize<AssetBundleInfo>(str);
		foreach (var s in abm.AssetBundles) {
			foreach (var g in s.Value)
				set_bundle_name(g, s.Key);
		}
		AssetDatabase.RemoveUnusedAssetBundleNames();
		AssetDatabase.Refresh();
		AssetDatabase.SaveAssets();
	}
	[MenuItem("ABT/Export AssetBundle Setting")]
	static void export() {
		AssetBundleInfo abi = new AssetBundleInfo();
		var abs = AssetDatabase.GetAllAssetBundleNames();
		for (int i = 0; i < abs.Length; i++) {
			var files = AssetDatabase.GetAssetPathsFromAssetBundle(abs[i]);
			abi.AssetBundles[abs[i]] = files;
			foreach (var f in files)
				Debug.Log(abs[i] + ":" + f);
		}
		var writer = new YamlDotNet.Serialization.Serializer();
		string str = writer.Serialize(abi);
		Debug.Log(str);
		File.WriteAllText(getconfpath(), str);
	}
}


