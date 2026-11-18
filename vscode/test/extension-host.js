// Point VS Code's --extensionTestsPath here with tests/Controls as its workspace.
// In a profile with Microsoft's C# extension, this also exercises a real breakpoint.
const assert = require('node:assert/strict');
const vscode = require('vscode');

async function launch(folder, debug) {
    const type = debug ? 'coreclr' : 'mac-winform';
    let exitCode;
    let hit = false;
    const tracker = vscode.debug.registerDebugAdapterTrackerFactory(type, {
        createDebugAdapterTracker(session) {
            return { onDidSendMessage(message) {
                if (message.event === 'exited') exitCode = message.body.exitCode;
                if (message.event === 'stopped' && message.body.reason === 'breakpoint') {
                    hit = true;
                    void session.customRequest('continue', { threadId: message.body.threadId });
                }
            } };
        }
    });
    let finish;
    const finished = new Promise(resolve => { finish = resolve; });
    const end = vscode.debug.onDidTerminateDebugSession(session => {
        if (session.type === type) finish();
    });
    let timeout;
    try {
        const config = { type: 'mac-winform', request: 'launch', name: 'Mac WinForms test' };
        assert.ok(await vscode.debug.startDebugging(folder, config, { noDebug: !debug }));
        await Promise.race([finished, new Promise((_, reject) => {
            timeout = setTimeout(() => reject(new Error('Timed out waiting for ' + type)), 90000);
        })]);
        assert.equal(exitCode, 0);
        if (debug) assert.ok(hit, 'Expected to hit a Forms source breakpoint');
    } finally {
        clearTimeout(timeout);
        end.dispose();
        tracker.dispose();
    }
}

exports.run = async () => {
    const folder = vscode.workspace.workspaceFolders[0];
    await vscode.extensions.getExtension('local.mac-winform').activate();
    const tasks = await vscode.tasks.fetchTasks({ type: 'mac-winform' });
    assert.deepEqual(tasks.map(task => task.definition.action).sort(), ['build', 'run']);
    await launch(folder, false);
    if (vscode.extensions.getExtension('ms-dotnettools.csharp')) {
        const doc = await vscode.workspace.openTextDocument(vscode.Uri.joinPath(folder.uri, 'Program.cs'));
        const pos = doc.positionAt(doc.getText().indexOf('grid.DataSource ='));
        const bp = new vscode.SourceBreakpoint(new vscode.Location(doc.uri, pos));
        vscode.debug.addBreakpoints([bp]);
        try { await launch(folder, true); }
        finally { vscode.debug.removeBreakpoints([bp]); }
    }
    console.log('Mac WinForms VS Code launch tests passed.');
};
