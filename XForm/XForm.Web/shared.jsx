window.log = function() { console.log.apply(console, arguments) }

window.xhr = (urlRoot, path, params) => {
    return new Promise((resolve, reject) => {
        const encodedParams = Object.keys(params)
            .filter(k => params[k] !== undefined)
            .map(k => `${k}=${encodeURIComponent(params[k])}`).join('&')
        const xhr = new XMLHttpRequest();
        xhr.withCredentials = false;
        xhr.open("GET", `${urlRoot}/${path}?${encodedParams}`, true); // For testing: http://httpbin.org/post
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
