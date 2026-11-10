from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path('work/save-20261110/media'); folders=list((root/'frames').iterdir());groups=[['Heavy_Hammer_Swing','Charged_Axe_Chop','Pull_Radish','Collect_Object','Carry_Heavy_Object_Walk','Walking'],['Running','Injured_Walk','Sleep_Normally','Prone_Reach_Help','Sit_Cross_Legged_on_Floor','Long_Breathe_and_Look_Around'],['Female_Bend_Over_Pick_Up_Inspect','Hit_Reaction','Dead','Right_Hand_Sword_Slash','Shield_Push_Left','Archery_Shot_2'],['Victory_Fist_Pump','Idle_3','Idle_4','Idle_5','Talk_with_Hands_Open','Listening_Gesture']]
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
for gi,names in enumerate(groups):
 frames=[]
 for f in range(30):
  im=Image.new('RGB',(960,700),(46,56,61));d=ImageDraw.Draw(im)
  for j,n in enumerate(names):
   im.paste(Image.open(root/'frames'/n/f'{f:03}.png'),((j%3)*320,(j//3)*350+30));d.text(((j%3)*320+8,(j//3)*350+7),n[:32],font=font,fill='white')
  frames.append(im)
 frames[0].save(root/f'motions-{gi+1}.gif',save_all=True,append_images=frames[1:],duration=100,loop=0,optimize=True)
 frames[7].save(root/f'motions-{gi+1}-sample.png')
frames=[Image.open('work/animation-1110/'+n).convert('RGB') for n in ['unity-mining-live.png','unity-mining-next.png']]
frames[0].save(root/'actual-mining-two-frames.gif',save_all=True,append_images=frames[1:],duration=450,loop=0)
print([(p.name,p.stat().st_size) for p in root.glob('*.gif')])
