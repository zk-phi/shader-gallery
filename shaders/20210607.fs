precision mediump float;

#define PI 3.141592653589793

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;

float circle (vec2 pos, float r) {
  return 1. - smoothstep(0., .01, length(pos) - r) +
    .7 * (1. - smoothstep(-.1, .1, abs(length(pos) - r)));
}

vec3 color1 (vec2 pos) {
  return vec3(1. - (pos + 0.5), mouse.y);
}

vec3 color2 (vec2 pos) {
  return vec3(pos + 0.5, mouse.x);
}

vec2 rot2d(vec2 pos, float rad) {
  return vec2(
    pos.x * cos(rad) - pos.y * sin(rad),
    pos.x * sin(rad) + pos.y * cos(rad)
  );
}

vec2 rep (vec2 pos) {
  float time = mod(time, 2.);
  vec2 d1 = vec2(0, 1) * .25 * clamp(time, 0., 1.);
  vec2 d2 = vec2(1, 0) * .25 * clamp(time - 1., 0., 1.);
  return mod(pos - d1 + d2, 0.5) - 0.5 * 0.5;
}

void main(void) {
  vec2 pos = (gl_FragCoord.xy * 2. - resolution.xy) / min(resolution.x, resolution.y);
  vec2 pos2 = rot2d(pos, PI * 3. / 2.) + vec2(0, .25);

  float r = 0.08;
  float circles1 = circle(rep(pos), r) + circle(rep(pos + vec2(.25)), r);
  float circles2 = circle(rep(pos2), r) + circle(rep(pos2 + vec2(.25)), r);

  float sw = mod(time, 16.);
  float sw1 = smoothstep(0., 1., sw) - smoothstep(11., 13., sw);
  float sw2 = smoothstep(0., 1., time);

  vec3 col = sw1 * circles1 * color1(pos) + sw2 * circles2 * color2(pos);
  gl_FragColor = vec4(col, 1.);
}
