-- The forest's props drawn as pixel art at the rendered scale (2026-11-04, the owner: "redraw the art at the new scale ...
-- do all the others props"): every prop the forest places (DungeonDressing.ForestDrawn), one function each, drawn with
-- kit.lua. 27 pixels a unit; the ground line BASE rows above the bottom.
--   aseprite -b --script-param out=<folder> --script-param kit=<kit.lua> [--script-param only=<name,name>] --script forest_props.lua
-- Writes <folder>/<name>.png and .aseprite for each.
local kit = dofile(app.params["kit"])
local out = app.params["out"] or "."
local only = app.params["only"]
local K = kit

local props = {}

-- The bandits' canvas ridge tent (first drawn alone in bandit_tent.lua). Its depth runs back at the camera's 2:1, and its
-- shadow lies under the whole of it (the owner: "the tents right side looks to be floating in the air").
props.prop_bandit_tent = function()
  local c = kit.new(46, 42)
  local g = c.ground
  local A, L, R = { 15, g - 26 }, { 3, g }, { 27, g }
  local D = { 14, -7 }
  local A2, R2, L2 = { A[1] + D[1], A[2] + D[2] }, { R[1] + D[1], R[2] + D[2] }, { L[1] + D[1], L[2] + D[2] }
  -- The shadow over its footprint and a little past it, behind and to the right where the light throws it.
  for y = 0, c.H - 1 do
    for x = -4, c.W + 3 do
      local ins = false
      local poly = { L[1] - 1, L[2] + 2, R[1] + 3, R[2] + 2, R2[1] + 4, R2[2] + 1, L2[1] + 2, L2[2] - 1 }
      local n, j = #poly // 2, #poly // 2
      for i = 1, n do
        local xi, yi, xj, yj = poly[2 * i - 1], poly[2 * i], poly[2 * j - 1], poly[2 * j]
        if ((yi > y + 0.5) ~= (yj > y + 0.5)) and (x + 0.5 < (xj - xi) * (y + 0.5 - yi) / (yj - yi) + xi) then ins = not ins end
        j = i
      end
      if ins and (x + y) % 2 == 0 then c:dot(x, y, K.OUTLINE) end
    end
  end
  -- The slope.
  c:poly({ A[1], A[2], A2[1], A2[2], R2[1] + 1, R2[2], R[1] + 1, R[2] }, function(x, y)
    local t = (y - (A[2] + (x - A[1]) * D[2] / D[1])) / (R[2] - A[2])
    if t > 0.8 and t < 0.91 then return K.RED[4] end
    if t >= 0.91 then return K.RED[3] end
    local along = (x - A[1]) - (y - A[2]) * (R[1] - A[1]) / (R[2] - A[2])
    if math.floor(along + 0.5) % 5 == 0 and t > 0.15 then return K.WOOD[5] end
    return t < 0.12 and K.SKIN[5] or K.WOOD[6]
  end)
  for y = 0, 3 do
    for x = 0, 4 do
      c:px(30 + x - y // 2, g - 15 + y, (x == 0 or x == 4 or y == 0 or y == 3) and K.RED[2] or K.RED[4])
    end
  end
  -- The front.
  c:poly({ A[1], A[2], L[1], L[2], R[1], R[2] }, function(x, y)
    local left = A[1] + (y - A[2]) * (L[1] - A[1]) / (L[2] - A[2])
    local right = A[1] + (y - A[2]) * (R[1] - A[1]) / (R[2] - A[2])
    if y > g - 3 then return K.RED[2] end
    if math.min(x - left, right - x) < 1.5 then return K.WOOD[4] end
    return K.WOOD[5]
  end)
  c:poly({ 15, g - 16, 10, g, 20, g }, K.STONE[1])
  c:poly({ 15, g - 16, 10, g, 8, g, 12, g - 11 }, function(x, y) return y % 3 == 0 and K.WOOD[5] or K.WOOD[6] end)
  c:px(9, g - 7, K.SKIN[4])
  c:px(10, g - 7, K.SKIN[4])
  -- Pegs along the slope's foot, where the canvas is pinned to the ground.
  for k = 0, 2 do
    c:px(R[1] + 2 + k * 6, R[2] - k * 3 + 1, K.WOOD[2])
  end
  c:line(A[1], A[2], A2[1], A2[2], K.SKIN[5])
  c:line(A[1], A[2] - 1, A[1], A[2] - 3, K.WOOD[2])
  c:line(A2[1], A2[2] - 1, A2[1], A2[2] - 3, K.WOOD[2])
  c:outline()
  c:line(A[1] - 1, A[2] - 1, 1, g - 3, K.SKIN[4], false)
  c:line(A2[1] + 1, A2[2] - 1, 44, g - 9, K.SKIN[4], false)
  c:px(1, g - 2, K.WOOD[1], false)
  c:px(44, g - 8, K.WOOD[1], false)
  return c
end

-- A wooden crate, planks and a dark iron band, its top lit.
props.prop_crate = function()
  -- Redrawn after the barrel (2026-11-04, the owner: "amazing, now to the same for the crate"): flat tones per face, lit
  -- from the left (the left face in shade, the right half lit, the top brightest); each side a frame of lighter planks
  -- round its edge, the boards inside running across with a dark seam between each, a
  -- nail at each frame corner; the top's boards running back with seams, its near edges catching the light.
  local c = kit.new(24, 26)
  local g = c.ground
  local x0, y0, d, h = 12, g + 2, 5, 12
  c:shadow(x0 + 1, g, 11, 4)
  local side = {
    left = { infill = 3, frame = 4, seam = 2 },
    right = { infill = 5, frame = 6, seam = 4 },
  }
  c:box(x0, y0, d, d, h, function(face, x, y)
    if face == "top" then
      -- How far in from the near edges, along each: 0 on the edge itself.
      local inLeft = (y0 - h) - y - (x0 - 1 - x) / 2
      local inRight = (y0 - h) - y - (x - x0) / 2
      if inLeft < 1 or inRight < 1 then return K.WOOD[6] end
      -- Seams running back to the right at the camera's 2:1, two pixels across for each one up.
      if (x + 2 * y) % 8 < 2 then return K.WOOD[4] end
      return K.WOOD[5]
    end
    local tone = side[face]
    local along = face == "left" and (x0 - 1 - x) or (x - x0)
    local up = (y0 - along / 2) - y
    local width = 2 * d
    local onFrame = along < 2 or along >= width - 2 or up < 2.5 or up >= h - 1
    local corner = (along == 0 or along == width - 1) and (math.floor(up) == 1 or math.floor(up) == h - 1)
    if corner then return K.STONE[5] end
    if onFrame then
      if along == 0 and face == "right" then return K.WOOD[6] end
      return K.WOOD[tone.frame]
    end
    if math.floor(up) % 3 == 1 then return K.WOOD[tone.seam] end
    return K.WOOD[tone.infill]
  end)
  c:outline()
  return c
end

-- A barrel: staves bulging at the middle, two iron hoops, a dark lid.
props.prop_barrel = function()
  -- Redrawn three times on 2026-11-04 (the owner: "remake the barrel, it looks bad", "the top rim does not show", "it
  -- still looks a bit bad"): flat bands of light across its staves (lit from the left), a seam where one stave turns
  -- into the next, every row following the camera's curve round it (its foot and hoops bow down in front), a belly of
  -- one pixel each side in the middle, two thin dark iron hoops with one lit row each, a dark row at its foot, and on top
  -- a lid as wide as the barrel: its rim a lit ring, the far rim's shadow on the lid's back, planks across it.
  local c = kit.new(18, 28)
  local g = c.ground
  local cx, R, ry, H = 9, 8, 3, 14
  local T = g - H
  c:shadow(cx + 1, g + 1, 10, 4)
  local function u(x) return (x + 0.5 - cx) / R end
  local function curve(x) local q = u(x); return math.floor(ry * math.sqrt(math.max(0, 1 - q * q)) + 0.5) end
  local function band(q)
    if q < -0.8 then return 4 elseif q < -0.45 then return 6 elseif q < -0.1 then return 5
    elseif q < 0.3 then return 4 elseif q < 0.7 then return 3 else return 2 end
  end
  local function stave(x) return math.floor(math.asin(math.max(-1, math.min(1, u(x)))) / (math.pi / 6) + 0.5) end
  for x = cx - R, cx + R - 1 do
    local q = u(x)
    local seam = stave(x) ~= stave(x + 1) and x < cx + R - 2
    for k = 0, H do
      local y = T + curve(x) + k
      local colour
      if k == 2 or k == H - 3 then colour = q < 0.3 and K.STONE[5] or K.STONE[4]
      elseif k == 3 or k == H - 2 then colour = q < 0.3 and K.STONE[3] or K.STONE[2]
      elseif k == H then colour = K.WOOD[2]
      else
        local tone = band(q)
        if seam then tone = math.max(1, tone - 2) end
        colour = K.WOOD[tone]
      end
      c:px(x, y, colour)
    end
  end
  -- The belly: one more column each side through the middle rows.
  for k = 5, H - 5 do
    c:px(cx - R - 1, T + k, K.WOOD[4])
    c:px(cx + R, T + k, K.WOOD[2])
  end
  c:ellipse(cx, T, R, ry + 0.5, function(x, y, uu, v)
    if uu * uu + v * v > 0.5 then return v < 0 and K.WOOD[5] or K.WOOD[6] end
    if v < -0.05 then return K.WOOD[2] end
    if (x - cx) % 3 == 1 then return K.WOOD[3] end
    return uu < 0.2 and K.WOOD[5] or K.WOOD[4]
  end)
  c:outline()
  return c
end

-- A burlap sack, tied at the neck, slumped.
props.prop_sack = function()
  -- Redrawn after the barrel and the crate (2026-11-04, the owner: "well done, now do the bag"): a burlap sack slumped
  -- on the ground, its belly low and wide, drawn row by row from a profile in flat bands of light from the left (lighter
  -- toward its shoulders), its foot bowing down in front with the camera's curve, two folds running down from the neck,
  -- a rope tied round the neck with its end hanging, and above it the gathered mouth in pleats with ragged tips.
  local c = kit.new(20, 24)
  local g = c.ground
  local cx = 10
  local profile = { 6.5, 7.5, 8, 8.2, 8, 7.6, 7, 6.3, 5.5, 4.7, 4, 3.6, 3.4 }
  c:shadow(cx + 1, g + 1, 9, 3)
  local function tone(u, k)
    local q = u - 0.3 * (k - 4) / 8
    if q < -0.6 then return 5 elseif q < -0.15 then return 4 elseif q < 0.35 then return 3 elseif q < 0.75 then return 2 end
    return 1
  end
  for k = 0, #profile - 3 do
    local hw = profile[k + 1]
    for x = math.floor(cx - hw), math.ceil(cx + hw) - 1 do
      local u = (x + 0.5 - cx) / hw
      local t = tone(u, k)
      -- The folds: two creases from the neck, curving out as the sack widens.
      if k >= 4 and k <= 9 then
        local spread = (10 - k) * 0.06
        if math.abs(u - (-0.3 - spread)) < 0.09 or math.abs(u - (0.28 + spread)) < 0.09 then t = math.max(1, t - 1) end
      end
      if k == 0 then t = math.max(1, t - 1) end
      c:px(x, g - k, K.BURLAP[t])
      -- The foot bows down in front.
      if k == 0 then
        local drop = math.floor(1.6 * math.sqrt(math.max(0, 1 - u * u)) + 0.5)
        for dy = 1, drop do c:px(x, g + dy, K.BURLAP[math.max(1, t - 1)]) end
      end
    end
  end
  -- The rope round the neck: a dark row under a lit one, standing a pixel proud each side, the knot on the right and
  -- its end hanging down over the sack.
  local neck = #profile - 2
  for x = cx - 4, cx + 3 do
    c:px(x, g - neck, K.WOOD[2])
    c:px(x, g - neck - 1, x < cx and K.WOOD[5] or x < cx + 2 and K.WOOD[4] or K.WOOD[3])
  end
  c:px(cx + 4, g - neck, K.WOOD[3])
  c:px(cx + 4, g - neck - 1, K.WOOD[4])
  c:px(cx + 4, g - neck + 1, K.WOOD[3])
  c:px(cx + 4, g - neck + 2, K.WOOD[2])
  c:px(cx + 5, g - neck + 3, K.WOOD[2])
  -- The gathered mouth: a tight pleated row over the rope, a wider one, and ragged tips of cloth of uneven heights.
  local function pleat(x, y, dark)
    local dx = x - cx
    local t = dx < -1 and 5 or dx < 2 and 4 or 3
    if dx % 2 ~= 0 then t = t - 1 end
    c:px(x, y, K.BURLAP[math.max(1, t - (dark and 1 or 0))])
  end
  local y = g - neck - 2
  for x = cx - 3, cx + 2 do pleat(x, y, true) end
  for x = cx - 5, cx + 4 do pleat(x, y - 1, false) end
  for _, tip in ipairs({ { -5, 1 }, { -3, 2 }, { -2, 1 }, { 0, 1 }, { 1, 2 }, { 3, 1 } }) do
    for k = 1, tip[2] do pleat(cx + tip[1], y - 1 - k, false) end
  end
  c:outline()
  return c
end

-- A weapon rack: two posts and a crossbar, spears and a sword leaning on it.
props.prop_weapon_rack = function()
  -- Redrawn after the barrel (2026-11-04, the owner: "well done, now the weapon rack"): a rack running back to the
  -- right at the camera's 2:1, two posts and a top rail, each two pixels thick (lit on its left or top, dark on the
  -- other), and leaning on the rail four weapons with their own shapes: a spear with a pointed head and a red tassel,
  -- an axe with a bearded head, a sword point down (a two-tone blade, an iron guard, a grip, a pommel) and a second
  -- spear; a round red shield with an iron rim and boss against the near post.
  local c = kit.new(34, 40)
  local g = c.ground
  local x0, y0, run, tall = 6, g + 1, 22, 15
  local function rail(x) return math.floor(y0 - tall - (x - x0) / 2 + 0.5) end
  c:shadow(17, g - 4, 15, 5)
  local function post(x, foot)
    for y = foot - tall - 1, foot do
      c:px(x, y, K.WOOD[5])
      c:px(x + 1, y, K.WOOD[3])
    end
    c:px(x, foot - tall - 2, K.WOOD[6])
    c:px(x + 1, foot - tall - 2, K.WOOD[4])
  end
  post(x0 + run, y0 - run // 2)
  for x = x0, x0 + run + 1 do
    c:px(x, rail(x) - 1, K.WOOD[5])
    c:px(x, rail(x), K.WOOD[3])
  end
  post(x0, y0)
  local function shaft(xa, ya, xb, yb) c:line(xa, ya, xb, yb, K.WOOD[4]) end
  -- A spear's head, its tip at (x, y): widening to three pixels, narrowing to its socket, lit on the left; a red tassel.
  local function spearhead(x, y)
    c:px(x, y, K.STONE[7])
    for k = 1, 4 do
      c:px(x - 1, y + k, K.STONE[6]); c:px(x, y + k, K.STONE[5]); c:px(x + 1, y + k, K.STONE[3])
    end
    c:px(x, y + 5, K.STONE[4])
    c:px(x - 1, y + 6, K.RED[5]); c:px(x, y + 6, K.RED[4]); c:px(x + 1, y + 6, K.RED[3])
    c:px(x, y + 7, K.RED[3])
  end
  local tx = 10
  local top = rail(tx) - 9
  shaft(8, g + 4, tx, top + 6)
  spearhead(tx, top)
  -- The axe, its bearded head out to the right of the shaft.
  tx = 14
  top = rail(tx) - 5
  shaft(15, g + 3, tx, top)
  c:poly({ tx + 1, top, tx + 5, top - 2, tx + 6, top + 3, tx + 4, top + 7, tx + 1, top + 4 }, function(px)
    if px >= tx + 5 then return K.STONE[6] end
    return px <= tx + 2 and K.STONE[3] or K.STONE[4]
  end)
  -- The sword, point down on the ground, its hilt over the rail.
  tx = 23
  top = rail(tx) - 5
  for y = top + 3, g do
    c:px(tx, y, K.STONE[6])
    c:px(tx + 1, y, K.STONE[4])
  end
  for x = tx - 2, tx + 3 do c:px(x, top + 2, x < tx + 1 and K.STONE[4] or K.STONE[3]) end
  c:px(tx, top + 1, K.WOOD[2]); c:px(tx + 1, top + 1, K.WOOD[2])
  c:px(tx, top, K.WOOD[3]); c:px(tx + 1, top, K.WOOD[2])
  c:px(tx, top - 1, K.STONE[6]); c:px(tx + 1, top - 1, K.STONE[4])
  -- The second spear, leaning the other way.
  tx = 27
  top = rail(tx) - 9
  shaft(30, g - 1, tx, top + 6)
  spearhead(tx, top)
  -- The shield against the near post: iron rim, red face lit on its left, a boss.
  c:ellipse(6, g - 4, 4.5, 5, function(x, y, u, v)
    local d = u * u + v * v
    if d > 0.62 then return u < 0 and K.STONE[5] or K.STONE[3] end
    if d < 0.08 then return K.STONE[6] end
    if d < 0.16 then return K.STONE[3] end
    return (u + v * 0.5) < -0.2 and K.RED[5] or (u + v * 0.5) < 0.35 and K.RED[4] or K.RED[3]
  end)
  c:outline()
  return c
end

-- A wooden cage on the ground for prisoners: a frame of poles, bars, a lashed roof.
props.prop_cage = function()
  local c = kit.new(32, 30)
  local g = c.ground
  c:shadow(16, g - 3, 15, 6)
  local a, b, h = 6, 6, 13
  local x0, y0 = 16, g + 1
  local L, R, B = { x0 - 2 * a, y0 - a }, { x0 + 2 * b, y0 - b }, { x0 - 2 * a + 2 * b, y0 - a - b }
  -- The back bars first, dark, then the floor, then the front bars over them.
  for k = 0, 4 do
    local t = k / 4
    c:line(L[1] + (B[1] - L[1]) * t, L[2] + (B[2] - L[2]) * t, L[1] + (B[1] - L[1]) * t, L[2] + (B[2] - L[2]) * t - h, K.WOOD[2])
    c:line(B[1] + (R[1] - B[1]) * t, B[2] + (R[2] - B[2]) * t, B[1] + (R[1] - B[1]) * t, B[2] + (R[2] - B[2]) * t - h, K.WOOD[2])
  end
  c:poly({ x0, y0, L[1], L[2], B[1], B[2], R[1], R[2] }, function(x, y) return (x + y) % 3 == 0 and K.EARTH[4] or K.EARTH[5] end)
  for k = 0, 4 do
    local t = k / 4
    c:line(x0 + (L[1] - x0) * t, y0 + (L[2] - y0) * t, x0 + (L[1] - x0) * t, y0 + (L[2] - y0) * t - h, K.WOOD[4])
    c:line(x0 + (R[1] - x0) * t, y0 + (R[2] - y0) * t, x0 + (R[1] - x0) * t, y0 + (R[2] - y0) * t - h, K.WOOD[5])
  end
  -- The roof and the cross bars, lashed with rope.
  c:poly({ x0, y0 - h, L[1], L[2] - h, B[1], B[2] - h, R[1], R[2] - h }, function(x, y) return (x - y) % 4 == 0 and K.WOOD[4] or K.WOOD[6] end)
  c:line(x0, y0 - 6, L[1], L[2] - 6, K.WOOD[3])
  c:line(x0, y0 - 6, R[1], R[2] - 6, K.WOOD[4])
  c:px(x0, y0 - 6, K.SKIN[4])
  c:px(x0, y0 - h, K.SKIN[4])
  c:outline()
  return c
end

-- A campfire: a ring of stones, crossed logs, flames from red at the foot to pale yellow at the tips.
props.prop_campfire = function()
  local c = kit.new(26, 28)
  local g = c.ground
  c:shadow(13, g - 1, 12, 4)
  c:ellipse(13, g - 2, 9, 4, K.EARTH[1])
  local rnd = kit.random(41)
  for k = 0, 9 do
    local a = k / 10 * math.pi * 2
    c:ellipse(13 + math.cos(a) * 9, g - 2 + math.sin(a) * 4, 1.6, 1.2, function(x, y, u, v) return v < 0 and K.STONE[5] or K.STONE[3] end)
  end
  c:stroke(7, g - 1, 19, g - 4, 1, K.WOOD[3])
  c:stroke(8, g - 4, 18, g - 1, 1, K.WOOD[4])
  -- Flames: tongues of fire rising, hottest in the middle.
  for k = 0, 4 do
    local fx = 9 + k * 2 + rnd(0, 1)
    local top = g - 9 - rnd(2, 8) + math.abs(k - 2) * 2
    for y = top, g - 3 do
      local t = (y - top) / math.max(1, g - 3 - top)
      local half = 0.5 + t * 1.5
      for x = math.floor(fx - half), math.ceil(fx + half) - 1 do
        local hot = 1 - math.abs(x + 0.5 - fx) / (half + 0.5)
        local colour = t < 0.25 and K.EMBER[5] or (hot > 0.55 and t < 0.7) and K.EMBER[4] or t > 0.75 and K.RED[4] or K.EMBER[3]
        c:px(x, y, colour)
      end
    end
  end
  for k = 1, 4 do
    c:px(10 + rnd(0, 6), g - 14 - rnd(0, 6), K.EMBER[4], false)
  end
  c:outline()
  return c
end

-- A pile of bones: a skull on top, long bones and ribs under it.
props.prop_bone_pile = function()
  local c = kit.new(24, 18)
  local g = c.ground
  c:shadow(12, g, 11, 3)
  local function bone(x0, y0, x1, y1)
    c:line(x0, y0, x1, y1, K.BONE[2])
    c:ellipse(x0, y0, 1.3, 1, K.BONE[3])
    c:ellipse(x1, y1, 1.3, 1, K.BONE[2])
  end
  bone(3, g - 1, 15, g - 4)
  bone(8, g, 20, g - 2)
  bone(6, g - 4, 13, g)
  for k = 0, 3 do
    c:line(14 + k * 2, g - 6, 15 + k * 2, g - 2, K.BONE[1])
  end
  c:ellipse(10, g - 6, 3.5, 3, function(x, y, u, v) return (u < -0.1 and v < 0.2) and K.BONE[3] or K.BONE[2] end)
  c:px(9, g - 6, K.STONE[1])
  c:px(11, g - 6, K.STONE[1])
  c:line(9, g - 4, 11, g - 4, K.BONE[1])
  c:outline()
  return c
end

-- Ferns: fronds arching out from a clump, each a stem with leaflets, lit toward their tips.
props.prop_ferns = function()
  local c = kit.new(28, 22)
  local g = c.ground
  c:shadow(14, g, 12, 3)
  local fronds = { { -11, -7 }, { -7, -12 }, { -2, -14 }, { 4, -13 }, { 9, -10 }, { 12, -5 }, { -4, -8 }, { 6, -7 } }
  for k, f in ipairs(fronds) do
    local x0, y0, x1, y1 = 14, g - 1, 14 + f[1], g - 1 + f[2]
    local n = 7
    for i = 0, n do
      local t = i / n
      -- Arching: up first, then falling toward the tip.
      local x = x0 + (x1 - x0) * t
      local y = y0 + (y1 - y0) * (t * (2 - t)) + t * t * 3
      local colour = t < 0.35 and K.MOSS[3] or t < 0.75 and K.MOSS[4] or K.MOSS[5]
      c:px(x, y, colour)
      if i > 1 and i < n then
        c:px(x, y - 1, colour)
        c:px(x + (f[1] > 0 and 1 or -1), y + 1, K.MOSS[3])
      end
    end
  end
  c:outline()
  return c
end

-- Mushrooms: a cluster of red caps with pale spots on pale stems, one fallen over.
props.prop_mushrooms = function()
  local c = kit.new(22, 20)
  local g = c.ground
  c:shadow(11, g, 10, 3)
  local function cap(x, base, h, r)
    c:poly({ x - 1, base, x + 1, base, x + 1, base - h, x - 1, base - h }, K.BONE[2])
    c:ellipse(x, base - h, r, r * 0.65, function(px, py, u, v)
      if v > 0.35 then return K.RED[2] end
      if (px * 5 + py * 3) % 7 == 0 then return K.BONE[3] end
      return u < -0.2 and K.RED[5] or K.RED[4]
    end)
  end
  cap(15, g, 5, 3.5)
  cap(7, g, 8, 4.5)
  cap(11, g + 1, 3, 2.5)
  c:outline()
  return c
end

-- A tree stump: sawn flat, its rings showing, bark around it, roots reaching into the ground, a little moss.
props.prop_stump = function()
  local c = kit.new(26, 22)
  local g = c.ground
  c:shadow(13, g, 12, 4)
  -- Roots.
  for _, r in ipairs({ { 4, g + 1 }, { 22, g }, { 9, g + 2 }, { 18, g + 2 } }) do
    c:stroke(13, g - 3, r[1], r[2], 0.9, K.WOOD[3])
  end
  -- The trunk: bark in vertical runs, darker on the right.
  for y = g - 10, g - 1 do
    for x = 6, 19 do
      local colour = x < 9 and K.WOOD[4] or x > 16 and K.WOOD[2] or K.WOOD[3]
      if (x * 7 + y) % 5 == 0 then colour = K.WOOD[2] end
      if y > g - 4 and x < 9 and (x + y) % 2 == 0 then colour = K.MOSS[3] end
      c:px(x, y, colour)
    end
  end
  c:ellipse(12.5, g - 10, 7, 3.2, function(x, y, u, v)
    local r = math.sqrt(u * u + v * v)
    if r > 0.85 then return K.WOOD[3] end
    if math.floor(r * 6) % 2 == 0 then return K.WOOD[6] end
    return K.WOOD[5]
  end)
  c:outline()
  return c
end

-- The woodcutters' stump with an axe left in it.
props.prop_axe_stump = function()
  local c = props.prop_stump()
  local g = c.ground
  -- The handle angled back from the stump's top, the head buried in the wood.
  c:stroke(11, g - 11, 4, g - 21, 0.6, K.WOOD[5])
  c:poly({ 10, g - 13, 15, g - 11, 15, g - 9, 11, g - 9 }, K.STONE[4])
  c:line(14, g - 11, 15, g - 9, K.STONE[6])
  c:outline()
  return c
end

-- A fallen log lying back into the scene at the camera's 2:1, its near end sawn, moss along its top, a branch stub.
props.prop_fallen_log = function()
  local c = kit.new(40, 24)
  local g = c.ground
  c:shadow(20, g - 3, 18, 5)
  local x0, y0, x1, y1, r = 8, g - 3, 32, g - 15, 4.5
  c:stroke(x0, y0, x1, y1, r, function(x, y)
    -- Distance across the log: lit on its top, dark underneath.
    local t = ((x - x0) * (x1 - x0) + (y - y0) * (y1 - y0)) / ((x1 - x0) ^ 2 + (y1 - y0) ^ 2)
    local cx, cy = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
    local across = (y - cy)
    if across < -2.5 then return (x % 3 == 0) and K.MOSS[4] or K.MOSS[3] end
    if across < 0 then return K.WOOD[4] end
    if across > 2.5 then return K.WOOD[2] end
    return (x + y) % 4 == 0 and K.WOOD[2] or K.WOOD[3]
  end)
  c:stroke(22, g - 12, 25, g - 18, 0.8, K.WOOD[3])
  c:ellipse(x0 - 1, y0, 3.2, 4.4, function(x, y, u, v)
    local rr = math.sqrt(u * u + v * v)
    if rr > 0.8 then return K.WOOD[3] end
    return math.floor(rr * 5) % 2 == 0 and K.WOOD[6] or K.WOOD[5]
  end)
  c:outline()
  return c
end

-- A standing stone: a tall weathered slab, lit on its left face, lichen low on it, a grass tuft at its foot.
props.prop_standing_stone = function()
  local c = kit.new(22, 40)
  local g = c.ground
  c:shadow(11, g, 9, 3)
  -- The front face (shade by height and a few cracks), then the lit side.
  c:poly({ 5, g, 15, g, 14, g - 26, 11, g - 30, 7, g - 27 }, function(x, y)
    if (x * 3 + y * 7) % 23 == 0 then return K.STONE[2] end
    if y > g - 7 and (x + y) % 3 == 0 then return K.MOSS[3] end
    return x < 9 and K.STONE[5] or K.STONE[4]
  end)
  c:poly({ 15, g, 18, g - 2, 17, g - 25, 14, g - 26 }, function(x, y)
    return y > g - 6 and K.MOSS[2] or K.STONE[3]
  end)
  c:line(7, g - 27, 11, g - 30, K.STONE[6])
  c:line(9, g - 18, 11, g - 14, K.STONE[2])
  for _, t in ipairs({ { 4, g }, { 6, g + 1 }, { 16, g + 1 } }) do
    c:px(t[1], t[2] - 1, K.MOSS[4]); c:px(t[1] + 1, t[2] - 2, K.MOSS[5]); c:px(t[1] - 1, t[2] - 2, K.MOSS[3])
  end
  c:outline()
  return c
end

-- A spider's web sac: a pale cocoon hung from threads, a dark shape inside it, strands to the ground.
props.prop_web_sac = function()
  -- A clutch of three spider eggs (2026-11-04, the owner: "now do the spider egg, it looks really bad, try make a
  -- clyster of three eggs"): a big egg at the back, two smaller in front, each egg-shaped (narrow at its top, full at
  -- its foot), cream in flat bands of light from the upper left with a glossy highlight, a dark edge of its own parting
  -- it from the one behind, a silk thread wound round it; strands of web from them out over the ground.
  local c = kit.new(28, 28)
  local g = c.ground
  c:shadow(14, g, 12, 3)
  -- Half the width at height v (-1 at its top, 1 at its foot): an egg, fuller below its middle.
  local function half(v) return math.sqrt(math.max(0, 1 - v * v)) * (0.88 + 0.12 * v) end
  local function egg(cx, foot, rx, ry, edged)
    local cy = foot - ry
    local function inside(x, y, grow)
      local v = (y + 0.5 - cy) / (ry + grow)
      if math.abs(v) > 1 then return nil end
      local u = (x + 0.5 - cx) / ((rx + grow) * 1.12)
      if math.abs(u) > half(v) then return nil end
      return u, v
    end
    for y = math.floor(cy - ry - 2), math.ceil(cy + ry + 2) do
      for x = math.floor(cx - rx - 2), math.ceil(cx + rx + 2) do
        local u, v = inside(x, y, 0)
        if u then
          local q = u * 0.85 + v * 0.5
          local t = q < -0.6 and 5 or q < -0.15 and 4 or q < 0.35 and 3 or q < 0.75 and 2 or 1
          -- The gloss: a short streak up on its lit side.
          if math.abs(u + 0.38) < 0.13 and v > -0.62 and v < -0.2 then t = 5 end
          c:px(x, y, t == 5 and math.abs(u + 0.38) < 0.13 and v > -0.62 and v < -0.2 and K.BONE[3] or K.CREAM[t])
          -- A silk thread wound round it, a little lighter than the egg under it (not over the gloss).
          if t < 4 and math.abs(v - (0.42 * u + 0.3)) < 0.07 then c:px(x, y, K.CREAM[t + 1]) end
        elseif edged and c:get(x, y) and inside(x, y, 1) then
          c:px(x, y, K.OUTLINE)
        end
      end
    end
  end
  egg(14, g - 2, 5, 7.5, false)
  egg(8, g + 1, 4, 5, true)
  egg(20, g + 1, 3.6, 4.6, true)
  c:outline()
  -- Web from the eggs out over the ground, drawn over everything with no outline.
  c:line(4, g - 2, 1, g + 2, K.SILK[3], false)
  c:line(23, g - 1, 27, g + 1, K.SILK[3], false)
  c:line(9, g + 2, 6, g + 4, K.SILK[3], false)
  c:line(20, g + 2, 23, g + 4, K.SILK[3], false)
  return c
end

-- A carcass: a deer on its side, ribs bare where it was fed on, blood under it.
props.prop_carcass = function()
  local c = kit.new(34, 22)
  local g = c.ground
  c:ellipse(17, g, 15, 4, function(x, y) return (x + y) % 2 == 0 and K.RED[2] or K.RED[1] end, false)
  -- The body: a hide ellipse lit on top.
  c:ellipse(16, g - 5, 10, 5, function(x, y, u, v)
    if v < -0.4 then return K.SKIN[4] end
    if v > 0.5 then return K.SKIN[1] end
    return K.SKIN[3]
  end)
  -- The opened ribs on its flank.
  for i = 0, 3 do
    c:line(12 + i * 3, g - 8, 13 + i * 3, g - 2, K.BONE[3])
    c:line(13 + i * 3, g - 7, 14 + i * 3, g - 3, K.RED[3])
  end
  -- The head and neck to the left, an ear and an antler; legs out to the right.
  c:ellipse(5, g - 5, 3.5, 2.5, function(x, y, u, v) return v < 0 and K.SKIN[4] or K.SKIN[2] end)
  c:line(4, g - 8, 2, g - 12, K.BONE[2]); c:line(3, g - 10, 5, g - 13, K.BONE[2])
  c:stroke(25, g - 3, 31, g - 1, 0.6, K.SKIN[2])
  c:stroke(24, g - 6, 31, g - 7, 0.6, K.SKIN[3])
  c:px(31, g - 1, K.OUTLINE); c:px(31, g - 7, K.OUTLINE)
  c:outline()
  return c
end

for name, draw in pairs(props) do
  if not only or string.find("," .. only .. ",", "," .. name .. ",", 1, true) then
    draw():save(out, name)
  end
end
