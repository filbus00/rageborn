-- The bandits' tent, drawn as pixel art at the game's new scale (2026-11-04, the owner: "redraw the art at the new
-- scale ... start with something a bit simpler, like some of the props. Start with the tents"): 27 pixels a world unit,
-- the rendered size since the camera moved back (RenderResolution.RenderedUnit), so every pixel here is one pixel on
-- screen. A canvas ridge tent seen from the game's camera: its front a shaded triangle with the door open and one flap
-- tied back, its right slope lit, a red bandit stripe and a patch, the ridge pole's ends, two guy ropes to their pegs and
-- a dithered shadow. Colours are the game's palette (PixelArt.cs), outlined in its near-black.
--   aseprite -b --script-param out=<folder> --script ArtSource/tools/pixel/bandit_tent.lua
-- Writes <folder>/prop_bandit_tent.png and .aseprite (open it in Aseprite to touch it up by hand).
local out = app.params["out"] or "."
local W, H = 46, 40
local sprite = Sprite(W, H, ColorMode.RGB)
local image = sprite.cels[1].image
local rgba = app.pixelColor.rgba

local OUTLINE = rgba(14, 10, 10, 255)
local CANVAS = { rgba(72, 47, 30, 255), rgba(100, 68, 43, 255), rgba(132, 94, 60, 255), rgba(168, 128, 86, 255), rgba(222, 186, 150, 255) }
local RED = { rgba(70, 15, 14, 255), rgba(108, 24, 20, 255), rgba(150, 38, 28, 255) }
local WOOD = { rgba(28, 18, 13, 255), rgba(48, 31, 21, 255), rgba(72, 47, 30, 255) }
local ROPE = rgba(186, 142, 110, 255)
local DOOR = rgba(30, 26, 26, 255)
local SHADOW = rgba(14, 10, 10, 255)

local solid = {}   -- pixels the outline goes round
local function key(x, y) return y * W + x end
local function put(x, y, c, outlined)
  if x < 0 or y < 0 or x >= W or y >= H then return end
  image:drawPixel(x, y, c)
  if outlined ~= false then solid[key(x, y)] = true end
end

-- Whether (x, y) lies inside a polygon (even-odd rule), the polygon given as {x1, y1, x2, y2, ...}.
local function inside(poly, x, y)
  local n, c = #poly // 2, false
  local j = n
  for i = 1, n do
    local xi, yi, xj, yj = poly[2 * i - 1], poly[2 * i], poly[2 * j - 1], poly[2 * j]
    if ((yi > y) ~= (yj > y)) and (x < (xj - xi) * (y - yi) / (yj - yi) + xi) then c = not c end
    j = i
  end
  return c
end

local function fill(poly, shade)
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      if inside(poly, x + 0.5, y + 0.5) then put(x, y, shade(x, y)) end
    end
  end
end

local function line(x0, y0, x1, y1, c, outlined)
  local dx, dy = math.abs(x1 - x0), -math.abs(y1 - y0)
  local sx, sy = x0 < x1 and 1 or -1, y0 < y1 and 1 or -1
  local err = dx + dy
  while true do
    put(x0, y0, c, outlined)
    if x0 == x1 and y0 == y1 then break end
    local e2 = 2 * err
    if e2 >= dy then err = err + dy; x0 = x0 + sx end
    if e2 <= dx then err = err + dx; y0 = y0 + sy end
  end
end

-- The ground under it: a dithered shadow, darker toward its middle (hard pixels only, so a checker of the outline).
local cx, cy = 23, 33
for y = 28, 39 do
  for x = 0, W - 1 do
    local u, v = (x - cx) / 19, (y - cy) / 3.5
    if u * u + v * v < 1 and (x + y) % 2 == 0 then image:drawPixel(x, y, SHADOW) end
  end
end

-- The tent: the front triangle A L R, the lit right slope A R R' A' going back up and to the right.
local A, L, R = { 15, 7 }, { 3, 33 }, { 27, 33 }
local D = { 14, -6 }
local A2, R2 = { A[1] + D[1], A[2] + D[2] }, { R[1] + D[1], R[2] + D[2] }

-- The slope: lit canvas, its folds running down it, a red stripe along its foot.
fill({ A[1], A[2], A2[1], A2[2], R2[1] + 1, R2[2], R[1] + 1, R[2] }, function(x, y)
  -- Where along the slope, 0 at the ridge to 1 at its foot.
  local t = (y - (A[2] + (x - A[1]) * D[2] / D[1])) / (R[2] - A[2])
  if t > 0.78 and t < 0.9 then return RED[3] end
  if t >= 0.9 then return RED[2] end
  -- Folds: lines running down the slope, every few pixels along the ridge.
  local along = (x - A[1]) - (y - A[2]) * (R[1] - A[1]) / (R[2] - A[2])
  if math.floor(along + 0.5) % 5 == 0 and t > 0.15 then return CANVAS[3] end
  return t < 0.12 and CANVAS[5] or CANVAS[4]
end)
-- A patch on the slope, sewn on.
for y = 0, 3 do
  for x = 0, 4 do
    put(29 + x - y // 2, 17 + y, (x == 0 or x == 4 or y == 0 or y == 3) and RED[1] or RED[3])
  end
end
-- The front: in shade, darker toward its edges.
fill({ A[1], A[2], L[1], L[2], R[1], R[2] }, function(x, y)
  local edge = math.min(x - (A[1] + (y - A[2]) * (L[1] - A[1]) / (L[2] - A[2])), (A[1] + (y - A[2]) * (R[1] - A[1]) / (R[2] - A[2])) - x)
  if y > 29 then return RED[1] end
  if edge < 1.5 then return CANVAS[2] end
  return CANVAS[3]
end)
-- The door: a dark opening, its left flap tied back as a lighter fold.
fill({ 15, 17, 10, 33, 20, 33 }, function() return DOOR end)
fill({ 15, 17, 10, 33, 8, 33, 12, 22 }, function(x, y) return y % 3 == 0 and CANVAS[3] or CANVAS[4] end)
put(9, 26, ROPE)
put(10, 26, ROPE)
-- The ridge: its line lit along the top, the pole's ends standing out at both apexes.
line(A[1], A[2], A2[1], A2[2], CANVAS[5])
line(A[1], A[2] - 1, A[1], A[2] - 3, WOOD[2])
line(A2[1], A2[2] - 1, A2[1], A2[2] - 3, WOOD[2])
put(A[1], A[2] - 3, WOOD[3])
put(A2[1], A2[2] - 3, WOOD[3])

-- The outline: every clear pixel beside the tent's pixels takes the near-black.
local edge = {}
for y = 0, H - 1 do
  for x = 0, W - 1 do
    if not solid[key(x, y)] then
      for _, d in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
        if solid[key(x + d[1], y + d[2])] then
          edge[#edge + 1] = { x, y }
          break
        end
      end
    end
  end
end
for _, p in ipairs(edge) do image:drawPixel(p[1], p[2], OUTLINE) end

-- Guy ropes, from the apexes down to their pegs, drawn over everything with no outline of their own.
line(A[1] - 1, A[2] - 1, 1, 30, ROPE, false)
line(A2[1] + 1, A2[2] - 1, 44, 24, ROPE, false)
for _, peg in ipairs({ { 1, 31 }, { 44, 25 } }) do
  put(peg[1], peg[2], WOOD[1], false)
  put(peg[1], peg[2] + 1, WOOD[1], false)
end

sprite:saveAs(out .. "/prop_bandit_tent.aseprite")
sprite:saveCopyAs(out .. "/prop_bandit_tent.png")
print("REPORT wrote " .. out .. "/prop_bandit_tent.png " .. W .. " x " .. H)
