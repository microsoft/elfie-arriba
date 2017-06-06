import "./SearchBox.scss";

import EventedComponent from "./EventedComponent";

export default class SearchBox extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = { suggestions: [], sel: 0, completed: "", completionCharacters: [] };
        this.events = {
            "storage": e => {
                if (!["favorites"].includes(e.key)) return;
                setTimeout(() => this.forceUpdate()); // Just to update the star.
            },
            "click": e => { // Ideally use input.blur, but that fires before suggestions.item.onClick.
                if (e.target === this.refs.input) return;
                this.setState({ suggestions: [] });
            },
            "mousewheel": e => { // Prefer "mousewheel" over "scroll" as the latter gets (noisily) triggered by results loading.
                if (!this.refs.suggestions || this.refs.suggestions.contains(e.target)) return;
                this.setState({ suggestions: [] });
            },
        }
    }
    componentDidMount() {
        super.componentDidMount();
        this.refs.input.focus();
    }
    onInput(query) {
        // IE focus/blur spurriously triggers onInput(), this works around that.
        if (this.props.query !== query) this.setQuery(query);
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
            var newQuery = item.replaceAs || (this.state.completed + item.completeAs + separator + suffix);
            this.setQuery(newQuery);
            e.preventDefault(); // Suppress focus tabbing.
        }
        if (e.key === "Escape") {
            this.setState({ suggestions: [] });
        }
    }
    onClickSuggestion(item) {
        var separator = (item.category === "Value" ? "" : " ");
        this.setQuery(item.replaceAs || this.state.completed + item.completeAs + separator);
        this.refs.input.focus();
    }
    setQuery(query) {
        this.props.onSearchChange(query);

        if (this.lastRequest) this.lastRequest.abort();
        this.lastRequest = jsonQuery(
            configuration.url + "/suggest?q=" + encodeURIComponent(query),
            data => this.setState({
                suggestions: data.content.suggestions,
                sel: 0,
                completed: data.content.complete, 
                completionCharacters: data.content.completionCharacters.map(c => ({ "\t": "Tab" })[c] || c),
            })
        );
    }
    toggleFavorite() {
        if (!this.props.parsedQuery) return;
        localStorage.updateJson("favorites", favs => (favs || []).toggle(this.props.parsedQuery));
    }
    render() {
        // Generates a SVG histogram to be displayed behind the completion list.
        // The path goes counter-clockwise starting from the top-right.
        var svg = this.state.suggestions.length && (() => {
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
        })();

        return <div className="searchBox">
            <div className={ "loading " + (this.props.loading ? "loading-active" : "") }></div>
            <input ref="input" type="text"
                spellCheck="false"
                placeholder="Search for..."
                tabIndex="1" 
                value={this.props.query}
                onInput={e => this.onInput(e.target.value)}
                onKeyDown={e => this.onKeyDown(e)}
                onClick={e => this.setQuery(this.refs.input.value)} />
            <div className="rail">
                {this.state.completed}
                <span style={{ position: "relative" }} >
                    <div className="railContents">
                        {this.state.suggestions.length > 0 &&
                            <div ref="suggestions" className="suggestions" style={isIE() ? {minWidth: "300px"} : {}} >
                                {svg}
                                {this.state.suggestions.map((item, index) =>
                                    <div className={"suggestion " + (this.state.sel == index ? "suggestion-sel" : "" )}
                                        onClick={e => this.onClickSuggestion(item) }>
                                        <span>{item.display}</span>
                                        <span className="suggestion-hint">{item.hint}</span>
                                    </div>
                                )}
                            </div>
                        }
                    </div>
                </span>
            </div>
            <i className={"searchIcon clickable " + ((localStorage.getJson("favorites") || []).includes(this.props.parsedQuery) ? "icon-solid-star" : "icon-outlined-star")} onClick={e => this.toggleFavorite()}></i>
            <i className="searchIcon icon-find"></i>
        </div>
    }
}
