import json,time,pathlib,subprocess,sys
root=pathlib.Path('/private/tmp/realm-operations-06')
def state():
 for _ in range(30):
  try:return json.loads((root/'state.json').read_text())
  except (ValueError,FileNotFoundError):time.sleep(.1)
 raise RuntimeError('No state')
def command(c):
 (root/'command.txt').write_text(c);time.sleep(.35)
def mouse(*args):
 subprocess.run([str(root/'mouse'),*map(str,args)],check=True,stdout=subprocess.DEVNULL);time.sleep(.2)
def point(x,y):
 s=state();scale=s['gw']/s['rw'];mouse(s['gx']+scale*x,s['gy']+45+scale*y)
def button(name):
 command('scroll:'+name);s=state();b=next(b for b in s['buttons'] if b['name']==name) if not name.startswith('text:') else next(b for b in s['buttons'] if b['text'].startswith(name[5:]));point(b['x'],b['y'])
def field(label,choice):
 command('scroll:label:'+label);f=next(f for f in state()['fields'] if f['label']==label)
 target=f['choices'].index(choice) if isinstance(choice,str) else choice
 if f['index']==target:return
 point(f['x']+90,f['y'])
 mouse('key','home')
 for _ in range(target):mouse('key','down')
 mouse('key','return');time.sleep(.2)
 assert next(f for f in state()['fields'] if f['label']==label)['index']==target,(label,target,state()['fields'])
def snap(name):
 (root/'results'/f'{name}.json').write_text(json.dumps(state(),indent=2))
 wid=(root/'window-id.txt').read_text().strip()
 subprocess.run(['/usr/sbin/screencapture','-x','-l',wid,str(root/'results'/f'{name}.png')],check=True)
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
 before=state()['world']['completed']
 button('realm-end')
 if state()['world'] and state()['world']['completed']==before and not state()['world']['handoff']:button('realm-end')
 if state()['world']['handoff']:button('realm-continue')
def cycle():end();end()
def move(n):
 button('realm-node-'+str(n));button('realm-move');q=state()['world'];a=next(a for a in q['realm']['armies'] if a['id']==q['realm']['west' if q['activeSide']==0 else 'east']['selected']);assert a['node']==n,('move failed',a['node'],n,state()['hud'][-600:])
def choose(label,choice):field(label,choice)
def toggle_id(id):
 command('scroll:toggle:'+id)
 t=next(t for t in state()['toggles'] if t['label'].startswith(id+' ·'))
 point(t['x'],t['y'])
def army(side,slot):return f'realm06-{side}-army-{slot}'
def entry(first='West'):
 command('recreate');time.sleep(.8);button('realm-start-'+first.lower())
def physical_battle(mode='retreat-east',limit=700):
 for i in range(limit):
  q=state()
  if q['ended']:
   command('export');return
  u=next(u for u in q['units'] if u['id']==q['active'])
  if mode=='retreat-east':
   if u['side']=='West':button('end-activation');continue
   command('escape')
  elif mode=='joint':
   lead=[v for v in q['units'] if v['persistent'].startswith('realm06-West-army-1-')]
   gone=all(v['status']=='Escaped' for v in lead)
   if not ((gone and u['side']=='East') or (not gone and u in lead)):button('end-activation');continue
   command('escape')
  else:command('plan')
  q=state();plan=q['plan']
  if plan=='EndActivationCommand':button('end-activation')
  elif plan=='DefendCommand':button('defend')
  elif plan=='MoveCommand':button('primary-attack');target(q['tx'],q['ty']);button('confirm-command')
  elif plan=='BasicAttackCommand':button('staff-attack') if u['profile'].endswith('MageTI') or u['profile'].endswith('MageTII') else button('primary-attack');target(q['tx'],q['ty']);button('confirm-command')
  elif plan=='CastCommand':
   button('spell-'+q['spell']);target(q['tx'],q['ty'])
   if q['ff']:
    t=next(t for t in state()['toggles'] if t['label']=='Explicitly confirm allied target')
    if not t['value']:point(t['x'],t['y'])
   button('confirm-command')
  else:raise RuntimeError(plan)
  if i%20==0:print('physical commands',i,state()['records'],flush=True)
 raise RuntimeError('bounded battle did not end')
