/******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId])
/******/ 			return installedModules[moduleId].exports;
/******/
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// identity function for calling harmony imports with the correct context
/******/ 	__webpack_require__.i = function(value) { return value; };
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, {
/******/ 				configurable: false,
/******/ 				enumerable: true,
/******/ 				get: getter
/******/ 			});
/******/ 		}
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = 27);
/******/ })
/************************************************************************/
/******/ ([
/* 0 */
/***/ (function(module, exports, __webpack_require__) {

var map = {
	"./configuration/Configuration.jsx": 10
};
function webpackContext(req) {
	return __webpack_require__(webpackContextResolve(req));
};
function webpackContextResolve(req) {
	var id = map[req];
	if(!(id + 1)) // check for number
		throw new Error("Cannot find module '" + req + "'.");
	return id;
};
webpackContext.keys = function webpackContextKeys() {
	return Object.keys(map);
};
webpackContext.resolve = webpackContextResolve;
module.exports = webpackContext;
webpackContext.id = 0;


/***/ }),
/* 1 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
// DefaultConfiguration.jsx contains default configuration settings for this site instance.
// Add a copy in configuration\configuration.jsx to override these settings.

/* harmony default export */ __webpack_exports__["a"] = {
    // Arriba Service URL to query
    // Hardcode to the name of your service. Use port 42784 for HTTP, 42785 for HTTPS.
    url: "http://" + window.location.hostname + ":42784",

    // Name of tool to show [top right and elsewhere]
    toolName: "Arriba",

    // Theme to use [see theme.css for options and to define others]
    theme: "theme-teal",

    // E-Mail addresses to whom feedback should go
    feedbackEmailAddresses: "",

    // Listing: Default Columns and Sort Order to show per table
    listingDefaults: {
        /* "<TableName">: { columns: ["<Col1>", "Col2", "..."], sortColumn: "<SortColumnName>", sortOrder: "asc" } */
    },

    // Listing: Default details rendering per table
    customDetailsProviders: {
        /* "<TableName>": <ReactClassNameUnquoted> */
    },

    // Grid: Pre-defined Grid queries
    gridDefaultQueries: {
        "Clear": {}
    },

    // Content to show before a query is typed
    splashContent: React.createElement(
        "div",
        null,
        "Arriba is a data exploration, free text search, and structured search engine."
    ),

    // Content to show if query fails due to access denied (401)
    accessDeniedContent: React.createElement(
        "span",
        null,
        "Access to Arriba Search denied."
    ),

    // Content to show if query fails because service is unavailable (404, 500)
    serviceUnavailableContent: React.createElement(
        "span",
        null,
        "Arriba Search is currently unavailable."
    )
};

/***/ }),
/* 2 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";

// Write a simple table with name and value for each property in a data set
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "DetailsTable",

    onAddClause: function (e) {
        var name = e.target.getAttribute("data");
        this.props.onAddClause(name, stripHighlight(this.props.data[name]));
    },
    render: function () {
        var details = [];

        for (var name in this.props.data) {
            var commands = [];
            var rawValue = stripHighlight(this.props.data[name]);

            // Add 'Pivot to Grid' URL
            var gridUrl = "Grid.html" + buildUrlParameters({ q: this.props.query, t: this.props.table, R1: name + ">" });
            commands.push(React.createElement("a", { href: gridUrl, className: "icon-view-all-albums icon-column-heading", title: "Grid By " + name }));

            var showAddClause = this.props.onAddClause;
            if (showAddClause && rawValue.length < 100) commands.push(React.createElement("span", { className: "icon-zoom icon-column-heading", onClick: this.onAddClause, title: "Add Clause to Query", data: name }));

            var commandCell = null;
            if (commands) {
                var width = 22 * commands.length + "px";
                commandCell = React.createElement(
                    "td",
                    { style: { minWidth: width, width: width } },
                    commands
                );
            }

            details.push(React.createElement(
                "tr",
                { key: name },
                commandCell,
                React.createElement(
                    "td",
                    null,
                    name
                ),
                React.createElement(
                    "td",
                    null,
                    React.createElement("div", { className: "detailsValue", dangerouslySetInnerHTML: highlight(this.props.data[name]), title: name + ':' + rawValue })
                )
            ));
        }

        return React.createElement(
            "table",
            { className: "detailsTable" },
            React.createElement(
                "tbody",
                null,
                details
            )
        );
    }
});

/***/ }),
/* 3 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
// An error page to show in place of all content when access is denied or the site is down.
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "ErrorPage",

    render: function () {
        return React.createElement(
            "div",
            { className: "errorPage" },
            React.createElement(
                "h1",
                null,
                React.createElement(
                    "span",
                    { className: "errorTitle" },
                    this.props.title
                ),
                React.createElement(
                    "span",
                    { className: "errorStatus" },
                    this.props.status == 0 ? "" : this.props.status
                )
            ),
            React.createElement(
                "article",
                null,
                React.createElement(
                    "p",
                    null,
                    this.props.message
                )
            )
        );
    }
});

/***/ }),
/* 4 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
// QueryStats is a bar just under the SearchHeader. It shows errors, if any, or the current query, returned count, and runtime.
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "QueryStats",

    onTableTileClick: function (name, e) {
        this.props.onSelectedTableChange(name);
    },
    render: function () {
        if (this.props.error) {
            // If the query didn't return, show an error
            return React.createElement(
                "div",
                { className: "queryStats" },
                React.createElement("span", { className: "error-icon icon" }),
                React.createElement(
                    "span",
                    { className: "error" },
                    this.props.error
                )
            );
        } else {
            // If there's no query, keep this area empty
            if (!this.props.allCountData || !this.props.allCountData.content) return null;
            var allCountContent = this.props.allCountData.content;

            // Write tiles with results per table
            var tiles = [];
            for (var i = 0; i < allCountContent.length; ++i) {
                var tableResult = allCountContent[i];

                var cssClasses = "";
                var parts = [];

                if (tableResult.tableName === this.props.currentTable) cssClasses += " current";

                if (!tableResult.allowedToRead) {
                    parts.push(React.createElement("span", { className: "lock-icon icon" }));
                } else if (!tableResult.succeeded) {
                    parts.push(React.createElement("span", { className: "error-icon icon" }));
                } else {
                    parts.push(React.createElement(
                        "span",
                        { className: "countValue" },
                        tableResult.count.toLocaleString()
                    ));
                }

                tiles.push(React.createElement(
                    "span",
                    { key: "tableTile_" + tableResult.tableName, className: cssClasses + " statsForTable clickable", onClick: this.onTableTileClick.bind(this, tableResult.tableName) },
                    parts,
                    React.createElement(
                        "span",
                        null,
                        tableResult.tableName
                    )
                ));
            }

            // Write details for selected table
            var selectedDetails = [];
            if (this.props.selectedData && this.props.selectedData.content) {
                var selectedContent = this.props.selectedData.content;

                if (selectedContent.query.where) {
                    selectedDetails.push(React.createElement(
                        "span",
                        null,
                        "for ",
                        React.createElement(
                            "span",
                            { className: "h" },
                            selectedContent.query.where
                        )
                    ));
                }

                if (selectedContent.details.errors) {
                    selectedDetails.push(React.createElement(
                        "span",
                        null,
                        "\xA0",
                        React.createElement("span", { className: "error-icon icon" }),
                        React.createElement(
                            "b",
                            null,
                            selectedContent.details.errors
                        )
                    ));
                }

                if (selectedContent.details.warnings) {
                    selectedDetails.push(React.createElement(
                        "span",
                        null,
                        "\xA0",
                        React.createElement("span", { className: "icon-warning icon" }),
                        React.createElement(
                            "b",
                            null,
                            selectedContent.details.warnings
                        )
                    ));
                }

                if (selectedContent.details.accessDeniedColumns) {
                    var deniedColumnList = "Denied Columns: ";
                    for (var i = 0; i < selectedContent.details.accessDeniedColumns.length; ++i) {
                        if (i > 0) deniedColumnList += ", ";
                        deniedColumnList += selectedContent.details.accessDeniedColumns[i];
                    }

                    selectedDetails.push(React.createElement(
                        "span",
                        null,
                        "\xA0",
                        React.createElement("span", { className: "icon-lock icon", title: deniedColumnList })
                    ));
                }

                if (this.props.rssUrl && selectedContent.details.succeeded) {
                    selectedDetails.push(React.createElement(
                        "a",
                        { title: "RSS Link", target: "_blank", href: this.props.rssUrl },
                        React.createElement("img", { className: "rss", src: "icons/feed-icon-14x14.png", style: { position: "relative", top: "1px" }, alt: "RSS" })
                    ));
                }

                if (this.props.csvUrl && selectedContent.details.succeeded) {
                    selectedDetails.push(React.createElement(
                        "a",
                        { title: "Download CSV", target: "_blank", href: this.props.csvUrl },
                        React.createElement("span", { className: "icon-download" })
                    ));
                }
            }
        }

        return React.createElement(
            "div",
            { className: "queryStats" },
            tiles,
            selectedDetails
        );
    }
});

/***/ }),
/* 5 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
function _newArrowCheck(innerThis, boundThis) { if (innerThis !== boundThis) { throw new TypeError("Cannot instantiate an arrow function"); } }

// SearchHeader contains the top bar - branching, the search box, and top-level buttons
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "SearchHeader",

    getInitialState: function () {
        return { suggestions: [], sel: 0, completed: "", completionCharacters: [] };
    },
    componentDidMount: function () {
        var _this = this;

        searchBox.focus();
        this.handleClickDocument = function (e) {
            _newArrowCheck(this, _this);

            if (e.target === searchBox) return; // Don't hide when clicking on input[text]
            this.setState({ suggestions: [] });
        }.bind(this);
        document.addEventListener("click", this.handleClickDocument);
    },
    componentWillUnmount: function () {
        document.removeEventListener("click", this.handleClickDocument);
    },
    onInput: function (e) {
        this.setQuery(e.target.value);
    },
    handleKeyDown: function (e) {
        if (!this.state.suggestions.length) return;
        if (e.key === "ArrowUp") {
            this.setState({ sel: (this.state.sel - 1).clamp(0, this.state.suggestions.length - 1) });
            e.stopPropagation();
        }
        if (e.key === "ArrowDown") {
            this.setState({ sel: (this.state.sel + 1).clamp(0, this.state.suggestions.length - 1) });
            e.stopPropagation();
        }
        if (e.key === "Enter" || this.state.completionCharacters.includes(e.key)) {
            var suffix = e.key === "Enter" || e.key === "Tab" || e.key === " " ? "" : e.key;
            var newQuery = this.state.completed + this.state.suggestions[this.state.sel].completeAs + " " + suffix;
            this.setQuery(newQuery);
            e.preventDefault(); // Suppress focus tabbing.
        }
        if (e.key === "Escape") {
            this.setState({ suggestions: [] });
        }
    },
    handleClick: function (item) {
        this.setQuery(this.state.completed + item.completeAs + " ");
        searchBox.focus();
    },
    setQuery: function (query) {
        var _this2 = this;

        this.props.onSearchChange(query);

        if (this.lastRequest) this.lastRequest.abort();
        this.lastRequest = jsonQuery(configuration.url + "/suggest?q=" + encodeURIComponent(query), function (data) {
            _newArrowCheck(this, _this2);

            this.setState({
                suggestions: data.content.suggestions,
                sel: 0,
                completed: data.content.complete,
                completionCharacters: data.content.completionCharacters.map(function (c) {
                    _newArrowCheck(this, _this2);

                    return { "\t": "Tab" }[c] || c;
                }.bind(this))
            });
        }.bind(this), function (xhr, status, err) {
            _newArrowCheck(this, _this2);

            return console.error(xhr.url, status, err.toString());
        }.bind(this));
    },
    render: function () {
        var _this3 = this;

        var tables = this.props.tables || [];

        var suggestions = this.state.suggestions.length <= 0 ? null : React.createElement(
            "div",
            { className: "suggestions" },
            this.state.suggestions.map(function (item, index) {
                _newArrowCheck(this, _this3);

                return React.createElement(
                    "div",
                    { className: "suggestion " + (this.state.sel == index ? "suggestion-sel" : ""),
                        onClick: this.handleClick.bind(this, item)
                    },
                    React.createElement(
                        "span",
                        { style: { opacity: 0.3 } },
                        this.state.completed
                    ),
                    item.display
                );
            }.bind(this))
        );

        return React.createElement(
            "div",
            { className: "header theme-background-medium" },
            React.createElement(
                "div",
                { className: "title font-light theme-background-vdark" },
                React.createElement(
                    "a",
                    { href: "/" },
                    this.props.name
                )
            ),
            React.createElement(
                "div",
                { className: "searchBar" },
                React.createElement("input", { id: "searchBox", ref: "searchBox", type: "text", placeholder: "Search for " + tables.join(", ") + "...", tabIndex: "1", onInput: this.onInput, value: this.props.query, onKeyDown: this.handleKeyDown }),
                React.createElement(
                    "div",
                    { className: "searchIcon" },
                    React.createElement("i", { className: "icon-find" })
                ),
                suggestions
            ),
            React.createElement(
                "div",
                { className: "buttons" },
                React.createElement(
                    "a",
                    { className: "theme-background-dark", href: "mailto:?subject=" + encodeURIComponent(this.props.name) + ": " + encodeURIComponent(this.props.query) + "&body=" + encodeURIComponent(window.location.href) },
                    React.createElement("i", { className: "icon-mail", title: "Mail" })
                ),
                React.createElement(
                    "a",
                    { className: "theme-background-dark", href: "mailto:" + encodeURIComponent(this.props.feedbackEmailAddresses) + "?subject=" + encodeURIComponent(this.props.name) + " Feedback" },
                    React.createElement("i", { className: "icon-emoji2", title: "Feedback" })
                ),
                React.createElement(
                    "a",
                    { className: "theme-background-dark", href: "/?help=true" },
                    React.createElement("i", { className: "icon-help", title: "Help" })
                )
            )
        );
    }
});

/***/ }),
/* 6 */
/***/ (function(module, exports, __webpack_require__) {

// style-loader: Adds some css to the DOM by adding a <style> tag

// load the styles
var content = __webpack_require__(20);
if(typeof content === 'string') content = [[module.i, content, '']];
// add the styles to the DOM
var update = __webpack_require__(24)(content, {});
if(content.locals) module.exports = content.locals;
// Hot Module Replacement
if(false) {
	// When the styles change, update the <style> tags
	if(!content.locals) {
		module.hot.accept("!!./node_modules/css-loader/index.js!./Search.css", function() {
			var newContent = require("!!./node_modules/css-loader/index.js!./Search.css");
			if(typeof newContent === 'string') newContent = [[module.id, newContent, '']];
			update(newContent);
		});
	}
	// When the module is disposed, remove the <style> tags
	module.hot.dispose(function() { update(); });
}

/***/ }),
/* 7 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
Object.defineProperty(__webpack_exports__, "__esModule", { value: true });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__ErrorPage__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__QueryStats__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__SearchHeader__ = __webpack_require__(5);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__DefaultConfiguration__ = __webpack_require__(1);
__webpack_require__(6);





// NOTE: Depends on configuration from zConfiguration.jsx.

window.configuration = __WEBPACK_IMPORTED_MODULE_3__DefaultConfiguration__["a" /* default */];
var optionalContext = __webpack_require__(0);
if (optionalContext.keys().includes("./configuration/Configuration.jsx")) {
    window.configuration = optionalContext("./configuration/Configuration.jsx").default;
}

// The GridFunctionCell is the corner cell which shows the aggregation function and column [COUNT(*)].
var GridFunctionCell = React.createClass({
    displayName: "GridFunctionCell",

    getInitialState: function () {
        return {
            isEditing: false,
            aggregationFunction: this.props.aggregationFunction,
            aggregateColumn: this.props.aggregateColumn
        };
    },
    handleEdit: function () {
        this.setState({ isEditing: true });
    },
    handleChange: function () {
        this.setState({ aggregationFunction: this.refs.aggregationFunction.value, aggregateColumn: this.refs.aggregateColumn.value });
    },
    handleCancel: function () {
        this.setState(this.getInitialState());
    },
    handleAccept: function () {
        this.setState({ isEditing: false });
        this.props.onChange(this.refs.aggregationFunction.value, this.refs.aggregateColumn.value);
    },
    isNumericColumn: function (t) {
        t = t.toLowerCase();
        return t.indexOf("int") === 0 || t.indexOf("uint") === 0 || t === "short" || t === "byte" || t === "float" || t === "double";
    },
    render: function () {
        if (this.state.isEditing) {
            var columns = [];

            if (this.state.aggregationFunction === "COUNT") {
                columns.push(React.createElement("option", { key: "C*", value: "" }));
            } else {
                for (var i = 0; i < this.props.allColumns.length; ++i) {
                    if (!this.isNumericColumn(this.props.allColumns[i].type)) continue;
                    var columnName = this.props.allColumns[i].name;
                    columns.push(React.createElement(
                        "option",
                        { key: columnName, value: columnName },
                        columnName
                    ));
                }
            }

            return React.createElement(
                "td",
                { className: "grid-heading" },
                React.createElement("span", { className: "icon-cancel icon-column-heading clickable", title: "Cancel", onClick: this.handleCancel }),
                React.createElement("span", { className: "icon-accept icon-column-heading clickable", title: "Done", onClick: this.handleAccept }),
                React.createElement(
                    "div",
                    { className: "cell" },
                    React.createElement(
                        "select",
                        { ref: "aggregationFunction", value: this.state.aggregationFunction, onChange: this.handleChange },
                        React.createElement(
                            "option",
                            null,
                            "COUNT"
                        ),
                        React.createElement(
                            "option",
                            null,
                            "SUM"
                        ),
                        React.createElement(
                            "option",
                            null,
                            "MIN"
                        ),
                        React.createElement(
                            "option",
                            null,
                            "MAX"
                        )
                    ),
                    "\xA0of\xA0",
                    React.createElement(
                        "select",
                        { ref: "aggregateColumn", value: this.state.aggregateColumn, onChange: this.handleChange },
                        columns
                    )
                )
            );
        }

        return React.createElement(
            "td",
            { className: "grid-heading", onClick: this.handleEdit },
            React.createElement(
                "div",
                null,
                this.props.aggregationFunction + "(" + this.props.aggregateColumn + ")"
            )
        );
    }
});

// GridHeadingCell renders the cells used in the first row and column
var GridHeadingCell = React.createClass({
    displayName: "GridHeadingCell",

    getInitialState: function () {
        return {
            isEditing: false,
            label: this.props.label,
            query: this.props.value
        };
    },
    componentDidMount: function () {
        if (this.props.value === "All") document.body.addEventListener("keydown", this.handleKeyDown);
    },
    handleKeyDown: function (e) {
        if (e.altKey) {
            // Alt+Right Arrow -> new column
            if (e.keyCode == 39 && this.props.type === "column") {
                this.handleEdit();
            }

            // Alt+Down Arrow -> new row
            if (e.keyCode == 40 && this.props.type === "row") {
                this.handleEdit();
            }
        }
    },
    handleRemove: function (e) {
        this.props.onChange(this.props.type, this.props.index, "", "");
        e.stopPropagation();
    },
    handleEdit: function (e) {
        var editQuery = this.props.value === "All" ? "" : this.props.value;
        this.setState({ isEditing: true, query: editQuery });
        e.stopPropagation();
    },
    handleQueryChange: function (e) {
        this.setState({ query: e.target.value });
    },
    handleLabelChange: function (e) {
        this.setState({ label: e.target.value });
    },
    handleCancel: function (e) {
        this.setState({ isEditing: false, query: this.props.value, label: this.props.label });
    },
    handleAccept: function (e) {
        this.props.onChange(this.props.type, this.props.index, this.refs.query.value, this.refs.label.value);
        this.setState({ isEditing: false });
    },
    handleKey: function (e) {
        // ESC: Cancel change
        if (e.keyCode === 27) {
            this.setState({ isEditing: false, query: this.props.value, label: this.props.label });
        }

        // Enter: Change query
        if (e.keyCode === 13) {
            this.props.onChange(this.props.type, this.props.index, this.refs.query.value, this.refs.label.value);
            this.setState({ isEditing: false });
            return false;
        }
    },
    render: function () {
        if (this.state.isEditing) {
            return React.createElement(
                "td",
                { className: "grid-heading editing" },
                React.createElement("span", { className: "icon-cancel icon-column-heading clickable", title: "Cancel", onClick: this.handleCancel }),
                React.createElement("span", { className: "icon-accept icon-column-heading clickable", title: "Done", onClick: this.handleAccept }),
                React.createElement(
                    "div",
                    null,
                    React.createElement("span", { className: "icon-find" }),
                    React.createElement("input", {
                        ref: "query",
                        autoFocus: true,
                        tabIndex: "2",
                        type: "text",
                        placeholder: "Query",
                        value: this.state.query,
                        onChange: this.handleQueryChange,
                        onKeyDown: this.handleKey })
                ),
                React.createElement(
                    "div",
                    null,
                    React.createElement("span", { className: "icon-rename" }),
                    React.createElement("input", {
                        ref: "label",
                        tabIndex: "3",
                        type: "text",
                        placeholder: "Label",
                        value: this.state.label,
                        onChange: this.handleLabelChange,
                        onKeyDown: this.handleKey })
                )
            );
        }

        var label = this.state.label || this.state.query;

        var rowOrColumn = this.props.type === "row" ? "Row" : "Column";
        var control;
        if (this.props.value !== "All") {
            control = React.createElement("span", { key: "removeButton", className: "icon-cancel icon-column-heading", title: "Remove " + rowOrColumn, onClick: this.handleRemove });
        } else {
            control = React.createElement("span", { ref: "addButton", className: "add-column-button icon-add icon-column-heading", title: "Add " + rowOrColumn, onClick: this.handleEdit });
        }

        return React.createElement(
            "td",
            { onClick: this.handleEdit, title: this.state.query, className: "grid-heading" },
            control,
            React.createElement(
                "div",
                null,
                label
            )
        );
    }
});

