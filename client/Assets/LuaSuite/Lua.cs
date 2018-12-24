using LuaInterface;
using UnityEngine;

class Lua {
	private static LuaState lua = new LuaState();
	private static LuaFunction func_require = null;
	private static LuaFunction func_update = null;
	private static LuaFunction func_expire = null;
	private static LuaFunction func_reload = null;
	private static uint clock() {
		return (uint)(Time.realtimeSinceStartup * 1000.0f);
	}
	private static void init_core() {
		func_require = lua.GetFunction("require");
		System.Diagnostics.Debug.Assert(func_require != null);
		var suite = func_require.Invoke<string, LuaTable>("suite.core");
		func_update = suite.GetLuaFunction("_update");
		func_expire = suite.GetLuaFunction("_expire");
		func_reload = suite.GetLuaFunction("_reload");
		suite.Dispose();
	}
	private static void init_intent(Transform uiroot) {
		var res = func_require.Invoke<string, LuaTable>("suite.intent");
		var func = res.GetLuaFunction("start");
		func.Call(uiroot);
		func.Dispose();
		res.Dispose();
	}

	public static void start(GameObject uiroot) {
		LuaBinder.Bind(lua);
		DelegateFactory.Init();
		netsocket.reg(lua);
		lua.Start();
		init_core();
		init_intent(uiroot.transform);
		lua.DoFile("main.lua");
		lua.CheckTop();
	}
	public static void reload(GameObject go) {
		func_reload.Call<string, GameObject>("V." + go.name, go);
	}
	public static void update() {
		var now = clock();
		func_update.Call(now);
	}
	public static void expire(uint session) {
		func_expire.Call(session);
	}
	public static LuaFunction getfunc(string module, string field) {
		var table = func_require.Invoke<string, LuaTable>(module);
		var func = table.GetLuaFunction(field);
		table.Dispose();
		return func;
	}
};

