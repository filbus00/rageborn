-- The forest's grass and path as pixel art on the screen's grid (2026-11-05, the owner chose "Move to screen grid"):
-- seamless squares at 27 pixels a unit, read by GroundPainter at the world position (one texture pixel a screen pixel),
-- so they are drawn as seen on screen: tufts of blades standing up, flat pebbles twice as wide as tall, light from the
-- upper left. Everything wraps round the edges.
--   aseprite -b --script-param out=<folder> --script forest_ground.lua
local out = app.params["out"]
local S = 216 -- 8 units
local rgba = app.pixelColor.rgba

local GRASS = { rgba(22, 33, 17, 255), rgba(31, 46, 21, 255), rgba(41, 60, 26, 255), rgba(53, 75, 31, 255),
  rgba(68, 91, 37, 255), rgba(88, 110, 46, 255), rgba(112, 132, 58, 255) }
local EARTH = { rgba(34, 26, 20, 255), rgba(46, 36, 27, 255), rgba(58, 46, 34, 255), rgba(70, 56, 41, 255),
  rgba(84, 68, 50, 255), rgba(102, 84, 62, 255), rgba(126, 106, 80, 255) }
local STONE = { rgba(40, 35, 31, 255), rgba(58, 52, 45, 255), rgba(78, 71, 61, 255), rgba(100, 92, 79, 255), rgba(124, 115, 99, 255) }
local TWIG = { rgba(38, 28, 20, 255), rgba(66, 50, 34, 255), rgba(96, 76, 52, 255) }

-- A seeded random stream.
local function rng(seed)
  local s = seed
  return function(a, b)
    s = (s * 1103515245 + 12345) % 2147483648
    local r = s / 2147483648
    if a then return a + math.floor(r * (b - a + 1)) end
    return r
  end
end

-- Value noise that repeats every S pixels (the lattice step divides S).
local function hash(x, y, seed)
  local h = (x * 374761393 + y * 668265263 + seed * 982451653) % 4294967296
  h = ((h ~ (h >> 13)) * 1274126177) % 4294967296
  h = h ~ (h >> 16)
  return (h & 0xFFFFFF) / 0x1000000
end
local function noise(x, y, step, seed)
  local n = S // step
  local fx, fy = x / step, y / step
  local x0, y0 = math.floor(fx), math.floor(fy)
  local tx, ty = fx - x0, fy - y0
  tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
  local function at(i, j) return hash(i % n, j % n, seed) end
  local a = at(x0, y0) + (at(x0 + 1, y0) - at(x0, y0)) * tx
  local b = at(x0, y0 + 1) + (at(x0 + 1, y0 + 1) - at(x0, y0 + 1)) * tx
  return a + (b - a) * ty
end

local function new()
  local image = Image(S, S, ColorMode.RGB)
  return image
end
local function put(image, x, y, c) image:drawPixel(x % S, y % S, c) end
local function get(image, x, y) return image:getPixel(x % S, y % S) end

