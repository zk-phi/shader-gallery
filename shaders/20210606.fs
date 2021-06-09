precision highp float;

uniform vec2 resolution;
uniform vec2 mouse;
uniform float time;

// ---- noise (copy-pasted from gslify plugin "glsl-fbm")

#define NUM_OCTAVES 2

float mod289(float x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
vec4 mod289(vec4 x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
vec4 perm(vec4 x){return mod289(((x * 34.0) + 1.0) * x);}

float noise(vec3 p){
  vec3 a = floor(p);
  vec3 d = p - a;
  d = d * d * (3.0 - 2.0 * d);

  vec4 b = a.xxyy + vec4(0.0, 1.0, 0.0, 1.0);
  vec4 k1 = perm(b.xyxy);
  vec4 k2 = perm(k1.xyxy + b.zzww);

  vec4 c = k2 + a.zzzz;
  vec4 k3 = perm(c);
  vec4 k4 = perm(c + 1.0);

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

// ---- main (based on https://qiita.com/archeleeds/items/d99283212525eff182fc)

vec3 sky(vec2 pos) {
  return mix(vec3(.4, .9, 1), vec3(.4, .4, 1.), abs(pos.y));
}

void main(void) {
  vec2 pos = 2. * gl_FragCoord.xy / resolution.xy - 1.0;
  pos.x *= resolution.x / resolution.y;

  float dy = clamp(- 10000., 0., - 20000. * (mouse.y - 0.5));
  vec3 campos = vec3(0, 4000. + dy, 6000. * time);
  vec3 camdir = normalize(vec3(- (mouse.x - 0.5), 0, 1));
  vec3 right = normalize(cross(camdir, vec3(0, 1, 0)));
  vec3 up = normalize(cross(right, camdir));
  vec3 raydir = normalize(pos.x * right + pos.y * up + camdir * .5);

  // clouds
  vec3 cloudColor = vec3(0, 0, 0);
  float rayStrength = 1.;
  vec3 ray = campos;
  for (int i = 0; i < 50; i++) {
    float areaDensity = smoothstep(-20000., -15000., ray.y) - smoothstep(0., 5000., ray.y);
    float localDensity = smoothstep(0.1, 1.0, fbm(ray * 0.0003));
    vec3 localcolor = vec3(mix(1.1, 0.3, localDensity));
    float alpha = rayStrength * areaDensity * localDensity;
    cloudColor += localcolor * alpha;
    rayStrength -= alpha;
    ray += raydir * 1500.;
  }

  vec3 col = mix(cloudColor, sky(pos), rayStrength);

  gl_FragColor = vec4(col, 1.0);
}
