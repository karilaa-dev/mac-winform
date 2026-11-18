const vscode = require('vscode');
const { spawn } = require('node:child_process');
const fs = require('node:fs/promises');
const path = require('node:path');
const os = require('node:os');

async function buildForDebugger(cli, project, output, launchToken) {
    const temp = await fs.mkdtemp(path.join(os.tmpdir(), 'mac-winform-debug-'));
    const report = path.join(temp, 'app-path');
    try {
        const ok = await vscode.window.withProgress({
            location: vscode.ProgressLocation.Notification,
            title: 'Building ' + path.basename(project), cancellable: true
        }, async (_progress, token) => {
            if (token.isCancellationRequested || launchToken?.isCancellationRequested) return false;
            const child = spawn('python3', [cli, 'build', project, '--app-path-file', report],
                { detached: true, cwd: path.dirname(project), stdio: ['ignore', 'pipe', 'pipe'] });
            const cancel = () => {
                if (child.pid) {
                    try { process.kill(-child.pid, 'SIGTERM'); }
                    catch (error) { if (error.code !== 'ESRCH') throw error; }
                }
            };
            const uiCancel = token.onCancellationRequested(cancel);
            const sessionCancel = launchToken?.onCancellationRequested(cancel);
            output.appendLine('Building ' + project);
            for (const stream of [child.stdout, child.stderr]) {
                stream.setEncoding('utf8');
                stream.on('data', text => output.append(text));
            }
            try {
                const code = await new Promise((resolve, reject) => {
                    child.once('error', reject);
                    child.once('close', resolve);
                });
                if (token.isCancellationRequested || launchToken?.isCancellationRequested) return false;
                if (code !== 0) {
                    output.show(true);
                    throw new Error('Mac WinForms build failed. See the Mac WinForms output channel.');
                }
                return true;
            } finally {
                uiCancel.dispose();
                sessionCancel?.dispose();
            }
        });
        if (ok) return (await fs.readFile(report, 'utf8')).trimEnd();
    } finally {
        await fs.rm(temp, { recursive: true, force: true });
    }
}
module.exports = { buildForDebugger };
