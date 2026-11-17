import numpy as np, wave
SR=44100
rng=np.random.default_rng(7)

def t_(d): return np.arange(int(d*SR))/SR
def env_ad(n,a,d,curve=4.0):
    t=np.arange(n)/SR; e=np.where(t<a, t/max(a,1e-6), np.exp(-(t-a)/max(d,1e-6)*curve/4)); return e
def adsr(n,a,dcy,s,r,hold):
    t=np.arange(n)/SR; e=np.zeros(n)
    e=np.where(t<a,t/a,1.0)
    e=np.where((t>=a)&(t<a+dcy),1-(1-s)*(t-a)/dcy,e)
    e=np.where((t>=a+dcy)&(t<hold),s,e)
    e=np.where(t>=hold,s*np.exp(-(t-hold)/r),e)
    return e
def lowpass(x,fc):
    X=np.fft.rfft(x); f=np.fft.rfftfreq(len(x),1/SR); X*=1/np.sqrt(1+(f/fc)**4); return np.fft.irfft(X,len(x))
def bandpass(x,lo,hi):
    X=np.fft.rfft(x); f=np.fft.rfftfreq(len(x),1/SR)
    X*=1/np.sqrt(1+(f/hi)**4)*(1/np.sqrt(1+(lo/np.maximum(f,1))**4)); return np.fft.irfft(X,len(x))
def hz(n): return 440*2**((n-69)/12)
def tailfade(y,ms=.25):
    n=min(len(y),int(ms*len(y)) if ms<1 else int(ms)); y=y.copy(); y[-n:]*=np.cos(np.linspace(0,np.pi/2,n))**2; return y
def bell(f,d,amp=1,bright=1.0):
    t=t_(d); y=np.zeros_like(t)
    for ratio,a,dec in [(1,1,1),(2.0,.35*bright,.55),(3.01,.18*bright,.35),(4.2,.08*bright,.22),(5.4,.04*bright,.15)]:
        if f*ratio<SR/2.2: y+=a*np.sin(2*np.pi*f*ratio*t)*np.exp(-t/(d*dec*.45))
    return tailfade(amp*y*np.minimum(1,t/0.002))
def pluck(f,d,amp=1,soft=1.0):
    t=t_(d); y=np.zeros_like(t)
    for k in range(1,9):
        if f*k>SR/2.2: break
        y+=(1/k**(1.3+soft*.5))*np.sin(2*np.pi*f*k*t+rng.uniform(0,.3))*np.exp(-t*(2.2+k*1.1*soft)/d*1.2)
    return tailfade(amp*y*np.minimum(1,t/0.004))
def pad(f,d,amp=1,a=.8,r=1.2):
    t=t_(d); y=np.zeros_like(t)
    for det in (-0.12,0,0.12):
        ff=f*2**(det/12)
        for k in range(1,7):
            y+=(1/k**1.6)*np.sin(2*np.pi*ff*k*t+rng.uniform(0,6.28))
    e=np.minimum(1,t/a)*np.minimum(1,(d-t)/r)
    return amp*y/3*np.clip(e,0,1)
def noise(d): return rng.standard_normal(int(d*SR))
def place(buf,sig,start,circular=False):
    i=int(start*SR)
    if circular:
        idx=(np.arange(len(sig))+i)%len(buf); np.add.at(buf,idx,sig)
    else:
        end=min(len(buf),i+len(sig)); buf[i:end]+=sig[:end-i]
def reverb_ir(d,decay,seed):
    r=np.random.default_rng(seed); n=int(d*SR); t=np.arange(n)/SR
    ir=r.standard_normal(n)*np.exp(-t/decay*3); ir=lowpass(ir,5500); ir[0]=0; ir=tailfade(ir,.4); return ir/np.sqrt(np.sum(ir**2))
def add_reverb(x,wet=.25,d=1.6,decay=1.2,seed=1,tail=True):
    ir=reverb_ir(d,decay,seed)
    n=len(x)+(len(ir) if tail else 0)
    y=np.fft.irfft(np.fft.rfft(x,n)*np.fft.rfft(ir,n),n)
    out=np.zeros(n); out[:len(x)]=x
    return out*(1-wet*.5)+y*wet
