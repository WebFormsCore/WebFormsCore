import typescript from '@rollup/plugin-typescript';
import { terser } from "@chiogen/rollup-plugin-terser";
import { nodeResolve } from '@rollup/plugin-node-resolve';

export default [
    {
        input: './src/WebFormsCore/Scripts/form.ts',
        output: [
            { file: './src/WebFormsCore/Scripts/form.js', format: 'iife' },
            { file: './src/WebFormsCore/Scripts/form.min.js', format: 'iife', plugins: [terser()] }
        ],
        plugins: [typescript(), nodeResolve()]
    }
]