// Similar to utilities.js except:
// a) Not run in the global context (does not use script-loader).
// b) Babel/JSX supported.

import Promise from "promise-polyfill";

if (!window.Promise) {
    window.Promise = Promise;
}

window.xhr = (path, params, body) => {
    return new Promise((resolve, reject) => {
        const pathParams = path + buildUrlParameters(params);
        var xhr = new XMLHttpRequest();
        xhr.withCredentials = true;
        xhr.open(body ? "POST" : "GET", configuration.url + "/" + pathParams, true); // For testing: http://httpbin.org/post
        xhr.onload = () => {
            if (xhr.status >= 200 && xhr.status < 300 || xhr.status === 404) { // 404 workaround for delete table.
                const o = JSON.parse(xhr.responseText);
                resolve(o.content || o);
            } else {
                reject(xhr);
            }
        };
        xhr.onerror = () => {
            if (errorBar) errorBar(`Request failed: ${pathParams}`);
            reject(xhr);
        };
        xhr.send(typeof body === "string" ? body : JSON.stringify(body));
    });
};

Object.keysNoFunctions = function(o) {
    const keys = Object.keys(o)
    return keys.filter(k => typeof o[k] !== "function")
}

// Example: Object.diff({a: 1, b: 2, c: 3}, {b: 3, c: 3, d: 4}) >> Set(a, b, d)
// Note: Ignores function values are they don't compare predictably.
Object.diff = function(a, b) {
    const makeSet = iterable => {
        // IE Set.ctor doesn't take args. Must manually add.
        const s = new Set(iterable);
        if (isIE()) iterable.forEach(k => s.add(k));
        return s;
    }

    const allKeys = makeSet([...Object.keysNoFunctions(a), ...Object.keysNoFunctions(b)]);
    return makeSet([...allKeys.values()].filter(i => a[i] !== b[i]));
};

Array.prototype.toObject = function(keyFunc) {
    return this.reduce((o, item) => {
        o[keyFunc(item)] = item;
        return o;
    }, {});
}

Set.prototype.values = Set.prototype.values || function() { // For Edge/IE.
    var values = [];
    this.forEach(v => values.push(v));
    return values;
};

Set.prototype.hasAny = function(...values) {
    return values.any(v => this.has(v));
};

Blob.prototype.readAsText = function() {
    return new Promise((resolve, reject) => {
        var reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsText(this);
    });
};
