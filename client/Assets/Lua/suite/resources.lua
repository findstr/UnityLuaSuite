local M = {}
local require = require
local tremove = table.remove
local core = require "suite.core"
local weakmt = {__mode="kv"}
local objholder = {}
local objpool = {}
local cs = UnityEngine.Resources
local inst = UnityEngine.GameObject.Instantiate
local destroy = UnityEngine.Object.Destroy
local namecache = setmetatable({}, {
	__index = function(tbl, k)
		local v = "view." .. k
		tbl[k] = v
		return v
	end
})

local function obj_gc(obj)
	if not obj.__go then
		destroy(obj.__go)
	end
end

local function obj_grab(go)
	local obj = tremove(objpool)
	if not obj then
		obj = {
			__go = go
		}
	else
		obj.__go = go
	end
	return obj
end

local function obj_free(obj)
	local n = #objpool
	if n > 3000 then
		return
	end
	for k, v in pairs(obj) do
		obj[k] = nil
	end
	setmetatable(obj, nil)
	objpool[n + 1] = obj
end

function M.load(path, typ)
	path = path:gsub("%..*$", "")
	print(path)
	if typ then
		return cs.Load(path, typ)
	else
		return cs.Load(path)
	end
end

function M.unload_asset(asset)
	cs.UnloadAsset(asset)
end

function M.instantiate(path, parent)
	local asset = M.load(path)
	print('asset', path, asset)
	local go = inst(asset, parent)
	local name = asset.name
	go.name = name
	local class = require(namecache[name])
	class.__index = class
	class.__name = name
	class.__gc = obj_gc
	local obj = obj_grab(go)
	setmetatable(obj, class)
	core._start(obj)
	objholder[obj] = true
	return obj

end

function M.clone(obj, parent)
	local go = inst(obj.__go, parent)
	local new = setmetatable({
		__go = go
	}, getmetatable(obj))
	core._start(new)
	objholder[new] = true
	return new
end

function M.destroy(obj)
	objholder[obj] = nil
	destroy(obj.__go)
	obj_free(obj)
end

return M

