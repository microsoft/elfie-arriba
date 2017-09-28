import "./Automator.scss";

// Back-door/unsupported method of getting a React component from an HTMLElement.
// For testing purposes only.
Object.defineProperties(HTMLElement.prototype, {
    'react': {
        get: function() {
            for (var key in this) {
                if (!key.startsWith("__reactInternalInstance$")) continue;
                return this[key]._currentElement._owner._instance;
            }
            return null;
        }
    }
});

// Emits a hidden clickable square that, when clicked, provides
// a rudimentary way to test the site via automated `commands`.
// There is currently no error reporting, just watch the console for issues.
export default class extends React.Component {
    run() {
        if (isIE()) return; // Not working for IE yet due to Eventing model.

        function $(query) {
            return document.querySelector(query);
        }

        function dispatch(query, type, options) {
            var e = new Event(type, { bubbles: true });
            Object.assign(e, options); // Workaround for actually creating the specific event type.
            $(query).dispatchEvent(e);
        }

        function run() {
            if (commands.length) {
                var c = commands.shift()
                if (typeof c === "function") {
                    c();
                    setTimeout(run, 250);
                }
                if (typeof c === "number") {
                    setTimeout(run, c);
                }
            }
        }

        const commands = [
            () => $("[data-reactroot]").react.setState({ query: "" }),
            500,
            () => $(".searchBox input").focus(),
            () => $("[data-reactroot]").react.setState({ query: "j" }),
            () => $("[data-reactroot]").react.setState({ query: "je" }),
            () => $("[data-reactroot]").react.setState({ query: "jef" }),
            () => $("[data-reactroot]").react.setState({ query: "jeff" }),
            () => dispatch(".searchBox input", "keydown", { key: "ArrowDown" }),
            () => dispatch(".searchBox input", "keydown", { key: "ArrowDown" }),
            () => dispatch(".suggestion:nth-child(5)", "mouseover"),
            () => dispatch(".suggestion:nth-child(6)", "mouseover"),
        ];
        run();
    }
    render() {
        return <div className="automator" onClick={e => this.run()}></div>
    }
}
