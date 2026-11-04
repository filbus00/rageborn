-- Tools for drawing the game's props as pixel art in Aseprite (2026-11-04, the owner: "redraw the art at the new scale"):
-- 27 pixels a world unit, the rendered scale, so one pixel here is one on screen. A prop's sprite stands on its cell's
-- middle a quarter unit up from its bottom (PixelArtImporter), so its ground line is BASE rows above the bottom row.
-- The game's camera looks down at the ground plane at 2:1: a step back into the scene is two pixels across, one up.
-- Shapes are filled with a shade function (x, y) -> colour, outlined in the palette's near-black, and stand on a
-- dithered shadow. Colours are the game's palette (Assets/_Project/Scripts/Runtime/Core/PixelArt.cs).
local kit = {}
local rgba = app.pixelColor.rgba

kit.BASE = 7

kit.OUTLINE = rgba(14, 10, 10, 255)
kit.STONE = { rgba(30, 26, 26, 255), rgba(50, 46, 45, 255), rgba(74, 69, 66, 255), rgba(102, 96, 91, 255), rgba(134, 128, 120, 255), rgba(170, 164, 154, 255), rgba(214, 208, 196, 255) }
kit.WOOD = { rgba(28, 18, 13, 255), rgba(48, 31, 21, 255), rgba(72, 47, 30, 255), rgba(100, 68, 43, 255), rgba(132, 94, 60, 255), rgba(168, 128, 86, 255) }
kit.RED = { rgba(38, 9, 9, 255), rgba(70, 15, 14, 255), rgba(108, 24, 20, 255), rgba(150, 38, 28, 255), rgba(192, 62, 40, 255) }
kit.SKIN = { rgba(62, 40, 32, 255), rgba(102, 68, 52, 255), rgba(144, 102, 78, 255), rgba(186, 142, 110, 255), rgba(222, 186, 150, 255) }
kit.MOSS = { rgba(18, 26, 16, 255), rgba(34, 48, 28, 255), rgba(54, 72, 40, 255), rgba(82, 100, 58, 255), rgba(118, 132, 82, 255) }
kit.COLD = { rgba(20, 24, 32, 255), rgba(38, 46, 58, 255), rgba(62, 74, 90, 255), rgba(94, 108, 124, 255), rgba(136, 150, 164, 255), rgba(184, 196, 204, 255) }
kit.EMBER = { rgba(88, 40, 12, 255), rgba(148, 70, 18, 255), rgba(204, 110, 30, 255), rgba(236, 160, 60, 255), rgba(252, 214, 120, 255) }
kit.BONE = { rgba(150, 142, 118, 255), rgba(196, 188, 162, 255), rgba(232, 224, 198, 255) }
kit.BURLAP = { rgba(64, 46, 28, 255), rgba(104, 78, 46, 255), rgba(144, 112, 68, 255), rgba(184, 150, 96, 255), rgba(218, 188, 132, 255) }
kit.SILK = { rgba(70, 74, 80, 255), rgba(122, 126, 126, 255), rgba(172, 174, 166, 255), rgba(212, 212, 200, 255), rgba(242, 240, 228, 255) }
kit.CREAM = { rgba(84, 72, 52, 255), rgba(142, 128, 92, 255), rgba(194, 182, 138, 255), rgba(228, 220, 180, 255), rgba(250, 246, 222, 255) }
kit.EARTH = { rgba(36, 31, 25, 255), rgba(52, 46, 36, 255), rgba(68, 61, 47, 255), rgba(86, 77, 59, 255), rgba(106, 96, 74, 255), rgba(130, 118, 92, 255), rgba(156, 144, 114, 255) }

