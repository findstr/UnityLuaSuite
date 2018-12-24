using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ABM {
	private class ABO {	//asset bundle object
		public int abi = -1;
		public int refn = 1;
		public AssetBundle ab = null;
		public HashSet<int> dependencies = new HashSet<int>();
	};
	private class AO {      //asset object
		public AO(int abi, string name) {
			this.abi = abi;
			this.name = name;
		}
		public string name = null;
		public int abi = -1;
		public int refn = 1;
		public Object asset = null;
	};
	private class initctx {
		public int i;
		public string[] files;
	};

	private enum OP{
		SET_ACTIVE_SCENE = 1,
	};
	private class operation  {
		public operation(OP op, string ud) {
			this.op = op; this.ud = ud;
		}
		public OP op;
		public string ud;
	};
	private static int ABIDX = 0;
	private static string ROOT = null;
	private static string MAINNAME = "MAIN.bundle";
	private static AssetBundleManifest MANIFEST = null;
	private static initctx ctx = null;
	private static Dictionary<int, string> abi_to_name = new Dictionary<int,string>();
	private static Dictionary<string, int> name_to_abi = new Dictionary<string,int>();
	private static Dictionary<int, ABO> abi_to_abo = new Dictionary<int,ABO>();
	private static Dictionary<string, int> abi_of_asset = new Dictionary<string,int>();
	private static Dictionary<string, AO> name_to_ao = new Dictionary<string,AO>();
	private static Dictionary<int, AO> object_to_ao = new Dictionary<int, AO>();
	private static List<operation> operation_currentframe = new List<operation>();
	private static List<operation> operation_nextframe = new List<operation>();
	private static void DBGPRINT(string str) {
		//Debug.Log(str);
	}
	private static ABO load_abo(int abi) {
		//TODO: remove recursive call
		ABO abo = null;
		abi_to_abo.TryGetValue(abi, out abo);
		if (abo != null) {
			abo.refn++;
			DBGPRINT("load_abo abi:" + abi + " ref:" + abo.refn);
			return abo;
		}
		abo = new ABO();
		abi_to_abo[abi] = abo;//insert first, for circle depend
		var name = abi_to_name[abi];
		var depend = MANIFEST.GetDirectDependencies(name);
		for (int i = 0; i < depend.Length; i++) {
			var dn = depend[i];
			var di = 0;
			name_to_abi.TryGetValue(dn, out di);
			if (di == 0) { 
				DBGPRINT("load_abo depend nonexist assetbundle:" + dn);
				return null;
			}
			load_abo(di);
			abo.dependencies.Add(di);
		}
		var path = Path.Combine(ROOT, name);
		DBGPRINT("load_abo depend assetbundle:" + path);
		var ab = AssetBundle.LoadFromFile(path);
		System.Diagnostics.Debug.Assert(ab != null);
		abo.ab = ab;
		abo.abi = abi;
		return abo;
	}
	private static void unload_abo(int abi) {
		ABO abo = null;
		abi_to_abo.TryGetValue(abi, out abo);
		if (abo == null) {
			DBGPRINT("unload_abo nonexist abo:" + abi);
			return ;
		}
		--abo.refn;
		DBGPRINT("unload_abo abi:" + abi + " ref:" + abo.refn);
		if (abo.refn <= 0) {
			var iter = abo.dependencies.GetEnumerator();
			while (iter.MoveNext())
				unload_abo(iter.Current);
			abo.ab.Unload(true);
			abi_to_abo.Remove(abi);
			DBGPRINT("unload_abo clear abi:" + abi);
		}
	}
	public static int start(string path) {
		ctx = new initctx();
		var c = path[path.Length - 1];
		var ab = AssetBundle.LoadFromFile(Path.Combine(path, MAINNAME));
		MANIFEST = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
		ab.Unload(false);
		ROOT = path;
		ctx.i = 0;
		ctx.files = MANIFEST.GetAllAssetBundles();
		return ctx.files.Length;
	}
	public static bool init() {
		if (ctx.i < ctx.files.Length) {
			var abi = ++ABIDX;
			var subname = ctx.files[ctx.i];
			var name = Path.Combine(ROOT, subname);
			var ab = AssetBundle.LoadFromFile(name);
			var asset_child = ab.GetAllAssetNames();
			var scene_child = ab.GetAllScenePaths();
			abi_to_name[abi] = subname;
			name_to_abi[subname] = abi;
			for (int i = 0; i < asset_child.Length; i++) {
				var s = asset_child[i];
				DBGPRINT("Asset:" + s + " -> " + subname);
				abi_of_asset[s] = abi;
			}
			for (int i = 0; i < scene_child.Length; i++) {
				var s = scene_child[i];
				DBGPRINT("Scene:" + s + " -> " + subname);
				abi_of_asset[s] = abi;
			}
			ab.Unload(true);
			ctx.i++;
			return true;
		} else {
			ctx = null;
			return false;
		}
	}
	public static T load_asset<T>(string name_) where T : Object {
		int abi = 0;
		AO ao = null;
		var name = name_.ToLower();
		DBGPRINT("ABM::load_asset:" + name);
		if (name_to_ao.TryGetValue(name, out ao)) { 
			//TODO: load_asset<Sprite> and load_asset<Texture2D> may has same name
			DBGPRINT("load_asset name:" + name + " hit");
			++ao.refn;
			return (T)ao.asset;
		}
		if (abi_of_asset.TryGetValue(name, out abi) == false) 
			return null;
		var abo = load_abo(abi);
		if (abo == null)
			return null;
		var asset = abo.ab.LoadAsset<T>(name);
		if (asset != null) {
			ao = new AO(abi, name);
			ao.asset = asset;
			name_to_ao[name] = ao;
			object_to_ao[asset.GetInstanceID()] = ao;
		}
		return asset;	
	}
	public static void ref_asset(int instance_id) {
		AO ao = null;
		if (object_to_ao.TryGetValue(instance_id, out ao)) { 
			++ao.refn;
			DBGPRINT("ref_asset instance id:" + instance_id + " ref count:" + ao.refn);
		}
		System.Diagnostics.Debug.Assert(ao != null);
		return ;
	}
	public static void unref_asset(int instance_id) {
		AO ao = null;
		if (object_to_ao.TryGetValue(instance_id, out ao) == false)
			return ;
		--ao.refn;
	
		DBGPRINT("ABM::load_asset:" + ao.name);
		DBGPRINT("unref_asset instance id:" + instance_id + " ref count:" + ao.refn);
		if (ao.refn <= 0) {
			unload_abo(ao.abi);
			name_to_ao.Remove(ao.name);
			object_to_ao.Remove(instance_id);
		}
		return ;
	}
	public static Object load_asset(string name) {
		if (name.EndsWith(".png") || name.EndsWith(".jpg"))
			return load_asset<Sprite>(name);
		else
			return load_asset<Object>(name);
	}	
	public static void unload_asset(Object o) {
		unref_asset(o.GetInstanceID());
	}
	public static void load_scene(string name, LoadSceneMode mode) {
		int abi;
		if (abi_of_asset.TryGetValue(name, out abi) == false)  {
			DBGPRINT("load_scene:" + name + " fail");
			return ;
		}
		var abo = load_abo(abi);
		if (abo == null) {
			DBGPRINT("load_scene:" + name + " fail");
			return ;
		}
		var scenename = Path.GetFileNameWithoutExtension(name);
		SceneManager.LoadScene(scenename, mode);
		DBGPRINT("load_scene:" + scenename);
		return ;
	}
	public static AsyncOperation load_scene_async(string name, LoadSceneMode mode) {
		int abi;
		if (abi_of_asset.TryGetValue(name, out abi) == false) 
			return null;
		var abo = load_abo(abi);
		if (abo == null)
			return null;
		var scenename = Path.ChangeExtension(name, null);
		return SceneManager.LoadSceneAsync(scenename, mode);
	}
	public static AsyncOperation unload_scene_async(string name) {
		int abi;
		if (abi_of_asset.TryGetValue(name, out abi) == false) 
			return null;
		unload_abo(abi);
		return SceneManager.UnloadSceneAsync(name);
	}

	public static void set_active_scene(string scenepath) {
		var scenename = Path.GetFileNameWithoutExtension(scenepath);
		operation_nextframe.Add(new operation(OP.SET_ACTIVE_SCENE, scenename));
	}
	public static void update() {
		var exec = operation_currentframe;
		operation_currentframe = operation_nextframe;
		var iter = exec.GetEnumerator();
		while (iter.MoveNext()) {
			var op = iter.Current;
			switch (op.op) {
			case OP.SET_ACTIVE_SCENE: {
				var scene = SceneManager.GetSceneByName(op.ud);
				DBGPRINT("SetActiveScene:" + op.ud + ":" + scene);
				SceneManager.SetActiveScene(scene);
				break;}
			}
		}
		exec.Clear();
		operation_nextframe = exec;
	}
}
