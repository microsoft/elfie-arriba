// Usage: node build [configSource]
// Copies contents of `configSource` into `~\configuration`. These contents override Arriba.Web custom UI and theming.
//   If `~\configuration` had newer edits, those are first copied back to `configCustom`.
//   If `~\configuration` existed, the contents will be backed up to `~\configuration.BAK`
//   If no `configCustom` is given, default will be `~\configuration.default`. However, edits are never copied back to default.

const fs = require('fs')
const {execSync} = require('child_process')
function run(cmd) { try { execSync(cmd, { stdio: [0, 1, 2] }) } catch(e) {} } // Robocopy exit code isn't always 0, suppressing all errors for now.

const wantSync = true // Set false if you're just testing
const configDestination = `${__dirname}\\configuration`
const configDefault = `${__dirname}\\configuration.default`
const configSource = process.argv[2] || configDefault

// Validate input.
if (!fs.existsSync(`${configSource}\\Configuration.jsx`))
    return console.log(`ERROR. '${configSource}' didn't contain a Configuration.jsx. Is your path right?`)

// Copy any newer edits from configDestination back to configSource.
if (wantSync && configSource !== configDefault && fs.existsSync(configDestination)) {
    console.log(`Copying reverse...\n  ${configSource}\n  ${configDestination}`)
    run(`ROBOCOPY /E /XO /NJH /NJS "${configDestination}" "${configSource}"`)
}

// Backup existing config.
if (fs.existsSync(configDestination)) {
    var backup = `${configDestination}.BAK`
    console.log(`Backing up...\n  ${configDestination}\n  ${backup}`)
    if (fs.existsSync(backup))
        run(`RMDIR /S /Q "${backup}"`)
    run(`MOVE "${configDestination}" "${backup}"`)
}

// Sync source -> destination.
console.log(`Copying forward...\n  ${configSource}\n  ${configDestination}`)
run(`ROBOCOPY /E /NJH /NJS "${configSource}" "${configDestination}"`)

// Validate webpack available
if (!fs.existsSync(`node_modules\\.bin\\webpack.cmd`))
    return console.log(`ERROR. 'node_modules\\.bin\\webpack.cmd'not found. Did you run "npm install" from Arriba.Web?`)

// Run webpack.
console.log(`Building Website...`)
run(`node_modules\\.bin\\webpack.cmd`)
