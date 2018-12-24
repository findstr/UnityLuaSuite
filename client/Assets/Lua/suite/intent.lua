local res = require "suite.resources"
local M = {}
local ROOT
local order = {}
local task = {}
local stack = {}
local tostring = tostring
local destroy = res.destroy
local instantiate = res.instantiate

function M.start(root)
	ROOT = root
	local order_0 = ROOT:Find("0")
	order[0] = order_0.transform
end

function M.createorder()
	local n = #order
	local o = UnityEngine.Object.Instantiate(order[n].gameObject, ROOT)
	n = n + 1
	o.name = tostring(n)
	order[n] = o.transform
	return n
end

function M.open(name, orderidx)
	orderidx = orderidx or 0
	local parent = order[orderidx]
	local obj = task[name]
	if not obj then
		obj = instantiate(name, parent)
	else
		obj.__go.transform.parent = parent
	end
	return obj
end

local function clear(n)
	local count = #stack
	for i = n, count do
		local obj = stack[i]
		stack[i] = nil
		destroy(obj)
	end
end

function M.push(name, orderidx)
	local obj = M.open(name, orderidx)
	local n = obj.__order
	if not n then
		n = #stack + 1
		obj.__order = n
		stack[n] = obj
	else
		clear(n+1)
		obj = stack[n]
	end
	return obj
end

function M.close(obj)
	local n = obj.__order
	if n then
		clear(n)
		obj.__order = nil
	else
		destroy(obj)
	end
end

return M

