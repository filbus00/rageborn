-- The pet wolf's coat made the charcoal greys of the owner's picture (2026-11-05): the shape's paint took a green cast
-- from the picture's background; every colour turns to its grey at its lightness, a little darker and more contrasted,
-- except the reds (the eyes, the collar), which stay and are widened by a few texture pixels (thicken=<pixels>), so the
-- collar still shows on a sprite thirty pixels long.
--   aseprite -b --script-param f=<albedo.png> [--script-param thicken=8] --script neutral_coat.lua
local im = Image { fromFile = app.params["f"] }
local rgba, R, G, B, A = app.pixelColor.rgba, app.pixelColor.rgbaR, app.pixelColor.rgbaG, app.pixelColor.rgbaB, app.pixelColor.rgbaA
for it in im:pixels() do
  local c = it()
  local r, g, b = R(c), G(c), B(c)
  local mx, mn = math.max(r, g, b), math.min(r, g, b)
  local red = r == mx and r - math.max(g, b) > 40 and r > 70
  if not red then
    local l = (r * 0.3 + g * 0.55 + b * 0.15) / 255
    l = math.max(0, math.min(1, (l - 0.5) * 1.3 + 0.62))
    -- A few flat tones (the projected paint is mottled), a warm charcoal (a cool grey went blue in the palette).
    local steps = { 0.13, 0.21, 0.3, 0.41, 0.54 }
    local best = steps[1]
    for _, st in ipairs(steps) do if math.abs(st - l) < math.abs(best - l) then best = st end end
    local v = math.floor(best * 255)
    it(rgba(v, v, v, A(c)))
  end
end
-- The reds widened: each pass spreads a red pixel to its neighbours across, then down (a square of 2n+1).
local n = tonumber(app.params["thicken"] or "0")
if n > 0 then
  local W, H = im.width, im.height
  local function isRed(c)
    local r, g, b = R(c), G(c), B(c)
    return A(c) > 0 and r - math.max(g, b) > 40 and r > 70
  end
  for pass = 1, 2 do
    local copy = im:clone()
    for y = 0, H - 1 do
      local run, colour = -1, 0
      -- Forward and back along the row (pass 1) or the column (pass 2), carrying the last red within n pixels.
      for i = 0, (pass == 1 and W or H) - 1 do
        local x, yy = pass == 1 and i or y, pass == 1 and y or i
        if pass == 2 and y >= W then break end
        local c = copy:getPixel(x, yy)
        if isRed(c) then run, colour = i, c elseif run >= 0 and i - run <= n then im:drawPixel(x, yy, colour) end
      end
      run = -1
      for i = (pass == 1 and W or H) - 1, 0, -1 do
        local x, yy = pass == 1 and i or y, pass == 1 and y or i
        if pass == 2 and y >= W then break end
        local c = copy:getPixel(x, yy)
        if isRed(c) then run, colour = i, c elseif run >= 0 and run - i <= n then im:drawPixel(x, yy, colour) end
      end
    end
  end
end
im:saveAs(app.params["f"])
print("REPORT coat")
