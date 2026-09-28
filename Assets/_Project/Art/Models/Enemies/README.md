# Enemy models

One folder per enemy type, named as its character: `husk/`, `ghoul/`, `bandit_archer/` (rank looks: `husk_champion/`, `husk_elite/`). In each:

- `<name>.fbx`: the Mixamo character **With Skin** (its own animation is the idle)
- `<name>_albedo.png`: its texture, from `ArtSource/tools/extract_textures.py`
- `<name>_run.fbx`, `<name>_attack.fbx`, `<name>_hit.fbx`, `<name>_death.fbx`: Mixamo animations **Without Skin**, In Place

Then run Tools > ARPG > Sprite Bake > Bake Enemies. The full steps and prompts are in `Docs/09-art-brief.md`, section 5.4.
