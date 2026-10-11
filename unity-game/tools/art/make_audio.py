"""Original synthetic domestic soundscape. No samples, licensed songs or voice datasets."""
from pathlib import Path
import numpy as np, wave
OUT=Path(__file__).resolve().parents[2]/'Assets/_Project/Audio'
OUT.mkdir(parents=True,exist_ok=True)
SR=32000;rng=np.random.default_rng(1996)
def write(name,a):
 a=np.nan_to_num(a);a=np.clip(a,-.96,.96)
 with wave.open(str(OUT/(name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes((a*32767).astype('<i2').tobytes())
def noise(n,low,high):
 f=np.fft.rfftfreq(n,1/SR);s=rng.normal(size=f.size)+1j*rng.normal(size=f.size);s*=np.exp(-(f/high)**2)*(1-np.exp(-(f/low)**2));a=np.fft.irfft(s,n=n);return a/(np.std(a)+1e-8)
duration=36;n=SR*duration;t=np.arange(n)/SR;music=np.zeros(n)
# A simple newly authored slow triple-meter radio miniature, 8 bars twice.
melody=[74,71,67,69,71,74,72,69,66,67,71,69,64,67,71,69,66,62,67,69,71,74,71,67]
chords=[[55,59,62],[52,55,59],[57,60,64],[50,54,57],[52,55,59],[48,52,55],[50,54,57],[55,59,62]]
def note(midi,start,dur,amp):
 N=min(int(dur*SR),n-int(start*SR));tt=np.arange(N)/SR;f=440*2**((midi-69)/12);env=(1-np.exp(-tt*45))*np.exp(-tt*1.5);v=np.sin(2*np.pi*f*tt)+.19*np.sin(2*np.pi*f*2*tt)*np.exp(-tt*3)+.055*np.sin(2*np.pi*f*3*tt)
 music[int(start*SR):int(start*SR)+N]+=v*env*amp
beat=.75
for i in range(48):
 note(melody[i%24],i*beat,2.1,.085)
 chord=chords[(i//3)%8];note(chord[i%3],i*beat,1.8,.07)
# Short room/radio coloration and tiny tape noise, not an imitation recording.
for delay,gain in [(1700,.12),(3100,.07),(5100,.04)]:music[delay:]+=music[:-delay]*gain
music+=noise(n,400,3000)*.0018
fade=np.minimum(1,t/.3)*np.minimum(1,(duration-t)/.5);write('radio_original_waltz',music*fade)
n=SR*16;t=np.arange(n)/SR;rain=noise(n,180,6500)*(.10+.018*np.sin(2*np.pi*t/8));rain+=noise(n,30,360)*.12
# Sleet tapping the outside sill.
for _ in range(210):
 at=int(rng.uniform(0,n-1500));tt=np.arange(1500)/SR;rain[at:at+1500]+=rng.uniform(.03,.1)*np.sin(2*np.pi*rng.uniform(1300,3300)*tt)*np.exp(-tt*180)
write('sleet_outside',rain)
t=np.arange(int(SR*.13))/SR;tick=(np.sin(2*np.pi*1800*t)*np.exp(-t*100)+.4*np.sin(2*np.pi*780*t)*np.exp(-t*48))*.22;write('clock_tick',tick)
for i in range(4):
 n=int(SR*.34);t=np.arange(n)/SR;a=noise(n,50,1100)*np.exp(-t*24)*.17+np.sin(2*np.pi*(75+i*6)*t)*np.exp(-t*26)*.11;a*=1-np.exp(-t*160);write('wood_step_'+str(i),a)
print(OUT)
