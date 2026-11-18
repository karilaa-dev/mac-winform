// Run on macOS with: swift tests/Gallery/capture-macos.swift OUTPUT_DIR
import Foundation
import CoreGraphics
import ImageIO
import UniformTypeIdentifiers

struct CaptureError: LocalizedError {
    let reason: String
    var errorDescription: String? { reason }
}

func command(_ executable: URL, _ arguments: [String]) throws {
    let process = Process()
    process.executableURL = executable
    process.arguments = arguments
    try process.run()
    process.waitUntilExit()
    if process.terminationStatus != 0 {
        throw CaptureError(reason: "\(executable.lastPathComponent) exited with \(process.terminationStatus)")
    }
}

func capture(_ output: URL) throws {
    let fm = FileManager.default
    let gallery = URL(fileURLWithPath: #filePath).deletingLastPathComponent()
    let scratch = fm.temporaryDirectory.appendingPathComponent("winforms-gallery-" + UUID().uuidString)
    try fm.createDirectory(at: scratch, withIntermediateDirectories: true)
    defer { try? fm.removeItem(at: scratch) }
    let appPath = scratch.appendingPathComponent("app-path")
    try command(URL(fileURLWithPath: "/usr/bin/env"), ["dotnet", "winforms", "build", gallery.path,
                                                       "--app-path-file", appPath.path])
    let executable = try String(contentsOf: appPath, encoding: .utf8).trimmingCharacters(in: .whitespacesAndNewlines)
    let app = Process()
    app.executableURL = URL(fileURLWithPath: executable)
    app.currentDirectoryURL = gallery
    app.arguments = ["--probe", scratch.path]
    try app.run()
    defer { if app.isRunning { app.terminate() } }

    let state = scratch.appendingPathComponent("state.json")
    let deadline = Date().addingTimeInterval(30)
    var windowNumber: NSNumber?
    while Date() < deadline && app.isRunning {
        let windows = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], 0) as? [[String: Any]]
        if fm.fileExists(atPath: state.path),
           let window = windows?.first(where: {
               ($0[kCGWindowOwnerPID as String] as? NSNumber)?.int32Value == app.processIdentifier &&
               ($0[kCGWindowName as String] as? String) == "Record Library"
           }) {
            windowNumber = window[kCGWindowNumber as String] as? NSNumber
            break
        }
        Thread.sleep(forTimeInterval: 0.05)
    }
    guard let windowNumber = windowNumber else {
        throw CaptureError(reason: "The gallery did not present a verified window. Check probe diagnostics and screen capture permission.")
    }
    Thread.sleep(forTimeInterval: 0.25)
    let frame = scratch.appendingPathComponent("window.png")
    try command(URL(fileURLWithPath: "/usr/sbin/screencapture"), ["-x", "-o", "-l", windowNumber.stringValue, frame.path])

    let data = try Data(contentsOf: state)
    let reported = try JSONSerialization.jsonObject(with: data) as? [String: Any]
    guard let size = reported?["Client"] as? [Int], size.count == 2,
          let source = CGImageSourceCreateWithURL(frame as CFURL, nil),
          let image = CGImageSourceCreateImageAtIndex(source, 0, nil) else {
        throw CaptureError(reason: "Invalid gallery metrics or window capture.")
    }
    let scale = CGFloat(image.width) / CGFloat(size[0])
    let height = Int((CGFloat(size[1]) * scale).rounded())
    guard let client = image.cropping(to: CGRect(x: 0, y: image.height - height, width: image.width, height: height)) else {
        throw CaptureError(reason: "Window capture is smaller than its reported client area.")
    }
    try fm.createDirectory(at: output, withIntermediateDirectories: true)
    let png = output.appendingPathComponent("gallery.png")
    guard let destination = CGImageDestinationCreateWithURL(png as CFURL, UTType.png.identifier as CFString, 1, nil) else {
        throw CaptureError(reason: "Cannot open the output PNG.")
    }
    CGImageDestinationAddImage(destination, client, nil)
    guard CGImageDestinationFinalize(destination) else { throw CaptureError(reason: "Cannot save the client image.") }

    app.waitUntilExit()
    guard app.terminationStatus == 0 else { throw CaptureError(reason: "Gallery probe exit \(app.terminationStatus).") }
    try data.write(to: output.appendingPathComponent("state.json"), options: .atomic)
    print("Captured \(client.width) x \(client.height) client pixels at \(png.path)")
}

guard CommandLine.arguments.count == 2 else {
    FileHandle.standardError.write(Data("Usage: swift capture-macos.swift OUTPUT_DIR\n".utf8))
    exit(2)
}
do { try capture(URL(fileURLWithPath: CommandLine.arguments[1])) }
catch { FileHandle.standardError.write(Data("\(error.localizedDescription)\n".utf8)); exit(1) }
