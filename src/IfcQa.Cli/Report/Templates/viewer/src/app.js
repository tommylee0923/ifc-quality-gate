import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader.js";

async function fetchIssues() {
  const res = await fetch("./issues.json");
  const data = await res.json();
  const issues = data.Issues ?? data.issues ?? [];
  return issues;
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

function findAnyGlobalIdInText(obj, sampleGids) {
  // Look in common places the converter might store identifiers
  const candidates = [];

  if (obj.name) candidates.push(["name", obj.name]);
  if (obj.uuid) candidates.push(["uuid", obj.uuid]);

  // userData can contain extras
  if (obj.userData) {
    for (const [k, v] of Object.entries(obj.userData)) {
      if (typeof v === "string") candidates.push([`userData.${k}`, v]);
      if (v && typeof v === "object") {
        // shallow scan
        for (const [k2, v2] of Object.entries(v)) {
          if (typeof v2 === "string") candidates.push([`userData.${k}.${k2}`, v2]);
        }
      }
    }
  }

  // Return the first match against sample gids
  for (const [where, text] of candidates) {
    for (const gid of sampleGids) {
      if (text.includes(gid)) return { where, text, gid };
    }
  }
  return null;
}

async function main() {
  const canvas = document.getElementById("viewerCanvas");
  if (!canvas) {
    console.warn("viewerCanvas not found. Add <canvas id='viewerCanvas'> to report.");
    return;
  }

  const issues = await fetchIssues();
  console.log("Issues loaded:", issues.length);

  const issuesByGid = buildIssuesByGlobalId(issues);
  console.log("Unique GlobalIds:", issuesByGid.size);

  // Take a few sample gids from your issues to test against GLB metadata
  const sampleGids = Array.from(issuesByGid.keys()).slice(0, 20);
  console.log("Sample gids:", sampleGids);

  // Basic three setup
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  renderer.setPixelRatio(window.devicePixelRatio);

  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(45, 1, 0.1, 5000);
  camera.position.set(10, 10, 10);

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.target.set(0, 0, 0);
  controls.update();

  scene.add(new THREE.HemisphereLight(0xffffff, 0x444444, 1.0));
  const dir = new THREE.DirectionalLight(0xffffff, 0.8);
  dir.position.set(10, 20, 10);
  scene.add(dir);

  function resize() {
    const w = canvas.clientWidth;
    const h = canvas.clientHeight;
    renderer.setSize(w, h, false);
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
  }
  window.addEventListener("resize", resize);
  resize();

  // Load GLB
  const loader = new GLTFLoader();
  loader.load(
    "./model.glb",
    (gltf) => {
      scene.add(gltf.scene);

      // Fit camera roughly (optional)
      const box = new THREE.Box3().setFromObject(gltf.scene);
      const size = box.getSize(new THREE.Vector3()).length();
      const center = box.getCenter(new THREE.Vector3());
      controls.target.copy(center);
      camera.near = size / 1000;
      camera.far = size * 10;
      camera.position.copy(center).add(new THREE.Vector3(size / 3, size / 3, size / 3));
      camera.updateProjectionMatrix();
      controls.update();

      // === PATH A TEST ===
      let found = null;
      let checked = 0;

      gltf.scene.traverse((obj) => {
        if (found) return;
        checked++;

        const hit = findAnyGlobalIdInText(obj, sampleGids);
        if (hit) {
          found = { obj, ...hit };
        }
      });

      console.log("Traverse checked nodes:", checked);

      if (found) {
        console.log("✅ Found GlobalId inside GLB metadata!");
        console.log("Where:", found.where);
        console.log("Matched gid:", found.gid);
        console.log("Text:", found.text);
        console.log("Object:", found.obj);
      } else {
        console.log("❌ No GlobalId found in name/userData (Path A likely not viable).");
        console.log("Next would be Path B: generate map.json during export.");
      }
    },
    (progress) => {
      // optional
    },
    (err) => {
      console.error("Failed to load model.glb:", err);
    }
  );

  function animate() {
    requestAnimationFrame(animate);
    renderer.render(scene, camera);
  }
  animate();
}

main().catch(console.error);
