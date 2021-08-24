precision highp float;
uniform vec2 resolution;
uniform float time;

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

float star(vec2 fragCoord, float density, float brightness, float matatakiFactor) {
  float rand = random(fragCoord / resolution.xy);
  float matataki = random(vec2((1000. * rand + time) / 100., 0.));
  return (brightness + matatakiFactor * matataki) * smoothstep(density, 1., rand);
}

void main(void) {
  // bg
  vec3 color = mix(vec3(.0, .05, .19), vec3(0.), gl_FragCoord.y / resolution.y);

  vec3 starColor = vec3(.7, .8, 1.);

  // stars glow
  float localStarDensity = min(.9999, fbm(vec3(gl_FragCoord.xy / 300., 17.)) * 1.8);
  color = mix(color, starColor, 0.25 * (1. - localStarDensity));
  // color = vec3(1.) * localStarDensity;

  // small stars
  // +---+---+---+
  // |   |   |   |
  // +---+---+---+
  // |   | 1 |0.4|
  // +---+---+---+
  // |   |0.4|   |
  // +---+---+---+
  float smallStarDensity = .7 + .28 * localStarDensity;
  float starValue1 = star(gl_FragCoord.xy, smallStarDensity, .4, .2);
  starValue1 += .4 * star((gl_FragCoord.xy + vec2(0.0, 1.0)), smallStarDensity, .4, .2);
  starValue1 += .4 * star((gl_FragCoord.xy + vec2(1.0, 0.0)), smallStarDensity, .4, .2);
  color = mix(color, starColor, starValue1);
  // color = vec3(1.) * starValue1;

  // // large stars
  // +---+---+---+---+---+
  // |   |   |0.2|   |   |
  // +---+---+---+---+---+
  // |   |0.4|0.8|0.4|   |
  // +---+---+---+---+---+
  // |0.2|0.8| 1 |0.8|0.2|
  // +---+---+---+---+---+
  // |   |0.4|0.8|0.4|   |
  // +---+---+---+---+---+
  // |   |   |0.2|   |   |
  // +---+---+---+---+---+
  float largeStarDensity = min(.9999, .99 + .01 * localStarDensity);
  float starValue2 = star(gl_FragCoord.xy, largeStarDensity, .5, .5);
  starValue2 += .8 * star((gl_FragCoord.xy + vec2(-1., 0.0)), largeStarDensity, .5, .5);
  starValue2 += .8 * star((gl_FragCoord.xy + vec2(0.0, -1.)), largeStarDensity, .5, .5);
  starValue2 += .8 * star((gl_FragCoord.xy + vec2(0.0, 1.0)), largeStarDensity, .5, .5);
  starValue2 += .8 * star((gl_FragCoord.xy + vec2(1.0, 0.0)), largeStarDensity, .5, .5);
  starValue2 += .4 * star((gl_FragCoord.xy + vec2(-1., -1.)), largeStarDensity, .5, .5);
  starValue2 += .4 * star((gl_FragCoord.xy + vec2(-1., 1.0)), largeStarDensity, .5, .5);
  starValue2 += .4 * star((gl_FragCoord.xy + vec2(1.0, -1.)), largeStarDensity, .5, .5);
  starValue2 += .4 * star((gl_FragCoord.xy + vec2(1.0, 1.0)), largeStarDensity, .5, .5);
  starValue2 += .2 * star((gl_FragCoord.xy + vec2(-2., 0.0)), largeStarDensity, .5, .5);
  starValue2 += .2 * star((gl_FragCoord.xy + vec2(0.0, -2.)), largeStarDensity, .5, .5);
  starValue2 += .2 * star((gl_FragCoord.xy + vec2(0.0, 2.0)), largeStarDensity, .5, .5);
  starValue2 += .2 * star((gl_FragCoord.xy + vec2(2.0, 0.0)), largeStarDensity, .5, .5);
  color = mix(color, starColor, starValue2);
  // color = vec3(1.) * starValue2;

  // bg noise
  vec3 noise = vec3(fbm(vec3(gl_FragCoord.x + 10. * time, gl_FragCoord.y, 0.) / 100.) * .9);
  color = softlight(color, noise);

  // light
  float lightGradLen = length(gl_FragCoord.xy - vec2(resolution.x * .4, 0.));
  float lightPos = max(0., 1. - lightGradLen / (resolution.y * .5));
  vec3 light = mix(vec3(.0, .04, .16), vec3(.34, .67, .88), lightPos);
  color += light * .2;

  // skyline
  float skyline1 = fbm(vec3(1900., gl_FragCoord.x * .002, 0.));
  float skyline2 = (1. - skyline1) * fbm(vec3(9., gl_FragCoord.x * .1, 0.));
  float threshold = resolution.y * (.3 * skyline1 + .015 * skyline2);
  color *= smoothstep(threshold, threshold + 5., gl_FragCoord.y);

  gl_FragColor = vec4(color, 1.);
}
