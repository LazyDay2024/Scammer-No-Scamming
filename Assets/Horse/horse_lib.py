
import bpy, math, random, os
import numpy as np
from mathutils import Vector

ROOT=os.path.join(os.path.expanduser('~'),'Documents','HORSE')
TEXDIR=os.path.join(ROOT,'textures')
os.makedirs(TEXDIR,exist_ok=True)
pi=math.pi; rad=math.radians; cos=math.cos; sin=math.sin

# ---------------- layout ----------------
R_PLAZA=70.0; R_PLAZA_ROAD=80.0
RING_INNER=(80.0,171.0); RING_MID=(182.0,319.0); RING_OUT=(332.0,449.0); R_EDGE=466.0
SP_INNER=[0,45,90,135,180,225,270,315]
SP_MO=[0,22.5,67.5,90,112.5,157.5,180,202.5,247.5,270,292.5,337.5]
def spoke_w(a):
    a=a%360
    return 16.0 if min(abs(a-x) for x in (0,90,180,270,360))<1e-6 else 9.0
TRAIN_R=176.5; DECK_Z=12.0; STATIONS=[0.0,180.0]
QUEST={'Vet':(106,160,46,38,'in'),'Auction':(223,45,68,68,'in'),'Police':(206,214,44,40,'in'),
       'Stable':(212,233,52,42,'in'),'WeaponShop':(140,28,26,16,'out')}
def lot(name):
    r,a,w,d,f=QUEST[name]; ar=rad(a)
    rot=ar-pi/2 if f=='in' else ar+pi/2
    return (r*cos(ar),r*sin(ar)),rot,w,d
def l2w(c,rot,x,y):
    cs,sn=cos(rot),sin(rot); return (c[0]+x*cs-y*sn,c[1]+x*sn+y*cs)
def w2l(c,rot,X,Y):
    dx,dy=X-c[0],Y-c[1]; cs,sn=cos(rot),sin(rot); return (dx*cs+dy*sn,-dx*sn+dy*cs)
def _excl():
    out=[]
    for k in QUEST:
        c,rot,w,d=lot(k); out.append((c,rot,w/2+3,d/2+3))
    for a in STATIONS:
        ar=rad(a)
        for rr in (163.0,190.0):
            c=(rr*cos(ar),rr*sin(ar)); out.append((c,ar-pi/2,34,8))
    return out
EXCL=_excl()
def excluded(x,y,pad=0.0):
    for c,rot,hw,hd in EXCL:
        lx,ly=w2l(c,rot,x,y)
        if abs(lx)<hw+pad and abs(ly)<hd+pad: return True
    return False

# ---------------- collections ----------------
def coll(name,parent=None):
    c=bpy.data.collections.get(name)
    if c is None:
        c=bpy.data.collections.new(name)
        (parent or bpy.context.scene.collection).children.link(c)
    return c

# ---------------- mesh builder ----------------
def newell(pts):
    nx=ny=nz=0.0; k=len(pts)
    for i in range(k):
        x1,y1,z1=pts[i]; x2,y2,z2=pts[(i+1)%k]
        nx+=(y1-y2)*(z1+z2); ny+=(z1-z2)*(x1+x2); nz+=(x1-x2)*(y1+y2)
    l=math.sqrt(nx*nx+ny*ny+nz*nz) or 1.0
    return (nx/l,ny/l,nz/l)
def basis(n):
    if abs(n[2])>0.7: return (1.0,0.0,0.0),((0.0,1.0,0.0) if n[2]>0 else (0.0,-1.0,0.0))
    ux,uy=-n[1],n[0]; l=math.hypot(ux,uy) or 1.0
    return (ux/l,uy/l,0.0),(0.0,0.0,1.0)
def dot(a,b): return a[0]*b[0]+a[1]*b[1]+a[2]*b[2]
def cellrect(idx,grid=8,px=2048,pad=6):
    cx,cy=idx%grid,idx//grid; e=pad/px
    return (cx/grid+e,cy/grid+e,(cx+1)/grid-e,(cy+1)/grid-e)
def rect(cx,cy,w,d,rot=0.0):
    c,s=cos(rot),sin(rot)
    return [(cx+x*c-y*s,cy+x*s+y*c) for x,y in ((-w/2,-d/2),(w/2,-d/2),(w/2,d/2),(-w/2,d/2))]
def arect(x0,y0,x1,y1): return [(x0,y0),(x1,y0),(x1,y1),(x0,y1)]
def ngon(cx,cy,r,n,rot=0.0,sx=1.0,sy=1.0):
    return [(cx+cos(rot+2*pi*i/n)*r*sx,cy+sin(rot+2*pi*i/n)*r*sy) for i in range(n)]

