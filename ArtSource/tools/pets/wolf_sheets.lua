-- The pet wolf's sheets from the owner's strip (2026-11-05, the owner: "base your art on that. Remove the colar"):
-- the eight views strip_frame.lua cut (v1 to v4 the side walk, facing left; v5 three-quarter front facing left; v6
-- three-quarter front facing right; v7 front; v8 back), laid out as the game's sheets: rows s, sw, w, nw, n, ne, e, se.
-- The side walk is the run and the walk (mirrored to face right); the other views step with a bob; the bite lunges the
-- frame forward and back; a hit jerks it back; death tips it over onto its side.
--   aseprite -b --script-param views=<folder> --script-param out=<folder> --script wolf_sheets.lua
local rgba = app.pixelColor.rgba
local dir, out = app.params["views"], app.params["out"]

local function load(i, flip)
  local im = Image { fromFile = dir .. "/v" .. i .. ".png" }
  if not flip then return im end
  local m = Image(im.width, im.height, ColorMode.RGB)
  m:clear(rgba(0, 0, 0, 0))
  for y = 0, im.height - 1 do
    for x = 0, im.width - 1 do m:drawPixel(im.width - 1 - x, y, im:getPixel(x, y)) end
  end
  return m
end

local CW, CH, PX, PY = 88, 60, 44, 4
local side = { load(1), load(2), load(3), load(4) }
local sideR = { load(1, true), load(2, true), load(3, true), load(4, true) }
local views = { s = { load(7) }, sw = { load(5) }, w = side, nw = side, n = { load(8) }, ne = sideR, e = sideR, se = { load(6) } }
-- Which way each row faces on screen (x), for the lunge.
local facing = { s = 0, sw = -1, w = -1, nw = -1, n = 0, ne = 1, e = 1, se = 1 }
local rows = { "s", "sw", "w", "nw", "n", "ne", "e", "se" }

-- A picture into a cell, its bottom PY above the cell's bottom, its middle on the pivot, moved by (dx, dy) (dy up), and
-- tipped (death: 0 standing, 1 lying, squashed onto its side and leaning the way it falls).
local function place(sheet, col, row, im, dx, dy, tip)
  local w, h = im.width, im.height
  local x0 = col * CW + PX - w // 2 + dx
  local bottom = row * CH + CH - PY - dy
  local t = tip or 0
  local nh = math.max(1, math.floor(h * (1 - 0.5 * t)))
  for y = 0, nh - 1 do
    local sy = math.floor(y * h / nh)
    local shift = math.floor((nh - y) / nh * 6 * t)
    for x = 0, w - 1 do
      local c = im:getPixel(x, sy)
      if app.pixelColor.rgbaA(c) > 0 then sheet:drawPixel(x0 + x + shift, bottom - nh + y, c) end
    end
  end
end

local anims = {
  { name = "idle", frames = 4, seconds = 2.0, f = function(r, i) return views[r][1], 0, (i == 2) and 1 or 0 end },
  { name = "run", frames = 8, seconds = 0.6, speed = 2.6, f = function(r, i)
    local v = views[r]
    if #v > 1 then return v[(i % 4) + 1], 0, 0 end
    return v[1], 0, (i % 2 == 1) and 1 or 0
  end },
  { name = "walk", frames = 8, seconds = 1.0, speed = 1.4, f = function(r, i)
    local v = views[r]
    if #v > 1 then return v[(i % 4) + 1], 0, 0 end
    return v[1], 0, (i % 4 == 1 or i % 4 == 3) and 1 or 0
  end },
  { name = "attack", frames = 8, seconds = 0.917, f = function(r, i)
    local lunge = ({ 0, -1, -2, 1, 5, 6, 3, 1 })[i + 1]
    local up = ({ 0, 0, -1, 0, 2, 1, 0, 0 })[i + 1]
    return views[r][1], lunge * facing[r], up
  end },
  { name = "hit", frames = 3, seconds = 0.333, f = function(r, i)
    return views[r][1], -({ 2, 3, 1 })[i + 1] * facing[r], 0
  end },
  { name = "death", frames = 8, seconds = 1.0, f = function(r, i)
    return views[r][1], 0, 0, math.min(1, i / 5)
  end },
}

local list = {}
for _, a in ipairs(anims) do
  local sheet = Image(CW * a.frames, CH * #rows, ColorMode.RGB)
  sheet:clear(rgba(0, 0, 0, 0))
  for ri, r in ipairs(rows) do
    for i = 0, a.frames - 1 do
      local im, dx, dy, tip = a.f(r, i)
      place(sheet, i, ri - 1, im, dx, dy, tip)
    end
  end
  sheet:saveAs(out .. "/pet_wolf_" .. a.name .. ".png")
  list[#list + 1] = string.format('{"name":"%s","frames":%d,"seconds":%s,"speed":%s}', a.name, a.frames, a.seconds, a.speed or 0)
end
local f = io.open(out .. "/sheet.json", "w")
f:write(string.format('{"cellWidth":%d,"cellHeight":%d,"pivotX":%d,"pivotY":%d,"directions":8,"animations":[%s]}\n', CW, CH, PX, PY,
  table.concat(list, ",")))
f:close()
print("REPORT sheets " .. #anims)
