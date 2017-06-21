// Similar to utilities.js except:
// a) Not run in the global context (does not use script-loader).
// b) Babel/JSX supported.

import Promise from "promise-polyfill";

if (!window.Promise) {
    window.Promise = Promise;
}
