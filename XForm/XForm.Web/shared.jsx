window.log = function() { console.log.apply(console, arguments) }

window.encodeParams = function(params) {
    return Object.keys(params)
        .filter(k => params[k] !== undefined)
        .map(k => `${k}=${encodeURIComponent(params[k])}`).join('&')
}

window.xhr = (path, paramObj) => {
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.withCredentials = true;
        xhr.open("GET", `${window.xhr.urlRoot}/${path}?${encodeParams(paramObj)}`, true); // For testing: http://httpbin.org/post
        xhr.onload = () => {
            const responseText = xhr.responseText;

            if (true || xhr.status >= 200 && xhr.status < 300 || xhr.status === 404) { // 404 workaround for delete table.
                try {
                    const o = JSON.parse(responseText);

                    // Custom logic for XForm
                    function sugar() {
                        // For 'Suggest' oo 'Count' outputs, convert the one result row to a normal object
                        if (path === "suggest" || path === "count") {
                            const dict = {};
                            Object.keys(o.colIndex).forEach(k => dict[k] = o.rows[0][o.colIndex[k]])
                            return dict
                        } else {
                            return {
                                cols: Object.keys(o.colIndex),
                                rows: o.rows,
                            }
                        }
                    }
                    resolve(sugar(o));
                } catch(e) {
                    resolve(responseText)
                }
            } else {
                reject(xhr);
            }
        };
        xhr.onerror = e => {
            reject(xhr);
        };
        xhr.send();
    });
};

window.CachableReusedRequest = class CachableReusedRequest {
    constructor(path) {
        this._path = path;
        this.reset();
    }
    reset() {
        this._cache = {};
    }
    update(paramObj, then) {
        if (this._xhr) this._xhr.abort();

        if (!paramObj) return then(); // return undef

        const paramStr = encodeParams(paramObj)
        const cache = this.caching && this._cache[paramStr];
        if (cache) {
            then(cache);
        } else {
            const xhr = this._xhr = new XMLHttpRequest();
            xhr.withCredentials = true;
            xhr.open("GET", `${window.xhr.urlRoot}/${this._path}?${paramStr}`);
            xhr.onload = () => {
                if (xhr.status >= 200 && xhr.status < 400) {
                    const json = JSON.parse(xhr.responseText);
                    if (this.caching) this._cache[paramStr] = json;
                    then(json);
                }
            }
            xhr.send();
        }
    }
}

window.o2c = function(o) {
    // Object --> css class names (string)
    // { a: true, b: false, c: 1 } --> 'a c'
    return Object.keys(o).filter(k => o[k]).join(' ')
}
