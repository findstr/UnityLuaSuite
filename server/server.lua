local core = require "sys.core"
local zproto = require "zproto"
local np = require "sys.netpacket"
local msg = require "saux.msg"
local rpc = require "saux.rpc"
local router = {}
local NIL = {}
local proto
local server

local function decode(d, sz)
	local data
	local str = np.tostring(d, sz)
	local len = #str
	assert(len >= 2)
	local cmd = string.unpack("<I2", str)
	if len > 2 then
		data = proto:decode(cmd, str, 2)
	else
		data = NIL
	end
	return cmd, data
end

local function encode(cmd, body)
	if type(cmd) == "string" then
		cmd = proto:tag(cmd)
	end
	local cmddat = string.pack("<I2", cmd)
	local bodydat = proto:encode(cmd, body)
	return cmddat .. bodydat
end

local function reg(cmd, cb)
	print("cmd:", cmd)
	cmd = proto:tag(cmd)
	router[cmd] = cb
end

local function send(fd, cmd, ack)
	local dat = encode(cmd, ack)
	return server:send(fd, dat)
end

local function event_accept(fd, addr)
	core.log("accept", addr)
end

local function event_close(fd, errno)
	core.log("close", fd, errno)
end

local function event_data(fd, d, sz)
	print("data", fd)
	local cmd, data = decode(d, sz)
	assert(router[cmd])(fd, cmd, data)
end

local M = {
	start = function(protolist, addr)
		local err
		local str = table.concat(protolist)
		proto, err = zproto:parse(str)
		assert(proto, err)
		server = msg.createserver {
			addr = addr,
			accept = event_accept,
			close = event_close,
			data = event_data,
		}
		server:start()
	end,
	reg = reg,
	send = send,
}

return M

