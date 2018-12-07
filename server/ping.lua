
local server = require "server"

server.reg("ping", function(fd, req)
	server.send(fd, "pong", {str = "foo"})
end)
