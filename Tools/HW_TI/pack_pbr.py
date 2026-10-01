"""Pack remesh-compatible glTF metallic/roughness into URP metallic/smoothness."""
from PIL import Image,ImageOps
from pathlib import Path
import sys,shutil,json
src=Path(sys.argv[1]);dst=Path(sys.argv[2]);dst.mkdir(parents=True,exist_ok=True)
mr=Image.open(src/'metallic_roughness.png').convert('RGB');_,rough,metal=mr.split()
assert Image.open(src/'metallic.png').tobytes()==metal.tobytes()
assert Image.open(src/'roughness.png').tobytes()==rough.tobytes()
Image.merge('RGBA',(metal,Image.new('L',mr.size,0),Image.new('L',mr.size,0),ImageOps.invert(rough))).save(dst/'MetallicSmoothness.png')
shutil.copy2(src/'base_color.png',dst/'BaseColor.png');shutil.copy2(src/'normal.png',dst/'Normal.png')
print(json.dumps({'source':'remesh glTF','metallic':'blue -> red','smoothness':'1 - green -> alpha','size':mr.size,'normal':'OpenGL tangent +Y preserved; Unity normal importer','allPixelsVerified':True}))
