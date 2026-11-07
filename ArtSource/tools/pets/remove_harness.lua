-- Takes the orange harness out of the owner's wolf picture (2026-11-05, the owner: "Remove the colar"): its orange (and
-- the buckle's dark rims next to it) is filled in from the fur around it, a ring at a time, then the picture is enlarged.
--   aseprite -b --script-param f=<in.png> --script-param o=<out.png> [--script-param s=3] --script remove_harness.lua
local im = Image { fromFile = app.params["f"] }
local W, H = im.width, im.height
local rgba, R, G, B = app.pixelColor.rgba, app.pixelColor.rgbaR, app.pixelColor.rgbaG, app.pixelColor.rgbaB
local hole = {}
local function orange(c)
  local r, g, b = R(c), G(c), B(c)
  return r > 70 and r - b > 50 and g < r * 0.78
end
for y = 0, H - 1 do
  for x = 0, W - 1 do
    if orange(im:getPixel(x, y)) then hole[y * W + x] = true end
  end
end
-- The harness's dark red-brown shading and rims, only near its orange (the eyes and ears are warm brown too): within
-- seven pixels of it, any reddish dark pixel, and every pixel right beside it.
local grow = {}
for k in pairs(hole) do
  local x, y = k % W, k // W
  for dy = -7, 7 do for dx = -7, 7 do
    local nx, ny = x + dx, y + dy
    if nx >= 0 and ny >= 0 and nx < W and ny < H then
      local c = im:getPixel(nx, ny)
      local r, g, b = R(c), G(c), B(c)
      local dark = r + g + b < 150 and not (g > r + 6 and g > b + 6)
      if (math.abs(dx) <= 1 and math.abs(dy) <= 1) or (r > 22 and r - g > 5 and r - b > 8) or (dark and math.abs(dx) <= 4 and math.abs(dy) <= 4) then
        grow[ny * W + nx] = true
      end
    end
  end end
end
for k in pairs(grow) do hole[k] = true end
local left = 0
for _ in pairs(hole) do left = left + 1 end
local stuck = false
while left > 0 do
  local fill = {}
  for k in pairs(hole) do
    local x, y = k % W, k // W
    local sr, sg, sb, n = 0, 0, 0, 0
    for _, d in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 }, { 1, 1 }, { -1, -1 }, { 1, -1 }, { -1, 1 } }) do
      local nx, ny = x + d[1], y + d[2]
      if nx >= 0 and ny >= 0 and nx < W and ny < H and not hole[ny * W + nx] then
        local c = im:getPixel(nx, ny)
        -- Not from the green background.
        if not (G(c) > R(c) + 6 and G(c) > B(c) + 6) then sr, sg, sb, n = sr + R(c), sg + G(c), sb + B(c), n + 1 end
      end
    end
    if n >= 2 then fill[k] = rgba(sr // n, sg // n, sb // n, 255) elseif stuck then fill[k] = im:getPixel(x, y) end
  end
  local any = false
  for k, c in pairs(fill) do
    im:drawPixel(k % W, k // W, c); hole[k] = nil; left = left - 1; any = true
  end
  if not any then
    if stuck then break end
    stuck = true
  end
end
local s = tonumber(app.params["s"] or "1")
im:resize(W * s, H * s)
im:saveAs(app.params["o"])
print("REPORT harness removed")