def finish(x,peak=.85,fade=.01):
    x=x-np.mean(x)
    n=int(fade*SR); x[:min(n,len(x))]*=np.linspace(0,1,min(n,len(x)))
    m=min(len(x)//3,int(.08*SR)); x[-m:]*=np.cos(np.linspace(0,np.pi/2,m))**2
    return x/np.max(np.abs(x))*peak
def write(name,x,stereo=False):
    if stereo:
        data=(np.clip(x,-1,1)*32767).astype('<i2').T.reshape(-1)
    else: data=(np.clip(x,-1,1)*32767).astype('<i2')
    with wave.open(name,'wb') as w:
        w.setnchannels(2 if stereo else 1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())

# ---------------- SFX ----------------
def sfx_click():
    d=.09; t=t_(d)
    tick=bandpass(noise(d),2500,6000)*np.exp(-t/.004)*.6
    tone=np.sin(2*np.pi*1650*t)*np.exp(-t/.018)*.5+np.sin(2*np.pi*3300*t)*np.exp(-t/.008)*.15
    return finish(tick+tone,.55)
def sfx_back():
    d=.12; t=t_(d); f=1100-500*np.minimum(1,t/.05)
    tone=np.sin(2*np.pi*np.cumsum(f)/SR)*np.exp(-t/.025)*.6
    tick=bandpass(noise(d),1500,4000)*np.exp(-t/.004)*.4
    return finish(tone+tick,.5)
def sfx_place():
    d=.45; t=t_(d)
    modes=[(520,1,.09),(1290,.55,.05),(2410,.3,.03),(3720,.15,.02)]
    y=sum(a*np.sin(2*np.pi*f*t)*np.exp(-t/dec) for f,a,dec in modes)
    thump=np.sin(2*np.pi*(150-60*np.minimum(1,t/.06))*t)*np.exp(-t/.05)*.9
    hit=lowpass(noise(d),3500)*np.exp(-t/.006)*.7
    x=tailfade(y*.6+thump+hit,.3)
    return finish(add_reverb(x,.12,.5,.3,3)[:int(.55*SR)],.8)
def sfx_lock():
    x=np.zeros(int(.8*SR))
    place(x,bell(hz(76),.35,.6),0); place(x,bell(hz(83),.4,.7),.075)
    t=t_(.05); place(x,tailfade(bandpass(noise(.05),3000,8000)*np.exp(-t/.006)*.25),0)
    return finish(add_reverb(x,.18,.8,.5,4)[:int(.7*SR)],.6)
def sfx_reveal():
    d=1.6; x=np.zeros(int(d*SR)); t=t_(.7)
    w=noise(.7); y=np.zeros_like(w); seg=1024
    for i in range(0,len(w),seg):
        k=i/len(w); y[i:i+seg]=bandpass(w[i:i+seg] if len(w[i:i+seg])==seg else np.pad(w[i:i+seg],(0,seg-len(w[i:i+seg]))),300+2500*k,900+5000*k)[:len(y[i:i+seg])]
    y*=np.sin(np.pi*np.minimum(1,t/.7))**2*.5
    place(x,lowpass(y,7000),0)
    for i,n in enumerate([72,76,79,84]): place(x,bell(hz(n),1.0,.35,.8),.45+i*.05)
    return finish(add_reverb(x,.3,1.6,1.1,5)[:int(2.4*SR)],.75)
def sfx_move():
    d=.55; t=t_(d); w=noise(d); y=np.zeros_like(w); seg=1024
    for i in range(0,len(w),seg):
        k=i/len(w); blk=w[i:i+seg]
        if len(blk)<seg: blk=np.pad(blk,(0,seg-len(blk)))
        y[i:i+seg]=bandpass(blk,600+900*k,1800+2500*k)[:len(y[i:i+seg])]
    y*=np.sin(np.pi*t/d)**1.5
    return finish(add_reverb(y,.15,.6,.4,6)[:int(.75*SR)],.45)
def chord_sustain(notes,start,d,amp,buf,bright=.7):
    for n in notes: place(buf,pad(hz(n),d,amp,.05,d*.6),start)
def sfx_win():
    x=np.zeros(int(3.2*SR))
    for i,n in enumerate([72,76,79,84]): place(x,pluck(hz(n),1.2,.7,.6),i*.11); place(x,bell(hz(n),1.0,.35),i*.11)
    chord_sustain([60,64,67,72,76],.48,2.2,.18,x)
    for i,n in enumerate([88,91,96]): place(x,bell(hz(n),1.2,.18,.6),.55+i*.07)
    return finish(add_reverb(x,.35,2.2,1.6,7)[:int(4*SR)],.85)
def sfx_lose():
    x=np.zeros(int(2.6*SR))
    for i,n in enumerate([67,63,60]): place(x,lowpass(pluck(hz(n),1.3,.7,1.2),2500),i*.22)
    chord_sustain([48,55,60,63],.66,1.8,.16,x)
    return finish(add_reverb(x,.35,2.0,1.4,8)[:int(3.4*SR)],.7)
def sfx_draw():
    x=np.zeros(int(2.2*SR))
    place(x,pluck(hz(72),1.0,.6,.8),0); place(x,bell(hz(72),.8,.25),0); place(x,pluck(hz(67),1.0,.6,.8),.18); place(x,bell(hz(67),.8,.25),.18)
    chord_sustain([55,60,62,67],.36,1.4,.15,x)
    return finish(add_reverb(x,.3,1.8,1.3,9)[:int(3*SR)],.7)
def sfx_unlock():
    x=np.zeros(int(2.2*SR))
    for i,n in enumerate([79,83,86,91,95,98]): place(x,bell(hz(n),.9,.5,1.0),i*.055)
    t=t_(1.0); sh=bandpass(noise(1.0),5000,12000)*np.exp(-t/.35)*np.minimum(1,t/.05)*.12
    place(x,tailfade(sh),.1); chord_sustain([67,71,74,79],.3,1.4,.12,x)
    return finish(add_reverb(x,.35,1.8,1.3,10)[:int(2.8*SR)],.8)
def sfx_reward():
    x=np.zeros(int(1.0*SR))
    place(x,bell(hz(88),.7,.7,1.1),0); place(x,bell(hz(95),.8,.6,1.1),.08)
    return finish(add_reverb(x,.25,1.2,.8,11)[:int(1.4*SR)],.7)
def sfx_select():
    x=np.zeros(int(.5*SR)); t=t_(.06)
    place(x,tailfade(np.sin(2*np.pi*(400+600*t/.06)*t)*np.exp(-t/.02)*.5),0)
    place(x,bell(hz(81),.4,.6),.02)
    return finish(add_reverb(x,.15,.8,.5,12)[:int(.7*SR)],.6)
def sfx_error():
    x=np.zeros(int(.45*SR))
    for s in (0,.16):
        t=t_(.12); f=196
        y=(np.sin(2*np.pi*f*t)+.3*np.sin(2*np.pi*f*2*t)+.15*np.sin(2*np.pi*f*3*t))*np.exp(-t/.06)*np.minimum(1,t/.004)
        place(x,tailfade(lowpass(y,1500)),s)
    return finish(x,.55)
def sfx_notify():
    x=np.zeros(int(1.0*SR))
    place(x,bell(hz(79),.6,.6),0); place(x,bell(hz(86),.8,.6),.12)
    return finish(add_reverb(x,.25,1.2,.8,13)[:int(1.3*SR)],.65)

# ---------------- MUSIC (seamless loops via circular placement + circular reverb) ----------------
def circ_reverb(x,wet,decay,seed):
    ir=reverb_ir(min(3.0,len(x)/SR*.5),decay,seed); ir=np.pad(ir,(0,len(x)-len(ir)))
    return x*(1-wet*.4)+np.fft.irfft(np.fft.rfft(x)*np.fft.rfft(ir),len(x))*wet
def music(bpm,prog,bars_per_chord,style,seed):
    global rng; rng=np.random.default_rng(seed)
    beat=60/bpm; bar=beat*4; total=bar*bars_per_chord*len(prog)
    n=int(round(total*SR))
    L=np.zeros(n); R=np.zeros(n)
    for ci,(root,quality) in enumerate(prog):
        st=ci*bars_per_chord*bar; dur=bars_per_chord*bar
        third=3 if quality=='m' else 4
        tones=[root,root+7,root+12,root+12+third,root+19]
        # pad (stereo spread)
        for k,tn in enumerate([root+12,root+12+third,root+19,root+24]):
            p=pad(hz(tn),dur+1.6,.075,a=1.2,r=1.6)
            pan=.5+.35*(-1)**k
            place(L,p*(1-pan)*1.4,st,True); place(R,p*pan*1.4,st,True)
        # bass
        bass=pad(hz(root-12),dur,.16,a=.3,r=.6); bass=lowpass(bass,400)
        place(L,bass,st,True); place(R,bass,st,True)
        # arpeggio
        steps=int(dur/(beat/2))
        pattern=[0,2,1,3,4,3,2,1] if style=='menu' else [0,3,2,3,1,3,2,4]
        for s in range(steps):
            if style=='menu' and s%2==1 and rng.random()<.45: continue
            tn=tones[pattern[s%8]]+12
            amp=.12 if style=='menu' else .1
            if s%4==0: amp*=1.25
            sig=pluck(hz(tn),beat*2.2,amp,1.0 if style=='menu' else 1.3)
            pan=.5+.3*np.sin(s*.9)
            place(L,sig*(1-pan)*1.4,st+s*beat/2,True); place(R,sig*pan*1.4,st+s*beat/2,True)
        # sparkle bells (menu) / soft shaker + pulse (game)
        if style=='menu':
            for b in range(bars_per_chord):
                if rng.random()<.6:
                    tn=tones[rng.integers(2,5)]+24; s=bell(hz(tn),2.0,.05,.5)
                    pos=st+b*bar+beat*rng.integers(1,4); pan=rng.uniform(.2,.8)
                    place(L,s*(1-pan)*1.4,pos,True); place(R,s*pan*1.4,pos,True)
        else:
            for s in range(int(dur/(beat/2))):
                tt=t_(.07); hit=tailfade(bandpass(rng.standard_normal(len(tt)),5000,11000)*np.exp(-tt/.015)*(.022 if s%2 else .011))
                place(L,hit*.8,st+s*beat/2,True); place(R,hit*1.2,st+s*beat/2,True)
            for b in range(bars_per_chord*4):
                tt=t_(.35); k=tailfade(np.sin(2*np.pi*(hz(root-12)*2)*tt)*np.exp(-tt/.12)*.07)
                place(L,k,st+b*beat,True); place(R,k,st+b*beat,True)
    L=circ_reverb(L,.45,2.2,21); R=circ_reverb(R,.45,2.2,22)
    m=max(np.abs(L).max(),np.abs(R).max()); L,R=L/m*.8,R/m*.8
    return np.vstack([L,R])

sfx={'Click':sfx_click,'Back':sfx_back,'Place':sfx_place,'Lock':sfx_lock,'Reveal':sfx_reveal,'Move':sfx_move,
     'Win':sfx_win,'Lose':sfx_lose,'Draw':sfx_draw,'Unlock':sfx_unlock,'Reward':sfx_reward,'Select':sfx_select,'Error':sfx_error,'Notify':sfx_notify}
import os; os.makedirs('out',exist_ok=True)
for k,f in sfx.items(): write(f'out/{k}.wav',f())
# Menu: A minor, calm — Am F C G | Am F G E(m->major V feel as m to keep simple)
menu=music(72,[(57,'m'),(53,''),(48,''),(55,''),(57,'m'),(53,''),(55,''),(52,'')],2,'menu',31)
write('out/MenuMusic.wav',menu,True)
game=music(88,[(50,'m'),(46,''),(53,''),(48,''),(50,'m'),(46,''),(48,''),(45,'')],2,'game',41)
write('out/GameMusic.wav',game,True)
