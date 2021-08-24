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

// ---- main (inspired by https://ae-style.net/tutorials/e06.html)

vec3 softlight(vec3 base, vec3 ref) {
  vec3 flag = step(vec3(.5, .5, .5), ref);
  vec3 res1 = 2. * base * ref + base * base * (1. - 2. * ref);
  vec3 res2 = 2. * base * (1. - ref) + sqrt(base) * (2. * ref - 1.);
  return (1. - flag) * res1 + flag * res2;
}

float star(vec2 uv, float density, float brightness, float matatakiFactor) {
  float rand = random(uv);
  float matataki = random(vec2((1000. * rand + time) / 100., 0.));
  return (brightness + matatakiFactor * matataki) * smoothstep(density, 1., rand);
}

void main(void) {
  vec2 coord = floor(gl_FragCoord.xy / quality);
  vec2 resolution = floor(resolution / quality);
  vec2 uv = coord / resolution;
  float dx = 1. / resolution.x;
  float dy = 1. / resolution.y;

  // bg
  vec3 color = mix(vec3(.0, .05, .19), vec3(0.), uv.y);

  vec3 starColor = vec3(.6, .7, 1.);

  // bg noise
  vec3 noise = vec3(fbm(vec3(coord.x + 10. * time, coord.y, 0.) / 100.));
  color = softlight(color, noise * .7);

  // light
  float lightGradLen = length(coord - vec2(resolution.x * .4, 0.));
  float lightPos = pow(max(0., 1. - lightGradLen / resolution.y), 1.2);
  vec3 light = mix(vec3(.0, .04, .16), vec3(.34, .67, .88), lightPos);
  color += light * .5;

  // stars glow
  float localStarDensity = min(.9999, fbm(vec3(coord / 300., 17.)) * 1.8);
  color = mix(color, starColor, 0.2 * (1. - localStarDensity));
  // color = vec3(1.) * localStarDensity;

  // small stars
  // +---+---+---+
  // |   |   |   |
  // +---+---+---+
  // |   | 1 |0.4|
  // +---+---+---+
  // |   |0.4|   |
  // +---+---+---+
  float smallStarDensity = .7 + .295 * localStarDensity;
  float starValue1 = star((uv + vec2(0.0 * dx, 0.0 * dy)), smallStarDensity, .3, .45);
  starValue1 += .4 * star((uv + vec2(0.0 * dx, 1.0 * dy)), smallStarDensity, .3, .45);
  starValue1 += .4 * star((uv + vec2(1.0 * dx, 0.0 * dy)), smallStarDensity, .3, .45);
  color = mix(color, starColor, starValue1);
  // color = vec3(1.) * starValue1;

  // // large stars
  // +---+---+---+---+---+
  // |   |   |0.3|   |   |
  // +---+---+---+---+---+
  // |   |0.6| 1 |0.6|   |
  // +---+---+---+---+---+
  // |0.3| 1 | 1 | 1 |0.3|
  // +---+---+---+---+---+
  // |   |0.6| 1 |0.6|   |
  // +---+---+---+---+---+
  // |   |   |0.3|   |   |
  // +---+---+---+---+---+
  float largeStarDensity = .99 + .009 * localStarDensity;
  float starValue2 = star((uv + vec2( 0.0 * dx,  0.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += 1. * star((uv + vec2(-1.0 * dx,  0.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += 1. * star((uv + vec2( 0.0 * dx, -1.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += 1. * star((uv + vec2( 0.0 * dx,  1.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += 1. * star((uv + vec2( 1.0 * dx,  0.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += .6 * star((uv + vec2(-1.0 * dx, -1.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += .6 * star((uv + vec2(-1.0 * dx,  1.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += .6 * star((uv + vec2( 1.0 * dx, -1.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += .6 * star((uv + vec2( 1.0 * dx,  1.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += .3 * star((uv + vec2(-2.0 * dx,  0.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += .3 * star((uv + vec2( 0.0 * dx, -2.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += .3 * star((uv + vec2( 0.0 * dx,  2.0 * dy)), largeStarDensity, .5, .75);
  starValue2 += .3 * star((uv + vec2( 2.0 * dx,  0.0 * dy)), largeStarDensity, .5, .75);
  color = mix(color, starColor, starValue2);
  // color = vec3(1.) * starValue2;

  // skyline
  float skyline1 = fbm(vec3(1900., coord.x * .002, 0.));
  float skyline2 = (1. - skyline1) * fbm(vec3(9., coord.x * .1, 0.));
  float threshold = resolution.y * (.3 * skyline1 + .015 * skyline2);
  color *= smoothstep(threshold, threshold + 5., coord.y);

  gl_FragColor = vec4(color, 1.);
}