// GridValueCell renders the cells in the grid, showing their value formatted according to settings.
var GridValueCell = React.createClass({
    displayName: "GridValueCell",

    renderPie: function (percentage, emptyColor, filledColor) {
        if (!emptyColor) emptyColor = "rgba(181, 231, 247, 0.5)";
        if (!filledColor) filledColor = "rgb(61, 195, 246)";

        // For technique, see: https://www.smashingmagazine.com/2015/07/designing-simple-pie-charts-with-css/
        return React.createElement(
            "svg",
            { className: "pie", viewBox: "0 0 32 32", style: { transform: "rotate(-90deg)", background: emptyColor, borderRadius: "50%" } },
            React.createElement("circle", { r: "16", cx: "16", cy: "16", style: { fill: emptyColor, stroke: filledColor, strokeWidth: "32", strokeDasharray: (100 * percentage).toFixed(0) + " 100" } })
        );
    },
    render: function () {
        var nullCell = React.createElement(
            "td",
            { key: "R" + this.props.rowIndex + "C" + this.props.colIndex },
            React.createElement(
                "div",
                { className: "cell" },
                "-"
            )
        );
        if (!this.props.content.values) return nullCell;

        var colors = ["rgba(88, 207, 119, 0.5)", "rgba(96, 117, 194, 0.5)", "rgba(81, 164, 183, 0.5)", "rgba(255, 181, 108, 0.5)", "rgba(255, 125, 108, 0.5)", "rgba(170, 84, 191, 0.5)"];
        var darkColors = ["rgb(13, 186, 58)", "rgb(17, 42, 134)", "rgb(15, 127, 154)", "rgb(248, 132, 17)", "rgb(245, 44, 17)", "rgb(135, 16, 164)"];

        var dataBlockRowIndex = this.props.rowIndex * this.props.columnCount + this.props.colIndex;
        var dataBlockValueColumnIndex = this.props.content.values.columnCount - 1;

        // Bail out if we don't find the expected cell values
        if (this.props.content.values.rows.length <= dataBlockRowIndex || this.props.content.values.rows[dataBlockRowIndex].length <= dataBlockValueColumnIndex) {
            return nullCell;
        }

        // Get the numeric value
        var value = this.props.content.values.rows[dataBlockRowIndex][dataBlockValueColumnIndex] || 0;

        // Determine the denominator to compare against
        var denominatorRowIndex;
        if (this.props.showPortionOf === "row") {
            denominatorRowIndex = (this.props.rowIndex + 1) * this.props.columnCount - 1;
        } else if (this.props.showPortionOf === "col") {
            denominatorRowIndex = (this.props.rowCount - 1) * this.props.columnCount + this.props.colIndex;
        } else {
            denominatorRowIndex = this.props.content.values.rows.length - 1;
        }

        var denominator = this.props.content.values.rows[denominatorRowIndex][dataBlockValueColumnIndex] || 0;
        var percentage = value / denominator;
        if (denominator === 0) percentage = 0;

        // Write the value in the requested form - number with commas or percentage with three significant digits.
        var valueFormatted = (percentage * 100).toFixed(1) + "%";
        if (valueFormatted === "100.0%") valueFormatted = "100%";

        if (this.props.show === "number") {
            valueFormatted = value.toLocaleString();
        } else if (this.props.show === "both") {
            valueFormatted = React.createElement(
                "span",
                null,
                React.createElement(
                    "span",
                    null,
                    valueFormatted
                ),
                React.createElement(
                    "span",
                    { style: { float: "right" } },
                    value.toLocaleString()
                )
            );
        }

        // Compute the Query URL for this cell
        var cellQuery = this.props.content.query.where;

        for (var columnIndex = 0; columnIndex < dataBlockValueColumnIndex; ++columnIndex) {
            var filter = this.props.content.values.rows[dataBlockRowIndex][columnIndex];
            if (filter) {
                if (cellQuery) cellQuery += " AND ";
                cellQuery += filter;
            }
        }

        if (this.props.content.query.aggregator === "MIN" || this.props.content.query.aggregator === "MAX") {
            if (cellQuery) cellQuery += " AND ";
            cellQuery += this.props.content.query.aggregationColumns[0] + " = " + value.toString();
        }

        var cellUrl = buildUrlParameters({ t: this.props.content.query.tableName, q: cellQuery });

        // Choose a color for the relative value rendering
        var colorIndex = 0;
        if (this.props.showPortionOf === "row") {
            colorIndex = this.props.rowIndex % colors.length;
        } else if (this.props.showPortionOf === "col") {
            colorIndex = this.props.colIndex % colors.length;
        }

        // Write the element or style needed to show the relative value
        var portionElement = null;
        if (this.props.showPortionAs === "pie") {
            portionElement = this.renderPie(percentage, colors[colorIndex], darkColors[colorIndex]);
        }

        var portionStyle = null;
        if (this.props.showPortionAs === "bar") {
            // Target Width: 150px + (2 x 8x padding) = 166px.
            portionStyle = { boxShadow: colors[colorIndex] + " " + 166 * percentage + "px 0px 0px inset" };

            // Fills 100% of the cell width, but doesn't animate [in Chrome, 12/2016]. The animation is a higher priority until we can find a way to get both.
            //portionStyle = { background: "linear-gradient(to right, " + colors[colorIndex] + " 0, " + colors[colorIndex] + " " + (100 * percentage).toFixed(0) + "%, rgba(0, 0, 0, 0) " + (100 * percentage).toFixed(0) + "%)" };
        }

        return React.createElement(
            "td",
            { key: "R" + this.props.rowIndex + "C" + this.props.colIndex, className: "c" },
            React.createElement(
                "div",
                { style: portionStyle },
                portionElement,
                React.createElement(
                    "a",
                    { href: "/" + cellUrl },
                    valueFormatted
                )
            )
        );
    }
});

// GridMain wraps the overall grid UI
var GridMain = React.createClass({
    displayName: "GridMain",

    getInitialState: function () {
        return {
            blockingErrorTitle: null,
            blockingErrorStatus: null,
            blockingErrorContent: null,

            query: this.props.params.q || "",
            pivotQueries: [],
            currentTable: this.props.params.t,
            currentTableAllColumns: [],

            aggregationFunction: this.props.params.af || "COUNT",
            aggregateColumn: this.props.params.ac || "",
            rows: getParameterArrayForPrefix(this.props.params, "r"),
            rowLabels: getParameterArrayForPrefix(this.props.params, "rl"),
            cols: getParameterArrayForPrefix(this.props.params, "c"),
            colLabels: getParameterArrayForPrefix(this.props.params, "cl"),

            show: this.props.params.show || "both",
            showPortionOf: this.props.params.of || "total",
            showPortionAs: this.props.params.as || "bar",

            gridData: null
        };
    },
    getClearedUserSelections: function () {
        return {
            /* Query is kept to allow "re-exploring" the query. All other state is cleared. */

            aggregationFunction: "COUNT",
            aggregateColumn: "",
            rows: [],
            rowLabels: [],
            cols: [],
            colLabels: [],

            show: "both",
            showPortionOf: "total",
            showPortionAs: "bar",

            userSelectedTable: "",

            gridData: null
        };
    },
    componentDidMount: function () {
        if (this.props.params.p) {
            this.selectDefaultQuery(this.props.params.p);
        } else {
            this.runSearch();
        }
    },
    handleSelectDefaultQuery: function (e) {
        var name = e.target.value;
        this.selectDefaultQuery(name);
    },
    selectDefaultQuery: function (name) {
        var query = this.props.gridDefaultQueries[name];
        if (query) {
            this.setState(this.getClearedUserSelections());
            this.setState(query, this.runSearch);
        }
    },
    handleChangeAggregation: function (aggregationFunction, aggregateColumn) {
        this.setState({ aggregationFunction: aggregationFunction, aggregateColumn: aggregateColumn, userSelectedTable: this.state.currentTable }, this.runSearch);
    },
    handleChangeShow: function (e) {
        this.setState({ show: e.target.value }, this.setHistory);
    },
    handleChangeShowPortionOf: function (e) {
        this.setState({ showPortionOf: e.target.value }, this.setHistory);
    },
    handleChangeShowPortionAs: function (e) {
        this.setState({ showPortionAs: e.target.value }, this.setHistory);
    },
    handleQueryChange: function (type, index, value, label) {
        var newState = { userSelectedTable: this.state.currentTable, gridData: null, addColumn: false, addRow: false };

        // NOTE: When a column or row is changed, we lock the current table and clear the grid data.
        //  We lock the table because the rows/cols are cleared when the active table is changed and we don't want "top query" changes to lose the cols/rows you've picked
        //  We clear Grid data because after the change but before the query, the data is inconsistent with the cols/rows and we don't want React to draw it like that
        if (type === "column") {
            var columns = this.state.cols.slice();
            var colLabels = this.state.colLabels.slice();

            if (value) {
                columns[index] = value;
                colLabels[index] = label;
            } else {
                columns.splice(index, 1);
                colLabels.splice(index, 1);
            }

            newState.cols = columns;
            newState.colLabels = colLabels;
        } else if (type === "row") {
            var rows = this.state.rows.slice();
            var rowLabels = this.state.rowLabels.slice();

            if (value) {
                rows[index] = value;
                rowLabels[index] = label;
            } else {
                rows.splice(index, 1);
                rowLabels.splice(index, 1);
            }

            newState.rows = rows;
            newState.rowLabels = rowLabels;
        }

        this.setState(newState, this.runSearch);
    },
    onSelectedTableChange: function (name) {
        if (this.state.currentTable === name) {
            // If the selected table is clicked, just mark it actively selected and fix the URL
            this.setState({ userSelectedTable: name }, this.setHistory);
        } else {
            // Otherwise, clear the columns/sort/sortOrder and query the new selected table
            var cleared = this.getClearedUserSelections();
            cleared.userSelectedTable = name;
            cleared.currentTable = name;
            this.setState(cleared, this.runSearch);
        }
    },
    onSearchChange: function (value) {
        this.setState({ query: value }, this.delayedRunSearch);
    },
    delayedRunSearch: function () {
        // Only query every 250 milliseconds while typing
        if (!this.timer) {
            this.timer = window.setTimeout(this.runSearch, 250);
        }
    },
    runSearch: function () {
        this.timer = null;
        this.getAllCounts();
        this.setHistory();
    },
    getAllCounts: function () {
        // On query, ask for the count from every table.
        var params = { q: this.state.query };
        this.addPivotClauses(params);

        // Get the count of matches from each accessible table
        jsonQuery(this.props.url + "/allCount", function (data) {
            var tableToShow = this.state.userSelectedTable;
            if (!tableToShow) tableToShow = data.content[0].tableName;

            this.setState({ allCountData: data, currentTable: tableToShow, error: null }, this.getTableBasics.bind(this, this.getGrid));
        }.bind(this), function (xhr, status, err) {
            this.setState({ allCountData: [], error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });

            if (status === 401) {
                this.setState({ blockingErrorTitle: "Access Denied", blockingErrorStatus: status, blockingErrorContent: this.props.accessDeniedContent });
            } else {
                this.setState({ blockingErrorTitle: "Service Unavailable", blockingErrorStatus: status, blockingErrorContent: this.props.serviceUnavailableContent });
            }
            console.error(xhr.url, status, err.toString());
        }.bind(this), params);
    },
    getTableBasics: function (next) {
        // Once a table is selected, find out the columns
        jsonQuery(this.props.url + "/table/" + this.state.currentTable, function (data) {
            this.setState({
                currentTableAllColumns: data.content.columns,
                error: null
            }, next);
        }.bind(this), function (xhr, status, err) {
            this.setState({ gridData: [], error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            console.error(xhr.url, status, err.toString());
        }.bind(this));
    },
    getGrid: function () {
        // Once the counts query runs and table basics are loaded, get a page of results

        // Get a page of matches for the given query for the desired columns and sort order, with highlighting.
        jsonQuery(this.buildQueryUrl(), function (data) {
            var state = { gridData: data, error: null };

            // If the rows or columns were expanded by the query, use the expanded values so subsequent editing works
            // NOTE: Track the dimension for rows and columns; if only columns were passed, dimensions[0] is the column.
            if (data.content && data.content.query && data.content.query.dimensions) {
                var dimensions = data.content.query.dimensions;
                var dimensionIndex = 0;

                if (this.state.rows && this.state.rows.length === 1) {
                    var lastRow = this.state.rows[0];
                    if (lastRow.indexOf(">") === lastRow.length - 1) {
                        state.rows = dimensions[dimensionIndex] ? dimensions[dimensionIndex].groupByWhere : [];
                        state.rowLabels = [];
                    }

                    dimensionIndex++;
                }

                if (this.state.cols && this.state.cols.length === 1) {
                    var lastColumn = this.state.cols[0];
                    if (lastColumn.indexOf(">") === lastColumn.length - 1) {
                        state.cols = dimensions[dimensionIndex] ? dimensions[dimensionIndex].groupByWhere : [];
                        state.colLabels = [];
                    }

                    dimensionIndex++;
                }
            }

            this.setState(state);
        }.bind(this), function (xhr, status, err) {
            this.setState({ gridData: [], error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            console.error(xhr.url, status, err.toString());
        }.bind(this));
    },
    setHistory: function () {
        var url = this.buildThisUrl(true);
        if (url !== window.location.href) {
            history.pushState("", "", url);
        }
    },
    buildQueryUrl: function () {
        var parameters = {
            action: "aggregate",
            q: this.state.query,
            a: this.state.aggregationFunction,
            col: this.state.aggregateColumn
        };

        // Only pass defined dimensions. Pass Rows first, so draw order is correct.
        var dimension = "d";
        if (this.state.rows && this.state.rows.length > 0) {
            addArrayParameters(parameters, dimension, this.state.rows);
            dimension = String.fromCharCode(dimension.charCodeAt(0) + 1);
        }

        if (this.state.cols && this.state.cols.length > 0) {
            addArrayParameters(parameters, dimension, this.state.cols);
            dimension = String.fromCharCode(dimension.charCodeAt(0) + 1);
        }

        var queryString = buildUrlParameters(parameters);
        return this.props.url + "/table/" + this.state.currentTable + queryString;
    },
    buildThisUrl: function (includeOpen) {
        var relevantParams = {};

        if (this.state.query) relevantParams.q = this.state.query;
        if (this.state.currentTable) relevantParams.t = this.state.currentTable;

        if (this.state.aggregationFunction !== "COUNT") {
            relevantParams.af = this.state.aggregationFunction;
            relevantParams.ac = this.state.aggregateColumn;
        }

        if (this.state.show !== "both") relevantParams.show = this.state.show;
        if (this.state.showPortionOf !== "total") relevantParams.of = this.state.showPortionOf;
        if (this.state.showPortionAs !== "bar") relevantParams.as = this.state.showPortionAs;

        if (this.state.rows && this.state.rows.length > 0) addArrayParameters(relevantParams, "r", this.state.rows);
        if (this.state.cols && this.state.cols.length > 0) addArrayParameters(relevantParams, "c", this.state.cols);
        if (this.state.rowLabels && this.state.rowLabels.length > 0) addArrayParameters(relevantParams, "rl", this.state.rowLabels);
        if (this.state.colLabels && this.state.colLabels.length > 0) addArrayParameters(relevantParams, "cl", this.state.colLabels);

        return window.location.protocol + '//' + window.location.host + window.location.pathname + buildUrlParameters(relevantParams);
    },
    addPivotClauses: function (set) {
        for (var i = 0; i < this.state.pivotQueries.length; ++i) {
            set["q" + (i + 1)] = this.state.pivotQueries[i].q;
            set["t" + (i + 1)] = this.state.pivotQueries[i].t;
        }
    },
    render: function () {
        if (this.state.blockingErrorTitle) return React.createElement(__WEBPACK_IMPORTED_MODULE_0__ErrorPage__["a" /* default */], { title: this.state.blockingErrorTitle, status: this.state.blockingErrorStatus, message: this.state.blockingErrorContent });

        var headings = [];
        var gridRows = [];

        var mainContent = null;

        if (this.state.gridData && this.state.gridData.content) {
            var content = this.state.gridData.content;

            var rows = this.state.rows.slice();
            var rowLabels = this.state.rowLabels;
            rows.push("All");

            var columns = this.state.cols.slice();
            var colLabels = this.state.colLabels;
            columns.push("All");

            headings.push(React.createElement(GridFunctionCell, { key: "AC", aggregationFunction: this.state.aggregationFunction, aggregateColumn: this.state.aggregateColumn, allColumns: this.state.currentTableAllColumns, onChange: this.handleChangeAggregation }));
            for (var columnIndex = 0; columnIndex < columns.length; ++columnIndex) {
                headings.push(React.createElement(GridHeadingCell, { key: "HC" + columns[columnIndex], type: "column", index: columnIndex, value: columns[columnIndex], label: colLabels[columnIndex], onChange: this.handleQueryChange }));
            }

            for (var rowIndex = 0; rowIndex < rows.length; ++rowIndex) {
                var cells = [];
                cells.push(React.createElement(GridHeadingCell, { key: "HR" + rows[rowIndex], type: "row", index: rowIndex, value: rows[rowIndex], label: rowLabels[rowIndex], onChange: this.handleQueryChange }));

                for (var colIndex = 0; colIndex < columns.length; ++colIndex) {
                    cells.push(React.createElement(GridValueCell, {
                        key: "C" + colIndex + "R" + rowIndex,
                        colIndex: colIndex,
                        rowIndex: rowIndex,
                        content: content,
                        columnCount: columns.length, rowCount: rows.length,
                        show: this.state.show, showPortionAs: this.state.showPortionAs, showPortionOf: this.state.showPortionOf }));
                }

                gridRows.push(React.createElement(
                    "tr",
                    { key: "R" + rowIndex },
                    cells
                ));
            }

            var defaultQueries = [];
            defaultQueries.push(React.createElement("option", { key: "SQNone", value: "" }));

            for (var name in this.props.gridDefaultQueries) {
                defaultQueries.push(React.createElement(
                    "option",
                    { key: "SQ" + name, value: name },
                    name
                ));
            }

            mainContent = React.createElement(
                "div",
                { className: "grid" },
                React.createElement(
                    "table",
                    { className: this.state.showPortionOf },
                    React.createElement(
                        "thead",
                        null,
                        React.createElement(
                            "tr",
                            null,
                            headings
                        )
                    ),
                    React.createElement(
                        "tbody",
                        null,
                        gridRows
                    )
                ),
                React.createElement(
                    "div",
                    { className: "options" },
                    "Show\xA0",
                    React.createElement(
                        "select",
                        { value: this.state.show, onChange: this.handleChangeShow },
                        React.createElement(
                            "option",
                            { value: "number" },
                            "Number"
                        ),
                        React.createElement(
                            "option",
                            { value: "percentage" },
                            "Percentage"
                        ),
                        React.createElement(
                            "option",
                            { value: "both" },
                            "Both"
                        )
                    ),
                    "\xA0of\xA0",
                    React.createElement(
                        "select",
                        { value: this.state.showPortionOf, onChange: this.handleChangeShowPortionOf },
                        React.createElement(
                            "option",
                            { value: "total" },
                            "Total"
                        ),
                        React.createElement(
                            "option",
                            { value: "row" },
                            "Row"
                        ),
                        React.createElement(
                            "option",
                            { value: "col" },
                            "Column"
                        )
                    ),
                    "\xA0as\xA0",
                    React.createElement(
                        "select",
                        { value: this.state.showPortionAs, onChange: this.handleChangeShowPortionAs },
                        React.createElement(
                            "option",
                            { value: "bar" },
                            "Bar"
                        ),
                        React.createElement(
                            "option",
                            { value: "pie" },
                            "Pie"
                        )
                    ),
                    "\xA0\xA0\xA0\xA0 Load\xA0",
                    React.createElement(
                        "select",
                        { value: "", onChange: this.handleSelectDefaultQuery },
                        defaultQueries
                    )
                )
            );
        }

        var listingUrl = "/" + buildUrlParameters({ t: this.state.currentTable, q: this.state.query });

        return React.createElement(
            "div",
            { className: "viewport " + configuration.theme, onKeyDown: this.handleKeyDown },
            React.createElement(__WEBPACK_IMPORTED_MODULE_2__SearchHeader__["a" /* default */], { name: configuration.toolName,
                feedbackEmailAddresses: configuration.feedbackEmailAddresses,
                query: this.state.query,
                allColumns: this.state.currentTableAllColumns,
                onSearchChange: this.onSearchChange }),
            React.createElement(
                "div",
                { className: "middle" },
                React.createElement(
                    "div",
                    { className: "mode theme-background-dark" },
                    React.createElement(
                        "a",
                        { href: listingUrl },
                        React.createElement("i", { className: "icon-details", title: "Listing" })
                    ),
                    React.createElement(
                        "a",
                        { className: "selected" },
                        React.createElement("i", { className: "icon-view-all-albums", title: "Grid" })
                    )
                ),
                React.createElement(
                    "div",
                    { className: "center" },
                    React.createElement(__WEBPACK_IMPORTED_MODULE_1__QueryStats__["a" /* default */], { error: this.state.error,
                        allCountData: this.state.allCountData,
                        selectedData: this.state.gridData,
                        currentTable: this.state.currentTable,
                        onSelectedTableChange: this.onSelectedTableChange }),
                    React.createElement(
                        "div",
                        { className: "scrollable" },
                        mainContent
                    )
                )
            )
        );
    }
});

if (document.getElementById("gridContainer")) {
    var params = getQueryStringParameters();
    ReactDOM.render(React.createElement(GridMain, {
        url: configuration.url,
        gridDefaultQueries: configuration.gridDefaultQueries,
        accessDeniedContent: configuration.accessDeniedContent,
        serviceUnavailableContent: configuration.serviceUnavailableContent,
        params: params }), document.getElementById("gridContainer"));

    document.title = configuration.toolName;
}

/***/ }),
/* 8 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
Object.defineProperty(__webpack_exports__, "__esModule", { value: true });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__ErrorPage__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__QueryStats__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__SearchHeader__ = __webpack_require__(5);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__InfiniteScroll__ = __webpack_require__(13);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__SplitPane__ = __webpack_require__(16);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__SyntaxHelp__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__ResultDetails__ = __webpack_require__(14);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__ResultListing__ = __webpack_require__(15);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__DefaultConfiguration__ = __webpack_require__(1);
function _newArrowCheck(innerThis, boundThis) { if (innerThis !== boundThis) { throw new TypeError("Cannot instantiate an arrow function"); } }

__webpack_require__(6);












// NOTE: Depends on configuration from zConfiguration.jsx.

window.configuration = __WEBPACK_IMPORTED_MODULE_8__DefaultConfiguration__["a" /* default */];
var optionalContext = __webpack_require__(0);
if (optionalContext.keys().includes("./configuration/Configuration.jsx")) {
    window.configuration = optionalContext("./configuration/Configuration.jsx").default;
}

// SearchMain wraps the overall search UI
var SearchMain = React.createClass({
    displayName: "SearchMain",

    getInitialState: function () {
        return {
            blockingErrorTitle: null,
            blockingErrorStatus: null,
            blockingErrorContent: null,

            tables: [],
            allCountData: [],
            listingData: [],
            selectedItemData: null,

            allItemCount: 0,

            page: 0,
            hasMoreData: false,

            query: this.props.params.q || "",
            pivotQueries: [],

            currentTable: this.props.params.t,
            currentTableIdColumn: "",
            currentTableAllColumns: [],
            currentListingColumns: [],
            currentSortColumn: "",
            currentSortOrder: "",

            userSelectedTable: this.props.params.t,
            userSelectedColumns: getParameterArrayForPrefix(this.props.params, "c"),
            userSelectedSortColumn: this.props.params.ob,
            userSelectedSortOrder: this.props.params.so,
            userSelectedId: this.props.params.open
        };
    },
    getClearedUserSelections: function () {
        return {
            userSelectedTable: null,
            userSelectedColumns: [],
            userSelectedSortColumn: null,
            userSelectedSortOrder: null,
            userSelectedId: null,
            pivotQueries: []
        };
    },
    componentDidMount: function () {
        // Load table details
        this.getTables(function () {
            if (this.state.query) {
                // If there's a query, run it
                this.runSearch();
            } else if (this.state.userSelectedId) {
                // If there's an item to open, open it
                this.getTableBasics();
            }
        });
    },
    handleKeyDown: function (e) {
        // Backspace: Clear state *if query empty*
        if (e.keyCode === 8) {
            if (!this.state.query) {
                var cleared = this.getClearedUserSelections();
                cleared.allCountData = [];
                cleared.listingData = [];
                cleared.selectedItemData = null;

                this.setState(cleared, this.setHistory);
            }
        }

        // ESC: Close
        if (e.keyCode === 27) {
            this.onClose();
            e.stopPropagation();
        }

        // Up/Down: Open Previous/Next
        if (e.keyCode === 40 || e.keyCode === 38) {
            var indexChange = e.keyCode === 40 ? 1 : -1;
            this.refs.list.selectByRelativeIndex(indexChange);
            e.stopPropagation();
        }
    },
    onSelectionChanged: function (e) {
        this.setState({ userSelectedId: e }, this.getDetails);
    },
    onClose: function () {
        this.setState({ userSelectedId: null }, this.setHistory);
    },
    onResort: function (sortColumn, sortOrder) {
        // If a column heading was clicked, re-query with a new sort order
        this.setState({ userSelectedTable: this.state.currentTable, userSelectedSortColumn: sortColumn, userSelectedSortOrder: sortOrder }, this.runSearch);
    },
    onAddClause: function (name, value) {
        this.setState({ query: this.state.query + " AND [" + name + "]=\"" + value + "\"" }, this.runSearch);
    },
    onSetColumns: function (columns) {
        this.setState({ userSelectedTable: this.state.currentTable, userSelectedColumns: columns }, this.runSearch);
    },
    onSelectedTableChange: function (name) {
        if (this.state.currentTable === name) {
            // If the selected table is clicked, just mark it actively selected and fix the URL
            this.setState({ userSelectedTable: name }, this.setHistory);
        } else {
            // Otherwise, clear the columns/sort/sortOrder and query the new selected table
            var cleared = this.getClearedUserSelections();
            cleared.userSelectedTable = name;
            cleared.currentTable = name;
            this.setState(cleared, this.runSearch);
        }
    },
    onPivot: function (table, baseQuery) {
        var pivots = this.state.pivotQueries;
        pivots.push({ q: this.state.query, t: this.state.currentTable });
        this.setState({ pivotQueries: pivots, query: baseQuery, currentTable: table, userSelectedTable: table }, this.runSearch);
    },
    onSearchChange: function (value) {
        this.setState({ query: value, userSelectedId: null }, this.delayedRunSearch);
    },
    delayedRunSearch: function () {
        // Only query every 250 milliseconds while typing
        if (!this.timer) {
            this.timer = window.setTimeout(this.runSearch, 250);
        }
    },
    runSearch: function () {
        this.timer = null;
        this.getAllCounts();
        this.setHistory();
    },
    getTables: function (callback) {
        // On Page load, find the list of known table names
        jsonQuery(this.props.url, function (data) {
            this.setState({ tables: data.content, error: null }, callback);
        }.bind(this), function (xhr, status, err) {
            if (status === 401) {
                this.setState({ blockingErrorTitle: "Access Denied", blockingErrorStatus: status, blockingErrorContent: this.props.accessDeniedContent });
            } else {
                this.setState({ blockingErrorTitle: "Service Unavailable", blockingErrorStatus: status, blockingErrorContent: this.props.serviceUnavailableContent });
            }
            console.error(xhr.url, status, err.toString());
        }.bind(this));
    },
    getAllCounts: function () {
        // On query, ask for the count from every table.

        // If there's no query, clear results and do nothing else
        if (!this.state.query) {
            var cleared = {};
            cleared.allCountData = [];
            cleared.listingData = [];
            cleared.selectedItemData = null;

            this.setState(cleared);
            return;
        }

        var params = { q: this.state.query };
        this.addPivotClauses(params);

        // Get the count of matches from each accessible table
        this.jsonQueryWithError(this.props.url + "/allCount", function (data) {
            var tableToShow = this.state.userSelectedTable;
            if (!tableToShow) tableToShow = data.content[0].tableName;

            this.setState({ allCountData: data, currentTable: tableToShow, error: null }, this.getTableBasics);
        }.bind(this), params);
    },
    getTableBasics: function () {
        var _this = this;

        // Once a table is selected, find out the columns and primary key column for the table
        this.jsonQueryWithError(this.props.url + "/table/" + this.state.currentTable, function (data) {
            _newArrowCheck(this, _this);

            var idColumn = data.content.columns.find(function (col) {
                _newArrowCheck(this, _this);

                return col.isPrimaryKey;
            }.bind(this)).name || "";

            // Choose columns, sort column, sort order
            var defaultsForTable = this.props.listingDefaults && this.props.listingDefaults[this.state.currentTable] || {};

            // Set the ID column, all columns, and listing columns
            this.setState({
                currentTableIdColumn: idColumn,
                currentTableAllColumns: data.content.columns,
                currentListingColumns: firstNonEmptyArray(this.state.userSelectedColumns, defaultsForTable.columns, [idColumn]),
                currentSortColumn: this.state.userSelectedSortColumn || defaultsForTable.sortColumn || idColumn,
                currentSortOrder: this.state.userSelectedSortOrder || defaultsForTable.sortOrder || "asc",
                error: null
            }, function () {
                _newArrowCheck(this, _this);

                if (this.state.query) this.getResultsPage();
                if (this.state.userSelectedId) this.getDetails();
            }.bind(this));
        }.bind(this));
    },
    getResultsPage: function (i) {
        // Once the counts query runs and table basics are loaded, get a page of results

        // If there's no table, id column, or query, don't do anything yet
        if (!this.state.query || !this.state.currentTable || !this.state.currentTableIdColumn) return;

        // Get enough items to fill the requested page number (rather than trying to append one page)
        if (!i) i = 0;
        var pageSize = 50 * (i + 1);

        // Get a page of matches for the given query for the desired columns and sort order, with highlighting.
        this.jsonQueryWithError(this.buildQueryUrl() + "&h=%CF%80&t=" + pageSize, function (data) {
            this.setState({ listingData: data, hasMoreData: data.content.total > pageSize, page: i, error: null });
        }.bind(this));
    },
    getDetails: function () {
        // When an item is selected, get details for it

        // If there's no table or id column don't do anything yet
        if (!this.state.currentTable || !this.state.currentTableIdColumn) return;

        var detailsQuery = this.state.currentTableIdColumn + '="' + this.state.userSelectedId + '"';
        if (this.state.query) detailsQuery = detailsQuery + " AND " + this.state.query;

        var params = {
            q: detailsQuery,
            c1: "*",
            action: "select",
            h: "π",
            s: 0,
            t: 1
        };
        this.addPivotClauses(params);

        // Select all columns for the selected item, with highlighting
        this.jsonQueryWithError(this.props.url + "/table/" + this.state.currentTable, function (data) {
            if (data.content.values) {
                this.setState({ selectedItemData: arribaRowToObject(data.content.values, 0) });
            } else {
                if (!this.state.query) {
                    this.setState({ selectedItemData: null, error: "Item '" + this.state.userSelectedId + "' not found." });
                } else {
                    this.setState({ selectedItemData: null, userSelectedId: null }, this.setHistory);
                }
            }
        }.bind(this), params);

        this.setHistory();
    },
    jsonQueryWithError: function (url, onSuccess, parameters) {
        jsonQuery(url, onSuccess, function (xhr, status, err) {
            this.setState({ allCountData: [], listingData: [], selectedItemData: null, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            console.error(xhr.url, status, err.toString());
        }.bind(this), parameters);
    },
    setHistory: function () {
        var url = this.buildThisUrl(true);
        if (url !== window.location.href) {
            history.pushState("", "", url);
        }
    },
    buildQueryUrl: function () {
        var parameters = {
            action: "select",
            q: this.state.query,
            ob: this.state.currentSortColumn,
            so: this.state.currentSortOrder,
            s: 0
        };

        addArrayParameters(parameters, "c", this.state.currentListingColumns);
        this.addPivotClauses(parameters);

        var queryString = buildUrlParameters(parameters);
        return this.props.url + "/table/" + this.state.currentTable + queryString;
    },
    buildThisUrl: function (includeOpen) {
        var relevantParams = {};
        this.addPivotClauses(relevantParams);

        if (this.state.userSelectedTable) relevantParams.t = this.state.userSelectedTable;
        if (this.state.query) relevantParams.q = this.state.query;
        if (this.state.userSelectedSortColumn) relevantParams.ob = this.state.userSelectedSortColumn;
        if (this.state.userSelectedSortOrder === "desc") relevantParams.so = this.state.userSelectedSortOrder;

        for (var i = 0; i < this.state.userSelectedColumns.length; ++i) {
            relevantParams["c" + (i + 1).toString()] = this.state.userSelectedColumns[i];
        }

        if (includeOpen && this.state.userSelectedId) {
            relevantParams.open = this.state.userSelectedId;
        }

        return window.location.protocol + '//' + window.location.host + window.location.pathname + buildUrlParameters(relevantParams);
    },
    addPivotClauses: function (set) {
        for (var i = 0; i < this.state.pivotQueries.length; ++i) {
            set["q" + (i + 1)] = this.state.pivotQueries[i].q;
            set["t" + (i + 1)] = this.state.pivotQueries[i].t;
        }
    },
    render: function () {
        if (this.state.blockingErrorTitle) return React.createElement(__WEBPACK_IMPORTED_MODULE_0__ErrorPage__["a" /* default */], { title: this.state.blockingErrorTitle, status: this.state.blockingErrorStatus, message: this.state.blockingErrorContent });

        var customDetailsView = this.props.customDetailsProviders && this.props.customDetailsProviders[this.state.currentTable] || __WEBPACK_IMPORTED_MODULE_6__ResultDetails__["a" /* default */];

        var mainContent = this.state.query ? React.createElement(
            __WEBPACK_IMPORTED_MODULE_4__SplitPane__["a" /* default */],
            { split: "horizontal", minSize: "300", isFirstVisible: this.state.listingData.content, isSecondVisible: this.state.userSelectedId },
            React.createElement(
                __WEBPACK_IMPORTED_MODULE_3__InfiniteScroll__["a" /* default */],
                { page: this.state.page, hasMoreData: this.state.hasMoreData, loadMore: this.getResultsPage },
                React.createElement(__WEBPACK_IMPORTED_MODULE_7__ResultListing__["a" /* default */], { ref: "list",
                    data: this.state.listingData,
                    idColumn: this.state.currentTableIdColumn,
                    allColumns: this.state.currentTableAllColumns,
                    sortColumn: this.state.currentSortColumn,
                    sortOrder: this.state.currentSortOrder,
                    selectedId: this.state.userSelectedId,
                    onResort: this.onResort,
                    onSelectionChanged: this.onSelectionChanged,
                    onSetColumns: this.onSetColumns,
                    onPivot: this.onPivot })
            ),
            React.createElement(
                "div",
                { className: "scrollable" },
                React.createElement(customDetailsView, {
                    itemId: this.state.userSelectedId,
                    table: this.state.currentTable,
                    query: this.state.query,
                    data: this.state.selectedItemData,
                    onClose: this.onClose,
                    onAddClause: this.onAddClause
                })
            )
        ) : React.createElement(__WEBPACK_IMPORTED_MODULE_5__SyntaxHelp__["a" /* default */], { showHelp: this.props.params.help, splashContent: configuration.splashContent });

        var queryUrl = this.buildQueryUrl();
        var baseUrl = this.buildThisUrl(false);
        var rssUrl = queryUrl + "&fmt=rss&t=100&iURL=" + encodeURIComponent(baseUrl + "&open=");
        var csvUrl = queryUrl + "&fmt=csv&t=50000";

        var gridUrl = "/Grid.html" + buildUrlParameters({ t: this.state.currentTable, q: this.state.query });
        if (!this.state.query) gridUrl = "/Grid.html?p=Scorecard";

        return React.createElement(
            "div",
            { className: "viewport " + configuration.theme, onKeyDown: this.handleKeyDown },
            React.createElement(__WEBPACK_IMPORTED_MODULE_2__SearchHeader__["a" /* default */], { name: configuration.toolName,
                feedbackEmailAddresses: configuration.feedbackEmailAddresses,
                query: this.state.query,
                tables: this.state.tables,
                allColumns: this.state.currentTableAllColumns,
                onSearchChange: this.onSearchChange }),
            React.createElement(
                "div",
                { className: "middle" },
                React.createElement(
                    "nav",
                    { className: "mode theme-background-dark" },
                    React.createElement(
                        "a",
                        { className: "selected" },
                        React.createElement("i", { className: "icon-details" }),
                        React.createElement(
                            "span",
                            null,
                            "Listing"
                        )
                    ),
                    React.createElement(
                        "a",
                        { href: gridUrl },
                        React.createElement("i", { className: "icon-view-all-albums" }),
                        React.createElement(
                            "span",
                            null,
                            "Grid"
                        )
                    )
                ),
                React.createElement(
                    "div",
                    { className: "center" },
                    React.createElement(__WEBPACK_IMPORTED_MODULE_1__QueryStats__["a" /* default */], { error: this.state.error,
                        allCountData: this.state.allCountData,
                        selectedData: this.state.listingData,
                        rssUrl: rssUrl,
                        csvUrl: csvUrl,
                        currentTable: this.state.currentTable,
                        onSelectedTableChange: this.onSelectedTableChange }),
                    mainContent
                )
            )
        );
    }
});

if (document.getElementById("searchContainer")) {
    var params = getQueryStringParameters();
    ReactDOM.render(React.createElement(SearchMain, {
        url: configuration.url,
        listingDefaults: configuration.listingDefaults,
        customDetailsProviders: configuration.customDetailsProviders,
        accessDeniedContent: configuration.accessDeniedContent,
        serviceUnavailableContent: configuration.serviceUnavailableContent,
        params: params }), document.getElementById("searchContainer"));

    document.title = configuration.toolName;
}

/***/ }),
/* 9 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__jsx_DetailsTable__ = __webpack_require__(2);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__ = __webpack_require__(11);



var PatchComplianceSummary = React.createClass({
    displayName: "PatchComplianceSummary",

    render: function () {
        if (this.props.data["IsPatchCompliant"]) return React.createElement(
            "div",
            null,
            React.createElement("span", { className: "good-icon icon", title: "IsPatchCompliant:1" }),
            "Patch Compliant"
        );

        return React.createElement(
            "div",
            null,
            React.createElement(
                "div",
                null,
                React.createElement("span", { className: "error-icon icon" }),
                React.createElement(
                    "b",
                    null,
                    "Not Patch Compliant"
                ),
                React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["a" /* DetailReportLink */], { url: "https://insights/Report?id=07e846a5-a100-498b-8fe0-68a907b695cc&isPublished=true#assetName={ID}&_tab=0", id: this.props.data["Name"], title: "Patch Compliance Summary" })
            ),
            React.createElement(
                "table",
                { className: "smallTable" },
                React.createElement(
                    "thead",
                    null,
                    React.createElement(
                        "tr",
                        null,
                        React.createElement(
                            "td",
                            null,
                            "\xA0"
                        ),
                        React.createElement(
                            "td",
                            null,
                            "Sev 5"
                        ),
                        React.createElement(
                            "td",
                            null,
                            "Sev 4"
                        ),
                        React.createElement(
                            "td",
                            null,
                            "Sev 3"
                        )
                    )
                ),
                React.createElement(
                    "tbody",
                    null,
                    React.createElement(
                        "tr",
                        null,
                        React.createElement(
                            "td",
                            null,
                            "Out-of-Cycle"
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["b" /* BadUnlessZero */], { value: this.props.data["Sev5_OutCycleCount"], columnName: "Sev5_OutCycleCount" })
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["b" /* BadUnlessZero */], { value: this.props.data["Sev4_OutCycleCount"], columnName: "Sev4_OutCycleCount" })
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["b" /* BadUnlessZero */], { value: this.props.data["Sev3_OutCycleCount"], columnName: "Sev3_OutCycleCount" })
                        )
                    ),
                    React.createElement(
                        "tr",
                        null,
                        React.createElement(
                            "td",
                            null,
                            "Third Party"
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["b" /* BadUnlessZero */], { value: this.props.data["Sev5_ThirdPartyCount"], columnName: "Sev5_ThirdPartyCount" })
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["b" /* BadUnlessZero */], { value: this.props.data["Sev4_ThirdPartyCount"], columnName: "Sev4_ThirdPartyCount" })
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["b" /* BadUnlessZero */], { value: this.props.data["Sev3_ThirdPartyCount"], columnName: "Sev3_ThirdPartyCount" })
                        )
                    )
                )
            )
        );
    }
});

