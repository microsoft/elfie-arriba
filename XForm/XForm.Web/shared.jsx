window.log = function() { console.log.apply(console, arguments) }

window.xhr = (path, body) => {
    return new Promise((resolve, reject) => {
        var host = "localhost:5073"
        const pathParams = path;
        var xhr = new XMLHttpRequest();
        xhr.withCredentials = false;
        xhr.open(body ? "POST" : "GET", `http://${host}/${pathParams}`, true); // For testing: http://httpbin.org/post
        xhr.onload = () => {
            const responseText = xhr.responseText;

            if (true || xhr.status >= 200 && xhr.status < 300 || xhr.status === 404) { // 404 workaround for delete table.
                try {
                    const o = JSON.parse(responseText);

                    // Custom logic for XForm
                    function sugar() {
                        if (o.colIndex.Count === 0) {
                            return o.rows[0][0];
                        } else if (o.colIndex.Valid === 0) {
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
        xhr.send(typeof body === "string" ? body : JSON.stringify(body));
    });
};