if __name__=='__main__':
 action=sys.argv[1]
 if action=='button':button(sys.argv[2])
 elif action=='command':command(sys.argv[2])
 elif action=='snap':snap(sys.argv[2])
 elif action=='field':field(sys.argv[2],sys.argv[3])
 elif action=='move':move(int(sys.argv[2]))
 elif action=='cycle':cycle()
 elif action=='end':end()
 elif action=='toggle':toggle_id(sys.argv[2])
 elif action=='battle':physical_battle(sys.argv[2])
 elif action=='r1-setup':
  move(8);snap('r1-01-two-distinct-tasks');end();move(9);field('Selected army',army('East',2));move(7);end();snap('r1-02-shared-refresh');button('realm-node-7');snap('r1-03-one-hop-preview');button('realm-attack');button('realm-fight');snap('r1-04-tactical-hotseat');physical_battle('fight');snap('r1-05-real-result');command('export');button('world-return');snap('r1-06-persistent-aftermath')
 elif action=='r1-return-r2':
  field('Selected army',army('West',1));move(3);snap('r1-07-return-decision-armor-damage');end();field('Selected army',army('East',1));move(13);end();field('Selected army',army('West',1));move(1);field('Selected army',army('West',2));move(1);snap('r1-08-both-at-keep')
  field('Physical source',army('West',2));field('Recipient / recruit destination','Reserve');toggle_id(army('West',2)+'-unit-2');snap('r2-01-deposit-quote');button('realm-transfer');field('Reserve officer / Commission candidate',army('West',2)+'-unit-2');button('realm-commission');snap('r2-02-paid-commission');button('realm-research');button('realm-save');saved=state()['world']['checksum'];command('recreate');time.sleep(.8);button('realm-load-slot');assert state()['world']['checksum']==saved;snap('r4-01-pending-orders-recreated');cycle();snap('r2-03-no-same-cycle-completion');cycle();snap('r2-04-commission-completed');field('Physical source','Reserve');field('Reserve officer / Commission candidate',army('West',2)+'-unit-2');button('realm-form');snap('r2-05-third-army-same-officer');field('Recipient / recruit destination','Reserve');button('realm-recruit');cycle();snap('r2-06-paid-reserve-recruit');field('Physical source','Reserve');field('Recipient / recruit destination',army('West',3));new=next(u['id'] for u in state()['world']['realm']['west']['reserve'] if 'recruit' in u['id']);toggle_id(new);snap('r2-07-actual-foreign-load-quote');button('realm-transfer');snap('r2-08-transfer-no-food-created');button('realm-save');saved=state()['world']['checksum'];command('recreate');time.sleep(.8);button('realm-load-slot');assert state()['world']['checksum']==saved;snap('r4-02-three-armies-reloaded')
 elif action=='resume-r1-r2':
  button('realm-load-slot');button('realm-select');move(13);end();field('Selected army',army('West',1));move(1);field('Selected army',army('West',2));move(1);snap('r1-08-both-at-keep')
  field('Physical source',army('West',2));field('Recipient / recruit destination','Reserve');toggle_id(army('West',2)+'-unit-2');snap('r2-01-deposit-quote');button('realm-transfer');field('Reserve officer / Commission candidate',army('West',2)+'-unit-2');button('realm-commission');snap('r2-02-paid-commission');button('realm-research');button('realm-save');saved=state()['world']['checksum'];command('recreate');time.sleep(.8);button('realm-load-slot');assert state()['world']['checksum']==saved;snap('r4-01-pending-orders-recreated');cycle();snap('r2-03-no-same-cycle-completion');cycle();snap('r2-04-commission-completed');field('Physical source','Reserve');field('Reserve officer / Commission candidate',army('West',2)+'-unit-2');button('realm-form');snap('r2-05-third-army-same-officer');field('Recipient / recruit destination','Reserve');button('realm-recruit');cycle();snap('r2-06-paid-reserve-recruit');field('Physical source','Reserve');field('Recipient / recruit destination',army('West',3));new=next(u['id'] for u in state()['world']['realm']['west']['reserve'] if 'recruit' in u['id']);toggle_id(new);snap('r2-07-actual-foreign-load-quote');button('realm-transfer');snap('r2-08-transfer-no-food-created');button('realm-save');saved=state()['world']['checksum'];command('recreate');time.sleep(.8);button('realm-load-slot');assert state()['world']['checksum']==saved;snap('r4-02-three-armies-reloaded')
 elif action=='openfield':
  command('scroll:label:'+sys.argv[2]);f=next(f for f in state()['fields'] if f['label']==sys.argv[2]);point(f['x']+90,f['y']);time.sleep(.3);snap('input-open-dropdown')

 elif action=='r2-continue':
  field('Selected army',army('West',2));move(1);snap('r1-08-both-at-keep')
  field('Physical source',army('West',2));field('Recipient / recruit destination','Reserve');toggle_id(army('West',2)+'-unit-2');snap('r2-01-deposit-quote');button('realm-transfer');field('Reserve officer / Commission candidate',army('West',2)+'-unit-2');button('realm-commission');snap('r2-02-paid-commission');button('realm-research');button('realm-save');saved=state()['world']['checksum'];command('recreate');time.sleep(.8);button('realm-load-slot');assert state()['world']['checksum']==saved;snap('r4-01-pending-orders-recreated');cycle();snap('r2-03-no-same-cycle-completion');cycle();snap('r2-04-commission-completed');field('Physical source','Reserve');field('Reserve officer / Commission candidate',army('West',2)+'-unit-2');button('realm-form');snap('r2-05-third-army-same-officer');field('Recipient / recruit destination','Reserve');button('realm-recruit');cycle();snap('r2-06-paid-reserve-recruit');field('Physical source','Reserve');field('Recipient / recruit destination',army('West',3));new=next(u['id'] for u in state()['world']['realm']['west']['reserve'] if 'recruit' in u['id']);toggle_id(new);snap('r2-07-actual-foreign-load-quote');button('realm-transfer');snap('r2-08-transfer-no-food-created');button('realm-save');saved=state()['world']['checksum'];command('recreate');time.sleep(.8);button('realm-load-slot');assert state()['world']['checksum']==saved;snap('r4-02-three-armies-reloaded')
 elif action=='r1-finish':
  field('Selected army',army('West',3));move(8);snap('r1-10-redeploy-new-army');end();field('Selected army',army('East',1));move(7);snap('r1-11-east-recaptures-beacon');end()
  for i in range(8):
   if state()['world']['winner']>=0:break
   cycle()
  assert state()['world']['winner']==0;snap('r1-12-legal-pressure-victory');button('realm-save');print('R1 legal result',state()['world']['refresh'],state()['world']['west']['pressure'],state()['world']['east']['pressure'],flush=True)
 elif action=='r3-city':
  command('mode:r3-commanderless');command('fixture:commanderless');snap('r3-01-authored-commanderless');field('Physical source',army('West',1));field('Recipient / recruit destination',army('West',2));toggle_id(army('West',1)+'-unit-2');snap('r3-02-partial-absorption-preview');button('realm-transfer');snap('r3-03-remnant-still-exists');field('Selected army',army('West',2));button('realm-disband');field('Reserve officer / Commission candidate',army('West',2)+'-unit-1');field('Selected army',army('West',1));button('realm-assign');snap('r3-04-real-officer-reassigned');w=state()['world'];a=next(a for a in w['realm']['armies'] if a['id']==army('West',1));assert a['commander']==army('West',2)+'-unit-1';assert next(u for u in a['units'] if u['id']==army('West',1)+'-unit-1')['status']==2
 elif action=='r3-joint':
  command('mode:r3-joint');command('fixture:joint');button('realm-recruit');button('realm-save');saved=state()['world']['checksum'];command('recreate');time.sleep(.8);button('realm-load-slot');assert state()['world']['checksum']==saved;snap('r4-03-before-joint-pending-recruit');button('realm-node-7');snap('r3-05-joint-preview');button('realm-attack');button('realm-fight');snap('r3-06-full-initial-deployment');physical_battle('joint');snap('r3-07-independent-withdrawals-result');button('world-return');snap('r3-08-no-substitute-lead');w=state()['world'];assert w['refresh']==1;assert next(a for a in w['realm']['armies'] if a['id']==army('West',2))['tempo']==50;assert w['realm']['west']['hasRecruit'];button('realm-save');saved=w['checksum'];command('recreate');time.sleep(.8);button('realm-load-slot');assert state()['world']['checksum']==saved;snap('r4-04-after-joint-recreated');cycle();snap('r4-05-continuation-one-recruit-refresh');assert len(state()['world']['realm']['west']['reserve'])==1
 elif action=='r5':
  command('mode:r5-mirrored');entry('East');field('Selected army',army('East',1));move(6);field('Selected army',army('East',2));move(11);snap('r5-01-east-concentrated');end();move(8);field('Selected army',army('West',2));move(3);snap('r5-02-west-split');end();snap('r5-03-mirrored-refresh');print('R5 mirrored',state()['world']['refresh'],state()['world']['west']['pressure'],state()['world']['east']['pressure'],flush=True)
