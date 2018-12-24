using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour {
	// Use this for initialization
	public GameObject uiroot;
	void Start () {
		Lua.start(uiroot);
	}

	// Update is called once per frame
	void Update () {
		Lua.update();
		CS.Timer.update();
	}
}
