local core = require "sys.core"
local server = require "server"

core.start(function()
	local proto = require "proto"
	server.start (proto, "0.0.0.0:7080")
	require "ping"
end)
