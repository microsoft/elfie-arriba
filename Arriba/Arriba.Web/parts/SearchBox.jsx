import "./SearchBox.scss";

export default React.createClass({
    getInitialState: function () {
        return { suggestions: [], sel: 0, completed: "", completionCharacters: [] };   
    },
    componentDidMount: function () {
        this.refs.searchBox.focus();
        this.handleClickDocument = e => {
            if (e.target === this.refs.searchBox) return; // Don't hide when clicking on input[text]
            this.setState({ suggestions: [] });
        }
        document.addEventListener("click", this.handleClickDocument);
        window.addEventListener("storage", this);
        window.addEventListener("mousewheel", this);
    },
    componentWillUnmount: function() {
        document.removeEventListener("click", this.handleClickDocument);
        window.removeEventListener("storage", this);
        window.removeEventListener("mousewheel", this);
    },
    handleEvent: function(e) {
        // Prefer "mousewheel" over "scroll" as the latter gets (noisily) triggered by results loading.
        if (e.type === "mousewheel" && this.refs.suggestions && !this.refs.suggestions.contains(e.target)) this.setState({ suggestions: [] });
        if (e.type === "storage" && ["favorites"].includes(e.key)) setTimeout(() => this.forceUpdate()); // Just to update the star.
    },
    handleFocusOrBlur: function () {
        if (isIE()) this.bypassInputOnce = true;
    },
    onInput: function (e) {
        if (this.bypassInputOnce) {
            this.bypassInputOnce = false;
            return;
        }
        this.setQuery(this.refs.searchBox.value);
    },
    handleClick: function (e) {
        // Re-show the suggestion list even if the textbox already has focus.
        // Limiting to "", to avoid spurious re-shows if clicking around existing text.
        this.setQuery(this.refs.searchBox.value);
    },
    handleKeyDown: function (e) {
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
    },
    handleClickSuggestion: function (item) {
        var separator = (item.category === "Value" ? "" : " ");
        this.setQuery(item.replaceAs || this.state.completed + item.completeAs + separator);
        this.refs.searchBox.focus();
    },
    setQuery: function (query) {
        this.props.onSearchChange(query);

        if (this.suggestions && query.startsWith(this.state.completed)) {
            const startsWithCI = (a, b) => a.toUpperCase().startsWith(b.toUpperCase());
            const incomplete = query.slice(this.state.completed.length);
            const matching = this.suggestions.filter(s => startsWithCI(s.completeAs.trimIf("["), incomplete));
            if (matching.length) {
                this.setState({ suggestions: matching, sel: 0 });
                return;
            }
        }


        if (this.lastRequest) this.lastRequest.abort();
        this.lastRequest = jsonQuery(
            configuration.url + "/suggest?q=" + encodeURIComponent(query),
            data => {
                this.suggestions = data.content.suggestions; // A cache which is later filtered and assigned to state.suggestions.
                this.setState({
                    suggestions: data.content.suggestions,
                    sel: 0,
                    completed: data.content.complete, 
                    completionCharacters: data.content.completionCharacters.map(c => ({ "\t": "Tab" })[c] || c),
                });
            }
        );
    },
    toggleFavorite: function () {
        if (!this.props.parsedQuery) return;
        localStorage.updateJson("favorites", favs => ([] || favs).toggle(this.props.parsedQuery));
    },
    render: function () {
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
                inst("S", x, y + 18 - 18, ",", x, y + 18);
                y += 37; // Matches the CSS declared height of each row.
            });
            inst("L", w - values[values.length - 1] * 0.75, y);
            inst("L", w, y);
            inst("Z");
            return <svg><path id="p" d={d} /></svg>
        })();

        return <div className="searchBox">
            <div className={ "loading " + (this.props.loading ? "loading-active" : "") }></div>
            <input ref="searchBox" type="text" spellCheck="false"
                placeholder="Search for..." 
                tabIndex="1" onInput={this.onInput} value={this.props.query} 
                onKeyDown={this.handleKeyDown} onClick={this.handleClick} 
                onFocus={this.handleFocusOrBlur} onBlur={this.handleFocusOrBlur}/>
            <div className="rail">
                {this.state.completed}
                <span style={{ position: "relative" }} >
                    <div className="railContents">
                        {this.state.suggestions.length > 0 &&
                            <div ref="suggestions" className="suggestions" >
                                {svg}
                                {this.state.suggestions.map((item, index) =>
                                    <div className={"suggestion " + (this.state.sel == index ? "suggestion-sel" : "" )}
                                        onClick={ this.handleClickSuggestion.bind(this, item) }>
                                        <span>{item.display}</span>
                                        <span className="suggestion-hint">{item.hint}</span>
                                    </div>
                                )}
                            </div>
                        }
                    </div>
                </span>
            </div>
            <i className={"searchIcon clickable " + ((localStorage.getJson("favorites") || []).includes(this.props.parsedQuery) ? "icon-solid-star" : "icon-outlined-star")} onClick={this.toggleFavorite}></i>
            <i className="searchIcon icon-find"></i>
        </div>;
    }
});
