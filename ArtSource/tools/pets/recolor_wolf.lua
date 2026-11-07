-- The pet wolf's fur (2026-11-05, the owner chose "Grey Wolf, recoloured"): the forest's Grey Wolf texture with its greys
-- turned a warm brown and gold by their lightness, so the pet never reads as one of the forest's grey wolves.
--   aseprite -b --script-param src=<wolf_albedo.png> --script-param out=<pet_wolf_albedo.png> --script recolor_wolf.lua
local src = Image { fromFile = app.params["src"] }
local rgba, R, G, B, A = app.pixelColor.rgba, app.pixelColor.rgbaR, app.pixelColor.rgbaG, app.pixelColor.rgbaB, app.pixelColor.rgbaA
local stops = {
  { 0.0, 26, 16, 10 }, { 0.3, 74, 44, 22 }, { 0.55, 132, 84, 40 }, { 0.75, 184, 132, 70 }, { 0.9, 222, 182, 118 }, { 1.0, 244, 222, 172 },
}
local lut = {}
for i = 0, 255 do
  local l = (i / 255) ^ 1.35
  for k = 1, #stops - 1 do
    local a, b = stops[k], stops[k + 1]
    if l <= b[1] then
      local t = (l - a[1]) / (b[1] - a[1])
      lut[i] = { math.floor(a[2] + (b[2] - a[2]) * t), math.floor(a[3] + (b[3] - a[3]) * t), math.floor(a[4] + (b[4] - a[4]) * t) }
      break
    end
  end
end
local out = Image(src.width, src.height, ColorMode.RGB)
for it in src:pixels() do
  local c = it()
  local l = math.floor(R(c) * 0.3 + G(c) * 0.55 + B(c) * 0.15)
  local m = lut[math.min(255, l)]
  out:drawPixel(it.x, it.y, rgba(m[1], m[2], m[3], A(c)))
end
out:saveAs(app.params["out"])
print("REPORT recoloured " .. out.width .. " x " .. out.height)