var AVSummary = React.createClass({
    displayName: "AVSummary",

    render: function () {
        // If no AntiVirus, just report that
        if (!this.props.data["HasAV"]) return React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["c" /* RiskMarker */], { condition: !this.props.data["HasAV"], columnName: "HasAV", label: "AV Missing" });

        // Otherwise, consider details
        return React.createElement(
            "div",
            null,
            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["AVName"], columnName: "AVName", label: "AV", id: this.props.data["Name"], url: "https://insights/Report?id=07e846a5-a100-498b-8fe0-68a907b695cc&isPublished=true#assetName={ID}&_tab=2", "url-title": "AV Details" }),
            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["e" /* RelativeDateTime */], { value: this.props.data["LatestScanDate"], columnName: "LatestScanDate", label: "Last Scan" }),
            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["c" /* RiskMarker */], { condition: !this.props.data["IsScanned"], columnName: "IsScanned", label: "Unscanned" }),
            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["c" /* RiskMarker */], { condition: !this.props.data["HasAuthenticatedScan"], columnName: "HasAuthenticatedScan", label: "Unauthenticated Scan" })
        );
    }
});

var MonitoringSummary = React.createClass({
    displayName: "MonitoringSummary",

    render: function () {
        // If not monitored, just say that
        if (!this.props.data["IsMonitored"]) return React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["c" /* RiskMarker */], { condition: !this.props.data["IsMonitored"], columnName: "IsMonitored", label: "Unmonitored" });

        // Otherwise, consider details
        return React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["e" /* RelativeDateTime */], { value: this.props.data["LatestMonitoredDate"], columnName: "LatestMonitoredDate", label: "Last Monitored" });
    }
});

// AssetDetails shows details for an asset specifically when clicked.
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "AssetDetails",

    getInitialState: function () {
        return { allTableShown: true };
    },
    handleClose: function () {
        this.props.onClose();
    },
    toggle: function () {
        this.setState({ allTableShown: !this.state.allTableShown });
    },
    render: function () {
        if (!this.props.itemId) return null;
        if (!this.props.data) return null;

        var fullTeam = [];

        fullTeam.push(React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["S1"], columnName: "S1", inline: "true" }));
        if (this.props.data["S2"]) {
            fullTeam.push(" | ");
            fullTeam.push(React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["S2"], columnName: "S2", inline: "true" }));
        }

        if (this.props.data["S3"]) {
            fullTeam.push(" | ");
            fullTeam.push(React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["S3"], columnName: "S3", inline: "true" }));
        }

        if (this.props.data["S4"]) {
            fullTeam.push(" | ");
            fullTeam.push(React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["S4"], columnName: "S4", inline: "true" }));
        }

        var allTableToggle = React.createElement("span", { className: "icon-chevron-down-bold clickable", onClick: this.toggle });
        var detailsTable = null;

        if (this.state.allTableShown) {
            allTableToggle = React.createElement("span", { className: "icon-chevron-up-bold clickable", onClick: this.toggle });
            detailsTable = React.createElement(__WEBPACK_IMPORTED_MODULE_0__jsx_DetailsTable__["a" /* default */], {
                data: this.props.data,
                table: this.props.table,
                query: this.props.query,
                onAddClause: this.props.onAddClause
            });
        }

        var baselineIcon = React.createElement(
            "div",
            null,
            React.createElement("span", { className: "good-icon icon", title: "IsBaseline:1" }),
            "Baseline"
        );
        if (!this.props.data["IsBaseline"]) baselineIcon = React.createElement(
            "div",
            null,
            React.createElement("span", { className: "warn-icon icon", title: "IsBaseline:0" }),
            "Not Baseline"
        );

        return React.createElement(
            "div",
            { className: "details" },
            React.createElement(
                "div",
                { className: "commands" },
                React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["a" /* DetailReportLink */], { url: "https://insights/Report?id=5337be64-6142-4dec-8191-711c3043d2bb&isPublished=true#assetName={ID}&_tab=0", id: this.props.data["Name"], title: "Insights Asset Detail" }),
                React.createElement("span", { className: "icon icon-cancel", onClick: this.handleClose })
            ),
            React.createElement(
                "div",
                { className: "detailHeader" },
                React.createElement(
                    "div",
                    { className: "detailTitle font-light" },
                    this.props.itemId
                ),
                React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: "." + this.props.data["Domain"], columnName: "Domain", inline: "true" }),
                React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["OU"], columnName: "OU" }),
                React.createElement(
                    "div",
                    { className: "overviewBoxes" },
                    React.createElement(
                        "div",
                        { className: "box" },
                        "Asset",
                        React.createElement(
                            "div",
                            { className: "indent" },
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["IP"], columnName: "IP", label: "IP" }),
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["Type"], columnName: "Type" }),
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["OSVersion"], columnName: "OSVersion", label: "OS" }),
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["Environment"], columnName: "Environment", label: "Env" }),
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["Asset_Source"], columnName: "Asset_Source", label: "Source" }),
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["XpertRole"], columnName: "XPertRole", label: "XPert Role" })
                        ),
                        React.createElement("br", null),
                        "Ownership",
                        React.createElement(
                            "div",
                            { className: "indent" },
                            React.createElement(
                                "div",
                                null,
                                React.createElement(
                                    "span",
                                    null,
                                    "Org:"
                                ),
                                " ",
                                fullTeam
                            ),
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["f" /* MailLink */], { to: this.props.data["ServiceOwner"], columnName: "ServiceOwner", subject: this.props.itemId, label: "FTE" }),
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["f" /* MailLink */], { to: this.props.data["ServiceOwnerOperations"], columnName: "ServiceOwnerOperations", subject: this.props.itemId, label: "Ops" })
                        )
                    ),
                    React.createElement(
                        "div",
                        { className: "box" },
                        "Status",
                        React.createElement(
                            "div",
                            { className: "indent" },
                            baselineIcon,
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["d" /* DetailsField */], { value: this.props.data["SecurityRisk"], columnName: "SecurityRisk", label: "Security Risk" }),
                            React.createElement(AVSummary, { data: this.props.data }),
                            React.createElement(MonitoringSummary, { data: this.props.data }),
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["c" /* RiskMarker */], { condition: this.props.data["IsEdge"], columnName: "IsEdge", label: "Edge" }),
                            React.createElement(__WEBPACK_IMPORTED_MODULE_1__ConfluxComponents__["c" /* RiskMarker */], { condition: this.props.data["IsDualHomed"], columnName: "IsDualHomed", label: "Dual-Homed" }),
                            React.createElement(PatchComplianceSummary, { data: this.props.data })
                        )
                    )
                ),
                React.createElement(
                    "div",
                    { className: "box" },
                    allTableToggle,
                    detailsTable
                )
            )
        );
    }
});

/***/ }),
/* 10 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
Object.defineProperty(__webpack_exports__, "__esModule", { value: true });
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__AssetDetails__ = __webpack_require__(9);


// zConfiguration.jsx contains configuration settings to configure this instance.
//  Until the site is fixed to use a proper require() model, this must be after the components [ResultListing] and before the modes [zGrid, zSearch] 

/* harmony default export */ __webpack_exports__["default"] = {
    // Arriba Service URL to query
    //url: window.location.protocol + '//' + window.location.hostname + ":42784",
    url: "https://confluxsearch:42785",

    // Name of tool to show [top right and elsewhere]
    toolName: "Conflux",

    // Theme to use [see theme.css for options and to define others]
    theme: "theme-green",

    // E-Mail addresses to whom feedback should go
    feedbackEmailAddresses: "v-scolo@microsoft.com;mikefan@microsoft.com",

    // Listing: Default Columns and Sort Order to show per table
    listingDefaults: {
        "Asset": { columns: ["Name", "IP"], sortColumn: "Name", sortOrder: "asc" },
        "Employee": { columns: ["Alias", "Name"], sortColumn: "Alias", sortOrder: "asc" },
        "AzureSubscription": { columns: ["SubscriptionId", "Name"], sortColumn: "Name", sortOrder: "asc" }
    },

    // Listing: Default details
    customDetailsProviders: {
        "Asset": __WEBPACK_IMPORTED_MODULE_0__AssetDetails__["a" /* default */]
    },

    // Grid: Pre-built Grid queries
    gridDefaultQueries: {
        "Clear": {},
        "Scorecard": {
            query: "IsBaseline=1",
            currentTable: "Asset",
            rows: ["HasAuthenticatedScan=1", "IsPatchCompliant=1", "LatestScanDate > today-7", "IsMFA=1", "IsJIT=1", "IsDualHomed=0", "IsMonitored=1"],
            rowLabels: ["1.2 Vulnerability Scanning Coverage", "1.3 Scanned Servers with no Critical Vulns", "1.5 Servers with AV Installed & Scanned < 24 hrs", "2.2 Machines with MFA Implemented", "2.3 Environment Requiring JIT", "2.5 Non-Dual-Homed Assets", "3.1 Monitoring Coverage"],
            cols: ["S1=\"Universal Store\"", "S1=Xbox", "S1=Core", "S1=MDG"],
            colLabels: ["Uni Store All", "XSO All", "Core All", "Devices All"],
            showPortionOf: "col"
        },
        "Fixcard": {
            query: "IsBaseline=1",
            currentTable: "Asset",
            rows: ["!HasAuthenticatedScan=1", "!IsPatchCompliant=1", "!LatestScanDate > today-7", "!IsMFA=1", "!IsJIT=1", "!IsDualHomed=0", "!IsMonitored=1"],
            rowLabels: ["NOT 1.2 Vulnerability Scanning Coverage", "NOT 1.3 Scanned Servers with no Critical Vulns", "NOT 1.5 Servers with AV Installed & Scanned < 24 hrs", "NOT 2.2 Machines with MFA Implemented", "NOT 2.3 Environment Requiring JIT", "NOT 2.5 Non-Dual-Homed Assets", "NOT 3.1 Monitoring Coverage"],
            cols: ["S1=\"Universal Store\"", "S1=Xbox", "S1=Core", "S1=MDG"],
            colLabels: ["Uni Store All", "XSO All", "Core All", "Devices All"],
            showPortionOf: "col"
        }
    },

    // Content to show before a query is typed
    splashContent: React.createElement(
        "div",
        null,
        "ConfluxSearch is an instant-searchable index of Asset, Employee, and Administrator data.",
        React.createElement(
            "div",
            { className: "smaller" },
            "This information is highly sensitive. You may see a limited view of the information depending on permissions."
        )
    ),

    // Content to show if query fails due to access denied (401)
    accessDeniedContent: React.createElement(
        "span",
        null,
        "Access to ConfluxSearch denied. To request access to the overall site, please join ",
        React.createElement(
            "a",
            { target: "_blank", href: "https://idweb/IdentityManagement/aspx/common/GlobalSearchResult.aspx?searchtype=e0c132db-08d8-4258-8bce-561687a8a51e&content=Conflux Report Reader" },
            "Conflux Report Reader"
        ),
        ". Some content is restricted to other groups."
    ),

    // Content to show if query fails because service is unavailable (404, 500)
    serviceUnavailableContent: React.createElement(
        "span",
        null,
        "ConfluxSearch is currently unavailable. Please ",
        React.createElement(
            "a",
            { href: "mailto:v-scolo@microsoft.com;mikefan@microsoft.com&subject=ConfluxSearch%20Unavailable" },
            "report"
        ),
        " this issue."
    )
};

