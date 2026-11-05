-- The company menu's pixel art (2026-11-05, the owner's two concept pictures, ArtSource/ui/concepts): frames to be
-- sliced (their border in the name, _b<n>), the backdrop tile, rules and ornaments, and icons, in the concept's dark
-- green and old gold. Drawn at one pixel a menu pixel; the menu shows them at a whole number of screen pixels each.
--   aseprite -b --script-param out=<folder> --script menu_art.lua
local out = app.params["out"]
local rgba = app.pixelColor.rgba

local C = {
  ["."] = rgba(0, 0, 0, 0),
  o = rgba(8, 12, 9, 255),       -- outline, near black green
  k = rgba(14, 22, 17, 255),     -- darkest fill
  f = rgba(20, 32, 24, 255),     -- panel fill
  F = rgba(27, 42, 31, 255),     -- lighter green fill
  m = rgba(36, 56, 40, 255),     -- moss
  d = rgba(92, 68, 26, 255),     -- dark gold
  g = rgba(168, 128, 52, 255),   -- gold
  G = rgba(214, 170, 82, 255),   -- light gold
  W = rgba(246, 222, 150, 255),  -- gold highlight
  r = rgba(70, 14, 16, 255),     -- dark red
  R = rgba(112, 22, 24, 255),    -- red
  E = rgba(170, 40, 36, 255),    -- bright red
  s = rgba(60, 62, 64, 255),     -- dark steel
  S = rgba(120, 124, 126, 255),  -- steel
  T = rgba(184, 186, 182, 255),  -- light steel
  b = rgba(70, 44, 26, 255),     -- dark leather
  B = rgba(120, 78, 42, 255),    -- leather
  L = rgba(164, 112, 62, 255),   -- light leather
  n = rgba(40, 60, 120, 255),    -- dark blue
  N = rgba(90, 120, 190, 255),   -- blue
  v = rgba(70, 74, 72, 255),     -- stone dark
  V = rgba(118, 122, 116, 255),  -- stone
  e = rgba(64, 120, 70, 255),    -- green gem
}

local function image(w, h)
  local im = Image(w, h, ColorMode.RGB)
  im:clear(C["."])
  return im
end

local function save(im, name)
  im:saveAs(out .. "/" .. name .. ".png")
  print("REPORT wrote " .. name .. " " .. im.width .. " x " .. im.height)
end

