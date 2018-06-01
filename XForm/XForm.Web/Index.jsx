import "./Index.scss"
import "./shared.jsx"
import React from "react"
import ReactDOM from "react-dom"

(() => {
    window.log = function() { console.log.apply(console, arguments) }

    // TODO: Run on leading edge AND trailing edge of last request.
    window.debounce = function(func, wait) {
    	var timeout
    	return function() {
    		clearTimeout(timeout)
    		timeout = setTimeout(() => {
                timeout = null
    			func.apply(this, arguments)
            }, wait)
    	}
    }

    window.singleTimeout = function() {
        // Usage:
        // st = singleTimeout()   // Ctor
        // st(f)                  // Cancels any previous f, runs f synchonously.
        // st(f, 100)             // Cancels any previous f, runs f after 100ms delay.
        // st()                   // Cancels any previous f.

        var timerId
        return (f, delayMs) => {
            clearTimeout(timerId) // No harm if id is undef.
            timerId = undefined
            if (!f) return
            if (delayMs) {
                timerId = setTimeout(f, delayMs)
            } else {
                f()
            }
        }
    }

    Array.prototype.remove = function(item) {
        var i = this.indexOf(item);
        if (i >= 0) this.splice(i, 1);
        return this;
    };

    Array.prototype.toggle = function(item) {
        this.includes(item) ? this.remove(item) : this.push(item);
        return this;
    }

    Array.prototype.last = function() {
        return this[this.length - 1]
    }

    Date.min = function(...dates) {
        return new Date(Math.min(...dates))
    }

    Date.range = function(from, to) {
        const range = [new Date(from)]
        while (from < to) {
            from.setDate(from.getDate() + 1)
            range.push(new Date(from))
        }
        return range
    }

    Date.isDateEquals = function(a, b) {
        return a && b
            && a.getYear()  === b.getYear()
            && a.getMonth() === b.getMonth()
            && a.getDate()  === b.getDate()
    }

    Date.prototype.firstOfMonth = function() {
        const copy = new Date(this)
        copy.setDate(1)
        return copy
    }

    Date.prototype.daysAgo = function(n) {
        const copy = new Date(this)
        copy.setDate(copy.getDate() - (n || 0))
        return copy
    }

    Date.prototype.sunday = function() {
        const copy = new Date(this)
        copy.setDate(copy.getDate() - copy.getDay())
        return copy
    }

    Date.prototype.toXFormat = function() {
        const mm = this.toLocaleString('en-US', { month: '2-digit' })
        const dd = this.toLocaleString('en-US', { day: '2-digit' })
        return `${this.getFullYear()}-${mm}-${dd}`
    }

    Date.prototype.toFriendly = function() {
        const today = new Date()
        if (Date.isDateEquals(this, today)) return "Today"
        if (Date.isDateEquals(this, today.daysAgo(1))) return "Yesterday"
        if (Date.isDateEquals(this, today.daysAgo(7))) return "Last Week"
        const mm = this.toLocaleString('en-US', { month: 'short' })
        const dd = this.toLocaleString('en-US', { day: 'numeric' })
        return `${mm} ${dd}`
    }
    
    String.prototype.ciIncludes = function(term) {
        return !term || this.toLowerCase().includes(term.toLowerCase())
    }

    window.extendEditor = function(editor) {
        editor.valueUntilPosition = function(from) {
            return this.getRange(from || { line: 0, ch: 0 }, this.getCursor())
        }
        editor.indexToPosition = function(i) {
            const lines = this.getValue().slice(0, i).split('\n')
            const col = lines.last().length
            return CodeMirror.Pos(lines.length - 1, col)
        }
        editor.append = function(value) {
            this.replaceRange(value, { line: Infinity })
        }
        editor.appendLine = function(line) {
            this.append(`${this.getValue().endsWith('\n') ? '' : '\n'}${line}`)
        }
    }
})()