/***/ }),
/* 11 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "a", function() { return DetailReportLink; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "f", function() { return MailLink; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "d", function() { return DetailsField; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "c", function() { return RiskMarker; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "b", function() { return BadUnlessZero; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "e", function() { return RelativeDateTime; });
// Create a styled link to another report
var DetailReportLink = React.createClass({
    displayName: "DetailReportLink",

    urlReplace: function (urlFormat, id) {
        var idForUrl = encodeURIComponent(id);
        return urlFormat.replace(/{ID}/, idForUrl);
    },
    render: function () {
        return React.createElement(
            "a",
            { className: "detailLink", target: "_blank", href: this.urlReplace(this.props.url, this.props.id), title: this.props.title },
            React.createElement("span", { className: "icon-open-new-windows" })
        );
    }
});

// Creates a mailto link for the given 'to' address and optional subject, safely escaping and stripping highlight
var MailLink = React.createClass({
    displayName: "MailLink",

    render: function () {
        var mailToUrl = "mailto:" + encodeURIComponent(stripHighlight(this.props.to));
        var parameters = {
            subject: stripHighlight(this.props.subject),
            body: window.location.href
        };
        mailToUrl += buildUrlParameters(parameters);

        var content = React.createElement("a", { href: mailToUrl, title: this.props.columnName + ':' + stripHighlight(this.props.to), dangerouslySetInnerHTML: highlight(this.props.to) });
        if (this.props.bold !== "false") content = React.createElement(
            "b",
            null,
            content
        );

        var label = null;
        if (this.props.label) label = React.createElement(
            "span",
            null,
            this.props.label + ": "
        );

        return React.createElement(
            "div",
            null,
            label,
            " ",
            content
        );
    }
});

// Writes out a field with label and highlighting, but only if the value is set
var DetailsField = React.createClass({
    displayName: "DetailsField",

    render: function () {
        if (!this.props.value || this.props.value === "N/A" || this.props.value === "Unknown") return null;

        var link = null;
        if (this.props.url) link = React.createElement(DetailReportLink, { url: this.props.url, id: this.props.id, title: this.props["url-title"] });

        var content = React.createElement("span", { dangerouslySetInnerHTML: highlight(this.props.value), title: this.props.columnName + ':' + stripHighlight(this.props.value) });
        if (this.props.bold) content = React.createElement(
            "b",
            null,
            content
        );

        var label = null;
        if (this.props.label) label = React.createElement(
            "span",
            null,
            this.props.label + ": "
        );

        if (this.props.inline === "true") {
            return React.createElement(
                "span",
                null,
                label,
                " ",
                content,
                link
            );
        } else {
            return React.createElement(
                "div",
                null,
                label,
                " ",
                content,
                link
            );
        }
    }
});

// RiskMarker emits an inline value with a red exclamation point if the given condition is
var RiskMarker = React.createClass({
    displayName: "RiskMarker",

    render: function () {
        if (!this.props.condition) return null;
        return React.createElement(
            "div",
            null,
            React.createElement("span", { className: "error-icon icon", title: this.props.columnName }),
            React.createElement(
                "b",
                null,
                this.props.label
            )
        );
    }
});

// RiskNumber emits a number which is red if not zero
var BadUnlessZero = React.createClass({
    displayName: "BadUnlessZero",

    render: function () {
        if (!this.props.value || this.props.value.toString() === "0") return React.createElement(
            "span",
            { title: this.props.columnName + ':0' },
            "0"
        );
        return React.createElement(
            "b",
            { title: this.props.columnName + ':' + this.props.value },
            this.props.value
        );
    }
});

// RelativeDateTime writes a DateTime in terms of now (ex: 3 days ago), and highlights it yellow if over 2.5 days and red if over 6.5 days.
var RelativeDateTime = React.createClass({
    displayName: "RelativeDateTime",

    render: function () {
        var label = null;
        if (this.props.label) label = React.createElement(
            "span",
            null,
            this.props.label + " "
        );

        if (!this.props.value || this.props.value === "0001-01-01T00:00:00Z") {
            return React.createElement(
                "div",
                null,
                React.createElement("span", { className: "error-icon icon" }),
                label,
                " Unknown"
            );
        }

        var dateTime = moment(this.props.value);
        var howLongSince = moment.duration(moment().diff(dateTime));

        var prefix = null;
        if (howLongSince.asDays() > 6.5) {
            prefix = React.createElement("span", { className: "error-icon icon" });
        } else if (howLongSince.asDays() > 2.5) {
            prefix = React.createElement("span", { className: "warn-icon icon" });
        }

        var content = React.createElement(
            "span",
            null,
            dateTime.fromNow()
        );
        if (prefix) content = React.createElement(
            "b",
            null,
            dateTime.fromNow()
        );

        return React.createElement(
            "div",
            { title: this.props.columnName + ':' + this.props.value },
            prefix,
            label,
            content
        );
    }
});



/***/ }),
/* 12 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
// AddColumnList is the list of unselected columns which shows up in the listing when the '+' is clicked.
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "AddColumnList",

    getInitialState: function () {
        return { filter: null, filteredColumns: this.filterColumns(null), selectedIndex: 0 };
    },
    filterColumns: function (filter) {
        if (!filter) filter = "";
        filter = filter.toLowerCase();

        var filteredColumns = [];
        for (var i = 0; i < this.props.allColumns.length; ++i) {
            var column = this.props.allColumns[i];

            // Filter columns already added and those not starting with the filter
            if (this.props.currentColumns.indexOf(column.name) !== -1) continue;
            if (filter && column.name.toLowerCase().indexOf(filter) !== 0) continue;

            filteredColumns.push(column.name);
        }

        return filteredColumns;
    },
    handleKeyDown: function (e) {
        if (e.keyCode === 27) {
            // ESC - Close AddColumnList
            this.setState(this.getInitialState());
            this.props.onAddColumn(null);
            e.stopPropagation();
        } else if (e.keyCode === 13 || e.keyCode === 9) {
            // ENTER/TAB - commit highlighted match
            var currentColumn = this.state.filteredColumns[this.state.selectedIndex];
            if (currentColumn) this.props.onAddColumn(currentColumn);
            e.stopPropagation();
        } else if (e.keyCode === 38) {
            // Up Arrow - select previous
            this.setState({ selectedIndex: this.state.selectedIndex <= 0 ? 0 : this.state.selectedIndex - 1 });
            e.stopPropagation();
        } else if (e.keyCode === 40) {
            // Down Arrow - select next
            this.setState({ selectedIndex: this.state.selectedIndex >= this.state.filteredColumns.length ? this.state.filteredColumns.length - 1 : this.state.selectedIndex + 1 });
            e.stopPropagation();
        }
    },
    handleAddColumn: function (e) {
        this.props.onAddColumn(e.target.getAttribute("data-name"));
        e.stopPropagation();
    },
    handleFilterChanged: function (e) {
        var newFilter = e.target.value;
        var newFilteredColumns = this.filterColumns(newFilter);
        this.setState({ filter: newFilter, filteredColumns: newFilteredColumns, selectedIndex: 0 });
    },
    render: function () {
        // Write an add column list (shown only once the '+' is clicked)
        if (!this.props.showing) return null;

        var addFunction = this.handleAddColumn;
        var addColumns = [];
        for (var i = 0; i < this.state.filteredColumns.length; ++i) {
            var name = this.state.filteredColumns[i];
            var className = i === this.state.selectedIndex ? "add-list-selected" : "";
            addColumns.push(React.createElement(
                "div",
                { key: "add_" + name, "data-name": name, onClick: addFunction, className: className },
                name
            ));
        }

        return React.createElement(
            "div",
            { style: { position: "absolute" } },
            React.createElement(
                "div",
                { className: "add-list", onKeyDown: this.handleKeyDown },
                React.createElement("input", { type: "text", autoFocus: true, placeholder: "Filter", value: this.state.filter, onChange: this.handleFilterChanged, onKeyDown: this.handleKeyDown }),
                addColumns
            )
        );
    }
});

/***/ }),
/* 13 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
// InfiniteScroll provides infinite scrolling support.
/* Usage:
    <InfiniteScroll page={this.state.page} hasMoreData={this.state.hasMoreData} loadMore={this.getResultsPage}>
        {content}
    </InfiniteScroll>

    Page identifies the integer page of items loaded so far, starting at zero.
    When scrolling is required, loadMore will be called for page+1.
    Page updating on the InfiniteScroll class tells it that it can call loadMore again (the previous loadMore is complete).
    hasMoreData is set to false when there are no more items to load and loadMore should no longer be called.
*/
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "InfiniteScroll",

    handleScroll: function (e) {
        var element = e.target;
        if (e.target === this.refs.scrollContainer) {
            var pixelsFromBottom = element.scrollHeight - element.clientHeight - element.scrollTop;

            if (pixelsFromBottom < 200) {
                // Remove this listener to avoid multiple loadMore calls
                // "page" changing in the caller will cause this to re-render and be re-attached
                e.target.removeEventListener("onScroll", this.handleScroll);

                // Load the next page of items
                if (this.props.hasMoreData) {
                    this.props.loadMore(this.props.page + 1);
                }
            }
        }
    },
    render: function () {
        return React.createElement(
            "div",
            { ref: "scrollContainer", className: "scrollable", onScroll: this.handleScroll },
            this.props.children
        );
    }
});

/***/ }),
/* 14 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__DetailsTable__ = __webpack_require__(2);


// ResultDetails shows (default formatted) details for an item when it was clicked.
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "ResultDetails",

    handleClose: function () {
        this.props.onClose();
    },
    render: function () {
        if (!this.props.itemId) return null;
        if (!this.props.data) return null;

        return React.createElement(
            "div",
            { className: "details" },
            React.createElement(
                "div",
                { className: "commands" },
                React.createElement("span", { className: "icon icon-cancel", onClick: this.handleClose })
            ),
            React.createElement(
                "div",
                null,
                React.createElement(
                    "div",
                    { className: "detailHeader" },
                    React.createElement(
                        "div",
                        { className: "detailTitle" },
                        this.props.itemId
                    )
                ),
                React.createElement(
                    "div",
                    { className: "detailBody" },
                    React.createElement(
                        "div",
                        { className: "box" },
                        React.createElement(__WEBPACK_IMPORTED_MODULE_0__DetailsTable__["a" /* default */], {
                            data: this.props.data,
                            table: this.props.table,
                            query: this.props.query,
                            onAddClause: this.props.onAddClause })
                    )
                )
            )
        );
    }
});

/***/ }),
/* 15 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__AddColumnList__ = __webpack_require__(12);


// ResultListing shows a table of items matching a query, with sortable columns
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "ResultListing",

    getInitialState: function () {
        return { selectedIndex: -1, addColumnsShowing: false };
    },
    handleResort: function (e) {
        // If a column heading was clicked, re-sort the table
        var columnNameClicked = e.target.getAttribute("data-name");
        var sortOrder = "asc";

        // If this column is already the sort column, toggle ascending/descending
        if (this.props.sortColumn === columnNameClicked) {
            sortOrder = this.props.sortOrder === "asc" ? "desc" : "asc";
        }

        // Store the sort column and order and raise the "onResort" event
        this.props.onResort(columnNameClicked, sortOrder);

        e.stopPropagation();
    },
    handleSelect: function (e) {
        var rowElement = e;
        this.setState({ selectedIndex: rowElement.props.itemIndex, addColumnsShowing: false });
        this.props.onSelectionChanged(rowElement.props.itemId);
    },
    handleAdd: function (e) {
        if (e.target === this.refs.addButton) {
            this.setState({ addColumnsShowing: !this.state.addColumnsShowing });
        }
        e.stopPropagation();
    },
    onAddColumn: function (name) {
        if (name) {
            var columns = this.props.data.content.query.columns;
            columns.push(name);

            this.setState({ addColumnsShowing: false }, this.props.onSetColumns(columns));
        } else {
            this.setState({ addColumnsShowing: false });
        }
    },
    handleRemoveColumn: function (e) {
        var columnName = e.target.getAttribute("data-column");
        var newColumns = this.props.data.content.query.columns.filter(function (name) {
            return name !== columnName;
        });
        this.props.onSetColumns(newColumns);

        e.stopPropagation();
    },
    handlePivot: function (e) {
        var element = e.target;

        this.props.onPivot(element.getAttribute("data-table"), element.getAttribute("data-query"));
        e.stopPropagation();
    },
    selectByRelativeIndex: function (i) {
        // Figure out the current row count
        var count = 0;
        if (this.props.data.content) count = this.props.data.content.values.rows.length;

        // See what index the caller wants selected
        var newSelectedIndex = this.state.selectedIndex + i;

        // If nothing was selected, let down open the first item
        if (!this.props.selectedId) newSelectedIndex = -1 + i;

        // If out-of-bounds, do nothing
        if (newSelectedIndex < 0 || newSelectedIndex >= count) return;

        // Otherwise, trigger a selection change
        var idColumnIndex = this.props.data.content.query.columns.indexOf(this.props.idColumn);
        var row = this.props.data.content.values.rows[newSelectedIndex];
        this.setState({ selectedIndex: newSelectedIndex });
        this.props.onSelectionChanged(stripHighlight(row[idColumnIndex]));
    },
    render: function () {
        var content = this.props.data.content;
        if (!content || !content.details.succeeded || !this.props.idColumn) return null;

        var idColumn = this.props.idColumn;
        var idColumnIndex = content.query.columns.indexOf(this.props.idColumn);

        // Write a column heading row - click to sort, remove/add columns
        var columnCells = [];
        for (var i = 0; i < content.values.columns.length; ++i) {
            var column = content.values.columns[i];

            var commands = [];

            // Non-ID column commands
            if (column.name !== idColumn) {
                // Remove button
                commands.push(React.createElement("div", { key: "remove_" + column.name, "data-column": column.name, className: "icon-cancel icon-column-heading", title: "Remove Column", onClick: this.handleRemoveColumn }));

                // Add 'Pivot to Grid' URL
                var gridUrl = "Grid.html" + buildUrlParameters({ q: content.query.where, t: content.query.tableName, R1: column.name + ">" });
                commands.push(React.createElement("a", { href: gridUrl, className: "icon-view-all-albums icon-column-heading", title: "Grid By " + column.name }));
            }

            // Last column
            if (i == content.values.columns.length - 1) {
                // Add column button
                commands.push(React.createElement(
                    "div",
                    { ref: "addButton", className: "add-column-button icon-add icon-column-heading", title: "Add Column", onClick: this.handleAdd },
                    React.createElement(__WEBPACK_IMPORTED_MODULE_0__AddColumnList__["a" /* default */], { showing: this.state.addColumnsShowing,
                        onAddColumn: this.onAddColumn,
                        allColumns: this.props.allColumns,
                        currentColumns: content.query.columns })
                ));
            }

            columnCells.push(React.createElement(
                "td",
                { key: "heading_" + column.name, "data-name": column.name, onClick: this.handleResort },
                React.createElement(
                    "div",
                    { className: "commands" },
                    commands
                ),
                column.name
            ));
        }

        // Write a row for each item
        var index = 0;
        var selectFunction = this.handleSelect;
        var selectedId = this.props.selectedId;
        var listingItems = content.values.rows.map(function (row) {
            var id = stripHighlight(row[idColumnIndex]);
            return React.createElement(ResultListingItem, { key: id, itemId: id, itemIndex: index++, data: row, onSelectionChanged: selectFunction, selected: selectedId === id });
        });

        return React.createElement(
            "table",
            { className: "resultTable", tabIndex: "2" },
            React.createElement(
                "thead",
                null,
                React.createElement(
                    "tr",
                    null,
                    columnCells
                )
            ),
            React.createElement(
                "tbody",
                null,
                listingItems
            )
        );
    }
});

var ResultListingItem = React.createClass({
    displayName: "ResultListingItem",

    handleClick: function (e) {
        this.props.onSelectionChanged(this);
    },
    render: function () {
        var id = this.props.itemId;

        // Write a cell for each property of each item with highlighting
        var cells = [];
        for (var i = 0; i < this.props.data.length; ++i) {
            var item = this.props.data[i];
            cells[i] = React.createElement("td", { key: id + "_" + i, title: stripHighlight(item), dangerouslySetInnerHTML: highlight(item) });
        }

        return React.createElement(
            "tr",
            { onClick: this.handleClick, className: this.props.selected ? "selected theme-background-light" : "theme-hover-light" },
            cells
        );
    }
});

/***/ }),
/* 16 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
// Inspired by https://github.com/tomkp/react-split-pane [MIT License]
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: 'SplitPane',

    defaultProps: { isFirstVisible: true, isSecondVisible: true },
    onMouseUp: function () {
        document.removeEventListener('mouseup', this.onMouseUp);
        document.removeEventListener('mousemove', this.onMouseMove);
    },
    onMouseDown: function (e) {
        document.addEventListener('mouseup', this.onMouseUp);
        document.addEventListener('mousemove', this.onMouseMove);

        this.setState({ totalWidth: this.refs.container.getBoundingClientRect().width });
    },
    onMouseMove: function (e) {
        var node = ReactDOM.findDOMNode(this.refs.first);
        var newWidth = e.clientX - node.offsetLeft;

        if (this.props.minSize) {
            if (newWidth < this.props.minSize) newWidth = this.props.minSize;
            if (newWidth > this.state.totalWidth - this.props.minSize) newWidth = this.state.totalWidth - this.props.minSize;
        }

        node.style.width = newWidth + 'px';
        node.style.flex = 'none';
    },
    render: function () {
        if (!this.props.isFirstVisible) return this.props.children[1];
        if (!this.props.isSecondVisible) return this.props.children[0];

        return React.createElement(
            'div',
            { ref: 'container', className: 'splitpane' },
            React.createElement(
                'div',
                { ref: 'first', className: 'pane' },
                this.props.children[0]
            ),
            React.createElement('div', { ref: 'handle',
                className: 'resizer vertical',
                onMouseDown: this.onMouseDown }),
            React.createElement(
                'div',
                { ref: 'second', className: 'pane' },
                this.props.children[1]
            )
        );
    }
});

/***/ }),
/* 17 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
/* harmony default export */ __webpack_exports__["a"] = React.createClass({
    displayName: "SyntaxHelp",

    getInitialState: function () {
        return { showHelp: this.props.showHelp };
    },
    toggle: function () {
        this.setState({ showHelp: !this.state.showHelp });
    },
    render: function () {
        if (!this.state.showHelp) {
            return React.createElement(
                "div",
                { className: "splash" },
                React.createElement(
                    "div",
                    { style: { width: "100%" } },
                    this.props.splashContent,
                    React.createElement(
                        "div",
                        { className: "buttons" },
                        React.createElement(
                            "a",
                            { className: "theme-background-dark", href: "/?help=true" },
                            React.createElement("i", { className: "icon-help", title: "Help" })
                        )
                    )
                )
            );
        }

        return React.createElement(
            "div",
            { className: "helpContent" },
            React.createElement(
                "div",
                { className: "helpIntro" },
                this.props.splashContent
            ),
            React.createElement(
                "div",
                { className: "features" },
                React.createElement(
                    "h2",
                    null,
                    "Features"
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click items to see details."
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click column headings to sort."
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click ",
                    React.createElement("span", { className: "icon-add" }),
                    " to add columns to listing, ",
                    React.createElement("span", { className: "icon-cancel" }),
                    " to remove them."
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click ",
                    React.createElement("span", { className: "icon-download" }),
                    " to download listing (same columns, query, and order) as CSV."
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click ",
                    React.createElement("img", { src: "icons/feed-icon-14x14.png", alt: "RSS" }),
                    " for URL to subscribe to query."
                )
            ),
            React.createElement(
                "div",
                { className: "syntaxIntro" },
                React.createElement(
                    "h2",
                    null,
                    "Syntax"
                ),
                React.createElement(
                    "table",
                    { className: "syntaxTable" },
                    React.createElement(
                        "thead",
                        null,
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "th",
                                { style: { width: "25%" } },
                                "Rule"
                            ),
                            React.createElement(
                                "th",
                                { style: { width: "25%" } },
                                "Example"
                            ),
                            React.createElement(
                                "th",
                                { style: { width: "50%" } },
                                "Meaning"
                            )
                        )
                    ),
                    React.createElement(
                        "tbody",
                        null,
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Type anything to search across all columns."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "halo Pris"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Find items with words starting with \"halo\" and \"Pris\" anywhere, case insensitive."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use <column> <operator> <value> to search one column."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "Team=Central AccessLevel > 3"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team equals \"Central\" (case sensitive) and AccessLevel is over 3."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use 'AND', 'OR', 'NOT', and parens for subexpressions."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "NOT Team=Central AND (AccessLevel > 3 OR Role=Administrator)"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team is not Central and (AccessLevel is over 3 or Role is Administrator."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use any operators from Web, SQL, or C# syntax."
                            ),
                            React.createElement(
                                "td",
                                null,
                                React.createElement(
                                    "table",
                                    { className: "syntaxTable" },
                                    React.createElement(
                                        "tbody",
                                        null,
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "&&"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "&"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "AND"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "aNd"
                                            )
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "||"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "|"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "OR"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "Or"
                                            )
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "!"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "-"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "NOT"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "noT"
                                            )
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "="
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "=="
                                            ),
                                            React.createElement("td", null),
                                            React.createElement("td", null)
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "<>"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "!="
                                            ),
                                            React.createElement("td", null),
                                            React.createElement("td", null)
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "|>"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "STARTSWITH"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "UNDER"
                                            ),
                                            React.createElement("td", null)
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                ":"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "MATCH"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "FREETEXT"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "CONTAINS"
                                            )
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "::"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "MATCHEXACT"
                                            ),
                                            React.createElement("td", null),
                                            React.createElement("td", null)
                                        )
                                    )
                                )
                            ),
                            React.createElement("td", null)
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use ':' for \"has a word starting with\" or \"::\" for \"has the exact word\""
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "Team:Centr && Name::Will"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team contains words starting with \"Centr\" and Name has the full word \"Will\" (\"William\" would not match) (case insensitive)."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use '|>' for starts with."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "Team |> Centr"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team starts with \"Centr\" (\"Grand Central\" would not match) (case sensitive)."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use \"\" to look for empty values."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "Team=\"\""
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team is empty."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use \"today-n\" (no spaces) for relative dates."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "HireDate < today-60"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "HireDate is more than 60 days ago [UTC]."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use any .NET DateTime.Parse-able formats."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "HireDate > \"2016-10-01 10:00AM\""
                            ),
                            React.createElement(
                                "td",
                                null,
                                "HireDate is after Oct 1, 2016 10:00 AM [UTC]."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Wrap column names with braces and values with quotes if they contain spaces. Escape braces and quotes by doubling them."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "[Owner [Ops]]]=\"Bilbo \"\"Ringbearer\"\" Baggins\""
                            ),
                            React.createElement(
                                "td",
                                null,
                                "The {Owner [Ops]} column equals {Bilbo \"Ringbearer\" Baggins}."
                            )
                        )
                    )
                )
            ),
            React.createElement(
                "div",
                { className: "syntaxExamples" },
                React.createElement(
                    "h2",
                    null,
                    "Examples"
                ),
                React.createElement(
                    "div",
                    { className: "exampleBox" },
                    React.createElement(
                        "div",
                        { className: "font-example" },
                        "Team=\"Central\" AND IP:10.194"
                    ),
                    React.createElement(
                        "div",
                        null,
                        "Find items where Team equals \"Central\", the IP address starts with \"10.194\"."
                    ),
                    React.createElement(
                        "div",
                        { className: "indent" },
                        "The word index operators (':', '::') are not case sensitive, but all other operators ('=', '!=', '|>') are case sensitive.",
                        React.createElement("br", null),
                        "The word index operators only match from word boundaries, so \"Team:ent\" will not match a Team of \"Central\".",
                        React.createElement("br", null),
                        "Text is split into alphanumeric words and dotted phrases. (\"10.194.155.11\" splits to \"10\", \"194\", \"155\", \"11\", \"10.194.155.11\")",
                        React.createElement("br", null),
                        "Therefore, \"IP:10.194\" will match \"10.194.155.11\" but not \"11.10.194.155\".",
                        React.createElement("br", null)
                    )
                ),
                React.createElement(
                    "div",
                    { className: "exampleBox" },
                    React.createElement(
                        "div",
                        { className: "font-example" },
                        "[Team]=\"Central\" && (HireDate < today-60 || IsManager == 1)"
                    ),
                    React.createElement(
                        "div",
                        null,
                        "Find items with Team \"Central\" which were hired in the last 60 days or are managers."
                    ),
                    React.createElement(
                        "div",
                        { className: "indent" },
                        "\"Today\" is midnight UTC, so \"today-1\" will match 11:59p the day before yesterday (UTC).",
                        React.createElement("br", null),
                        "DateTimes in queries are interpreted at UTC.",
                        React.createElement("br", null)
                    )
                )
            )
        );
    }
});

