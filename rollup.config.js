import typescript from '@rollup/plugin-typescript';
import { terser } from "@chiogen/rollup-plugin-terser";
import { nodeResolve } from '@rollup/plugin-node-resolve';
import { babel } from '@rollup/plugin-babel';
import commonjs from '@rollup/plugin-commonjs';
import css from "rollup-plugin-import-css";

export default [
    transformTypeScript('./src/WebFormsCore/Scripts/form.ts'),
    transformTypeScript('./src/WebFormsCore/Scripts/webforms-polyfill.ts'),
    transformTypeScript('./src/WebFormsCore.Extensions.Choices/Scripts/choices.ts'),
    transformTypeScript('./src/WebFormsCore.Extensions.TinyMCE/Scripts/tiny.ts')
]

function transformTypeScript(path) {
    return {
        input: path,
        output: [
            { file: path.replace('.ts', '.js'), format: 'iife' },
            { file: path.replace('.ts', '.min.js'), format: 'iife', plugins: [terser()] }
        ],
        plugins: [
            css(),
            typescript({
                allowJs: true,
                allowSyntheticDefaultImports: true
            }),
            nodeResolve(),
            commonjs()
        ]
    }
}

function transformJavaScript(path) {
    return {
        input: path,
        output: [
            { file: path.replace('.js', '.min.js'), format: 'iife', plugins: [terser()] }
        ],
        plugins: [nodeResolve(), babel({ babelHelpers: 'bundled' })]
    }
}