import worker from './Project.wasm';
import start from './Project.js';

function load() {
    return new Promise(resolve => {
        const Module = {};

        Module.locateFile = () => './Project.wasm';

        Module.instantiateWasm = async (info, receiveInstance) => {
            const result = await WebAssembly.instantiate(worker, info);

            receiveInstance(result);
        };

        Module.onRuntimeInitialized = () => {
            const initialize = Module.cwrap('Initialize', null, []);
            const beginRequest = Module.cwrap('BeginRequest', null, ['number', 'array', 'number', 'array']);
            const endRequest = Module.cwrap('EndRequest', null, ['number']);

            initialize()

            const encoder = new TextEncoder();
            const decoder = new TextDecoder('utf-8');
            const getInt32 = (ptr) => {
                const memory = Module['HEAP8'];
                return ((memory[ptr + 3] & 0xFF) << 24) | ((memory[ptr + 2] & 0xFF) << 16) | ((memory[ptr + 1] & 0xFF) << 8) | (memory[ptr] & 0xFF);
            }

            resolve((requestContext, requestBody) => {
                const json = encoder.encode(JSON.stringify(requestContext));
                const ptr = beginRequest(json.length, json, requestBody.length, requestBody);
                const memory = Module['HEAP8'];

                let offset = ptr;

                const responseBodyLength = getInt32(offset);
                offset += 4;

                const responseLength = getInt32(offset);
                offset += 4;

                const responseBody = memory.subarray(offset, offset + responseBodyLength);
                offset += responseBodyLength;

                const response = JSON.parse(decoder.decode(memory.subarray(offset, offset + responseLength)));
                const body = new ReadableStream({
                    start(controller) {
                        controller.enqueue(responseBody);
                    },
                    pull(controller) {
                        controller.close();
                        endRequest(ptr);
                    },
                    cancel() {
                        endRequest(ptr);
                    }
                });

                return { body, response };
            });
        }

        start(Module);
    })
}

const processRequest = await load();

export default {
    async fetch(request, env, ctx) {
        const arrayBuffer = await request.arrayBuffer()
        const requestBody = new Uint8Array(arrayBuffer)
        const headers = {}

        for (const [key, value] of request.headers) {
            headers[key] = value
        }

        const { body, response } = processRequest(
            {
                method: request.method,
                url: request.url,
                headers,
            },
            requestBody
        );

        return new Response(body, response);
    },
}