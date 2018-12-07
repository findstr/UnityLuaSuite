local zproto = require "suite.zproto"
local core = netsocket
local M = {}
local ALIVE = setmetatable({}, {__mode = "kv"})
local mt = {__index = M}

function M:create(proto, addr, port)
	local str = table.concat(proto, "\n")
	print(str)
	local proto, err = zproto:parse(str)
	assert(proto, err)
	local obj = {
		addr = addr,
		port = port,
		router = {},
		proto = proto,
		sock = core.create(),
	}
	setmetatable(obj, mt)
	ALIVE[obj] = true
	return obj
end


function M:connect()
	local sock = self.sock
	sock:connect(self.addr, self.port)
end

function M:close()
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

