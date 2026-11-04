-- A picture of drawn props side by side on the forest's green, each enlarged by a whole number, for looking them over.
--   aseprite -b --script-param files=<a.png,b.png> --script-param out=<png> [--script-param scale=6] --script montage.lua
local files, out = app.params["files"], app.params["out"]
local scale = tonumber(app.params["scale"] or "6")
local images, w, h = {}, 8, 0
for path in string.gmatch(files, "[^,]+") do
  local image = Image { fromFile = path }
  image:resize(image.width * scale, image.height * scale)
  images[#images + 1] = image
  w = w + image.width + 8
  h = math.max(h, image.height)
end
local sheet = Image(w, h + 16, ColorMode.RGB)
sheet:clear(app.pixelColor.rgba(34, 52, 28, 255))
local x = 8
for _, image in ipairs(images) do
  sheet:drawImage(image, Point(x, 8 + h - image.height))
  x = x + image.width + 8
end
sheet:saveAs(out)
print("REPORT montage " .. out)
