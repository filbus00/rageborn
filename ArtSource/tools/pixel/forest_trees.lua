-- The forest's trees and bushes drawn as pixel art at the rendered scale (2026-11-04, the owner: "do it", after the
-- props: the trees fill most of the screen). They are the forest's walls, one on every wall cell beside the floor
-- (ThemePieces.Wall: forest_tree_1..9, forest_bush_1..3 for clumps standing free), so they overlap their neighbours
-- into a canopy. Drawn with kit.lua at 27 pixels a unit, the foot BASE rows above the bottom; imported from
-- ArtSource/pixel/world27 by Import World Art.
--   aseprite -b --script-param out=<folder> --script-param kit=<kit.lua> [--script-param only=<name,name>] --script forest_trees.lua
local kit = dofile(app.params["kit"])
local out = app.params["out"] or "."
local only = app.params["only"]
local K = kit

local trees = {}

-- A pine: tiers of drooping boughs stacked up a trunk, each tier widening from the one above to a ragged hem of needle
-- tufts that hang lowest at its tips, in flat bands lit from the left (its left edge brightest, its right in shade),
-- the underside of each tier dark where it shades the tier below; the trunk showing under the lowest tier. Each seed
-- gives it its own tiers, widths and lean.
local function pine(seed)
  local c = kit.new(40, 82)
  local g = c.ground
  local rand = kit.random(seed)
  local cx = 20
  local tiers = rand(6, 7)
  local top = g - 70 + rand(0, 4)
  local lean = (rand() - 0.5) * 3
  c:shadow(cx + 1, g, 13, 4)
  for y = g - 12, g do
    c:px(cx - 2, y, K.WOOD[3]); c:px(cx - 1, y, K.WOOD[3]); c:px(cx, y, K.WOOD[2]); c:px(cx + 1, y, K.WOOD[1])
  end
  c:px(cx - 3, g, K.WOOD[2]); c:px(cx + 2, g, K.WOOD[1])
  -- From the lowest tier up, so each tier lies over the one below.
  for i = tiers, 1, -1 do
    local f = (i - 1) / (tiers - 1)
    local bottom = math.floor(g - 9 - (1 - f) * (g - 9 - top - 10))
    local tierTop = bottom - math.floor(14 - f * 4) - rand(0, 2)
    if i == 1 then tierTop = top end
    local half = 4 + f * 13 + rand(0, 2)
    local mid = cx + lean * (1 - f)
    local hems = {}
    for x = math.floor(mid - half) - 1, math.ceil(mid + half) + 1 do hems[x] = rand(0, 2) end
    for y = tierTop, bottom + 3 do
      local t = (y - tierTop) / math.max(1, bottom - tierTop)
      local w = half * math.min(1, t) ^ 0.85
      for x = math.floor(mid - w), math.ceil(mid + w) - 1 do
        local u = (x + 0.5 - mid) / math.max(1, half)
        -- The hem: tufts hanging lower toward its tips.
        local hang = bottom + math.floor(math.abs(u) * 2.5) + (hems[x] or 0) - 1
        if y <= hang then
          local tone
          if y >= hang - 1 then tone = 1
          elseif u < -0.55 then tone = 5
          elseif u < -0.1 then tone = 4
          elseif u < 0.4 then tone = 3
          else tone = 2 end
          -- Needle texture: a darker fleck now and then, running down the boughs.
          if tone > 2 and (x * 3 + y * 7) % 11 == 0 then tone = tone - 1 end
          c:px(x, y, K.PINE[tone])
        end
      end
    end
  end
  c:outline()
  return c
end

