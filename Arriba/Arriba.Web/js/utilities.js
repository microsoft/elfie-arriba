var highlightChar = '\\u03C0';
var highlightRangeRegex = new RegExp(highlightChar + '(.+?)' + highlightChar, 'g');
var highlightCharOnlyRegex = new RegExp(highlightChar, 'g');

function log() { console.log.apply(console, arguments) }

function isIE () {
    // Both Chrome and Edge report as "Chrome", only IE doesn't.
    return navigator.userAgent.indexOf('Chrome') === -1;
}

function isEdge() {
    return navigator.userAgent.indexOf('Edge') !== -1
}

Object.values = Object.values || function(o) {
    var vals = [];
    for(var key in o) {
        if(o.hasOwnProperty(key)) vals.push(o[key]);
    }
    return vals;    
}

// From: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Object/assign
Object.assign = Object.assign || function(target, varArgs) { // .length of function is 2
    'use strict';
    if (target == null) { // TypeError if undefined or null
        throw new TypeError('Cannot convert undefined or null to object');
    }
    var to = Object(target);
    for (var index = 1; index < arguments.length; index++) {
        var nextSource = arguments[index];
        if (nextSource != null) { // Skip over if undefined or null
            for (var nextKey in nextSource) {
                // Avoid bugs when hasOwnProperty is shadowed
                if (Object.prototype.hasOwnProperty.call(nextSource, nextKey)) {
                    to[nextKey] = nextSource[nextKey];
                }
            }
        }
    }
    return to;
};

// Like Object.assign, but undefined properties do not overwrite the base.
Object.merge = function() {
    var args = [].slice.call(arguments).map(function(arg) { return (arg || {}).cleaned });
    return Object.assign.apply(this, args);
}

Object.defineProperties(Object.prototype, {
    // Strips undefined properties.
    'cleaned': {
        get: function() { return JSON.parse(JSON.stringify(this)) }
    }
});

Object.map = function(o, f) {
    return Object.keys(o).map(function(key) { return f(key, o[key]) });
}

Number.prototype.clamp = function(min, max) {
    return Math.min(Math.max(this, min), max);
};

String.prototype.startsWith = String.prototype.startsWith || function(searchString, position) {
    position = position || 0;
    return this.indexOf(searchString, position) === position;
};

String.prototype.endsWith = String.prototype.endsWith || function(searchString, position) {
    var subjectString = this.toString();
    if (typeof position !== 'number' || !isFinite(position) || Math.floor(position) !== position || position > subjectString.length) {
        position = subjectString.length;
    }
    position -= searchString.length;
    var lastIndex = subjectString.lastIndexOf(searchString, position);
    return lastIndex !== -1 && lastIndex === position;
};

String.prototype.trimIf = function(prefix) {
    return this.startsWith(prefix)
        ? this.substring(prefix.length)
        : this;
}

// Polyfill.
Array.prototype.includes = Array.prototype.includes || function() {
    return Array.prototype.indexOf.apply(this, arguments) !== -1;
};

// Polyfill.
Array.prototype.find = Array.prototype.find || function(predicate) {
    for (var i = 0; i < this.length; i++) {
        var element = this[i];
        if (predicate.call(arguments[1], element, i, this)) return element;
    }
};

Array.prototype.remove = function(item) {
    var i = this.indexOf(item);
    if (i >= 0) this.splice(i, 1);
    return this;
};

// Additions are inserted at the front to cater to favorites which is the only current consumer of this method.
Array.prototype.toggle = function(item) {
    this.includes(item) ? this.remove(item) : this.unshift(item);
    return this;
}

Array.prototype.emptyToUndefined = function() {
    return this.length ? this : undefined;
}

// Takes two arrays: A (this), B (other)
// Returns three arrays: Only-A, Both, Only-B
// Order is preserved, A takes precedent.
Array.prototype.venn = function(other) {
    var self = this;
    return [
        this.filter(function(item) { return !other.includes(item) }),
        this.filter(function(item) { return other.includes(item) }),
        other.filter(function(item) { return !self.includes(item) }),
    ];
}

Storage.prototype.getJson = function(keyName) {
    return JSON.parse(this.getItem(keyName));
};

Storage.prototype.setJson = function(keyName, keyValue) {
    this.setItem(keyName, JSON.stringify(keyValue));
    this.dispatch(keyName);
};

Storage.prototype.updateJson = function(keyName, f) {
    if (typeof f !== "function") return;
    var value = localStorage.getJson(keyName);
    this.setJson(keyName, f(value));
    this.dispatch(keyName);
}

// Shallow merge the keyObject into localStorage.
Storage.prototype.mergeJson = function(keyName, keyObject) {
    if (typeof keyObject !== "object") return;
    this.setJson(keyName, Object.merge(localStorage.getJson(keyName), keyObject));
    this.dispatch(keyName);
}

// Chrome and Edge do not dispatch the storage event to the current tag (only other tabs).
// IE dispatches to all tabs. In this case we desire the IE behavior and dispatch makes the other browsers simulate it.
Storage.prototype.dispatch = function(keyName) {
    if (isIE()) return;
    if (isEdge()) {
        var e = new Event("storage");
        e.key = keyName;
        dispatchEvent(e);        
    } else {
        dispatchEvent(new StorageEvent("storage", { key: keyName }));                
    }
}


