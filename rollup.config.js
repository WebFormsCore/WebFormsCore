import typescript from '@rollup/plugin-typescript';
import { terser } from "@chiogen/rollup-plugin-terser";

export default [
    {
        input: './src/WebFormsCore/Scripts/form.ts',
        output: [
            { file: './src/WebFormsCore/Scripts/form.js' },
            { file: './src/WebFormsCore/Scripts/form.min.js', plugins: [terser()] }
        ],
        plugins: [typescript()]
    }
]