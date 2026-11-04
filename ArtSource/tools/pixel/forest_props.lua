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
  -- The bandits' ridge tent at the camera's 2:1 (its slope going back 14 across for 7 up, so its foot lies on the ground;
  -- 2026-11-04, the owner: "the tents right side looks to be floating in the air"), redrawn in the second pass in flat
  -- bands: the lit slope's canvas in three panels parted by seams running down it, lightest along the ridge, a red
  -- stripe along its foot; the front in shade with a darker hem and the red stripe; the door dark, its left flap tied
  -- back; the ridge pole's ends; guy ropes out to pegs; a shadow over its whole footprint.
  local c = kit.new(46, 42)
  local g = c.ground
  local A, L, R = { 15, g - 26 }, { 3, g }, { 27, g }
  local D = { 14, -7 }
  local A2, R2, L2 = { A[1] + D[1], A[2] + D[2] }, { R[1] + D[1], R[2] + D[2] }, { L[1] + D[1], L[2] + D[2] }
  local shade = { L[1] - 1, L[2] + 2, R[1] + 3, R[2] + 2, R2[1] + 4, R2[2] + 1, L2[1] + 2, L2[2] - 1 }
  c:poly(shade, function(x, y) return (x + y) % 2 == 0 and K.OUTLINE or nil end, false)
  -- The slope, in panels: where a pixel is along the ridge (s, front to back) and down from it (t, ridge to foot).
  local E = { R[1] - A[1], R[2] - A[2] }
  local det = D[1] * E[2] - D[2] * E[1]
  c:poly({ A[1], A[2], A2[1], A2[2], R2[1] + 1, R2[2], R[1] + 1, R[2] }, function(x, y)
    local px, py = x + 0.5 - A[1], y + 0.5 - A[2]
    local sAlong = (px * E[2] - py * E[1]) / det
    local t = (D[1] * py - D[2] * px) / det
    if t > 0.9 then return K.RED[3] end
    if t > 0.78 then return K.RED[4] end
    if math.abs(sAlong - 0.36) < 0.035 or math.abs(sAlong - 0.68) < 0.035 then return K.WOOD[4] end
    if t < 0.1 then return K.SKIN[5] end
    return t < 0.45 and K.SKIN[4] or K.WOOD[6]
  end)
  -- The front, in shade: a darker hem down its sides, the red stripe along its foot.
  c:poly({ A[1], A[2], L[1], L[2], R[1], R[2] }, function(x, y)
    local left = A[1] + (y - A[2]) * (L[1] - A[1]) / (L[2] - A[2])
    local right = A[1] + (y - A[2]) * (R[1] - A[1]) / (R[2] - A[2])
    if y > g - 3 then return K.RED[2] end
    if math.min(x - left, right - x) < 1.5 then return K.WOOD[3] end
    return K.WOOD[4]
  end)
  -- The door, and its left flap tied back with a cord.
  c:poly({ 15, g - 16, 10, g, 20, g }, K.STONE[1])
  c:poly({ 15, g - 16, 10, g, 7, g, 12, g - 10 }, function(x, y) return x < 10 and K.WOOD[6] or K.WOOD[5] end)
  c:px(9, g - 6, K.SKIN[5]); c:px(10, g - 6, K.SKIN[4]); c:px(11, g - 6, K.SKIN[4])
  -- Pegs along the slope's foot.
  for k = 0, 2 do c:px(R[1] + 2 + k * 6, R[2] - k * 3 + 1, K.WOOD[2]) end
  -- The ridge, and the pole's ends standing up out of it.
  c:line(A[1], A[2], A2[1], A2[2], K.SKIN[5])
  c:line(A[1], A[2] - 1, A[1], A[2] - 3, K.WOOD[2])
  c:line(A2[1], A2[2] - 1, A2[1], A2[2] - 3, K.WOOD[2])
  c:outline()
  -- Guy ropes from the poles out to their pegs, with no outline.
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
  -- Redrawn in the second pass (2026-11-04, the owner: "good, next prop"): the bandits' prisoner cage on a plank
  -- floor, a box at the camera's 2:1; its back bars dark, a skull and a bone on the floor between them, its front bars
  -- lit (the left face in shade, the right half lit), four posts two pixels thick lashed with rope at the top, a planked
  -- roof slab with its edges showing, and a padlock on the front.
  local c = kit.new(32, 34)
  local g = c.ground
  local a, b, h, base = 6, 6, 15, 2
  local x0, y0 = 16, g + 1
  local L, R, B = { x0 - 2 * a, y0 - a }, { x0 + 2 * b, y0 - b }, { x0 - 2 * a + 2 * b, y0 - a - b }
  c:shadow(x0, g - 4, 15, 6)
  -- The floor.
  c:box(x0, y0, a, b, base, function(face, x, y)
    -- In the roof's shade, so the bars stand out against it.
    if face == "top" then return (x + 2 * y) % 6 < 2 and K.WOOD[1] or K.WOOD[2] end
    return face == "left" and K.WOOD[2] or K.WOOD[3]
  end)
  -- Bars along a face from P to Q, every three pixels across, from the floor up to the roof.
  local function bars(P, Q, colour, skipEnds)
    local n = math.abs(Q[1] - P[1])
    for i = 0, n, 3 do
      if not (skipEnds and (i == 0 or i >= n - 1)) then
        local t = i / n
        local x = P[1] + (Q[1] - P[1]) * t
        local y = P[2] + (Q[2] - P[2]) * t
        -- No outline of their own, so the gaps between them stay open.
        c:line(x, y - base, x, y - h, colour, false)
      end
    end
  end
  bars(L, B, K.WOOD[2], true)
  bars(B, R, K.WOOD[2], true)
  -- The back post.
  c:line(B[1], B[2] - base, B[1], B[2] - h, K.WOOD[3], false)
  -- Inside, on the floor: a skull and a bone.
  local fx, fy = x0, y0 - base - a
  c:poly({ fx - 4, fy + 1, fx + 1, fy - 1, fx + 2, fy, fx - 3, fy + 2 }, K.BONE[2])
  c:ellipse(fx + 3, fy + 1, 2, 1.8, function(x, y, u, v) return u < 0 and K.BONE[3] or K.BONE[2] end)
  c:px(fx + 2, fy + 1, K.OUTLINE); c:px(fx + 4, fy + 1, K.OUTLINE)
  -- The front bars, then the posts over their ends.
  bars({ x0, y0 }, L, K.WOOD[4], true)
  bars({ x0, y0 }, R, K.WOOD[5], true)
  local function post(x, y)
    for yy = y - h, y do
      c:px(x, yy, K.WOOD[5])
      c:px(x + 1, yy, K.WOOD[3])
    end
    c:px(x, y - h + 1, K.SKIN[4]); c:px(x + 1, y - h + 1, K.SKIN[3])
    c:px(x, y - h + 2, K.SKIN[3]); c:px(x + 1, y - h + 2, K.SKIN[2])
  end
  post(L[1], L[2])
  post(R[1] - 1, R[2])
  post(x0 - 1, y0)
  -- The roof: a planked slab, its top boards running back, its edges two pixels deep.
  c:box(x0, y0 - h, a, b, 2, function(face, x, y)
    if face == "top" then return (x + 2 * y) % 6 < 2 and K.WOOD[4] or K.WOOD[6] end
    return face == "left" and K.WOOD[3] or K.WOOD[4]
  end)
  -- The padlock on the front's right face.
  local px, py = x0 + 5, y0 - 3 - 7
  c:px(px, py - 1, K.STONE[3]); c:px(px + 1, py - 2, K.STONE[3]); c:px(px + 2, py - 1, K.STONE[3])
  c:px(px, py, K.STONE[6]); c:px(px + 1, py, K.STONE[5]); c:px(px + 2, py, K.STONE[4])
  c:px(px, py + 1, K.STONE[5]); c:px(px + 1, py + 1, K.STONE[4]); c:px(px + 2, py + 1, K.STONE[3])
  c:outline()
  return c
