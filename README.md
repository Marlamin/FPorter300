# FPorter300
Utility to f-port World of Warcraft WMOs from 10.0+ to 9.2.7. 

## Notes on conversion
Causes loss in LOD 0 (No LOD) texture quality/accuracy due to lower-texture count [shader](https://wowdev.wiki/WMO#Shader_types_(26522)) (13 instead of 23, which is only available in 10.0+) being used in 9.2.7 as well as possible other issues with materials due to [MPY2](https://wowdev.wiki/WMO#MPY2_chunk) being converted back to [MOPY](https://wowdev.wiki/WMO#MOPY_chunk).

## Usage
Put input files in `input` directory, once you start FPorter300.WMO.exe they will be copied over to the `output` directory and patched accordingly. Press any key to exit when it is done.

## Credits
Credits to Pedron for the shader patching logic.