/***/ }),
/* 18 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


exports.byteLength = byteLength
exports.toByteArray = toByteArray
exports.fromByteArray = fromByteArray

var lookup = []
var revLookup = []
var Arr = typeof Uint8Array !== 'undefined' ? Uint8Array : Array

var code = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
for (var i = 0, len = code.length; i < len; ++i) {
  lookup[i] = code[i]
  revLookup[code.charCodeAt(i)] = i
}

revLookup['-'.charCodeAt(0)] = 62
revLookup['_'.charCodeAt(0)] = 63

function placeHoldersCount (b64) {
  var len = b64.length
  if (len % 4 > 0) {
    throw new Error('Invalid string. Length must be a multiple of 4')
  }

  // the number of equal signs (place holders)
  // if there are two placeholders, than the two characters before it
  // represent one byte
  // if there is only one, then the three characters before it represent 2 bytes
  // this is just a cheap hack to not do indexOf twice
  return b64[len - 2] === '=' ? 2 : b64[len - 1] === '=' ? 1 : 0
}

function byteLength (b64) {
  // base64 is 4/3 + up to two characters of the original data
  return b64.length * 3 / 4 - placeHoldersCount(b64)
}

function toByteArray (b64) {
  var i, j, l, tmp, placeHolders, arr
  var len = b64.length
  placeHolders = placeHoldersCount(b64)

  arr = new Arr(len * 3 / 4 - placeHolders)

  // if there are placeholders, only get up to the last complete 4 chars
  l = placeHolders > 0 ? len - 4 : len

  var L = 0

  for (i = 0, j = 0; i < l; i += 4, j += 3) {
    tmp = (revLookup[b64.charCodeAt(i)] << 18) | (revLookup[b64.charCodeAt(i + 1)] << 12) | (revLookup[b64.charCodeAt(i + 2)] << 6) | revLookup[b64.charCodeAt(i + 3)]
    arr[L++] = (tmp >> 16) & 0xFF
    arr[L++] = (tmp >> 8) & 0xFF
    arr[L++] = tmp & 0xFF
  }

  if (placeHolders === 2) {
    tmp = (revLookup[b64.charCodeAt(i)] << 2) | (revLookup[b64.charCodeAt(i + 1)] >> 4)
    arr[L++] = tmp & 0xFF
  } else if (placeHolders === 1) {
    tmp = (revLookup[b64.charCodeAt(i)] << 10) | (revLookup[b64.charCodeAt(i + 1)] << 4) | (revLookup[b64.charCodeAt(i + 2)] >> 2)
    arr[L++] = (tmp >> 8) & 0xFF
    arr[L++] = tmp & 0xFF
  }

  return arr
}

function tripletToBase64 (num) {
  return lookup[num >> 18 & 0x3F] + lookup[num >> 12 & 0x3F] + lookup[num >> 6 & 0x3F] + lookup[num & 0x3F]
}

function encodeChunk (uint8, start, end) {
  var tmp
  var output = []
  for (var i = start; i < end; i += 3) {
    tmp = (uint8[i] << 16) + (uint8[i + 1] << 8) + (uint8[i + 2])
    output.push(tripletToBase64(tmp))
  }
  return output.join('')
}

function fromByteArray (uint8) {
  var tmp
  var len = uint8.length
  var extraBytes = len % 3 // if we have 1 byte left, pad 2 bytes
  var output = ''
  var parts = []
  var maxChunkLength = 16383 // must be multiple of 3

  // go through the array every three bytes, we'll deal with trailing stuff later
  for (var i = 0, len2 = len - extraBytes; i < len2; i += maxChunkLength) {
    parts.push(encodeChunk(uint8, i, (i + maxChunkLength) > len2 ? len2 : (i + maxChunkLength)))
  }

  // pad the end with zeros, but make sure to not forget the extra bytes
  if (extraBytes === 1) {
    tmp = uint8[len - 1]
    output += lookup[tmp >> 2]
    output += lookup[(tmp << 4) & 0x3F]
    output += '=='
  } else if (extraBytes === 2) {
    tmp = (uint8[len - 2] << 8) + (uint8[len - 1])
    output += lookup[tmp >> 10]
    output += lookup[(tmp >> 4) & 0x3F]
    output += lookup[(tmp << 2) & 0x3F]
    output += '='
  }

  parts.push(output)

  return parts.join('')
}


/***/ }),
/* 19 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";
/* WEBPACK VAR INJECTION */(function(global) {/*!
 * The buffer module from node.js, for the browser.
 *
 * @author   Feross Aboukhadijeh <feross@feross.org> <http://feross.org>
 * @license  MIT
 */
/* eslint-disable no-proto */



var base64 = __webpack_require__(18)
var ieee754 = __webpack_require__(22)
var isArray = __webpack_require__(23)

exports.Buffer = Buffer
exports.SlowBuffer = SlowBuffer
exports.INSPECT_MAX_BYTES = 50

/**
 * If `Buffer.TYPED_ARRAY_SUPPORT`:
 *   === true    Use Uint8Array implementation (fastest)
 *   === false   Use Object implementation (most compatible, even IE6)
 *
 * Browsers that support typed arrays are IE 10+, Firefox 4+, Chrome 7+, Safari 5.1+,
 * Opera 11.6+, iOS 4.2+.
 *
 * Due to various browser bugs, sometimes the Object implementation will be used even
 * when the browser supports typed arrays.
 *
 * Note:
 *
 *   - Firefox 4-29 lacks support for adding new properties to `Uint8Array` instances,
 *     See: https://bugzilla.mozilla.org/show_bug.cgi?id=695438.
 *
 *   - Chrome 9-10 is missing the `TypedArray.prototype.subarray` function.
 *
 *   - IE10 has a broken `TypedArray.prototype.subarray` function which returns arrays of
 *     incorrect length in some situations.

 * We detect these buggy browsers and set `Buffer.TYPED_ARRAY_SUPPORT` to `false` so they
 * get the Object implementation, which is slower but behaves correctly.
 */
Buffer.TYPED_ARRAY_SUPPORT = global.TYPED_ARRAY_SUPPORT !== undefined
  ? global.TYPED_ARRAY_SUPPORT
  : typedArraySupport()

/*
 * Export kMaxLength after typed array support is determined.
 */
exports.kMaxLength = kMaxLength()

function typedArraySupport () {
  try {
    var arr = new Uint8Array(1)
    arr.__proto__ = {__proto__: Uint8Array.prototype, foo: function () { return 42 }}
    return arr.foo() === 42 && // typed array instances can be augmented
        typeof arr.subarray === 'function' && // chrome 9-10 lack `subarray`
        arr.subarray(1, 1).byteLength === 0 // ie10 has broken `subarray`
  } catch (e) {
    return false
  }
}

function kMaxLength () {
  return Buffer.TYPED_ARRAY_SUPPORT
    ? 0x7fffffff
    : 0x3fffffff
}

function createBuffer (that, length) {
  if (kMaxLength() < length) {
    throw new RangeError('Invalid typed array length')
  }
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    // Return an augmented `Uint8Array` instance, for best performance
    that = new Uint8Array(length)
    that.__proto__ = Buffer.prototype
  } else {
    // Fallback: Return an object instance of the Buffer class
    if (that === null) {
      that = new Buffer(length)
    }
    that.length = length
  }

  return that
}

/**
 * The Buffer constructor returns instances of `Uint8Array` that have their
 * prototype changed to `Buffer.prototype`. Furthermore, `Buffer` is a subclass of
 * `Uint8Array`, so the returned instances will have all the node `Buffer` methods
 * and the `Uint8Array` methods. Square bracket notation works as expected -- it
 * returns a single octet.
 *
 * The `Uint8Array` prototype remains unmodified.
 */

function Buffer (arg, encodingOrOffset, length) {
  if (!Buffer.TYPED_ARRAY_SUPPORT && !(this instanceof Buffer)) {
    return new Buffer(arg, encodingOrOffset, length)
  }

  // Common case.
  if (typeof arg === 'number') {
    if (typeof encodingOrOffset === 'string') {
      throw new Error(
        'If encoding is specified then the first argument must be a string'
      )
    }
    return allocUnsafe(this, arg)
  }
  return from(this, arg, encodingOrOffset, length)
}

Buffer.poolSize = 8192 // not used by this implementation

// TODO: Legacy, not needed anymore. Remove in next major version.
Buffer._augment = function (arr) {
  arr.__proto__ = Buffer.prototype
  return arr
}

function from (that, value, encodingOrOffset, length) {
  if (typeof value === 'number') {
    throw new TypeError('"value" argument must not be a number')
  }

  if (typeof ArrayBuffer !== 'undefined' && value instanceof ArrayBuffer) {
    return fromArrayBuffer(that, value, encodingOrOffset, length)
  }

  if (typeof value === 'string') {
    return fromString(that, value, encodingOrOffset)
  }

  return fromObject(that, value)
}

/**
 * Functionally equivalent to Buffer(arg, encoding) but throws a TypeError
 * if value is a number.
 * Buffer.from(str[, encoding])
 * Buffer.from(array)
 * Buffer.from(buffer)
 * Buffer.from(arrayBuffer[, byteOffset[, length]])
 **/
Buffer.from = function (value, encodingOrOffset, length) {
  return from(null, value, encodingOrOffset, length)
}

if (Buffer.TYPED_ARRAY_SUPPORT) {
  Buffer.prototype.__proto__ = Uint8Array.prototype
  Buffer.__proto__ = Uint8Array
  if (typeof Symbol !== 'undefined' && Symbol.species &&
      Buffer[Symbol.species] === Buffer) {
    // Fix subarray() in ES2016. See: https://github.com/feross/buffer/pull/97
    Object.defineProperty(Buffer, Symbol.species, {
      value: null,
      configurable: true
    })
  }
}

function assertSize (size) {
  if (typeof size !== 'number') {
    throw new TypeError('"size" argument must be a number')
  } else if (size < 0) {
    throw new RangeError('"size" argument must not be negative')
  }
}

function alloc (that, size, fill, encoding) {
  assertSize(size)
  if (size <= 0) {
    return createBuffer(that, size)
  }
  if (fill !== undefined) {
    // Only pay attention to encoding if it's a string. This
    // prevents accidentally sending in a number that would
    // be interpretted as a start offset.
    return typeof encoding === 'string'
      ? createBuffer(that, size).fill(fill, encoding)
      : createBuffer(that, size).fill(fill)
  }
  return createBuffer(that, size)
}

/**
 * Creates a new filled Buffer instance.
 * alloc(size[, fill[, encoding]])
 **/
Buffer.alloc = function (size, fill, encoding) {
  return alloc(null, size, fill, encoding)
}

function allocUnsafe (that, size) {
  assertSize(size)
  that = createBuffer(that, size < 0 ? 0 : checked(size) | 0)
  if (!Buffer.TYPED_ARRAY_SUPPORT) {
    for (var i = 0; i < size; ++i) {
      that[i] = 0
    }
  }
  return that
}

/**
 * Equivalent to Buffer(num), by default creates a non-zero-filled Buffer instance.
 * */
Buffer.allocUnsafe = function (size) {
  return allocUnsafe(null, size)
}
/**
 * Equivalent to SlowBuffer(num), by default creates a non-zero-filled Buffer instance.
 */
Buffer.allocUnsafeSlow = function (size) {
  return allocUnsafe(null, size)
}

function fromString (that, string, encoding) {
  if (typeof encoding !== 'string' || encoding === '') {
    encoding = 'utf8'
  }

  if (!Buffer.isEncoding(encoding)) {
    throw new TypeError('"encoding" must be a valid string encoding')
  }

  var length = byteLength(string, encoding) | 0
  that = createBuffer(that, length)

  var actual = that.write(string, encoding)

  if (actual !== length) {
    // Writing a hex string, for example, that contains invalid characters will
    // cause everything after the first invalid character to be ignored. (e.g.
    // 'abxxcd' will be treated as 'ab')
    that = that.slice(0, actual)
  }

  return that
}

function fromArrayLike (that, array) {
  var length = array.length < 0 ? 0 : checked(array.length) | 0
  that = createBuffer(that, length)
  for (var i = 0; i < length; i += 1) {
    that[i] = array[i] & 255
  }
  return that
}

function fromArrayBuffer (that, array, byteOffset, length) {
  array.byteLength // this throws if `array` is not a valid ArrayBuffer

  if (byteOffset < 0 || array.byteLength < byteOffset) {
    throw new RangeError('\'offset\' is out of bounds')
  }

  if (array.byteLength < byteOffset + (length || 0)) {
    throw new RangeError('\'length\' is out of bounds')
  }

  if (byteOffset === undefined && length === undefined) {
    array = new Uint8Array(array)
  } else if (length === undefined) {
    array = new Uint8Array(array, byteOffset)
  } else {
    array = new Uint8Array(array, byteOffset, length)
  }

  if (Buffer.TYPED_ARRAY_SUPPORT) {
    // Return an augmented `Uint8Array` instance, for best performance
    that = array
    that.__proto__ = Buffer.prototype
  } else {
    // Fallback: Return an object instance of the Buffer class
    that = fromArrayLike(that, array)
  }
  return that
}

function fromObject (that, obj) {
  if (Buffer.isBuffer(obj)) {
    var len = checked(obj.length) | 0
    that = createBuffer(that, len)

    if (that.length === 0) {
      return that
    }

    obj.copy(that, 0, 0, len)
    return that
  }

  if (obj) {
    if ((typeof ArrayBuffer !== 'undefined' &&
        obj.buffer instanceof ArrayBuffer) || 'length' in obj) {
      if (typeof obj.length !== 'number' || isnan(obj.length)) {
        return createBuffer(that, 0)
      }
      return fromArrayLike(that, obj)
    }

    if (obj.type === 'Buffer' && isArray(obj.data)) {
      return fromArrayLike(that, obj.data)
    }
  }

  throw new TypeError('First argument must be a string, Buffer, ArrayBuffer, Array, or array-like object.')
}

function checked (length) {
  // Note: cannot use `length < kMaxLength()` here because that fails when
  // length is NaN (which is otherwise coerced to zero.)
  if (length >= kMaxLength()) {
    throw new RangeError('Attempt to allocate Buffer larger than maximum ' +
                         'size: 0x' + kMaxLength().toString(16) + ' bytes')
  }
  return length | 0
}

function SlowBuffer (length) {
  if (+length != length) { // eslint-disable-line eqeqeq
    length = 0
  }
  return Buffer.alloc(+length)
}

Buffer.isBuffer = function isBuffer (b) {
  return !!(b != null && b._isBuffer)
}

Buffer.compare = function compare (a, b) {
  if (!Buffer.isBuffer(a) || !Buffer.isBuffer(b)) {
    throw new TypeError('Arguments must be Buffers')
  }

  if (a === b) return 0

  var x = a.length
  var y = b.length

  for (var i = 0, len = Math.min(x, y); i < len; ++i) {
    if (a[i] !== b[i]) {
      x = a[i]
      y = b[i]
      break
    }
  }

  if (x < y) return -1
  if (y < x) return 1
  return 0
}

Buffer.isEncoding = function isEncoding (encoding) {
  switch (String(encoding).toLowerCase()) {
    case 'hex':
    case 'utf8':
    case 'utf-8':
    case 'ascii':
    case 'latin1':
    case 'binary':
    case 'base64':
    case 'ucs2':
    case 'ucs-2':
    case 'utf16le':
    case 'utf-16le':
      return true
    default:
      return false
  }
}

Buffer.concat = function concat (list, length) {
  if (!isArray(list)) {
    throw new TypeError('"list" argument must be an Array of Buffers')
  }

  if (list.length === 0) {
    return Buffer.alloc(0)
  }

  var i
  if (length === undefined) {
    length = 0
    for (i = 0; i < list.length; ++i) {
      length += list[i].length
    }
  }

  var buffer = Buffer.allocUnsafe(length)
  var pos = 0
  for (i = 0; i < list.length; ++i) {
    var buf = list[i]
    if (!Buffer.isBuffer(buf)) {
      throw new TypeError('"list" argument must be an Array of Buffers')
    }
    buf.copy(buffer, pos)
    pos += buf.length
  }
  return buffer
}

function byteLength (string, encoding) {
  if (Buffer.isBuffer(string)) {
    return string.length
  }
  if (typeof ArrayBuffer !== 'undefined' && typeof ArrayBuffer.isView === 'function' &&
      (ArrayBuffer.isView(string) || string instanceof ArrayBuffer)) {
    return string.byteLength
  }
  if (typeof string !== 'string') {
    string = '' + string
  }

  var len = string.length
  if (len === 0) return 0

  // Use a for loop to avoid recursion
  var loweredCase = false
  for (;;) {
    switch (encoding) {
      case 'ascii':
      case 'latin1':
      case 'binary':
        return len
      case 'utf8':
      case 'utf-8':
      case undefined:
        return utf8ToBytes(string).length
      case 'ucs2':
      case 'ucs-2':
      case 'utf16le':
      case 'utf-16le':
        return len * 2
      case 'hex':
        return len >>> 1
      case 'base64':
        return base64ToBytes(string).length
      default:
        if (loweredCase) return utf8ToBytes(string).length // assume utf8
        encoding = ('' + encoding).toLowerCase()
        loweredCase = true
    }
  }
}
Buffer.byteLength = byteLength

function slowToString (encoding, start, end) {
  var loweredCase = false

  // No need to verify that "this.length <= MAX_UINT32" since it's a read-only
  // property of a typed array.

  // This behaves neither like String nor Uint8Array in that we set start/end
  // to their upper/lower bounds if the value passed is out of range.
  // undefined is handled specially as per ECMA-262 6th Edition,
  // Section 13.3.3.7 Runtime Semantics: KeyedBindingInitialization.
  if (start === undefined || start < 0) {
    start = 0
  }
  // Return early if start > this.length. Done here to prevent potential uint32
  // coercion fail below.
  if (start > this.length) {
    return ''
  }

  if (end === undefined || end > this.length) {
    end = this.length
  }

  if (end <= 0) {
    return ''
  }

  // Force coersion to uint32. This will also coerce falsey/NaN values to 0.
  end >>>= 0
  start >>>= 0

  if (end <= start) {
    return ''
  }

  if (!encoding) encoding = 'utf8'

  while (true) {
    switch (encoding) {
      case 'hex':
        return hexSlice(this, start, end)

      case 'utf8':
      case 'utf-8':
        return utf8Slice(this, start, end)

      case 'ascii':
        return asciiSlice(this, start, end)

      case 'latin1':
      case 'binary':
        return latin1Slice(this, start, end)

      case 'base64':
        return base64Slice(this, start, end)

      case 'ucs2':
      case 'ucs-2':
      case 'utf16le':
      case 'utf-16le':
        return utf16leSlice(this, start, end)

      default:
        if (loweredCase) throw new TypeError('Unknown encoding: ' + encoding)
        encoding = (encoding + '').toLowerCase()
        loweredCase = true
    }
  }
}

// The property is used by `Buffer.isBuffer` and `is-buffer` (in Safari 5-7) to detect
// Buffer instances.
Buffer.prototype._isBuffer = true

function swap (b, n, m) {
  var i = b[n]
  b[n] = b[m]
  b[m] = i
}

Buffer.prototype.swap16 = function swap16 () {
  var len = this.length
  if (len % 2 !== 0) {
    throw new RangeError('Buffer size must be a multiple of 16-bits')
  }
  for (var i = 0; i < len; i += 2) {
    swap(this, i, i + 1)
  }
  return this
}

Buffer.prototype.swap32 = function swap32 () {
  var len = this.length
  if (len % 4 !== 0) {
    throw new RangeError('Buffer size must be a multiple of 32-bits')
  }
  for (var i = 0; i < len; i += 4) {
    swap(this, i, i + 3)
    swap(this, i + 1, i + 2)
  }
  return this
}

Buffer.prototype.swap64 = function swap64 () {
  var len = this.length
  if (len % 8 !== 0) {
    throw new RangeError('Buffer size must be a multiple of 64-bits')
  }
  for (var i = 0; i < len; i += 8) {
    swap(this, i, i + 7)
    swap(this, i + 1, i + 6)
    swap(this, i + 2, i + 5)
    swap(this, i + 3, i + 4)
  }
  return this
}

Buffer.prototype.toString = function toString () {
  var length = this.length | 0
  if (length === 0) return ''
  if (arguments.length === 0) return utf8Slice(this, 0, length)
  return slowToString.apply(this, arguments)
}

Buffer.prototype.equals = function equals (b) {
  if (!Buffer.isBuffer(b)) throw new TypeError('Argument must be a Buffer')
  if (this === b) return true
  return Buffer.compare(this, b) === 0
}

Buffer.prototype.inspect = function inspect () {
  var str = ''
  var max = exports.INSPECT_MAX_BYTES
  if (this.length > 0) {
    str = this.toString('hex', 0, max).match(/.{2}/g).join(' ')
    if (this.length > max) str += ' ... '
  }
  return '<Buffer ' + str + '>'
}

Buffer.prototype.compare = function compare (target, start, end, thisStart, thisEnd) {
  if (!Buffer.isBuffer(target)) {
    throw new TypeError('Argument must be a Buffer')
  }

  if (start === undefined) {
    start = 0
  }
  if (end === undefined) {
    end = target ? target.length : 0
  }
  if (thisStart === undefined) {
    thisStart = 0
  }
  if (thisEnd === undefined) {
    thisEnd = this.length
  }

  if (start < 0 || end > target.length || thisStart < 0 || thisEnd > this.length) {
    throw new RangeError('out of range index')
  }

  if (thisStart >= thisEnd && start >= end) {
    return 0
  }
  if (thisStart >= thisEnd) {
    return -1
  }
  if (start >= end) {
    return 1
  }

  start >>>= 0
  end >>>= 0
  thisStart >>>= 0
  thisEnd >>>= 0

  if (this === target) return 0

  var x = thisEnd - thisStart
  var y = end - start
  var len = Math.min(x, y)

  var thisCopy = this.slice(thisStart, thisEnd)
  var targetCopy = target.slice(start, end)

  for (var i = 0; i < len; ++i) {
    if (thisCopy[i] !== targetCopy[i]) {
      x = thisCopy[i]
      y = targetCopy[i]
      break
    }
  }

  if (x < y) return -1
  if (y < x) return 1
  return 0
}

// Finds either the first index of `val` in `buffer` at offset >= `byteOffset`,
// OR the last index of `val` in `buffer` at offset <= `byteOffset`.
//
// Arguments:
// - buffer - a Buffer to search
// - val - a string, Buffer, or number
// - byteOffset - an index into `buffer`; will be clamped to an int32
// - encoding - an optional encoding, relevant is val is a string
// - dir - true for indexOf, false for lastIndexOf
function bidirectionalIndexOf (buffer, val, byteOffset, encoding, dir) {
  // Empty buffer means no match
  if (buffer.length === 0) return -1

  // Normalize byteOffset
  if (typeof byteOffset === 'string') {
    encoding = byteOffset
    byteOffset = 0
  } else if (byteOffset > 0x7fffffff) {
    byteOffset = 0x7fffffff
  } else if (byteOffset < -0x80000000) {
    byteOffset = -0x80000000
  }
  byteOffset = +byteOffset  // Coerce to Number.
  if (isNaN(byteOffset)) {
    // byteOffset: it it's undefined, null, NaN, "foo", etc, search whole buffer
    byteOffset = dir ? 0 : (buffer.length - 1)
  }

  // Normalize byteOffset: negative offsets start from the end of the buffer
  if (byteOffset < 0) byteOffset = buffer.length + byteOffset
  if (byteOffset >= buffer.length) {
    if (dir) return -1
    else byteOffset = buffer.length - 1
  } else if (byteOffset < 0) {
    if (dir) byteOffset = 0
    else return -1
  }

  // Normalize val
  if (typeof val === 'string') {
    val = Buffer.from(val, encoding)
  }

  // Finally, search either indexOf (if dir is true) or lastIndexOf
  if (Buffer.isBuffer(val)) {
    // Special case: looking for empty string/buffer always fails
    if (val.length === 0) {
      return -1
    }
    return arrayIndexOf(buffer, val, byteOffset, encoding, dir)
  } else if (typeof val === 'number') {
    val = val & 0xFF // Search for a byte value [0-255]
    if (Buffer.TYPED_ARRAY_SUPPORT &&
        typeof Uint8Array.prototype.indexOf === 'function') {
      if (dir) {
        return Uint8Array.prototype.indexOf.call(buffer, val, byteOffset)
      } else {
        return Uint8Array.prototype.lastIndexOf.call(buffer, val, byteOffset)
      }
    }
    return arrayIndexOf(buffer, [ val ], byteOffset, encoding, dir)
  }

  throw new TypeError('val must be string, number or Buffer')
}

function arrayIndexOf (arr, val, byteOffset, encoding, dir) {
  var indexSize = 1
  var arrLength = arr.length
  var valLength = val.length

  if (encoding !== undefined) {
    encoding = String(encoding).toLowerCase()
    if (encoding === 'ucs2' || encoding === 'ucs-2' ||
        encoding === 'utf16le' || encoding === 'utf-16le') {
      if (arr.length < 2 || val.length < 2) {
        return -1
      }
      indexSize = 2
      arrLength /= 2
      valLength /= 2
      byteOffset /= 2
    }
  }

  function read (buf, i) {
    if (indexSize === 1) {
      return buf[i]
    } else {
      return buf.readUInt16BE(i * indexSize)
    }
  }

  var i
  if (dir) {
    var foundIndex = -1
    for (i = byteOffset; i < arrLength; i++) {
      if (read(arr, i) === read(val, foundIndex === -1 ? 0 : i - foundIndex)) {
        if (foundIndex === -1) foundIndex = i
        if (i - foundIndex + 1 === valLength) return foundIndex * indexSize
      } else {
        if (foundIndex !== -1) i -= i - foundIndex
        foundIndex = -1
      }
    }
  } else {
    if (byteOffset + valLength > arrLength) byteOffset = arrLength - valLength
    for (i = byteOffset; i >= 0; i--) {
      var found = true
      for (var j = 0; j < valLength; j++) {
        if (read(arr, i + j) !== read(val, j)) {
          found = false
          break
        }
      }
      if (found) return i
    }
  }

  return -1
}

Buffer.prototype.includes = function includes (val, byteOffset, encoding) {
  return this.indexOf(val, byteOffset, encoding) !== -1
}

Buffer.prototype.indexOf = function indexOf (val, byteOffset, encoding) {
  return bidirectionalIndexOf(this, val, byteOffset, encoding, true)
}

Buffer.prototype.lastIndexOf = function lastIndexOf (val, byteOffset, encoding) {
  return bidirectionalIndexOf(this, val, byteOffset, encoding, false)
}

function hexWrite (buf, string, offset, length) {
  offset = Number(offset) || 0
  var remaining = buf.length - offset
  if (!length) {
    length = remaining
  } else {
    length = Number(length)
    if (length > remaining) {
      length = remaining
    }
  }

  // must be an even number of digits
  var strLen = string.length
  if (strLen % 2 !== 0) throw new TypeError('Invalid hex string')

  if (length > strLen / 2) {
    length = strLen / 2
  }
  for (var i = 0; i < length; ++i) {
    var parsed = parseInt(string.substr(i * 2, 2), 16)
    if (isNaN(parsed)) return i
    buf[offset + i] = parsed
  }
  return i
}

function utf8Write (buf, string, offset, length) {
  return blitBuffer(utf8ToBytes(string, buf.length - offset), buf, offset, length)
}

function asciiWrite (buf, string, offset, length) {
  return blitBuffer(asciiToBytes(string), buf, offset, length)
}

function latin1Write (buf, string, offset, length) {
  return asciiWrite(buf, string, offset, length)
}

function base64Write (buf, string, offset, length) {
  return blitBuffer(base64ToBytes(string), buf, offset, length)
}

function ucs2Write (buf, string, offset, length) {
  return blitBuffer(utf16leToBytes(string, buf.length - offset), buf, offset, length)
}

Buffer.prototype.write = function write (string, offset, length, encoding) {
  // Buffer#write(string)
  if (offset === undefined) {
    encoding = 'utf8'
    length = this.length
    offset = 0
  // Buffer#write(string, encoding)
  } else if (length === undefined && typeof offset === 'string') {
    encoding = offset
    length = this.length
    offset = 0
  // Buffer#write(string, offset[, length][, encoding])
  } else if (isFinite(offset)) {
    offset = offset | 0
    if (isFinite(length)) {
      length = length | 0
      if (encoding === undefined) encoding = 'utf8'
    } else {
      encoding = length
      length = undefined
    }
  // legacy write(string, encoding, offset, length) - remove in v0.13
  } else {
    throw new Error(
      'Buffer.write(string, encoding, offset[, length]) is no longer supported'
    )
  }

  var remaining = this.length - offset
  if (length === undefined || length > remaining) length = remaining

  if ((string.length > 0 && (length < 0 || offset < 0)) || offset > this.length) {
    throw new RangeError('Attempt to write outside buffer bounds')
  }

  if (!encoding) encoding = 'utf8'

  var loweredCase = false
  for (;;) {
    switch (encoding) {
      case 'hex':
        return hexWrite(this, string, offset, length)

      case 'utf8':
      case 'utf-8':
        return utf8Write(this, string, offset, length)

      case 'ascii':
        return asciiWrite(this, string, offset, length)

      case 'latin1':
      case 'binary':
        return latin1Write(this, string, offset, length)

      case 'base64':
        // Warning: maxLength not taken into account in base64Write
        return base64Write(this, string, offset, length)

      case 'ucs2':
      case 'ucs-2':
      case 'utf16le':
      case 'utf-16le':
        return ucs2Write(this, string, offset, length)

      default:
        if (loweredCase) throw new TypeError('Unknown encoding: ' + encoding)
        encoding = ('' + encoding).toLowerCase()
        loweredCase = true
    }
  }
}

Buffer.prototype.toJSON = function toJSON () {
  return {
    type: 'Buffer',
    data: Array.prototype.slice.call(this._arr || this, 0)
  }
}

function base64Slice (buf, start, end) {
  if (start === 0 && end === buf.length) {
    return base64.fromByteArray(buf)
  } else {
    return base64.fromByteArray(buf.slice(start, end))
  }
}

function utf8Slice (buf, start, end) {
  end = Math.min(buf.length, end)
  var res = []

  var i = start
  while (i < end) {
    var firstByte = buf[i]
    var codePoint = null
    var bytesPerSequence = (firstByte > 0xEF) ? 4
      : (firstByte > 0xDF) ? 3
      : (firstByte > 0xBF) ? 2
      : 1

    if (i + bytesPerSequence <= end) {
      var secondByte, thirdByte, fourthByte, tempCodePoint

      switch (bytesPerSequence) {
        case 1:
          if (firstByte < 0x80) {
            codePoint = firstByte
          }
          break
        case 2:
          secondByte = buf[i + 1]
          if ((secondByte & 0xC0) === 0x80) {
            tempCodePoint = (firstByte & 0x1F) << 0x6 | (secondByte & 0x3F)
            if (tempCodePoint > 0x7F) {
              codePoint = tempCodePoint
            }
          }
          break
        case 3:
          secondByte = buf[i + 1]
          thirdByte = buf[i + 2]
          if ((secondByte & 0xC0) === 0x80 && (thirdByte & 0xC0) === 0x80) {
            tempCodePoint = (firstByte & 0xF) << 0xC | (secondByte & 0x3F) << 0x6 | (thirdByte & 0x3F)
            if (tempCodePoint > 0x7FF && (tempCodePoint < 0xD800 || tempCodePoint > 0xDFFF)) {
              codePoint = tempCodePoint
            }
          }
          break
        case 4:
          secondByte = buf[i + 1]
          thirdByte = buf[i + 2]
          fourthByte = buf[i + 3]
          if ((secondByte & 0xC0) === 0x80 && (thirdByte & 0xC0) === 0x80 && (fourthByte & 0xC0) === 0x80) {
            tempCodePoint = (firstByte & 0xF) << 0x12 | (secondByte & 0x3F) << 0xC | (thirdByte & 0x3F) << 0x6 | (fourthByte & 0x3F)
            if (tempCodePoint > 0xFFFF && tempCodePoint < 0x110000) {
              codePoint = tempCodePoint
            }
          }
      }
    }

    if (codePoint === null) {
      // we did not generate a valid codePoint so insert a
      // replacement char (U+FFFD) and advance only 1 byte
      codePoint = 0xFFFD
      bytesPerSequence = 1
    } else if (codePoint > 0xFFFF) {
      // encode to utf16 (surrogate pair dance)
      codePoint -= 0x10000
      res.push(codePoint >>> 10 & 0x3FF | 0xD800)
      codePoint = 0xDC00 | codePoint & 0x3FF
    }

    res.push(codePoint)
    i += bytesPerSequence
  }

  return decodeCodePointsArray(res)
}

// Based on http://stackoverflow.com/a/22747272/680742, the browser with
// the lowest limit is Chrome, with 0x10000 args.
// We go 1 magnitude less, for safety
var MAX_ARGUMENTS_LENGTH = 0x1000

function decodeCodePointsArray (codePoints) {
  var len = codePoints.length
  if (len <= MAX_ARGUMENTS_LENGTH) {
    return String.fromCharCode.apply(String, codePoints) // avoid extra slice()
  }

  // Decode in chunks to avoid "call stack size exceeded".
  var res = ''
  var i = 0
  while (i < len) {
    res += String.fromCharCode.apply(
      String,
      codePoints.slice(i, i += MAX_ARGUMENTS_LENGTH)
    )
  }
  return res
}

function asciiSlice (buf, start, end) {
  var ret = ''
  end = Math.min(buf.length, end)

  for (var i = start; i < end; ++i) {
    ret += String.fromCharCode(buf[i] & 0x7F)
  }
  return ret
}

function latin1Slice (buf, start, end) {
  var ret = ''
  end = Math.min(buf.length, end)

  for (var i = start; i < end; ++i) {
    ret += String.fromCharCode(buf[i])
  }
  return ret
}

function hexSlice (buf, start, end) {
  var len = buf.length

  if (!start || start < 0) start = 0
  if (!end || end < 0 || end > len) end = len

  var out = ''
  for (var i = start; i < end; ++i) {
    out += toHex(buf[i])
  }
  return out
}

function utf16leSlice (buf, start, end) {
  var bytes = buf.slice(start, end)
  var res = ''
  for (var i = 0; i < bytes.length; i += 2) {
    res += String.fromCharCode(bytes[i] + bytes[i + 1] * 256)
  }
  return res
}

Buffer.prototype.slice = function slice (start, end) {
  var len = this.length
  start = ~~start
  end = end === undefined ? len : ~~end

  if (start < 0) {
    start += len
    if (start < 0) start = 0
  } else if (start > len) {
    start = len
  }

  if (end < 0) {
    end += len
    if (end < 0) end = 0
  } else if (end > len) {
    end = len
  }

  if (end < start) end = start

  var newBuf
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    newBuf = this.subarray(start, end)
    newBuf.__proto__ = Buffer.prototype
  } else {
    var sliceLen = end - start
    newBuf = new Buffer(sliceLen, undefined)
    for (var i = 0; i < sliceLen; ++i) {
      newBuf[i] = this[i + start]
    }
  }

  return newBuf
}

