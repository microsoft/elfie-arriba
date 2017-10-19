import "./Suggestions.scss";
import EventedComponent from "./EventedComponent";

export default class extends EventedComponent {
    constructor(props) {
        super(props);
        this.cache = {};
        this.state = { suggestions: [] };
        this.suggestions = { suggestions: [], complete: "", completionCharacters: [] };
        this.events = {
            "mousewheel": e => { // Prefer "mousewheel" over "scroll" as the latter gets (noisily) triggered by results loading.
                if (!this.refs.suggestions || this.refs.suggestions.contains(e.target)) return;
                this.clear();
            },
        };
    }
    componentDidUpdate(prevProps, prevState) {
        // Skip fetch in situations where the input is not focused. Namely, when a the query is set from the Recents/Favs.
        if (prevProps.query !== this.props.query
            && this.props.query !== "*"
            && (!this.props.hasFocus || this.props.hasFocus())) // If hasFocus is not available, assume true.
            this.fetch();

        var suggestions = this.state.suggestions;
        var sel = this.state.sel;
        if (this.props.selectedChanged && (prevState.suggestions !== suggestions || prevState.sel !== sel)) {
            this.props.selectedChanged(suggestions[sel]);
        }
    }

    set suggestions(dataContent) {
        this.setState({
            suggestions: dataContent.suggestions,
            sel: this.props.sel || 0,
            completed: dataContent.complete,
            completionCharacters: dataContent.completionCharacters.map(c => ({ "\t": "Tab" })[c] || c),
        });
    }
    fetch() {
        if (this.lastRequest) this.lastRequest.abort();

        if (this.props.query === undefined) {
            this.clear();
            return;
        }

        var cached = this.cache[this.props.query];
        if (this.props.cache && cached) {
            this.props.completedChanged(cached.complete);
            this.suggestions = cached;
            return;
        }

        const t = this.props.userSelectedTable ? `t=${encodeURIComponent(this.props.userSelectedTable)}&` : ""
        this.lastRequest = jsonQuery(
            configuration.url + `/suggest?${t}q=` + encodeURIComponent(this.props.query),
            data => {
                this.props.completedChanged(data.content.complete);
                this.suggestions = data.content;
                if (this.props.cache) this.cache[this.props.query] = data.content; // Assume query hasn't changed since the call, since new requests abort old ones.
            }
        );
    }
    clear() {
        this.setState({ suggestions: [] });
    }
    clearCache() {
        this.cache = {};
    }

    onClick(item) {
        var separator = (item.category === "Value" ? "" : " ");
        this.props.queryChanged(this.state.completed + item.completeAs + separator);
        this.props.refocus();
    }
    onKeyDown(e) {
        if (!this.state.suggestions.length) return;
        if (e.key === "ArrowUp") {
            this.setState({ sel: (this.state.sel - 1).clamp(0, this.state.suggestions.length - 1) });
            e.stopPropagation();
            e.preventDefault(); // Prevent cursor moving to beginning of line.
        }
        if (e.key === "ArrowDown") {
            this.setState({ sel: (this.state.sel + 1).clamp(0, this.state.suggestions.length - 1) });
            e.stopPropagation();
        }
        if (e.key === "Enter" || this.state.completionCharacters.includes(e.key)) {
            var item = this.state.suggestions[this.state.sel];

            var separator = (item.category === "Value" && e.key !== " " ? "" : " ");
            var suffix = (e.key === "Enter" || e.key === "Tab" || e.key === " ") ? "" : e.key;
            var newQuery = this.state.completed + item.completeAs + separator + suffix;
            this.props.queryChanged(newQuery);
            e.preventDefault(); // Suppress focus tabbing.
        }
        if (e.key === "Escape") {
            this.clear();
            e.stopPropagation(); // Prevent SelectedItem clear.
        }
    }

    get svg() {
        if (!this.state.suggestions.length) return false;

        // Generates a SVG histogram to be displayed behind the completion list.
        // The path goes counter-clockwise starting from the top-right.
        var d = '';

        // The inst() currently concats SVG commands to the list 'd'.
        // However when debugging, it is useful to redirect the ...params to the console.
        const inst = (...params) => d += params.join(" ") + " ";

        // Scrape ___% from the item.hint. If not found, default to 0.
        const values = this.state.suggestions.map(item => new Number(item.hint.replace('%', '')) + 0 || 0);

        const w = 80; // Matches CSS declared width.
        inst("M", w, 0);
        inst("L", w - values[0] * 0.75, 0);
        const max = Math.max(...values) || 1; // Prevent divide by zero.
        var y = 0; // Running total fo the height.
        values.forEach(val => {
            const x = w - (val/max) * w;
            inst("S", x, y, ",", x, y + 18);
            y += 37; // Matches the CSS declared height of each row.
        });
        const x = w - values[values.length - 1] * 0.75;
        inst("S", x, y, x, y + 18);
        inst("L", w, y);
        inst("Z");
        return <svg><path id="p" d={d} /></svg>
    }
    render() {
        var style = {};

        // IE is failing to size the suggestions list to fit it's children.
        // Thus upping the minWidth to compensate.
        if (isIE()) style["minWidth"] = "300px";

        if (this.props.marginTop) style["marginTop"] = this.props.marginTop;
        return this.state.suggestions.length > 0 &&
            <div ref="suggestions" className="suggestions" style={style} >
                {this.svg}
                {this.state.suggestions.map((item, index) =>
                    <div ref={r => item.offsetTop = r && this.refs.suggestions && r.offsetTop - this.refs.suggestions.scrollTop || undefined}
                        className={"suggestion " + (this.state.sel == index ? "suggestion-sel" : "" )}
                        onClick={e => this.onClick(item)}
                        onMouseEnter={e => this.setState({ sel: index })}>
                        <span>{item.display}</span>
                        <span className="suggestion-hint">{item.hint}</span>
                    </div>
                )}
            </div>
    }
}
