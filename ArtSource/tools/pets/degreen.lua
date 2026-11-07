-- Takes the picture's green background out of a pet's paint (2026-11-05): a shape painted from a picture on dark green
-- takes green where the picture's outline runs past the figure; every greenish pixel turns the grey of its lightness,
-- leaving the fur's own greys and browns as they are.
--   aseprite -b --script-param f=<albedo.png> --script degreen.lua
local im = Image { fromFile = app.params["f"] }
local rgba, R, G, B, A = app.pixelColor.rgba, app.pixelColor.rgbaR, app.pixelColor.rgbaG, app.pixelColor.rgbaB, app.pixelColor.rgbaA
for it in im:pixels() do
  local c = it()
  local r, g, b = R(c), G(c), B(c)
  if g > r + 4 and g > b + 4 then
    local v = math.floor(r * 0.3 + g * 0.55 + b * 0.15)
    it(rgba(v, v, math.max(0, v - 3), A(c)))
  end
end
im:saveAs(app.params["f"])
print("REPORT degreened")
