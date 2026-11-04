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
  local c = kit.new(24, 26)
  local g = c.ground
  c:shadow(12, g - 1, 11, 4)
  c:box(12, g + 1, 5, 5, 11, function(face, x, y)
    if face == "top" then return (x + y * 2) % 7 == 0 and K.WOOD[4] or K.WOOD[6] end
    local base = face == "left" and K.WOOD[3] or K.WOOD[5]
    -- Plank seams, the band across the middle.
    local fromTop = face == "left" and (y - (g + 1 - (12 - x) / 2 - 11)) or (y - (g + 1 - (x - 12) / 2 - 11))
    if math.floor(fromTop) == 5 then return K.STONE[2] end
    if face == "left" and (12 - x) % 4 == 0 then return K.WOOD[2] end
    if face == "right" and (x - 12) % 4 == 0 then return K.WOOD[3] end
    return base
  end)
  c:outline()
  return c
end

-- A barrel: staves bulging at the middle, two iron hoops, a dark lid.
props.prop_barrel = function()
  local c = kit.new(18, 26)
  local g = c.ground
  c:shadow(9, g, 8, 3)
  local top, bottom = g - 15, g
  for y = top, bottom do
    local t = (y - top) / (bottom - top)
    local half = 5.5 + 1.5 * math.sin(t * math.pi)
    for x = math.floor(9 - half), math.ceil(9 + half) - 1 do
      local u = (x + 0.5 - 9) / half
      local colour = u < -0.45 and K.WOOD[3] or u < 0.2 and K.WOOD[4] or u < 0.6 and K.WOOD[5] or K.WOOD[4]
      if (x - 9) % 3 == 0 then colour = K.WOOD[2] end
      if math.abs(t - 0.22) < 0.06 or math.abs(t - 0.8) < 0.06 then colour = u < 0 and K.STONE[3] or K.STONE[4] end
      c:px(x, y, colour)
    end
  end
  c:ellipse(9, top, 5.5, 2.2, function(x, y, u, v) return (u * u + v * v > 0.5) and K.WOOD[4] or K.WOOD[2] end)
  c:outline()
  return c
end

-- A burlap sack, tied at the neck, slumped.
props.prop_sack = function()
  local c = kit.new(18, 20)
  local g = c.ground
  c:shadow(9, g, 8, 3)
  c:ellipse(9, g - 4, 7, 5.5, function(x, y, u, v)
    if u + v * 0.6 < -0.5 then return K.SKIN[4] end
    if u - v > 0.9 then return K.SKIN[2] end
    if (x * 3 + y) % 8 == 0 then return K.SKIN[2] end
    return K.SKIN[3]
  end)
  c:poly({ 7, g - 9, 11, g - 9, 12, g - 13, 6, g - 13 }, K.SKIN[3])
  c:line(6, g - 10, 12, g - 10, K.WOOD[2])
  c:px(5, g - 13, K.SKIN[4])
  c:px(13, g - 13, K.SKIN[2])
  c:outline()
  return c
end

-- A weapon rack: two posts and a crossbar, spears and a sword leaning on it.
props.prop_weapon_rack = function()
  local c = kit.new(30, 36)
  local g = c.ground
  c:shadow(15, g, 13, 3)
  -- The posts and the bars.
  c:stroke(5, g, 5, g - 18, 0.6, K.WOOD[3])
  c:stroke(25, g - 3, 25, g - 21, 0.6, K.WOOD[4])
  c:stroke(5, g - 16, 25, g - 19, 0.7, K.WOOD[5])
  c:stroke(5, g - 5, 25, g - 8, 0.5, K.WOOD[3])
  -- Spears leaning back on the bar, steel heads up; a sword hanging between.
  for k, x in ipairs({ 8, 13, 21 }) do
    local lean = (k % 2 == 0) and 1 or -1
    local tx = x + lean * 2
    c:line(x, g - 1, tx, g - 25, K.WOOD[4])
    -- A leaf-shaped steel head, lit on its left.
    c:poly({ tx, g - 32, tx + 2, g - 28, tx, g - 25, tx - 2, g - 28 }, function(px) return px <= tx and K.STONE[7] or K.STONE[5] end)
  end
  c:line(17, g - 19, 17, g - 5, K.STONE[6])
  c:line(18, g - 19, 18, g - 5, K.STONE[4])
  c:line(15, g - 19, 20, g - 19, K.EMBER[2])
  c:px(17, g - 21, K.WOOD[2])
  c:px(17, g - 20, K.WOOD[2])
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
  local c = kit.new(24, 34)
  local g = c.ground
  c:shadow(12, g, 8, 2)
  c:ellipse(12, g - 12, 5, 9, function(x, y, u, v)
    if math.abs(u) < 0.35 and v > -0.5 and v < 0.4 then return K.BONE[1] end
    if (y + math.floor(x / 2)) % 3 == 0 then return K.BONE[1] end
    return u < 0 and K.BONE[3] or K.BONE[2]
  end)
  c:outline()
  -- Threads up out of the picture and down to the ground, drawn without outline.
  local W = K.BONE[2]
  c:line(12, g - 21, 12, 0, W, false)
  c:line(10, g - 20, 5, 0, W, false)
  c:line(14, g - 20, 20, 0, W, false)
  c:line(8, g - 6, 2, g, W, false)
  c:line(16, g - 6, 22, g + 1, W, false)
  c:line(12, g - 3, 13, g + 1, W, false)
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
