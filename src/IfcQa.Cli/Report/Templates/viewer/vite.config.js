import { defineConfig } from "vite";
import path from "path";

export default defineConfig({
    build: {
        lib: {
            entry: path.resolve(__dirname, "src/app.js"),
            name: "IfcQaViewer",
            formats: ["iife"],
            fileName: () => "viewer.bundle.js",
        },
        outDir: path.resolve(__dirname, "../viewer"),
        emptyOutDir: false,
    },
});