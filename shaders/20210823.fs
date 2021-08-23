precision highp float;
uniform vec2 resolution;
uniform float time;

// ---- noise

#define NUM_OCTAVES 6

float random(vec2 st) {
  return fract(sin(dot(st.xy,vec2(12.9898,78.233))) * 43758.5453123);
}

float mod289(float x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec2 mod289(vec2 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 mod289(vec4 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 mod7(vec4 x) {
  return x - floor(x * (1.0 / 7.0)) * 7.0;
}

vec4 permute(vec4 x) {
  return mod289((34.0 * x + 10.0) * x);
}

// taken from webgl-noise (https://github.com/ashima/webgl-noise)
vec2 cellular2x2(vec2 P) {
 #define K 0.142857142857 // 1/7
 #define K2 0.0714285714285 // K/2
 #define jitter 0.8 // jitter 1.0 makes F1 wrong more often
  vec2 Pi = mod289(floor(P));
  vec2 Pf = fract(P);
  vec4 Pfx = Pf.x + vec4(-0.5, -1.5, -0.5, -1.5);
  vec4 Pfy = Pf.y + vec4(-0.5, -0.5, -1.5, -1.5);
  vec4 p = permute(Pi.x + vec4(0.0, 1.0, 0.0, 1.0));
  p = permute(p + Pi.y + vec4(0.0, 0.0, 1.0, 1.0));
  vec4 ox = mod7(p)*K+K2;
  vec4 oy = mod7(floor(p*K))*K+K2;
  vec4 dx = Pfx + jitter*ox;
  vec4 dy = Pfy + jitter*oy;
  vec4 d = dx * dx + dy * dy; // d11, d12, d21 and d22, squared
  d.xy = (d.x < d.y) ? d.xy : d.yx; // Swap if smaller
  d.xz = (d.x < d.z) ? d.xz : d.zx;
  d.xw = (d.x < d.w) ? d.xw : d.wx;
  d.y = min(d.y, d.z);
  d.y = min(d.y, d.w);
  return sqrt(d.xy);
}

// taken from glsl-fbm (https://github.com/yiwenl/glsl-fbm)
float noise(vec3 p) {
  vec3 a = floor(p);
  vec3 d = p - a;
  d = d * d * (3.0 - 2.0 * d);

  vec4 b = a.xxyy + vec4(0.0, 1.0, 0.0, 1.0);
  vec4 k1 = permute(b.xyxy);
  vec4 k2 = permute(k1.xyxy + b.zzww);

  vec4 c = k2 + a.zzzz;
  vec4 k3 = permute(c);
  vec4 k4 = permute(c + 1.0);

  vec4 o1 = fract(k3 * (1.0 / 41.0));
  vec4 o2 = fract(k4 * (1.0 / 41.0));

  vec4 o3 = o2 * d.z + o1 * (1.0 - d.z);
  vec2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);

  return o4.y * d.y + o4.x * (1.0 - d.y);
}

float fbm(vec3 x) {
  float v = 0.0;
  float a = 0.5;
  vec3 shift = vec3(100);
  for (int i = 0; i < NUM_OCTAVES; ++i) {
    v += a * noise(x);
    x = x * 2.0 + shift;
    a *= 0.5;
  }
  return v;
}

// ---- main (inspired by https://ae-style.net/tutorials/e06.html)

void main(void) {
  vec2 d = resolution.xy / (gl_FragCoord.xy + 1.);

  // step1 bg
  vec3 color = mix(vec3(.0, .05, .19), vec3(0.), gl_FragCoord.y / resolution.y);

  // step2 cloud
  color += fbm(vec3(gl_FragCoord.x + time * 15., gl_FragCoord.y, 0.) / 100.) * .25;

  // step3 light
  float lightGradLen = length(gl_FragCoord.xy - vec2(resolution.x * .3, 0.));
  float lightEndLen = 500.;
  color += .3 * max(0., 1. - lightGradLen / lightEndLen) * vec3(0.34, 0.67, 0.88);

  // step4 small stars
  float starValue1 = 1. - length(cellular2x2(gl_FragCoord.xy /4.));
  starValue1 = min(1., pow(starValue1 * 1.4, 10.));
  color += (.2 + .6 * random(gl_FragCoord.xy + time)) * vec3(.9, .9, 1.) * starValue1;
  // color = vec3(1.) * starValue1;

  // step5 small stars
  float starValue2 = 1. - length(cellular2x2(gl_FragCoord.xy /20.));
  starValue2 = min(1., pow(starValue2 * 1.3, 50.));
  color += (1.5 * random(gl_FragCoord.xy + time)) * vec3(.9, .9, 1.) * starValue2;
  // color = vec3(1.) * starValue2;

  // step6 skyline
  float skyline1 = fbm(vec3(1900., gl_FragCoord.x * .002, 0.));
  float skyline2 = (1. - skyline1) * fbm(vec3(9., gl_FragCoord.x * .07, 0.));
  float threshold = resolution.y * (.3 * skyline1 + .02 * skyline2
  );
  color *= smoothstep(threshold, threshold + 5., gl_FragCoord.y);

  gl_FragColor = vec4(color, 1.);
}
