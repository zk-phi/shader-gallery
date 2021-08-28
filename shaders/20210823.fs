precision highp float;
uniform vec2 resolution;
uniform float time;
uniform float quality;

// ---- noise

float random(vec2 st) {
  return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

// Iq's value noise (https://www.shadertoy.com/view/lsf3WH)
float noise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);

  vec2 u = f * f * (3.0 - 2.0 * f);

  return mix(
    mix(random(i + vec2(0.0, 0.0)), random(i + vec2(1.0, 0.0)), u.x),
    mix(random(i + vec2(0.0, 1.0)), random(i + vec2(1.0, 1.0)), u.x),
    u.y
  );
}

// fbm (octave = 3)
float fractal(vec2 uv) {
  mat2 m = mat2(1.6,  1.2, -1.2,  1.6);
  float f  = 0.5000 * noise(uv);
  uv = m * uv;
  f += 0.2500 * noise(uv);
  uv = m * uv;
  f += 0.1250 * noise(uv);
  return f;
}

// ---- voronoi

vec2 random2(vec2 p) {
  return fract(
    sin(vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)))) * 43758.5453
  );
}

// simple voronoi
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

  // bg
  vec3 color = mix(vec3(.0, .05, .19), vec3(0.), uv.y);

  // bg noise
  vec3 noise = vec3(fractal(vec2(coord.x + 10. * time, coord.y) / 100.));
  color = softlight(color, noise * .5);

  // light
  float lightGradLen = length(uv - vec2(.4, 0.));
  float lightPos = pow(max(0., 1. - lightGradLen / .9), 1.3);
  vec3 light = mix(vec3(.0, .04, .16), vec3(.34, .67, .88), lightPos);
  color += light * .5;

  vec3 starColor = vec3(.6, .7, 1.);

  // stars glow
  float localStarDensity = min(.9999, fractal(starCoord / 200.) * 1.8);
  color = mix(color, starColor, .2 * pow(1. - localStarDensity, 2.));
  // color = vec3(1.) * pow(1. - localStarDensity, 1.5);

  // small stars
  float smallStarDensity = .9 * localStarDensity;
  float starValue1 = star(starCoord, 4., 1., 3., smallStarDensity, .0, 1., .3);
  color = mix(color, starColor, starValue1);
  // color = vec3(starValue1);

  // large stars
  float largeStarDensity = .9 * localStarDensity;
  float starValue2 = star(starCoord, 25., 2.5, 7.5, largeStarDensity, .3, .5, .5);
  color = mix(color, starColor, starValue2);
  // color = vec3(starValue2);

  // skyline
  float skyline1 = fractal(vec2(coord.x * .003));
  float skyline2 = (1. - skyline1) * fractal(vec2(coord.x * .1));
  float threshold = resolution.y * (.1  + .1 * skyline1 + .02 * skyline2);
  color *= smoothstep(threshold, threshold + 5., coord.y);

  gl_FragColor = vec4(color, 1.);
}
