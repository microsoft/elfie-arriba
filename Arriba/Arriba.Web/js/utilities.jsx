// Similar to utilities.js except:
// a) Not run in the global context (does not use script-loader).
// b) Babel/JSX supported.

import Promise from "promise-polyfill";

if (!window.Promise) {
    window.Promise = Promise;
}

window.xhr = (path, body) => {
    return new Promise((resolve, reject) => {
        var xhr = new XMLHttpRequest();
        xhr.withCredentials = true;
        xhr.open(body ? "POST" : "GET", configuration.url + "/" + path, true); // For testing: http://httpbin.org/post
        xhr.onload = () => {
            if (xhr.status >= 200 && xhr.status < 300 || xhr.status === 404) { // 404 workaround for delete table.
                const o = JSON.parse(xhr.responseText);
                resolve(o.content || o);
            } else {
                reject(xhr);
            }
        };
        xhr.onerror = () => reject(xhr);
        xhr.send(typeof body === "string" ? body : JSON.stringify(body));
    });
};

// Example: Object.diff({a: 1, b: 2, c: 3}, {b: 3, c: 3, d: 4}) >> Set(a, b, d)
Object.diff = function(a, b) {
    const makeSet = iterable => {
        // IE Set.ctor doesn't take args. Must manually add.
        const s = new Set(iterable);
        if (isIE()) iterable.forEach(k => s.add(k));
        return s;
    }

    const allKeys = makeSet([...Object.keys(a), ...Object.keys(b)]);
    return makeSet([...allKeys.values()].filter(i => a[i] !== b[i]));
};

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
