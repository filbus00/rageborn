-- Reduces a picture to pixel art's few colours (2026-11-04, the owner: "The current art style is not cohesive. I want it to
-- be pixel art"; Aseprite built from source, ~/Applications/Aseprite.app): the picture is cut to a palette of its own N
-- most telling colours (Aseprite's colour quantization), each pixel takes the nearest of them with no dithering, and every
-- pixel is made fully clear or fully solid. Written back as RGB.
--   aseprite -b --script-param in=<png> --script-param out=<png> --script-param colors=<N> --script ArtSource/tools/pixel/reduce.lua
local input = app.params["in"]
local output = app.params["out"]
local colors = tonumber(app.params["colors"] or "16")
local sprite = app.open(input)
if not sprite then
  print("REPORT cannot open " .. input)
  return
end
-- Hard alpha first, so the quantization sees only the pixels that show.
local image = sprite.cels[1].image
for it in image:pixels() do
  local c = it()
  local a = app.pixelColor.rgbaA(c)
  if a < 128 then
    it(app.pixelColor.rgba(0, 0, 0, 0))
  elseif a < 255 then
    it(app.pixelColor.rgba(app.pixelColor.rgbaR(c), app.pixelColor.rgbaG(c), app.pixelColor.rgbaB(c), 255))
  end
end
app.command.ColorQuantization { ui = false, withAlpha = false, maxColors = colors }
app.command.ChangePixelFormat { format = "indexed", dithering = "none" }
app.command.ChangePixelFormat { format = "rgb" }
sprite:saveCopyAs(output)
print("REPORT " .. output .. ": " .. colors .. " colours")
