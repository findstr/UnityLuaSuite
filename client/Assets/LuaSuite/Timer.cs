using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CS {
public class Timer {
	private static uint session = 0;
	private static SortedList<uint, uint> expire_list = new SortedList<uint, uint>();
	private static uint clock() { 
		return (uint)(Time.realtimeSinceStartup * 1000.0f);
	}
	public static uint timeout(uint ms) {
		uint sess = ++session;
		expire_list[clock() + ms] = sess;
		return sess;
	}
	public static void update() {
		uint now = clock();
		var iter = expire_list.GetEnumerator();
		while (iter.MoveNext()) {
			if (iter.Current.Key > now)
				break;
			Lua.expire(iter.Current.Value);
		}
	}
}
}

