precision highp float;

#define PI 3.141592653589793
#define EPS 0.001

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;

float rand(float n){return fract(sin(n) * 43758.5453123);}

float min3(float a, float b, float c) {return min(min(a, b), c);}

mat3 rot3d(vec3 axis, float angle) {
  return mat3(
    axis.x * axis.x * (1.0 - cos(angle)) + cos(angle),
    axis.y * axis.x * (1.0 - cos(angle)) + axis.z * sin(angle),
    axis.z * axis.x * (1.0 - cos(angle)) - axis.y * sin(angle),
    axis.x * axis.y * (1.0 - cos(angle)) - axis.z * sin(angle),
    axis.y * axis.y * (1.0 - cos(angle)) + cos(angle),
    axis.z * axis.y * (1.0 - cos(angle)) + axis.x * sin(angle),
    axis.x * axis.z * (1.0 - cos(angle)) + axis.y * sin(angle),
    axis.y * axis.z * (1.0 - cos(angle)) - axis.x * sin(angle),
    axis.z * axis.z * (1.0 - cos(angle)) + cos(angle)
  );
}

float roundedCube(vec3 pos, vec3 center){
  pos -= center;
  return length(max(abs(pos) - vec3(0.35, 0.35, 0.35), 0.0)) - 0.1;
}

float col(vec3 pos, vec3 center){
  pos -= center;
  return min3(
    roundedCube(pos, vec3(-1, 0, 0)),
    roundedCube(pos, vec3(0, 0, 0)),
    roundedCube(pos, vec3(1, 0, 0))
  );
}

float layer(vec3 pos, vec3 center){
  pos -= center;
  return min3(
    col(pos, vec3(0, -1, 0)),
    col(pos, vec3(0, 0, 0)),
    col(pos, vec3(0, 1, 0))
  );
}

float rubik(vec3 pos, float index, float mode, float angle) {
  mat3 rot = (
    step(2., mod(mode + 0., 3.)) * mat3(1, 0, 0, 0, 1, 0, 0, 0, 1)
    + step(2., mod(mode + 1., 3.)) * mat3(1, 0, 0, 0, 0, 1, 0, 1, 0)
    + step(2., mod(mode + 2., 3.)) * mat3(0, 0, 1, 0, 1, 0, 1, 0, 0)
  );
  pos *= rot;
  return min3(
    layer(rot3d(vec3(0, 0, 1), angle) * pos, vec3(0, 0, floor(mod(index + 0., 3.)) - 1.)),
    layer(pos, vec3(0, 0, floor(mod(index + 1., 3.)) - 1.)),
    layer(pos, vec3(0, 0, floor(mod(index + 2., 3.)) - 1.))
  );
}

float dist(vec3 pos) {
  pos *= rot3d(vec3(1, 0, 0), time);
  pos *= rot3d(vec3(0, 1, 0), time);
  pos *= rot3d(vec3(0, 0, 1), time);

  float t = time * 8.;
  float n = floor(t / (PI / 2.));
  float r1 = mod(rand(n) * 100., 3.);
  float r2 = mod(rand(n + 1.) * 100., 3.);
  return rubik(pos, r1, r2, t);
}

vec3 normal(vec3 pos) {
  return normalize(
    vec3(
      dist(pos) - dist(vec3(pos.x - EPS, pos.y, pos.z)),
      dist(pos) - dist(vec3(pos.x, pos.y - EPS, pos.z)),
      dist(pos) - dist(vec3(pos.x, pos.y, pos.z - EPS))
    )
  );
}

void main(void) {
  vec2 pos = (gl_FragCoord.xy * 2.0 - resolution.xy) / min(resolution.x, resolution.y);

  vec3 campos = vec3(2, 2., -2.);
  vec3 camdir = normalize(-campos);
  vec3 right = normalize(cross(camdir, vec3(0, 1, 0)));
  vec3 up = normalize(cross(right, camdir));
  vec3 raydir = normalize(pos.x * right + pos.y * up + camdir * .5);
  vec3 lightdir = vec3(-1, 0, -1);

  vec3 color = 0.5 + 0.5 * cos(time +(pos + 0.5).xyx + vec3(0,2,4));

  vec3 march = campos;
  for (int i = 0; i < 35; i++) {
    float dist = dist(march);

    if (dist < EPS) {
      vec3 normal = normal(march);
      float light = clamp(dot(normal, lightdir), 0., 1.);
      color = vec3(light) + vec3(.7, .2, .3);
      break;
    }

    march += raydir * dist;
  }

  gl_FragColor = vec4(color, 1);
}
