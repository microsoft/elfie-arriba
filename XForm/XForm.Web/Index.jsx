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
        // st = singleTimeout()   // Init
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

    Date.daysAgo = function(n) {
        const d = new Date()
        d.setDate(d.getDate() - (n || 0))
        return d
    }

    Date.firstOfMonth = function() {
        const now = new Date()
        return new Date(now.getFullYear(), now.getMonth())
    }

    Date.prototype.toXFormat = function() {
        const mm = this.toLocaleString('en-US', { month: '2-digit' })
        const dd = this.toLocaleString('en-US', { day: '2-digit' })
        return `${this.getFullYear()}-${mm}-${dd}`
    }

    window.extendEditor = function(editor) {
        editor.valueUntilPosition = function() {
            const position = this.getPosition()
            return this.getModel().getValueInRange({
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: position.lineNumber,
                endColumn: position.column,
            })
        }
        editor.decorate = function(newDecorations) {
            const old = this._oldDecorations || []
            if (old.length || newDecorations.length) {
                this._oldDecorations = this.deltaDecorations(old, newDecorations)
            }
        }
        editor.indexToPosition = function(i) {
            const lines = this.getValue().slice(0, i).split('\n')
            const col = lines.last().length + 1
            return new monaco.Position(lines.length, col)
        }

        // Assumes monaco is loaded
        monaco.Position.prototype.toRange = function(length) {
            return new monaco.Range(this.lineNumber, this.column, this.lineNumber, this.column + length)
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

        const params = new URLSearchParams(location.search.slice(1));
        const q = params.get("q");
        
        const loc = document.location;
        xhr.urlRoot = loc.port === '8080' ? `${loc.protocol}//${loc.hostname}:5073` : ''

        this.initialQuery = q ? q : [
            'read WebRequest',
            'where [HttpStatus] != "200"',
        ].join('\n');

        this.reqPeek = new CachableReusedRequest('run');
        this.reqPeek.caching = true;

        this.peekTimer = singleTimeout()
    }
    componentDidMount() {
        window.require.config({ paths: { 'vs': 'node_modules/monaco-editor/min/vs' }});

    	window.require(['vs/editor/editor.main'], () => {
            monaco.languages.register({ id: 'xform' });
            monaco.languages.setMonarchTokensProvider('xform', {
                tokenizer: {
                    root: [
                        [/^\w+/, 'verb'],
                        [/\[\w*\]/, 'column'],
                        [/"\w*"/, 'string'],
                    ]
                }
            })
            monaco.editor.defineTheme('xform', {
                base: 'vs',
                inherit: false,
                rules: [
                    // https://github.com/Microsoft/vscode/blob/bef497ff82391f4f29ea52f532d896a6903f6ff6/src/vs/editor/standalone/common/themes.ts
                    { token: 'verb',   foreground: '5c99d6' }, // hsa(210, 60%, 60%), Atom dark: 44C0C6
                    { token: 'column', foreground: '40bfbf' }, // hsl(180, 50%, 50%), Atom dark: D1BC92
                    { token: 'string', foreground: 'bf5540' }, // hsl( 10, 50%, 50%), Atom dark: FC8458
                ]
            })
            monaco.languages.registerCompletionItemProvider('xform', {
                provideCompletionItems: (model, position) =>
                    (this.suggestions && Promise.resolve(this.suggestions) || this.suggest).then(o => {
                        this.suggestions = undefined

                        if (!o.Values) return []

                        const textUntilPosition = this.editor.valueUntilPosition()
                        const trunate = o.ItemCategory === '[Column]' && /\[\w*$/.test(textUntilPosition)
                            || o.ItemCategory === 'CompareOperator' && /!$/.test(textUntilPosition)
                            || o.ItemCategory === 'CompareOperator' && /\|$/.test(textUntilPosition)

                        const kind = monaco.languages.CompletionItemKind;
                        return !o.Values.length ? [] : o.Values.split(";").map(s => ({
                            kind: {
                                verb: kind.Keyword,
                                compareOperator: kind.Keyword,
                                columnName: kind.Field,
                            }[o.ItemCategory] || kind.Text,
                            label: s,
                            insertText: trunate ? s.slice(1) : s,
                        }))
                    })
            })

    		this.editor = monaco.editor.create(document.getElementById('queryEditor'), {
    			value: this.initialQuery,
    			language: 'xform',
                scrollBeyondLastLine: false,
                minimap: { enabled: false },
                automaticLayout: true,
                theme: 'xform',
                overviewRulerBorder: false,
                occurrencesHighlight: false,
                hideCursorInOverviewRuler: true,
    		});
            extendEditor(this.editor)

            this.editor.onDidChangeModelContent(this.queryTextChanged.bind(this))
            this.editor.onDidChangeCursorPosition(e => {
                if (this.textJustChanged) this.queryAndCursorChanged()
                this.textJustChanged = false
            })

            this.validQuery = this.query
            this.queryChanged()
    	});
    }
    componentDidUpdate(prevProps, prevState) {
        if (prevState.peek !== this.state.peek) {
            const params = this.state.peek
                ? { q: `${this.query}\npeek [${this.state.peek.name}]` }
                : undefined
            this.reqPeek.update(params, json => {
                this.setState({ peekData: json && json.rows.sort((a, b) => b[1] - a[1]) && json.rows.slice(0, 7) })
            })
        }
    }
    get suggest() {
        return xhr(`suggest`, { asof: this.state.asOf, q: this.editor.valueUntilPosition() })
    }
    get query() {
        return this.editor && this.editor.getValue()
    }
    queryTextChanged(force) {
        this.textJustChanged = true
        const trimmedQuery = this.query.trim() // Pre async capture
        xhr(`suggest`, { asof: this.state.asOf, q: this.query }).then(info => {
            if (info.Valid && (force || this.validQuery !== trimmedQuery)) {
                this.validQuery = trimmedQuery
                this.debouncedQueryChanged()
            }

            const errorMessage = info.ErrorMessage
            if (errorMessage !== this.state.errorMessage) this.setState({ errorMessage })

            const usage = info.Usage
            if (usage !== this.state.usage) this.setState({ usage })

            const queryHint = !info.InvalidToken && info.ItemCategory || ''
            if (queryHint != this.state.queryHint) this.setState({ queryHint })

            this.editor.decorate(info.ErrorMessage // Need to verify info.InvalidTokenIndex < this.query.length?
                ? [{
                    range: this.editor.indexToPosition(info.InvalidTokenIndex).toRange(info.InvalidToken.length),
                    options: { inlineClassName: 'validationError' },
                }]
                : [])
        })
        setTimeout(() => {
            const ia = document.querySelector('.inputarea').style
            const qh = document.querySelector('.queryHint').style
            qh.top = parseInt(ia.top) + 1 + 'px'
            qh.left = ia.left
        })
    }
    queryAndCursorChanged() {
        const q = this.editor.valueUntilPosition()
        this.suggest.then(suggestions => {
            if (suggestions.Values && (suggestions.InvalidTokenIndex < q.length || /[\s\(]$/.test(q))) {
                this.suggestions = suggestions
                this.editor.trigger('source', 'editor.action.triggerSuggest', {});
            }
        })
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
                {this.state.peekData.map((row, i) => <div key={i} className="peek-value">
                    <span>{row[0] === '' ? '—' : row[0] }</span>
                    <span>{row[2]}</span>
                </div>)}
            </div>
        }

        const formatters = rows && cols.map(col => col === "Count" || col.endsWith(".Sum")
                ? cell => cell === "" ? "—" : (+cell).toLocaleString()
                : cell => cell)

        const encodedQuery = encodeURIComponent(this.validQuery)

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
                    <select onChange={e => this.setState({ asOf: e.target.value || undefined }, () => this.queryTextChanged(true))}>
                        <option value="">As of Now</option>
                        <option value={Date.daysAgo(1).toXFormat()}>As of Yesterday</option>
                        <option value={Date.daysAgo(7).toXFormat()}>As of Last Week</option>
                        <option value={Date.firstOfMonth().toXFormat()}>As of {(new Date()).toLocaleString('en-us', { month: "long" })} 1st</option>
                    </select>
                </div>
                <div className="queryUsage">{
                    this.state.errorMessage && <span className="errorMessage">{this.state.errorMessage}</span>
                    || this.state.usage || `\u200B`
                }</div>
                <div id="queryEditor">
                    <div className="queryHint">{this.state.queryHint}</div>
                </div>
            </div>
            <div id="schema">
                <div className="schemaHeader">
                    {!this.state.userCols.length && this.state.schemaBody && <span>{this.state.schemaBody.length} Columns</span>}
                    {!!this.state.userCols.length && <span className="button" onClick={e => this.setState({ userCols: [] }, () => this.limitChanged())}>Reset</span>}
                    <span className="flexFill"></span>
                    {!!this.state.userCols.length && <span className="button" onClick={e => {
                        const newLine = this.query.endsWith('\n') ? '' : '\n'
                        const userCols = this.state.userCols.length && `${newLine}select ${this.state.userCols.map(c => `[${c}]`).join(', ')}` || ''
                        const r = this.editor.getModel().getFullModelRange()
                        this.editor.executeEdits('my-source', [{
                                identifier: { major: 1, minor: 1 },
                                range: new monaco.Range(r.endLineNumber, r.endColumn, r.endLineNumber, r.endColumn),
                                text: userCols,
                                forceMoveMarkers: true,
                            }])
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
                    {encodedQuery && <a className="button" target="_blank" href={`${this.serviceUrl}/download?fmt=csv&q=${encodedQuery}`}>CSV</a>}
                    {encodedQuery && <a className="button" target="_blank" href={`${this.serviceUrl}/download?fmt=tsv&q=${encodedQuery}`}>TSV</a>}
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
                                {cols && cols.map(c => <td key={c}>{c}</td>)}
                            </tr>
                        </thead>
                        <tbody>
                            {rows && rows.map((r, i) => <tr key={i}>{r.map((c, ii) => <td key={i + "x" + ii}>{ formatters[ii](c) }</td>)}</tr>)}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>,
        <Peek key="peek" />]
    }
}

ReactDOM.render(<Index />, document.getElementById("app"))
