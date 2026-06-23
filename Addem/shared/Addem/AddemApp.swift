import SwiftUI

@main
struct AddemApp: App {
    @StateObject private var store = LinkStore()

    var body: some Scene {
        WindowGroup("AddEm") {
            ContentView()
                .environmentObject(store)
                .frame(minWidth: 820, minHeight: 560)
        }

        MenuBarExtra("AddEm", systemImage: "link.badge.plus") {
            MenuBarAddView()
                .environmentObject(store)
        }
        .menuBarExtraStyle(.window)
        .commands {
            CommandMenu("AddEm") {
                Button("Quick Add") {
                    NotificationCenter.default.post(name: .addemOpenQuickAdd, object: nil)
                }
                .keyboardShortcut("a", modifiers: [.command, .shift])

                Button("Print Day 1 Metrics") {
                    AddemAnalytics.shared.printDailySummary()
                }
            }
        }
    }
}
