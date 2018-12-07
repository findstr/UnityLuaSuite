local require = require
local socket = require "suite.socket"
local start_list = {}
local update_list = {}
local hook = {
	["update"] = update_list,
}

local function requirex(name, go)
	print("name", name)
	local m = require(name)
	if not m then
		return
	end
	local start = m.start
	if start then
		start_list[start] = go
	end
	for k, v in pairs(hook)  do
		if m[k] then
			v[#v + 1] = m.update
		end
	end
end

local function update()
	socket.update()
	for func, go in pairs(start_list) do
		func(go)
		start_list[func] = nil
	end
	for _, v in pairs(update_list) do
		v()
	end
end

local function reload(name, go)
	local m = require(name)
	for name, list in pairs(hook) do
		local func = m[name]
		if func then
			for k, v in pairs(list) do
				if v == func then
					table.remove(list, k)
					break
				end
			end
		end
	end
	package.loaded[name] = nil
	m = requirex(name, go)
end

local M = {
	require = requirex,
	update = update,
	reload = reload,
}

return M