end

-- A campfire: a ring of stones, crossed logs, flames from red at the foot to pale yellow at the tips.
props.prop_campfire = function()
  -- Redrawn in the second pass (2026-11-04, the owner: "do the campfire"): a ring of stones on the ground at the
  -- camera's 2:1 (those behind the fire drawn first, those in front last), a bed of ash with glowing embers, three logs
  -- crossed in it (lit along their tops, their cut ends showing), and flames rising from them, ragged (below), drawn without the black outline (fire
  -- glows; it has no line round it), and sparks above.
  local c = kit.new(28, 34)
  local g = c.ground
  local cx, cy, rx, ry = 14, g - 1, 9, 4.5
  c:shadow(cx, cy + 1, 12, 5)
  local stones = {}
  for k = 0, 11 do
    local a = k / 12 * math.pi * 2 + 0.2
    stones[#stones + 1] = { cx + math.cos(a) * rx, cy + math.sin(a) * ry }
  end
  local function stone(s)
    c:ellipse(s[1], s[2], 1.9, 1.3, function(x, y, u, v)
      if v < -0.2 then return u < 0.3 and K.STONE[6] or K.STONE[5] end
      return u < 0 and K.STONE[4] or K.STONE[3]
    end)
  end
  for _, s in ipairs(stones) do if s[2] < cy then stone(s) end end
  local function hash(ix, iy)
    local h = (ix * 374761393 + iy * 668265263 + 1013) % 2147483648
    h = (h * 1274126177) % 2147483648
    return (h % 1000) / 1000
  end
  local function noise(x, y)
    local ix, iy = math.floor(x), math.floor(y)
    local fx, fy = x - ix, y - iy
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    local top = hash(ix, iy) + (hash(ix + 1, iy) - hash(ix, iy)) * fx
    local bottom = hash(ix, iy + 1) + (hash(ix + 1, iy + 1) - hash(ix, iy + 1)) * fx
    return top + (bottom - top) * fy
  end
  -- The bed of coals: glowing hottest under the fire, cooling through red to grey ash at its rim, broken up by noise so
  -- it runs into the flames with no edge (2026-11-04, the owner: "the separation between the fire and coals is too
  -- sharp").
  c:ellipse(cx, cy, rx - 1.5, ry - 1, function(x, y, u, v)
    local glow = 1 - (u * u + v * v) + (noise(x * 0.7, y * 0.9 + 3) - 0.5) * 0.9 - 0.15
    if glow > 0.7 then return K.EMBER[4] elseif glow > 0.52 then return K.EMBER[3] elseif glow > 0.36 then return K.RED[4]
    elseif glow > 0.22 then return K.RED[3] elseif glow > 0.1 then return K.RED[2] end
    return v < 0 and K.STONE[2] or K.STONE[1]
  end)
  -- The logs: crossed, lit along their tops, a cut end at the near side of each.
  local function log(x0, y0, x1, y1)
    c:stroke(x0, y0, x1, y1, 1.2, function(x, y)
      local t = ((x - x0) * (x1 - x0) + (y - y0) * (y1 - y0)) / ((x1 - x0) ^ 2 + (y1 - y0) ^ 2)
      local ly = y0 + (y1 - y0) * t
      return y < ly - 0.3 and K.WOOD[5] or K.WOOD[3]
    end)
    c:px(x0, y0, K.WOOD[6]); c:px(x0, y0 - 1, K.WOOD[5])
  end
  log(7, cy, 19, cy - 4)
  log(21, cy, 10, cy - 4)
  log(13, cy + 2, 15, cy - 5)
  -- The flames: a field of heat over the logs, falling off with height and toward the sides, broken up by noise
  -- stretched upward (so it licks in ragged tongues of uneven heights, with bits breaking off above), cut into bands:
  -- yellow at the hot heart low in the middle, orange, then red, dark red at the tips (2026-11-04, the owner: "the fire
  -- looks like gas flames" of three smooth tongues).
  local function fire(x, y)
    local up = (cy - 2 - y) / 19
    local side = math.abs(x + 0.5 - cx) / 6.5
    if up < -0.15 or side > 1.2 then return nil end
    -- Columns of different heat (the tongues) from noise that changes fast across and slowly up.
    local n = 0.55 * noise(x * 0.85, y * 0.13) + 0.3 * noise(x * 1.6, y * 0.35 + 7) + 0.15 * noise(x * 0.3 + 3, y * 0.6)
    local heat = 1.05 - side * side - up * 1.05 + (n - 0.5) * 1.5
    -- Below its foot it thins out among the coals rather than stopping on a line.
    if up < 0 then heat = heat + up * 4 end
    if heat > 0.78 then return K.EMBER[5] elseif heat > 0.55 then return K.EMBER[4] elseif heat > 0.33 then return K.EMBER[3]
    elseif heat > 0.17 then return K.RED[4] elseif heat > 0.06 then return K.RED[3] end
    return nil
  end
  for _, s in ipairs(stones) do if s[2] >= cy then stone(s) end end
  c:outline()
  -- The flames after the outline, so no black line runs through them.
  for y = cy - 24, cy do
    for x = cx - 9, cx + 9 do
      local colour = fire(x, y)
      if colour then c:px(x, y, colour, false) end
    end
  end
  for _, p in ipairs({ { 9, cy - 21 }, { 18, cy - 23 }, { 13, cy - 27 }, { 20, cy - 17 } }) do
    c:px(p[1], p[2], K.EMBER[4], false)
  end
  c:px(15, cy - 30, K.EMBER[5], false)
  return c
end

-- A pile of bones: a skull on top, long bones and ribs under it.
props.prop_bone_pile = function()
  -- Redrawn in the second pass (2026-11-04): a heap of bones in a five-tone bone ramp (`kit.BONES`), lit from the upper
  -- left: long bones crossed on the ground (a shaft two pixels thick, lit along its top, a double knob at each end),
  -- a ribcage at the back with its spine along the top, and on the heap a skull (a domed head, dark eye sockets, a nose,
  -- a jaw with teeth).
  local c = kit.new(28, 22)
  local g = c.ground
  local B = K.BONES
  c:shadow(14, g, 12, 3)
  local function knob(x, y, lit)
    c:px(x, y - 1, lit and B[5] or B[4]); c:px(x + 1, y - 1, B[4])
    c:px(x, y, B[3]); c:px(x + 1, y, B[2])
    c:px(x, y + 1, lit and B[4] or B[3]); c:px(x + 1, y + 1, B[3])
  end
  local function femur(x0, y0, x1, y1)
    local n = math.max(math.abs(x1 - x0), math.abs(y1 - y0))
    for i = 0, n do
      local x = math.floor(x0 + (x1 - x0) * i / n + 0.5)
      local y = math.floor(y0 + (y1 - y0) * i / n + 0.5)
      c:px(x, y, B[4]); c:px(x, y + 1, B[1])
    end
    knob(x0 - 1, y0, true)
    knob(x1, y1, false)
  end
  -- The ribcage at the back right: the spine two pixels thick along its top, ribs bowing down off it to the ground,
  -- drawn without outline so the gaps between them stay open.
  for x = 15, 25 do
    local y = g - 10 + (x - 15) * 3 // 10
    c:px(x, y, (x % 2 == 0) and B[5] or B[4]); c:px(x, y + 1, B[2])
  end
  for k = 0, 3 do
    local x = 16 + k * 3
    local y = g - 8 + (x - 15) * 3 // 10
    for i = 0, g - y do
      local bow = i < 2 and 0 or i < 5 and 1 or 2
      c:px(x + bow, y + i, i < 4 and B[4] or B[3], false)
    end
  end
  femur(3, g, 14, g - 4)
  femur(6, g - 4, 17, g + 1)
  -- The skull on the heap.
  local sx, sy = 9, g - 8
  c:ellipse(sx, sy, 4, 3.6, function(x, y, u, v)
    local q = u * 0.7 + v * 0.6
    if q < -0.45 then return B[5] elseif q < 0.1 then return B[4] elseif q < 0.55 then return B[3] end
    return B[2]
  end)
  for _, e in ipairs({ { sx - 3, sy }, { sx + 1, sy } }) do
    c:px(e[1], e[2], K.OUTLINE); c:px(e[1] + 1, e[2], K.OUTLINE)
    c:px(e[1], e[2] + 1, B[1]); c:px(e[1] + 1, e[2] + 1, K.OUTLINE)
  end
  c:px(sx - 1, sy + 2, K.OUTLINE)
  -- The jaw, a little narrower, its teeth along its top.
  for x = sx - 3, sx + 2 do
    c:px(x, sy + 3, (x % 2 == 0) and B[5] or B[2])
    c:px(x, sy + 4, x < sx and B[3] or B[2])
  end
  c:outline()
  return c
end

-- Ferns: fronds arching out from a clump, each a stem with leaflets, lit toward their tips.
props.prop_ferns = function()
  -- Redrawn in the second pass (2026-11-04, the owner: "try again with the fern"): a fountain of seven fronds rising
  -- from the crown and curling over under their own weight, the low ones out to the sides drooping to the ground; each a
  -- midrib with leaflets in a herringbone (both sides, every other pixel, leaning toward its tip, longest a third along),
  -- the upper leaflets lit and the lower darker; each frond laid over a dark green shadow of itself a pixel down and
  -- right, which parts it from the fronds behind and from the ground without a black line filling its gaps; those behind
  -- drawn first and darker (`kit.FERN`). (Leaflets drawn round a stem ran into a tuft; solid saw-edged leaves read as a
  -- weed.)
  local c = kit.new(44, 36)
  local g = c.ground
  local cx = 22
  c:shadow(cx, g, 13, 3)
  local function frond(angle, curl, length, tone)
    local points = {}
    local x, y, a = cx, g - 1, angle
    local steps = math.floor(length * 2)
    for i = 0, steps do
      points[#points + 1] = { x, y, a, i / steps }
      x, y = x + math.cos(a) * 0.5, y + math.sin(a) * 0.5
      a = a + curl / steps
    end
    -- Thick (2026-11-04, the owner: "make it thicker"): a midrib two pixels deep and leaflets two pixels thick, set
    -- every other pixel, so the frond reads as a leaf rather than a line.
    for pass = 1, 2 do
      local dx, dy = pass == 1 and 1 or 0, pass == 1 and 1 or 0
      for i, p in ipairs(points) do
        local px, py, pa, t = p[1] + dx, p[2] + dy, p[3], p[4]
        local rib = pass == 1 and K.FERN[1] or K.FERN[math.max(1, tone - 1)]
        c:px(px, py, rib, false)
        if t < 0.85 then c:px(px, py + 1, pass == 1 and K.FERN[1] or K.FERN[math.max(1, tone - 2)], false) end
        if i % 4 == 1 and t > 0.15 and t < 0.96 then
          local leaf = 3.8 * math.sin(math.pi * math.min(1, 0.15 + t * 1.05)) * (1 - t * 0.4)
          for side = -1, 1, 2 do
            local la = pa + side * 1.0
            for k = 1, math.floor(leaf + 0.5) do
              local colour = pass == 1 and K.FERN[1] or (side < 0 and K.FERN[math.min(5, tone + 1)] or K.FERN[tone])
              local lx, ly = px + math.cos(la) * k, py + math.sin(la) * k * 0.9
              c:px(lx, ly, colour, false)
              c:px(lx, ly + 1, pass == 1 and K.FERN[1] or K.FERN[math.max(1, tone - 1)], false)
            end
          end
        end
      end
    end
  end
  -- Behind first: the tall ones arching over at the back, then lower and wider ones out to the sides.
  frond(-1.4, 1.15, 22, 3)
  frond(-1.75, -1.15, 22, 3)
  frond(-1.0, 1.1, 19, 4)
  frond(-2.15, -1.1, 19, 4)
  frond(-0.6, 1.0, 14, 4)
  frond(-2.55, -1.0, 14, 4)
  return c
end

-- Mushrooms: a cluster of red caps with pale spots on pale stems, one fallen over.
props.prop_mushrooms = function()
  -- Redrawn in the second pass (2026-11-04): three red-capped toadstools in a clump: each cap a dome in flat bands lit
  -- from the upper left with a few white flecks, the pale gills showing in a strip under its rim, the stem pale and
  -- shaded on its right with a ring under the cap; grass at their feet.
  local c = kit.new(24, 24)
  local g = c.ground
  c:shadow(12, g, 10, 3)
  local function toadstool(x, foot, height, r, flecks)
    local top = foot - height
    for y = top, foot do
      local half = 1.2 + (foot - y) / height * 0.3
      for xx = math.floor(x - half), math.ceil(x + half) - 1 do
        c:px(xx, y, xx < x - 0.3 and K.CREAM[5] or xx < x + 0.6 and K.CREAM[4] or K.CREAM[2])
      end
    end
    for xx = math.floor(x - 2), math.ceil(x + 2) - 1 do c:px(xx, top + 2, xx < x and K.CREAM[4] or K.CREAM[3]) end
    -- The gills: a pale strip under the cap's rim, ribbed dark.
    for xx = math.floor(x - r + 1), math.ceil(x + r - 1) - 1 do
      c:px(xx, top, (xx % 2 == 0) and K.CREAM[2] or K.CREAM[3])
    end
    c:ellipse(x, top - 1, r, r * 0.7, function(px, py, u, v)
      if v > 0.2 then return nil end
      if v > -0.05 then return K.RED[2] end
      local q = u * 0.7 + v * 0.7
      local tone = q < -0.6 and 5 or q < -0.1 and 4 or 3
      for _, f in ipairs(flecks) do
        if math.abs(px - (x + f[1])) < 1 and math.abs(py - (top - 1 + f[2])) < 1 then return K.BONE[3] end
      end
      return K.RED[tone]
    end)
  end
  toadstool(16, g + 1, 4, 3.6, { { -1, -2 }, { 1.5, -1 } })
  toadstool(8, g, 7, 4.6, { { -2, -2 }, { 1, -3 }, { 2.5, -1 } })
  toadstool(12, g + 2, 3, 2.6, { { 0, -1 } })
  c:outline()
  for _, t in ipairs({ { 4, g + 1 }, { 11, g + 3 }, { 19, g + 2 } }) do
    c:px(t[1], t[2] - 1, K.MOSS[4], false); c:px(t[1] + 1, t[2] - 2, K.MOSS[5], false); c:px(t[1] - 1, t[2] - 2, K.MOSS[3], false)
  end
  return c
end

-- A tree stump: sawn flat, its rings showing, bark around it, roots reaching into the ground, a little moss.
props.prop_stump = function()
  -- Redrawn in the second pass (2026-11-04) the way the barrel is drawn: a short cylinder of bark in flat bands of light
  -- from the left, grooves in the bark where its plates meet (following the curve round it), every row bowing down in
  -- front with the camera's curve; roots flaring out over the ground; moss low on its shaded right side; and on top the sawn
  -- face, an ellipse of rings round the heart with a crack across it, its rim the bark's edge.
  local c = kit.new(26, 26)
  local g = c.ground
  local cx, R, ry, H = 13, 7, 3, 9
  local T = g - H
  c:shadow(cx + 1, g + 1, 11, 4)
  local function u(x) return (x + 0.5 - cx) / R end
  local function curve(x) local q = u(x); return math.floor(ry * math.sqrt(math.max(0, 1 - q * q)) + 0.5) end
  local function band(q)
    if q < -0.75 then return 3 elseif q < -0.35 then return 5 elseif q < 0.1 then return 4 elseif q < 0.55 then return 3 end
    return 2
  end
  -- Roots (2026-11-04, the owner: "bigger roots"): thick where they leave the trunk, a little way up it, tapering out
  -- over the ground, lit along their tops; those behind the trunk drawn before it, those toward the camera after it.
  local function root(a, length, thick)
    local bx, by = cx + math.cos(a) * (R - 1.5), g - (thick > 2 and 3 or 0) + math.sin(a) * (ry - 0.5)
    local tx, ty = cx + math.cos(a) * (R + length), g + math.sin(a) * (ry + length * 0.5)
    for i = 0, 12 do
      local t = i / 12
      local x, y = bx + (tx - bx) * t, by + (ty - by) * t + math.sin(t * math.pi) * 0.8
      local r = thick * (1 - t) + 0.6
      c:ellipse(x, y, r, r * 0.8, function(px, py, u, v) return v < -0.2 and K.WOOD[5] or v < 0.4 and K.WOOD[4] or K.WOOD[3] end)
    end
  end
  local roots = { { math.pi, 7, 2.4 }, { 0, 6, 2.4 }, { 2.4, 5, 1.4 }, { 0.8, 5, 1.4 } }
  for _, r in ipairs(roots) do if math.sin(r[1]) <= 0.05 then root(r[1], r[2], r[3]) end end
  for x = cx - R, cx + R - 1 do
    local q = u(x)
    local groove = math.floor(math.asin(math.max(-1, math.min(1, q))) / (math.pi / 7) + 0.5)
    local seam = groove ~= math.floor(math.asin(math.max(-1, math.min(1, u(x + 1)))) / (math.pi / 7) + 0.5)
    for k = 0, H do
      local y = T + curve(x) + k
      local tone = band(q)
      if seam and (k + x) % 4 ~= 0 then tone = math.max(1, tone - 2) end
      local colour = K.WOOD[tone]
      if q > 0.25 and k > H - 4 and (x + k) % 3 ~= 0 then colour = K.MOSS[q > 0.6 and 3 or 4] end
      if k == H then colour = K.WOOD[2] end
      c:px(x, y, colour)
    end
  end
  for _, r in ipairs(roots) do if math.sin(r[1]) > 0.05 then root(r[1], r[2], r[3]) end end
  -- The sawn face.
  c:ellipse(cx, T, R, ry + 0.5, function(x, y, uu, v)
    local rr = math.sqrt(uu * uu + v * v)
    if rr > 0.86 then return v < 0 and K.WOOD[3] or K.WOOD[4] end
    if uu > 0 and math.abs(v - uu * 0.2) < 0.13 then return K.WOOD[3] end
    -- Freshly cut: pale wood, its rings in darker bands.
    if rr < 0.15 then return K.WOOD[5] end
    if math.floor(rr * 3.5) % 2 == 1 then return uu < 0 and K.WOOD[5] or K.WOOD[4] end
    return uu < 0 and K.SKIN[4] or K.WOOD[6]
  end)
  c:outline()
  return c
end

-- The woodcutters' stump with an axe left in it.
props.prop_axe_stump = function()
  -- The woodcutters' stump (`prop_stump`) with an axe left in its top: the handle angled back, lit along one side, the
  -- head buried in the wood with its edge catching the light.
  local c = props.prop_stump()
  local g = c.ground
  local top = g - 9
  c:stroke(12, top - 1, 5, top - 11, 0.6, function(x, y) return K.WOOD[5] end)
  c:line(13, top - 1, 6, top - 11, K.WOOD[3])
  c:px(5, top - 12, K.WOOD[2])
  c:poly({ 10, top - 3, 16, top - 1, 16, top + 1, 11, top + 1 }, function(x, y) return y < top - 1 and K.STONE[5] or K.STONE[4] end)
  c:line(15, top - 1, 16, top + 1, K.STONE[7])
  c:outline()
  return c
end

-- A fallen log lying back into the scene at the camera's 2:1, its near end sawn, moss along its top, a branch stub.
props.prop_fallen_log = function()
  -- Redrawn in the second pass (2026-11-04): a log lying back into the scene at the camera's 2:1, in flat bands of
  -- light across it (moss in clumps along its top, the bark lit, then in shade, dark underneath), grooves in its bark
  -- running along it, its near end sawn (rings, a crack, a rim of bark), its far end snapped and splintered, a branch
  -- stub, and two little shelves of bracket fungus on its side.
  local c = kit.new(42, 26)
  local g = c.ground
  local x0, y0, x1, y1, r = 9, g - 3, 32, g - 14, 4.5
  local dx, dy = x1 - x0, y1 - y0
  local len = math.sqrt(dx * dx + dy * dy)
  local rand = kit.random(23)
  local clumps, breaks = {}, {}
  for i = 0, 60 do clumps[i] = rand() end
  for i = 0, 60 do breaks[i] = rand() end
  c:shadow(21, g - 4, 19, 5)
  -- Splinters at the snapped far end.
  for _, s in ipairs({ { 2, -5, 0 }, { 3, -3, 3 }, { 2, 0, 2 } }) do
    c:line(x1, y1 + s[2], x1 + s[1] + 1, y1 + s[2] - s[3] // 2, K.WOOD[4])
  end
  c:stroke(x0, y0, x1, y1, r, function(x, y)
    -- Height above the log's middle line, in pixels (up the screen is up the log).
    local d = ((x - x0) * dy - (y - y0) * dx) / len
    local t = ((x - x0) * dx + (y - y0) * dy) / (len * len)
    local i = math.max(0, math.min(60, math.floor(t * 60)))
    local tone
    if d > 2.9 then
      if clumps[i // 2] > 0.35 then return d > 3.8 and K.MOSS[5] or K.MOSS[4] end
      tone = 5
    elseif d > 1.4 then tone = 5 elseif d > -0.6 then tone = 4 elseif d > -2.6 then tone = 3 else tone = 2 end
    for _, k in ipairs({ 0.6, -1.9 }) do
      if math.abs(d - k) < 0.45 and breaks[i // 3] > 0.2 then tone = tone - 1 end
    end
    return K.WOOD[math.max(1, tone)]
  end)
  -- The branch stub off its top.
  c:stroke(22, g - 13, 24, g - 18, 0.8, function(x, y) return x < 23 and K.WOOD[5] or K.WOOD[3] end)
  c:px(24, g - 19, K.WOOD[6])
  -- Bracket fungus on its side.
  for _, f in ipairs({ { 17, g - 6 }, { 20, g - 7 } }) do
    c:px(f[1], f[2], K.CREAM[3]); c:px(f[1] + 1, f[2], K.CREAM[4]); c:px(f[1] + 2, f[2], K.CREAM[3])
    c:px(f[1] + 1, f[2] + 1, K.WOOD[2]); c:px(f[1] + 2, f[2] + 1, K.WOOD[2])
  end
  -- The sawn near end: a rim of bark round the rings, a crack out from the heart.
  c:ellipse(x0 - 1, y0, 3.4, 4.6, function(x, y, u, v)
    local rr = math.sqrt(u * u + v * v)
    if rr > 0.82 then return K.WOOD[2] end
    if u > 0.05 and math.abs(v + u * 0.3) < 0.12 then return K.WOOD[3] end
    if rr < 0.2 then return K.WOOD[4] end
    return math.floor(rr * 4.5) % 2 == 0 and K.WOOD[6] or K.WOOD[5]
  end)
  c:outline()
  return c
end

-- A standing stone: a tall weathered slab, lit on its left face, lichen low on it, a grass tuft at its foot.
props.prop_standing_stone = function()
  -- Redrawn in the second pass (2026-11-04): a weathered monolith in flat facets lit from the left: its front split into
  -- two planes where it was dressed (the left lit, the right a step darker), its side face in shade with a crack, a lit
  -- facet on its broken top, nicks knocked out of its edges; a worn rune cut in its front (a dark groove, its far lip
  -- lit); lichen in specks; grass tufts at its foot. (A spiral and chips as bars read as a tombstone with a hole.)
  local c = kit.new(24, 42)
  local g = c.ground
  c:shadow(13, g, 10, 3)
  local front = { 5, g + 1, 4, g - 10, 6, g - 13, 5, g - 19, 7, g - 26, 9, g - 30, 12, g - 30, 14, g - 26, 15, g - 17,
                  14, g - 14, 15, g - 8, 15, g + 1 }
  local side = { 15, g + 1, 15, g - 8, 14, g - 14, 15, g - 17, 14, g - 26, 17, g - 24, 19, g - 18, 18, g - 10, 19, g - 1 }
  local top = { 9, g - 30, 12, g - 30, 14, g - 26, 17, g - 24, 13, g - 29 }
  -- The edge between the front's two planes, wandering down it.
  local function ridge(y) return 10 + math.floor(math.sin((y - g) * 0.31) * 1.2 + 0.5) end
  c:poly(side, function(x, y)
    if math.abs((x - 16) - (y - (g - 16)) * 0.25) < 0.5 and y > g - 22 and y < g - 9 then return K.STONE[1] end
    return y > g - 4 and K.MOSS[2] or K.STONE[2]
  end)
  c:poly(front, function(x, y)
    if y > g - 4 and (x + y) % 3 ~= 0 then return K.MOSS[3] end
    if (x * 7 + y * 13) % 19 == 0 and y < g - 5 then return K.MOSS[4] end
    if (x * 11 + y * 5) % 29 == 0 then return K.STONE[3] end
    if x < 7 then return K.STONE[6] end
    if x < ridge(y) then return K.STONE[5] end
    if x == ridge(y) then return K.STONE[6] end
    return K.STONE[4]
  end)
  c:poly(top, function(x, y) return x < 13 and K.STONE[7] or K.STONE[6] end)
  -- The rune: a stave with two branches up off it, the groove dark, its right lip catching the light.
  local rx, ry = 9, g - 20
  for k = 0, 8 do c:px(rx, ry + k, K.STONE[2]); c:px(rx + 1, ry + k, K.STONE[6]) end
  for k = 1, 3 do
    c:px(rx - k, ry - k + 2, K.STONE[2]); c:px(rx + k, ry - k + 2, K.STONE[2]); c:px(rx + k + 1, ry - k + 2, K.STONE[6])
  end
  for _, t in ipairs({ { 4, g + 1 }, { 6, g + 2 }, { 13, g + 2 }, { 17, g + 1 }, { 20, g } }) do
    c:px(t[1], t[2] - 1, K.MOSS[4]); c:px(t[1] + 1, t[2] - 2, K.MOSS[5]); c:px(t[1] - 1, t[2] - 2, K.MOSS[3])
    c:px(t[1], t[2] - 3, K.MOSS[4])
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
  -- Redrawn (2026-11-04, the owner: "do it", the carcass first of the second pass): a dead deer on its side in a pool
  -- of its blood, its back toward the far side; flat bands of light from the upper left over a red-brown hide
  -- (`kit.HIDE`), its underside in shade (a pale belly read as a grey line); its rump high and round, its shoulder lower; its flank torn open in a ragged wound
  -- with ribs curving across it; its neck stretched out to the left and its wedge of a head on the ground, an ear and a
  -- branching antler up; its long legs stuck out stiff toward the camera, a dark hoof on each; a white tail.
  local c = kit.new(40, 26)
  local g = c.ground
  c:ellipse(20, g + 1, 17, 4.5, function(x, y, u, v)
    if math.abs(u + 0.4) < 0.07 and math.abs(v + 0.35) < 0.25 then return K.RED[4] end
    return v < -0.2 and K.RED[2] or K.RED[1]
  end, false)
  local function leg(x0, y0, x1, y1)
    c:line(x0, y0, x1, y1, K.HIDE[3])
    c:line(x0 - 1, y0, (x0 + x1) // 2 - 1, (y0 + y1) // 2, K.HIDE[4])
    c:px(x1, y1, K.OUTLINE)
    c:px(x1, y1 + 1, K.OUTLINE)
  end
  leg(14, g - 2, 10, g + 4)
  leg(17, g - 2, 16, g + 5)
  leg(27, g - 2, 32, g + 4)
  leg(29, g - 3, 36, g + 1)
  -- The body: the shoulder and the round, higher rump, lit along the back, the belly pale, its lowest edge dark.
  local function hide(x, y, u, v)
    local q = v + u * 0.15
    if q < -0.55 then return K.HIDE[5] elseif q < -0.1 then return K.HIDE[4] elseif q < 0.5 then return K.HIDE[3] end
    return K.HIDE[2]
  end
  c:ellipse(17, g - 4, 7, 4.2, hide)
  c:ellipse(26, g - 5, 7, 5.4, hide)
  -- The wound: a ragged tear on the flank, dark inside, wet red along its upper lip, ribs curving down across it.
  local rand = kit.random(7)
  local ragged = {}
  for x = 15, 26 do ragged[x] = rand(0, 1) end
  local tops, bottoms = {}, {}
  for x = 15, 26 do
    local u = (x - 20.5) / 6
    local half = 2.6 * math.sqrt(math.max(0, 1 - u * u))
    tops[x], bottoms[x] = math.floor(g - 5 - half + ragged[x]), math.floor(g - 4 + half * 0.6)
    for y = tops[x], bottoms[x] do
      c:px(x, y, y == tops[x] and K.RED[3] or K.RED[1])
    end
  end
  -- Ribs: long bones the height of the tear, bowing toward the rump as they go down, lit at their tops.
  for _, x in ipairs({ 17, 19, 21, 23, 25 }) do
    for y = tops[x] + 1, bottoms[x] do
      local bow = (y - tops[x]) > 2 and 1 or 0
      c:px(x + bow, y, (y - tops[x]) < 2 and K.BONE[3] or K.BONE[2])
    end
  end
  c:px(33, g - 8, K.BONE[3]); c:px(34, g - 8, K.BONE[2]); c:px(33, g - 7, K.BONE[2])
  -- The neck out to the left, the head a wedge lying on the ground with the snout toward the left.
  c:stroke(11, g - 5, 8, g - 4, 1.8, function(x, y) return y < g - 5 and K.HIDE[4] or K.HIDE[3] end)
  c:poly({ 9, g - 6, 5, g - 5, 1, g - 3, 2, g - 1, 8, g - 1 }, function(x, y)
    if x <= 2 then return K.HIDE[1] end
    return y < g - 3 and K.HIDE[4] or K.HIDE[3]
  end)
  c:px(6, g - 4, K.OUTLINE)
  -- The ear back off the head, and the antler branching up and out.
  c:px(9, g - 7, K.HIDE[4]); c:px(10, g - 8, K.HIDE[3]); c:px(10, g - 7, K.HIDE[3])
  c:line(7, g - 6, 5, g - 12, K.BONE[2])
  c:line(6, g - 9, 2, g - 10, K.BONE[2])
  c:line(5, g - 12, 7, g - 15, K.BONE[3])
  c:line(5, g - 12, 2, g - 14, K.BONE[3])
  c:outline()
  return c
end

for name, draw in pairs(props) do
  if not only or string.find("," .. only .. ",", "," .. name .. ",", 1, true) then
    draw():save(out, name)
  end
end
