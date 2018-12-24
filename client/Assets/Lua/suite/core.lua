local require = require
local timer = CS.Timer
local socket = require "suite.socket"
local expire_list = {}
local start_list = {}
local update_list = {}
local second_list = {}
local M = nil
local hook = {
	["start"] = start_list,
	["update"] = update_list,
	["second"] = second_list,
}

table.clone = function(tbl)

end

local function start(obj)
	for k, v in pairs(hook) do
		local cb = obj[k]
		if cb then
			v[obj] = cb
		end
	end
end

local function regupdate(obj, cb)
	update_list[obj] = cb
end

local function unregupdate(obj)
	update_list[obj] = nil
end

local function update_func(list)
	for obj, cb in pairs(list) do
		cb(obj)
	end
end

local function update(now)
	local last = M.now
	M.now = now
	socket.update()
	for obj, _ in pairs(start_list) do
		obj:start()
		start_list[obj] = nil
	end
	update_func(update_list)
	if now ~= last then
		return
	end
	update_func(second_list)
end

local function timeout(ms, cb)
	local sess = timer.timeout(ms)
	expire_list[sess] = cb
end

local function expire(session)
	local cb = expire_list[session]
	if cb then
		expire_list[session] = nil
		cb()
	end
end

M = {
	_start = start,
	_update = update,
	_expire = expire,
	timeout = timeout,
	regupdate = regupdate,
	unregupdate = unregupdate,
}

return M

