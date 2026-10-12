"""Non-rendering Unity jobs only. Never launches a player or captures a screen.
Official -batchmode -nographics plus hidden startup/console and below-normal priority.
"""
from pathlib import Path
import argparse,subprocess,json,datetime,re
parser=argparse.ArgumentParser();parser.add_argument('operation',choices=['story-ready-build','story-props-build','guest-build','audio-build','story-spatial','kitchen13-build','edit-tests','door-tests','bedroom06-apply','bedside-smallprops','bedroom07-apply','bedroom07-final','hall09-apply','hall10-apply','apartment11-apply','apartment12-build','core-build']);parser.add_argument('--label',required=True);parser.add_argument('--verify-unheld-lock',action='store_true');args=parser.parse_args()
assert re.fullmatch(r'[A-Za-z0-9_-]+',args.label), 'Output label must be a simple directory name'
project=Path(__file__).resolve().parents[2]/'unity-game';out=project/'QA'/args.label
lock=project/'Temp/UnityLockfile'
if lock.exists():
 if not args.verify_unheld_lock:raise SystemExit('Unity project lock file exists. Check live process ownership before an explicit unheld-lock check.')
 # Use the OS sharing lock, never erase or rename the file. Unity still checks its own lock.
 import ctypes
 from ctypes import wintypes
 kernel=ctypes.WinDLL('kernel32',use_last_error=True)
 kernel.CreateFileW.argtypes=[wintypes.LPCWSTR,wintypes.DWORD,wintypes.DWORD,ctypes.c_void_p,wintypes.DWORD,wintypes.DWORD,wintypes.HANDLE]
 kernel.CreateFileW.restype=wintypes.HANDLE
 kernel.CloseHandle.argtypes=[wintypes.HANDLE];kernel.CloseHandle.restype=wintypes.BOOL
 handle=kernel.CreateFileW(str(lock),0x80000000,0,None,3,0x80,None)
 if handle==ctypes.c_void_p(-1).value:raise SystemExit('Project lock is unavailable for exclusive read; do not touch the holder. WinError='+str(ctypes.get_last_error()))
 kernel.CloseHandle(handle)
 print('Verified unheld lock file; leaving it intact for Unity own lock check.',flush=True)
if (out/'process.json').exists():raise SystemExit('Evidence directory already used; choose a new label, do not overwrite an earlier run.')
out.mkdir(parents=True,exist_ok=True)
editor=Path('C:/Users/User/UnityEditors/6000.3.26f1/Editor/Unity.exe');assert editor.is_file()
cmd=[str(editor),'-batchmode','-nographics','-silent-crashes','-job-worker-count','2','-projectPath',str(project),'-logFile',str(out/'editor.log')]
if args.operation=='edit-tests':cmd+=['-runTests','-testPlatform','EditMode','-testResults',str(out/'tests.xml')]
elif args.operation=='door-tests':cmd+=['-runTests','-testPlatform','EditMode','-testFilter','Ataka.Tests.ScenarioDoorSafetyTests','-testResults',str(out/'tests.xml')]
elif args.operation=='bedroom06-apply':cmd+=['-executeMethod','Ataka.Editor.BedroomArt06.ApplyPreparedArt','-quit']
elif args.operation=='story-ready-build':cmd+=['-executeMethod','Ataka.Editor.StoryPropsInstaller.InstallReadyAssetsAndBuild','-quit']
elif args.operation=='story-props-build':cmd+=['-executeMethod','Ataka.Editor.StoryPropsInstaller.InstallAndBuild','-quit']
elif args.operation=='guest-build':cmd+=['-executeMethod','Ataka.Editor.GuestInstaller.InstallAndBuild','-quit']
elif args.operation=='audio-build':cmd+=['-executeMethod','Ataka.Editor.ScenarioAudioBuilder.InstallAndBuild','-quit']
elif args.operation=='story-spatial':cmd+=['-executeMethod','Ataka.Editor.StorySpatialAudit.Export','-quit']
elif args.operation=='kitchen13-build':cmd+=['-executeMethod','Ataka.Editor.KitchenArt13.InstallAndBuild','-quit']
elif args.operation=='apartment12-build':cmd+=['-executeMethod','Ataka.Editor.ApartmentArt12.InstallAndBuild','-quit']
elif args.operation=='apartment11-apply':cmd+=['-executeMethod','Ataka.Editor.ApartmentArt11.Install','-quit']
elif args.operation=='hall10-apply':cmd+=['-executeMethod','Ataka.Editor.HallArt10.Install','-quit']
elif args.operation=='hall09-apply':cmd+=['-executeMethod','Ataka.Editor.HallArt09.Install','-quit']
elif args.operation=='bedroom07-final':cmd+=['-executeMethod','Ataka.Editor.BedroomArt07.ApplyReview07','-quit']
elif args.operation=='bedroom07-apply':cmd+=['-executeMethod','Ataka.Editor.BedroomArt07.ApplyReview06','-quit']
elif args.operation=='bedside-smallprops':cmd+=['-executeMethod','Ataka.Editor.BedroomArt06.ApplyBedsideSmallProps','-quit']
else:cmd+=['-executeMethod','Ataka.Editor.ScenarioBuilder.Build','-quit']
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
with (out/'console.log').open('w',encoding='utf-8') as log:
 process=subprocess.Popen(cmd,cwd=project,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT,startupinfo=startup,creationflags=subprocess.CREATE_NO_WINDOW|subprocess.BELOW_NORMAL_PRIORITY_CLASS)
 info={'pid':process.pid,'command':cmd,'operation':args.operation,'scope':'Null graphics; no player, visual acceptance, audio audition or simulated input','utc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
 (out/'process.json').write_bytes((json.dumps(info,indent=2)+'\n').encode('utf-8'));print('HEADLESS_PID',process.pid,flush=True)
 code=process.wait();(out/'exit.json').write_bytes((json.dumps({'pid':process.pid,'exit_code':code})+'\n').encode('utf-8'));print('HEADLESS_EXIT',code,flush=True)
 raise SystemExit(code)
