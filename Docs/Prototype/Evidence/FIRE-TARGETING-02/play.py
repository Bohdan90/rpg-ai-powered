import json,time,pathlib,subprocess,sys
root=pathlib.Path('/private/tmp/fire-targeting-02')
def state():
 for _ in range(30):
  try:return json.loads((root/'state.json').read_text())
  except (ValueError,FileNotFoundError):time.sleep(.1)
 raise RuntimeError('No state')
def command(c):
 (root/'command.txt').write_text(c);time.sleep(.35)
def mouse(*args):
 subprocess.run([str(root/'mouse'),*map(str,args)],check=True,stdout=subprocess.DEVNULL);time.sleep(.12)
def point(x,y):
 s=state();scale=s['gw']/s['rw'];mouse(s['gx']+scale*x,s['gy']+45+scale*y)
def button(name):
 command('scroll:'+name);s=state();b=next(b for b in s['buttons'] if b['name']==name) if not name.startswith('text:') else next(b for b in s['buttons'] if b['text'].startswith(name[5:]));point(b['x'],b['y'])
def field(label,choice):
 command('scroll:label:'+label);f=next(f for f in state()['fields'] if f['label']==label)
 target=f['choices'].index(choice) if isinstance(choice,str) else choice
 if f['index']==target:return
 point(f['x']+90,f['y'])
 for _ in range(target+1):mouse('key','down')
 mouse('key','return');time.sleep(.2)
 assert next(f for f in state()['fields'] if f['label']==label)['index']==target,(label,target,state()['fields'])
def snap(name):
 (root/'results'/f'{name}.json').write_text(json.dumps(state(),indent=2));subprocess.run(['/usr/sbin/screencapture','-x','-l','33582',str(root/'results'/f'{name}.png')],check=True)
def target(x,y):
 command(f'cell:{x},{y}');s=state();point(s['x'],s['y'])
def cast(spell,x,y,ff=False):
 field('Ability',spell);target(x,y)
 if ff:
  t=next(t for t in state()['toggles'] if t['label']=='Explicitly confirm allied target')
  if not t['value']:point(t['x'],t['y'])
 snap('preview-'+spell);button('confirm-command')

def hover(x,y):
 command(f'cell:{x},{y}');s=state();scale=s['gw']/s['rw'];mouse(s['gx']+scale*s['x'],s['gy']+45+scale*s['y'],'hover')
def active(id):
 for _ in range(20):
  if state()['active']==id:return
  button('end-activation')
 raise AssertionError('actor not reached')
def end():
 button('duel-end')
 if state()['world']['handoff']:button('duel-continue')
def move(n):button('duel-node-'+str(n));button('duel-move')
def smoke(resume=False):
 if resume:
  history=[json.loads(line) for line in (root/'results/fire-targeting-05b-trace.jsonl').read_text().splitlines()];world_before=next(q['world'] for q in reversed(history) if q['world'] is not None and q['world'].get('refresh',0)>0)
  mage=next(u for u in state()['units'] if u['profile']=='FireMageTII' and u['side']=='West');button('end-activation');active(mage['id']);cast('Fireball',mage['x']+3,mage['y']);snap('21-05b-cast');ids={u['persistent'] for u in state()['units']}
 else:
  command('recreate');time.sleep(.8);command('mode:fire-targeting-05b');button('city-start-b');move(6);end();move(9);end();world_before=state()['world'];button('duel-attack');snap('20-05b-battle-origin')
  mage=next(u for u in state()['units'] if u['profile']=='FireMageTII' and u['side']=='West');active(mage['id']);ally=next(u for u in state()['units'] if u['side']=='West' and u['id']!=mage['id'] and max(abs(u['x']-mage['x']),abs(u['y']-mage['y']))<=3);cast('FireArmor',ally['x'],ally['y']);snap('20b-05b-allied-fire-armor');button('end-activation');active(mage['id']);cast('Fireball',mage['x']+3,mage['y']);snap('21-05b-cast');ids={u['persistent'] for u in state()['units']}
 for i in range(180):
  q=state()
  if q['ended']:break
  u=next(u for u in q['units'] if u['id']==q['active'])
  if u['side']=='West':button('end-activation');continue
  command('escape');q=state()
  if q['plan']=='MoveCommand':button('primary-attack');target(q['tx'],q['ty']);button('confirm-command')
  else:button('end-activation')
 assert state()['ended'];command('export');snap('22-05b-physical-escape-result');battle=state();button('world-return');snap('23-05b-return');w=state()['world'];assert set(m['id'] for side in ['west','east'] for m in w[side]['formation']['members'])==ids
 assert next(m for m in w['west']['formation']['members'] if m['id']==mage['persistent'])['fireballUsed']==1
 for u in battle['units']:
  m=next(m for m in w[u['side'].lower()]['formation']['members'] if m['id']==u['persistent']);assert (m['hp'],m['armor'])==(u['hp'],u['armor'])
 assert w['refresh']==world_before['refresh'];assert all(u['barrier']==0 and not u['fire'] for u in battle['units']);button('duel-save');saved=state()['world']['checksum'];command('recreate');time.sleep(.8);button('city-load-b');assert state()['world']['checksum']==saved;snap('24-05b-recreated-loaded');print('05B battle / physical Escape / identical IDs pools budget / save-recreate-load PASS',flush=True)

