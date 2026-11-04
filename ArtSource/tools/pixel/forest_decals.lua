-- The forest's ground marks drawn as pixel art (2026-11-04, the owner: "do it", after the trees): the marks
-- DungeonDressing.ForestDecalWeights lays (leaves, flowers, pebbles, puddles, bones, blood). Each is stamped into one
-- ground tile (DungeonArt.StampDecal). The forest's tiles are 54 x 27 (2026-11-05, the ground on the screen's grid, two
-- tile pixels a screen pixel), so each mark is drawn on the screen's 27 x 14 grid, enlarged twice and cut to 54 x 27,
-- and keeps inside the tile's diamond (the stamp shows only over the ground). Drawn with kit.lua; no black outline (they
-- lie flat on the ground), a dark pixel under each thing for its shadow. Written to ArtSource/pixel/dungeon27.
--   aseprite -b --script-param out=<folder> --script-param kit=<kit.lua> [--script-param only=<name,name>] --script forest_decals.lua
local kit = dofile(app.params["kit"])
local out = app.params["out"] or "."
local only = app.params["only"]
local K = kit
local SHADOW = app.pixelColor.rgba(20, 30, 16, 255)

local marks = {}

local TW, TH = 27, 14
local CX, CY = TW / 2, TH / 2

local function tile()
  local c = kit.new(TW, TH)
  c.exact = true
  c.upscale, c.finalW, c.finalH = 2, 54, 27
  return c
end

-- Whether (x, y) lies inside the tile's diamond, with a margin.
local function inDiamond(x, y, margin)
  return math.abs(x + 0.5 - CX) / CX + math.abs(y + 0.5 - CY) / CY <= 1 - (margin or 0)
end

