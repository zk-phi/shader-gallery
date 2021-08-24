const match = location.href.match(/\?quality=([0-9.]+)/);
const QUALITY_FACTOR = match ? parseFloat(match[1]) : 1;

const log = document.getElementById("log");
const canvas = document.getElementById("canvas");

let gl;
try {
  gl = canvas.getContext("webgl") || canvas.getContext("experimental-webgl");
} catch (e) {
  log.append("WebGL NOT supported!");
  throw "WebGL NOT supported";
}

gl.getExtension('OES_standard_derivatives');

log.append("Loading (fragment shader) ...");
const req = new XMLHttpRequest();
req.open('GET', `./shaders/${location.href.split("#")[1]}.fs`, false);
try {
  req.send(null);
  if (req.status !== 200) throw "network error";
} catch (e) {
  log.append(`\nError (status = ${req.status})`);
  throw "network error";
}
const fragmentShader = req.responseText;
log.append(" Done.\n");

log.append("Compiling (fragment shader) ...");
const compiledFragmentShader = gl.createShader(gl.FRAGMENT_SHADER);
gl.shaderSource(compiledFragmentShader, fragmentShader);
gl.compileShader(compiledFragmentShader);
if (!gl.getShaderParameter(compiledFragmentShader, gl.COMPILE_STATUS)) {
  log.append("\n" + gl.getShaderInfoLog(compiledFragmentShader));
  throw "compile error";
}
log.append(" Done.\n");

log.append("Compiling (vertex shader) ...");
const compiledVertexShader = gl.createShader(gl.VERTEX_SHADER);
gl.shaderSource(compiledVertexShader, `
  precision highp float;
  attribute vec2 pos;
  void main(void) {
    gl_Position = vec4(pos, 0.0, 1.0);
  }
`);
gl.compileShader(compiledVertexShader);
if (!gl.getShaderParameter(compiledVertexShader, gl.COMPILE_STATUS)) {
  log.append("\n" + gl.getShaderInfoLog(compiledVertexShader));
  throw "compile error";
}
log.append(" Done.\n");

log.append("Linking ...");
const program = gl.createProgram();
gl.attachShader(program, compiledVertexShader);
gl.attachShader(program, compiledFragmentShader);
gl.linkProgram(program);
gl.useProgram(program);
if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
  log.append("\n" + gl.getProgramInfoLog(program));
  throw "link error";
}
log.append(" Done.\n");

const vertices = gl.createBuffer();
gl.bindBuffer(gl.ARRAY_BUFFER, vertices);
gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
  -1.0,  1.0,
  -1.0, -1.0,
  1.0,  1.0,
  1.0, -1.0,
]), gl.STATIC_DRAW);

const pos = gl.getAttribLocation(program, 'pos');
gl.enableVertexAttribArray(pos);
gl.vertexAttribPointer(pos, 2, gl.FLOAT, false, 0 ,0);

/* ---- */

const resolutionLoc = gl.getUniformLocation(program, 'resolution');
const mouseLoc = gl.getUniformLocation(program, 'mouse');
const qualityLoc = gl.getUniformLocation(program, 'quality');
const timeLoc = gl.getUniformLocation(program, 'time');

const startTime = new Date().getTime();

const updateResolution = () => {
  canvas.height = window.innerHeight * devicePixelRatio * QUALITY_FACTOR;
  canvas.width = window.innerWidth * devicePixelRatio * QUALITY_FACTOR;
  gl.viewport(0, 0, canvas.width, canvas.height);
  gl.uniform2f(resolutionLoc, canvas.width, canvas.height);
  gl.uniform1f(qualityLoc, devicePixelRatio * QUALITY_FACTOR);
};
window.addEventListener('resize', updateResolution);
updateResolution();

let mouse = [0.5, 0.5];
const updateMousePos = (e) => {
  e.preventDefault();
  if (e.touches) {
    e = e.touches[0];
  }
  mouse = [
    e.clientX / window.innerWidth,
    1 - e.clientY / window.innerHeight
  ];
};
const resetMousePos = (e) => {
  if (!e.toElement && !e.relatedTarget) {
    mouse = [0.5, 0.5];
  }
};
window.addEventListener('mousemove', updateMousePos);
window.addEventListener('mouseout', resetMousePos);
canvas.addEventListener('touchmove', updateMousePos);
canvas.addEventListener('touchend', resetMousePos);

let throttle = false;
const render = () => {
  if (!throttle) {
    gl.uniform1f(timeLoc, (new Date().getTime() - startTime) / 1000);
    gl.uniform2fv(mouseLoc, mouse)
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
    gl.flush();
  }
  throttle = !throttle;
  window.requestAnimationFrame(render);
};
render();

/* ---- UI */

const editor = document.getElementById("editor");
editor.append(fragmentShader);

const acejs = ace.edit("editor");
acejs.setReadOnly(true);
acejs.setTheme("ace/theme/xcode");
acejs.session.setMode("ace/mode/glsl");

const btn = document.getElementById("btn");
btn.append(` (${fragmentShader.split("\n").length} lines)`);

btn.addEventListener("click", () => {
  editor.classList.toggle("hidden");
});