-- A broadleaf tree: a dome of leafy clumps, each a scalloped round in flat bands lit from the upper left with a dark
-- rim on its lower right that parts it from the clump behind; the far clumps up top drawn first and the low front ones
-- last, darker where the crown shades itself underneath; a trunk flaring at its foot, forking into limbs that show in
-- the gaps under the crown. Each seed gives it its own clumps.
local function broadleaf(seed, leaves, opts)
  leaves = leaves or K.LEAF
  opts = opts or {}
  local c = kit.new(opts.w or 72, opts.h or 76)
  local g = c.ground
  local rand = kit.random(seed)
  local cx = (opts.w or 72) // 2
  c:shadow(cx + 2, g, opts.shadow or 22, 6)
  if opts.trunk then opts.trunk(c, cx, g) else
  -- The trunk and its limbs.
  c:poly({ cx - 6, g + 1, cx - 3, g - 6, cx - 3, g - 22, cx + 3, g - 22, cx + 3, g - 6, cx + 6, g + 1 }, function(x, y)
    return x < cx - 1 and K.WOOD[3] or x < cx + 1 and K.WOOD[2] or K.WOOD[1]
  end)
  c:stroke(cx - 1, g - 20, cx - 12, g - 32, 1.4, function(x, y) return K.WOOD[2] end)
  c:stroke(cx + 1, g - 20, cx + 13, g - 30, 1.4, function(x, y) return K.WOOD[1] end)
  c:stroke(cx, g - 22, cx + 2, g - 36, 1.6, function(x, y) return K.WOOD[2] end)
  end
  -- The clumps, from the top of the dome down.
  local clumps = {}
  local rows = opts.rows and opts.rows(g) or { { g - 58, 2, 8 }, { g - 50, 3, 16 }, { g - 41, 4, 24 }, { g - 32, 4, 27 }, { g - 25, 3, 20 } }
  for r, row in ipairs(rows) do
    for k = 1, row[2] do
      local t = row[2] == 1 and 0 or (k - 1) / (row[2] - 1) * 2 - 1
      clumps[#clumps + 1] = { cx + t * row[3] + rand(-2, 2), row[1] + rand(-2, 2), (opts.clump or 7) + rand(0, 2), r }
    end
  end
  for _, cl in ipairs(clumps) do
    local bx, by, rad, row = cl[1], cl[2], cl[3], cl[4]
    local bumps, phase = 5 + rand(0, 2), rand() * 6
    local under = row >= 4 and 1 or 0
    for y = math.floor(by - rad - 2), math.ceil(by + rad + 2) do
      for x = math.floor(bx - rad - 2), math.ceil(bx + rad + 2) do
        local dx, dy = x + 0.5 - bx, (y + 0.5 - by) * 1.1
        local a = math.atan(dy, dx)
        local edge = rad * (0.88 + 0.12 * math.cos(a * bumps + phase))
        local d = math.sqrt(dx * dx + dy * dy)
        if d <= edge then
          local u, v = dx / rad, dy / rad
          local q = u * 0.6 + v * 0.8
          local tone = q < -0.55 and 5 or q < -0.05 and 4 or q < 0.45 and 3 or 2
          if d > edge - 1.2 and q > 0.2 then tone = 1 end
          if tone > 2 and (x * 5 + y * 3) % 13 == 0 then tone = tone - 1 end
          c:px(x, y, leaves[math.max(1, tone - under)])
        end
      end
    end
  end
  c:outline()
  return c
end

-- A limb of bark from (x, y) at an angle, tapering from `thick`, lit along its left side; it forks into two thinner
-- limbs at its end until they are twigs.
local function limb(c, rand, x, y, angle, length, thick, bark)
  local ex, ey = x + math.cos(angle) * length, y + math.sin(angle) * length
  local steps = math.max(2, math.floor(length * 1.5))
  for i = 0, steps do
    local t = i / steps
    local r = thick * (1 - t * 0.35)
    local px, py = x + (ex - x) * t + math.sin(t * 3 + angle * 2) * 0.6, y + (ey - y) * t
    if r < 0.7 then
      c:px(px, py, bark[2])
    else
      c:ellipse(px, py, r, r, function(qx, qy, u, v) return u < -0.3 and bark[5] or u < 0.35 and bark[4] or bark[2] end)
    end
  end
  if thick > 0.55 then
    local n = thick > 1.2 and 2 or rand(1, 2)
    for k = 1, n do
      local spread = (k == 1 and -1 or 1) * (0.35 + rand() * 0.45)
      limb(c, rand, ex, ey, angle + spread, length * (0.62 + rand() * 0.15), thick * 0.62, bark)
    end
  end
end

-- The dead tree: a gnarled trunk splitting into crooked bare limbs that end in twigs, in dead grey-brown bark
-- (`kit.DEADWOOD`) lit along their left sides, roots splayed at its foot.
local function deadtree(seed)
  local c = kit.new(54, 84)
  local g = c.ground
  local rand = kit.random(seed)
  local cx = 27
  local B = K.DEADWOOD
  c:shadow(cx + 1, g, 12, 4)
  for _, r in ipairs({ { -7, 1 }, { 7, 1 }, { -3, 3 }, { 4, 3 } }) do
    c:stroke(cx, g - 2, cx + r[1], g + r[2], 1.2, function(x, y) return y < g and B[4] or B[3] end)
  end
  c:poly({ cx - 4, g + 1, cx - 3, g - 16, cx - 2, g - 32, cx + 2, g - 32, cx + 3, g - 16, cx + 4, g + 1 }, function(x, y)
    if (x * 3 + y) % 9 == 0 then return B[2] end
    return x < cx - 1 and B[5] or x < cx + 2 and B[4] or B[2]
  end)
  limb(c, rand, cx - 1, g - 31, -1.85, 15, 2.2, B)
  limb(c, rand, cx + 1, g - 31, -1.3, 15, 2.2, B)
  limb(c, rand, cx, g - 20, -0.6, 10, 1.3, B)
  limb(c, rand, cx - 1, g - 24, -2.6, 9, 1.1, B)
  c:outline()
  return c
end

-- The oak: a broad dome of autumn leaves (`kit.AUTUMN`) over a thick gnarled trunk with roots and spreading limbs.
local function oak(seed)
  return broadleaf(seed, K.AUTUMN, {
    w = 80, h = 76, clump = 7,
    rows = function(g) return { { g - 56, 3, 14 }, { g - 48, 4, 24 }, { g - 39, 5, 31 }, { g - 31, 4, 30 } } end,
    trunk = function(c, cx, g)
      for _, r in ipairs({ { -10, 1 }, { 10, 1 }, { -5, 3 }, { 6, 3 } }) do
        c:stroke(cx, g - 2, cx + r[1], g + r[2], 1.6, function(x, y) return y < g and K.WOOD[3] or K.WOOD[2] end)
      end
      c:poly({ cx - 7, g + 1, cx - 4, g - 8, cx - 5, g - 22, cx + 5, g - 22, cx + 4, g - 8, cx + 7, g + 1 }, function(x, y)
        if (x * 5 + y * 2) % 11 == 0 then return K.WOOD[1] end
        if y > g - 5 and x < cx and (x + y) % 3 ~= 0 then return K.MOSS[3] end
        return x < cx - 2 and K.WOOD[4] or x < cx + 2 and K.WOOD[3] or K.WOOD[2]
      end)
      c:stroke(cx - 2, g - 20, cx - 20, g - 30, 2, function(x, y) return K.WOOD[3] end)
      c:stroke(cx + 2, g - 20, cx + 20, g - 29, 2, function(x, y) return K.WOOD[2] end)
      c:stroke(cx, g - 21, cx - 2, g - 38, 2, function(x, y) return K.WOOD[3] end)
    end,
  })
end

-- The birch: a slender white trunk, a little bent, lit on its left and marked with dark bands, under a light crown of
-- small golden clumps (`kit.GOLD`).
local function birch(seed)
  return broadleaf(seed, K.GOLD, {
    w = 46, h = 80, clump = 5, shadow = 11,
    rows = function(g) return { { g - 64, 1, 0 }, { g - 57, 2, 7 }, { g - 49, 3, 11 }, { g - 41, 3, 12 }, { g - 34, 2, 9 } } end,
    trunk = function(c, cx, g)
      for y = g - 50, g do
        local bend = math.floor(math.sin((g - y) / 50 * 2.2) * 2 + 0.5)
        local mark = (y * 7) % 9 == 0 or (y * 5) % 13 == 0
        c:px(cx - 2 + bend, y, mark and K.STONE[2] or K.BONE[3])
        c:px(cx - 1 + bend, y, mark and K.OUTLINE or K.BONE[2])
        c:px(cx + bend, y, mark and K.STONE[2] or K.STONE[5])
      end
      c:stroke(cx + 1, g - 36, cx + 9, g - 46, 0.6, function() return K.STONE[5] end)
      c:stroke(cx - 1, g - 40, cx - 8, g - 50, 0.6, function() return K.BONE[2] end)
    end,
  })
end

-- The bushes: clumps standing free in a clearing. A leafy bush, a dark bramble with red berries, and a bush grown up
-- against a mossy boulder; the clumps as the broadleaf's, small and low on the ground.
local function bush(seed, leaves, rows, extra, rock)
  local c = broadleaf(seed, leaves, {
    w = 34, h = 30, clump = 4, shadow = 11,
    rows = rows,
    trunk = function() end,
  })
  local g = c.ground
  if rock then
    -- The boulder in front of the leaves, moss over its top.
    c:ellipse(c.W // 2 + 5, g - 3, 6.5, 4.5, function(x, y, u, v)
      local q = u * 0.6 + v * 0.8
      if v < -0.3 and (x + y) % 3 ~= 0 then return K.MOSS[4] end
      return q < -0.4 and K.STONE[5] or q < 0.3 and K.STONE[4] or K.STONE[3]
    end)
    c:outline()
  end
  if extra then extra(c, g) end
  return c
end

trees.forest_tree_1 = function() return pine(11) end
trees.forest_tree_2 = function() return pine(27) end
trees.forest_tree_3 = function() return pine(53) end
trees.forest_tree_4 = function() return broadleaf(7) end
trees.forest_tree_5 = function() return broadleaf(19) end
trees.forest_tree_6 = function() return broadleaf(41) end
trees.forest_tree_7 = function() return deadtree(5) end
trees.forest_tree_8 = function() return oak(13) end
trees.forest_tree_9 = function() return birch(29) end
trees.forest_bush_1 = function()
  return bush(3, K.LEAF, function(g) return { { g - 14, 2, 4 }, { g - 9, 3, 8 }, { g - 4, 3, 9 }, { g - 2, 2, 5 } } end)
end
trees.forest_bush_2 = function()
  return bush(9, K.PINE, function(g) return { { g - 12, 2, 5 }, { g - 7, 3, 9 }, { g - 3, 2, 7 } } end, function(c, g)
    for _, b in ipairs({ { 11, g - 12 }, { 19, g - 9 }, { 14, g - 5 }, { 23, g - 4 }, { 9, g - 6 } }) do
      c:px(b[1], b[2], K.RED[5]); c:px(b[1] + 1, b[2], K.RED[3]); c:px(b[1], b[2] + 1, K.RED[3])
    end
  end)
end
trees.forest_bush_3 = function()
  return bush(17, K.LEAF, function(g) return { { g - 11, 1, 0 }, { g - 7, 2, 5 }, { g - 3, 2, 6 } } end, nil, true)
end

for name, draw in pairs(trees) do
  if not only or string.find("," .. only .. ",", "," .. name .. ",", 1, true) then
    draw():save(out, name)
  end
end
