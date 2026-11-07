-- The pet wolf drawn as pixel art (2026-11-05, the owner: "redraw the wolf", then "Hand-drawn pixel art"): a wolf built
-- of round shapes on a small skeleton (torso, chest, neck, head and snout, ears, four two-jointed legs, a bushy tail, a
-- red collar), posed frame by frame for idle, run, attack, hit and death, seen from each of the 8 directions at the
-- game's 2:1 view, and drawn a pixel at a time: each shape shaded in flat bands of the fur's warm brown (cream under the
-- belly and on the tail's tip, a dark saddle on the back), the far side a shade darker, a dark line where a nearer part
-- crosses a farther one, and a dark outline round all. 27 pixels a unit, the game's screen grid.
-- Writes ArtSource/pixel/characters/pet_wolf: a sheet per animation (rows s, sw, w, nw, n, ne, e, se from the top; a
-- column a frame) and sheet.json (cell size, pivot, frames, seconds), for Tools > ARPG > Import Drawn Characters.
--   aseprite -b --script-param out=<folder> [--script-param only=run] --script wolf_drawn.lua
local out = app.params["out"]
local only = app.params["only"]
local rgba = app.pixelColor.rgba

local CW, CH = 88, 64          -- the cell
local PX, PY = 44, 12          -- the pivot: the feet's middle, from the left and from the bottom
local SIZE = 1.54             -- the whole wolf, against the measures below

local OUTLINE = rgba(16, 10, 8, 255)
local FUR = { rgba(34, 34, 36, 255), rgba(58, 58, 60, 255), rgba(86, 86, 88, 255), rgba(116, 116, 116, 255), rgba(146, 144, 140, 255),
  rgba(176, 174, 168, 255) }
local SADDLE = { rgba(36, 31, 26, 255), rgba(58, 50, 41, 255), rgba(84, 74, 61, 255), rgba(112, 100, 84, 255), rgba(138, 126, 108, 255) }
local CREAM = { rgba(120, 112, 100, 255), rgba(160, 152, 138, 255), rgba(196, 188, 172, 255), rgba(224, 218, 204, 255), rgba(242, 238, 228, 255) }
local TOOTH = rgba(232, 222, 196, 255)
local DARK = { rgba(20, 14, 12, 255), rgba(36, 26, 20, 255), rgba(56, 40, 30, 255), rgba(78, 58, 42, 255) }
local RED = { rgba(70, 14, 14, 255), rgba(120, 24, 20, 255), rgba(170, 40, 30, 255), rgba(210, 70, 44, 255) }
local EYE = rgba(226, 168, 60, 255)
local GOLDEN = { rgba(92, 70, 24, 255), rgba(150, 118, 40, 255), rgba(210, 172, 70, 255), rgba(244, 218, 120, 255) }

-- Light from the upper left, a little toward the viewer (screen x right, y up, z out of the screen).
local LX, LY, LZ = -0.55, 0.62, 0.56

local function v(x, y, z) return { x, y, z } end
local function add(a, b) return { a[1] + b[1], a[2] + b[2], a[3] + b[3] } end
local function scale(a, s) return { a[1] * s, a[2] * s, a[3] * s } end
local function lerp(a, b, t) return { a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t, a[3] + (b[3] - a[3]) * t } end

-- A leg from its top joint: swing (radians, forward positive) and lift; the middle joint bent forward (front legs) or
-- back (hind legs).
local function leg(top, length1, length2, swing, bend, lift)
  local mid = add(top, v(math.sin(swing) * length1 + bend, 0, -math.cos(swing) * length1))
  local foot = add(mid, v(math.sin(swing * 0.6) * length2 - bend * 0.4, 0, -math.cos(swing * 0.6) * length2 + lift))
  if foot[3] < 0.8 then foot[3] = 0.8 end
  return mid, foot
end

-- The wolf in a pose: a list of shapes, each a capsule from a to b with radii, a ramp and a kind.
local function wolf(p)
  local s = {}
  local group = "body"
  local function cap(a, b, r1, r2, ramp, kind)
    local k = SIZE
    s[#s + 1] = { a = scale(a, k), b = scale(b, k), r1 = r1 * k, r2 = r2 * k, ramp = ramp, kind = kind, group = group }
  end
  local bob, lean, dx = p.bob or 0, p.lean or 0, p.dx or 0
  -- A wolf's build: high withers over long, thin legs, a deep narrow chest, the waist tucked up, the back sloping a
  -- little to the hips, the head carried forward on a thick neck.
  local chest = v(4 + dx, 0, 14.4 + bob + lean)
  local hip = v(-7 + dx, 0, 13.8 + bob - lean * 0.5)
  -- Legs first (so the ones on the far side are behind the body when depths tie).
  local function legs(side, sy)
    group = "front" .. side
    local fs, fl = p.front[side][1], p.front[side][2]
    local hs, hl = p.hind[side][1], p.hind[side][2]
    local shoulder = add(chest, v(1.2, sy * 1.9, -2.2))
    local m, f = leg(shoulder, 4.2, 7.4, fs, 0.2, fl)
    cap(shoulder, m, 2.3, 1.5, FUR, "upperleg"); cap(m, f, 1.4, 1.15, FUR, "leg"); cap(f, add(f, v(1.6, 0, 0)), 1.35, 1.1, FUR, "paw")
    group = "hind" .. side
    local hipJoint = add(hip, v(0, sy * 2.0, -1.4))
    local knee = add(hipJoint, v(math.sin(hs) * 4.6 + 1.6, 0, -math.cos(hs) * 4.6))
    local hock = add(knee, v(-3.0 + math.sin(hs) * 1.2, 0, -4.2))
    local paw = add(hock, v(math.sin(hs) * 1.2 + 0.6, 0, -2.8 + hl))
    if paw[3] < 0.8 then paw[3] = 0.8 end
    cap(hipJoint, knee, 3.0, 1.8, FUR, "thigh"); cap(knee, hock, 1.5, 1.15, FUR, "leg"); cap(hock, paw, 1.15, 1.05, FUR, "leg")
    cap(paw, add(paw, v(1.6, 0, 0)), 1.35, 1.1, FUR, "paw")
  end
  legs("l", 1); legs("r", -1)
  group = "body"
  -- The deep chest down to the elbows, the thin waist, the haunch, the raised withers.
  cap(add(chest, v(0.6, 0, -1.6)), add(chest, v(0.6, 0, -1.6)), 4.8, 4.8, FUR, "chest")
  cap(add(chest, v(-1, 0, 0)), v(-2.5 + dx, 0, 14.4 + bob), 4.4, 3.6, FUR, "torso")
  cap(v(-2.5 + dx, 0, 14.4 + bob), add(hip, v(0.5, 0, 0.2)), 3.6, 3.7, FUR, "torso")
  cap(add(hip, v(-0.4, 0, 0.1)), add(hip, v(-0.4, 0, 0.1)), 4.0, 4.0, FUR, "haunch")
  cap(add(chest, v(0.8, 0, 2.4)), add(chest, v(0.8, 0, 2.4)), 3.7, 3.7, FUR, "ruff")
  -- The mane: tufts standing up along the neck and the withers, swept back.
  for i = 0, 5 do
    local base = add(chest, v(4.2 - i * 1.3, 0, 3.6 + (i < 3 and i * 0.5 or (5 - i) * 0.4)))
    cap(base, add(base, v(-1.6, 0, 1.9 - i * 0.15)), 1.15, 0.2, FUR, "mane")
  end
  -- The tail: hanging low from the rump, bushy, its tip dark.
  group = "tail"
  local t0 = add(hip, v(-3.0, 0, 0.6))
  local t1 = add(t0, v(-3.8, p.tail[1], -2.4 + p.tail[2]))
  local t2 = add(t1, v(-3.0, p.tail[1] * 1.5, -3.4 + p.tail[3]))
  cap(t0, t1, 1.7, 2.8, FUR, "tail"); cap(t1, t2, 2.8, 1.2, FUR, "tailtip")
  -- The neck forward and up, the head level: a broad skull, pale cheeks, a long narrow muzzle, tall pointed ears.
  group = "head"
  local neckTop = add(chest, v(4.8 + p.head[1] * 0.4, p.head[3] * 0.3, 3.4 + p.head[2] * 0.6))
  cap(add(chest, v(2.0, 0, 1.0)), neckTop, 3.4, 2.7, FUR, "neck")
  local head = add(neckTop, v(1.8 + p.head[1], p.head[3], 0.8 + p.head[2]))
  cap(head, head, 3.0, 3.0, FUR, "head")
  cap(add(head, v(-0.5, 0, -1.3)), add(head, v(-0.5, 0, -1.3)), 2.7, 2.7, FUR, "cheek")
  local jaw = p.jaw or 0
  local snoutEnd = add(head, v(6.0, p.head[3] * 0.25, -0.9 + jaw * 0.5))
  cap(add(head, v(1.6, 0, -0.3)), snoutEnd, 1.8, 1.0, FUR, "snout")
  if jaw > 0 then cap(add(head, v(1.5, 0, -1.6)), add(head, v(5.0, 0, -2.1 - jaw * 2.4)), 1.2, 0.7, FUR, "jaw") end
  cap(snoutEnd, snoutEnd, 0.85, 0.85, DARK, "nose")
  -- Bared fangs when the jaw opens.
  if jaw > 0.2 then
    for _, q in ipairs({ add(head, v(4.6, 0.6, -1.7)), add(head, v(4.6, -0.6, -1.7)), add(head, v(3.4, 0.9, -1.8)) }) do
      cap(q, add(q, v(0, 0, -0.9)), 0.45, 0.3, { TOOTH, TOOTH, TOOTH, TOOTH }, "tooth")
    end
  end
  local earBack = p.ears or 0
  for _, sy in ipairs({ 1, -1 }) do
    local base = add(head, v(-0.7, sy * 1.5, 2.1))
    cap(base, add(base, v(-0.3 - earBack * 2.2, sy * 0.5, 4.0 - earBack * 1.6)), 1.6, 0.25, FUR, "ear")
  end
  -- The eyes: points drawn over the head when seen.
  local eyes = { scale(add(head, v(2.0, 1.3, 0.7)), SIZE), scale(add(head, v(2.0, -1.3, 0.7)), SIZE) }
  return s, eyes, p.roll or 0
end

-- The model turned to face a ground direction and rolled over (death), then seen at the game's 2:1 view: screen x, screen
-- y (up), and depth (larger is farther).
local function project(pt, ang, roll)
  local x, y, z = pt[1], pt[2], pt[3]
  if roll ~= 0 then
    -- Rolled onto its side round its length, the side it falls to on the ground.
    local cy, cz = y, z - 2
    y = cy * math.cos(roll) - cz * math.sin(roll)
    z = cy * math.sin(roll) + cz * math.cos(roll) + 2 + math.sin(roll) * 3.5
  end
  local fx, fy = math.cos(ang), math.sin(ang)
  local gx = x * fx - y * fy
  local gy = x * fy + y * fx
  return gx, gy * 0.5 + z * 0.88, gy * 0.88 - z * 0.47
end

-- Draws one frame into the cell at (ox, oy), the image's top left; returns nothing.
local function draw(img, ox, oy, pose, ang)
  local shapes, eyes, roll = wolf(pose)
  local depth, kind, colour, groups = {}, {}, {}, {}
  local cx, cy = 0, 0
  for _, sh in ipairs(shapes) do
    local ax, ay, ad = project(sh.a, ang, roll)
    local bx, by, bd = project(sh.b, ang, roll)
    -- The far side a shade darker: shapes whose middle is farther than the body's.
    local _, _, md = project(lerp(sh.a, sh.b, 0.5), ang, roll)
    local _, _, bodyDepth = project(v(0, 0, 11), ang, roll)
    local far = md > bodyDepth + 1.4 and 1 or 0
    local r = math.max(sh.r1, sh.r2)
    local x0, x1 = math.floor(math.min(ax, bx) - r - 1), math.ceil(math.max(ax, bx) + r + 1)
    local y0, y1 = math.floor(math.min(ay, by) - r - 1), math.ceil(math.max(ay, by) + r + 1)
    local dxs, dys = bx - ax, by - ay
    local len2 = dxs * dxs + dys * dys
    for sy = y0, y1 do
      for sx = x0, x1 do
        local px, py = sx + 0.5, sy + 0.5
        local t = len2 > 0 and math.max(0, math.min(1, ((px - ax) * dxs + (py - ay) * dys) / len2)) or 0
        local qx, qy = ax + dxs * t, ay + dys * t
        local rr = sh.r1 + (sh.r2 - sh.r1) * t
        local ex, ey = px - qx, py - qy
        local d2 = ex * ex + ey * ey
        if d2 <= rr * rr then
          local nz = math.sqrt(math.max(0, rr * rr - d2)) / rr
          local d = ad + (bd - ad) * t - nz * rr
          local key = sx .. "," .. sy
          if depth[key] == nil or d < depth[key] then
            local nx, ny = ex / rr, ey / rr
            local light = nx * LX + ny * LY + nz * LZ
            local ramp, base = sh.ramp, 3
            if ramp == FUR then
              -- The owner's wolf (ArtSource/pixel/ai_exchange/pets/wolf_strip.jpg): a light grey coat, a darker brown-grey
              -- mane and saddle over the shoulders and back, pale cream on the face's mask, the throat, the belly and the
              -- legs, the tail grey with a darker top.
              local pale = sh.kind == "cheek" or (sh.kind == "snout" or sh.kind == "jaw") and ny < 0.5
              pale = pale or (sh.kind == "neck" or sh.kind == "chest" or sh.kind == "torso") and ny < -0.35
              -- The legs pale below the elbow and the hock, their backs a shade darker.
              pale = pale or (sh.kind == "leg" or sh.kind == "paw") and light > -0.3
              local saddle = (sh.kind == "ruff" or sh.kind == "mane" or sh.kind == "neck") and ny > 0.0 or
                (sh.kind == "torso" or sh.kind == "haunch") and ny > 0.45 or (sh.kind == "tail" or sh.kind == "tailtip") and ny > 0.35
              if pale then ramp, base = CREAM, (sh.kind == "leg" or sh.kind == "paw") and 3 or 4
              elseif saddle then ramp, base = SADDLE, 3
              elseif sh.kind == "head" and ny > 0.2 then base = 3
              end
              if sh.kind == "ear" then ramp, base = SADDLE, 2 end
              local streak = ((sx * 3 + sy * 5) % 11)
              if streak == 0 and light > -0.2 then base = base + 1 elseif streak == 5 and light < 0.4 then base = base - 1 end
            elseif sh.kind == "tooth" then
              base = 1
            elseif ramp == DARK or ramp == RED or ramp == GOLDEN then
              base = 2
            end
            local step = light > 0.62 and 1 or light > 0.12 and 0 or light > -0.35 and -1 or -2
            local i = math.max(1, math.min(#ramp, base + step - far))
            depth[key], kind[key], colour[key], groups[key] = d, sh.kind, ramp[i], sh.group
          end
        end
      end
    end
  end
  -- Pixels into the cell (screen y up, so the cell's rows from its bottom), dark lines where a nearer part crosses a
  -- farther one, then the outline round all.
  local solid = {}
  for key, c in pairs(colour) do
    local sx, sy = key:match("(-?%d+),(-?%d+)")
    sx, sy = tonumber(sx), tonumber(sy)
    local ix, iy = PX + sx, CH - 1 - (PY + sy)
    if ix >= 1 and iy >= 1 and ix < CW - 1 and iy < CH - 1 then
      local d = depth[key]
      local crease = false
      for _, n in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
        local nkey = (sx + n[1]) .. "," .. (sy + n[2])
        local nd = depth[nkey]
        if nd ~= nil and nd < d - 1.6 and groups[nkey] ~= groups[key] then crease = true end
      end
      img:drawPixel(ox + ix, oy + iy, crease and OUTLINE or c)
      solid[ix .. "," .. iy] = true
    end
  end
  for key in pairs(solid) do
    local ix, iy = key:match("(-?%d+),(-?%d+)")
    ix, iy = tonumber(ix), tonumber(iy)
    for _, n in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
      local nx, ny = ix + n[1], iy + n[2]
      if not solid[nx .. "," .. ny] and nx >= 0 and ny >= 0 and nx < CW and ny < CH then img:drawPixel(ox + nx, oy + ny, OUTLINE) end
    end
  end
  -- The eyes, where the head in front of them is what is seen there.
  for _, e in ipairs(eyes) do
    local sx, sy, d = project(e, ang, roll)
    sx, sy = math.floor(sx), math.floor(sy)
    local key = sx .. "," .. sy
    if depth[key] and kind[key] == "head" and d < depth[key] + 1.5 then
      img:drawPixel(ox + PX + sx, oy + CH - 1 - (PY + sy), EYE)
    end
  end
end

-- --- The poses -------------------------------------------------------------------------------------------------------

local function stand()
  return { front = { l = { 0, 0 }, r = { 0, 0 } }, hind = { l = { 0, 0 }, r = { 0, 0 } }, head = { 0, 0, 0 }, tail = { 0, 0, 0 } }
end

local animations = {
  { name = "idle", frames = 12, seconds = 2, loop = true, pose = function(t)
    local p = stand()
    local w = math.sin(t * math.pi * 2)
    p.bob = 0.3 * math.sin(t * math.pi * 4)
    p.tail = { 0.8 * math.sin(t * math.pi * 4), 0, 0.3 * math.sin(t * math.pi * 2) }
    p.head = { 0, 0.3 * w, 0.4 * math.sin(t * math.pi * 2 + 1) }
    return p
  end },
  { name = "run", frames = 10, seconds = 0.5, loop = true, pose = function(t)
    local a = t * math.pi * 2
    local p = stand()
    local A = 0.55
    p.front = { l = { A * math.sin(a), math.max(0, math.cos(a)) * 2.6 }, r = { A * math.sin(a + math.pi), math.max(0, math.cos(a + math.pi)) * 2.6 } }
    p.hind = { l = { A * math.sin(a + math.pi), math.max(0, math.cos(a + math.pi)) * 2.2 }, r = { A * math.sin(a), math.max(0, math.cos(a)) * 2.2 } }
    p.bob = 0.7 * math.cos(a * 2)
    p.lean = 0.4 * math.sin(a)
    p.head = { 0.6, -0.8 + 0.4 * math.cos(a * 2), 0 }
    p.tail = { 0.5 * math.sin(a), 1.8, 2.6 }
    p.ears = 0.6
    return p
  end },
  { name = "attack", frames = 12, seconds = 0.917, loop = false, pose = function(t)
    local p = stand()
    -- A crouch to half way, the lunge and bite past it, then back.
    local crouch = t < 0.45 and t / 0.45 or math.max(0, 1 - (t - 0.45) / 0.15)
    local lunge = t < 0.45 and 0 or t < 0.62 and (t - 0.45) / 0.17 or math.max(0, 1 - (t - 0.62) / 0.38)
    p.bob = -1.6 * crouch + 0.8 * lunge
    p.lean = -0.8 * crouch + 1.0 * lunge
    p.dx = -1.2 * crouch + 4 * lunge
    p.front = { l = { -0.3 * crouch + 0.7 * lunge, 1.5 * lunge }, r = { -0.2 * crouch + 0.5 * lunge, 1.2 * lunge } }
    p.hind = { l = { 0.35 * crouch - 0.5 * lunge, 0 }, r = { 0.3 * crouch - 0.45 * lunge, 0 } }
    p.head = { 0.8 * lunge, -1.6 * crouch + 0.6 * lunge, 0 }
    p.jaw = lunge > 0.3 and math.min(1, lunge * 1.4) or 0
    p.ears = 1.0 * crouch
    p.tail = { 0, 1.2, 1.8 }
    return p
  end },
  { name = "hit", frames = 4, seconds = 0.333, loop = false, pose = function(t)
    local p = stand()
    local k = math.sin(t * math.pi)
    p.dx = -1.6 * k
    p.lean = 0.6 * k
    p.head = { -1.0 * k, 1.0 * k, 0 }
    p.front = { l = { 0.3 * k, 0.8 * k }, r = { 0.2 * k, 0.6 * k } }
    p.ears = 1.2 * k
    p.tail = { 0, -0.4, 0 }
    return p
  end },
  { name = "death", frames = 12, seconds = 1.0, loop = false, pose = function(t)
    local p = stand()
    local k = math.min(1, t / 0.6)
    local ease = k * k * (3 - 2 * k)
    p.roll = ease * 1.45
    p.bob = -3.2 * ease
    p.head = { 0.6 * ease, -2.2 * ease, 0 }
    p.front = { l = { 0.6 * ease, 0 }, r = { 0.3 * ease, 0 } }
    p.hind = { l = { -0.5 * ease, 0 }, r = { -0.3 * ease, 0 } }
    p.ears = 1.4 * ease
    p.tail = { 0, 0.6 * ease, 1.0 * ease }
    return p
  end },
}

-- The rows' directions, from the top: s, sw, w, nw, n, ne, e, se, as ground angles (x right, y up the screen).
local angles = { 270, 225, 180, 135, 90, 45, 0, 315 }

local sheet = {}
for _, anim in ipairs(animations) do
  if not only or string.find("," .. only .. ",", "," .. anim.name .. ",", 1, true) then
    local img = Image(CW * anim.frames, CH * #angles, ColorMode.RGB)
    img:clear(rgba(0, 0, 0, 0))
    for row, deg in ipairs(angles) do
      for f = 0, anim.frames - 1 do
        local t = anim.loop and f / anim.frames or (anim.frames > 1 and f / (anim.frames - 1) or 0)
        draw(img, f * CW, (row - 1) * CH, anim.pose(t), math.rad(deg))
      end
    end
    img:saveAs(out .. "/pet_wolf_" .. anim.name .. ".png")
    print("REPORT wrote " .. anim.name)
  end
  sheet[#sheet + 1] = string.format('{"name":"%s","frames":%d,"seconds":%s}', anim.name, anim.frames, anim.seconds)
end
local f = io.open(out .. "/sheet.json", "w")
f:write(string.format('{"cellWidth":%d,"cellHeight":%d,"pivotX":%d,"pivotY":%d,"directions":8,"animations":[%s]}\n', CW, CH, PX, PY,
  table.concat(sheet, ",")))
f:close()
