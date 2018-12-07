local M = {}
local TYPE = require "TYPE"
local socket = require "suite.socket"
local proto = require "proto"
local sock = socket:create(proto, "192.168.2.118", 7080)

local function onclick()
	sock:connect()
	sock:recv("pong", function(ack)
		print("pong")
	end)
	print(ok, err)
	sock:send("ping", {str = "hello"})
	print('onclick x')
end

function M.start(go)
	local button
	print("-------start------------", go)
	button = go.transform:Find("Button").gameObject:GetComponent(TYPE.Button)
	button.onClick:AddListener(onclick)
	print("buttonx:", button)
end

return M

