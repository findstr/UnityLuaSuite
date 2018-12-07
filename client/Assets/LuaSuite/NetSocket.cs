using LuaInterface;
using System;
using System.Collections;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

class bufferstream {
	private byte[] buffer = null;
	private int length = 0;
	public int Length
	{
		get{return length;}
	}
	public int write(byte[] dat, int offset, int size) {
		//Debug.Log(":: Write:" + size);
		int need = length + size;
		if (buffer == null)
			buffer = new byte[need];
		else if (buffer.Length < need) {
			byte[] old = buffer;
			buffer = new byte[need];
			Buffer.BlockCopy(old, 0, buffer, 0, length);
		}
		Buffer.BlockCopy(dat, offset, buffer, length, size);
		length = need;
		return size;
	}

	public int read(byte[] dat, int offset, int size) {
		//Debug.Log(":: Begin Read:" + size + ":" + length);
		if (length < size)
			return 0;
		Buffer.BlockCopy(buffer, 0, dat, offset, size);
		length -= size;
		Buffer.BlockCopy(buffer, size, buffer, 0, length);
		//Debug.Log(":: Read:" + size + ":" + length);
		return size;
	}
	public void clear() {
		Debug.Log(":: Clear");
		length = 0;
	}
}


public class netsocket {
	public const int DISCONNECT = 1;
	public const int CONNECTING = 2;
	public const int CONNECTED = 3;
	private int pend = -1;
	private int cmd = 0;
	private Socket s = null;
	private int _status = DISCONNECT;
	private Queue sendq = new Queue();
	private byte[] buffer = new byte[128];
	private bufferstream readstream = new bufferstream();
	private static void send_cb(IAsyncResult ar) {
		netsocket obj = (netsocket) ar.AsyncState;
		if (obj.sendq.Count == 0)
			return ;
		byte[] a = (byte[]) obj.sendq.Dequeue();
		obj.dosend(a);
		return ;
	}

	private static void recv_cb(IAsyncResult ar) {
		netsocket obj = (netsocket) ar.AsyncState;
		int read = obj.s.EndReceive(ar);
		if (read > 0) {
			lock (obj.readstream) {
				obj.readstream.write(obj.buffer, 0, read);
			}
			obj.dorecv();
		} else {
			Debug.Log("RecvCB: Disconnect");
			obj._status = DISCONNECT;
			obj.s.Close();
			lock (obj.readstream) {
				obj.readstream.clear();
			}
		}
	}

	private static void connect_cb(IAsyncResult ar) {
		netsocket obj = (netsocket) ar.AsyncState;
		obj._status = CONNECTED;
		Debug.Log("Connect:" + obj.s.Connected);
		obj.dorecv();
		if (obj.sendq.Count > 0) {
			byte[] a = (byte[]) obj.sendq.Dequeue();
			obj.dosend(a);
		}
		return ;
	}

	private void dorecv() {
		s.BeginReceive(buffer, 0, buffer.Length,
			SocketFlags.None,
			new AsyncCallback(recv_cb), this);
		return ;
	}
	private int dosend(byte[] data) {
		SocketError err;
		s.BeginSend(data, 0, data.Length,
			SocketFlags.None,
			out err,
			new AsyncCallback(send_cb), this);
		return (int)err;
	}
	public int status {
		get {return _status;}
	}

	public int length {
		get {return (int)readstream.Length;}
	}

	public void connect(string addr, int port) {
		if (_status >= CONNECTING)
			return ;
		pend = -1;
		cmd = 0;
		s = new Socket(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);
		_status =  CONNECTING;
		s.BeginConnect(addr, port,
				new AsyncCallback(connect_cb), this);
		return ;
	}

