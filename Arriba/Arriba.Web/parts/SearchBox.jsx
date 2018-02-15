import "./SearchBox.scss";
import EventedComponent from "./EventedComponent";
import Suggestions from "./Suggestions";

export default class extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = { table: "", space: ""};
        this.events = {
            "storage": e => {
                if (!["favorites"].includes(e.key)) return;
                setTimeout(() => this.forceUpdate()); // Just to update the star.
            },
            "click": e => { // Ideally use input.blur, but that fires before suggestions.item.onClick.
                if (e.target === this.refs.input) return;
                this.reqQuery.clear();
            },
        };
        this.reqQuery = new CachableReusedRequest("suggest",
            () => this.state.space !== "" && ({ t: this.props.userSelectedTable, q: this.props.query }) || undefined,
            json => {
                this.setState({ completed: json && json.complete })
                this.refs.suggestions.suggestions = json
            });
        this.reqPeek = new CachableReusedRequest("suggest",
            () => this.state.selected && this.state.selected.category === "ColumnName"
                    ? { t: this.props.userSelectedTable, q: `${this.state.completed}${this.state.selected.completeAs} = ` }
                    : undefined,
            json => this.refs.peek.suggestions = json)
        this.reqPeek.caching = true
    }
    componentDidMount() {
        super.componentDidMount();
        this.focus();
    }
    componentDidUpdate(prevProps, prevState) {
        // Skip onInteract in situations where the input is not focused. Namely, when a the query is set from the Recents/Favs.
        const diffProps = Object.diff(prevProps, this.props);
        const diffState = Object.diff(prevState, this.state);

        const hasFocus = this.refs.input === document.activeElement;
        if (diffState.hasAny("space")) {
            this.reqQuery[this.state.space ? "update" : "clear"]();
        }
        if (diffProps.hasAny("query") && hasFocus && this.props.query !== "*") {
            this.reqPeek.resetCache();
            this.reqQuery.update();
        }
        if (diffState.hasAny("selected")) {
            this.reqPeek.update();
        }
    }
    focus() {
        this.refs.input.focus();
    }
    toggleFavorite() {
        if (!this.props.parsedQuery) return;
        localStorage.updateJson("favorites", favs => (favs || []).toggle(this.props.parsedQuery));
    }
    _complete(completed, item, key) {
        this.focus(); // Must come before?
        var separator = (item.category === "Value" && key !== " " ? "" : " ");
        var suffix = (key === "Enter" || key === "Tab" || key === " ") ? "" : key;
        var newQuery = completed + item.completeAs + separator + suffix;
        this.props.queryChanged(newQuery);
    }
    render() {
        var star = (localStorage.getJson("favorites") || []).includes(this.props.parsedQuery) ? "icon-solid-star" : "icon-outlined-star"
        const tableSpaceQuery = `${this.state.table}${this.state.space}${this.props.query}`
        return <div className="searchBox">
            <div className={ "loading " + (this.props.loading ? "loading-active" : "") }></div>
            <input ref="input" type="text"
                spellCheck="false"
                placeholder="Search for..."
                tabIndex="1"
                value={tableSpaceQuery}
                onClick={e => this.reqQuery.update()}
                onInput={e => {
                    // IE focus/blur spurriously triggers onInput(), this works around that.
                    var [_, table, space, query] = /([^\s]*)?(\s+)?(.*)?/.exec(e.target.value);
                    table = table || "" // Redundant
                    space = space || ""
                    query = query || ""

                    this.setState({ table, space });
                    if (this.props.query !== query) this.props.queryChanged(query);
                }}
                onKeyDown={e => this.refs.suggestions.onKeyDown(e)} />
            <div className="rail">
                {tableSpaceQuery}
                <span style={{ position: "relative" }} >
                    <div className="railContents">
                        <Suggestions
                            ref="suggestions"
                            selectedChanged={s => this.setState({ selected: s && s.category === "ColumnName" && s || undefined }) }
                            hide={() => this.reqQuery.clear()}
                            complete={this._complete.bind(this)} />
                        <Suggestions
                            ref="peek" sel={-1}
                            marginTop={this.state.selected && this.state.selected.offsetTop || 0}
                            hide={() => this.setState({ selected: undefined })}
                            complete={this._complete.bind(this)} />
                    </div>
                </span>
            </div>
            <i className={"searchBoxIcon clickable " + star} onClick={e => this.toggleFavorite(e)} title="Toggle Favorite"></i>
        </div>
    }
}
