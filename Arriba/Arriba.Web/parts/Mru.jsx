// Returns number of milliseconds between this date and now.
Date.prototype.elapsedMilliseconds = function() {
    return Date.now() - this.getTime();
}

// Listens to query changes via update() and push(), filters out non-"meaningful" queries,
// and then saves to localStorage. "Meaningful" is currently defined having not changed for 3 seconds.
export default class {
    constructor() {
        this.lastUpdate = new Date(8640000000000000); // Max Date
    }
    update(incoming) {
        if (!incoming) return;

        incoming = incoming.trim();
        if (incoming.length <= 2) return;

        if (this.lastUpdate && this.lastUpdate.elapsedMilliseconds() > 3000 && this.current) this.push();

        this.current = incoming;
        this.lastUpdate = new Date();
    }
    push() {
        if (!this.current) return;
        localStorage.updateJson("recents", r => [this.current, ...(r || []).remove(this.current)].slice(0, 100));
    }
}