class MB:
    def __init__(s,name):
        s.name=name; s.V=[]; s.F=[]; s.UV=[]; s.M=[]; s.mats=[]
    def mi(s,m):
        if m not in s.mats: s.mats.append(m)
        return s.mats.index(m)
    def face(s,pts,uvs,mat):
        b=len(s.V); s.V.extend([tuple(map(float,p)) for p in pts]); s.F.append(tuple(range(b,b+len(pts)))); s.UV.extend(uvs); s.M.append(s.mi(mat))
    def tris(s): return sum(len(f)-2 for f in s.F)
    def _orient(s,pts,want):
        n=newell(pts)
        if want is not None and dot(n,want)<0: return pts[::-1],(-n[0],-n[1],-n[2]),True
        return pts,n,False
    def fw(s,pts,mat,su=4.0,sv=None,uo=0.0,vo=0.0,want=None):
        pts=[tuple(p) for p in pts]; pts,n,_=s._orient(pts,want); u,v=basis(n); sv=sv or su
        s.face(pts,[(dot(p,u)/su+uo,dot(p,v)/sv+vo) for p in pts],mat)
    def fu(s,pts,uvs,mat,want=None):
        pts=[tuple(p) for p in pts]
        if want is not None and dot(newell(pts),want)<0: pts=pts[::-1]; uvs=list(uvs)[::-1]
        s.face(pts,uvs,mat)
    def fc(s,pts,cell,mat='M_Props_Atlas',grid=8,px=2048,want=None,sub=(0,0,1,1),flipu=False):
        pts=[tuple(p) for p in pts]; pts,n,_=s._orient(pts,want); u,v=basis(n)
        pu=[dot(p,u) for p in pts]; pv=[dot(p,v) for p in pts]
        u0,u1,v0,v1=min(pu),max(pu),min(pv),max(pv); du=(u1-u0) or 1.0; dv=(v1-v0) or 1.0
        a0,b0,a1,b1=cellrect(cell,grid,px)
        ca0=a0+(a1-a0)*sub[0]; ca1=a0+(a1-a0)*sub[2]; cb0=b0+(b1-b0)*sub[1]; cb1=b0+(b1-b0)*sub[3]
        uvs=[]
        for i in range(len(pts)):
            fu_=(pu[i]-u0)/du
            if flipu: fu_=1-fu_
            uvs.append((ca0+fu_*(ca1-ca0),cb0+(pv[i]-v0)/dv*(cb1-cb0)))
        s.face(pts,uvs,mat)
    def prism(s,poly,z0,z1,side,top='M_Concrete',su=4.0,sv=None,uo=0.0,vo=0.0,cap=True,bottom=False,
              top_poly=None,side_cell=None,top_cell=None,tsu=4.0,vfit=False,vscale=1.0,side_mat='M_Props_Atlas'):
        k=len(poly); tp=top_poly or poly; cum=0.0; sv=sv or su
        for i in range(k):
            a=poly[i]; b=poly[(i+1)%k]; ta=tp[i]; tb=tp[(i+1)%k]
            L=math.hypot(b[0]-a[0],b[1]-a[1])
            pts=[(a[0],a[1],z0),(b[0],b[1],z0),(tb[0],tb[1],z1),(ta[0],ta[1],z1)]
            if side_cell is not None: s.fc(pts,side_cell,side_mat)
            else:
                vt=vscale if vfit else (z1-z0)/sv
                s.face(pts,[(cum/su+uo,vo),((cum+L)/su+uo,vo),((cum+L)/su+uo,vo+vt),(cum/su+uo,vo+vt)],side)
            cum+=L
        if cap:
            pts=[(p[0],p[1],z1) for p in tp]
            if top_cell is not None: s.fc(pts,top_cell,side_mat,want=(0,0,1))
            else: s.fw(pts,top,tsu,want=(0,0,1))
        if bottom:
            pts=[(p[0],p[1],z0) for p in poly]
            if top_cell is not None: s.fc(pts,top_cell,side_mat,want=(0,0,-1))
            else: s.fw(pts,top,tsu,want=(0,0,-1))
    def box(s,x0,y0,z0,x1,y1,z1,mat,top=None,su=4.0,**kw):
        s.prism(arect(x0,y0,x1,y1),z0,z1,mat,top or mat,su=su,**kw)
    def cbox(s,x0,y0,z0,x1,y1,z1,cell,top_cell=None,bottom=False):
        s.prism(arect(x0,y0,x1,y1),z0,z1,None,side_cell=cell,top_cell=cell if top_cell is None else top_cell,bottom=bottom)
    def rcbox(s,cx,cy,z0,w,d,h,rot,cell,top_cell=None,bottom=False):
        s.prism(rect(cx,cy,w,d,rot),z0,z0+h,None,side_cell=cell,top_cell=cell if top_cell is None else top_cell,bottom=bottom)
    def cyl(s,cx,cy,z0,z1,r,n,cell,r2=None,cap=True,bottom=False):
        s.prism(ngon(cx,cy,r,n),z0,z1,None,side_cell=cell,top_cell=cell,top_poly=ngon(cx,cy,r2,n) if r2 is not None else None,cap=cap,bottom=bottom)
    def beam(s,p0,p1,t,cell,tz=None,ends=False):
        a=Vector(p0); b=Vector(p1); d=b-a; L=d.length
        if L<1e-6: return
        d.normalize(); up=Vector((0,0,1)) if abs(d.z)<0.95 else Vector((1,0,0))
        x=d.cross(up).normalized(); y=x.cross(d).normalized(); tz=tz or t
        cs=[(-t/2,-tz/2),(t/2,-tz/2),(t/2,tz/2),(-t/2,tz/2)]
        A=[a+x*u+y*v for u,v in cs]; B=[b+x*u+y*v for u,v in cs]; mid=(a+b)/2
        for i in range(4):
            j=(i+1)%4; fm=(A[i]+A[j]+B[i]+B[j])/4
            s.fc([tuple(A[i]),tuple(A[j]),tuple(B[j]),tuple(B[i])],cell,want=tuple(fm-mid))
        if ends:
            s.fc([tuple(p) for p in A],cell,want=tuple(-d)); s.fc([tuple(p) for p in B],cell,want=tuple(d))
    def wheel(s,cx,cy,cz,r,t,n,cell,rot=0.0):
        # disk standing vertical, axle along local y rotated by rot
        ax=(-sin(rot),cos(rot)); tx=(cos(rot),sin(rot))
        ring=[(cx+tx[0]*cos(2*pi*i/n)*r,cy+tx[1]*cos(2*pi*i/n)*r,cz+sin(2*pi*i/n)*r) for i in range(n)]
        A=[(p[0]-ax[0]*t/2,p[1]-ax[1]*t/2,p[2]) for p in ring]; B=[(p[0]+ax[0]*t/2,p[1]+ax[1]*t/2,p[2]) for p in ring]
        for i in range(n):
            j=(i+1)%n; m=((ring[i][0]+ring[j][0])/2-cx,(ring[i][1]+ring[j][1])/2-cy,(ring[i][2]+ring[j][2])/2-cz)
            s.fc([A[i],A[j],B[j],B[i]],cell,want=m)
        s.fc(A,cell,want=(-ax[0],-ax[1],0)); s.fc(B,cell,want=(ax[0],ax[1],0))
    def gable(s,x0,y0,x1,y1,ze,ph,oh=0.6,axis='x',roof='M_RoofTile',gmat='M_HalfTimber',under_cell=5,ends=True,inner_ends=True):
        if axis=='x':
            P=lambda a,b,z:(a,b,z); A0,A1,B0,B1=x0,x1,y0,y1
        else:
            P=lambda a,b,z:(b,a,z); A0,A1,B0,B1=y0,y1,x0,x1
        half=(B1-B0)/2; bm=(B0+B1)/2; zr=ze+ph; zoe=ze-oh*ph/half
        sl=math.hypot(half+oh,ph+oh*ph/half)/3.0
        p1=[P(A0-oh,B0-oh,zoe),P(A1+oh,B0-oh,zoe),P(A1+oh,bm,zr),P(A0-oh,bm,zr)]
        u1=[((A0-oh)/3,0),((A1+oh)/3,0),((A1+oh)/3,sl),((A0-oh)/3,sl)]
        p2=[P(A1+oh,B1+oh,zoe),P(A0-oh,B1+oh,zoe),P(A0-oh,bm,zr),P(A1+oh,bm,zr)]
        u2=[(-(A1+oh)/3,0),(-(A0-oh)/3,0),(-(A0-oh)/3,sl),(-(A1+oh)/3,sl)]
        w1=P(0,-1,1); w2=P(0,1,1)
        s.fu(p1,u1,roof,want=w1); s.fu(p2,u2,roof,want=w2)
        s.fc(p1,under_cell,want=(-w1[0],-w1[1],-w1[2])); s.fc(p2,under_cell,want=(-w2[0],-w2[1],-w2[2]))
        if ends:
            for A,sg in ((A0,-1),(A1,1)):
                tri=[P(A,B0,ze),P(A,B1,ze),P(A,bm,zr)]
                s.fw(tri,gmat,4.0,want=P(sg,0,0))
                if inner_ends: s.fc(tri,under_cell,want=P(-sg,0,0))
    def pyramid(s,cx,cy,w,d,ze,h,oh=0.4,roof='M_RoofTile',n=4):
        if n==4: poly=arect(cx-w/2-oh,cy-d/2-oh,cx+w/2+oh,cy+d/2+oh)
        else: poly=ngon(cx,cy,w/2+oh,n,pi/n)
        ap=(cx,cy,ze+h)
        for i in range(len(poly)):
            a=poly[i]; b=poly[(i+1)%len(poly)]
            tri=[(a[0],a[1],ze-0.2),(b[0],b[1],ze-0.2),ap]
            mx=(a[0]+b[0])/2-cx; my=(a[1]+b[1])/2-cy
            s.fw(tri,roof,3.0,want=(mx,my,0.5))
    def wall(s,a,b,t,h,ext=None,inn='M_Plaster',openings=(),z0=0.0,center=(0,0),su=4.0,glass=True,sv=None):
        ax,ay=a; bx,by=b; L=math.hypot(bx-ax,by-ay); dx,dy=(bx-ax)/L,(by-ay)/L; nx,ny=-dy,dx
        mx,my=(ax+bx)/2-center[0],(ay+by)/2-center[1]
        ops=sorted(openings); pieces=[]; cur=0.0
        for (p,w,zb,zt) in ops:
            s0=p-w/2; s1=p+w/2
            if s0>cur+1e-4: pieces.append((cur,s0,0.0,h))
            if zb>1e-4: pieces.append((s0,s1,0.0,zb))
            if zt<h-1e-4: pieces.append((s0,s1,zt,h))
            if glass and zb>0.05:
                for sg in (1,-1):
                    q=[(ax+dx*s0,ay+dy*s0,z0+zb),(ax+dx*s1,ay+dy*s1,z0+zb),(ax+dx*s1,ay+dy*s1,z0+zt),(ax+dx*s0,ay+dy*s0,z0+zt)]
                    s.fc(q,23,want=(nx*sg,ny*sg,0))
            cur=s1
        if cur<L-1e-4: pieces.append((cur,L,0.0,h))
        for (s0,s1,za,zb) in pieces:
            P0=(ax+dx*s0,ay+dy*s0); P1=(ax+dx*s1,ay+dy*s1)
            lo=[(P0[0]-nx*t/2,P0[1]-ny*t/2),(P1[0]-nx*t/2,P1[1]-ny*t/2)]
            hi=[(P0[0]+nx*t/2,P0[1]+ny*t/2),(P1[0]+nx*t/2,P1[1]+ny*t/2)]
            Za=z0+za; Zb=z0+zb
            for side,sg in ((lo,-1),(hi,1)):
                q=[(side[0][0],side[0][1],Za),(side[1][0],side[1][1],Za),(side[1][0],side[1][1],Zb),(side[0][0],side[0][1],Zb)]
                isext = ext is not None and (nx*sg*mx+ny*sg*my)>0
                s.fw(q,ext if isext else inn,su,sv,want=(nx*sg,ny*sg,0))
            s.fw([(lo[0][0],lo[0][1],Zb),(lo[1][0],lo[1][1],Zb),(hi[1][0],hi[1][1],Zb),(hi[0][0],hi[0][1],Zb)],inn,su,want=(0,0,1))
            if za>1e-4: s.fw([(lo[0][0],lo[0][1],Za),(lo[1][0],lo[1][1],Za),(hi[1][0],hi[1][1],Za),(hi[0][0],hi[0][1],Za)],inn,su,want=(0,0,-1))
            for (pa,pb,sg) in ((lo[0],hi[0],-1),(lo[1],hi[1],1)):
                q=[(pa[0],pa[1],Za),(pb[0],pb[1],Za),(pb[0],pb[1],Zb),(pa[0],pa[1],Zb)]
                s.fw(q,inn,su,want=(dx*sg,dy*sg,0))
    def mesh(s,name,rot=0.0):
        V=s.V
        if rot:
            c,sn=cos(rot),sin(rot); V=[(x*c-y*sn,x*sn+y*c,z) for x,y,z in V]
        me=bpy.data.meshes.new(name); me.from_pydata(V,[],s.F)
        uvl=me.uv_layers.new(name='UVMap'); uvl.data.foreach_set('uv',[c for uv in s.UV for c in uv])
        for m in s.mats: me.materials.append(bpy.data.materials[m])
        me.polygons.foreach_set('material_index',s.M); me.update()
        return me
    def build(s,coll_,name=None,loc=None,rot=0.0,recenter=False):
        if not s.F: return None
        name=name or s.name
        if recenter and loc is None:
            V=s.V
            if rot:
                c,sn=cos(rot),sin(rot); V=[(x*c-y*sn,x*sn+y*c,z) for x,y,z in V]
            xs=[v[0] for v in V]; ys=[v[1] for v in V]; off=((min(xs)+max(xs))/2,(min(ys)+max(ys))/2,0.0)
            s.V=[(x-off[0],y-off[1],z) for x,y,z in V]; rot=0.0; loc=off
        me=s.mesh(name,rot)
        ob=bpy.data.objects.new(name,me); coll_.objects.link(ob)
        if loc is not None: ob.location=loc
        return ob

