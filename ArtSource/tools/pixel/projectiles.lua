-- Her arrows, the enemies' thrown things and the hit particles as pixel art at 27 a unit (2026-11-05, the owner: "do 1",
-- after the pools and blood): drawn pointing right (+x), each to its exact size, outlined; the game turns them to the
-- nearest of 16 directions a pixel at a time (TurnedArt) instead of turning the sprite. Written to ArtSource/pixel/world27
-- with their pivots in pivots.json (pixels from the left and from the bottom, each at a pixel's middle, round which it
-- turns): an arrow's at its tip, the rest at the middle of their body.
--   aseprite -b --script-param out=<folder> --script-param kit=<kit.lua> [--script-param only=<name,name>] --script projectiles.lua
local kit = dofile(app.params["kit"])
local out = app.params["out"] or "."
local only = app.params["only"]
local K = kit
local rgba = app.pixelColor.rgba

local pieces, pivots = {}, {}

local function canvas(w, h)
  local c = kit.new(w, h)
  c.exact = true
  return c
end

-- A row of pixels.
local function run(c, x0, x1, y, col, outlined)
  for x = x0, x1 do c:px(x, y, col, outlined) end
end

-- An arrow `w` long pointing right, its shaft on row `y`: fletching at the back, the shaft in banded wood, then `head`
-- draws the head ending at the tip.
local function arrow(w, h, y, vane, head, opts)
  opts = opts or {}
  local c = canvas(w, h)
  local tip = w - 2
  -- Shaft: wood, lit every third pixel.
  for x = 2, tip - 5 do c:px(x, y, (x % 3 == 0) and K.WOOD[5] or K.WOOD[4]) end
  -- Fletching: two vanes slanting back from the shaft, each two rows.
  local len = opts.vaneLength or 5
  for k = 0, len - 1 do
    local x = 1 + k
    c:px(x, y - 1, k % 2 == 0 and vane[2] or vane[1])
    c:px(x, y + 1, k % 2 == 0 and vane[1] or vane[2])
    if k < len - 2 then
      c:px(x, y - 2, vane[2])
      c:px(x, y + 2, vane[1])
    end
  end
  c:px(1, y, K.WOOD[3])
  head(c, tip, y)
  c:outline()
  if opts.after then opts.after(c, tip, y) end
  return c
end

-- A broadhead: barbs at the back, a point tapering to the tip, lit on top.
local function broadhead(steel)
  return function(c, tip, y)
    c:px(tip - 4, y - 2, steel[3]); c:px(tip - 4, y + 2, steel[1])
    run(c, tip - 4, tip - 2, y - 1, steel[3]); run(c, tip - 4, tip - 2, y + 1, steel[1])
    run(c, tip - 5, tip, y, steel[2])
    c:px(tip - 1, y, steel[3])
  end
end

local STEEL = { K.COLD[3], K.COLD[5], K.COLD[6] }

pieces.arrow_plain = function()
  return arrow(24, 9, 4, { K.RED[3], K.RED[5] }, broadhead(STEEL)), 22.5, 4.5
end

pieces.arrow_fire = function()
  local c = arrow(24, 9, 4, { K.RED[3], K.RED[5] }, broadhead({ K.EMBER[2], K.EMBER[4], K.EMBER[5] }), {
    after = function(c, tip, y)
      -- Flames licking back over the head, drawn after the outline.
      c:px(tip - 2, y - 2, K.EMBER[4], false); c:px(tip - 3, y - 3, K.EMBER[3], false)
      c:px(tip - 5, y - 2, K.EMBER[3], false); c:px(tip - 6, y - 1, K.EMBER[2], false)
      c:px(tip - 3, y + 2, K.EMBER[3], false); c:px(tip - 6, y + 1, K.EMBER[2], false)
      c:px(tip, y, K.EMBER[5], false)
    end })
  return c, 22.5, 4.5
end

