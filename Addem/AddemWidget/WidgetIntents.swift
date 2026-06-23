import AppIntents
import Foundation

struct OpenQuickAddIntent: AppIntent {
    static var title: LocalizedStringResource = "Add Link"
    static var description = IntentDescription("Open AddEm and show the quick add sheet.")
    static var openAppWhenRun = true

    func perform() async throws -> some IntentResult {
        let defaults = UserDefaults(suiteName: AppGroupConfig.groupID)
        defaults?.set(true, forKey: AppGroupConfig.quickAddFlagKey)
        return .result()
    }
}

struct TogglePinIntent: AppIntent {
    static var title: LocalizedStringResource = "Toggle Pin"
    static var description = IntentDescription("Pin or unpin this link.")

    @Parameter(title: "Link ID")
    var linkID: String

    init() {}

    init(linkID: String) {
        self.linkID = linkID
    }

    func perform() async throws -> some IntentResult {
        guard let id = UUID(uuidString: linkID) else {
            return .result()
        }

        await MainActor.run {
            let store = LinkStore()
            store.togglePinned(id)
        }

        return .result()
    }
}

struct OpenTopLinkIntent: AppIntent {
    static var title: LocalizedStringResource = "Open Top Link"
    static var description = IntentDescription("Open your top pinned or recent link in AddEm.")
    static var openAppWhenRun = true

    func perform() async throws -> some IntentResult {
        let all = LinkStoreReader.load()
        let ranked = rankedLinks(all)

        guard let top = ranked.first else {
            return .result()
        }

        let defaults = UserDefaults(suiteName: AppGroupConfig.groupID)
        defaults?.set(top.urlString, forKey: AppGroupConfig.quickOpenURLKey)
        return .result()
    }

    private func rankedLinks(_ links: [LinkItem]) -> [LinkItem] {
        links.sorted { lhs, rhs in
            if lhs.isPinned != rhs.isPinned {
                return lhs.isPinned && !rhs.isPinned
            }
            let leftDate = lhs.lastOpenedAt ?? lhs.createdAt
            let rightDate = rhs.lastOpenedAt ?? rhs.createdAt
            return leftDate > rightDate
        }
    }
}