class Index extends React.Component {
    constructor(props) {
        super(props)
        this.count = this.baseCount = 50
        this.cols = this.baseCols = 20
        this.debouncedQueryChanged = debounce(this.queryChanged, 500)
        this.state = { query: this.query, userCols: [], saveAs: '', pausePulse: true }

        this.reqPeek = new CachableReusedRequest('run');
        this.reqPeek.caching = true;

        this.dateTimer = singleTimeout()
        this.peekTimer = singleTimeout()
    }
    componentDidMount() {
        CodeMirror.defineSimpleMode("xform", {
            start: [
                { regex: /^\w+/, sol: true, token: 'xf-verb' },
                { regex: /\[\w*\]/, token: 'variable' },
                { regex: /"\w*"/, token: 'string' },
            ]
        })

        this.refs.textArea.value = localStorage.q || 'read WebRequest\nwhere [HttpStatus] != "200"'
        this.editor = CodeMirror.fromTextArea(this.refs.textArea, {
            lineNumbers: true,
            mode: 'xform',
            extraKeys: { 'Ctrl-Space': 'autocomplete' },
            hintOptions: { completeSingle: false, hint: async (cm, option) =>
                this.suggest().then(o => {
                    const token = this.editor.valueUntilPosition(this.editor.indexToPosition(o.InvalidTokenIndex))
                    const results = o.Values && {
                        list: o.Values.split(";").filter(s => s.ciIncludes(token)),
                        from: this.editor.indexToPosition(o.InvalidTokenIndex), // Convert to line/ch
                        to:   this.editor.getCursor(),
                    }
                    if (results) CodeMirror.on(results, 'pick', () => this.justPicked = true) // Check if any more matches
                    return results
                })
            }
        })
        extendEditor(this.editor)

        this.editor.on('change', () => {
            this.textJustChanged = true
            this.queryTextChanged.bind(this)
        })
        this.editor.on('cursorActivity', async cm => {
            if (this.textJustChanged && !this.justPicked) this.queryAndCursorChanged()
            this.textJustChanged = false
            this.justPicked = false
        })
        this.validQuery = this.query
        this.queryChanged()
    }
    componentDidUpdate(prevProps, prevState) {
        if (prevState.peek !== this.state.peek) {
            const params = this.state.peek
                ? { asof: this.state.asOf, q: `${this.query}\npeek [${this.state.peek.name}]` }
                : undefined
            this.reqPeek.update(params, json => {
                this.setState({ peekData: json && json.rows.sort((a, b) => b[1] - a[1]) && json.rows.slice(0, 7) })
            })
        }
    }
    get suggest() {
        return xhr(`suggest`, { asof: this.state.asOf, q: this.editor.valueUntilPosition() })
    }
    async suggest() {
        const vup = this.editor.valueUntilPosition()
        
        // CodeMirror sometimes independently calls hintOptions.hint() after cursor activity.
        // This can happen before the async suggest returns, thus using lack of .root as a sign things are in-progress.
        // Hand-waving the cache key checking and assuming it is the same--which works in practice.
        if (this._cached?.root !== undefined) {
            const x = this._cached
            const token = vup.substring(x.iti)
            const filtered = x.values?.filter(s => s.ciIncludes(token)) || []
            if (x.root !== vup.substring(0, x.iti) || token.length && !filtered.length) {
                this._cached = undefined
            }
        }
        
        if (!this._cached) {
            const x = this._cached = xhr(`suggest`, { asof: this.state.asOf, q: vup })
            const s = await this._cached
            x.iti = Math.min(s.InvalidTokenIndex, vup.length)
            x.root = vup.substring(0, x.iti) // this.editor.getValue() technically more correct.
            x.values = s.Values?.split(';')
        }

        return this._cached
    }
    get query() {
        return this.editor?.getValue()
    }
    async queryTextChanged(force) {
        const trimmedQuery = this.query.trim() // Pre async capture
        
            const info = await this.suggest()
        
            if (info.Valid && (force || this.validQuery !== trimmedQuery)) {
                this.validQuery = trimmedQuery
                this.debouncedQueryChanged()
            }

            const errorMessage = info.ErrorMessage
            if (errorMessage !== this.state.errorMessage) this.setState({ errorMessage })

            const usage = info.Usage
            if (usage !== this.state.usage) this.setState({ usage })
        
            if (this.validationMarker) this.validationMarker.clear()
            if (errorMessage) { // Not using InvalidTokenIndex as the in-progress token is considered invalid.
                this.validationMarker = this.editor.markText(
                    this.editor.indexToPosition(info.InvalidTokenIndex),
                    this.editor.indexToPosition(info.InvalidTokenIndex + info.InvalidToken.length),
                    { className: 'validationError' })
            }
    }
    async queryAndCursorChanged() {
        if ((await this.suggest()).Values) this.editor.showHint()
    }
    queryChanged() {
        this.count = this.baseCount
        this.cols = this.baseCols

        if(!!this.validQuery) this.setState({ loading: true, pausePulse: true })

        xhr(`run`, { asof: this.state.asOf, q: `${this.validQuery}\nschema` }).then(o => {
            const schemaBody = (o.rows || []).map(r => ({ name: r[0], type: `${r[1]}` }))
            const colNames = new Set(schemaBody.map(r => r.name))
            this.setState({
                schemaBody,
                userCols: this.state.userCols.filter(c => colNames.has(c)),
            })
            this.limitChanged(0, 0, true)
        })
    }
    limitChanged(addCount = 0, addCols = 0, firstRun) { // firstRun... of the this specific query
        this.count += addCount
        this.cols += addCols
        const q = this.validQuery

        const userCols = this.state.userCols.length && `\nselect ${this.state.userCols.map(c => `[${c}]`).join(', ')}` || ''
        this.setState({ loading: true, pausePulse: firstRun })
        xhr(`run`, { rowLimit: this.count, colLimit: this.cols, asof: this.state.asOf, q: `${q}${userCols}` }).then(o => {
            if (o.Valid === false) {
                this.setState({
                    results: [],
                    resultCount: undefined,
                    loading: false,
                    pausePulse: false,
                })
                return
            }
            if (o.Message || o.ErrorMessage) throw 'Error should have been caught before run.'
            if (firstRun) {
                this.setState({ results: o })
                xhr(`count`, { asof: this.state.asOf, q: this.validQuery }).then(o => {
                    this.setState({
                        resultCount: typeof o.Count === "number" && `${o.Count.toLocaleString()} Results (${o.RuntimeMs} ms)`,
                        loading: false,
                        pausePulse: false,
                    })
                })
            } else {
                this.setState({ results: o, loading: false })
            }
        })
    }
    _makeSvg(list) {
        if (!list.length) return false;

        // Generates a SVG histogram to be displayed behind the completion list.
        // The path goes counter-clockwise starting from the top-right.
        var d = '';

        // The inst() currently concats SVG commands to the list 'd'.
        // However when debugging, it is useful to redirect the ...params to the console.
        const inst = (...params) => d += params.join(" ") + " ";

        // Scrape ___% from the item.hint. If not found, default to 0.
        const values = list.map(item => new Number(item[2].replace('%', '')) + 0 || 0);

        const w = 80; // Matches CSS declared width.
        inst("M", w, 0);
        inst("L", w - values[0] * 0.75, 0);
        const max = Math.max(...values) || 1; // Prevent divide by zero.
        var y = 0; // Running total fo the height.
        values.forEach(val => {
            const x = w - (val/max) * w;
            inst("S", x, y, ",", x, y + 17); // Half of the CSS height.
            y += 34; // Matches the CSS declared height of each row.
        });
        const x = w - values[values.length - 1] * 0.75;
        inst("S", x, y, x, y + 18);
        inst("L", w, y);
        inst("Z");
        return <svg><path id="p" d={d} /></svg>
    }
    render() {
        var cols, rows
        const results = this.state.results
        if (results) {
            cols = results.cols
            rows = results.rows
        }

        const Peek = () => {
            if (!this.state.peek || !this.state.peek.tr || !this.state.peekData) return null
            const rect = this.state.peek.tr.getBoundingClientRect()
            return <div className="peek" style={{ left: `${rect.x + rect.width - 5}px`, top: `${rect.y}px` }}
                onMouseEnter={e => this.peekTimer()}
                onMouseLeave={e => this.peekTimer(() => this.setState({ peek: undefined }), 100)}>
                {this._makeSvg(this.state.peekData)}
                {this.state.peekData.map((row, i) => <div key={i} className="peek-value"
                    onClick={e => {
                        const expr = `[${this.state.peek.name}] = "${row[0]}"`
                        const last = this.query.split('\n').last()
                        if (last.startsWith('where')) {
                            this.editor.append(` AND ${expr}`)
                        } else {
                            this.editor.appendLine(`where ${expr}`)
                        }
                        this.peekTimer(() => this.setState({ peek: undefined }))
                    }}>
                    <span>{row[0] === '' ? '—' : row[0] }</span>
                    <span>{row[2]}</span>
                </div>)}
            </div>
        }

        const DatePicker = () => {
            if (!this.state.showDatePicker) return null
            const today = new Date()
            const startDate = Date.min(today.firstOfMonth(), today.daysAgo(7)).sunday()
            return <div className="datePicker"
                onMouseEnter={e => this.dateTimer()}
                onMouseLeave={e => this.dateTimer(() => this.setState({ showDatePicker: undefined }), 100)}>
                <div>{new Date().toLocaleString('en-US', { month: 'long' })}</div>
                <div>
                    {'SMTWTFS'.split('').map((d, i)=> <span key={`header${i}`} className="header">{d}</span>)}
                    {Date.range(startDate, today).map(d => <span key={d}
                        className={o2c({
                            current: Date.isDateEquals(this.state.asOfDate || new Date(), d),
                            outside: today.getMonth() !== d.getMonth(),
                        })}
                        onClick={() => {
                            this.setState({ asOf: d.toXFormat(), asOfDate: d }, () => this.queryTextChanged(true))
                            this.dateTimer(() => this.setState({ showDatePicker: undefined }))
                        }}
                        onMouseEnter={e => this.setState({ hoverDate: d })}
                        onMouseLeave={e => this.setState({ hoverDate: undefined })}>
                        <span>{d.getDate()}</span>
                    </span>)}
                </div>
            </div>
        }

        const formatters = rows && cols.map(col => col === "Count" || col.endsWith(".Sum")
                ? cell => cell === "" ? "—" : (+cell).toLocaleString()
                : cell => cell)

        const encodedParams = encodeParams({ asof: this.state.asOf, q: this.validQuery })

        return [<div key="root" className={`root`}>
            <div className="query">
                <div className="queryHeader">
                    <input type="text" placeholder="Save As"
                        value={this.state.saveAs} onChange={e => this.setState({ saveAs: e.target.value })}/>
                    <span className="save" style={{ opacity: +!!this.state.saveAs }} onClick={e => {
                        const q = this.query
                        const name = this.state.saveAs
                        if (!name || !q) return
                        xhr(`save`, { name, q }).then(o => {
                            this.setState({ saving: "Saved" })
                            setTimeout(() => this.setState({ saving: "Save" }), 3000)
                        })
                    }}>{ this.state.saving || "Save" }</span>
                    <span title={(this.state.asOfDate || new Date()).toDateString()}
                        className={'button' + (this.state.showDatePicker ? ' hot' : '')}
                        onMouseEnter={e => this.dateTimer(() => this.setState({ showDatePicker: true }))}
                        onMouseLeave={e => this.dateTimer(() => this.setState({ showDatePicker: undefined }), 100)}>
                        As of {(this.state.hoverDate || this.state.asOfDate || new Date()).toFriendly()}</span>
                </div>
                <div className="queryUsage">{
                    this.state.errorMessage && <span className="errorMessage">{this.state.errorMessage}</span>
                    || this.state.usage || `\u200B`
                }</div>
                <div id="queryEditor">
                    <textarea ref="textArea"></textarea>
                </div>
                <DatePicker key="datePicker" />
            </div>
            <div id="schema">
                <div className="schemaHeader">
                    {!this.state.userCols.length && this.state.schemaBody && <span>{this.state.schemaBody.length} Columns</span>}
                    {!!this.state.userCols.length && <span className="button" onClick={e => this.setState({ userCols: [] }, () => this.limitChanged())}>Reset</span>}
                    <span className="flexFill"></span>
                    {!!this.state.userCols.length && <span className="button" onClick={e => {
                        this.editor.appendLine(`select ${this.state.userCols.map(c => `[${c}]`).join(', ')}`)
                        this.setState({ userCols: [] }, () => this.limitChanged())
                    }}>Apply</span>}
                </div>
                {this.state.schemaBody && <div className="tableWrapper">
                    <table>
                        <tbody>
                            {this.state.schemaBody && this.state.schemaBody.map((r, i) => <tr key={i}
                                ref={tr => r.tr = tr}
                                onMouseEnter={e => this.peekTimer(() => this.setState({ peek: r }))}
                                onMouseLeave={e => this.peekTimer(() => this.setState({ peek: undefined }), 100)}>
                                <td><label><input type="checkbox" checked={this.state.userCols.includes(r.name)} onChange={e => {
                                    this.setState({ userCols: [...this.state.userCols].toggle(r.name) }, () => this.limitChanged())
                                }}/>{r.name}</label></td>
                                <td>{r.type}</td>
                            </tr>)}
                        </tbody>
                    </table>
                </div>}
            </div>
            <div id="results">
                <div className="" className={`resultsHeader ${this.state.pausePulse ? '' : 'pulse'}`}>
                    <span>{this.state.resultCount}</span>
                    <span className="flexFill"></span>
                    {encodedParams && <a className="button" target="_blank" href={`${xhr.origin}/download?fmt=csv&${encodedParams}`}>CSV</a>}
                    {encodedParams && <a className="button" target="_blank" href={`${xhr.origin}/download?fmt=tsv&${encodedParams}`}>TSV</a>}
                    <span className={`loading ${ this.state.loading && 'loading-active' }`}></span>
                </div>
                <div className="tableWrapper" onScroll={e => {
                        const element = e.target
                        const pixelsFromLimitX = (element.scrollWidth - element.clientWidth - element.scrollLeft)
                        const pixelsFromLimitY = (element.scrollHeight - element.clientHeight - element.scrollTop)
                        if (pixelsFromLimitX < 20 && this.cols < this.state.schemaBody.length ) this.limitChanged(0, 10)
                        if (pixelsFromLimitY < 100) this.limitChanged(50)
                    }}>
                    <table>
                        <thead>
                            <tr>
                                {cols?.map(c => <td key={c}>{c}</td>)}
                            </tr>
                        </thead>
                        <tbody>
                            {rows?.map((r, i) => <tr key={i}>{r.map((c, ii) => <td key={i + "x" + ii}>{c}</td>)}</tr>)}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>,
        <Peek key="peek" />]
    }
}

ReactDOM.render(<Index />, document.getElementById("app"))
