// Usage: node build [configSource]
// Copies contents of `configSource` into `~\configuration`. These contents override Arriba.Web custom UI and theming.
//   If `~\configuration` had newer edits, those are first copied back to `configCustom`.
//   If `~\configuration` existed, the contents will be backed up to `~\configuration.BAK`
//   If no `configCustom` is given, default will be `~\configuration.default`. However, edits are never copied back to default.

const fs = require('fs')
const path = require('path');
const {execSync} = require('child_process')
function run(cmd) { try { execSync(cmd, { stdio: [0, 1, 2] }) } catch(e) {} } // Robocopy exit code isn't always 0, suppressing all errors for now.

// Verify a configuration folder was passed
if(!process.argv[2])
    return console.log(`ERROR. Pass a configuration folder to build.js [pass 'configuration.default' for the default experience]`)

const configDestination = `${__dirname}\\configuration`
const configSource = path.resolve(process.argv[2])

// Verify the configuration folder has a configuration.jsx
if (!fs.existsSync(`${configSource}\\Configuration.jsx`))
    return console.log(`ERROR. '${configSource}' didn't contain a Configuration.jsx. Is your path right?`)

// Validate webpack available
if (!fs.existsSync(`node_modules\\.bin\\webpack.cmd`))
    return console.log(`ERROR. 'node_modules\\.bin\\webpack.cmd'not found. Did you run "npm install" from Arriba.Web?`)

// Avoid losing work by copying any edits back to the previous config source
if(fs.existsSync(`${configDestination}\\SourcePath.txt`)) {
    var previousSource = fs.readFileSync(`${configDestination}\\SourcePath.txt`).toString()
    console.log(`\n - Copying changes back to "${previousSource}"...`)
    run(`ROBOCOPY /E /XO /NJH /NJS "${configDestination}" "${previousSource}" /XF SourcePath.txt`)
}

// Back up existing configuration folder.
if (fs.existsSync(configDestination)) {
    var backup = `${configDestination}.BAK`
    console.log(`\n - Saving current configuration as "${backup}"...`)
    if (fs.existsSync(backup)) run(`RMDIR /S /Q "${backup}"`)
    run(`MOVE "${configDestination}" "${backup}"`)
}

// Copy passed configuration (finally).
console.log(`\n - Getting configuration "${configSource}"...`)
run(`ROBOCOPY /E /MIR /NJH /NJS "${configSource}" "${configDestination}"`)
fs.writeFileSync(`${configDestination}\\SourcePath.txt`, configSource);

// Run WebPack to bundle the site
console.log(`\n - Building Arriba.Web bundle...`)
run(`node_modules\\.bin\\webpack.cmd`)