// Highlight values surrounded by Pi characters by wrapping them in <span class="h"></span>
function highlight(value) {
    var replacement = '<span class="h">$1</span>';

    if (value !== undefined) {
        // Escape the content to ensure it's not html
        var escaper = document.createElement("div");
        escaper.innerText = value;
        var escaped = escaper.innerHTML;

        return { __html: escaped.replace(highlightRangeRegex, replacement) };
    }

    return { __html: "" };
};

// Highlight surrounded by Pi characters by wrapping them in <span class="h"></span>
function highlightHtml(value) {
    var replacement = '<span class="h">$1</span>';

    if (value !== undefined) {
        return { __html: value.toString().replace(highlightRangeRegex, replacement) };
    }

    return { __html: "" };
};

// Remove highlight characters for values being compared or queried against rather than rendered
function stripHighlight(value) {
    if (value !== undefined) {
        return value.toString().replace(highlightCharOnlyRegex, "");
    }

    return value;
}

// Build a URL from an object containing a set of parameters { "q": "Query", "T": 50 } => "?q=Query&t=50"
function buildUrlParameters(parameters) {
    var paramUri = "?";

    if (parameters) {
        for (var name in parameters) {
            value = parameters[name];
            if (!value) value = "";
            if (paramUri.length !== 1) paramUri += "&";
            paramUri += encodeURIComponent(name.toLowerCase()) + "=" + encodeURIComponent(value);
        }
    }

    if (paramUri.length === 1) return "";
    return paramUri;
}

// Run an AJAX query which returns JSON [like $.ajax]
function jsonQuery(url, onSuccess, onError, parameters) {
    // Build a URL from the parameters object
    var paramUri = buildUrlParameters(parameters);

    var request = new XMLHttpRequest();
    request.withCredentials = true;
    request.url = url + paramUri;
    request.open('GET', request.url, true);
    

    request.onload = function () {
        if (request.status >= 200 && request.status < 400) {
            var data = JSON.parse(request.responseText);
            onSuccess(data);
        } else {
            onError(request, request.status, request.statusText);
        }
    }

    request.onerror = function () {
        onError(request, request.status, request.statusText);
    }

    request.send();

    return request;
}

// Build an object with a property for each querystring parameter
function getQueryStringParameters() {
    var urlParameters = window.location.search.substring(1);
    var parameterParts = urlParameters.split("&");

    var result = {};

    for (var i = 0; i < parameterParts.length; ++i) {
        if (!parameterParts[i]) continue;
        var parameterAndValue = parameterParts[i].split("=");
        if (parameterAndValue.length != 2) continue;
        result[decodeURIComponent(parameterAndValue[0]).toLowerCase()] = decodeURIComponent(parameterAndValue[1]);
    }

    return result;
}

// Build an array of the parameters starting with a prefix. { "Q": "Query", "T": "Table", "c1": "Name", "c2": "Priority", "c3": 10 }, "C" => [ "Name", "Priority", 10 ]
function getParameterArrayForPrefix(parameters, prefix) {
    var result = [];

    // Build a map of lowercase properties on parameters to the real casing
    var lowercaseMap = {};
    for (var name in parameters) {
        if (parameters.hasOwnProperty(name)) {
            lowercaseMap[name.toLowerCase()] = name;
        }
    }

    // Look for properties starting with the prefix against the lowercase set
    prefix = prefix.toLowerCase();
    for (var i = 1; lowercaseMap.hasOwnProperty(prefix + i.toString()) ; ++i) {
        var exactCaseName = lowercaseMap[prefix + i.toString()];
        result.push(parameters[exactCaseName]);
    }

    return result;
}

// Put an array of parameters onto a parameters object with a prefix ({}, "P", [ "One", "Two", "Three" ]) => { "P1": "One", "P2": "Two", "P3": "Three" }
function addArrayParameters(parameters, prefix, array) {
    if (!array) return;
    for (var i = 0; i < array.length; ++i) {
        parameters[prefix + (i + 1).toString()] = array[i];
    }
}

// Return the first parameter which is a non-empty array, or return an empty array
function firstNonEmptyArray() {
    for (var i = 0; i < arguments.length; ++i) {
        var argument = arguments[i];
        if (argument && argument.length > 0) return argument;
    }

    return [];
}

// Merge a set of objects into the first object and return it.
// Same as Object.assign() for browsers which don't have it.
function merge() {
    var target = arguments[0];

    for (var i = 1; i < arguments.length; ++i) {
        var mergeSource = arguments[i];
        for(var name in mergeSource)
        {
            if (mergeSource.hasOwnProperty(name)) {
                target[name] = mergeSource[name]
            }
        }
    }

    return target;
}

// Get the absolution position of an element, walking up the offsetParent hierarchy
function getAbsolutePosition(element) {
    var position = { top: 0, left: 0 };

    var current = element;
    while(current)
    {
        position.top += element.offsetTop;
        position.left += element.offsetLeft;
        current = current.offsetParent;
    }

    return position;
}

// Convert a single row from an Arriba response into an object with properties for each column name.
function arribaRowToObject(values, rowIndex) {
    if (!values || !values.rows || rowIndex < 0 || values.rows.length <= rowIndex) return {};

    var object = {};
    for(var i = 0; i < values.columns.length; ++i) {
        object[values.columns[i].name] = values.rows[rowIndex][i];
    }

    return object;
}
