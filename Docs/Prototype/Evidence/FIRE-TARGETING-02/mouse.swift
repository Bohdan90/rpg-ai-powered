import AppKit
import ApplicationServices
let args=CommandLine.arguments
let windows=CGWindowListCopyWindowInfo([.optionOnScreenOnly,.excludeDesktopElements],kCGNullWindowID) as? [[String:Any]] ?? []
guard let w=windows.first(where:{($0[kCGWindowName as String] as? String ?? "").contains(" - Fire-Targeting-02 - ")}),
let pid=w[kCGWindowOwnerPID as String] as? Int32, let app=NSRunningApplication(processIdentifier:pid) else {print("No Fire-Targeting-02 window");exit(1)}
if args[1]=="quit" { app.terminate(); exit(0) }
if NSWorkspace.shared.frontmostApplication?.processIdentifier != pid {
app.activate(options:[.activateAllWindows])
let ax=AXUIElementCreateApplication(pid)
AXUIElementSetAttributeValue(ax,kAXFrontmostAttribute as CFString,kCFBooleanTrue)
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.5))
}
guard NSWorkspace.shared.frontmostApplication?.processIdentifier==pid else {print("Wrong foreground app");exit(2)}
if args[1]=="focus" { print(w);exit(0) }
if args[1]=="inspect" {
 print("front",NSWorkspace.shared.frontmostApplication?.localizedName ?? "none")
 for screen in NSScreen.screens {print("screen",screen.localizedName,screen.frame)}
 for win in (CGWindowListCopyWindowInfo([.optionOnScreenOnly,.excludeDesktopElements],kCGNullWindowID) as? [[String:Any]] ?? []).prefix(10) {print(win[kCGWindowOwnerName as String] ?? "",win[kCGWindowBounds as String] ?? "")}
 let p=Process();p.executableURL=URL(fileURLWithPath:"/usr/sbin/screencapture");p.arguments=["-x","-R0,33,1708,994","/private/tmp/convergence-5051/manual-complete/focused-region.png"];try! p.run();p.waitUntilExit();exit(0)
}
if args[1]=="key" {
 let code:CGKeyCode = ["up":126,"down":125,"return":36,"escape":53,"refresh":15,"home":115][args[2]]!
 for down in [true,false] {let e=CGEvent(keyboardEventSource:nil,virtualKey:code,keyDown:down);if args[2]=="refresh" {e?.flags = .maskCommand};e?.post(tap:.cghidEventTap);usleep(80000)}
 exit(0)
}
print("trusted",AXIsProcessTrusted(),"before",CGEvent(source:nil)!.location)
let point=CGPoint(x:Double(args[1])!,y:Double(args[2])!)
CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap)
if args.count<4 || args[3] != "hover" {
for t in [CGEventType.leftMouseDown,CGEventType.leftMouseUp] {
let e=CGEvent(mouseEventSource:nil,mouseType:t,mouseCursorPosition:point,mouseButton:.left)
e?.setIntegerValueField(.mouseEventClickState,value:args.count>3 && args[3]=="double" ? 2 : 1);e?.post(tap:.cghidEventTap);usleep(50000)
}
}
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.04))

print("after",CGEvent(source:nil)!.location)