/*
 * Need to make sure that buffer isn't trying to write out of bounds.
 */
function checkOffset (offset, ext, length) {
  if ((offset % 1) !== 0 || offset < 0) throw new RangeError('offset is not uint')
  if (offset + ext > length) throw new RangeError('Trying to access beyond buffer length')
}

Buffer.prototype.readUIntLE = function readUIntLE (offset, byteLength, noAssert) {
  offset = offset | 0
  byteLength = byteLength | 0
  if (!noAssert) checkOffset(offset, byteLength, this.length)

  var val = this[offset]
  var mul = 1
  var i = 0
  while (++i < byteLength && (mul *= 0x100)) {
    val += this[offset + i] * mul
  }

  return val
}

Buffer.prototype.readUIntBE = function readUIntBE (offset, byteLength, noAssert) {
  offset = offset | 0
  byteLength = byteLength | 0
  if (!noAssert) {
    checkOffset(offset, byteLength, this.length)
  }

  var val = this[offset + --byteLength]
  var mul = 1
  while (byteLength > 0 && (mul *= 0x100)) {
    val += this[offset + --byteLength] * mul
  }

  return val
}

Buffer.prototype.readUInt8 = function readUInt8 (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 1, this.length)
  return this[offset]
}

Buffer.prototype.readUInt16LE = function readUInt16LE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 2, this.length)
  return this[offset] | (this[offset + 1] << 8)
}

Buffer.prototype.readUInt16BE = function readUInt16BE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 2, this.length)
  return (this[offset] << 8) | this[offset + 1]
}

Buffer.prototype.readUInt32LE = function readUInt32LE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 4, this.length)

  return ((this[offset]) |
      (this[offset + 1] << 8) |
      (this[offset + 2] << 16)) +
      (this[offset + 3] * 0x1000000)
}

Buffer.prototype.readUInt32BE = function readUInt32BE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 4, this.length)

  return (this[offset] * 0x1000000) +
    ((this[offset + 1] << 16) |
    (this[offset + 2] << 8) |
    this[offset + 3])
}

Buffer.prototype.readIntLE = function readIntLE (offset, byteLength, noAssert) {
  offset = offset | 0
  byteLength = byteLength | 0
  if (!noAssert) checkOffset(offset, byteLength, this.length)

  var val = this[offset]
  var mul = 1
  var i = 0
  while (++i < byteLength && (mul *= 0x100)) {
    val += this[offset + i] * mul
  }
  mul *= 0x80

  if (val >= mul) val -= Math.pow(2, 8 * byteLength)

  return val
}

Buffer.prototype.readIntBE = function readIntBE (offset, byteLength, noAssert) {
  offset = offset | 0
  byteLength = byteLength | 0
  if (!noAssert) checkOffset(offset, byteLength, this.length)

  var i = byteLength
  var mul = 1
  var val = this[offset + --i]
  while (i > 0 && (mul *= 0x100)) {
    val += this[offset + --i] * mul
  }
  mul *= 0x80

  if (val >= mul) val -= Math.pow(2, 8 * byteLength)

  return val
}

Buffer.prototype.readInt8 = function readInt8 (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 1, this.length)
  if (!(this[offset] & 0x80)) return (this[offset])
  return ((0xff - this[offset] + 1) * -1)
}

Buffer.prototype.readInt16LE = function readInt16LE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 2, this.length)
  var val = this[offset] | (this[offset + 1] << 8)
  return (val & 0x8000) ? val | 0xFFFF0000 : val
}

Buffer.prototype.readInt16BE = function readInt16BE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 2, this.length)
  var val = this[offset + 1] | (this[offset] << 8)
  return (val & 0x8000) ? val | 0xFFFF0000 : val
}

Buffer.prototype.readInt32LE = function readInt32LE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 4, this.length)

  return (this[offset]) |
    (this[offset + 1] << 8) |
    (this[offset + 2] << 16) |
    (this[offset + 3] << 24)
}

Buffer.prototype.readInt32BE = function readInt32BE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 4, this.length)

  return (this[offset] << 24) |
    (this[offset + 1] << 16) |
    (this[offset + 2] << 8) |
    (this[offset + 3])
}

Buffer.prototype.readFloatLE = function readFloatLE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 4, this.length)
  return ieee754.read(this, offset, true, 23, 4)
}

Buffer.prototype.readFloatBE = function readFloatBE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 4, this.length)
  return ieee754.read(this, offset, false, 23, 4)
}

Buffer.prototype.readDoubleLE = function readDoubleLE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 8, this.length)
  return ieee754.read(this, offset, true, 52, 8)
}

Buffer.prototype.readDoubleBE = function readDoubleBE (offset, noAssert) {
  if (!noAssert) checkOffset(offset, 8, this.length)
  return ieee754.read(this, offset, false, 52, 8)
}

function checkInt (buf, value, offset, ext, max, min) {
  if (!Buffer.isBuffer(buf)) throw new TypeError('"buffer" argument must be a Buffer instance')
  if (value > max || value < min) throw new RangeError('"value" argument is out of bounds')
  if (offset + ext > buf.length) throw new RangeError('Index out of range')
}

Buffer.prototype.writeUIntLE = function writeUIntLE (value, offset, byteLength, noAssert) {
  value = +value
  offset = offset | 0
  byteLength = byteLength | 0
  if (!noAssert) {
    var maxBytes = Math.pow(2, 8 * byteLength) - 1
    checkInt(this, value, offset, byteLength, maxBytes, 0)
  }

  var mul = 1
  var i = 0
  this[offset] = value & 0xFF
  while (++i < byteLength && (mul *= 0x100)) {
    this[offset + i] = (value / mul) & 0xFF
  }

  return offset + byteLength
}

Buffer.prototype.writeUIntBE = function writeUIntBE (value, offset, byteLength, noAssert) {
  value = +value
  offset = offset | 0
  byteLength = byteLength | 0
  if (!noAssert) {
    var maxBytes = Math.pow(2, 8 * byteLength) - 1
    checkInt(this, value, offset, byteLength, maxBytes, 0)
  }

  var i = byteLength - 1
  var mul = 1
  this[offset + i] = value & 0xFF
  while (--i >= 0 && (mul *= 0x100)) {
    this[offset + i] = (value / mul) & 0xFF
  }

  return offset + byteLength
}

Buffer.prototype.writeUInt8 = function writeUInt8 (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 1, 0xff, 0)
  if (!Buffer.TYPED_ARRAY_SUPPORT) value = Math.floor(value)
  this[offset] = (value & 0xff)
  return offset + 1
}

function objectWriteUInt16 (buf, value, offset, littleEndian) {
  if (value < 0) value = 0xffff + value + 1
  for (var i = 0, j = Math.min(buf.length - offset, 2); i < j; ++i) {
    buf[offset + i] = (value & (0xff << (8 * (littleEndian ? i : 1 - i)))) >>>
      (littleEndian ? i : 1 - i) * 8
  }
}

Buffer.prototype.writeUInt16LE = function writeUInt16LE (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 2, 0xffff, 0)
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    this[offset] = (value & 0xff)
    this[offset + 1] = (value >>> 8)
  } else {
    objectWriteUInt16(this, value, offset, true)
  }
  return offset + 2
}

Buffer.prototype.writeUInt16BE = function writeUInt16BE (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 2, 0xffff, 0)
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    this[offset] = (value >>> 8)
    this[offset + 1] = (value & 0xff)
  } else {
    objectWriteUInt16(this, value, offset, false)
  }
  return offset + 2
}

function objectWriteUInt32 (buf, value, offset, littleEndian) {
  if (value < 0) value = 0xffffffff + value + 1
  for (var i = 0, j = Math.min(buf.length - offset, 4); i < j; ++i) {
    buf[offset + i] = (value >>> (littleEndian ? i : 3 - i) * 8) & 0xff
  }
}

Buffer.prototype.writeUInt32LE = function writeUInt32LE (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 4, 0xffffffff, 0)
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    this[offset + 3] = (value >>> 24)
    this[offset + 2] = (value >>> 16)
    this[offset + 1] = (value >>> 8)
    this[offset] = (value & 0xff)
  } else {
    objectWriteUInt32(this, value, offset, true)
  }
  return offset + 4
}

Buffer.prototype.writeUInt32BE = function writeUInt32BE (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 4, 0xffffffff, 0)
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    this[offset] = (value >>> 24)
    this[offset + 1] = (value >>> 16)
    this[offset + 2] = (value >>> 8)
    this[offset + 3] = (value & 0xff)
  } else {
    objectWriteUInt32(this, value, offset, false)
  }
  return offset + 4
}

Buffer.prototype.writeIntLE = function writeIntLE (value, offset, byteLength, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) {
    var limit = Math.pow(2, 8 * byteLength - 1)

    checkInt(this, value, offset, byteLength, limit - 1, -limit)
  }

  var i = 0
  var mul = 1
  var sub = 0
  this[offset] = value & 0xFF
  while (++i < byteLength && (mul *= 0x100)) {
    if (value < 0 && sub === 0 && this[offset + i - 1] !== 0) {
      sub = 1
    }
    this[offset + i] = ((value / mul) >> 0) - sub & 0xFF
  }

  return offset + byteLength
}

Buffer.prototype.writeIntBE = function writeIntBE (value, offset, byteLength, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) {
    var limit = Math.pow(2, 8 * byteLength - 1)

    checkInt(this, value, offset, byteLength, limit - 1, -limit)
  }

  var i = byteLength - 1
  var mul = 1
  var sub = 0
  this[offset + i] = value & 0xFF
  while (--i >= 0 && (mul *= 0x100)) {
    if (value < 0 && sub === 0 && this[offset + i + 1] !== 0) {
      sub = 1
    }
    this[offset + i] = ((value / mul) >> 0) - sub & 0xFF
  }

  return offset + byteLength
}

Buffer.prototype.writeInt8 = function writeInt8 (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 1, 0x7f, -0x80)
  if (!Buffer.TYPED_ARRAY_SUPPORT) value = Math.floor(value)
  if (value < 0) value = 0xff + value + 1
  this[offset] = (value & 0xff)
  return offset + 1
}

Buffer.prototype.writeInt16LE = function writeInt16LE (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 2, 0x7fff, -0x8000)
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    this[offset] = (value & 0xff)
    this[offset + 1] = (value >>> 8)
  } else {
    objectWriteUInt16(this, value, offset, true)
  }
  return offset + 2
}

Buffer.prototype.writeInt16BE = function writeInt16BE (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 2, 0x7fff, -0x8000)
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    this[offset] = (value >>> 8)
    this[offset + 1] = (value & 0xff)
  } else {
    objectWriteUInt16(this, value, offset, false)
  }
  return offset + 2
}

Buffer.prototype.writeInt32LE = function writeInt32LE (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 4, 0x7fffffff, -0x80000000)
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    this[offset] = (value & 0xff)
    this[offset + 1] = (value >>> 8)
    this[offset + 2] = (value >>> 16)
    this[offset + 3] = (value >>> 24)
  } else {
    objectWriteUInt32(this, value, offset, true)
  }
  return offset + 4
}

Buffer.prototype.writeInt32BE = function writeInt32BE (value, offset, noAssert) {
  value = +value
  offset = offset | 0
  if (!noAssert) checkInt(this, value, offset, 4, 0x7fffffff, -0x80000000)
  if (value < 0) value = 0xffffffff + value + 1
  if (Buffer.TYPED_ARRAY_SUPPORT) {
    this[offset] = (value >>> 24)
    this[offset + 1] = (value >>> 16)
    this[offset + 2] = (value >>> 8)
    this[offset + 3] = (value & 0xff)
  } else {
    objectWriteUInt32(this, value, offset, false)
  }
  return offset + 4
}

function checkIEEE754 (buf, value, offset, ext, max, min) {
  if (offset + ext > buf.length) throw new RangeError('Index out of range')
  if (offset < 0) throw new RangeError('Index out of range')
}

function writeFloat (buf, value, offset, littleEndian, noAssert) {
  if (!noAssert) {
    checkIEEE754(buf, value, offset, 4, 3.4028234663852886e+38, -3.4028234663852886e+38)
  }
  ieee754.write(buf, value, offset, littleEndian, 23, 4)
  return offset + 4
}

Buffer.prototype.writeFloatLE = function writeFloatLE (value, offset, noAssert) {
  return writeFloat(this, value, offset, true, noAssert)
}

Buffer.prototype.writeFloatBE = function writeFloatBE (value, offset, noAssert) {
  return writeFloat(this, value, offset, false, noAssert)
}

function writeDouble (buf, value, offset, littleEndian, noAssert) {
  if (!noAssert) {
    checkIEEE754(buf, value, offset, 8, 1.7976931348623157E+308, -1.7976931348623157E+308)
  }
  ieee754.write(buf, value, offset, littleEndian, 52, 8)
  return offset + 8
}

Buffer.prototype.writeDoubleLE = function writeDoubleLE (value, offset, noAssert) {
  return writeDouble(this, value, offset, true, noAssert)
}