	public void close() {
		lock(readstream) {
			readstream.clear();
		}
		s.Close();
		_status = DISCONNECT;
	}
	public int write(byte[] data) {
		if (sendq.Count != 0 || _status == CONNECTING) {
			sendq.Enqueue(data);
			return (int)SocketError.Success;
		}
		return dosend(data);
	}
	public byte[] read(int sz) {
		if (readstream.Length < sz)
			return null;
		lock (readstream) {
			byte[] buf = new byte[sz];
			readstream.read(buf, 0, sz);
			return buf;
		}
	}
	public int writepacket(int cmd, IntPtr ptr, int size) {
		byte[] buffer = new byte[size + 4];
		//header
		buffer[0] = (byte)((size+2) >> 8);
		buffer[1] = (byte)(size+2);
		//cmd
		buffer[2] = (byte)cmd;
		buffer[3] = (byte)(cmd >> 8);
		//body
		Marshal.Copy(ptr, buffer, 4, size);
		return write(buffer);
	}
	public byte[] readpacket(out int cmd) {
		cmd = -1;
		if (pend == -1) {
			if (readstream.Length < 4)
				return null;
			var buf = read(4);
			//header
			pend = (int)buf[1] | ((int)buf[0] << 8);
			//cmd
			this.cmd = (int)buf[2] | ((int)buf[3] << 8);
			pend -= 2;
		}
		cmd = this.cmd;
		var dat = read(pend);
		if (dat != null) {
			this.pend = -1;
			this.cmd = 0;
		}
		return dat;
	}
	//lua bind
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	public static int lcreate(IntPtr L) {
		try {
			int count = LuaDLL.lua_gettop(L);
			if (count == 0) {
				netsocket obj = new netsocket();
				ToLua.Push(L, obj);
				return 1;
			} else {
				return LuaDLL.luaL_throw(L, "invalid arguments to ctor method: netsocket.create");
			}
		} catch (Exception e) {
			return LuaDLL.toluaL_exception(L, e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	public static int lconnect(IntPtr L) {
		try {
			ToLua.CheckArgsCount(L, 3);
			var obj = (netsocket)ToLua.CheckObject(L, 1, typeof(netsocket));
			var ip = ToLua.CheckString(L, 2);
			var port = (int)LuaDLL.luaL_checknumber(L, 3);
			obj.connect(ip, port);
			return 0;
		} catch (Exception e) {
			return LuaDLL.toluaL_exception(L, e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	public static int lclose(IntPtr L) {
		try {
			ToLua.CheckArgsCount(L, 1);
			var obj = (netsocket)ToLua.CheckObject(L, 1, typeof(netsocket));
			obj.close();
			return 0;
		} catch (Exception e) {
			return LuaDLL.toluaL_exception(L, e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	public static int lwrite(IntPtr L) {
		try {
			int size;
			ToLua.CheckArgsCount(L, 3);
			var obj = (netsocket)ToLua.CheckObject(L, 1, typeof(netsocket));
			if (obj.status == DISCONNECT) {
				LuaDLL.lua_pushboolean(L, false);
			} else {
				int cmd = LuaDLL.luaL_checkinteger(L, 2);
				IntPtr str = LuaDLL.tolua_tolstring(L, 3, out size);
				obj.writepacket(cmd, str, size);
				LuaDLL.lua_pushboolean(L, true);
			}
			return 1;
		} catch (Exception e) {
			return LuaDLL.toluaL_exception(L, e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	public static int lread(IntPtr L) {
		try {
			ToLua.CheckArgsCount(L, 1);
			var obj = (netsocket)ToLua.CheckObject(L, 1, typeof(netsocket));
			if (obj.status == DISCONNECT) {
				LuaDLL.lua_pushnil(L);
				LuaDLL.lua_pushnil(L);
			} else {
				int cmd;
				var buf = obj.readpacket(out cmd);
				LuaDLL.lua_pushinteger(L, cmd);
				if (buf != null)
					LuaDLL.lua_pushlstring(L, buf, buf.Length);
				else
					LuaDLL.lua_pushnil(L);
			}
			return 2;
		} catch (Exception e) {
			return LuaDLL.toluaL_exception(L, e);
		}
	}

	public static void reg(LuaState L) {
		L.BeginModule(null);
		L.BeginClass(typeof(netsocket), typeof(System.Object));
		L.RegFunction("create",  lcreate);
		L.RegFunction("connect",  lconnect);
		L.RegFunction("close", lclose);
		L.RegFunction("write", lwrite);
		L.RegFunction("read", lread);
		L.EndClass();
		L.EndModule();
	}
}


