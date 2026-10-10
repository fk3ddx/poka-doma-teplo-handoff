// Subtle analogue treatment in display colour space; DOM text remains untouched.
export const VHSShader = {
 uniforms: {tDiffuse:{value:null},time:{value:0},resolution:{value:null},amount:{value:.34}},
 vertexShader:`varying vec2 vUv; void main(){vUv=uv;gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.);}`,
 fragmentShader:`
 uniform sampler2D tDiffuse;
 uniform float time, amount;
 uniform vec2 resolution;
 varying vec2 vUv;
 float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
 void main(){
   vec2 uv=vUv;
   float frame=floor(time*24.);
   float line=floor(uv.y*resolution.y*.5);
   float drift=sin(uv.y*19.+time*.8)*.22;
   // Brief tracking shimmer, once per seventeen seconds, less than one pixel.
   float pulse=1.-smoothstep(.10,.28,mod(time,17.));
   float band=exp(-pow((uv.y-fract(time*.13))/.016,2.))*pulse;
   uv.x+=(drift+band*.9)*amount/resolution.x;
   vec2 chroma=vec2(.72*amount/resolution.x,0.);
   vec3 original=texture2D(tDiffuse,uv).rgb;
   vec3 c=vec3(texture2D(tDiffuse,uv+chroma).r,original.g,texture2D(tDiffuse,uv-chroma).b);
   float grain=hash(gl_FragCoord.xy+vec2(frame*17.,frame*31.))-.5;
   float scan=sin(uv.y*resolution.y*3.14159265);
   c+=grain*.030*amount + (hash(vec2(line,frame))-.5)*.006*amount;
   c*=1.-(.5+.5*scan)*.011*amount;
   c+=band*.009*amount;
   gl_FragColor=vec4(clamp(c,0.,1.),1.);
 }`
};
