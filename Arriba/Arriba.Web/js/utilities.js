var highlightChar = '\\u03C0';
var highlightRangeRegex = new RegExp(highlightChar + '(.+?)' + highlightChar, 'g');
var highlightCharOnlyRegex = new RegExp(highlightChar, 'g');

// Polyfill for Array.includes
if (!Array.prototype.includes) {
    Array.prototype.includes = function() {
        return Array.prototype.indexOf.apply(this, arguments) !== -1;
    };
}

// Highlight values surrounded by Pi characters by wrapping them in <span class="h"></span>
function highlight(value) {
    var replacement = '<span class="h">$1</span>';

    if (value !== undefined) {
        return { __html: unescape(value.toString()).replace(highlightRangeRegex, replacement) };
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