-- Spots scattered over the diamond, never too close together.
local function scatter(rand, n, margin, gap)
  local spots, tries = {}, 0
  while #spots < n and tries < 400 do
    tries = tries + 1
    local x, y = rand(2, TW - 3), rand(1, TH - 2)
    if inDiamond(x, y, margin) then
      local ok = true
      for _, s in ipairs(spots) do
        if math.abs(s[1] - x) + math.abs(s[2] - y) * 2 < gap then ok = false end
      end
      if ok then spots[#spots + 1] = { x, y } end
    end
  end
  return spots
end

-- Fallen leaves: small leaf shapes, a lit and a shaded half each, in autumn reds, golds and dried browns, a twig or two.
local function leaves(seed)
  local c = tile()
  local rand = kit.random(seed)
  local ramps = { K.AUTUMN, K.GOLD, K.WOOD, K.AUTUMN }
  for _, s in ipairs(scatter(rand, 5, 0.18, 4)) do
    local ramp = ramps[rand(1, #ramps)]
    local x, y, flip = s[1], s[2], rand(0, 1) == 1
    local d = flip and -1 or 1
    c:px(x + d, y + 1, SHADOW, false); c:px(x + 2 * d, y + 1, SHADOW, false)
    c:px(x - d, y, ramp[5], false); c:px(x, y, ramp[4], false); c:px(x + d, y, ramp[3], false)
    c:px(x, y - 1, ramp[4], false); c:px(x + d, y - 1, ramp[4], false); c:px(x + 2 * d, y, ramp[2], false)
  end
  for k = 1, 1 do
    local s = scatter(rand, 1, 0.3, 0)[1]
    if s then
      c:line(s[1], s[2], s[1] + 3, s[2] - 1, K.WOOD[3], false)
      c:px(s[1] + 1, s[2] - 2, K.WOOD[3], false)
    end
  end
  return c
end

-- Flowers: little patches of them, each a yellow eye with four petals on a dark leaf.
local function flowers(seed, petals)
  local c = tile()
  local rand = kit.random(seed)
  for _, patch in ipairs(scatter(rand, 2, 0.3, 9)) do
    for _, f in ipairs({ { 0, 0 }, { 3, 1 }, { -3, -1 } }) do
      if rand() < 0.8 then
        local x, y = patch[1] + f[1], patch[2] + f[2]
        if inDiamond(x, y, 0.1) then
          c:px(x - 1, y + 1, K.FERN[2], false); c:px(x + 1, y + 1, K.FERN[3], false)
          c:px(x, y + 2, SHADOW, false)
          c:px(x, y - 1, petals[2], false); c:px(x - 1, y, petals[2], false); c:px(x + 1, y, petals[1], false)
          c:px(x, y + 1, petals[1], false)
          c:px(x, y, K.EMBER[5], false)
        end
      end
    end
  end
  return c
end

-- Pebbles: a few small rounded stones, lit on their upper left, each with its shadow below and to the right.
local function pebbles(seed)
  local c = tile()
  local rand = kit.random(seed)
  for _, s in ipairs(scatter(rand, 4, 0.15, 5)) do
    local w, h = rand(1, 2), 1
    local x, y = s[1], s[2]
    for dx = 0, w do c:px(x + dx + 1, y + h + 1, SHADOW, false) end
    for dy = 0, h do
      for dx = 0, w do
        local tone = (dx == 0 or dy == 0) and 5 or (dx == w or dy == h) and 3 or 4
        c:px(x + dx, y + dy, K.STONE[tone], false)
      end
    end
    c:px(x, y, K.STONE[6], false)
  end
  return c
end

-- A puddle: dark water in a soft outline, its far rim lit, a streak of sky across it, the mud round its edge.
local function puddle(seed, rx, ry)
  local c = tile()
  local rand = kit.random(seed)
  local cx, cy = CX + rand(-1, 1), CY
  c:ellipse(cx, cy, rx + 1.5, ry + 1, function(x, y, u, v)
    if not inDiamond(x, y, 0.05) then return nil end
    return K.EARTH[2]
  end, false)
  c:ellipse(cx, cy, rx, ry, function(x, y, u, v)
    if not inDiamond(x, y, 0.08) then return nil end
    if v < -0.6 then return K.COLD[4] end
    if math.abs(v + 0.15 - u * 0.3) < 0.12 and u > -0.5 and u < 0.4 then return K.COLD[5] end
    return v < 0 and K.COLD[2] or K.COLD[1]
  end, false)
  return c
end

-- Bones: a long bone and a few small ones scattered, in the bone ramp, each with its shadow.
local function bones(seed)
  local c = tile()
  local rand = kit.random(seed)
  local B = K.BONES
  local s = scatter(rand, 1, 0.35, 0)[1] or { 12, 7 }
  local x, y = s[1] - 3, s[2]
  for k = 0, 5 do c:px(x + k, y + (k // 4), B[4], false); c:px(x + k, y + 1 + (k // 4), SHADOW, false) end
  c:px(x - 1, y - 1, B[5], false); c:px(x - 1, y + 1, B[3], false); c:px(x + 6, y, B[4], false); c:px(x + 6, y + 2, B[3], false)
  for _, b in ipairs(scatter(rand, 2, 0.2, 6)) do
    c:px(b[1], b[2], B[4], false); c:px(b[1] + 1, b[2], B[3], false); c:px(b[1] + 1, b[2] + 1, SHADOW, false)
  end
  return c
end

-- Blood: a splash, a dark pool with a wet glint and drops flung out from it.
local function blood(seed)
  local c = tile()
  local rand = kit.random(seed)
  local cx, cy = CX + rand(-2, 2), CY
  -- An uneven splash: a few overlapping blobs of different sizes, the drops flung out past them.
  local blobs = { { cx, cy, rand(3, 4), 2 } }
  for k = 1, rand(2, 3) do
    local a = rand() * math.pi * 2
    blobs[#blobs + 1] = { cx + math.cos(a) * rand(2, 4), cy + math.sin(a) * rand(1, 2), rand(1, 3), 1 }
  end
  for _, bl in ipairs(blobs) do
    c:ellipse(bl[1], bl[2], bl[3], bl[4], function(x, y, u, v)
      if not inDiamond(x, y, 0.08) then return nil end
      return v < -0.2 and K.RED[3] or K.RED[2]
    end, false)
  end
  c:px(cx - 1, cy - 1, K.RED[4], false)
  for k = 1, 5 do
    local a = rand() * math.pi * 2
    local d = 4 + rand() * 3
    local x, y = cx + math.cos(a) * d, cy + math.sin(a) * d * 0.45
    if inDiamond(x, y, 0.08) then
      c:px(x, y, K.RED[3], false)
      if rand() < 0.4 then c:px(x + 1, y, K.RED[2], false) end
    end
  end
  return c
end

marks.decal_leaves_1 = function() return leaves(3) end
marks.decal_leaves_2 = function() return leaves(17) end
marks.decal_flowers_1 = function() return flowers(5, { K.BONE[2], K.BONE[3] }) end
marks.decal_flowers_2 = function() return flowers(23, { app.pixelColor.rgba(118, 84, 150, 255), app.pixelColor.rgba(156, 120, 190, 255) }) end
marks.decal_pebbles_1 = function() return pebbles(7) end
marks.decal_pebbles_2 = function() return pebbles(31) end
marks.decal_puddle_1 = function() return puddle(2, 6, 3) end
marks.decal_puddle_2 = function() return puddle(9, 4, 2) end
marks.decal_bones_1 = function() return bones(4) end
marks.decal_bones_2 = function() return bones(19) end
marks.decal_blood_1 = function() return blood(1) end
marks.decal_blood_2 = function() return blood(8) end
marks.decal_blood_3 = function() return blood(15) end
marks.decal_blood_4 = function() return blood(27) end

for name, draw in pairs(marks) do
  if not only or string.find("," .. only .. ",", "," .. name .. ",", 1, true) then
    draw():save(out, name)
  end
end
