-- The pet wolf animated as a cut-out rig (2026-11-06, the owner: "how should we rig the dog?", then "do all
-- animations"): each of the owner's views (ArtSource/pixel/characters/pet_wolf/views, cut from the strip by
-- strip_frame.lua) is split into pieces: the legs (each shape of pixels below the leg line), the body, the head with its
-- jaw, the tail. Every frame moves pieces by whole pixels, never turning or stretching them, so the owner's pixels stay
-- as drawn: a walk lifts and swings alternate legs while the body bobs; idle breathes and wags; the bite crouches, lunges
-- with the jaw dropping open and snaps; a hit jerks back; death lowers the body onto folded legs and the head onto the
-- ground. The side view's own four walk frames stay the side walk and run.
-- Rows s, sw, w, nw, n, ne, e, se; the right-facing side is the left one mirrored.
--   aseprite -b --script-param views=<folder> --script-param out=<folder> --script wolf_rig.lua
local rgba, A = app.pixelColor.rgba, app.pixelColor.rgbaA
local dir, out = app.params["views"], app.params["out"]
local CW, CH, PX, PY = 88, 60, 44, 4
local MOUTH = rgba(30, 14, 14, 255)

local function load(i)
  return Image { fromFile = dir .. "/v" .. i .. ".png" }
end

-- Each view: its leg line (rows from it down are legs), its head, the jaw inside the head, the tail (x0, y0, x1, y1,
-- inclusive), and which way is forward on screen.
local configs = {
  side = { img = 1, leg = 21, head = { 0, 0, 16, 19 }, jaw = { 0, 13, 13, 17 }, tail = { 51, 9, 70, 22 }, fx = -1, fy = 0 },
  sw = { img = 5, leg = 23, head = { 0, 6, 15, 27 }, jaw = { 0, 21, 13, 27 }, tail = { 44, 0, 58, 13 }, fx = -1, fy = 0 },
  se = { img = 6, leg = 23, head = { 33, 9, 49, 26 }, jaw = { 35, 21, 49, 26 }, tail = { 0, 3, 11, 12 }, fx = 1, fy = 0 },
  s = { img = 7, leg = 29, head = { 3, 0, 20, 21 }, jaw = { 7, 17, 16, 21 }, fx = 0, fy = 1 },
  n = { img = 8, leg = 43, head = { 2, 0, 20, 12 }, tail = { 6, 26, 17, 46 }, fx = 0, fy = -1 },
}
local walkSide = { load(1), load(2), load(3), load(4) }

