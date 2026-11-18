const vscode = require('vscode');
const fs = require('node:fs');
const path = require('node:path');
const { RunAdapter } = require('./run-adapter');

const { buildForDebugger } = require('./debug-build');

const type = 'mac-winform';
const exclude = '**/{bin,obj,.git,node_modules}/**';
const launch = { name: 'Mac WinForms: Launch Project', type, request: 'launch' };

async function projects(folder) {
    const folders = folder ? [folder] : (vscode.workspace.workspaceFolders || []);
    const results = await Promise.all(folders.map(async scope => {
        const uris = await vscode.workspace.findFiles(new vscode.RelativePattern(scope, '**/*.{csproj,vbproj,fsproj}'), exclude);
        return uris.map(uri => ({ uri, scope }));
    }));
    return results.flat().sort((a, b) => a.uri.fsPath.localeCompare(b.uri.fsPath));
}

async function selectProject(folder, uri) {
    if (uri) return { uri, scope: vscode.workspace.getWorkspaceFolder(uri) };
    const matches = await projects(folder);
    if (!matches.length) {
        await vscode.window.showErrorMessage('Open a workspace folder that contains your WinForms project.');
        return;
    }
    if (matches.length === 1) return matches[0];
    // Prefer obvious WinForms executables in mixed solutions, while retaining
    // the picker for projects whose settings come from imported props.
    const hints = await Promise.all(matches.map(async item => {
        try {
            const xml = await fs.promises.readFile(item.uri.fsPath, 'utf8');
            const forms = /<UseWindowsForms>\s*true\s*<\/UseWindowsForms>/i.test(xml)
                || /<Reference\s+Include=["']System\.Windows\.Forms(?:["',])/i.test(xml);
            return forms
                && /<OutputType>\s*(?:WinExe|Exe)\s*<\/OutputType>/i.test(xml);
        } catch { return false; }
    }));
    const hinted = matches.filter((_, i) => hints[i]);
    if (hinted.length === 1) return hinted[0];
    const picked = await vscode.window.showQuickPick(matches.map(item => ({
        label: path.basename(item.uri.fsPath),
        description: vscode.workspace.asRelativePath(item.uri, true),
        item
    })), { title: 'Choose the WinForms application project', matchOnDescription: true });
    return picked?.item;
}

function activate(context) {
    if (process.platform !== 'darwin' || !vscode.workspace.isTrusted) return;
    const bundled = context.asAbsolutePath('bin/mac-winform');
    const cli = fs.existsSync(bundled) ? bundled : context.asAbsolutePath('../bin/mac-winform');

    function makeTask(action, project, args = [], definition) {
        if (!['build', 'run'].includes(action)) throw new Error('Unknown Mac WinForms action.');
        if (!Array.isArray(args) || !args.every(arg => typeof arg === 'string')) throw new Error('Application arguments must be a list of strings.');
        const file = project.uri.fsPath;
        const relative = project.scope ? path.relative(project.scope.uri.fsPath, file) : file;
        const taskArgs = [cli, action, file];
        if (action === 'run' && args.length) taskArgs.push('--', ...args);
        const task = new vscode.Task(definition || { type, action, project: relative },
            project.scope || vscode.TaskScope.Workspace, action + ' ' + relative, 'Mac WinForms',
            new vscode.ProcessExecution('python3', taskArgs, { cwd: path.dirname(file) }), '$msCompile');
        if (action === 'build') task.group = vscode.TaskGroup.Build;
        return task;
    }

    const subscriptions = context.subscriptions;
    const output = vscode.window.createOutputChannel('Mac WinForms');
    subscriptions.push(output);
    for (const action of ['run', 'build']) {
        subscriptions.push(vscode.commands.registerCommand(type + '.' + action, async uri => {
            const project = await selectProject(undefined, uri);
            if (!project || !await vscode.workspace.saveAll(false)) return;
            return vscode.tasks.executeTask(makeTask(action, project));
        }));
    }
    subscriptions.push(vscode.tasks.registerTaskProvider(type, {
        async provideTasks() {
            return (await projects()).flatMap(project => ['build', 'run'].map(action => makeTask(action, project)));
        },
        resolveTask(task) {
            const def = task.definition;
            if (!task.scope?.uri || typeof def.project !== 'string') return;
            const uri = vscode.Uri.file(path.resolve(task.scope.uri.fsPath, def.project));
            return makeTask(def.action, { uri, scope: task.scope }, def.args, def);
        }
    }));

    const provider = {
        async provideDebugConfigurations(folder) {
            return (await projects(folder)).length ? [{ ...launch }] : [];
        },
        async resolveDebugConfiguration(folder, input, token) {
            const config = { ...launch, ...input };
            let uri;
            if (config.project) {
                if (!path.isAbsolute(config.project) && !folder) throw new Error('Open the project folder to use a relative project path.');
                const projectPath = config.project.replaceAll('${workspaceFolder}', folder?.uri.fsPath || '');
                if (projectPath.includes('${')) throw new Error('Use an absolute project path or a path relative to the workspace folder.');
                uri = vscode.Uri.file(path.resolve(folder?.uri.fsPath || '/', projectPath));
            }
            const project = await selectProject(folder, uri);
            if (!project || token?.isCancellationRequested || !await vscode.workspace.saveAll(false)) return;
            const file = project.uri.fsPath;
            if (!config.noDebug && vscode.extensions.getExtension('ms-dotnettools.csharp')) {
                const program = await buildForDebugger(cli, file, output, token);
                if (!program) return;
                const { project: ignored, ...debugConfig } = config;
                return { ...debugConfig, type: 'coreclr', program, cwd: path.dirname(file), console: 'internalConsole' };
            }
            return { ...config, project: file, noDebug: true };
        }
    };
    subscriptions.push(vscode.debug.registerDebugConfigurationProvider(type, provider, vscode.DebugConfigurationProviderTriggerKind.Dynamic));
    subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory(type, {
        createDebugAdapterDescriptor() {
            return new vscode.DebugAdapterInlineImplementation(new RunAdapter(cli));
        }
    }));
}
module.exports = { activate };
