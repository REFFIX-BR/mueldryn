from importlib.util import spec_from_file_location, module_from_spec
from pathlib import Path
spec = spec_from_file_location('exp', Path('tools/export-cosmetic-icons.py'))
mod = module_from_spec(spec)
spec.loader.exec_module(mod)
img = mod.load_texture(Path(r'../MuMain/src/bin/Data/Item/CustomItem/Skin/Magma/basehelmofemia.ozt'))
print(img.size if img else None)
if img:
    img.save('tmp/debug-basehelm.png')
