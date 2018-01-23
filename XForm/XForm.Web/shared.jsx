window.log = function() { console.log.apply(console, arguments) }

window.xhr = (path, params) => {
    return new Promise((resolve, reject) => {
        const host = "localhost:5073"
        const encodedParams = Object.keys(params)
            .filter(k => params[k] !== undefined)
            .map(k => `${k}=${encodeURIComponent(params[k])}`).join('&')
        const xhr = new XMLHttpRequest();
        xhr.withCredentials = false;
        xhr.open("GET", `http://${host}/${path}?${encodedParams}`, true); // For testing: http://httpbin.org/post
        xhr.onload = () => {
            const responseText = xhr.responseText;

            if (true || xhr.status >= 200 && xhr.status < 300 || xhr.status === 404) { // 404 workaround for delete table.
                try {
                    const o = JSON.parse(responseText);

                    // Custom logic for XForm
                    function sugar() {
                        if (o.colIndex.Count === 0 || o.colIndex.Valid === 0) {
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