pieces.arrow_frost = function()
  local ICE = { rgba(70, 120, 170, 255), rgba(150, 206, 238, 255), rgba(236, 250, 255, 255) }
  local c = arrow(24, 9, 4, { rgba(60, 96, 140, 255), rgba(120, 170, 210, 255) }, broadhead(ICE), {
    after = function(c, tip, y)
      c:px(tip - 1, y - 1, ICE[3], false); c:px(tip - 6, y - 2, ICE[2], false); c:px(tip - 7, y + 1, ICE[3], false)
    end })
  return c, 22.5, 4.5
end

pieces.arrow_lightning = function()
  local VOLT = { rgba(80, 110, 190, 255), rgba(170, 200, 255, 255), rgba(250, 250, 210, 255) }
  local c = arrow(24, 9, 4, { rgba(50, 60, 110, 255), rgba(100, 120, 190, 255) }, broadhead(VOLT), {
    after = function(c, tip, y)
      -- A zigzag spark running back along the shaft.
      local zig = { { -6, -2 }, { -7, -1 }, { -8, -2 }, { -9, -3 }, { -11, 2 }, { -12, 1 }, { -13, 2 } }
      for _, p in ipairs(zig) do c:px(tip + p[1], y + p[2], VOLT[3], false) end
    end })
  return c, 22.5, 4.5
end

pieces.arrow_explosive = function()
  local c = arrow(24, 9, 4, { K.RED[3], K.RED[5] }, function(c, tip, y)
    broadhead(STEEL)(c, tip, y)
    -- A black powder charge tied behind the head, a red band round it.
    for dy = -1, 1 do run(c, tip - 9, tip - 6, y + dy, dy < 0 and K.STONE[3] or K.STONE[2]) end
    run(c, tip - 8, tip - 7, y - 2, K.STONE[2])
    run(c, tip - 8, tip - 7, y + 2, K.STONE[1])
    c:px(tip - 7, y - 1, K.RED[5]); c:px(tip - 7, y, K.RED[4]); c:px(tip - 7, y + 1, K.RED[3])
    c:px(tip - 9, y - 1, K.STONE[5])
  end)
  return c, 22.5, 4.5
end

pieces.arrow_pierce = function()
  -- A long, narrow bodkin point that punches through, dark fletching.
  local c = arrow(26, 9, 4, { K.STONE[2], K.STONE[4] }, function(c, tip, y)
    run(c, tip - 7, tip - 1, y, STEEL[2])
    run(c, tip - 7, tip - 3, y - 1, STEEL[3])
    run(c, tip - 7, tip - 4, y + 1, STEEL[1])
    c:px(tip, y, STEEL[3])
  end, { vaneLength = 6 })
  return c, 24.5, 4.5
end

-- The bandits' arrow: grey goose fletching, a plain head.
pieces.projectile_arrow = function()
  return arrow(28, 9, 4, { K.SILK[2], K.SILK[4] }, broadhead(STEEL), { vaneLength = 6 }), 14.5, 4.5
end

-- A thrown knife: a steel blade lit on its back edge, a guard, a wrapped grip and a pommel.
pieces.projectile_knife = function()
  local c = canvas(24, 7)
  local y = 3
  -- Grip, wrapped.
  for x = 2, 7 do c:px(x, y, x % 2 == 0 and K.WOOD[3] or K.WOOD[5]) end
  c:px(1, y, K.STONE[5])
  -- Guard.
  c:px(8, y - 1, K.GOLD[4]); c:px(8, y, K.GOLD[3]); c:px(8, y + 1, K.GOLD[2])
  -- Blade: two rows, the top lit, tapering to the point.
  run(c, 9, 20, y - 1, STEEL[3])
  run(c, 9, 21, y, STEEL[2])
  c:px(22, y, STEEL[3])
  c:outline()
  return c, 12.5, 3.5
end

