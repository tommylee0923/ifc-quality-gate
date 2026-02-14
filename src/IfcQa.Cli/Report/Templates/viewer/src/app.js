console.log("viewer src/app.js booted");

window.addEventListener("error", (e) => console.log("WINDOW ERROR:", e.message));
window.addEventListener("unhandledrejection", (e) => console.log("PROMISE ERROR:", e.reason));

import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader.js";

const state = {
  renderer: null,
  scene: null,
  camera: null,
  controls: null,

  modelRoot: null,                 // gltf.scene
  objectsByGlobalId: new Map(),    // gid -> Mesh[]
  issuesByGid: new Map(),          // gid -> Issue[]

  selectedGid: null,
  originalMaterials: new WeakMap(),

  viewerInfo: null,
};

async function fetchIssues() {
  const res = await fetch("./issues.json");
  const data = await res.json();
  return data.Issues ?? data.issues ?? [];
}

function buildIssuesByGlobalId(issues) {
  const map = new Map();
  for (const it of issues) {
    const gid = it.GlobalId ?? it.globalId;
    if (!gid) continue;
    if (!map.has(gid)) map.set(gid, []);
    map.get(gid).push(it);
  }
  return map;
}

// Some GLB pipelines store gid on parent nodes; climb up to find a name.
function findNamedAncestor(obj) {
  let cur = obj;
  while (cur) {
    if (cur.name) return cur;
    cur = cur.parent;
  }
  return null;
}

function setHighlighted(mesh, on) {
  if (!mesh?.isMesh) return;

  if (on) {
    if (!state.originalMaterials.has(mesh)) state.originalMaterials.set(mesh, mesh.material);
    mesh.material = new THREE.MeshStandardMaterial({
      emissive: 0xffcc00,
      emissiveIntensity: 0.9,
    });
  } else {
    const orig = state.originalMaterials.get(mesh);
    if (orig) mesh.material = orig;
  }
}

function clearSelection() {
  if (!state.selectedGid) return;
  const prev = state.objectsByGlobalId.get(state.selectedGid) ?? [];
  for (const m of prev) setHighlighted(m, false);
  state.selectedGid = null;
}

function focusOnMeshes(meshes) {
  if (!meshes.length) return;

  const group = new THREE.Group();
  for (const m of meshes) group.add(m);

  const box = new THREE.Box3().setFromObject(group);
  const center = box.getCenter(new THREE.Vector3());
  const size = box.getSize(new THREE.Vector3()).length();

  state.controls.target.copy(center);

  const dir = new THREE.Vector3(1, 1, 1).normalize();
  state.camera.position.copy(center).add(dir.multiplyScalar(Math.max(size, 1) * 0.8));

  state.camera.near = Math.max(size / 1000, 0.01);
  state.camera.far = Math.max(size * 20, 1000);
  state.camera.updateProjectionMatrix();
  state.controls.update();
}

function showIssuesForGlobalId(gid) {
  if (!state.viewerInfo) return;

  const list = state.issuesByGid.get(gid) ?? [];
  const header = `${gid} — ${list.length} issue(s)`;
  const lines = list.slice(0, 8).map(it => `- [${it.Severity}] ${it.Message}`);
  state.viewerInfo.textContent = [header, ...lines].join("\n");
}

function selectGlobalId(gid) {
  clearSelection();

  state.selectedGid = gid;
  const meshes = state.objectsByGlobalId.get(gid) ?? [];

  for (const m of meshes) setHighlighted(m, true);
  focusOnMeshes(meshes);
  showIssuesForGlobalId(gid);

  console.log("Selected gid:", gid, "meshes:", meshes.length);
}

