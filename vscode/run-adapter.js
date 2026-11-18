const vscode = require('vscode');
const { spawn } = require('node:child_process');
const path = require('node:path');

// F5 is a launch-only session. The debug toolbar owns the CLI and its child
// processes, so Stop also works while dotnet is still building the project.
class RunAdapter {
    constructor(cli) {
        this.cli = cli;
        this.events = new vscode.EventEmitter();
        this.onDidSendMessage = this.events.event;
        this.sequence = 0;
    }

    send(message) {
        this.events.fire({ ...message, seq: ++this.sequence });
    }

    event(event, body) {
        this.send({ type: 'event', event, body });
    }

    handleMessage(request) {
        if (request.type !== 'request') return;
        const respond = (body = {}, success = true, message) => this.send({
            type: 'response', request_seq: request.seq, command: request.command, success, body, message
        });
        switch (request.command) {
            case 'initialize':
                respond({ supportsTerminateRequest: true });
                this.event('initialized');
                break;
            case 'launch': {
                const { project, args = [] } = request.arguments;
                if (!Array.isArray(args) || args.some(arg => typeof arg !== 'string')) {
                    respond({}, false, 'Application arguments must be a list of strings.');
                    return;
                }
                const child = spawn('python3', [this.cli, 'run', project, '--', ...args], {
                    detached: true, cwd: path.dirname(project), stdio: ['ignore', 'pipe', 'pipe']
                });
                this.child = child;
                for (const category of ['stdout', 'stderr']) {
                    child[category].setEncoding('utf8');
                    child[category].on('data', output => this.event('output', { category, output }));
                }
                child.once('spawn', () => respond());
                child.once('error', error => respond({}, false, error.message));
                child.once('close', (code, signal) => {
                    this.child = undefined;
                    if (code !== 0) this.event('output', { category: 'console', output: 'Mac WinForms exited with ' + (signal || code) + '\n' });
                    this.event('exited', { exitCode: code ?? 1 });
                    this.event('terminated');
                });
                break;
            }
            case 'disconnect':
            case 'terminate':
                this.stop();
                respond();
                break;
            case 'threads':
                respond({ threads: [] });
                break;
            case 'setExceptionBreakpoints':
            case 'configurationDone':
                respond();
                break;
            default:
                respond({}, false, 'Mac WinForms supports launch and stop.');
        }
    }

    stop() {
        if (!this.child?.pid) return;
        try {
            process.kill(-this.child.pid, 'SIGTERM');
        } catch (error) {
            if (error.code !== 'ESRCH') throw error;
        }
    }

    dispose() {
        this.stop();
        this.events.dispose();
    }
}
module.exports = { RunAdapter };