-- A hand axe: a wooden haft along the flight, a bearded steel head at the far end.
pieces.projectile_axe = function()
  local c = canvas(24, 16)
  local y = 7
  for x = 2, 19 do
    c:px(x, y, x % 4 == 0 and K.WOOD[5] or K.WOOD[4])
    c:px(x, y + 1, K.WOOD[3])
  end
  -- A hatchet's head on the haft's end: a short poll above it, the blade hanging below and flaring both ways to a
  -- curved edge along its bottom, lit there.
  run(c, 17, 20, y - 1, STEEL[2])
  run(c, 17, 20, y, STEEL[1]); run(c, 17, 20, y + 1, STEEL[1])
  local rows = { { 17, 20 }, { 16, 21 }, { 15, 21 }, { 14, 22 }, { 14, 22 } }
  for i, r in ipairs(rows) do
    local yy = y + 1 + i
    run(c, r[1], r[2], yy, i == #rows and STEEL[3] or (i >= 4 and STEEL[2] or STEEL[1]))
  end
  c:px(15, y + 5, STEEL[3]); c:px(21, y + 5, STEEL[3])
  c:px(22, y + 5, rgba(255, 255, 255, 255))
  c:outline()
  return c, 12.5, 7.5
end

-- A spinning blade: four hooked points round a hub with a hole.
pieces.projectile_blade = function()
  local c = canvas(19, 19)
  local cx, cy = 9, 9
  c:ellipse(cx + 0.5, cy + 0.5, 3.5, 3.5, function(x, y, u, v) return (u + v < 0) and STEEL[3] or STEEL[2] end)
  -- The points: each a curved hook, turning the same way.
  local hooks = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } }
  for _, d in ipairs(hooks) do
    local px, py = d[1], d[2]
    local sx, sy = -py, px -- the hook bends this way
    for k = 3, 8 do
      local bend = math.floor((k - 3) * (k - 3) * 0.16)
      c:px(cx + px * k + sx * bend, cy + py * k + sy * bend, k < 7 and STEEL[2] or STEEL[3])
      if k < 7 then c:px(cx + px * k + sx * (bend + 1), cy + py * k + sy * (bend + 1), STEEL[1]) end
    end
  end
  c:outline()
  -- The hub's hole, and a glint.
  c:px(cx, cy, K.OUTLINE, false); c:px(cx + 1, cy, K.STONE[2], false)
  c:px(cx - 2, cy - 2, rgba(255, 255, 255, 255), false)
  return c, 9.5, 9.5
end

-- A bomb: an iron ball lit from the upper left, a fuse with its spark.
pieces.projectile_bomb = function()
  local c = canvas(17, 17)
  local cx, cy = 7, 9
  c:ellipse(cx, cy, 5.5, 5.5, function(x, y, u, v)
    local lit = -u * 0.6 - v * 0.8
    return lit > 0.55 and K.STONE[4] or lit > 0.1 and K.STONE[3] or lit > -0.4 and K.STONE[2] or K.STONE[1]
  end)
  -- The fuse cap and the fuse.
  run(c, cx + 2, cx + 4, cy - 6, K.STONE[3])
  c:px(cx + 5, cy - 7, K.WOOD[5]); c:px(cx + 6, cy - 8, K.WOOD[4])
  c:outline()
  c:px(cx - 2, cy - 3, K.STONE[6], false); c:px(cx - 3, cy - 2, K.STONE[5], false)
  -- The spark, drawn after the outline.
  c:px(cx + 7, cy - 9, K.EMBER[5], false); c:px(cx + 8, cy - 9, K.EMBER[3], false); c:px(cx + 7, cy - 10, K.EMBER[4], false)
  c:px(cx + 6, cy - 9, K.EMBER[3], false)
  return c, 7.5, 7.5
end