-- A picture from rows of colour letters.
local function glyph(name, rows)
  local im = image(#rows[1], #rows)
  for y, row in ipairs(rows) do
    for x = 1, #row do
      local c = C[row:sub(x, x)]
      if c then im:drawPixel(x - 1, y - 1, c) end
    end
  end
  save(im, name)
end

-- A frame to be sliced: rings from the outside in, each a colour (or a pair: top-left lit, bottom-right dark), then the
-- fill; corner ornaments optional.
local function frame(name, size, rings, fill, corner)
  local im = image(size, size)
  for y = 0, size - 1 do
    for x = 0, size - 1 do
      local d = math.min(x, y, size - 1 - x, size - 1 - y)
      local ring = rings[d + 1]
      local c = fill
      if ring then
        if type(ring) == "table" then
          c = (x - d <= 0 or y - d <= 0) and ring[1] or ((size - 1 - x - d <= 0 or size - 1 - y - d <= 0) and ring[2] or ring[1])
          if (x == d or y == d) then c = ring[1] elseif (x == size - 1 - d or y == size - 1 - d) then c = ring[2] end
        else
          c = ring
        end
      end
      im:drawPixel(x, y, C[c] or c)
    end
  end
  if corner then corner(im, size) end
  save(im, name .. "_b" .. #rings)
end

-- Little gold diamonds on a frame's corners, over its gold line.
local function diamonds(at)
  return function(im, size)
    for _, p in ipairs({ { at, at }, { size - 1 - at, at }, { at, size - 1 - at }, { size - 1 - at, size - 1 - at } }) do
      local x, y = p[1], p[2]
      im:drawPixel(x, y, C.W)
      for _, q in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
        local px, py = x + q[1], y + q[2]
        if px >= 0 and py >= 0 and px < size and py < size then im:drawPixel(px, py, C.G) end
      end
    end
  end
end

-- --- Frames -------------------------------------------------------------------------------------------------------

-- The big panels: an outline, a gold line lit on its upper left, a dark gap, a thin dark gold line, the fill.
frame("frame_panel", 24, { "o", { "G", "g" }, "o", "d" }, "f", diamonds(1))
-- The cards in a list.
frame("frame_card", 16, { "o", { "g", "d" }, "o" }, "f")
-- A box inside a panel (the grave list's entries).
frame("frame_inset", 16, { "o", "d", "k" }, "k")
-- Tabs: plain, and the chosen one red with a brighter line.
frame("frame_tab", 16, { "o", { "g", "d" }, "o" }, "F")
frame("frame_tab_on", 16, { "o", { "W", "G" }, "o" }, "r")
-- Buttons: a thick gold rim, lit above, and a green face lit along its top.
frame("frame_button", 20, { "o", { "W", "g" }, { "G", "d" }, "o", { "m", "F" } }, "F")
-- The title plaque and the portrait frame: double gold lines.
frame("frame_plaque", 24, { "o", { "G", "g" }, "o", { "g", "d" }, "o" }, "k", diamonds(1))
frame("frame_portrait", 16, { "o", { "G", "g" }, "o" }, "k", diamonds(1))
-- The whole screen's edge.
frame("frame_screen", 24, { "o", "d", { "G", "g" }, "o" }, ".", diamonds(2))

-- --- The backdrop: dark green, mottled, a tile ------------------------------------------------------------------------

do
  local S = 48
  local im = image(S, S)
  local seed = 7
  local function rnd()
    seed = (seed * 1103515245 + 12345) % 2147483648
    return seed / 2147483648
  end
  for y = 0, S - 1 do
    for x = 0, S - 1 do
      local n = math.sin(x * 0.39 + y * 0.13) + math.sin(y * 0.41 - x * 0.17) + math.sin((x + y) * 0.26)
      im:drawPixel(x, y, (n > 1.2 and C.f) or (n < -1.4 and C.o) or C.k)
    end
  end
  for _ = 1, 40 do
    local x, y = math.floor(rnd() * S), math.floor(rnd() * S)
    im:drawPixel(x, y, rnd() < 0.5 and C.F or C.o)
  end
  save(im, "backdrop")
end

-- --- Rules and ornaments ----------------------------------------------------------------------------------------------

glyph("rule", { "g" })
glyph("rule_dim", { "d" })
glyph("rule_diamond", { "..G..", ".GWG.", "GWgWG", ".GgG.", "..G.." })
glyph("dot", { "G" })
-- The arrows each side of the title, pointing in.
glyph("title_arrow", {
  "......W.........",
  "......GW........",
  "......gGW.......",
  "......dgGW......",
  "GGGGGGGGGGW.....",
  "WWWWWWWWWWGW....",
  "ggggggggggG.....",
  "......dgG.......",
  "......dg........",
  "......d.........",
})
-- The little feathered arrows round the subtitle.
glyph("small_arrow", {
  "g.g.....G..",
  ".gGGGGGGWG.",
  "g.g.....G..",
})
glyph("arrow_emblem", {
  "........W........",
  ".......WGG.......",
  "......WGGgg......",
  ".....WG.G.gg.....",
  "....WG..G..gg....",
  "...WG...G...gd...",
  "..WG....G....gd..",
  ".GG.....G.....dd.",
  "........G........",
  "........G........",
  "........G........",
  "........G........",
  "........G........",
  ".......GgG.......",
  "......G.g.d......",
  ".....G..g..d.....",
  "........g........",
})

-- --- Icons ------------------------------------------------------------------------------------------------------------

glyph("icon_coin", {
  "..ogggo..",
  ".oGWGGgo.",
  "oGWGddGgo",
  "oGGd.dGgo",
  "oGGd.dGgo",
  "oGGGdGGgo",
  "ogGGGGgdo",
  ".oggggdo.",
  "..ooooo..",
})
glyph("icon_skull", {
  "..ooooo..",
  ".oEEEEEo.",
  "oEEEEEEEo",
  "oEooEooEo",
  "oEooEooEo",
  "oEEEoEEEo",
  ".oEEEEEo.",
  "..oEoEo..",
  "...ooo...",
})
glyph("icon_scroll", {
  ".oooooooo.",
  "oGWWWWWGgo",
  "ogoooooodo",
  ".oGWWWWgo.",
  ".oGgdgdgo.",
  ".oGWWWWgo.",
  ".oGgdgdgo.",
  "oGWWWWWGgo",
  "ogoooooodo",
  ".oooooooo.",
})
glyph("icon_helm", {
  "...oooo...",
  "..oGWGgo..",
  ".oGWGGggo.",
  ".oGGGGgdo.",
  "oGGooooggo",
  "oGGo..oggo",
  "oGGoGgogdo",
  "oGGoGgogdo",
  ".oGoGgodo.",
  "..oo..oo..",
})
glyph("icon_hand", {
  "..o.o.o...",
  ".oGoGoGo..",
  ".oGoGoGo.o",
  ".oGoGoGooG",
  ".oGGGGGoGo",
  "oGGGWGGGgo",
  "oGGGGGGgo.",
  ".oGGGGgdo.",
  "..oggggo..",
  "...oooo...",
})
glyph("icon_eye", {
  "...oooooo...",
  ".ooNNNNNNoo.",
  "oNNooWWooNNo",
  "NNoWNnnNWoNN",
  "oNNooNNooNNo",
  ".ooNNNNNNoo.",
  "...oooooo...",
})
glyph("icon_shield", {
  "oooooooooo",
  "oGWWGGGGgo",
  "oGoGoGoGdo",
  "oGoGoGoGdo",
  "oGGGGGGGdo",
  "oGoGoGoGdo",
  ".oGoGoGdo.",
  ".oGGGGgdo.",
  "..oGgddo..",
  "...oooo...",
})
glyph("icon_hammer", {
  ".oooooo...",
  "oTTTTTSo..",
  "oSSSSSso..",
  ".oooBoo...",
  "....oBo...",
  "....oBo...",
  "....oBo...",
  "....oBo...",
  "....obo...",
  ".....o....",
})
glyph("icon_key", {
  ".oooo.....",
  "oGWGgo....",
  "oGooGo....",
  "oGWGgoooo.",
  ".oooggGGgo",
  "......odgo",
  "......oo..",
})
glyph("icon_paw", {
  ".oo..oo..",
  "oLLooLLo.",
  "oLLooLLoo",
  ".oo..ooLo",
  "oo.ooo.oo",
  "oLoLLLooo",
  ".oLLLLLo.",
  ".oLLLLLo.",
  "..ooooo..",
})
glyph("icon_lock", {
  "..oooo..",
  ".oSoosSo",
  ".oSo.oSo",
  "oooooooo",
  "oGWGGGgo",
  "oGGooGgo",
  "oGGooGdo",
  "oGGGGgdo",
  "oooooooo",
})
glyph("icon_grave", {
  "...oooooo...",
  "..oVVVVVVo..",
  ".oVVVVVVvvo.",
  ".oVVooooVvo.",
  ".oVVVVVVVvo.",
  ".oVVooooVvo.",
  ".oVVVVVVVvo.",
  ".oVVVVVVVvo.",
  ".oVVVVVVVvo.",
  ".oVVVVVVVvo.",
  "oooooooooooo",
  "ovvvvvvvvvvo",
  "oooooooooooo",
})
glyph("icon_bag", {
  "......oooooo..........",
  ".....oBLLLLBo.........",
  "....oBo....oBo........",
  "...oBo......oBo.......",
  "..ooooooooooooooo.....",
  ".oLLLLLLLLLLLLLLLo....",
  "oLLLLLLLLLLLLLLLLBo...",
  "oLBBBBBBooBBBBBBBBo...",
  "oLBBBBBoGGoBBBBBBbo...",
  "oLBBBBBoGgoBBBBBBbo...",
  "oLBBBBBBooBBBBBBBbo...",
  "oLBBBBBBBBBBBBBBBbo...",
  "oLBBBBBBBBBBBBBBBbo...",
  "oBBBBBBBBBBBBBBBBbo...",
  "oBBBBBBBBBBBBBBBbbo...",
  ".obbbbbbbbbbbbbbbo....",
  "..ooooooooooooooo.....",
})
glyph("icon_gear", {
  "......ooo......",
  "......oTo......",
  "..oo.oTTSo.oo..",
  "..oTooTTSooSo..",
  "...oTTTTTSSo...",
  ".ooTTSoooSSSoo.",
  "oTTTSo...oSSSSo",
  "oTTTSo...oSSssO",
  ".ooSSSoooSSsoo.",
  "...oSSSSSSso...",
  "..oSooSSsoosO..",
  "..oo.oSso.oo...",
  "......oso......",
  "......ooo......",
})
-- The keepers' emblems on the right of their cards.
glyph("emblem_sword", {
  ".......W.......",
  "......WGg......",
  "......GGd......",
  ".W....GGd....g.",
  ".GW...GGd...gd.",
  "..GW..GGd..gd..",
  "...GWGGGddgd...",
  "....GGGGGgd....",
  ".....GGGgd.....",
  "......GGd......",
  "......GGd......",
  "......GGd......",
  "......GGd......",
  ".....GGGgd.....",
  "......GGd......",
  ".......g.......",
})
glyph("emblem_goblet", {
  "..oooooooooo..",
  ".oWGGGGGGGGgo.",
  ".oGeeeeeeeegdo",
  ".oGGeeeeeegdo.",
  "..oGGGGGGgdo..",
  "...oGGGGgdo...",
  "....ooGgoo....",
  "......Go......",
  "......Go......",
  ".....oGgo.....",
  "...ooGGggoo...",
  "..oGGGGGgddo..",
  "..oooooooooo..",
})
glyph("emblem_feather", {
  "............WG",
  "..........WGGg",
  "........WGGgd.",
  ".......WGGgd..",
  "......WGGgd...",
  ".....WGGgd....",
  "....WGGgd.....",
  "...WGGgd......",
  "...GGgd.......",
  "..GGgd........",
  "..Ggd.........",
  ".Gd...........",
  "G.............",
})
glyph("emblem_compass", {
  ".......W.......",
  "......GWG......",
  ".....G.g.G.....",
  "....G..g..G....",
  "...G...g...G...",
  "..G..W.g.W..G..",
  ".G....WgW....G.",
  "WgggggggggggggW",
  ".G....WgW....G.",
  "..G..W.g.W..G..",
  "...G...g...G...",
  "....G..g..G....",
  ".....G.g.G.....",
  "......GWG......",
  ".......W.......",
})
glyph("emblem_anvil", {
  "ooooooooooooo..",
  "oTTTTTTTTTTTSoo",
  "oSSSSSSSSSSSsSo",
  ".ooooSSSSSsooo.",
  "....oSSSSso....",
  "....oSSSsso....",
  "...oSSSSSsso...",
  "..oSSSSSSSsso..",
  "..ooooooooooo..",
})
glyph("emblem_chest", {
  ".oooooooooooo.",
  "oLLLLLLLLLLLBo",
  "oBBBBBBBBBBBbo",
  "ooooooGGoooooo",
  "oLBBBBGgBBBBbo",
  "oBBBBBBBBBBBbo",
  "oBBBBBBBBBBBbo",
  "obbbbbbbbbbbbo",
  "oooooooooooooo",
})
glyph("emblem_paw", {
  "..GG...GG...",
  ".GWGg.GWGg..",
  ".GGgd.GGgd..",
  "..gd...gd.G.",
  "GG.......GWg",
  "GWg..GG..Ggd",
  "Ggd.GWGg..d.",
  ".d.GGGGGg...",
  "...GGGGGgd..",
  "...gGGGggd..",
  "....dddd....",
})
