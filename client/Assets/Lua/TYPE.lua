require "typeof"
local typeof = typeof
local M = {
    Button = typeof(UnityEngine.UI.Button),
    Image = typeof(UnityEngine.UI.Image),
    Sprite = typeof(UnityEngine.Sprite),
    Text = typeof(UnityEngine.UI.Text),
	--[[
    Slider = typeof(UnityEngine.UI.Slider),
    BoxCollider = typeof(UnityEngine.BoxCollider),
    RectTransform = typeof(UnityEngine.RectTransform),
    TextMesh = typeof(UnityEngine.TextMesh),
    ]]--
}
print("TYPEOF", M)
return M