Buffer.prototype.writeDoubleBE = function writeDoubleBE (value, offset, noAssert) {
  return writeDouble(this, value, offset, false, noAssert)
}

// copy(targetBuffer, targetStart=0, sourceStart=0, sourceEnd=buffer.length)
Buffer.prototype.copy = function copy (target, targetStart, start, end) {
  if (!start) start = 0
  if (!end && end !== 0) end = this.length
  if (targetStart >= target.length) targetStart = target.length
  if (!targetStart) targetStart = 0
  if (end > 0 && end < start) end = start

  // Copy 0 bytes; we're done
  if (end === start) return 0
  if (target.length === 0 || this.length === 0) return 0

  // Fatal error conditions
  if (targetStart < 0) {
    throw new RangeError('targetStart out of bounds')
  }
  if (start < 0 || start >= this.length) throw new RangeError('sourceStart out of bounds')
  if (end < 0) throw new RangeError('sourceEnd out of bounds')

  // Are we oob?
  if (end > this.length) end = this.length
  if (target.length - targetStart < end - start) {
    end = target.length - targetStart + start
  }

  var len = end - start
  var i

  if (this === target && start < targetStart && targetStart < end) {
    // descending copy from end
    for (i = len - 1; i >= 0; --i) {
      target[i + targetStart] = this[i + start]
    }
  } else if (len < 1000 || !Buffer.TYPED_ARRAY_SUPPORT) {
    // ascending copy from start
    for (i = 0; i < len; ++i) {
      target[i + targetStart] = this[i + start]
    }
  } else {
    Uint8Array.prototype.set.call(
      target,
      this.subarray(start, start + len),
      targetStart
    )
  }

  return len
}

// Usage:
//    buffer.fill(number[, offset[, end]])
//    buffer.fill(buffer[, offset[, end]])
//    buffer.fill(string[, offset[, end]][, encoding])
Buffer.prototype.fill = function fill (val, start, end, encoding) {
  // Handle string cases:
  if (typeof val === 'string') {
    if (typeof start === 'string') {
      encoding = start
      start = 0
      end = this.length
    } else if (typeof end === 'string') {
      encoding = end
      end = this.length
    }
    if (val.length === 1) {
      var code = val.charCodeAt(0)
      if (code < 256) {
        val = code
      }
    }
    if (encoding !== undefined && typeof encoding !== 'string') {
      throw new TypeError('encoding must be a string')
    }
    if (typeof encoding === 'string' && !Buffer.isEncoding(encoding)) {
      throw new TypeError('Unknown encoding: ' + encoding)
    }
  } else if (typeof val === 'number') {
    val = val & 255
  }

  // Invalid ranges are not set to a default, so can range check early.
  if (start < 0 || this.length < start || this.length < end) {
    throw new RangeError('Out of range index')
  }

  if (end <= start) {
    return this
  }

  start = start >>> 0
  end = end === undefined ? this.length : end >>> 0

  if (!val) val = 0

  var i
  if (typeof val === 'number') {
    for (i = start; i < end; ++i) {
      this[i] = val
    }
  } else {
    var bytes = Buffer.isBuffer(val)
      ? val
      : utf8ToBytes(new Buffer(val, encoding).toString())
    var len = bytes.length
    for (i = 0; i < end - start; ++i) {
      this[i + start] = bytes[i % len]
    }
  }

  return this
}

// HELPER FUNCTIONS
// ================

var INVALID_BASE64_RE = /[^+\/0-9A-Za-z-_]/g

function base64clean (str) {
  // Node strips out invalid characters like \n and \t from the string, base64-js does not
  str = stringtrim(str).replace(INVALID_BASE64_RE, '')
  // Node converts strings with length < 2 to ''
  if (str.length < 2) return ''
  // Node allows for non-padded base64 strings (missing trailing ===), base64-js does not
  while (str.length % 4 !== 0) {
    str = str + '='
  }
  return str
}

function stringtrim (str) {
  if (str.trim) return str.trim()
  return str.replace(/^\s+|\s+$/g, '')
}

function toHex (n) {
  if (n < 16) return '0' + n.toString(16)
  return n.toString(16)
}

function utf8ToBytes (string, units) {
  units = units || Infinity
  var codePoint
  var length = string.length
  var leadSurrogate = null
  var bytes = []

  for (var i = 0; i < length; ++i) {
    codePoint = string.charCodeAt(i)

    // is surrogate component
    if (codePoint > 0xD7FF && codePoint < 0xE000) {
      // last char was a lead
      if (!leadSurrogate) {
        // no lead yet
        if (codePoint > 0xDBFF) {
          // unexpected trail
          if ((units -= 3) > -1) bytes.push(0xEF, 0xBF, 0xBD)
          continue
        } else if (i + 1 === length) {
          // unpaired lead
          if ((units -= 3) > -1) bytes.push(0xEF, 0xBF, 0xBD)
          continue
        }

        // valid lead
        leadSurrogate = codePoint

        continue
      }

      // 2 leads in a row
      if (codePoint < 0xDC00) {
        if ((units -= 3) > -1) bytes.push(0xEF, 0xBF, 0xBD)
        leadSurrogate = codePoint
        continue
      }

      // valid surrogate pair
      codePoint = (leadSurrogate - 0xD800 << 10 | codePoint - 0xDC00) + 0x10000
    } else if (leadSurrogate) {
      // valid bmp char, but last char was a lead
      if ((units -= 3) > -1) bytes.push(0xEF, 0xBF, 0xBD)
    }

    leadSurrogate = null

    // encode utf8
    if (codePoint < 0x80) {
      if ((units -= 1) < 0) break
      bytes.push(codePoint)
    } else if (codePoint < 0x800) {
      if ((units -= 2) < 0) break
      bytes.push(
        codePoint >> 0x6 | 0xC0,
        codePoint & 0x3F | 0x80
      )
    } else if (codePoint < 0x10000) {
      if ((units -= 3) < 0) break
      bytes.push(
        codePoint >> 0xC | 0xE0,
        codePoint >> 0x6 & 0x3F | 0x80,
        codePoint & 0x3F | 0x80
      )
    } else if (codePoint < 0x110000) {
      if ((units -= 4) < 0) break
      bytes.push(
        codePoint >> 0x12 | 0xF0,
        codePoint >> 0xC & 0x3F | 0x80,
        codePoint >> 0x6 & 0x3F | 0x80,
        codePoint & 0x3F | 0x80
      )
    } else {
      throw new Error('Invalid code point')
    }
  }

  return bytes
}

function asciiToBytes (str) {
  var byteArray = []
  for (var i = 0; i < str.length; ++i) {
    // Node's code seems to be doing this and not & 0x7F..
    byteArray.push(str.charCodeAt(i) & 0xFF)
  }
  return byteArray
}

function utf16leToBytes (str, units) {
  var c, hi, lo
  var byteArray = []
  for (var i = 0; i < str.length; ++i) {
    if ((units -= 2) < 0) break

    c = str.charCodeAt(i)
    hi = c >> 8
    lo = c % 256
    byteArray.push(lo)
    byteArray.push(hi)
  }

  return byteArray
}

function base64ToBytes (str) {
  return base64.toByteArray(base64clean(str))
}

function blitBuffer (src, dst, offset, length) {
  for (var i = 0; i < length; ++i) {
    if ((i + offset >= dst.length) || (i >= src.length)) break
    dst[i + offset] = src[i]
  }
  return i
}

function isnan (val) {
  return val !== val // eslint-disable-line no-self-compare
}

/* WEBPACK VAR INJECTION */}.call(exports, __webpack_require__(26)))