function onCanvasPick(ev) {
  if (!state.modelRoot) {
    console.log("Model not loaded yet.");
    return;
  }

  const rect = state.renderer.domElement.getBoundingClientRect();
  const mouse = new THREE.Vector2(
    ((ev.clientX - rect.left) / rect.width) * 2 - 1,
    -(((ev.clientY - rect.top) / rect.height) * 2 - 1)
  );

  const raycaster = new THREE.Raycaster();
  raycaster.setFromCamera(mouse, state.camera);

  const hits = raycaster.intersectObject(state.modelRoot, true);
  console.log("hits:", hits.length);

  if (!hits.length) return;

  const hit = hits[0].object;
  const named = findNamedAncestor(hit);
  const gid = named?.name;

  console.log("hit name:", hit.name);
  console.log("resolved gid:", gid);

  if (gid) selectGlobalId(gid);
}

async function main() {
  let isDragging = false;
  let downPos = { x: 0, y: 0 };

  const DRAG_PX = 6;

  const canvas = document.getElementById("viewerCanvas");
  if (!canvas) {
    console.warn("viewerCanvas not found. Add <canvas id='viewerCanvas'> to report.");
    return;
  }

  state.viewerInfo = document.getElementById("viewerInfo") ?? null;

  // Load issues and build lookup
  const issues = await fetchIssues();
  state.issuesByGid = buildIssuesByGlobalId(issues);
  console.log("Issues loaded:", issues.length, "Unique gids:", state.issuesByGid.size);

  // Three setup
  state.renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  state.renderer.setPixelRatio(window.devicePixelRatio);

  state.scene = new THREE.Scene();
  state.camera = new THREE.PerspectiveCamera(45, 1, 0.1, 5000);
  state.camera.position.set(10, 10, 10);

  state.controls = new OrbitControls(state.camera, state.renderer.domElement);
  state.controls.update();

  state.scene.add(new THREE.HemisphereLight(0xffffff, 0x444444, 1.0));
  const dir = new THREE.DirectionalLight(0xffffff, 0.8);
  dir.position.set(10, 20, 10);
  state.scene.add(dir);

  function resize() {
    const w = canvas.clientWidth;
    const h = canvas.clientHeight;
    state.renderer.setSize(w, h, false);
    state.camera.aspect = w / h;
    state.camera.updateProjectionMatrix();
  }
  window.addEventListener("resize", resize);
  resize();

  state.renderer.domElement.addEventListener("pointerdown", (e) => {
    isDragging = false;
    downPos = { x: e.clientX, y: e.clientY };
  });

  state.renderer.domElement.addEventListener("pointermove", (e) => {
    const dx = e.clientX - downPos.x;
    const dy = e.clientY - downPos.y;
    if (dx * dx + dy * dy > DRAG_PX * DRAG_PX) isDragging = true;
  });

  // pointerup as “click” event
  state.renderer.domElement.addEventListener("pointerup", (e) => {
    if (isDragging) return;
    if (e.button !== 0) return;
    onCanvasPick(e);
  });

  // Load GLB
  const loader = new GLTFLoader();
  loader.load("./model.glb", (gltf) => {
    state.modelRoot = gltf.scene;
    state.scene.add(state.modelRoot);

    // Build objectsByGlobalId
    state.objectsByGlobalId = new Map();
    state.modelRoot.traverse((obj) => {
      if (!obj.isMesh) return;
      const gid = obj.name;
      if (!gid) return;

      if (!state.objectsByGlobalId.has(gid)) state.objectsByGlobalId.set(gid, []);
      state.objectsByGlobalId.get(gid).push(obj);
    });

    console.log("GLB loaded. Meshes indexed by GlobalId:", state.objectsByGlobalId.size);
  }, undefined, (err) => {
    console.error("Failed to load model.glb:", err);
  });

  function animate() {
    requestAnimationFrame(animate);
    state.renderer.render(state.scene, state.camera);
  }
  animate();
}

main().catch(console.error);

window.addEventListener("ifcqa:select", (e) => {
  const gid = e.detail?.gid;
  if (gid) selectGlobalId(gid);
});