def inst(me,name,coll_,loc,rot=0.0,parent=None):
    ob=bpy.data.objects.new(name,me); coll_.objects.link(ob); ob.location=loc; ob.rotation_euler=(0,0,rot)
    if parent is not None: ob.parent=parent
    return ob
def lod_inst(meshes,base,coll_,loc,rot=0.0):
    e=bpy.data.objects.new(base,None); coll_.objects.link(e); e.location=loc; e.rotation_euler=(0,0,rot); e.empty_display_size=0.5
    for i,me in enumerate(meshes):
        ob=bpy.data.objects.new(f'{base}_LOD{i}',me); coll_.objects.link(ob); ob.parent=e
        if i>0: ob.hide_set(True)
    return e
def lod_build(mbs,base,coll_):
    # merged chunk with LODs: recenter on LOD0 bounds
    V=mbs[0].V; xs=[v[0] for v in V]; ys=[v[1] for v in V]; off=((min(xs)+max(xs))/2,(min(ys)+max(ys))/2,0.0)
    e=bpy.data.objects.new(base,None); coll_.objects.link(e); e.location=off; e.empty_display_size=2
    for i,mb in enumerate(mbs):
        if not mb.F: continue
        mb.V=[(x-off[0],y-off[1],z) for x,y,z in mb.V]
        ob=bpy.data.objects.new(f'{base}_LOD{i}',mb.mesh(f'{base}_LOD{i}')); coll_.objects.link(ob); ob.parent=e
        if i>0: ob.hide_set(True)
    return e