/***/ }),
/* 20 */
/***/ (function(module, exports, __webpack_require__) {

exports = module.exports = __webpack_require__(21)(undefined);
// imports


// module
exports.push([module.i, "/* Goals:\r\n    - Header [full width, fixed height]\r\n    - Left and Right sides [fixed width, full height]\r\n    - Middle content [remaining width and height, scrollable]\r\n    - Footer [full width, fixed height]\r\n\r\n   Requirements:\r\n    - Ancestors of the root flow element *all* must have \"width: 100%; height 100%\" in order to fill viewport height.\r\n    - All ancestors of the scrolling element must have \"overflow: hidden\" to coerce the main element into scrolling [Edge]\r\n*/\r\n\r\n/* --- Overall Layout --- */\r\n\r\n/* Make all root elements full height/width, unpadded, and non-scrolling */\r\nhtml, body, .viewport {\r\n    width: 100%;\r\n    height: 100%;\r\n    padding: 0;\r\n    margin: 0;\r\n\r\n    overflow: hidden;\r\n}\r\n\r\n/* Make root element flow contents as columns (fill the height) */\r\n.viewport {\r\n    display: flex;\r\n    flex-flow: column;\r\n}\r\n\r\n/* Make the header fixed height */\r\n.header {\r\n    height: 64px;\r\n    min-height: 64px;\r\n\r\n    display: flex;\r\n    flex-flow: row;\r\n}\r\n\r\n/* Make the search bar fill header width. Make contents also fill inner width */\r\n.searchBar {\r\n    flex: 1;\r\n\r\n    display: flex;\r\n    flex-flow: row;\r\n\r\n    position: relative;\r\n}\r\n\r\n/* Make the middle area resize and full width.\r\n    - Make contents of it flow across.\r\n    - Ensure it's non-scrollable so the content will be scrollable\r\n*/\r\n.middle {\r\n    flex: 1 100%;\r\n    background: #F5F5F5;\r\n\r\n    display: flex;\r\n    flex-flow: row;\r\n\r\n    overflow: hidden;\r\n}\r\n\r\n/* Make the footer fixed height */\r\n.footer {\r\n    background: #999;\r\n    height: 32px;\r\n    min-height: 32px;\r\n}\r\n\r\n/* Make the left bar fixed width */\r\n.mode {\r\n    width: 53px;\r\n}\r\n\r\n/* Make the center [between left and right] full width, contents share height */\r\n.center {\r\n    flex: 1 100%;\r\n\r\n    display: flex;\r\n    flex-flow: column;\r\n\r\n    overflow: hidden;\r\n}\r\n\r\n/* Make query stats fixed height */\r\n.queryStats {\r\n    min-height: 20px;\r\n    max-height: 20px;\r\n}\r\n\r\n/* Make the right bar fixed width */\r\n.right {\r\n    background: #999;\r\n    padding: 8px;\r\n    width: 100px;\r\n}\r\n\r\n/* Make a marker for the panel which is to be scrollable.\r\n   It must also be flex in order to take up the correct height.\r\n   All ancestors must be overflow: hidden for it to scroll properly.\r\n*/\r\n.scrollable {\r\n    flex: 1 100%;\r\n    overflow: auto;\r\n}\r\n\r\n/* --- Basic Look and Feel --- */\r\n\r\n/* Set default font, font-size, and background (light grey) */\r\nbody {\r\n    font-family: \"Segoe UI\", \"Segoe WP\", Tahoma, Arial, sans-serif;\r\n    font-size: 14px;\r\n    background: #f5f5f5;\r\n    color: #444444;\r\n}\r\n\r\n/* Add a 'light' font option, and use for H1-H6 */\r\n.font-light, h1, h2, h3, h4, h5, h6 {\r\n    font-family: \"Segoe UI Light\", \"Segoe UI\", \"Segoe WP\", Tahoma, Arial, sans-serif;\r\n}\r\n\r\n/* Add an 'example' font which is a fixed-width font */\r\n.font-example {\r\n    font-family: Consolas, 'Lucida Console', 'DejaVu Sans Mono', monospace;\r\n    font-weight: bold;\r\n}\r\n\r\n/* Set default font in controls */\r\ninput,\r\ninput[type=submit],\r\nselect,\r\nbutton,\r\ntextarea {\r\n    font-family: \"Segoe UI\", \"Segoe WP\", Tahoma, Arial, sans-serif;\r\n    font-weight: 600;\r\n}\r\n\r\n/* Disable the blue border on selected inputs */\r\ntextarea:focus,\r\ninput:focus,\r\nselect:focus,\r\ndiv:focus {\r\n    outline: 0;\r\n}\r\n\r\n/* Add a style to request indentation */\r\n.indent {\r\n    margin-left: 1.5em;\r\n}\r\n\r\n/* Make table backgrounds white and harmonize boreders */\r\ntable {\r\n    border-collapse: collapse;\r\n    background: white;\r\n}\r\n\r\n/* Give table cells decent padding, silver borders, and put text at the top */\r\ntable td, table th {\r\n    padding: 4px 6px;\r\n    border: 1px solid #CCCCCC;\r\n    vertical-align: top;\r\n    text-align: left;\r\n}\r\n\r\n/* Make the first row (headings) grey on light grey */\r\ntable thead th, table thead td {\r\n    color: #777777;\r\n    background-color: #EEEEEE;\r\n}\r\n\r\n/* Make a 'smallTable' option */\r\n.smallTable {\r\n    font-size: 80%;\r\n}\r\n\r\n/* Smalltable heading rows are borderless, narrowly padded, black on surrounding background */\r\n.smallTable thead td {\r\n    border: none;\r\n    padding: 0px 6px;\r\n\r\n    background-color: transparent;\r\n    color: black;\r\n}\r\n\r\n/* Smalltable cells are much more narrowly padded */\r\n.smallTable td {\r\n    padding: 1px 4px;\r\n}\r\n\r\n/* Highlight query matching text with yellow background highlight */\r\n.h {\r\n    background: #ffeb0a;\r\n}\r\n\r\n/* Mark error content with light red highlight */\r\n.error {\r\n    background: #f67c87;\r\n}\r\n\r\n/* Use the Segoe UI Symbol fonts with standing size and spacing */\r\n.icon {\r\n    display: inline-block;\r\n    font-family: \"Segoe UI Symbol\";\r\n    font-size: 80%;\r\n    padding: 1px;\r\n    margin: 0px 4px 0px 0px;\r\n    width: 1.5em;\r\n    text-align: center;\r\n}\r\n\r\n/* Column Controls (add/remove column/row) grey and put them on the right side */\r\n.icon-column-heading {\r\n    color: #BBB;\r\n    cursor: pointer;\r\n    float: right;\r\n    padding: 0px 2px;\r\n}\r\n\r\n/* Column controls which are links shouldn't be blue when visited */\r\n.icon-column-heading:visited {\r\n    color: #BBB;\r\n}\r\n\r\n/* Make column controls highlight slightly on mouseover. Don't underline ones which are links */\r\n.icon-column-heading:hover {\r\n    color: #999;\r\n\r\n    text-decoration: none;\r\n}\r\n\r\n/* Indicate things are clickable with the pointer (hand) mouse cursor and a hover color */\r\n.clickable {\r\n    cursor: pointer;\r\n}\r\n\r\n.clickable:hover {\r\n    color: #0aa2dc;\r\n}\r\n\r\n/* Customize link color and hover color */\r\na,\r\na:visited {\r\n    color: #0c6dce;\r\n    cursor: pointer;\r\n    text-decoration: none;\r\n}\r\n\r\na:hover,\r\na:visited:hover {\r\n    color: #3393f3;\r\n    text-decoration: none;\r\n}\r\n\r\n/* --- Header Styles --- */\r\n\r\n/* Overall header: 64px, across */\r\n.header {\r\n    height: 64px;\r\n\r\n    display: flex;\r\n    flex-flow: row;\r\n}\r\n\r\n/* Top Left Title - white, middle, bigger */\r\n.title {\r\n    cursor: default;\r\n    padding: 16px 8px;\r\n    vertical-align: middle;\r\n    font-size: 18pt;\r\n}\r\n\r\n/* The title is a link; fix it to look like plain text */\r\n.header .title a {\r\n    color: white;\r\n    font-weight: normal;\r\n}\r\n\r\n/* Make the search box large, fill the space, well padded, slightly grey */\r\n.header .searchBar input {\r\n    font-size: 16pt;\r\n    box-sizing: border-box;\r\n    border: none;\r\n    margin: 8px;\r\n    margin-right: 0px;\r\n    width: 100%;\r\n    padding: 8px;\r\n    color: #666666;\r\n    background: #F7F7F7;\r\n}\r\n\r\n/* Make the search icon (magnifying glass) the same as the search box and right next to it */\r\n.header .searchBar .searchIcon {\r\n    font-size: 16pt;\r\n    color: #666666;\r\n    background: #F7F7F7;\r\n    padding: 8px;\r\n    margin: 8px;\r\n    margin-left: 0px;\r\n}\r\n\r\n/* Make the buttons (top right) large, white on dark green */\r\n.buttons a {\r\n    font-size: 20px;\r\n    display: inline-block;\r\n    color: white;\r\n    height: 48px;\r\n    width: 48px;\r\n    margin: 8px 8px 0px 0px;\r\n}\r\n\r\n.buttons a:hover {\r\n    color: #999;\r\n    text-decoration: none;\r\n}\r\n\r\n/* Center the icons within the buttons */\r\n.buttons a i {\r\n    margin: 12px;\r\n}\r\n\r\n/* --- Query Stats Bar --- */\r\n\r\n/* Make the QueryStats grey and hide overflowing content */\r\n.queryStats {\r\n    padding: 6px;\r\n    background: #E3E3E3;\r\n    \r\n    white-space: nowrap;\r\n    text-overflow: ellipsis;\r\n    overflow: hidden;\r\n}\r\n\r\n/* Make the per-table buttons dark grey and spaced out */\r\n.queryStats .statsForTable {\r\n    border: 1px solid #999999;\r\n    padding: 2px 4px;\r\n    margin-right: 8px;\r\n}\r\n\r\n/* Make the selected table white */\r\n.queryStats .statsForTable.current {\r\n    background-color: white;\r\n}\r\n\r\n/* Make the per-table counts bold and add spacing before the table name */\r\n.queryStats .statsForTable .countValue {\r\n    padding-right: 4px;\r\n    font-weight: bold;\r\n}\r\n\r\n/* Space out the QueryStats buttons a bit */\r\n.queryStats a {\r\n    padding-left: 6px;\r\n}\r\n\r\n\r\n/* --- Mode Bar [left side] --- */\r\n\r\n/* Make mode bar white on darker green, sized for icons, animated */\r\n.mode {\r\n    color: white;\r\n    width: 53px;\r\n    overflow: hidden;\r\n}\r\n\r\n/* Make the currently selected mode grey on light grey (match main panel background) */\r\n.mode .selected {\r\n    color: #666666;\r\n    background: #F5F5F5;\r\n}\r\n\r\n/* Make the currently selected mode light up on hover but keep the matching background */\r\n.mode .selected:hover {\r\n    color: #222222;\r\n    background: #F5F5F5;\r\n}\r\n\r\n/* Make mode bar modes large icons */\r\n.mode a {\r\n    display: block;\r\n    width: 200px;\r\n    padding: 12px;\r\n\r\n    font-size: 12pt;\r\n    color: #EEEEEE;\r\n    text-decoration: none;\r\n\r\n    transition: all 0.2s ease-in;\r\n    transition-delay: 0s;\r\n}\r\n\r\n/* Make the mode links turn darker grey on hover */\r\n.mode a:hover {\r\n    color: #999;\r\n}\r\n\r\n/* Make mode bar icons large and spaced */\r\n.mode a i {\r\n    font-size: 22px;\r\n    margin-right: 12px;\r\n}\r\n\r\n\r\n/* --- Splash Content UX --- */\r\n\r\n.splash {\r\n    animation: splash-fade ease-in 0.5s;\r\n    font-size: 28pt;\r\n    color: #555555;\r\n    justify-content: center;\r\n    align-items: center;\r\n    text-align: center;\r\n    min-height: 200px;\r\n    font-weight: 300;\r\n    padding: 24px;\r\n}\r\n\r\n.smaller {\r\n    margin-top: 4px;\r\n    font-size: 70%;\r\n}\r\n\r\n/* --- Help Content UX --- */\r\n\r\n/* Help Panel: Full size with 24px around, scrollable, grey border, padded */\r\n.helpContent {\r\n    flex: 1 100%;\r\n    overflow: auto;\r\n    margin: 24px 36px;\r\n    \r\n    border: 1px solid #999999;\r\n    background-color: white;\r\n\r\n    padding: 12px;\r\n}\r\n\r\n/* Make the first part bigger */\r\n.helpIntro {\r\n    font-size: 150%;\r\n}\r\n\r\n/* Make section headings nicely spaced */\r\n.helpContent h2 {\r\n    margin-top: 1em;\r\n    margin-bottom: 4px;\r\n    font-weight: 600;\r\n}\r\n\r\n.exampleBox {\r\n    margin-bottom: 1.5em;\r\n}\r\n\r\n\r\n/* --- Error / Access Denied UX --- */\r\n\r\n/* Move the overall error page down and limit the width */\r\n.errorPage {\r\n    padding: 48px 24px;\r\n}\r\n\r\n/* Make the heading large white text on red, centered, left-aligned. */\r\n/* Make the status code smaller, right-aligned. */\r\n.errorPage h1 {\r\n    color: white;\r\n    background: #e81123;\r\n    padding: 12px;\r\n    margin: auto;\r\n    width: 50%;\r\n    border: 2px solid #e81123;\r\n    display: flex;\r\n    align-items: center;\r\n    flex-direction: row;\r\n    justify-content: space-between;\r\n}\r\n\r\n.errorPage h1 > .errorTitle {\r\n    font-size: 24pt;\r\n}\r\n\r\n.errorPage h1 > .errorStatus {\r\n    font-size: 16px;\r\n}\r\n\r\n/* Make error content centered, red border, spaced */\r\n.errorPage article {\r\n    margin: 0 auto;\r\n    width: 50%;\r\n    border: 2px solid #e81123;\r\n    padding: 12px;\r\n    font-size: 12pt;\r\n}\r\n\r\n\r\n/* --- Listing UX --- */\r\n\r\n/* Make the result listing take the full available width */\r\n.resultTable {\r\n    width: 100%;\r\n}\r\n\r\n/* Show that table rows are all clickable (to open details) */\r\n.resultTable tr {\r\n    cursor: pointer;\r\n}\r\n\r\n/* Make listing table slightly more padded, ellipse on overflow */\r\n.resultTable td {\r\n    padding: 6px;\r\n\r\n    min-width: 120px;\r\n    max-width: 400px;\r\n\r\n    white-space: nowrap;\r\n    text-overflow: ellipsis;\r\n    overflow: hidden;\r\n}\r\n\r\n/* Make the selected listing row white on color, bold */\r\n.resultTable .selected {\r\n    color: white;\r\n    font-weight: bold;\r\n}\r\n\r\n\r\n/* Make the AddColumnList absolute positioned, on top, scrollable, black on white */\r\n/* ISSUE: Unsure how to ensure it isn't too tall when very little height */\r\n.add-list {\r\n    z-index: 10;\r\n    position: absolute;\r\n    background: white;\r\n    color: black;\r\n    border: 1px solid #888;\r\n    top: 0em;\r\n    right: -1em;\r\n    width: 12em;\r\n    padding: 2px;\r\n    overflow: auto;\r\n    max-height: 24em;\r\n}\r\n\r\n/* Make Filter textbox in AddColumnList spaced from edges and narrow enough not to be covered by scrollbar */\r\n.add-list input {\r\n    width: 10.75em;\r\n    margin: 2px;\r\n    margin-bottom: 4px;\r\n}\r\n\r\n/* Make AddColumnList hovered on items look selected */\r\n.add-list div:hover {\r\n    color: white;\r\n    background-color: #3d56af;\r\n}\r\n\r\n/* Make the AddColumnList programmatically selected item look selected */\r\n.add-list-selected {\r\n    color: white;\r\n    background-color: #3d56af;\r\n}\r\n\r\n/* --- Details UX --- */\r\n\r\n/* Make the details panel white, padded */\r\n.details {\r\n    background-color: white;\r\n    padding: 12px;\r\n}\r\n\r\n/* Make commands float on the right side */\r\n.details .commands {\r\n    float: right;\r\n    font-size: 133%;\r\n}\r\n\r\n/* Space the commands out and make sure they're clickable */\r\n.details .commands * {\r\n    margin: 0px 4px;\r\n    cursor: pointer;\r\n}\r\n\r\n/* Make the title large and share a line */\r\n.details .detailTitle {\r\n    display: inline-block;\r\n    font-size: 28px;\r\n}\r\n\r\n/* Make the detail boxes columns */\r\n.details .overviewBoxes {\r\n    margin-top: 16px;\r\n    display: flex;\r\n    flex-direction: row;\r\n}\r\n\r\n.details .box {\r\n    flex: 1;\r\n}\r\n\r\n/* Space the detail links from the text slightly */\r\n.detailLink {\r\n    margin-left: 4px;\r\n}\r\n\r\n/* Details Table values - limit the height and make scrollable */\r\n.detailsValue {\r\n    max-height: 6.75em;\r\n    min-width: 300px;\r\n    overflow: auto;\r\n}\r\n\r\n\r\n/* --- Grid UX --- */\r\n\r\n/* Add padding around grid */\r\n.grid {\r\n    padding: 16px;\r\n}\r\n\r\n/* Make Grid text larger, semi-bold */\r\n.grid * {\r\n    font-size: 16px;\r\n    font-family: \"Segoe UI Semibold\", \"Segoe UI\", \"Segoe WP\", Tahoma, Arial, sans-serif;\r\n}\r\n\r\n/* Make Grid cells animated and unpadded */\r\n.grid td {\r\n    padding: 0px;\r\n}\r\n\r\n/* Make cell content padded, single-line, and standard width. [Not the cells themselves, so background bars are full size] */\r\n.grid table td div {\r\n    padding: 8px 8px;\r\n\r\n    white-space: nowrap;\r\n    text-overflow: ellipsis;\r\n    overflow: hidden;\r\n\r\n    transition: all 0.15s ease-in;\r\n    transition-delay: 0s;\r\n\r\n    min-width: 150px;\r\n    max-width: 150px;\r\n}\r\n\r\n/* Ensure Grid cells don't grow wider than the values [excluding column and row headings] */\r\n.grid table tbody td.c {\r\n    width: 166px;\r\n}\r\n\r\n/* Row and Column heading cells have controls *outside* the div [to ensure they're clickable], so the cells need the padding, but less on the right */\r\n.grid td.grid-heading {\r\n    padding: 8px 2px 8px 8px;\r\n}\r\n\r\n/* Make Row and Column heading divs (with the text only) small enough that the controls don't wrap */\r\n.grid td.grid-heading div {\r\n    padding: 0px;\r\n\r\n    min-width: 125px;\r\n    max-width: 125px;\r\n\r\n    white-space: nowrap;\r\n    text-overflow: ellipsis;\r\n    overflow: hidden;\r\n}\r\n\r\n/* Allow *Row* heading divs to be wider */\r\n.grid tr td.grid-heading:first-child div {\r\n    width: auto;\r\n    min-width: 150px;\r\n    max-width: 300px;\r\n}\r\n\r\n/* Allow *Column* headings to wrap */\r\n.grid thead td.grid-heading div {\r\n    min-width: 125px;\r\n    max-width: 125px;\r\n\r\n    white-space: normal;\r\n}\r\n\r\n/* Grid column headings are wider when being edited, less padded, not animated. */\r\n.grid table td.grid-heading.editing div {\r\n    min-width: 300px;\r\n    max-width: 300px;\r\n\r\n    transition: none;\r\n}\r\n\r\n/* Editing Textboxes are narrow enough not to wrap with icons and are not bumped right against icons or each other */\r\n.grid table .editing input {\r\n    width: 260px;\r\n    margin-left: 4px;\r\n    margin-bottom: 4px;\r\n}\r\n\r\n/* Make Grid values very dark blue */\r\n.grid table td a {\r\n    color: #1E3CA9; \r\n}\r\n\r\n/* Make Grid values lighten and underline on hover */\r\n.grid table td a:hover {\r\n    color: #3D56AF;\r\n    text-decoration: underline;\r\n}\r\n\r\n/* Put options panel a line below the Grid table */\r\n.grid .options {\r\n    padding-top: 24px;\r\n}\r\n\r\n/* Make the Pie visualization small and off to the left */\r\n.pie {\r\n    width: 20px;\r\n    height: 20px;\r\n    margin-right: 8px;\r\n    float: left;\r\n}\r\n\r\n/* --- Left/Right panel resizer --- */\r\n\r\n/* Make the resizer take the full container space, grey, on top of other content */\r\n.resizer {\r\n    box-sizing: border-box;\r\n    opacity: 0.2;\r\n    background: #000;\r\n    z-index: 1;\r\n    background-clip: padding-box;\r\n}\r\n\r\n/* Make the resizer visibility animate */\r\n.resizer:hover {\r\n    transition: all 1s ease;\r\n}\r\n\r\n/* Make the resizer 11px wide but appear only 1px wide, with the left-right resize cursor */\r\n.resizer.vertical {\r\n    width: 11px;\r\n    margin: 0px -5px;\r\n    border-left: 5px solid rgba(255, 255, 255, 0);\r\n    border-right: 5px solid rgba(255, 255, 255, 0);\r\n    cursor: col-resize;\r\n}\r\n\r\n/* Make the full resizer target area partially visible on hover */\r\n.resizer.vertical:hover {\r\n    border-left: 5px solid rgba(0, 0, 0, 0.5);\r\n    border-right: 5px solid rgba(0, 0, 0, 0.5);\r\n}\r\n\r\n/* Make the panes split on width via flex layout */\r\n.splitpane {\r\n    display: flex;\r\n    flex: 1 1 0%;\r\n    flex-direction: row;\r\n    outline: none;\r\n\r\n    overflow: hidden;\r\n}\r\n\r\n/* Make the panes take up the space and use flex inside, with no scrolling. */\r\n.pane {\r\n    flex: 1 1 0%;\r\n\r\n    display: flex;\r\n    overflow: hidden;\r\n}\r\n\r\n.suggestions {\r\n    position: absolute;\r\n    top: 58px; /* Todo: Don't hard code the top. */\r\n    left: 8px;\r\n    background-color: white;\r\n    box-shadow: 0 3px 8px 0 rgba(0,0,0,0.2), 0 0 0 1px rgba(0,0,0,0.08);\r\n    z-index: 1;\r\n    max-height: 70vh;\r\n    min-width: 150px;\r\n    overflow-y: scroll;\r\n}\r\n\r\n.suggestions .suggestion {\r\n    font-size: 12pt;\r\n    font-weight: 600;\r\n    padding: 8px;\r\n    cursor: pointer;\r\n    box-sizing: border-box;\r\n    white-space: nowrap;\r\n}\r\n\r\n.suggestions .suggestion:hover {\r\n     background-color: rgba(0, 0, 0, 0.02); \r\n}\r\n\r\n.suggestions .suggestion-sel {\r\n    background-color: rgba(180, 210, 255, 0.5);\r\n}\r\n", ""]);

// exports


/***/ }),
/* 21 */
/***/ (function(module, exports, __webpack_require__) {

/* WEBPACK VAR INJECTION */(function(Buffer) {/*
	MIT License http://www.opensource.org/licenses/mit-license.php
	Author Tobias Koppers @sokra
*/
// css base code, injected by the css-loader
module.exports = function(useSourceMap) {
	var list = [];

	// return the list of modules as css string
	list.toString = function toString() {
		return this.map(function (item) {
			var content = cssWithMappingToString(item, useSourceMap);
			if(item[2]) {
				return "@media " + item[2] + "{" + content + "}";
			} else {
				return content;
			}
		}).join("");
	};

	// import a list of modules into the list
	list.i = function(modules, mediaQuery) {
		if(typeof modules === "string")
			modules = [[null, modules, ""]];
		var alreadyImportedModules = {};
		for(var i = 0; i < this.length; i++) {
			var id = this[i][0];
			if(typeof id === "number")
				alreadyImportedModules[id] = true;
		}
		for(i = 0; i < modules.length; i++) {
			var item = modules[i];
			// skip already imported module
			// this implementation is not 100% perfect for weird media query combinations
			//  when a module is imported multiple times with different media queries.
			//  I hope this will never occur (Hey this way we have smaller bundles)
			if(typeof item[0] !== "number" || !alreadyImportedModules[item[0]]) {
				if(mediaQuery && !item[2]) {
					item[2] = mediaQuery;
				} else if(mediaQuery) {
					item[2] = "(" + item[2] + ") and (" + mediaQuery + ")";
				}
				list.push(item);
			}
		}
	};
	return list;
};

function cssWithMappingToString(item, useSourceMap) {
	var content = item[1] || '';
	var cssMapping = item[3];
	if (!cssMapping) {
		return content;
	}

	if (useSourceMap) {
		var sourceMapping = toComment(cssMapping);
		var sourceURLs = cssMapping.sources.map(function (source) {
			return '/*# sourceURL=' + cssMapping.sourceRoot + source + ' */'
		});

		return [content].concat(sourceURLs).concat([sourceMapping]).join('\n');
	}

	return [content].join('\n');
}

// Adapted from convert-source-map (MIT)
function toComment(sourceMap) {
  var base64 = new Buffer(JSON.stringify(sourceMap)).toString('base64');
  var data = 'sourceMappingURL=data:application/json;charset=utf-8;base64,' + base64;

  return '/*# ' + data + ' */';
}

/* WEBPACK VAR INJECTION */}.call(exports, __webpack_require__(19).Buffer))

/***/ }),
/* 22 */
/***/ (function(module, exports) {

exports.read = function (buffer, offset, isLE, mLen, nBytes) {
  var e, m
  var eLen = nBytes * 8 - mLen - 1
  var eMax = (1 << eLen) - 1
  var eBias = eMax >> 1
  var nBits = -7
  var i = isLE ? (nBytes - 1) : 0
  var d = isLE ? -1 : 1
  var s = buffer[offset + i]

  i += d

  e = s & ((1 << (-nBits)) - 1)
  s >>= (-nBits)
  nBits += eLen
  for (; nBits > 0; e = e * 256 + buffer[offset + i], i += d, nBits -= 8) {}

  m = e & ((1 << (-nBits)) - 1)
  e >>= (-nBits)
  nBits += mLen
  for (; nBits > 0; m = m * 256 + buffer[offset + i], i += d, nBits -= 8) {}

  if (e === 0) {
    e = 1 - eBias
  } else if (e === eMax) {
    return m ? NaN : ((s ? -1 : 1) * Infinity)
  } else {
    m = m + Math.pow(2, mLen)
    e = e - eBias
  }
  return (s ? -1 : 1) * m * Math.pow(2, e - mLen)
}

exports.write = function (buffer, value, offset, isLE, mLen, nBytes) {
  var e, m, c
  var eLen = nBytes * 8 - mLen - 1
  var eMax = (1 << eLen) - 1
  var eBias = eMax >> 1
  var rt = (mLen === 23 ? Math.pow(2, -24) - Math.pow(2, -77) : 0)
  var i = isLE ? 0 : (nBytes - 1)
  var d = isLE ? 1 : -1
  var s = value < 0 || (value === 0 && 1 / value < 0) ? 1 : 0

  value = Math.abs(value)

  if (isNaN(value) || value === Infinity) {
    m = isNaN(value) ? 1 : 0
    e = eMax
  } else {
    e = Math.floor(Math.log(value) / Math.LN2)
    if (value * (c = Math.pow(2, -e)) < 1) {
      e--
      c *= 2
    }
    if (e + eBias >= 1) {
      value += rt / c
    } else {
      value += rt * Math.pow(2, 1 - eBias)
    }
    if (value * c >= 2) {
      e++
      c /= 2
    }

    if (e + eBias >= eMax) {
      m = 0
      e = eMax
    } else if (e + eBias >= 1) {
      m = (value * c - 1) * Math.pow(2, mLen)
      e = e + eBias
    } else {
      m = value * Math.pow(2, eBias - 1) * Math.pow(2, mLen)
      e = 0
    }
  }

  for (; mLen >= 8; buffer[offset + i] = m & 0xff, i += d, m /= 256, mLen -= 8) {}

  e = (e << mLen) | m
  eLen += mLen
  for (; eLen > 0; buffer[offset + i] = e & 0xff, i += d, e /= 256, eLen -= 8) {}

  buffer[offset + i - d] |= s * 128
}


/***/ }),
/* 23 */
/***/ (function(module, exports) {

var toString = {}.toString;

module.exports = Array.isArray || function (arr) {
  return toString.call(arr) == '[object Array]';
};


/***/ }),
/* 24 */
/***/ (function(module, exports, __webpack_require__) {

/*
	MIT License http://www.opensource.org/licenses/mit-license.php
	Author Tobias Koppers @sokra
*/
var stylesInDom = {},
	memoize = function(fn) {
		var memo;
		return function () {
			if (typeof memo === "undefined") memo = fn.apply(this, arguments);
			return memo;
		};
	},
	isOldIE = memoize(function() {
		return /msie [6-9]\b/.test(self.navigator.userAgent.toLowerCase());
	}),
	getElement = (function(fn) {
		var memo = {};
		return function(selector) {
			if (typeof memo[selector] === "undefined") {
				memo[selector] = fn.call(this, selector);
			}
			return memo[selector]
		};
	})(function (styleTarget) {
		return document.querySelector(styleTarget)
	}),
	singletonElement = null,
	singletonCounter = 0,
	styleElementsInsertedAtTop = [],
	fixUrls = __webpack_require__(25);

module.exports = function(list, options) {
	if(typeof DEBUG !== "undefined" && DEBUG) {
		if(typeof document !== "object") throw new Error("The style-loader cannot be used in a non-browser environment");
	}

	options = options || {};
	options.attrs = typeof options.attrs === "object" ? options.attrs : {};

	// Force single-tag solution on IE6-9, which has a hard limit on the # of <style>
	// tags it will allow on a page
	if (typeof options.singleton === "undefined") options.singleton = isOldIE();

	// By default, add <style> tags to the <head> element
	if (typeof options.insertInto === "undefined") options.insertInto = "head";

	// By default, add <style> tags to the bottom of the target
	if (typeof options.insertAt === "undefined") options.insertAt = "bottom";

	var styles = listToStyles(list);
	addStylesToDom(styles, options);

	return function update(newList) {
		var mayRemove = [];
		for(var i = 0; i < styles.length; i++) {
			var item = styles[i];
			var domStyle = stylesInDom[item.id];
			domStyle.refs--;
			mayRemove.push(domStyle);
		}
		if(newList) {
			var newStyles = listToStyles(newList);
			addStylesToDom(newStyles, options);
		}
		for(var i = 0; i < mayRemove.length; i++) {
			var domStyle = mayRemove[i];
			if(domStyle.refs === 0) {
				for(var j = 0; j < domStyle.parts.length; j++)
					domStyle.parts[j]();
				delete stylesInDom[domStyle.id];
			}
		}
	};
};

function addStylesToDom(styles, options) {
	for(var i = 0; i < styles.length; i++) {
		var item = styles[i];
		var domStyle = stylesInDom[item.id];
		if(domStyle) {
			domStyle.refs++;
			for(var j = 0; j < domStyle.parts.length; j++) {
				domStyle.parts[j](item.parts[j]);
			}
			for(; j < item.parts.length; j++) {
				domStyle.parts.push(addStyle(item.parts[j], options));
			}
		} else {
			var parts = [];
			for(var j = 0; j < item.parts.length; j++) {
				parts.push(addStyle(item.parts[j], options));
			}
			stylesInDom[item.id] = {id: item.id, refs: 1, parts: parts};
		}
	}
}

function listToStyles(list) {
	var styles = [];
	var newStyles = {};
	for(var i = 0; i < list.length; i++) {
		var item = list[i];
		var id = item[0];
		var css = item[1];
		var media = item[2];
		var sourceMap = item[3];
		var part = {css: css, media: media, sourceMap: sourceMap};
		if(!newStyles[id])
			styles.push(newStyles[id] = {id: id, parts: [part]});
		else
			newStyles[id].parts.push(part);
	}
	return styles;
}

function insertStyleElement(options, styleElement) {
	var styleTarget = getElement(options.insertInto)
	if (!styleTarget) {
		throw new Error("Couldn't find a style target. This probably means that the value for the 'insertInto' parameter is invalid.");
	}
	var lastStyleElementInsertedAtTop = styleElementsInsertedAtTop[styleElementsInsertedAtTop.length - 1];
	if (options.insertAt === "top") {
		if(!lastStyleElementInsertedAtTop) {
			styleTarget.insertBefore(styleElement, styleTarget.firstChild);
		} else if(lastStyleElementInsertedAtTop.nextSibling) {
			styleTarget.insertBefore(styleElement, lastStyleElementInsertedAtTop.nextSibling);
		} else {
			styleTarget.appendChild(styleElement);
		}
		styleElementsInsertedAtTop.push(styleElement);
	} else if (options.insertAt === "bottom") {
		styleTarget.appendChild(styleElement);
	} else {
		throw new Error("Invalid value for parameter 'insertAt'. Must be 'top' or 'bottom'.");
	}
}

function removeStyleElement(styleElement) {
	styleElement.parentNode.removeChild(styleElement);
	var idx = styleElementsInsertedAtTop.indexOf(styleElement);
	if(idx >= 0) {
		styleElementsInsertedAtTop.splice(idx, 1);
	}
}

function createStyleElement(options) {
	var styleElement = document.createElement("style");
	options.attrs.type = "text/css";

	attachTagAttrs(styleElement, options.attrs);
	insertStyleElement(options, styleElement);
	return styleElement;
}

function createLinkElement(options) {
	var linkElement = document.createElement("link");
	options.attrs.type = "text/css";
	options.attrs.rel = "stylesheet";

	attachTagAttrs(linkElement, options.attrs);
	insertStyleElement(options, linkElement);
	return linkElement;
}

function attachTagAttrs(element, attrs) {
	Object.keys(attrs).forEach(function (key) {
		element.setAttribute(key, attrs[key]);
	});
}

function addStyle(obj, options) {
	var styleElement, update, remove;

	if (options.singleton) {
		var styleIndex = singletonCounter++;
		styleElement = singletonElement || (singletonElement = createStyleElement(options));
		update = applyToSingletonTag.bind(null, styleElement, styleIndex, false);
		remove = applyToSingletonTag.bind(null, styleElement, styleIndex, true);
	} else if(obj.sourceMap &&
		typeof URL === "function" &&
		typeof URL.createObjectURL === "function" &&
		typeof URL.revokeObjectURL === "function" &&
		typeof Blob === "function" &&
		typeof btoa === "function") {
		styleElement = createLinkElement(options);
		update = updateLink.bind(null, styleElement, options);
		remove = function() {
			removeStyleElement(styleElement);
			if(styleElement.href)
				URL.revokeObjectURL(styleElement.href);
		};
	} else {
		styleElement = createStyleElement(options);
		update = applyToTag.bind(null, styleElement);
		remove = function() {
			removeStyleElement(styleElement);
		};
	}

	update(obj);

	return function updateStyle(newObj) {
		if(newObj) {
			if(newObj.css === obj.css && newObj.media === obj.media && newObj.sourceMap === obj.sourceMap)
				return;
			update(obj = newObj);
		} else {
			remove();
		}
	};
}

var replaceText = (function () {
	var textStore = [];

	return function (index, replacement) {
		textStore[index] = replacement;
		return textStore.filter(Boolean).join('\n');
	};
})();

function applyToSingletonTag(styleElement, index, remove, obj) {
	var css = remove ? "" : obj.css;

	if (styleElement.styleSheet) {
		styleElement.styleSheet.cssText = replaceText(index, css);
	} else {
		var cssNode = document.createTextNode(css);
		var childNodes = styleElement.childNodes;
		if (childNodes[index]) styleElement.removeChild(childNodes[index]);
		if (childNodes.length) {
			styleElement.insertBefore(cssNode, childNodes[index]);
		} else {
			styleElement.appendChild(cssNode);
		}
	}
}

function applyToTag(styleElement, obj) {
	var css = obj.css;
	var media = obj.media;

	if(media) {
		styleElement.setAttribute("media", media)
	}

	if(styleElement.styleSheet) {
		styleElement.styleSheet.cssText = css;
	} else {
		while(styleElement.firstChild) {
			styleElement.removeChild(styleElement.firstChild);
		}
		styleElement.appendChild(document.createTextNode(css));
	}
}

function updateLink(linkElement, options, obj) {
	var css = obj.css;
	var sourceMap = obj.sourceMap;

	/* If convertToAbsoluteUrls isn't defined, but sourcemaps are enabled
	and there is no publicPath defined then lets turn convertToAbsoluteUrls
	on by default.  Otherwise default to the convertToAbsoluteUrls option
	directly
	*/
	var autoFixUrls = options.convertToAbsoluteUrls === undefined && sourceMap;

	if (options.convertToAbsoluteUrls || autoFixUrls){
		css = fixUrls(css);
	}

	if(sourceMap) {
		// http://stackoverflow.com/a/26603875
		css += "\n/*# sourceMappingURL=data:application/json;base64," + btoa(unescape(encodeURIComponent(JSON.stringify(sourceMap)))) + " */";
	}

	var blob = new Blob([css], { type: "text/css" });

	var oldSrc = linkElement.href;

	linkElement.href = URL.createObjectURL(blob);

	if(oldSrc)
		URL.revokeObjectURL(oldSrc);
}


/***/ }),
/* 25 */
/***/ (function(module, exports) {


/**
 * When source maps are enabled, `style-loader` uses a link element with a data-uri to
 * embed the css on the page. This breaks all relative urls because now they are relative to a
 * bundle instead of the current page.
 *
 * One solution is to only use full urls, but that may be impossible.
 *
 * Instead, this function "fixes" the relative urls to be absolute according to the current page location.
 *
 * A rudimentary test suite is located at `test/fixUrls.js` and can be run via the `npm test` command.
 *
 */

module.exports = function (css) {
  // get current location
  var location = typeof window !== "undefined" && window.location;

  if (!location) {
    throw new Error("fixUrls requires window.location");
  }

	// blank or null?
	if (!css || typeof css !== "string") {
	  return css;
  }

  var baseUrl = location.protocol + "//" + location.host;
  var currentDir = baseUrl + location.pathname.replace(/\/[^\/]*$/, "/");

	// convert each url(...)
	/*
	This regular expression is just a way to recursively match brackets within
	a string.

	 /url\s*\(  = Match on the word "url" with any whitespace after it and then a parens
	   (  = Start a capturing group
	     (?:  = Start a non-capturing group
	         [^)(]  = Match anything that isn't a parentheses
	         |  = OR
	         \(  = Match a start parentheses
	             (?:  = Start another non-capturing groups
	                 [^)(]+  = Match anything that isn't a parentheses
	                 |  = OR
	                 \(  = Match a start parentheses
	                     [^)(]*  = Match anything that isn't a parentheses
	                 \)  = Match a end parentheses
	             )  = End Group
              *\) = Match anything and then a close parens
          )  = Close non-capturing group
          *  = Match anything
       )  = Close capturing group
	 \)  = Match a close parens

	 /gi  = Get all matches, not the first.  Be case insensitive.
	 */
	var fixedCss = css.replace(/url\s*\(((?:[^)(]|\((?:[^)(]+|\([^)(]*\))*\))*)\)/gi, function(fullMatch, origUrl) {
		// strip quotes (if they exist)
		var unquotedOrigUrl = origUrl
			.trim()
			.replace(/^"(.*)"$/, function(o, $1){ return $1; })
			.replace(/^'(.*)'$/, function(o, $1){ return $1; });

		// already a full url? no change
		if (/^(#|data:|http:\/\/|https:\/\/|file:\/\/\/)/i.test(unquotedOrigUrl)) {
		  return fullMatch;
		}

		// convert the url to a full url
		var newUrl;

		if (unquotedOrigUrl.indexOf("//") === 0) {
		  	//TODO: should we add protocol?
			newUrl = unquotedOrigUrl;
		} else if (unquotedOrigUrl.indexOf("/") === 0) {
			// path should be relative to the base url
			newUrl = baseUrl + unquotedOrigUrl; // already starts with '/'
		} else {
			// path should be relative to current directory
			newUrl = currentDir + unquotedOrigUrl.replace(/^\.\//, ""); // Strip leading './'
		}

		// send back the fixed url(...)
		return "url(" + JSON.stringify(newUrl) + ")";
	});

	// send back the fixed css
	return fixedCss;
};


/***/ }),
/* 26 */
/***/ (function(module, exports) {

var g;

// This works in non-strict mode
g = (function() {
	return this;
})();

try {
	// This works if eval is allowed (see CSP)
	g = g || Function("return this")() || (1,eval)("this");
} catch(e) {
	// This works if the window reference is available
	if(typeof window === "object")
		g = window;
}

// g can still be undefined, but nothing to do about it...
// We return undefined, instead of nothing here, so it's
// easier to handle this case. if(!global) { ...}

module.exports = g;


/***/ }),
/* 27 */
/***/ (function(module, exports, __webpack_require__) {

__webpack_require__(7);
module.exports = __webpack_require__(8);


/***/ })
/******/ ]);
//# sourceMappingURL=Search.js.map