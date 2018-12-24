local M = {}

function M.child(ui, go, conf)
	local transform = go.transform
	print("transform", transform, transform.Find)
	for k, v in pairs(conf) do
		local o = transform:Find(v)
		assert(o, v)
		ui[k] = o.gameObject
	end
end

function M.component(ui, router)
	for k, v in pairs(router) do
		local go = ui[k]
		local c = go:GetComponent(v)
		assert(c, k)
		ui[k] = c
	end
end

return M


