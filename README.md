# S5Converter

converting renderware files from/to json.


# Notes for datatypes:

## Texture:
- not stored directly in the object, just a filename

## Geometry:
- native format geometry not supported (s5: not used)
- flags: NumTextureCoordinates and PreLit are informative only, exporting overrides them.

## Frame:
- parentFrame builds hierarchy
- hanimPLG extension defines bones for animations
	- (s5: hardcoded bone ids, not all of them need to exist)
		- 100-131 particle effect switch (ED::CParticleEffectSwitchBehavior + EGL::CParticleEffectSwitchBehavior)
		- 200 building flag (GD::CBuildingBehavior + GD::CBuildingBehaviorProps)
		- 300++ house fires (GD::CBuildingBehavior + number from GD::CBuildingBehaviorProps in entity xml)
		- 400++ construction dust clouds (GD::CBuildingBehavior + number from GD::CBuildingBehaviorProps in entity xml)
		- 500-502 particle attachments (ED::CParticleEffectAttachmentBehavior + EGL::CParticleEffectAttachmentBehavior)
- userDataPLG
	- (s5: holds extra data on frames)

## Atomic:
- links to a frame (position/transformation in world) and a geometry by index
- flags: (s5: RenderShadow, override from model.xml?)

## Clump:
- dff files
- (s5: loaded as model)

## Matrix:
- flags: not sure if they get set after reading

## Standard Animation:
- changes verticies of the model via bones
- types
	- HierarchicalAnim
	- CompressedAnim (HierarchicalAnim with most floats compressed into 16 bits)
- anm files
- (s5: loaded as animations and can be played on a model, game time time dependent)

## UV Animation:
- changes uv coordinates of textures
- types
	- UVAnimLinear
	- UVAnimParam
- uva files
- attached to cumps via Material UVAnim extension. lookup by name in the curretly active uv anim dict
- (s5: model xml defines, if a model/clump has uv anims, if yes, loads uv anim dict with the same filename, real time dependent?)
- (s5: example PB_Alchemist1 called acid, green stuff)

## Morph:
- changes verticies of the model directly
- defined in the morphTargets of a geometry
- timing and order defined by MorphPLG extension on geometry
- (s5: example: XD_StandardLarge)
- (s5: real time dependent)

## Material FX:
- needs to be added to Material
- needs to be enabled on Atomic
- flags need to match Data fields
- (s5: used for snow textures)

## ParticleStandard:
- on atomic
- automatically builds particle & emitter classes/property tables
- requires ids for particle & emitter to be set, recommended to have different flags if you change the presence of any of the options
- not all emitter fileds can be combined (currently no check)
- (s5: real time dependent)

## UserData:
- can be attached as extension to practically everything
- (s5: contains varoius data)
	- "3dsmax User Properties" array
		- frame of particleeffect (atomic): "(srcblend=|destblend=)(zero|one|srcalpha|invsrcalpha)" sets to something particle related?
		- frame of atomic: alphablendmode=("onepass|twopass|building|ornamental|default) sets some atomic flags (0x40|0x80|0x80|0x80|0x80)
		- frame of atomic: decal=? doodad => copied the atomic somewhere and removes it from clump?
		- frame of atomic: effect=X sets directx effect (shader) for atomic? (ignored if set by model xml?)

## BinMesh:
- contains predefined meshes as BinMeshPLG extension on geometry

## Skin:
- makes verticies influenced by up to 4 bones instead of 1
- attached as SkinPLG extension on geometry
