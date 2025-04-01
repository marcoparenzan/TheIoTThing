export function initWorkspace(name, elName) {
    debugger;

    if (window.flowUILib == undefined) {
        window.flowUILib = {};
    }

    if (window.flowUILib[name] != undefined) return;
    window.flowUILib[name] = {};
}

export function createVariable(name, varName) {
    window.flowUILib[name].workspace.createVariable(varName);
}

export function loadWorkspace(name, json) {
    let state = JSON.parse(json);
}

export function saveWorkspace(name) {
    let json = JSON.stringify(state);
    return json;
}