-- A fireball: a white-hot head on the right, a tail of flame thinning out behind it, no outline (it is light).
pieces.projectile_fireball = function()
  local c = canvas(30, 16)
  local cx, cy = 22, 8
  local rand = kit.random(7)
  for y = 0, 15 do
    for x = 0, 29 do
      local dx, dy = (x + 0.5 - cx) / 6.5, (y + 0.5 - cy) / 6
      local head = 1 - math.sqrt(dx * dx + dy * dy)
      -- The tail: narrowing back from the head, ragged.
      local back = (cx - x) / 21
      local width = (1 - back) * 5.5
      local tail = (back > 0 and back < 1) and (1 - math.abs(y + 0.5 - cy - math.sin(back * 7) * back * 1.5) / math.max(0.5, width)) or -1
      local base = math.max(head * 1.4, tail > 0 and tail * (1 - back) * 1.1 or -1)
      local heat = base + (rand() - 0.5) * 0.25
      if base > 0 and heat > 0.05 then
        local col = heat > 1.05 and rgba(255, 250, 220, 255) or heat > 0.8 and K.EMBER[5] or heat > 0.5 and K.EMBER[4]
          or heat > 0.25 and K.EMBER[3] or K.EMBER[2]
        c:px(x, y, col, false)
      end
    end
  end
  return c, 22.5, 8.5
end

-- --- Particles (shown at their own size, a few pixels each) --------------------------------------------------------

