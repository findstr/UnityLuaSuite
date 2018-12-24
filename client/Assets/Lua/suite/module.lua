local M = {}

M.__index = M

function M.create()
	local m = setmetatable({}, M)
	return m
end

function M:watch(obj, cb)
	local v = self._viewers
	if not v then
		v = {}
		self._viewers = v
	end
	v[obj] = cb
end

function M:unwatch(obj)
	local v = self._viewers
	if v then
		v[obj] = nil
	end
end

function M:dirty()
	local v = self._viewers
	if v then
		for obj, cb in pairs(v) do
			cb(obj, self)
		end
	end
end

return M

