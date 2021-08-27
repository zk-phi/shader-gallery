precision highp float;
uniform vec2 resolution;
uniform float time;
uniform float quality;

// ---- noise

#define NUM_OCTAVES 6

float random(vec2 st) {
  return fract(sin(dot(st.xy,vec2(12.9898,78.233))) * 43758.5453123);
}

vec4 mod289(vec4 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 permute(vec4 x) {
  return mod289((34.0 * x + 10.0) * x);
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

// ---- voronoi

// taken and modified from the book of shaders

vec2 random2(vec2 p) {
  return fract(sin(vec2(dot(p,vec2(127.1,311.7)),dot(p,vec2(269.5,183.3))))*43758.5453);
}

// returns vec3(center.x, center.y, distance)
vec3 voronoi(vec2 pos, float gridSize) {
  vec2 cellOrigin = floor(pos / gridSize);
  vec2 cellPos = fract(pos / gridSize);

  float dist = 10.;
  vec2 center;

  for (int j = -1; j <= 1; j++) {
    for (int i = -1; i <= 1; i++) {
      vec2 neighbor = vec2(float(i), float(j));
      vec2 neighborOrigin = cellOrigin + neighbor;
      vec2 neighborCenter = random2(neighborOrigin);
      float d = length(neighbor + neighborCenter - cellPos);

      if(d < dist) {
        dist = d;
        center = neighborOrigin + neighborCenter;
      }
    }
  }

  return vec3(center, dist) * gridSize;
}

// ---- main (inspired by https://ae-style.net/tutorials/e06.html)

vec2 rot2d(vec2 pos, float rad) {
  return vec2(
    pos.x * cos(rad) - pos.y * sin(rad),
    pos.x * sin(rad) + pos.y * cos(rad)
  );
}

vec3 softlight(vec3 base, vec3 ref) {
  vec3 flag = step(vec3(.5, .5, .5), ref);
  vec3 res1 = 2. * base * ref + base * base * (1. - 2. * ref);
  vec3 res2 = 2. * base * (1. - ref) + sqrt(base) * (2. * ref - 1.);
  return (1. - flag) * res1 + flag * res2;
}

float star(vec2 pos, float gridSize, float radius, float growRadius, float threshold, float minBrightness, float randomFactor, float matatakiFactor) {
  vec3 voronoi = voronoi(pos, gridSize);
  float star = 1. - smoothstep(0., radius, voronoi.z);
  float glow = 1. - smoothstep(0., growRadius, voronoi.z);
  float flag = step(threshold, random(voronoi.xy / resolution));
  float matataki = matatakiFactor * random(voronoi.xy + time * .001);
  float random = randomFactor * random(voronoi.xy);
  return flag * (minBrightness + random + matataki) * (star + .1 * glow);
}

void main(void) {
  vec2 rotCenter = vec2(resolution.x * .3, resolution.y * .6);
  vec2 starCoord = (rot2d(gl_FragCoord.xy - rotCenter, - time * .0025) + rotCenter) / quality;
  vec2 coord = gl_FragCoord.xy / quality;
  vec2 resolution = resolution / quality;
  vec2 uv = coord / resolution;
  float dx = 1. / resolution.x;
  float dy = 1. / resolution.y;

  // bg
  vec3 color = mix(vec3(.0, .05, .19), vec3(0.), uv.y);

  // bg noise
  vec3 noise = vec3(fbm(vec3(coord.x + 10. * time, coord.y, 0.) / 100.));
  color = softlight(color, noise * .5);

  // light
  float lightGradLen = length(uv - vec2(.4, 0.));
  float lightPos = pow(max(0., 1. - lightGradLen / .9), 1.3);
  vec3 light = mix(vec3(.0, .04, .16), vec3(.34, .67, .88), lightPos);
  color += light * .5;

  vec3 starColor = vec3(.6, .7, 1.);

  // stars glow
  float localStarDensity = min(.9999, fbm(vec3(starCoord / 300., 17.)) * 1.8);
  color = mix(color, starColor, .25 * pow(1. - localStarDensity, 1.5));
  // color = vec3(1.) * pow(1. - localStarDensity, 1.5);

  // small stars
  float smallStarDensity = .9 * localStarDensity;
  float starValue1 = star(starCoord, 4., 1., 3., smallStarDensity, .0, 1., .3);
  color = mix(color, starColor, starValue1);
  // color = vec3(starValue1);

  // large stars
  float largeStarDensity = .9 * localStarDensity;
  float starValue2 = star(starCoord, 25., 2., 6., largeStarDensity, .5, .5, .3);
  color = mix(color, starColor, starValue2);
  // color = vec3(starValue2);

  // skyline
  float skyline1 = fbm(vec3(1900., coord.x * .002, 0.));
  float skyline2 = (1. - skyline1) * fbm(vec3(9., coord.x * .1, 0.));
  float threshold = resolution.y * (.3 * skyline1 + .015 * skyline2);
  color *= smoothstep(threshold, threshold + 5., coord.y);

  gl_FragColor = vec4(color, 1.);
}
