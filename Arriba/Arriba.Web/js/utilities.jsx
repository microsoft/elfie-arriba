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

Blob.prototype.readAsText = function() {
    return new Promise((resolve, reject) => {
        var reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsText(this);
    });
}
