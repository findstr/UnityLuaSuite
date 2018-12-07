require "typeof"
local typeof = typeof
local M = {
    Button = typeof(UnityEngine.UI.Button),
	--[[
    Text = typeof(UnityEngine.UI.Text),
    Image = typeof(UnityEngine.UI.Image),
    Slider = typeof(UnityEngine.UI.Slider),
    BoxCollider = typeof(UnityEngine.BoxCollider),
    RectTransform = typeof(UnityEngine.RectTransform),
    TextMesh = typeof(UnityEngine.TextMesh),
    ]]--
}
print("TYPEOF", M)
return M