# ---------------- textures ----------------
RNG=np.random.default_rng(11)
def vnoise(n,cells):
    g=RNG.random((cells,cells)).astype(np.float32)
    x=np.arange(n)*cells/n; i0=np.floor(x).astype(int); f=(x-i0).astype(np.float32); i1=(i0+1)%cells
    f=f*f*(3-2*f)
    rows=g[i0]*(1-f)[:,None]+g[i1]*f[:,None]
    return rows[:,i0]*(1-f)[None,:]+rows[:,i1]*f[None,:]
def fbm(n,base=4,octv=4):
    s_=np.zeros((n,n),np.float32); a=1.0; t=0.0
    for o in range(octv):
        c=base*2**o
        if c>n//2: break
        s_+=vnoise(n,c)*a; t+=a; a*=0.5
    return s_/t
def grain(n,amt): return (RNG.random((n,n)).astype(np.float32)-0.5)*amt
def C(*c): return np.array(c,np.float32)
def canvas(n,c): return np.ones((n,n,3),np.float32)*C(*c)
def shade(a,m): a*=m[:,:,None]
def cslice(idx,grid,n):
    b=n//grid; cx,cy=idx%grid,idx//grid; r0=n-(cy+1)*b; c0=cx*b
    return r0,r0+b,c0,c0+b
