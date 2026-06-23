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
    }
}
