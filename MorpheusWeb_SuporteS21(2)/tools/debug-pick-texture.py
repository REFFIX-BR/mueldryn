import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location(
    "export_cosmetic_icons",
    Path(__file__).resolve().parent / "export-cosmetic-icons.py",
)
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)

load_json = mod.load_json
MODELS_JSON = mod.MODELS_JSON
skin_dir_for = mod.skin_dir_for
pick_texture = mod.pick_texture
load_texture = mod.load_texture

models = {f"{r['Group']}-{r['Number']}": r for r in load_json(MODELS_JSON)}
for key in ['7-316', '7-304', '7-313', '12-317']:
    row = models[key]
    folder = skin_dir_for(row)
    tex = pick_texture(folder, int(row['Group']), row['File'])
    print(key, row.get('Name'), 'folder=', folder.name if folder else None)
    print('  picked', tex.name if tex else None)
    if tex:
        img = load_texture(tex)
        print('  size', img.size if img else None)
        out = Path('tmp') / f'debug-{key}.png'
        out.parent.mkdir(exist_ok=True)
        img.save(out)
