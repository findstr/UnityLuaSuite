﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour {
	// Use this for initialization
	void Start () {
		Lua.start();
	}

	// Update is called once per frame
	void Update () {
		Lua.update();
	}
}