-- A view cut into its pieces: lists of {x, y, colour}.
local function cut(cfg)
  local im = load(cfg.img)
  local W, H = im.width, im.height
  local function inside(r, x, y) return r and x >= r[1] and x <= r[3] and y >= r[2] and y <= r[4] end
  local p = { W = W, H = H, body = {}, head = {}, jaw = {}, tail = {}, legs = {}, under = {} }
  -- The legs: shapes of pixels at or below the leg line, found by flood fill; each keeps three rows above the line
  -- (drawn under the body) so a moved leg shows no gap where it was cut.
  local legOf = {}
  local n = 0
  for y = cfg.leg, H - 1 do
    for x = 0, W - 1 do
      local key = y * W + x
      if A(im:getPixel(x, y)) > 0 and not legOf[key] then
        n = n + 1
        local stack = { { x, y } }
        legOf[key] = n
        while #stack > 0 do
          local q = table.remove(stack)
          for _, d in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
            local nx, ny = q[1] + d[1], q[2] + d[2]
            if nx >= 0 and nx < W and ny >= cfg.leg and ny < H and A(im:getPixel(nx, ny)) > 0 and not legOf[ny * W + nx] then
              legOf[ny * W + nx] = n
              stack[#stack + 1] = { nx, ny }
            end
          end
        end
      end
    end
  end
  for k = 1, n do p.legs[k] = { px = {}, cx = 0, count = 0 } end
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      local c = im:getPixel(x, y)
      if A(c) > 0 then
        local px = { x, y, c }
        local leg = legOf[y * W + x]
        if leg then
          local l = p.legs[leg]
          l.px[#l.px + 1] = px
          l.cx, l.count = l.cx + x, l.count + 1
        else
          p.body[#p.body + 1] = px
          if inside(cfg.head, x, y) then
            if inside(cfg.jaw, x, y) then p.jaw[#p.jaw + 1] = px else p.head[#p.head + 1] = px end
          elseif inside(cfg.tail, x, y) then
            p.tail[#p.tail + 1] = px
          end
          -- The three rows over the leg line, under each leg below them.
          if y >= cfg.leg - 3 then
            for k, l in ipairs(p.legs) do
              if legOf[(cfg.leg) * W + x] == k then l.px[#l.px + 1] = px end
            end
          end
        end
      end
    end
  end
  -- Legs in order across the picture; tiny shapes (a stray pixel) dropped.
  local legs = {}
  for _, l in ipairs(p.legs) do
    if l.count >= 4 then
      l.cx = l.cx / l.count
      legs[#legs + 1] = l
    end
  end
  table.sort(legs, function(a, b) return a.cx < b.cx end)
  -- Legs that touch (the front and back views' pair) come as one shape: split down its middle.
  if #legs == 1 then
    local one, left, right = legs[1], { px = {}, cx = 0, count = 0 }, { px = {}, cx = 0, count = 0 }
    for _, q in ipairs(one.px) do
      local half = q[1] < one.cx and left or right
      half.px[#half.px + 1] = q
      half.cx, half.count = half.cx + q[1], half.count + 1
    end
    left.cx, right.cx = left.cx / math.max(1, left.count), right.cx / math.max(1, right.count)
    legs = { left, right }
  end
  p.legs = legs
  return p
end

local cuts = {}
for name, cfg in pairs(configs) do cuts[name] = cut(cfg) end

-- Draws a posed view into a cell. pose: body {dx, dy} (dy up), legs[i] {dx, dy}, head {dx, dy}, jaw (pixels open),
-- tail {dx, dy}, sink (the body lowered onto folded legs, 0 to 1), mirror.
local function draw(sheet, col, row, cfg, p, pose)
  local x0 = col * CW + PX - p.W // 2
  local y0 = row * CH + CH - PY - p.H
  local function put(x, y, c)
    if pose.mirror then x = p.W - 1 - x end
    local sx, sy = x0 + x, y0 + y
    if sx >= col * CW and sx < (col + 1) * CW and sy >= row * CH and sy < (row + 1) * CH then sheet:drawPixel(sx, sy, c) end
  end
  local b = pose.body or { 0, 0 }
  local sink = pose.sink or 0
  -- The legs fold as the body sinks: rows squeezed toward the ground.
  for i, l in ipairs(p.legs) do
    local o = pose.legs and pose.legs[i] or { 0, 0 }
    for _, q in ipairs(l.px) do
      local y = q[2]
      if sink > 0 then y = math.floor(p.H - 1 - (p.H - 1 - y) * (1 - 0.65 * sink)) end
      put(q[1] + o[1] + b[1] * 0, y - o[2], q[3])
    end
  end
  local drop = math.floor((p.H - cfg.leg) * 0.6 * sink + 0.5)
  for _, q in ipairs(p.body) do put(q[1] + b[1], q[2] - b[2] + drop, q[3]) end
  local t = pose.tail or { 0, 0 }
  if t[1] ~= 0 or t[2] ~= 0 then
    for _, q in ipairs(p.tail) do put(q[1] + b[1] + t[1], q[2] - b[2] - t[2] + drop, q[3]) end
  end
  local h = pose.head or { 0, 0 }
  local jaw = pose.jaw or 0
  if h[1] ~= 0 or h[2] ~= 0 or jaw > 0 then
    local hx, hy = b[1] + h[1], -b[2] - h[2] + drop
    for _, q in ipairs(p.head) do put(q[1] + hx, q[2] + hy, q[3]) end
    -- The open mouth: dark between the jaw's old and new places, then the jaw lowered.
    if jaw > 0 then
      for _, q in ipairs(p.jaw) do
        for k = 0, jaw - 1 do put(q[1] + hx, q[2] + hy + k, MOUTH) end
      end
    end
    for _, q in ipairs(p.jaw) do put(q[1] + hx, q[2] + hy + jaw, q[3]) end
  end
end

-- The legs' two sets: alternate legs across the picture.
local function stepLegs(p, frame, lift, swing, fx)
  local legs = {}
  local phase = frame % 4
  for i = 1, #p.legs do
    local set = (i % 2 == 1) and 1 or 3
    if phase == set then
      legs[i] = { swing * fx, lift }
    elseif phase == (set + 2) % 4 then
      legs[i] = { -swing * fx, 0 }
    else
      legs[i] = { 0, 0 }
    end
  end
  return legs
end

local rows = {
  { name = "s", view = "s" }, { name = "sw", view = "sw" }, { name = "w", view = "side" }, { name = "nw", view = "side" },
  { name = "n", view = "n" }, { name = "ne", view = "side", mirror = true }, { name = "e", view = "side", mirror = true },
  { name = "se", view = "se" },
}

-- The side's own walk frames into a cell, mirrored when asked, bobbed by dy.
local function drawSide(sheet, col, row, im, mirror, dy)
  local x0 = col * CW + PX - im.width // 2
  local y0 = row * CH + CH - PY - im.height - (dy or 0)
  for y = 0, im.height - 1 do
    for x = 0, im.width - 1 do
      local c = im:getPixel(x, y)
      if A(c) > 0 then sheet:drawPixel(x0 + (mirror and im.width - 1 - x or x), y0 + y, c) end
    end
  end
end

local anims = {
  { name = "idle", frames = 8, seconds = 2.4, pose = function(cfg, p, i)
    -- Breathing (the body up a pixel), the tail wagging, a small nod.
    local breathe = (i >= 3 and i <= 5) and 1 or 0
    local wag = ({ 0, 1, 0, -1, 0, 1, 0, -1 })[i + 1]
    return { body = { 0, breathe }, tail = { cfg.fy == 0 and 0 or wag, cfg.fy == 0 and wag or 0 }, head = { 0, i == 6 and -1 or 0 } }
  end },
  { name = "walk", frames = 8, seconds = 1.0, speed = 1.4, side = true, pose = function(cfg, p, i)
    return { body = { 0, (i % 2 == 1) and 1 or 0 }, legs = stepLegs(p, i, 2, 1, cfg.fx), tail = { 0, (i % 4 == 1) and 1 or 0 } }
  end },
  { name = "run", frames = 8, seconds = 0.5, speed = 2.6, side = true, pose = function(cfg, p, i)
    return { body = { 0, (i % 2 == 1) and 1 or 0 }, legs = stepLegs(p, i, 3, 2, cfg.fx), head = { 0, (i % 2 == 1) and -1 or 0 },
      tail = { 0, (i % 2 == 0) and 1 or 0 } }
  end },
  { name = "attack", frames = 8, seconds = 0.917, pose = function(cfg, p, i)
    local crouch = ({ 0, 1, 2, 0, 0, 0, 0, 0 })[i + 1]
    local lunge = ({ 0, 0, -1, 3, 4, 3, 1, 0 })[i + 1]
    local jaw = ({ 0, 0, 0, 2, 3, 1, 0, 0 })[i + 1]
    local fx, fy = cfg.fx, cfg.fy
    return { body = { lunge * fx, -crouch + (fy ~= 0 and lunge * -fy or 0) }, head = { lunge * fx, -crouch + (i == 4 and 1 or 0) },
      jaw = jaw, tail = { 0, (i >= 3 and i <= 5) and 1 or 0 } }
  end },
  { name = "hit", frames = 3, seconds = 0.333, pose = function(cfg, p, i)
    local back = ({ 2, 3, 1 })[i + 1]
    return { body = { -back * cfg.fx, cfg.fy ~= 0 and back * cfg.fy or 0 }, head = { -cfg.fx, 1 }, tail = { 0, -1 } }
  end },
  { name = "death", frames = 8, seconds = 1.0, pose = function(cfg, p, i)
    local k = math.min(1, i / 5)
    return { sink = k, head = { cfg.fx * math.floor(2 * k), -math.floor(3 * k) }, tail = { 0, -math.floor(2 * k) } }
  end },
}

local list = {}
for _, a in ipairs(anims) do
  local sheet = Image(CW * a.frames, CH * #rows, ColorMode.RGB)
  sheet:clear(rgba(0, 0, 0, 0))
  for ri, r in ipairs(rows) do
    local cfg = configs[r.view]
    for i = 0, a.frames - 1 do
      if a.side and r.view == "side" then
        -- The owner's own walk frames for the side, a bob in the run.
        drawSide(sheet, i, ri - 1, walkSide[(i % 4) + 1], r.mirror, a.name == "run" and (i % 2) or 0)
      else
        local pose = a.pose(cfg, cuts[r.view], i)
        pose.mirror = r.mirror
        draw(sheet, i, ri - 1, cfg, cuts[r.view], pose)
      end
    end
  end
  sheet:saveAs(out .. "/pet_wolf_" .. a.name .. ".png")
  list[#list + 1] = string.format('{"name":"%s","frames":%d,"seconds":%s,"speed":%s}', a.name, a.frames, a.seconds, a.speed or 0)
end
local f = io.open(out .. "/sheet.json", "w")
f:write(string.format('{"cellWidth":%d,"cellHeight":%d,"pivotX":%d,"pivotY":%d,"directions":8,"animations":[%s]}\n', CW, CH, PX, PY,
  table.concat(list, ",")))
f:close()
local legs = {}
for name, p in pairs(cuts) do legs[#legs + 1] = name .. " " .. #p.legs end
print("REPORT rig legs: " .. table.concat(legs, ", "))
