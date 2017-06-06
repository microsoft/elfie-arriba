import "./Suggestions.scss";
import EventedComponent from "./EventedComponent";

export default class Suggestions extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = { suggestions: [], sel: 0, completed: "", completionCharacters: [] };
        this.events = {
            "mousewheel": e => { // Prefer "mousewheel" over "scroll" as the latter gets (noisily) triggered by results loading.
                if (!this.refs.suggestions || this.refs.suggestions.contains(e.target)) return;
                this.clear();
            },
        };
    }
    componentDidUpdate(prevProps, prevState) {
        if (prevProps.query !== this.props.query) this.fetch();
    }

    fetch() {
        if (this.lastRequest) this.lastRequest.abort();
        this.lastRequest = jsonQuery(
            configuration.url + "/suggest?q=" + encodeURIComponent(this.props.query),
            data => {
                this.props.completedChanged(data.content.complete);
                this.setState({
                    suggestions: data.content.suggestions,
                    sel: 0,
                    completed: data.content.complete, 
                    completionCharacters: data.content.completionCharacters.map(c => ({ "\t": "Tab" })[c] || c),
                });
            }
        );
    }
    clear() {
        this.setState({ suggestions: [] });
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
        return this.state.suggestions.length > 0 &&
            <div ref="suggestions" className="suggestions" style={isIE() ? {minWidth: "300px"} : {}} >
                {this.svg}
                {this.state.suggestions.map((item, index) =>
                    <div className={"suggestion " + (this.state.sel == index ? "suggestion-sel" : "" )}
                        onClick={e => this.onClick(item)} 
                        onMouseEnter={e => this.setState({ sel: index })}>
                        <span>{item.display}</span>
                        <span className="suggestion-hint">{item.hint}</span>
                    </div>
                )}
            </div>
    }
}