-- A prop drawn on a w x h picture (its coordinates), on a canvas with PAD pixels of room above and to each side, so
-- nothing that reaches past the picture is cut off; save() trims the room back to the art (2026-11-04, the owner: "you
-- seem to have cropped the tops of a couple of the props").
kit.PAD = 24

function kit.new(w, h)
  local P = kit.PAD
  local c = { W = w, H = h, ox = P, oy = P, CW = w + 2 * P, CH = h + P, solid = {} }
  c.sprite = Sprite(c.CW, c.CH, ColorMode.RGB)
  c.image = c.sprite.cels[1].image
  c.ground = h - 1 - kit.BASE
  return setmetatable(c, { __index = kit })
end

function kit:key(x, y) return y * self.CW + x end

-- A pixel; `outlined` false keeps it out of the outline (ropes, strands, sparks).
function kit:px(x, y, c, outlined)
  x, y = math.floor(x) + self.ox, math.floor(y) + self.oy
  if x < 0 or y < 0 or x >= self.CW or y >= self.CH then return end
  self.image:drawPixel(x, y, c)
  if outlined ~= false then self.solid[self:key(x, y)] = true end
end

-- A pixel of shadow: drawn, never outlined, and drawn over by everything after it.
function kit:dot(x, y, c) self:px(x, y, c, false) end

function kit:get(x, y) return self.solid[self:key(math.floor(x) + self.ox, math.floor(y) + self.oy)] end

local function inside(poly, x, y)
  local n, c, j = #poly // 2, false, #poly // 2
  for i = 1, n do
    local xi, yi, xj, yj = poly[2 * i - 1], poly[2 * i], poly[2 * j - 1], poly[2 * j]
    if ((yi > y) ~= (yj > y)) and (x < (xj - xi) * (y - yi) / (yj - yi) + xi) then c = not c end
    j = i
  end
  return c
end

-- A polygon {x1, y1, x2, y2, ...}, each pixel coloured by shade(x, y) (or one colour).
function kit:poly(points, shade, outlined)
  local f = type(shade) == "function" and shade or function() return shade end
  for y = -self.oy, self.H - 1 do
    for x = -self.ox, self.W + self.ox - 1 do
      if inside(points, x + 0.5, y + 0.5) then
        local c = f(x, y)
        if c then self:px(x, y, c, outlined) end
      end
    end
  end
end

-- An ellipse round (cx, cy), shade(x, y, u, v) with u, v from -1 to 1 across it.
function kit:ellipse(cx, cy, rx, ry, shade, outlined)
  local f = type(shade) == "function" and shade or function() return shade end
  for y = math.floor(cy - ry), math.ceil(cy + ry) do
    for x = math.floor(cx - rx), math.ceil(cx + rx) do
      local u, v = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
      if u * u + v * v <= 1 then
        local c = f(x, y, u, v)
        if c then self:px(x, y, c, outlined) end
      end
    end
  end
end

function kit:line(x0, y0, x1, y1, c, outlined)
  x0, y0, x1, y1 = math.floor(x0), math.floor(y0), math.floor(x1), math.floor(y1)
  local dx, dy = math.abs(x1 - x0), -math.abs(y1 - y0)
  local sx, sy = x0 < x1 and 1 or -1, y0 < y1 and 1 or -1
  local err = dx + dy
  while true do
    self:px(x0, y0, c, outlined)
    if x0 == x1 and y0 == y1 then break end
    local e2 = 2 * err
    if e2 >= dy then err = err + dy; x0 = x0 + sx end
    if e2 <= dx then err = err + dx; y0 = y0 + sy end
  end
end

-- A thick line: a run of small discs.
function kit:stroke(x0, y0, x1, y1, r, shade, outlined)
  local n = math.max(1, math.ceil(math.max(math.abs(x1 - x0), math.abs(y1 - y0))))
  for i = 0, n do
    local t = i / n
    self:ellipse(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, r, r, shade, outlined)
  end
end

-- A box on the ground plane, its near corner at (x, y): `a` pixels of depth back to the left, `b` back to the right,
-- `h` tall. Faces: top lit, left in shade, right half lit. shade(face, x, y) or a table {top, left, right}.
function kit:box(x, y, a, b, h, shade)
  local f = type(shade) == "function" and shade or function(face) return shade[face] end
  local L = { x - 2 * a, y - a }
  local R = { x + 2 * b, y - b }
  local B = { x - 2 * a + 2 * b, y - a - b }
  self:poly({ x, y, L[1], L[2], L[1], L[2] - h, x, y - h }, function(px, py) return f("left", px, py) end)
  self:poly({ x, y, R[1], R[2], R[1], R[2] - h, x, y - h }, function(px, py) return f("right", px, py) end)
  self:poly({ x, y - h, L[1], L[2] - h, B[1], B[2] - h, R[1], R[2] - h }, function(px, py) return f("top", px, py) end)
end

-- The ground's shadow: a dithered ellipse (hard pixels only, so a checker of the outline colour), drawn first.
function kit:shadow(cx, cy, rx, ry)
  for y = math.floor(cy - ry), math.ceil(cy + ry) do
    for x = math.floor(cx - rx), math.ceil(cx + rx) do
      local u, v = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
      if u * u + v * v <= 1 and (x + y) % 2 == 0 then self:dot(x, y, kit.OUTLINE) end
    end
  end
end

-- The outline: every pixel beside the shape that is not part of it takes the near-black (canvas pixels).
function kit:outline()
  local edge = {}
  for y = 0, self.CH - 1 do
    for x = 0, self.CW - 1 do
      if not self.solid[self:key(x, y)] then
        for _, d in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
          local nx, ny = x + d[1], y + d[2]
          if nx >= 0 and ny >= 0 and nx < self.CW and ny < self.CH and self.solid[self:key(nx, ny)] then
            edge[#edge + 1] = { x, y }
            break
          end
        end
      end
    end
  end
  for _, p in ipairs(edge) do self.image:drawPixel(p[1], p[2], kit.OUTLINE) end
end

-- Saves the art trimmed to what was drawn: the top down to its first pixel, the sides by the same amount each so its
-- middle stays over the cell's middle (the importer's pivot), the bottom kept so the ground line stays BASE rows up.
function kit:save(folder, name)
  local top, left, right = self.CH, self.CW, -1
  for y = 0, self.CH - 1 do
    for x = 0, self.CW - 1 do
      if app.pixelColor.rgbaA(self.image:getPixel(x, y)) > 0 then
        if y < top then top = y end
        if x < left then left = x end
        if x > right then right = x end
      end
    end
  end
  local side = math.min(left, self.CW - 1 - right)
  self.sprite:crop(side, top, self.CW - 2 * side, self.CH - top)
  self.sprite:saveAs(folder .. "/" .. name .. ".aseprite")
  self.sprite:saveCopyAs(folder .. "/" .. name .. ".png")
  print("REPORT wrote " .. name .. " " .. self.sprite.width .. " x " .. self.sprite.height)
  self.sprite:close()
end

-- A small deterministic random, so a prop draws the same every time.
function kit.random(seed)
  local s = seed
  return function(a, b)
    s = (s * 1103515245 + 12345) % 2147483648
    local r = s / 2147483648
    if a then return a + math.floor(r * (b - a + 1)) end
    return r
  end
end

return kit
