-- One wolf from the owner's strip (ArtSource/pixel/ai_exchange/pets/wolf_strip.jpg, 2026-11-05) brought down to the game's
-- size as pixel art: its harness filled in from the fur (remove_harness.lua does the same on a picture), the green
-- background keyed out, shrunk by averaging only the wolf's pixels, its colours snapped to a few greys and browns taken
-- from the strip, the alpha made hard, and a dark outline drawn round it.
--   aseprite -b --script-param f=<strip> --script-param x= y= w= h= --script-param height=<pixels> --script-param o=<out.png>
--     [--script-param flip=1] --script strip_frame.lua
local rgba, R, G, B, A = app.pixelColor.rgba, app.pixelColor.rgbaR, app.pixelColor.rgbaG, app.pixelColor.rgbaB, app.pixelColor.rgbaA
local src = Image { fromFile = app.params["f"] }
local X, Y, W, H = tonumber(app.params["x"]), tonumber(app.params["y"]), tonumber(app.params["w"]), tonumber(app.params["h"])
local im = Image(W, H, ColorMode.RGB)
im:drawImage(src, Point(-X, -Y))
local function bg(c)
  local r, g, b = R(c), G(c), B(c)
  return g > r + 6 and g > b + 4
end
local function orange(c)
  local r, g, b = R(c), G(c), B(c)
  return r > 70 and r - b > 50 and g < r * 0.78
end
-- The harness: its orange and, near it, its dark red-brown rims, filled from the fur round it.
local hole = {}
for y = 0, H - 1 do for x = 0, W - 1 do if orange(im:getPixel(x, y)) then hole[y * W + x] = true end end end
local grow = {}
for k in pairs(hole) do
  local x, y = k % W, k // W
  for dy = -6, 6 do for dx = -6, 6 do
    local nx, ny = x + dx, y + dy
    if nx >= 0 and ny >= 0 and nx < W and ny < H then
      local c = im:getPixel(nx, ny)
      local r, g, b = R(c), G(c), B(c)
      local dark = r + g + b < 150 and not bg(c)
      local buckle = r + g + b > 420 and math.abs(dx) <= 4 and math.abs(dy) <= 4
      if (math.abs(dx) <= 1 and math.abs(dy) <= 1) or (r > 22 and r - g > 5 and r - b > 8) or (dark and math.abs(dx) <= 3 and math.abs(dy) <= 3) or buckle then
        grow[ny * W + nx] = true
      end
    end
  end end
end
for k in pairs(grow) do hole[k] = true end
for _ = 1, 200 do
  local fill, any = {}, false
  for k in pairs(hole) do
    local x, y = k % W, k // W
    local sr, sg, sb, n = 0, 0, 0, 0
    for _, d in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 }, { 1, 1 }, { -1, -1 }, { 1, -1 }, { -1, 1 } }) do
      local nx, ny = x + d[1], y + d[2]
      if nx >= 0 and ny >= 0 and nx < W and ny < H and not hole[ny * W + nx] then
        local c = im:getPixel(nx, ny)
        if not bg(c) then sr, sg, sb, n = sr + R(c), sg + G(c), sb + B(c), n + 1 end
      end
    end
    if n >= 2 then fill[k] = rgba(sr // n, sg // n, sb // n, 255) end
  end
  for k, c in pairs(fill) do im:drawPixel(k % W, k // W, c); hole[k] = nil; any = true end
  if not any then break end
end
-- The wolf's box (the strip's dark outline included), then the shrink: each new pixel the average of the wolf's
-- pixels under it, clear when fewer than half are wolf.
local x0, y0, x1, y1 = W, H, -1, -1
for y = 0, H - 1 do for x = 0, W - 1 do
  if not bg(im:getPixel(x, y)) then x0, y0, x1, y1 = math.min(x0, x), math.min(y0, y), math.max(x1, x), math.max(y1, y) end
end end
-- Shrunk by a set factor (the same for every view), or to a height.
local f = app.params["factor"] and tonumber(app.params["factor"]) or (y1 - y0 + 1) / tonumber(app.params["height"])
print("REPORT factor " .. f)
local ow, oh = math.ceil((x1 - x0 + 1) / f), math.ceil((y1 - y0 + 1) / f)
local out = Image(ow + 2, oh + 2, ColorMode.RGB)
out:clear(rgba(0, 0, 0, 0))
-- The strip's own colours, a few: dark to light greys, the mane's browns, the pale cream, the eye.
local palette = {
  { 20, 19, 19 }, { 36, 35, 35 }, { 52, 51, 51 }, { 68, 67, 67 }, { 86, 85, 84 }, { 106, 105, 103 }, { 126, 125, 122 },
  { 148, 146, 142 }, { 170, 167, 161 },
  { 62, 54, 46 }, { 88, 78, 66 },
  { 196, 190, 176 }, { 222, 217, 204 }, { 214, 140, 48 },
}
local function snap(r, g, b)
  local best, bd = palette[1], 1e9
  for _, p in ipairs(palette) do
    -- Lightness counts most, then the hue (a grey must not turn brown).
    local l1, l2 = (r + g + b) / 3, (p[1] + p[2] + p[3]) / 3
    local d = 4 * (l1 - l2) ^ 2 + 2 * ((r - g) - (p[1] - p[2])) ^ 2 + 2 * ((g - b) - (p[2] - p[3])) ^ 2
    if d < bd then best, bd = p, d end
  end
  return best
end
for oy = 0, oh - 1 do
  for ox = 0, ow - 1 do
    local sr, sg, sb, n, total = 0, 0, 0, 0, 0
    for sy = math.floor(y0 + oy * f), math.floor(y0 + (oy + 1) * f) - 1 do
      for sx = math.floor(x0 + ox * f), math.floor(x0 + (ox + 1) * f) - 1 do
        if sx <= x1 and sy <= y1 then
          total = total + 1
          local c = im:getPixel(sx, sy)
          if not bg(c) then sr, sg, sb, n = sr + R(c), sg + G(c), sb + B(c), n + 1 end
        end
      end
    end
    if total > 0 and n * 2 >= total then
      local p = snap(sr / n, sg / n, sb / n)
      out:drawPixel(ox + 1, oy + 1, rgba(p[1], p[2], p[3], 255))
    end
  end
end
-- A lone pixel whose four neighbours share one colour takes it (the shrink leaves grain).
local copy = out:clone()
for y = 1, out.height - 2 do for x = 1, out.width - 2 do
  local c = copy:getPixel(x, y)
  if A(c) > 0 then
    local l, r, u, d = copy:getPixel(x - 1, y), copy:getPixel(x + 1, y), copy:getPixel(x, y - 1), copy:getPixel(x, y + 1)
    if A(l) > 0 and l == r and l == u and l == d and l ~= c then out:drawPixel(x, y, l) end
  end
end end
-- The outline: clear pixels touching the wolf go dark.
local solid = {}
for y = 0, out.height - 1 do for x = 0, out.width - 1 do solid[y * out.width + x] = A(out:getPixel(x, y)) > 0 end end
for y = 0, out.height - 1 do for x = 0, out.width - 1 do
  if not solid[y * out.width + x] then
    for _, d in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
      local nx, ny = x + d[1], y + d[2]
      if nx >= 0 and ny >= 0 and nx < out.width and ny < out.height and solid[ny * out.width + nx] then
        out:drawPixel(x, y, rgba(14, 12, 12, 255))
        break
      end
    end
  end
end end
if app.params["flip"] == "1" then out:flip(FlipType.HORIZONTAL) end
out:saveAs(app.params["o"])
print("REPORT frame " .. out.width .. " x " .. out.height)
