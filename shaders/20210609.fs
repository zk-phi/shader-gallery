precision highp float;

#define PI 3.141592653589793

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;

vec2 rot2d(vec2 pos, float rad) {
  return vec2(
    pos.x * cos(rad) - pos.y * sin(rad),
    pos.x * sin(rad) + pos.y * cos(rad)
  );
}

// Iq's triangle distance fn (modified)
// https://www.shadertoy.com/view/Xl2yDW
float triangle(vec2 p, float scale) {
  p /= scale;
  const float k = sqrt(3.0);
  p.x = abs(p.x) - 1.0;
  p.y = p.y + 1.0 / k;
  if( p.x+k*p.y>0.0 ) p=vec2(p.x-k*p.y,-k*p.x-p.y)/2.0;
  p.x -= clamp( p.x, -2.0, 0.0 );
  return -length(p)*sign(p.y);
}

float triangles(vec2 p) {
  float sum = 0.;
  for (float i = .1; i < 2.; i += .2) {
    p = rot2d(p, .1 * sin(time));
    sum += (2. - i) * (1. - smoothstep(0., .05, abs(triangle(p, .1 + i + 1. * sin(time)))));
  }
  return sum;
}

void main(void) {
  vec2 pos = (gl_FragCoord.xy * 2.0 - resolution.xy) / min(resolution.x, resolution.y);
  // gl_FragColor = vec4(vec3(step(0., triangles(pos.xy))), 1);
  gl_FragColor = vec4(vec3(triangles(pos.xy)), 1);
}