def textblocks(a,x0,y0,x1,y1,c,lines=1):
    h=(y1-y0)/lines; col=C(*c)
    for l in range(lines):
        yy0=int(y0+l*h+h*0.15); yy1=int(y0+(l+1)*h-h*0.15); x=int(x0)
        while x<x1-6:
            w=int(RNG.integers(5,14))
            if x+w>x1: break
            top=yy0+int(RNG.integers(0,max(1,(yy1-yy0)//3)))
            a[top:yy1,x:x+w]=col
            if RNG.random()<0.4: a[yy0:yy0+3,x:x+w]=col
            x+=w+int(RNG.integers(2,6))
            if RNG.random()<0.1: x+=10
def save_img(name,a):
    h,w=a.shape[:2]; a=np.clip(a,0,1)
    rgba=np.dstack([a,np.ones((h,w),np.float32)]); rgba=np.ascontiguousarray(np.flipud(rgba)).astype(np.float32)
    img=bpy.data.images.get(name)
    if img: bpy.data.images.remove(img)
    img=bpy.data.images.new(name,w,h,alpha=False); img.pixels.foreach_set(rgba.ravel())
    p=os.path.join(TEXDIR,name+'.png'); img.filepath_raw=p; img.file_format='PNG'; img.save(); img.filepath=p
    return img

def t_asphalt(n=1024):
    a=canvas(n,(0.25,0.25,0.26)); shade(a,0.82+0.3*fbm(n,4,5)+grain(n,0.12))
    for k in range(12):
        x,y=[float(v) for v in RNG.integers(0,n,2)]; ang=RNG.random()*6.28
        for st in range(int(RNG.integers(60,220))):
            ang+=RNG.normal(0,0.35); x=(x+cos(ang)*2)%n; y=(y+sin(ang)*2)%n
            a[int(y),int(x)]*=0.45; a[int(y),(int(x)+1)%n]*=0.6
    for k in range(3):
        x,y=RNG.integers(0,n-220,2); w,h=RNG.integers(80,200,2); a[y:y+h,x:x+w]*=0.86
    return a
def t_sidewalk(n=1024,t=6):
    a=canvas(n,(0.6,0.58,0.56)); s_=n//t
    for i in range(t):
        for j in range(t):
            c=C(0.66,0.56,0.53) if (i+j)%2 else C(0.63,0.62,0.61)
            a[i*s_:(i+1)*s_,j*s_:(j+1)*s_]=c*(0.93+0.12*RNG.random())
    shade(a,0.85+0.25*fbm(n,4,4)+grain(n,0.06))
    for i in range(t+1):
        k=min(i*s_,n); a[max(k-2,0):min(k+2,n),:]*=0.62; a[:,max(k-2,0):min(k+2,n)]*=0.62
    return a
def t_plain(c,n=1024,amt=0.25,g=0.05,base=4):
    a=canvas(n,c); shade(a,1-amt/2+amt*fbm(n,base,5)+grain(n,g)); return a
def t_glass(n=1024):
    a=np.zeros((n,n,3),np.float32); b=n//4
    for i in range(4):
        for j in range(4):
            a[i*b:(i+1)*b,j*b:(j+1)*b]=C(0.22,0.34,0.44)*(0.82+0.32*RNG.random())
    yy,xx=np.mgrid[0:n,0:n].astype(np.float32)
    shade(a,0.86+0.18*np.sin((xx+yy)/n*2*pi)+grain(n,0.03))
    for i in range(4): a[i*b:i*b+24,:]=C(0.17,0.2,0.24)
    for j in range(8):
        x=j*b//2; w=6 if j%2==0 else 3; a[:,max(x-w//2,0):x+w//2+1]=C(0.72,0.74,0.76)
    return a
def t_condo(wall,win,band,n=1024):
    a=canvas(n,wall); shade(a,0.92+0.1*fbm(n,4,4)+grain(n,0.03)); b=n//4
    for i in range(4):
        for j in range(4):
            x0=j*b+int(b*0.16); x1=j*b+int(b*0.84); y0=i*b+int(b*0.14); y1=i*b+int(b*0.72)
            a[y0:y1,x0:x1]=C(*win)*(0.8+0.5*RNG.random())
            a[y0:y1,(x0+x1)//2-2:(x0+x1)//2+2]=C(0.75,0.75,0.75)
            a[y1:y1+16,j*b+6:(j+1)*b-6]=C(*band)
            a[y1-36:y1,j*b+8:(j+1)*b-8:10]=C(*band)*0.9
            a[y1-38:y1-34,j*b+8:(j+1)*b-8]=C(*band)*0.9
            if RNG.random()<0.5:
                ax_=j*b+int(b*0.84)+4; a[y1-30:y1-4,ax_:ax_+22]=C(0.82,0.82,0.8); a[y1-26:y1-8:3,ax_+3:ax_+19]=C(0.4,0.4,0.4)
    st=np.repeat(vnoise(n,64)[0:1,:],n,axis=0); shade(a,0.95+0.08*st)
    return a
def t_office(n=1024):
    a=canvas(n,(0.77,0.77,0.75)); b=n//4
    for i in range(4):
        y0=i*b+70; y1=i*b+b-10; a[y0:y1,:]=C(0.17,0.27,0.31)
        for j in range(16):
            x=j*n//16; a[y0:y1,x+4:x+n//16]*=(0.85+0.3*RNG.random()); a[y0:y1,x:x+4]=C(0.55,0.58,0.6)
    shade(a,0.92+0.12*fbm(n,4,3)+grain(n,0.03)); return a
def t_shophouse(n=1024):
    a=np.zeros((n,n,3),np.float32); bw=n//4; gf=int(n*0.35)
    walls=[(0.87,0.82,0.68),(0.78,0.84,0.80),(0.90,0.78,0.68),(0.76,0.76,0.75)]
    signs=[(0.8,0.1,0.1),(0.95,0.75,0.1),(0.1,0.45,0.25),(0.12,0.3,0.7),(0.9,0.45,0.1)]
    for j in range(4):
        x0=j*bw; x1=x0+bw; wc=C(*walls[j]); a[:,x0:x1]=wc; fh=(n-gf)//2
        for f in range(2):
            y0=f*fh
            a[y0+60:y0+fh-50,x0+30:x1-30]=C(0.18,0.2,0.22)*(0.9+0.4*RNG.random())
            a[y0+60:y0+fh-50:14,x0+30:x1-30]=C(0.33,0.31,0.29); a[y0+60:y0+fh-50,x0+30:x1-30:14]=C(0.33,0.31,0.29)
            a[y0+fh-50:y0+fh-38,x0+20:x1-20]=C(0.66,0.66,0.63)
        sy=n-gf-56; sc=C(*signs[int(RNG.integers(0,len(signs)))]); a[sy:sy+56,x0+6:x1-6]=sc
        textblocks(a,x0+16,sy+8,x1-16,sy+48,(0.97,0.97,0.95) if sc.sum()<1.8 else (0.1,0.1,0.1))
        gy=n-gf+4
        if j%2==0:
            a[gy:n,x0+10:x1-10]=C(0.52,0.53,0.54); a[gy:n:8,x0+10:x1-10]=C(0.4,0.41,0.42)
        else:
            a[gy:n,x0+10:x1-10]=C(0.1,0.09,0.08)
            for k in range(30):
                gx=int(RNG.integers(x0+14,x1-30)); gy2=int(RNG.integers(gy+20,n-20)); a[gy2:gy2+16,gx:gx+16]=C(*RNG.random(3))*0.8+0.15
        a[:,x0:x0+10]=wc*0.85; a[0:18,x0:x1]=wc*0.8
    st=np.repeat(vnoise(n,48)[0:1,:],n,axis=0); yy=np.linspace(1,0,n,dtype=np.float32)[:,None]
    shade(a,1-0.14*st*yy+grain(n,0.03)); shade(a,0.9+0.12*fbm(n,4,4)); return a
def t_halftimber(n=1024):
    a=canvas(n,(0.93,0.91,0.86)); shade(a,0.9+0.12*fbm(n,4,4)+grain(n,0.03))
    yy,xx=np.mgrid[0:n,0:n].astype(np.float32); w=26; m=np.zeros((n,n),bool)
    for x in (0,n//2,n-1): m|=np.abs(xx-x)<w/2
    for y in (0,n//2,n-1): m|=np.abs(yy-y)<w/2
    q=n/2
    for qx,qy,dr in ((0,0,1),(q,0,-1),(0,q,-1),(q,q,1)):
        lx=xx-qx; ly=yy-qy; ins=(lx>=0)&(lx<q)&(ly>=0)&(ly<q)
        d=np.abs(ly-lx) if dr==1 else np.abs(ly-(q-lx)); m|=ins&(d<w*0.7)
    tim=C(0.25,0.16,0.10)*(0.85+0.3*fbm(n,8,3))[:,:,None]; a[m]=tim[m]; return a
def t_rooftile(n=1024):
    a=np.zeros((n,n,3),np.float32); rows=12; rh=n//rows; tw=64
    prof=(0.7+0.3*np.sin(np.arange(tw)/tw*pi)).astype(np.float32)
    for r in range(rows):
        off=(tw//2)*(r%2); y0=r*rh
        for t in range(n//tw+1):
            x0=t*tw-off; col=C(0.62,0.29,0.18)*(0.8+0.35*RNG.random())
            xa=max(x0,0); xb=min(x0+tw,n)
            if xb>xa: a[y0:y0+rh,xa:xb]=col[None,None,:]*prof[None,xa-x0:xb-x0,None]
        a[y0+rh-8:y0+rh,:]*=0.45
    shade(a,0.9+0.15*fbm(n,4,4)+grain(n,0.04)); return a
def t_cobble(n=1024,k=16):
    a=np.zeros((n,n,3),np.float32); s_=n//k
    yy,xx=np.mgrid[0:s_,0:s_].astype(np.float32)
    e=np.minimum(np.minimum(xx,s_-1-xx),np.minimum(yy,s_-1-yy)); prof=np.clip(e/6.0,0,1)
    for i in range(k):
        off=(s_//2)*(i%2)
        for j in range(k+1):
            x0=j*s_-off; c=C(0.52,0.5,0.47)*(0.8+0.35*RNG.random()); tile=c*(0.5+0.5*prof)[:,:,None]
            xa=max(x0,0); xb=min(x0+s_,n)
            if xb>xa: a[i*s_:(i+1)*s_,xa:xb]=tile[:,xa-x0:xb-x0]
    shade(a,0.9+0.15*fbm(n,4,4)+grain(n,0.06)); return a
def t_wood(base=(0.45,0.28,0.15),planks=8,n=1024):
    a=canvas(n,base); yy,xx=np.mgrid[0:n,0:n].astype(np.float32)
    g=np.sin(xx/n*2*pi*planks*6+vnoise(n,8)*12.0)
    shade(a,0.88+0.08*g+0.1*fbm(n,4,3)+grain(n,0.04)); pw=n//planks
    for p in range(planks):
        a[:,p*pw:(p+1)*pw]*=0.9+0.2*RNG.random(); a[:,p*pw:p*pw+3]*=0.55
    a[n//2-2:n//2+2,:]*=0.6; a[0:3,:]*=0.6
    return a
def t_floortile(n=1024,t=4):
    a=canvas(n,(0.86,0.85,0.81)); s_=n//t
    for i in range(t):
        for j in range(t): a[i*s_:(i+1)*s_,j*s_:(j+1)*s_]*=0.96+0.06*RNG.random()
    shade(a,0.95+0.06*fbm(n,4,3))
    for i in range(t+1):
        k=min(i*s_,n); a[max(k-2,0):min(k+2,n),:]=C(0.6,0.6,0.58); a[:,max(k-2,0):min(k+2,n)]=C(0.6,0.6,0.58)
    return a
def t_foliage(n=1024):
    f=fbm(n,8,5); a=C(0.15,0.29,0.09)[None,None,:]*(1-f[:,:,None])+C(0.36,0.52,0.17)[None,None,:]*f[:,:,None]
    shade(a,0.8+0.4*vnoise(n,64)+grain(n,0.1)); return a.astype(np.float32)
def t_screens(n=1024):
    a=np.zeros((n,n,3),np.float32); b=n//4
    yy,xx=np.mgrid[0:b,0:b].astype(np.float32)/b
    pals=[((0.95,0.2,0.5),(0.2,0.3,0.95)),((0.1,0.85,0.75),(0.1,0.2,0.55)),((1,0.6,0.1),(0.85,0.1,0.1)),
          ((0.3,0.95,0.3),(0.05,0.3,0.6)),((0.95,0.9,0.2),(0.9,0.3,0.1)),((0.6,0.2,0.95),(0.1,0.1,0.35))]
    for idx in range(16):
        r0,r1,c0,c1=cslice(idx,4,n)
        if idx in (12,13,14):
            cell=np.ones((b,b,3),np.float32)*0.025; cell+=(0.03*(1-yy))[:,:,None]
        elif idx==15:
            cell=np.ones((b,b,3),np.float32)*C(0.05,0.15,0.55); cell[:]*=(0.8+0.3*yy)[:,:,None]
            cell[int(b*0.12):int(b*0.2),:]=C(0.9,0.9,0.95)*0.4
            textblocks(cell,20,int(b*0.25),b-20,int(b*0.45),(1,1,1)); textblocks(cell,40,int(b*0.5),b-40,int(b*0.8),(1,1,1))
        else:
            p=pals[idx%len(pals)]; t=(xx*0.6+yy*0.4)[:,:,None]
            cell=C(*p[0])*(1-t)+C(*p[1])*t
            cx_,cy_,rr=RNG.random()*0.6+0.2,RNG.random()*0.6+0.2,0.15+RNG.random()*0.15
            m=((xx-cx_)**2+(yy-cy_)**2)<rr*rr; cell[m]=C(1,1,0.9)
            textblocks(cell,16,int(b*0.75),b-16,int(b*0.92),(1,1,1))
        a[r0:r1,c0:c1]=cell
    return a
def t_props(n=2048):
    A=np.zeros((n,n,3),np.float32); b=n//8
    def cell(i): r0,r1,c0,c1=cslice(i,8,n); return A[r0:r1,c0:c1]
    def fill(i,c,amt=0.12):
        v=cell(i); v[:]=C(*c); shade(v,1-amt/2+amt*fbm(b,4,3)+grain(b,0.03)); return v
    def sign(i,bg,tc,lines=2):
        v=fill(i,bg,0.06); v[0:10,:]*=0.6; v[-10:,:]*=0.6; textblocks(v,14,24,b-14,b-24,tc,lines); return v
    fill(0,(0.92,0.92,0.9),0.2); fill(1,(0.95,0.75,0.15)); fill(2,(0.15,0.16,0.17)); fill(3,(0.72,0.74,0.76),0.08)
    for i,c in ((4,(0.62,0.45,0.28)),(5,(0.3,0.18,0.1)),(40+0,(0.5,0.34,0.2))):
        v=fill(i,c); yy,xx=np.mgrid[0:b,0:b].astype(np.float32); shade(v,0.9+0.1*np.sin(xx/6+vnoise(b,4)*8))
        v[:,::32]*=0.6
    v=fill(6,(0.86,0.71,0.36),0.2); v[::5,:]*=0.85; v[:,::7]*=1.08
    fill(7,(0.85,0.12,0.1)); v=cell(7); v[::18,:]*=0.7
    sign(8,(0.8,0.1,0.12),(1,1,1)); sign(9,(0.96,0.78,0.15),(0.1,0.1,0.1)); sign(10,(0.1,0.5,0.25),(1,1,1))
    sign(11,(0.12,0.3,0.72),(1,1,1)); sign(12,(0.92,0.45,0.1),(1,1,1))
    fill(13,(0.85,0.25,0.45))
    v=cell(14); v[:]=C(0.85,0.12,0.12); v[:,np.arange(b)%32<16]=C(0.95,0.95,0.93)
    v=cell(15); v[:]=C(0.12,0.3,0.75); v[:,np.arange(b)%32<16]=C(0.95,0.95,0.93)
    v=fill(16,(0.62,0.63,0.62)); yy,xx=np.mgrid[0:b,0:b].astype(np.float32); shade(v,0.8+0.2*np.sin(xx/4)); v*=0.95
    for i,pal in ((17,[(0.98,0.8,0.1),(0.98,0.5,0.1),(0.95,0.9,0.4)]),(18,[(0.3,0.75,0.2),(0.85,0.15,0.2),(0.5,0.15,0.5)])):
        v=cell(i); v[:]=C(0.25,0.18,0.1)
        for k in range(70):
            x,y=RNG.integers(0,b-30,2); r_=int(RNG.integers(10,22)); col=C(*pal[int(RNG.integers(0,3))])
            yy,xx=np.mgrid[0:2*r_,0:2*r_]; m=((xx-r_)**2+(yy-r_)**2)<r_*r_
            sub=v[y:y+2*r_,x:x+2*r_]; mm=m[:sub.shape[0],:sub.shape[1]]; sub[mm]=col*(0.8+0.3*RNG.random())
    v=fill(19,(0.8,0.12,0.1)); v[::24,:]*=0.6; v[:,::24]*=0.6
    v=fill(20,(0.95,0.78,0.12)); v[::24,:]*=0.7; v[:,::24]*=0.7
    v=fill(21,(0.93,0.93,0.92),0.05); v[int(b*0.62):int(b*0.74),:]=C(0.1,0.3,0.72); v[int(b*0.75):int(b*0.78),:]=C(0.85,0.1,0.1)
    for k in range(6):
        x0=8+k*(b//6); v[int(b*0.2):int(b*0.5),x0:x0+b//6-14]=C(0.12,0.16,0.2)
    v=fill(22,(0.93,0.93,0.92),0.05); v[int(b*0.1):int(b*0.5),20:b-20]=C(0.1,0.13,0.16); v[int(b*0.62):int(b*0.74),:]=C(0.1,0.3,0.72)
    v=fill(23,(0.12,0.18,0.22),0.1); yy=np.linspace(0,1,b,dtype=np.float32); shade(v,np.repeat((0.8+0.5*yy)[:,None],b,1))
    fill(24,(0.05,0.05,0.05)); fill(25,(0.42,0.45,0.44)); fill(26,(0.14,0.3,0.33),0.3); fill(27,(0.15,0.25,0.55)); fill(28,(0.92,0.92,0.9))
    sign(29,(0.05,0.42,0.22),(1,1,1),3)
    v=sign(30,(0.08,0.12,0.38),(0.95,0.8,0.3),2)
    v=fill(31,(0.95,0.96,0.95),0.04); v[b//2-40:b//2+40,b//2-14:b//2+14]=C(0.1,0.6,0.3); v[b//2-14:b//2+14,b//2-40:b//2+40]=C(0.1,0.6,0.3)
    v=sign(32,(0.38,0.05,0.08),(0.95,0.8,0.3),2)
    fill(33,(0.78,0.62,0.25)); fill(34,(0.8,0.74,0.6)); fill(35,(0.33,0.34,0.36),0.08)
    v=fill(36,(0.55,0.26,0.18)); v[::16,:]=C(0.7,0.68,0.62)
    for r in range(0,b,16): v[r:r+16,(r//16%2)*16::32]=C(0.7,0.68,0.62)
    fill(37,(0.6,0.6,0.58)); v=fill(38,(0.53,0.54,0.55)); v[::8,:]*=0.75
    v=cell(39); v[:]=C(0.2,0.2,0.22)
    for k in range(7):
        x0=6+k*36; v[30:b-20,x0:x0+30]=C(*RNG.random(3))*0.8+0.15
    v=cell(41); v[:]=C(0.15,0.4,0.8)
    v=cell(42); v[:]=C(0.2,0.55,0.28)
    v=fill(43,(0.42,0.26,0.14)); v[:,::21]*=0.65; ii=np.arange(b); v[ii,ii]=C(0.25,0.15,0.08); v[ii,b-1-ii]=C(0.25,0.15,0.08); v[0:8,:]*=0.6; v[-8:,:]*=0.6
    fill(44,(0.95,0.95,0.94),0.05); fill(45,(0.2,0.38,0.14),0.3)
    v=fill(46,(0.88,0.72,0.12)); v[::12,::12]*=0.7
    fill(47,(0.48,0.06,0.08),0.1); fill(48,(0.75,0.7,0.62)); fill(49,(0.86,0.9,0.94),0.05); fill(50,(0.4,0.68,0.6))
    fill(51,(0.03,0.03,0.03),0.0)
    v=cell(52); v[:]=C(0.1,0.08,0.06); yy,xx=np.mgrid[0:b,0:b].astype(np.float32)-b/2; rr=np.sqrt(xx*xx+yy*yy)
    v[rr<b*0.42]=C(0.95,0.93,0.86); v[(rr<b*0.42)&(rr>b*0.39)]=C(0.1,0.1,0.1)
    v[b//2-3:b//2+3,b//2:b//2+70]=C(0.05,0.05,0.05); v[b//2-90:b//2,b//2-3:b//2+3]=C(0.05,0.05,0.05)
    fill(53,(0.78,0.79,0.8),0.06); v=cell(53); v[::20,:]*=0.85
    fill(54,(0.62,0.28,0.18)); fill(55,(0.88,0.45,0.1)); fill(56,(0.72,0.56,0.26),0.25); fill(57,(0.24,0.3,0.36),0.05)
    v=cell(58); v[:]=C(0.12,0.1,0.08)
    for k in range(60):
        x,y=RNG.integers(4,b-24,2); v[y:y+18,x:x+14]=C(*RNG.random(3))*0.8+0.15
    for y in range(40,b,50): v[y:y+4,:]=C(0.6,0.6,0.6)
    v=fill(59,(0.88,0.88,0.86),0.05); v[30:b-30:6,30:b-30]*=0.5
    fill(60,(0.86,0.66,0.2))
    v=cell(61); v[:]=C(0.75,0.1,0.15); v[b//6:5*b//6,:]=C(0.97,0.97,0.97); v[b//3:2*b//3,:]=C(0.1,0.12,0.42)
    fill(62,(0.55,0.62,0.3)); fill(63,(0.3,0.3,0.32))
    return A

MATS={}
def make_mat(name,img=None,rough=0.85,metal=0.0,emis=None,es=0.0,color=None):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    try: m.use_nodes=True
    except Exception: pass
    nt=m.node_tree; nt.nodes.clear()
    out=nt.nodes.new('ShaderNodeOutputMaterial'); bs=nt.nodes.new('ShaderNodeBsdfPrincipled'); out.location=(300,0)
    nt.links.new(bs.outputs['BSDF'],out.inputs['Surface'])
    if img is not None:
        tx=nt.nodes.new('ShaderNodeTexImage'); tx.image=img; tx.location=(-350,0)
        nt.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
        if emis=='tex':
            nt.links.new(tx.outputs['Color'],bs.inputs['Emission Color']); bs.inputs['Emission Strength'].default_value=es
    if color is not None: bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=rough; bs.inputs['Metallic'].default_value=metal
    if emis is not None and emis!='tex':
        bs.inputs['Emission Color'].default_value=(*emis,1); bs.inputs['Emission Strength'].default_value=es
    return m
