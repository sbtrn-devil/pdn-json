# PDN-JSON ("PDN via JSON")
[Paint.NET](https://getpaint.net/) (PDN) is a nice freeware image editor, which combines advantages of simple editors like Paint (comprehensive drawing tools, per-pixel editing capabilities) with advanced capabilities of more professional, yet much harder to handle editors like GIMP or Photoshop (layers, effects, extensibility with plugins). Over the years PDN gained acceptance and grown into a powerful ecosystem with a strong community and multitudes of plugins for all sorts of needs.

Unfortunately, the editor suffers an inherent defect that keeps it from being a tool of choice for things like sprite/tile editing. PDN's save file format (.pdn) is closed and undocumented, therefore it can not be oprerated by external tools and embedded into automated toolchains.

Saving the work in more "toolable" image formats (.png etc.) is a very poor workaround, because none of the formats available in PDN ecosystem supports layers, and therefore can not back a proper PDN workspace. The exceptions are more than few. In fact the only one is known to me ([plugin for Photoshop .psd files](https://www.psdplugin.com/)), and the .psd format is extremely wasteful, making it effectively unusable for the purpose.

Hence the motivation is to improvise an image format that PDN could handle, which would meet the following requirements:
- support multiple layers, to the extent sufficient to be a workspace replacement for .pdn,
- have a reasonable storage density,
- be easy readable and writable for custom tooling (by which I primarily, but not solely, mean Javascript based environments, like browsers and node.js).

And so, PDN-JSON (.pdn-json extension) comes onto the stage.

## The PDN-JSON format

The format is simple:

```js
// it is an UTF-8 encoded JSON file (with no comments of course)
{
	// array of strings, reserved to list used extensions, in case the format ever needs any;
	// as of now, should be empty
	"features": [],
	// document width, in pixels
	"width": int,
	// document height, in pixels
	"height": int,
	// array of layers, ordered from bottommost to topmost
	"layers": [
		{
			// layer name
			"name": "<arbitrary string>",
			// layer blend mode, one of PDN's blend modes (see the list below) -
			// in most cases it is "Normal", unrecognized/missing mode also defaults to normal
			"blendMode": "<blendmode>",
			// The valid modes are: "Normal", "Multiply", "Additive", "ColorBurn",
			// "ColorDodge", "Reflect", "Glow", "Overlay", "Difference", "Negation",
			// "Lighten", "Darken", "Screen", "Xor"

			// layer width, in pixels (normally matches document width)
			"width": int,
			// layer height, in pixels (normally matches document height)
			"width": int,
			// MIME type of the image used for storing this layer (see "base64" below)
			// the plugin saves to "image/png", theoretically it may be "image/jpeg" etc.
			"mimeType": "<mimetype>",
			// the base64 encoded bytes of the image holding the layer pixel data
			// the assumption is, when B64-decoded to a file, it becomes a valid image file of type
			// given by "mimeType" (e. g. a .PNG file)
			"base64": "<base64-encoded-blob>"

			// actually the width, height, and mimeType are somewhat redundant - the type and size
			// of the image can be deducted from the bytes alone; nevertheless, these are stored
			// for tools convenience, as well as for more accurate reflection of PDN data model
		},
		... // other layers
	]
}
```

In average the .pdn-json written out by the plugin is ~1.7 times larger than matching .pdn, it in fact is quite compact. Externally generated files can be compacted even further by using more compact image formats (image/jpeg etc) for layers or putting them through image optimization services like [TinyPNG](https://tinypng.com/).

## The PDN plugin

The plugin, as expected, enables PDN to save and load .pdn-json files. It comes in 2 versions:
- *PdnJsonFileType.dll* - for PDN 4.1.x and later (5.0.x+ untested!)
- *PdnJsonFileTypeOld.dll* - for PDN versions up to 4.2.x and earlier

In either case, check out or download the repo and pick the .dll for the appropriate version of your PDN, then place it into `<PDN home dir>/FileTypes` folder.

## Plugin source code

Naturally, the source code (C#) is provided.

Things are kept as simple as possible - there is only one source file (per each version, that is), and it is intended to be built with a simple one-liner, with opening no IDEs and creating no projects nor solutions nor whatsoever else. (You will just need the PDN of an appropriate version installed.) The file `build.bat` / `build_old.bat` should do the trick, although you need to set a PDN_HOME variable. Or just open the .bat and check the required command - it is literally a one-liner.

There may be a case that system complains on `csc` not found - that never is true. If you have PDN installed it means you also have .NET installed, and it means the csc.exe _is_ there, just not on path for some reason - search for it and invoke by full path.
