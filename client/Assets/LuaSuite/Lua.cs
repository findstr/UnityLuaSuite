using LuaInterface;
using UnityEngine;

class Lua {
	private static LuaState lua = new LuaState();
	private static LuaFunction func_require = null;
	private static LuaFunction func_requirex = null;
	private static LuaFunction func_update = null;
	private static LuaFunction func_reload = null;
	public static void start() {
		LuaBinder.Bind(lua);
		DelegateFactory.Init();
		netsocket.reg(lua);
		lua.Start();
		func_require = lua.GetFunction("require");
		System.Diagnostics.Debug.Assert(func_require != null);
		var suite = func_require.Invoke<string, LuaTable>("suite.core");
		func_requirex = suite.GetLuaFunction("require");
		func_update = suite.GetLuaFunction("update");
		func_reload = suite.GetLuaFunction("reload");
		suite.Dispose();
		lua.DoFile("main.lua");
		lua.CheckTop();
	}
	public static void require(GameObject go) {
		func_requirex.Call<string, GameObject>(go.name, go);
	}
	public static void reload(GameObject go) {
		func_reload.Call<string, GameObject>(go.name, go);
	}
	public static void update() {
		func_update.Call();
	}
	public static LuaFunction getfunc(string module, string field) {
		var table = func_require.Invoke<string, LuaTable>(module);
		var func = table.GetLuaFunction(field);
		table.Dispose();
		return func;
	}

};