if __name__=='__main__':
 if sys.argv[1]=='firetarget':
  command('mode:fire-targeting-gui');command('fixture:offaxis');active(1);hover(10,9);snap('01-default-offaxis-preview');before=state()['records'];target(10,9);assert state()['records']==before+1;assert next(u for u in state()['units'] if u['id']==2)['hp']==29;snap('02-default-offaxis-hit');command('export')
  command('fixture:armor');active(1);button('spell-FireStream');hover(10,10);snap('03-empty-cell-full-stream-friendly-fire');before=state()['records'];target(10,10);button('confirm-command') if any(b['name']=='confirm-command' for b in state()['buttons']) else None;assert state()['records']==before
  t=next(t for t in state()['toggles'] if t['label']=='Explicitly confirm allied target');point(t['x'],t['y']);button('confirm-command');assert state()['records']==before+1;assert next(u for u in state()['units'] if u['id']==3)['hp']==29;snap('04-friendly-fire-cast');command('export')
  command('fixture:armor');active(1);button('spell-FireArmor');hover(9,9);snap('05-allied-recipient-preview');target(9,9);button('confirm-command');s=state();a=next(u for u in s['units'] if u['id']==1);u=next(u for u in s['units'] if u['id']==3);assert u['barrier']==6 and u['expiry']==2 and u['fire'] and not u['exhausted'];assert a['barrier']==0 and a['exhausted'];snap('06-allied-barrier-caster-cost')
  button('end-activation');active(2);target(9,9);button('confirm-command');snap('07-melee-fire-trigger');s=state();print('post melee',[(u['id'],u['barrier'],u['burn'],u['expiry'],u['fire']) for u in s['units']],flush=True);assert next(u for u in s['units'] if u['id']==2)['burn']==1;assert next(u for u in s['units'] if u['id']==3)['barrier']==0 and next(u for u in s['units'] if u['id']==3)['fire']
  button('end-activation');active(3);assert next(u for u in state()['units'] if u['id']==3)['expiry']==1;snap('08-first-recipient-start');button('end-activation');active(3);assert not next(u for u in state()['units'] if u['id']==3)['fire'];snap('09-second-recipient-start-expired');command('export');command('old-replays');print('GUI offaxis / empty aim / Friendly Fire / ally barrier / melee Burn / recipient expiry PASS',flush=True)
 elif sys.argv[1]=='final':
  command('mode:ux-final');button('lab-FireVsIce');active(1);button('spell-Fireball');hover(13,6);snap('50-final-hover');target(13,6);hover(14,6);assert any(b['name']=='confirm-command' for b in state()['buttons']);button('fit-board');assert not any(b['name']=='confirm-command' for b in state()['buttons']);snap('51-final-fit-clears');command('old-replays');button('spell-FireArmor');hover(9,8);snap('52-final-self');button('cancel-preview');command('fixture:ux');active(1);target(11,11);before=state()['records'];button('end-activation');active(1);target(11,11);assert state()['records']>before;snap('53-primary-next-activation');command('export');print('final hover / click lock / fit clear / Self / repeated primary PASS',flush=True)
 elif sys.argv[1]=='extras':
  command('recreate');time.sleep(.7);command('mode:ux-support-obstacles');button('lab-SupportVsFire');active(4);button('spell-FireStream');hover(9,9);snap('30-stream-obstructed');button('cancel-preview');button('spell-FireStream');hover(10,5);snap('31-stream-off-ray');button('cancel-preview');button('spell-FireStream');hover(12,10);snap('32-stream-fourth-cell');button('cancel-preview')
  command('fixture:freeze');active(1);button('spell-Freeze');hover(10,8);snap('33-explicit-freeze');button('cancel-preview');before=state()['records'];target(7,8);snap('34-ground-move-only');assert state()['records']==before;button('cancel-preview');target(8,9);snap('35-friendly-inspection');assert state()['records']==before;button('cancel-preview')
  target(9,8);button('confirm-command');button('staff-attack');target(10,8);button('confirm-command');snap('36-explicit-staff');command('export');button('end-activation');snap('37-handoff-clear')
  command('fixture:freeze');active(1);button('spell-IceShield');hover(8,9);snap('38-friendly-shield-preview');target(8,9);button('confirm-command');snap('39-friendly-shield-cast')
  command('recreate');time.sleep(.7);button('lab-FireVsIce');field('Controller',1);snap('40-ai-controller');button('end-activation');time.sleep(3);snap('41-ai-casts');command('export')
 elif sys.argv[1]=='smoke':smoke()
 elif sys.argv[1]=='resume-smoke':smoke(True)
 elif sys.argv[1]=='finish-smoke':
  w=state()['world'];assert w['refresh']==2;assert next(m for m in w['west']['formation']['members'] if m['id']=='duel-West-5')['fireballUsed']==1;assert all(u['barrier']==0 and not u['fire'] for u in state()['units']);button('duel-save');saved=state()['world']['checksum'];command('recreate');time.sleep(.8);button('city-load-b');assert state()['world']['checksum']==saved;snap('24-05b-recreated-loaded');print('05B same Refresh 2 / same IDs pools budget / save-recreate-load PASS',flush=True)
 elif sys.argv[1]=='ux':
  command('fixture:ux');active(1);snap('06-authored-origin')
  before=state()['records'];hover(10,9);target(10,9);assert state()['records']==before;button('cancel-preview')
  hover(11,11);snap('07-primary-diagonal-hover');target(11,11);assert state()['records']==before+1;snap('08-primary-diagonal-cast')
  button('end-activation');active(4);hover(8,8);snap('09-ice-primary-hover');before=state()['records'];target(8,8);assert state()['records']==before+1;snap('10-ice-primary-cast');command('export')
  button('end-activation');active(1);button('spell-Fireball');hover(8,9);snap('11-friendly-fire-hover');before=state()['records'];target(8,9);assert state()['records']==before
  t=next(t for t in state()['toggles'] if t['label']=='Explicitly confirm allied target');point(t['x'],t['y']);button('confirm-command');assert state()['records']==before+1;snap('12-friendly-fire-cast');command('export')
 elif sys.argv[1]=='pending':
  command('fixture:ux');active(1);button('spell-Fireball');target(13,6);before=state()['records'];hover(14,6);snap('13-moving-to-confirm');print('confirm enabled',any(b['name']=='confirm-command' for b in state()['buttons']), 'records',before)
 elif sys.argv[1]=='armor':
  active(1);button('spell-FireArmor');hover(9,8);snap('02-self-hover');before=state()['records'];target(9,8);snap('03-self-invalid');assert state()['records']==before;print(state()['hud'][-2500:]);button('cancel-preview')
 elif sys.argv[1]=='fireball':
  button('spell-Fireball');hover(13,6);snap('04-fireball-empty-hover');before=state()['records'];target(13,6);button('confirm-command');snap('05-fireball-empty-cast');assert state()['records']==before+1
 elif sys.argv[1]=='command':command(sys.argv[2])
 elif sys.argv[1]=='button':button(sys.argv[2])
 elif sys.argv[1]=='snap':snap(sys.argv[2])