local function blob(w, h, ramp, opts)
  opts = opts or {}
  local c = canvas(w, h)
  local cx, cy = (w - 1) / 2, (h - 1) / 2
  c:ellipse(cx + 0.5, cy + 0.5, w / 2 - 0.4, h / 2 - 0.4, function(x, y, u, v)
    local lit = -u * 0.5 - v * 0.7
    local r = math.sqrt(u * u + v * v)
    if opts.core then return r < 0.45 and ramp[#ramp] or r < 0.8 and ramp[#ramp - 1] or ramp[#ramp - 2] end
    return lit > 0.45 and ramp[#ramp] or lit > -0.2 and ramp[#ramp - 1] or ramp[#ramp - 2]
  end, false)
  return c
end

local HOT = { K.EMBER[2], K.EMBER[3], K.EMBER[4], K.EMBER[5], rgba(255, 250, 225, 255) }
local BLOOD = { rgba(60, 4, 8, 255), rgba(110, 10, 14, 255), rgba(160, 30, 30, 255), rgba(206, 70, 64, 255) }
local SLIME = { rgba(40, 80, 14, 255), rgba(80, 140, 26, 255), rgba(140, 200, 50, 255), rgba(206, 240, 120, 255) }
local ICE = { rgba(70, 120, 170, 255), rgba(150, 206, 238, 255), rgba(214, 238, 252, 255), rgba(255, 255, 255, 255) }
local SMOKE = { rgba(52, 50, 52, 255), rgba(84, 82, 84, 255), rgba(120, 118, 118, 255), rgba(160, 158, 156, 255) }

pieces.fx_ember = function() return blob(5, 5, HOT, { core = true }), 2.5, 2.5 end
pieces.fx_ember_small = function() return blob(3, 3, HOT, { core = true }), 1.5, 1.5 end

-- A spark: a bright head on the right, a short tail behind (turned to its flight).
pieces.fx_spark = function()
  local c = canvas(7, 3)
  run(c, 0, 2, 1, HOT[3], false); run(c, 3, 4, 1, HOT[4], false); c:px(5, 1, HOT[5], false); c:px(6, 1, HOT[4], false)
  c:px(5, 0, HOT[3], false); c:px(5, 2, HOT[3], false)
  return c, 5.5, 1.5
end

-- A streak: a long thin line of light, brightest at its head.
pieces.fx_streak = function()
  local c = canvas(9, 1)
  for x = 0, 8 do c:px(x, 0, x < 3 and HOT[3] or x < 6 and HOT[4] or HOT[5], false) end
  return c, 8.5, 0.5
end

-- An ice shard: a pointed crystal, lit on top.
pieces.fx_shard = function()
  local c = canvas(7, 3)
  run(c, 0, 5, 1, ICE[2], false); c:px(6, 1, ICE[4], false)
  run(c, 1, 4, 0, ICE[3], false); run(c, 1, 3, 2, ICE[1], false)
  return c, 3.5, 1.5
end

pieces.fx_chip = function()
  local c = canvas(3, 3)
  c:px(1, 0, ICE[4], false); c:px(0, 1, ICE[3], false); c:px(1, 1, ICE[2], false); c:px(2, 1, ICE[2], false); c:px(1, 2, ICE[1], false)
  return c, 1.5, 1.5
end

-- A drop of blood: a little teardrop with a glint.
pieces.fx_blood = function()
  local c = canvas(3, 4)
  c:px(1, 0, BLOOD[2], false)
  run(c, 0, 2, 1, BLOOD[2], false); run(c, 0, 2, 2, BLOOD[2], false); run(c, 0, 2, 3, BLOOD[1], false)
  c:px(0, 1, BLOOD[3], false); c:px(1, 2, BLOOD[3], false)
  return c, 1.5, 2
end

-- A splash of blood: a blob and two flecks.
pieces.fx_blood_splash = function()
  local c = blob(5, 4, BLOOD)
  c:px(-1, 0, BLOOD[2], false)
  return c, 2.5, 2
end

pieces.fx_poison = function()
  local c = canvas(3, 4)
  c:px(1, 0, SLIME[3], false)
  run(c, 0, 2, 1, SLIME[3], false); run(c, 0, 2, 2, SLIME[2], false); run(c, 0, 2, 3, SLIME[1], false)
  c:px(0, 1, SLIME[4], false)
  return c, 1.5, 2
end

pieces.fx_poison_splash = function()
  local c = canvas(7, 5)
  local b = blob(5, 4, SLIME)
  -- A blob in the middle and flecks round it.
  for y = 0, 3 do
    for x = 0, 4 do
      local u, v = (x - 2) / 2.2, (y - 1.5) / 1.8
      if u * u + v * v <= 1 then c:px(x + 1, y + 1, (u + v < -0.3) and SLIME[4] or (u + v < 0.4) and SLIME[3] or SLIME[2], false) end
    end
  end
  b.sprite:close()
  c:px(0, 0, SLIME[3], false); c:px(6, 1, SLIME[2], false); c:px(6, 4, SLIME[3], false)
  return c, 3.5, 2.5
end

-- Smoke: a lumpy puff of grey, lit on top.
local function puff(w, h, seed)
  local c = canvas(w, h)
  local rand = kit.random(seed)
  local lumps = { { w * 0.35, h * 0.6, w * 0.32 }, { w * 0.62, h * 0.55, w * 0.3 }, { w * 0.5, h * 0.35, w * 0.3 } }
  for y = 0, h - 1 do
    for x = 0, w - 1 do
      local best = -1
      for _, l in ipairs(lumps) do
        local dx, dy = (x + 0.5 - l[1]) / l[3], (y + 0.5 - l[2]) / (l[3] * 0.85)
        best = math.max(best, 1 - (dx * dx + dy * dy) - dy * 0.15)
      end
      if best > 0 then
        local lit = 1 - (y + 0.5) / h + (rand() - 0.5) * 0.2
        c:px(x, y, lit > 0.62 and SMOKE[4] or lit > 0.4 and SMOKE[3] or lit > 0.2 and SMOKE[2] or SMOKE[1], false)
      end
    end
  end
  return c
end

pieces.fx_smoke = function() return puff(9, 7, 3), 4.5, 3.5 end
pieces.fx_smoke_small = function() return puff(5, 4, 5), 2.5, 2 end

local names = {}
for name in pairs(pieces) do names[#names + 1] = name end
table.sort(names)
for _, name in ipairs(names) do
  if not only or string.find("," .. only .. ",", "," .. name .. ",", 1, true) then
    local c, px, py = pieces[name]()
    -- The pivot from the left and, since Unity counts from the bottom, from the bottom.
    pivots[#pivots + 1] = string.format('  { "name": "%s", "x": %s, "y": %s }', name, px, c.H - py)
    c:save(out, name)
  end
end
if not only then
  local f = io.open(out .. "/pivots.json", "w")
  f:write('{ "items": [\n' .. table.concat(pivots, ",\n") .. "\n] }\n")
  f:close()
end
