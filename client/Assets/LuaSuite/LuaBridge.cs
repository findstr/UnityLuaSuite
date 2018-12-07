using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LuaBridge : MonoBehaviour {
	public bool Reload = false;
	// Use this for initialization
	void Start() {
		Lua.require(gameObject);
	}
	void Update() {
		if (Reload == true) {
			Lua.reload(gameObject);
			Reload = false;
		}
	}
}


