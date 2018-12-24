local zproto = require "suite.zproto"
local core = netsocket
local M = {}
local ALIVE = setmetatable({}, {__mode = "kv"})
local mt = {__index = M}

function M:create(proto)
	local str = table.concat(proto, "\n")
	str = [[
		error 0x1 {
			.cmd:integer 1
			.errno:integer 2
		}
	]] .. str
	print(str)
	local proto, err = zproto:parse(str)
	assert(proto, err)
	local obj = {
		ip = false,
		port = false,
		router = {},
		proto = proto,
		sock = core.create(),
	}
	setmetatable(obj, mt)
	obj:recv("error", function(tbl)
		local r = obj.router
		local cb = r[tbl.cmd]
		if cb then
			cb(nil, tbl.errno)
		end
	end)
	return obj
end

function M:error(cb)
	self:recv("error", cb)
end

function M:connect(addr)
	local ip, port = addr:match("([%d.]+):(%d+)")
	print(ip, port)
	ALIVE[self] = true
	self.ip = ip
	self.port = port
	local sock = self.sock
	sock:connect(ip, port)
end

function M:close()
	ALIVE[self] = nil
	sock:close()
end

function M:recv(cmd, cb)
	local proto = self.proto
	local router = self.router
	local cmd = proto:tag(cmd)
	assert(cmd)
	router[cmd] = cb
end

function M:send(cmd, tbl)
	local proto = self.proto
	local cmd = proto:tag(cmd)
	assert(cmd)
	local dat = proto:encode(cmd, tbl)
	print("cmd", cmd, #dat)
	self.sock:write(cmd, dat)
end

function M.update()
	for obj, _ in pairs(ALIVE) do
		local sock = obj.sock
		local cmd, dat = sock:read()
		if dat then
			local proto = obj.proto
			local tbl = proto:decode(cmd, dat)
			local cb = obj.router[cmd]
			if cb then
				cb(tbl)
			end
		end
	end
end

return M