-- The grass: a ground of mottled dark green, then tufts of blades, each a few strokes leaning out from a root, the
-- strokes lit on their upper left and darkest at the root, with a shadow pixel under the root.
local function grass()
  local image = new()
  for y = 0, S - 1 do
    for x = 0, S - 1 do
      local n = noise(x, y, 24, 3) * 0.6 + noise(x, y, 8, 5) * 0.3 + hash(x, y, 7) * 0.1
      local tone = n < 0.36 and 2 or n < 0.6 and 3 or 4
      put(image, x, y, GRASS[tone])
    end
  end
  local r = rng(11)
  local tufts = {}
  -- Tufts in a jittered grid, thicker where the noise says the grass is lush.
  for gy = 0, S - 1, 5 do
    for gx = 0, S - 1, 6 do
      local x, y = gx + r(0, 5), gy + r(0, 4)
      local lush = noise(x, y, 24, 3)
      if r() < (lush - 0.3) * 1.3 then tufts[#tufts + 1] = { x, y, lush } end
    end
  end
  -- Drawn top of the screen first, so lower tufts stand in front.
  table.sort(tufts, function(a, b) return a[2] < b[2] end)
  for _, t in ipairs(tufts) do
    local x, y, lush = t[1], t[2], t[3]
    local blades = r(2, 4)
    local tall = r(2, 3) + (lush > 0.6 and 1 or 0)
    put(image, x, y + 1, GRASS[1])
    put(image, x + 1, y + 1, GRASS[2])
    for b = 1, blades do
      local lean = ({ -1, 1, 0, -1, 1 })[b] + (r() < 0.3 and (r() < 0.5 and -1 or 1) or 0)
      local h = tall - (b > 2 and 1 or 0)
      local bx = x + (b == 2 and 1 or 0)
      for k = 0, h do
        local px = bx + math.floor(lean * k / math.max(1, h) + (lean < 0 and 0 or 0.5) * (k / h))
        local tone = k == 0 and 3 or k == h and (lean <= 0 and 6 or 5) or (lean <= 0 and 5 or 4)
        if lush < 0.5 and tone > 4 then tone = tone - 1 end
        put(image, px, y - k, GRASS[tone])
      end
    end
  end
  -- A few darker hollows and lighter specks, single pixels, to break the tufts' rhythm.
  for _ = 1, 260 do
    local x, y = r(0, S - 1), r(0, S - 1)
    put(image, x, y, GRASS[r() < 0.6 and 2 or 5])
  end
  return image
end

-- The path: trodden earth in soft patches, darker wheel ruts of grain, pebbles (flat, twice as wide as tall, lit top
-- left, a shadow under them to the right), crumbs of dark and light grit, and now and then a twig.
local function path()
  local image = new()
  for y = 0, S - 1 do
    for x = 0, S - 1 do
      local n = noise(x, y, 27, 13) * 0.55 + noise(x, y, 9, 17) * 0.3 + hash(x, y, 19) * 0.15
      local tone = n < 0.34 and 3 or n < 0.62 and 4 or 5
      put(image, x, y, EARTH[tone])
    end
  end
  local r = rng(23)
  -- Grit.
  for _ = 1, 600 do
    local x, y = r(0, S - 1), r(0, S - 1)
    local dark = r() < 0.6
    put(image, x, y, dark and EARTH[2] or EARTH[6])
    if dark and r() < 0.4 then put(image, x + 1, y, EARTH[3]) end
  end
  -- Small cracks: short runs mostly across, a pixel's step now and then.
  for _ = 1, 22 do
    local x, y = r(0, S - 1), r(0, S - 1)
    for _ = 1, r(4, 9) do
      put(image, x, y, EARTH[2])
      x = x + 1
      if r() < 0.3 then y = y + (r() < 0.5 and -1 or 1) end
    end
  end
  -- Twigs: a dark line with a lit top edge.
  for _ = 1, 7 do
    local x, y = r(0, S - 1), r(0, S - 1)
    local len, dy = r(5, 9), (r() < 0.5 and -1 or 1)
    for k = 0, len do
      local py = y + math.floor(k * dy / 3)
      put(image, x + k, py + 1, EARTH[2])
      put(image, x + k, py, TWIG[2])
      if k % 3 == 1 then put(image, x + k, py - 1, TWIG[3]) end
    end
  end
  -- Pebbles: half of them tiny, a few larger.
  for _ = 1, 55 do
    local x, y = r(0, S - 1), r(0, S - 1)
    local w = r() < 0.55 and r(2, 3) or r(4, 6)
    local h = math.max(1, w // 2)
    if w <= 2 then
      put(image, x, y, STONE[4]); put(image, x + 1, y, STONE[3]); put(image, x + 1, y + 1, EARTH[1])
    else
      for j = 0, h do
        for i = 0, w do
          local u, v = (i + 0.5) / (w + 1) * 2 - 1, (j + 0.5) / (h + 1) * 2 - 1
          if u * u + v * v <= 1.05 then
            local lit = -u * 0.6 - v * 0.8
            local tone = lit > 0.45 and 5 or lit > -0.1 and 4 or lit > -0.6 and 3 or 2
            put(image, x + i, y + j, STONE[tone])
          end
        end
      end
      -- The shadow it casts down to the right, and a dark outline under it.
      for i = 1, w + 1 do
        if get(image, x + i, y + h + 1) ~= STONE[2] then put(image, x + i, y + h + 1, EARTH[1]) end
      end
      put(image, x + w + 1, y + h, EARTH[2])
    end
  end
  return image
end

grass():saveAs(out .. "/forest_grass.png")
path():saveAs(out .. "/forest_path.png")
print("REPORT ground " .. out)
